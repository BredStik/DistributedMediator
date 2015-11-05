using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace DistributedMediator.Requests
{
    public class RequestTypeProvider
    {
        private readonly ICollection<Type> _requestTypes;

        public RequestTypeProvider()
        {
            _requestTypes = this.GetType().Assembly.GetExportedTypes()
                .Where(x => x.GetInterfaces().Any(i => i.IsGenericType && (typeof(IAsyncRequest<>).IsAssignableFrom(i.GetGenericTypeDefinition()) || typeof(IRequest<>).IsAssignableFrom(i.GetGenericTypeDefinition())))).ToList();
        }

        public ICollection<Type> RequestTypes
        {
            get { return _requestTypes; }
        }

        public Type FindByFullName(string fullName)
        {
            return _requestTypes.FirstOrDefault(x => x.FullName.Equals(fullName, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}