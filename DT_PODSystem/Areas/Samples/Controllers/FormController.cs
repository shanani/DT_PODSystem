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
    public class FormController : Controller
    {

        public IActionResult Elements()
        {
            return View();
        }

        public IActionResult Plugins()
        {
            return View();
        }

        public IActionResult SliderSwitcher()
        {
            return View();
        }

        public IActionResult Validation()
        {
            return View();
        }

        public IActionResult Wizards()
        {
            return View();
        }

        public IActionResult Wysiwyg()
        {
            return View();
        }

        public IActionResult XEditable()
        {
            return View();
        }

        public IActionResult MultipleFileUpload()
        {
            return View();
        }

        public IActionResult Summernote()
        {
            return View();
        }

        public IActionResult Dropzone()
        {
            return View();
        }
    }
}
