using DistributedMediator.Requests;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedMediator.RequestHandlers
{
    public class SampleFileRequestHandler : IRequestHandler<SampleFileRequest, Unit>
    {
        public Unit Handle(SampleFileRequest message)
        {
            var outStream = message.GetStream();

            var result = "My text to stream";

            var bytes = Encoding.UTF8.GetBytes(result);

            foreach (var singleByte in bytes)
            {
                outStream.Write(new byte[1] { singleByte }, 0, 1);
                outStream.Flush();
            }

            return default(Unit);
        }
    }
}
