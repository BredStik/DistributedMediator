using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DistributedMediator.Requests
{
    [DataContract]
    public class SampleFileRequest : FileRequest
    {
        public SampleFileRequest(Stream stream) : base(stream)
        {
        }

        protected SampleFileRequest() : base() { }
    }
}
