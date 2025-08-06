using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DT_PODSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Areas.Samples.Controllers
{
    [Area("Samples")]
    public class ExtraController : Controller
    {

        public IActionResult Timeline()
        {
            return View();
        }

        public IActionResult ComingSoon()
        {
            return View();
        }

        public IActionResult SearchResult()
        {
            return View();
        }

        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult Invoice()
        {
            return View();
        }

        public IActionResult ErrorPage()
        {
            return View();
        }

        public IActionResult ScrumBoard()
        {
            return View();
        }

        public IActionResult CookieAcceptanceBanner()
        {
            return View();
        }

        public IActionResult Orders()
        {
            return View();
        }

        public IActionResult OrderDetails()
        {
            return View();
        }

        public IActionResult Products()
        {
            return View();
        }

        public IActionResult ProductDetails()
        {
            return View();
        }

        public IActionResult FileManager()
        {
            return View();
        }

        public IActionResult PricingPage()
        {
            return View();
        }

        public IActionResult MessengerPage()
        {
            return View();
        }

        public IActionResult DataManagement()
        {
            return View();
        }

        public IActionResult SettingsPage()
        {
            return View();
        }
    }
}
