using DistributedMediator.BackEndService.Wcf.Host.Contracts;
using DistributedMediator.Requests;
using DistributedMediator.WebMVC.Properties;
using MediatR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace DistributedMediator.WebMVC
{
    public class GenericWebApiRemoteHandler<TRequest, TResponse> : GenericWebApiRemoteHandlerBase<TRequest, TResponse>,
        IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        public GenericWebApiRemoteHandler(HttpClient httpClient)
            : base(httpClient, Settings.Default.BackEndServiceUri)
        { }

        public virtual TResponse Handle(TRequest request)
        {
            var httpRequest = PrepareRequest(request);

            if (typeof(TRequest).BaseType != typeof(object) && typeof(TRequest).BaseType == typeof(FileRequest))
            {
                var fileRequest = request as FileRequest;

                var uploadStream = fileRequest.GetStream();
                var buffer = new byte[131072];
                int chunk;

                var sendRequestTask = _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
                sendRequestTask.Wait();

                using (HttpResponseMessage serviceResponse = sendRequestTask.Result)
                {
                    var readStreamTask = serviceResponse.Content.ReadAsStreamAsync();
                    readStreamTask.Wait();
                    using (var responseStream = readStreamTask.Result)
                    {
                        while ((chunk = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            uploadStream.Write(buffer, 0, chunk);
                            uploadStream.Flush();
                        }
                    }
                }

                return default(TResponse);
            }

            var response = _httpClient.SendAsync(httpRequest).ConfigureAwait(false).GetAwaiter().GetResult();

            HandleUnsuccessfulStatusCodes(response);

            var stringContent = EnsureQuotedString(response.Content.ReadAsStringAsync().Result);

            return JsonConvert.DeserializeObject<TResponse>("");//stringContent);
        }

        protected override object Unwrap(TRequest request)
        {
            return request;
        }
    }

    public class AsyncGenericWebApiRemoteHandler<TRequest, TResponse> : GenericWebApiRemoteHandlerBase<TRequest, TResponse>,
        IAsyncRequestHandler<TRequest, TResponse> where TRequest : IAsyncRequest<TResponse>
    {
        public AsyncGenericWebApiRemoteHandler(HttpClient httpClient)
            : base(httpClient, Settings.Default.BackEndServiceUri)
        {}

        public virtual async Task<TResponse> Handle(TRequest request)
        {
            var httpRequest = PrepareRequest(request);

            if (typeof(TRequest).BaseType != typeof(object) && typeof(TRequest).BaseType == typeof(FileRequest))
            {
                var fileRequest = request as FileRequest;

                var uploadStream = fileRequest.GetStream();
                var buffer = new byte[131072];
                int chunk;

                using (HttpResponseMessage serviceResponse = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead))
                {
                    using (var responseStream = await serviceResponse.Content.ReadAsStreamAsync())
                    {
                        while ((chunk = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            uploadStream.Write(buffer, 0, chunk);
                            uploadStream.Flush();
                        }
                    }
                }

                return default(TResponse);
            }

            var response = await _httpClient.SendAsync(httpRequest);

            HandleUnsuccessfulStatusCodes(response);

            var stringContent = EnsureQuotedString(await response.Content.ReadAsStringAsync());

            return JsonConvert.DeserializeObject<TResponse>(stringContent);
        }
    }

    public abstract class GenericWebApiRemoteHandlerBase<TRequest, TResponse>
    {
        private readonly string _backEndServiceUri;
        protected readonly HttpClient _httpClient;

        public GenericWebApiRemoteHandlerBase(HttpClient httpClient, string backEndServiceUri)
        {
            _httpClient = httpClient;
            _backEndServiceUri = backEndServiceUri;
        }

        protected virtual string EnsureQuotedString(string stringContent)
        {
            if (typeof(TResponse) == typeof(string) && !stringContent.StartsWith("\""))
            {
                //enclose in double quotes
                stringContent = string.Format("\"{0}\"", stringContent);
            }

            return stringContent;
        }

        protected virtual void HandleUnsuccessfulStatusCodes(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                //do whatever is needed if an error occured
                var errorMessage = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                throw new Exception(errorMessage);
            }
        }

        protected virtual HttpRequestMessage PrepareRequest(TRequest request)
        {
            var unwrappedRequest = Unwrap(request);

            Type requestType = unwrappedRequest.GetType();

            var postBody = JsonConvert.SerializeObject(unwrappedRequest);

            var uri = string.Format("{0}/{1}/", _backEndServiceUri, requestType.FullName);

            return new HttpRequestMessage(HttpMethod.Post, uri) { Content = new StringContent(postBody, Encoding.UTF8, "application/json") };
        }

        protected virtual object Unwrap(TRequest request)
        {
            var requestType = request.GetType();

            //unwrap AsyncWrappedRequest if necessary
            if (requestType == typeof(AsyncWrappedRequest) ||
                (requestType.IsGenericType && requestType.GetGenericTypeDefinition() == typeof(AsyncWrappedRequest<>)))
            {
                return requestType.GetProperty("InnerRequest").GetValue(request);
            }

            return request;
        }
    }

    public static class HttpWebRequestExtensions
    {
        public static HttpWebResponse SafeGetResponse(this WebRequest request)
        {
            try
            {
                return (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                return (HttpWebResponse)e.Response;
            }
        }
    }

    public static class MediatrExtensions
    {
        /// <summary>
        /// Allows a non-async request to be handled by an async request handler for remote processing (when enabled) 
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="mediator"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static async Task<TResponse> SendAsync<TResponse>(this IMediator mediator, IRequest<TResponse> request)
        {
            if (Settings.Default.MediatorLocation == MediatorLocation.Remote.ToString())
            {
                return await mediator.SendAsync(new AsyncWrappedRequest<TResponse>(request));
            }

            return mediator.Send(request);
        }

        /// <summary>
        /// Allows a non-async request to be handled by an async request handler for remote processing (when enabled) 
        /// </summary>
        /// <param name="mediator"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static async Task SendAsync(this IMediator mediator, IRequest request)
        {
            if (Settings.Default.MediatorLocation == MediatorLocation.Remote.ToString())
            {
                await mediator.SendAsync(new AsyncWrappedRequest(request));
                return;
            }

            mediator.Send(request);
        }
    }

    /// <summary>
    /// Allows IRequest<> to be handled by an async handler when using distributed mediator (remote async call)
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    public class AsyncWrappedRequest<TResponse>: IAsyncRequest<TResponse>
    {
        public AsyncWrappedRequest(IRequest<TResponse> innerRequest)
        {
            InnerRequest = innerRequest;
        }

        public IRequest<TResponse> InnerRequest { get; set; }
    }

    //
    /// <summary>
    /// Allows IRequest to be handled by an async handler when using distributed mediator (remote async call)
    /// </summary>
    public class AsyncWrappedRequest: IAsyncRequest
    {
        public AsyncWrappedRequest(IRequest innerRequest)
        {
            InnerRequest = innerRequest;
        }

        public IRequest InnerRequest { get; set; }
    }
}
