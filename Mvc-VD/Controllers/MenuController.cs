using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Mvc_VD.Models;

namespace Mvc_VD.Controllers
{
    public class MenuController : Controller
    {
        private Entities db = new Entities();

        //
        // GET: /Menu/

        public ActionResult Index()
        {
            return View(db.menu_info.ToList());
        }

        //
        // GET: /Menu/Details/5

        public ActionResult Details(int id = 0)
        {
            menu_info menu_info = db.menu_info.Find(id);
            if (menu_info == null)
            {
                return HttpNotFound();
            }
            return View(menu_info);
        }

        //
        // GET: /Menu/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /Menu/Create

        [HttpPost]
        public ActionResult Create(menu_info menu_info)
        {
            if (ModelState.IsValid)
            {
                db.menu_info.Add(menu_info);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(menu_info);
        }

        //
        // GET: /Menu/Edit/5

        public ActionResult Edit(int id = 0)
        {
            menu_info menu_info = db.menu_info.Find(id);
            if (menu_info == null)
            {
                return HttpNotFound();
            }
            return View(menu_info);
        }

        //
        // POST: /Menu/Edit/5

        [HttpPost]
        public ActionResult Edit(menu_info menu_info)
        {
            if (ModelState.IsValid)
            {
                db.Entry(menu_info).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(menu_info);
        }

        //
        // GET: /Menu/Delete/5

        public ActionResult Delete(int id = 0)
        {
            menu_info menu_info = db.menu_info.Find(id);
            if (menu_info == null)
            {
                return HttpNotFound();
            }
            return View(menu_info);
        }

        //
        // POST: /Menu/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            menu_info menu_info = db.menu_info.Find(id);
            db.menu_info.Remove(menu_info);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        public PartialViewResult Menu()
        {
            var list = db.menu_info.ToList();
            var listY = list.Where(item => item.use_yn == "Y").ToList();
            return PartialView(listY);
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}