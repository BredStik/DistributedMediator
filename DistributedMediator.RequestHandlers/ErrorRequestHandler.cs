using DistributedMediator.Requests;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedMediator.RequestHandlers
{
    public class ErrorRequestHandler: IRequestHandler<ErrorRequest, string>
    {
        public string Handle(ErrorRequest message)
        {
            throw new NotImplementedException();
        }
    }
}
