using Castle.MicroKernel.Registration;
using Castle.Windsor;
using CommonServiceLocator.WindsorAdapter.Unofficial;
using DistributedMediator.RequestHandlers;
using DistributedMediator.Requests;
using MediatR;
using Microsoft.Practices.ServiceLocation;
using Nancy.Bootstrappers.Windsor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedMediator.BackEndService.Nancy.Host
{
    public class Bootstrapper : WindsorNancyBootstrapper
    {
        protected override void ConfigureApplicationContainer(IWindsorContainer container)
        {
            container.Register(Classes.FromAssemblyContaining<IMediator>().Pick().WithServiceAllInterfaces());
            container.Register(Classes.FromAssemblyContaining<MyRequestHandler>().BasedOn(typeof(MediatR.IRequestHandler<,>)).Unless(x => x == typeof(SampleFileRequestHandler)).WithServiceAllInterfaces().LifestyleScoped<NancyPerWebRequestScopeAccessor>());
            container.Register(Component.For<IRequestHandler<SampleFileRequest, Unit>>().ImplementedBy<SampleFileRequestHandler>().LifestyleTransient());
            container.Register(Classes.FromAssemblyContaining<MyRequestHandler>().BasedOn(typeof(MediatR.IAsyncRequestHandler<,>)).WithServiceAllInterfaces().LifestyleScoped<NancyPerWebRequestScopeAccessor>());

            container.Register(Component.For<RequestTypeProvider>().LifestyleSingleton());

            var serviceLocator = new WindsorServiceLocator(container);
            var serviceLocatorProvider = new ServiceLocatorProvider(() => serviceLocator);
            container.Register(Component.For<ServiceLocatorProvider>().Instance(serviceLocatorProvider));
        }
    }
}
