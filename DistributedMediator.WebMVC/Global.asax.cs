using Castle.Facilities.WcfIntegration;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using CommonServiceLocator.WindsorAdapter.Unofficial;
using DistributedMediator.BackEndService.Wcf.Host.Contracts;
using DistributedMediator.RequestHandlers;
using DistributedMediator.WebMVC.Properties;
using MediatR;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace DistributedMediator.WebMVC
{
    public class MvcApplication : System.Web.HttpApplication
    {
        IWindsorContainer _container;

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            ConfigureContainer();
            ControllerBuilder.Current.SetControllerFactory(new CustomControllerFactory(_container.Kernel));
        }

        private void ConfigureContainer()
        {
            _container = new WindsorContainer();
            _container.Register(Classes.FromAssemblyContaining<IMediator>().Pick().WithServiceAllInterfaces());            

            _container.Register(Component.For<HttpClient>().UsingFactoryMethod(() => {
                var httpClient = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true });
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                return httpClient;
            }).LifestyleSingleton());

            var mediatorLocation = (MediatorLocation)Enum.Parse(typeof(MediatorLocation), Settings.Default.MediatorLocation);
            var mediatorProtocol = (MediatorProtocol)Enum.Parse(typeof(MediatorProtocol), Settings.Default.MediatorProtocol);

            if (mediatorLocation == MediatorLocation.Local)
            {
                //in-proc handlers
                _container.Register(Classes.FromAssemblyContaining<MyRequestHandler>().Pick().WithServiceAllInterfaces());
            }
            else
            {
                switch(mediatorProtocol)
                {
                    case MediatorProtocol.Http:
                        //generic web api remote handler
                        _container.Register(Component.For(typeof(IRequestHandler<,>)).ImplementedBy(typeof(GenericWebApiRemoteHandler<,>)).LifestylePerWebRequest());
                        _container.Register(Component.For(typeof(IAsyncRequestHandler<,>)).ImplementedBy(typeof(AsyncGenericWebApiRemoteHandler<,>)).LifestylePerWebRequest());
                        break;
                    case MediatorProtocol.WCF:
                        _container.Kernel.AddFacility<WcfFacility>();
                        _container.Register(Component.For<IRequestHandlerService>()
                                   .AsWcfClient(new DefaultClientModel
                                   {
                                       Endpoint = WcfEndpoint.BoundTo(new NetTcpBinding() { TransferMode = TransferMode.Streamed })
                                           .At(Settings.Default.BackEndServiceUri)
                                   }).LifestylePerWebRequest());
                        //wcf remote handler
                        _container.Register(Component.For(typeof(IRequestHandler<,>)).ImplementedBy(typeof(GenericWcfRemoteHandler<,>)).LifestylePerWebRequest());
                        _container.Register(Component.For(typeof(IAsyncRequestHandler<,>)).ImplementedBy(typeof(AsyncGenericWcfRemoteHandler<,>)).LifestylePerWebRequest());
                        break;
                }                
            }

            _container.Kernel.AddHandlersFilter(new ContravariantFilter());

            var serviceLocator = new WindsorServiceLocator(_container);
            var serviceLocatorProvider = new ServiceLocatorProvider(() => serviceLocator);
            _container.Register(Component.For<ServiceLocatorProvider>().Instance(serviceLocatorProvider));

            //BootstrapControllers
            _container.Register(Classes.FromThisAssembly().BasedOn<IController>().LifestyleTransient());

        }
    }
}
