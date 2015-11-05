using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedMediator.Requests
{
    /// <summary>
    /// An request throwing an error
    /// </summary>
    public class ErrorRequest: IRequest<string>
    {
    }
}
