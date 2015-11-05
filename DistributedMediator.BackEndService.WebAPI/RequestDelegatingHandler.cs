using DistributedMediator.Requests;
using MediatR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace DistributedMediator.BackEndService.WebAPI
{
    public class RequestDelegatingHandler: DelegatingHandler
    {
        private readonly IMediator _mediator;
        private readonly RequestTypeProvider _requestTypeProvider;

        public RequestDelegatingHandler(IMediator mediator, RequestTypeProvider requestTypeProvider)
        {
            _mediator = mediator;
            _requestTypeProvider = requestTypeProvider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            //allow POST or GET only
            if (!new[] { HttpMethod.Post, HttpMethod.Get }.Contains(request.Method))
            {
                return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.MethodNotAllowed));
            }

            var routeData = request.GetRouteData();

            //check content-type header for request-type
            var requestTypeString = routeData.Values.ContainsKey("requestType") ? (string)routeData.Values["requestType"] : null;

            if (requestTypeString == null)
            {
                if (request.Method.Equals(HttpMethod.Get))
                {
                    //return all request types and signatures
                    var allowedRequestTypes = _requestTypeProvider.RequestTypes;

                    var queryDict = new Dictionary<string, string>();
                    var commandDict = new Dictionary<string, string>();

                    using (var fs = new FileStream(AppDomain.CurrentDomain.RelativeSearchPath + @"\DistributedMediator.Requests.xml", FileMode.Open))
                    {
                        var doc = XDocument.Load(fs);

                        var queryTypes = allowedRequestTypes.Where(x => !typeof(IRequest).IsAssignableFrom(x));
                        var commandTypes = allowedRequestTypes.Except(queryTypes);

                        foreach (var rt in queryTypes)
                        {
                            queryDict.Add(rt.FullName, doc.Descendants("member").Single(x => "T:" + rt.FullName == x.Attribute("name").Value).Element("summary").Value);
                        }

                        foreach (var rt in commandTypes)
                        {
                            commandDict.Add(rt.FullName, doc.Descendants("member").Single(x => "T:" + rt.FullName == x.Attribute("name").Value).Element("summary").Value);
                        }
                    }

                    SanitizeDescription(queryDict);
                    SanitizeDescription(commandDict);

                    var uriBase = request.RequestUri.OriginalString;
                    uriBase += uriBase.EndsWith("/") ? string.Empty : "/";

                    var resourceUrl = string.Format("{0}{{0}}/", uriBase);


                    var meta = new {queries = queryDict.Select(x => new { Name = x.Key, Description= x.Value, @Uri = string.Format(resourceUrl, x.Key) }), commands = commandDict.Select(x => new { Name = x.Key, Description= x.Value, @Uri = string.Format(resourceUrl, x.Key) })};

                    return request.CreateResponse(meta);
                }

                return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));
            }


            var requestType = _requestTypeProvider.FindByFullName(requestTypeString);

            if (requestType == null)
            {
                throw new ArgumentException("Unprocessable request");
            }

            var isAsyncRequest = requestType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IAsyncRequest<>));

            if (request.Method.Equals(HttpMethod.Get))
            {
                using (var fs = new FileStream(AppDomain.CurrentDomain.RelativeSearchPath + @"\DistributedMediator.Requests.xml", FileMode.Open))
                {
                    var doc = XDocument.Load(fs);

                    var fieldsDesc = doc.Descendants("member").Where(x => x.Attribute("name").Value.StartsWith("P:" + requestType.FullName)).Select(x => new { name = x.Attribute("name").Value.Substring(x.Attribute("name").Value.LastIndexOf('.') + 1), description = SanitizeDescription(x.Element("summary").Value) });
                
                    return request.CreateResponse(new { sampleBody = Activator.CreateInstance(requestType), fieldDescriptions = fieldsDesc });
                }                
            }            

            dynamic content = await request.Content.ReadAsAsync(requestType);

            if (requestType.BaseType != typeof(object) && requestType.BaseType == typeof(FileRequest))
            {
                var response = request.CreateResponse(HttpStatusCode.OK);
                response.Content = new PushStreamContent(async (stream, httpContent, context) =>
                {
                    ((FileRequest)content).SetStream(stream);

                    if (isAsyncRequest)
                    {
                        await _mediator.SendAsync(content);
                    }
                    else
                    {
                        _mediator.Send(content);
                    }

                    stream.Close();
                }, "application/octet-stream");

                return response;
            }

            try
            {
                dynamic result;

                if (isAsyncRequest)
                {
                    result = await _mediator.SendAsync(content);
                }
                else
                {
                    result = _mediator.Send(content);
                }

                return request.CreateResponse((object)result);
            }
            catch (Exception exc)
            {
                //todo:log, etc.
                return request.CreateResponse(HttpStatusCode.InternalServerError, exc.GetBaseException().Message);
            }

        }

        private void SanitizeDescription(Dictionary<string, string> commandDict)
        {
            var keys = commandDict.Keys.ToArray();

            foreach (var key in keys)
            {
                commandDict[key] = SanitizeDescription(commandDict[key]);
            }
        }

        private string SanitizeDescription(string description)
        {
            return Regex.Replace(description.Trim(), @"\n(\s*)", ". ");
        }
    }
}