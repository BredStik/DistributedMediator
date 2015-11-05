using Castle.MicroKernel.Registration;
using Castle.Windsor;
using CommonServiceLocator.WindsorAdapter.Unofficial;
using DistributedMediator.RequestHandlers;
using DistributedMediator.Requests;
using MediatR;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace DistributedMediator.BackEndService.WebAPI
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        IWindsorContainer windsorContainer;

        protected void Application_Start()
        {
            windsorContainer = new WindsorContainer();

            windsorContainer.Register(Classes.FromAssemblyContaining<IMediator>().Pick().WithServiceAllInterfaces());
            windsorContainer.Register(Classes.FromAssemblyContaining<MyRequestHandler>().BasedOn(typeof(MediatR.IRequestHandler<,>)).WithServiceAllInterfaces().LifestylePerWebRequest());
            windsorContainer.Register(Classes.FromAssemblyContaining<MyRequestHandler>().BasedOn(typeof(MediatR.IAsyncRequestHandler<,>)).WithServiceAllInterfaces().LifestylePerWebRequest());

            //windsorContainer.Kernel.AddHandlersFilter(new ContravariantFilter());

            windsorContainer.Register(Component.For<RequestTypeProvider>().LifestyleSingleton());

            var serviceLocator = new WindsorServiceLocator(windsorContainer);
            var serviceLocatorProvider = new ServiceLocatorProvider(() => serviceLocator);
            windsorContainer.Register(Component.For<ServiceLocatorProvider>().Instance(serviceLocatorProvider));

            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(new WebApiConfig(windsorContainer).Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);            
        }
    }
}
