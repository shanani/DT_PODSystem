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
    public class LoginRegisterController : Controller
    {

        public IActionResult LoginV1()
        {
            return View();
        }

        public IActionResult LoginV2()
        {
            return View();
        }

        public IActionResult LoginV3()
        {
            return View();
        }

        public IActionResult RegisterV3()
        {
            return View();
        }
    }
}
