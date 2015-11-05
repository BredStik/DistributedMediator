using DistributedMediator.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using System.IO;

namespace DistributedMediator.RequestHandlers
{
    public class ParameterLessRequestHandler: IAsyncRequestHandler<ParameterLessRequest, Point[]>
    {
        public async Task<Point[]> Handle(ParameterLessRequest message)
        {
            var result = await Task.Run(() =>
            {
                var points = new List<Point>();

                for (int i = 0; i < 10; i++)
                {
                    points.Add(new Point{X=i, Y= i});
                }

                return points;
            });

            return result.ToArray();
        }
    }
}
