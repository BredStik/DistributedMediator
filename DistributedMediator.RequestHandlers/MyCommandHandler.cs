using DistributedMediator.Requests;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedMediator.RequestHandlers
{
    public class MyCommandHandler: RequestHandler<MyCommand>//IRequestHandler<MyCommand, Unit>
    {
        protected override void HandleCore(MyCommand message)
        {
            
        }
    }
}
