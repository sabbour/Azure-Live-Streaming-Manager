using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";

            var channelsController = new ALSManager.Web.Controllers.API.ChannelsController();
            var channels = channelsController.Get();

            ViewBag.Channels = channels;

            return View();
        }
    }
}
