using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedMediator.Requests
{
    /// <summary>
    /// Represents a request
    /// It will return a string
    /// </summary>
    public class MyRequest: IRequest<string>
    {
        public int Id { get; set; }
    }
}
