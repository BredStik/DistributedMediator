using System.IO;
using MediatR;
using System.Runtime.Serialization;

namespace DistributedMediator.Requests
{
    [DataContract]
    public abstract class FileRequest: IRequest<Unit>
    {
        private Stream _stream;

        public Stream GetStream()
        {
            return _stream;
        }

        public void SetStream(Stream stream)
        {
            _stream = stream;
        }

        public FileRequest(Stream stream)
        {
            _stream = stream;
        }

        protected FileRequest() { }
    }
}
