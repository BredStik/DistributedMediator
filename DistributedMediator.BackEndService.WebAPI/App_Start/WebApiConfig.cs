using Castle.Windsor;
using DistributedMediator.Requests;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace DistributedMediator.BackEndService.WebAPI
{
    public class WebApiConfig
    {
        private readonly IWindsorContainer _container;

        public WebApiConfig(IWindsorContainer container)
        {
            _container = container;
        }
        public void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            config.Routes.MapHttpRoute("RequestHandler", "requestHandler/request/{requestType}", new { requestType = RouteParameter.Optional }, null, new RequestDelegatingHandler(_container.Resolve<IMediator>(), _container.Resolve<RequestTypeProvider>()));
            //config.Routes.MapHttpRoute("AsyncRequestHandler", "asyncRequestHandler/request/{requestType}", new { requestType = RouteParameter.Optional }, null, new AsyncRequestDelegatingHandler(_container.Resolve<IMediator>()));

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
