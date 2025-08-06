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
    public class PosController : Controller
    {

        public IActionResult CustomerOrder()
        {
            return View();
        }

        public IActionResult KitchenOrder()
        {
            return View();
        }

        public IActionResult CounterCheckout()
        {
            return View();
        }

        public IActionResult TableBooking()
        {
            return View();
        }

        public IActionResult MenuStock()
        {
            return View();
        }
    }
}
