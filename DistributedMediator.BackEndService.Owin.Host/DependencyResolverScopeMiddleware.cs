using Castle.MicroKernel;
using Castle.Windsor;
using Castle.MicroKernel.Lifestyle;
using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedMediator.BackEndService.Owin.Host
{
    public class DependencyResolverScopeMiddleware : OwinMiddleware
    {
        private readonly IWindsorContainer _container;

        public DependencyResolverScopeMiddleware(OwinMiddleware next, IWindsorContainer container)
            : base(next)
        {
            _container = container;
        }

        public override async Task Invoke(IOwinContext context)
        {
            using (var scope = _container.BeginScope())
            {
                await Next.Invoke(context);
            }
        }
    }
}
