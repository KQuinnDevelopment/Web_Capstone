using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using WBSAlpha.Models;
/*
Modified By:    Quinn Helm
Date:           01-12-2021
*/
namespace WBSAlpha.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Default home view.
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// About the Company view.
        /// </summary>
        public IActionResult About()
        {
            return View();
        }

        /// <summary>
        /// Privacy policy goes here should it be updated.
        /// </summary>
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}