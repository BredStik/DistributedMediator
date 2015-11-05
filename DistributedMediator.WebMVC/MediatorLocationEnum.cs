using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DistributedMediator.WebMVC
{
    public enum MediatorLocation
    {
        Local, Remote
    }

    public enum MediatorProtocol
    {
        Http, WCF
    }
}