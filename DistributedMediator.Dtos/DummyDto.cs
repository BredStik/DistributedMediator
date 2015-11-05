using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedMediator.Dtos
{
    public class DummyDto
    {
        /// <summary>
        /// Id on which to perform the action
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// New name of the item
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// New birthdate of the item
        /// </summary>
        public DateTime BirthDate{ get; set; }
    }
}
