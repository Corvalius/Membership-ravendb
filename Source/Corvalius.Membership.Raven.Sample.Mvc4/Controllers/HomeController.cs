using Corvalius.Membership.Raven.Sample.Mvc4.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Corvalius.Membership.Raven.Sample.Mvc4.Controllers
{
    [InitializeSimpleRavenMembershipAttribute]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Message = "Modify this template to jump-start your ASP.NET MVC application.";

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your app description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Administration()
        {            
            ViewBag.Message = "Your administrator page.";

            return View();
        }
    }
}