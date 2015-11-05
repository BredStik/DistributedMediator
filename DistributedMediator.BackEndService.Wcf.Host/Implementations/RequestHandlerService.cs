using DistributedMediator.BackEndService.Wcf.Host.Contracts;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using DistributedMediator.Requests;
using System.IO;

namespace DistributedMediator.BackEndService.Wcf.Host.Implementations
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class RequestHandlerService: IRequestHandlerService
    {
        private readonly IMediator _mediator;

        public RequestHandlerService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<Stream> HandleFileRequest(FileRequest request)
        {
            Console.WriteLine("Call to HandleRequest");
            var requestType = request.GetType();

            var isValidRequest = requestType.GetInterfaces().Any(x =>
                x.IsGenericType &&
                (typeof(IAsyncRequest<>).IsAssignableFrom(x.GetGenericTypeDefinition())
                || typeof(IRequest<>).IsAssignableFrom(x.GetGenericTypeDefinition()))
                );

            if (!isValidRequest)
            {
                throw new ArgumentException("Unprocessable request");
            }

            //var isAsyncRequest = requestType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IAsyncRequest<>));

            var responseStream = new MemoryStream();
            request.SetStream(responseStream);

            //if (isAsyncRequest)
            //{
            //    await _mediator.SendAsync(request);
            //}

            _mediator.Send(request);
            responseStream.Position = 0;
            return responseStream;
        }

        public async Task<object> HandleRequest(object request) 
        {
            Console.WriteLine("Call to HandleRequest");
            var requestType = request.GetType();

            var isValidRequest = requestType.GetInterfaces().Any(x => 
                x.IsGenericType && 
                (typeof(IAsyncRequest<>).IsAssignableFrom(x.GetGenericTypeDefinition()) 
                || typeof(IRequest<>).IsAssignableFrom(x.GetGenericTypeDefinition()))
                );

            if (!isValidRequest)
            {
                throw new ArgumentException("Unprocessable request");
            }

            var isAsyncRequest = requestType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IAsyncRequest<>));

            if(isAsyncRequest)
            {
                return await _mediator.SendAsync((dynamic)request);
            }

            return _mediator.Send((dynamic)request);
        }
    }
}
