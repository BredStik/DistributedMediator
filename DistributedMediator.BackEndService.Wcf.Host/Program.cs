using Castle.Facilities.WcfIntegration;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using CommonServiceLocator.WindsorAdapter.Unofficial;
using DistributedMediator.BackEndService.Wcf.Host.Contracts;
using DistributedMediator.BackEndService.Wcf.Host.Implementations;
using DistributedMediator.RequestHandlers;
using MediatR;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace DistributedMediator.BackEndService.Wcf.Host
{
    class Program
    {
        static void Main(string[] args)
        {
			var windsorContainer = new WindsorContainer().AddFacility<WcfFacility>();

            windsorContainer.Register(Classes.FromAssemblyContaining<IMediator>().Pick().WithServiceAllInterfaces());

            windsorContainer.Register(Classes.FromAssemblyContaining<MyRequestHandler>().BasedOn(typeof(MediatR.IRequestHandler<,>)).WithServiceAllInterfaces().LifestylePerWcfOperation());
            windsorContainer.Register(Classes.FromAssemblyContaining<MyRequestHandler>().BasedOn(typeof(MediatR.IAsyncRequestHandler<,>)).WithServiceAllInterfaces().LifestylePerWcfOperation());

            //windsorContainer.Kernel.AddHandlersFilter(new ContravariantFilter());

            windsorContainer.Register(Component.For<IRequestHandlerService>().ImplementedBy<RequestHandlerService>().AsWcfService(new DefaultServiceModel(WcfEndpoint.BoundTo(new NetTcpBinding() { TransferMode = TransferMode.Streamed }).At("net.tcp://localhost:9101/requestHandler")))
                );
            var serviceLocator = new WindsorServiceLocator(windsorContainer);
            var serviceLocatorProvider = new ServiceLocatorProvider(() => serviceLocator);
            windsorContainer.Register(Component.For<ServiceLocatorProvider>().Instance(serviceLocatorProvider));

            var hostFactory = new DefaultServiceHostFactory(windsorContainer.Kernel);
            var requestHandlerHost = hostFactory.CreateServiceHost<IRequestHandlerService>();
            
            try
			{
                Console.WriteLine("WCF host currently listening at net.tcp://localhost:9101/requestHandler");
				Console.ReadLine();
			}
			finally
			{
                requestHandlerHost.Close();
			}
        }
    }

    public static class ServiceFactoryExtensions
    {
        public static ServiceHostBase CreateServiceHost<TService>(this DefaultServiceHostFactory factory)
        {
            return factory.CreateServiceHost(typeof(TService).AssemblyQualifiedName, new Uri[0]);
        }
    }
}
