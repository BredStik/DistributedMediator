using DistributedMediator.Requests;
using MediatR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace DistributedMediator.WebMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMediator _mediator;

        public HomeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<ActionResult> Index()
        {
            var result = await _mediator.SendAsync(new MyRequest { Id = 1 });

            return View("Index", model:result);
        }

        public ActionResult File()
        {
            return new FileGeneratingResult("test.txt", "text/plain", s => _mediator.Send(new SampleFileRequest(s)));
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }
        
        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}