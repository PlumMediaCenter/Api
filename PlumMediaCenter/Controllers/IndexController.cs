using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PlumMediaCenter.Attributues;
using Newtonsoft.Json.Linq;
using PlumMediaCenter.Graphql;

namespace PlumMediaCenter.Controllers
{
    [ExceptionHandlerFilter()]
    public class IndexController : Controller
    {
        public IndexController() { }

        public IActionResult Index()
        {
            return File("~/index.html", "text/html");
        }
    }
}
