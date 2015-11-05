using DistributedMediator.BackEndService.Wcf.Host.Contracts;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Castle.Facilities.WcfIntegration;
using DistributedMediator.Requests;

namespace DistributedMediator.WebMVC
{
    public class GenericWcfRemoteHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        private readonly IRequestHandlerService _requestHandlerService;

        public GenericWcfRemoteHandler(IRequestHandlerService requestHandlerService)
        {
            _requestHandlerService = requestHandlerService;
        }

        public virtual TResponse Handle(TRequest request)
        {
            try
            {
                if (typeof(TRequest).BaseType != typeof(object) && typeof(TRequest).BaseType == typeof(FileRequest))
                {
                    var fileRequest = request as FileRequest;

                    var uploadStream = fileRequest.GetStream();
                    var buffer = new byte[131072];
                    int chunk;

                    using(var responseStream = _requestHandlerService.HandleFileRequest(fileRequest).Result)
                    {
                        while ((chunk = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            uploadStream.Write(buffer, 0, chunk);
                            uploadStream.Flush();
                        }
                    }

                    return default(TResponse);
                }

                return (TResponse)_requestHandlerService.HandleRequest(request).Result;
            }
            catch (Exception exc)
            {
                //todo: log, etc.
                throw;
            }            
        }
    }

    public class AsyncGenericWcfRemoteHandler<TRequest, TResponse> : IAsyncRequestHandler<TRequest, TResponse> where TRequest : IAsyncRequest<TResponse>
    {
        private readonly IRequestHandlerService _requestHandlerService;

        public AsyncGenericWcfRemoteHandler(IRequestHandlerService requestHandlerService)
        {
            _requestHandlerService = requestHandlerService;
        }

        public virtual async Task<TResponse> Handle(TRequest request)
        {
            object unwrappedRequest = Unwrap(request);

            try
            {
                if (typeof(TRequest).BaseType != typeof(object) && typeof(TRequest).BaseType == typeof(FileRequest))
                {
                    var fileRequest = request as FileRequest;

                    var uploadStream = fileRequest.GetStream();
                    var buffer = new byte[131072];
                    int chunk;

                    using (var responseStream = await _requestHandlerService.HandleFileRequest(fileRequest))
                    {
                        while ((chunk = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            uploadStream.Write(buffer, 0, chunk);
                            uploadStream.Flush();
                        }
                    }

                    return default(TResponse);
                }

                return (TResponse)await _requestHandlerService.HandleRequest(unwrappedRequest);// _requestHandlerService.CallAsync(x => x.HandleRequest(request));//
            }
            catch (Exception exc)
            {
                //todo: log, etc.
                throw;
            }
            
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

    public static class Extensions
    {
        public static Task CallAsync<TService>(this TService service, Action<TService> call)
        {
            return Task.Factory.FromAsync(service.BeginWcfCall(call), ar => service.EndWcfCall(ar));
        }

        public static Task<TResult> CallAsync<TService, TResult>(this TService service, Func<TService, TResult> call)
        {
            return Task.Factory.FromAsync(service.BeginWcfCall(call), ar => service.EndWcfCall<TResult>(ar));
        }
    }
}