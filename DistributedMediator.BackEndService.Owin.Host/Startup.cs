using DistributedMediator.Requests;
using MediatR;
using Newtonsoft.Json;
using Owin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DistributedMediator.BackEndService.Owin.Host
{
    public class Startup
    {
        private readonly IMediator _mediator;
        private readonly RequestTypeProvider _requestTypeProvider;

        public Startup(IMediator mediator, RequestTypeProvider requestTypeProvider)
        {
            _mediator = mediator;
            _requestTypeProvider = requestTypeProvider;
        }

        public void Configuration(IAppBuilder app)
        {
            app.Run(async context =>
            {
                Console.WriteLine("Call to OWIN middleware");
                var requestPath = context.Request.Path;

                var handlerPathRegex = new Regex(@"^/requestHandler/request/([^/]*)(/?)$", RegexOptions.IgnoreCase);

                if(!handlerPathRegex.IsMatch(requestPath.Value))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return; 
                }

                if(context.Request.Method != "POST")
                {
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    return; 
                }

                var regexMatch = handlerPathRegex.Match(requestPath.Value);

                var requestTypeString = regexMatch.Groups[1].Value;

                if (string.IsNullOrEmpty(requestTypeString))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
                    return; 
                }

                var requestType = _requestTypeProvider.FindByFullName(requestTypeString);

                if (requestType == null)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotAcceptable;
                    return ;
                }

                string jsonPayload;

                using(var sr = new StreamReader(context.Request.Body))
                {
                    jsonPayload = await sr.ReadToEndAsync();
                }

                dynamic deserializedRequest = JsonConvert.DeserializeObject(jsonPayload, requestType);
                dynamic returnValue;
                var isAsync = requestType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IAsyncRequest<>));

                if (requestType.BaseType != typeof(object) && requestType.BaseType == typeof(FileRequest))
                {
                    var response = context.Response;
                    response.ContentType = "application/octet-stream";
                    response.StatusCode = 200;
                    
                    ((FileRequest)deserializedRequest).SetStream(response.Body);

                    if (isAsync)
                    {
                        await _mediator.SendAsync(deserializedRequest);
                    }
                    else
                    {
                        _mediator.Send(deserializedRequest);
                    }

                    response.Body.Close();
                    return;
                }                

                try
                {
                    if (isAsync)
                    {
                        returnValue = await _mediator.SendAsync(deserializedRequest);
                    }
                    else
                    {
                        returnValue = _mediator.Send(deserializedRequest);
                    }

                    await context.Response.WriteAsync(JsonConvert.SerializeObject(returnValue));
                }
                catch(Exception exc)
                {
                    context.Response.StatusCode = 500;
                    var task = context.Response.WriteAsync(exc.GetBaseException().Message);
                    Task.WaitAll(task);
                }                
            });
        }
    }
}
