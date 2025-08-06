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
    public class GalleryController : Controller
    {

        public IActionResult GalleryV1()
        {
            return View();
        }

        public IActionResult GalleryV2()
        {
            return View();
        }
    }
}
