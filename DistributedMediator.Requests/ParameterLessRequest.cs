using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedMediator.Requests
{

    /// <summary>
    /// A parameterless request
    /// </summary>
    public class ParameterLessRequest: IAsyncRequest<Point[]>
    {
    }

    public class Point
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}
