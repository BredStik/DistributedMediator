using DistributedMediator.Requests;
using MediatR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DistributedMediator.BackEndService.Wcf.Host.Contracts
{
    [ServiceContract]   
    public interface IRequestHandlerService
    {
        [OperationContract]
        [ServiceKnownType("GetKnownTypes", typeof(KnownTypesProvider))]
        Task<object> HandleRequest(object request);

        [OperationContract]
        [ServiceKnownType("GetKnownTypes", typeof(KnownTypesProvider))]
        Task<Stream> HandleFileRequest(FileRequest request);
    }

    internal class KnownTypesProvider
    {
        private static Type[] _knownTypes = null;

        public static IEnumerable<Type> GetKnownTypes(ICustomAttributeProvider provider)
        {
            if (_knownTypes != null)
            {
                return _knownTypes;
            }

            var contractAssembly = typeof(MyRequest).Assembly;

            var requestTypes = (
                from type in contractAssembly.GetExportedTypes()
                where TypeIsRequestType(type)
                select type)
                .ToList();

            var resultTypes =
                from requestType in requestTypes
                select GetRequestResultType(requestType);

            var genericResultTypes = resultTypes.Where(x => x.IsGenericType).SelectMany(x => x.GetGenericArguments());

            _knownTypes = requestTypes.Union(resultTypes).Union(genericResultTypes).ToArray();

            return _knownTypes;
        }

        private static bool TypeIsRequestType(Type type)
        {
            return GetRequestInterface(type) != null;
        }

        private static Type GetRequestResultType(Type requestType)
        {
            return GetRequestInterface(requestType).GetGenericArguments()[0];
        }

        private static Type GetRequestInterface(Type type)
        {
            return (
                from interfaceType in type.GetInterfaces()
                where interfaceType.IsGenericType
                where typeof(IRequest<>).IsAssignableFrom(interfaceType.GetGenericTypeDefinition()) || typeof(IAsyncRequest<>).IsAssignableFrom(interfaceType.GetGenericTypeDefinition()) 
                select interfaceType)
                .SingleOrDefault();
        }
    }
}
