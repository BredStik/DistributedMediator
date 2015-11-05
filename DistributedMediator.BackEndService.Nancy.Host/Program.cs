using Nancy.Hosting.Self;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedMediator.BackEndService.Nancy.Host
{
    class Program
    {
        static void Main(string[] args)
        {
            var uri = "http://localhost:12345";

            using (var host = new NancyHost(new HostConfiguration{UrlReservations = new UrlReservations{CreateAutomatically = true}}, new Uri(uri)))
            {
                host.Start();
                Console.WriteLine("Nancy host currently listening at {0}", uri);
                Console.ReadLine();
            }
        }
    }
}
