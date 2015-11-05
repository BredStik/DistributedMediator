using DistributedMediator.Requests;
using MediatR;
using Nancy;
using Nancy.ModelBinding;
using Nancy.ModelBinding.DefaultBodyDeserializers;
using Nancy.Responses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedMediator.BackEndService.Nancy.Host
{
    public class RequestHandlerModule: NancyModule
    {
        public RequestHandlerModule(IMediator mediator, RequestTypeProvider requestTypeProvider)
            : base("/RequestHandler/request")
        {
            Post["/{requestType}", true] = async (parameters, ct) =>
            {
                Console.WriteLine("Call to AsyncRequestHandlerModule");
                var requestTypeString = (string)parameters.requestType;

                var requestType = requestTypeProvider.FindByFullName(requestTypeString);

                if (requestType == null)
                {
                    return (int)HttpStatusCode.NotAcceptable;
                }

                dynamic deserializedRequest = this.Bind(requestType);

                var isAsyncRequest = requestType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IAsyncRequest<>));

                if (requestType.BaseType != typeof(object) && requestType.BaseType == typeof(FileRequest))
                {
                    var response = new Response { StatusCode = HttpStatusCode.OK, ContentType = "application/octet-stream", Contents = async s => {
                        ((FileRequest)deserializedRequest).SetStream(s);
                        if (isAsyncRequest)
                        {
                            await mediator.SendAsync(deserializedRequest);
                        }
                        else
                        {
                            mediator.Send(deserializedRequest);
                        }
                    } };

                    return response;
                }



                try
                {
                    if (isAsyncRequest)
                    {
                        return await mediator.SendAsync(deserializedRequest);
                    }

                    return mediator.Send(deserializedRequest);
                }
                catch (Exception exc)
                {
                    //todo:log, etc.
                    var errorResponse = Response.AsText(exc.Message);
                    errorResponse.StatusCode = HttpStatusCode.InternalServerError;

                    return errorResponse;
                }              
            };
        }
    }

    public static class ModuleExtensions
    {
        public static object Bind(this NancyModule module, Type destinationType)
        {
            return new JsonBodyDeserializer().Deserialize(module.Request.Headers.ContentType, module.Request.Body, new BindingContext { DestinationType = destinationType });
        }
    }

    class SlowStreamResponse : Response
    {
        public SlowStreamResponse(Action<Stream> outputStream)
        {
            ContentType = "text/plain";
            Contents = outputStream;
        }
    }
}
