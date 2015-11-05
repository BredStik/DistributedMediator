using Castle.MicroKernel.Registration;
using Castle.Windsor;
using CommonServiceLocator.WindsorAdapter.Unofficial;
using DistributedMediator.RequestHandlers;
using MediatR;
using Microsoft.Owin.Hosting;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Owin;
using DistributedMediator.BackEndService.Owin.Host.Properties;
using DistributedMediator.Requests;

namespace DistributedMediator.BackEndService.Owin.Host
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = new WindsorContainer();

            container.Register(Classes.FromAssemblyContaining<IMediator>().Pick().WithServiceAllInterfaces());
            container.Register(Classes.FromAssemblyContaining<MyRequestHandler>().BasedOn(typeof(MediatR.IRequestHandler<,>)).WithServiceAllInterfaces().LifestyleScoped());
            container.Register(Classes.FromAssemblyContaining<MyRequestHandler>().BasedOn(typeof(MediatR.IAsyncRequestHandler<,>)).WithServiceAllInterfaces().LifestyleScoped());

            container.Register(Component.For<RequestTypeProvider>().LifestyleSingleton());

            var serviceLocator = new WindsorServiceLocator(container);
            var serviceLocatorProvider = new ServiceLocatorProvider(() => serviceLocator);
            container.Register(Component.For<ServiceLocatorProvider>().Instance(serviceLocatorProvider));

            using (WebApp.Start(Settings.Default.Host, appBuilder => {
                appBuilder.Use<DependencyResolverScopeMiddleware>(container);
                new Startup(container.Resolve<IMediator>(), container.Resolve<RequestTypeProvider>()).Configuration(appBuilder); 
            }))
            {
                Console.WriteLine("OWIN host currently listening at {0}", Settings.Default.Host);
                Console.ReadLine();
            }
        }
    }
}
