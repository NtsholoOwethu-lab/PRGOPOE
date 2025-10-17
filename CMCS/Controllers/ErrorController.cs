using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using CMCS.Models;

namespace CMCS.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            var errorViewModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };

            switch (statusCode)
            {
                case 404:
                    ViewBag.ErrorMessage = "Sorry, the page you requested could not be found.";
                    break;
                case 500:
                    ViewBag.ErrorMessage = "An internal server error occurred. Please try again later.";
                    break;
                default:
                    ViewBag.ErrorMessage = "An error occurred while processing your request.";
                    break;
            }

            return View("Error", errorViewModel);
        }

        [Route("Error")]
        public IActionResult Error()
        {
            var errorViewModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };

            return View(errorViewModel);
        }
    }
}