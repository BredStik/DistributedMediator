using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedMediator.Requests
{
    /// <summary>
    /// Represents a command
    /// </summary>
    public class MyCommand: IRequest
    {
        /// <summary>
        /// This id on which to perform the command
        /// </summary>
        public int Id { get; set; }
    }
}
