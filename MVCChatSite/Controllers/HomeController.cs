using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MVCChatSite.Models;

namespace MVCChatSite.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var vm = new HomeVM
            {
                Messages = ChatServer.GetHistory()
            };

            return View(vm);
        }

        public ActionResult About()
        {
            return View();
        }

    }
}
