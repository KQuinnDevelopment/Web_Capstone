using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
/*
Modified By:    Quinn Helm
Date:           27-11-2021
*/
namespace WBSAlpha.Controllers
{
    [Authorize(Roles = "Moderator,Administrator")]
    public class ModerationController : Controller
    {
        // Get: ModerationController
        public ActionResult Index()
        {
            // force it to default to the metrics view
            return View("Metrics");
        }

        // GET: ModerationController/Metrics
        public ActionResult Metrics()
        {
            return View();
        }

        // GET: ModerationController/Reports
        public ActionResult Reports()
        {
            return View();
        }

        // GET: ModerationController/ChatHistory
        public ActionResult ChatHistory()
        {
            return View();
        }


        // GET: ModerationController/ManageBans
        [Authorize(Roles = "Administrator")]
        public ActionResult ManageBans()
        {
            return View();
        }

        // GET: ModerationController/ManageModerators
        [Authorize(Roles = "Administrator")]
        public ActionResult ManageModerators()
        {
            return View();
        }

        // GET: ModerationController/ManageChatrooms
        [Authorize(Roles = "Administrator")]
        public ActionResult ManageChatrooms()
        {
            return View();
        }

        // GET: ModerationController/ManageBuilds
        [Authorize(Roles = "Administrator")]
        public ActionResult ManageBuilds()
        {
            return View();
        }

        // POST: ModerationController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ModerationController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ModerationController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ModerationController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ModerationController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
