using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PlumMediaCenter.Attributues;
using PlumMediaCenter.Business;

namespace PlumMediaCenter.Controllers
{
    public class BaseController : Controller
    {
        public Manager Manager
        {
            get
            {
                if (_Manager == null)
                {
                    _Manager = new Manager();
                }
                return _Manager;
            }
        }
        private Manager _Manager;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_Manager != null)
                {
                    _Manager.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
