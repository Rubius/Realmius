using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Realmius_mancheck_Web.DAL;
using Realmius_mancheck_Web.Models;

namespace Realmius_mancheck_Web.Controllers
{
    public class ChatMessageRealmsController : Controller
    {
        private RealmiusServerContext db = new RealmiusServerContext();

        // GET: ChatMessageRealms
        public ActionResult Index()
        {
            return View(db.ChatMessages.ToList());
        }

        // GET: ChatMessageRealms/Details/5
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ChatMessageRealm chatMessageRealm = db.ChatMessages.Find(id);
            if (chatMessageRealm == null)
            {
                return HttpNotFound();
            }
            return View(chatMessageRealm);
        }

        // GET: ChatMessageRealms/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ChatMessageRealms/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Text,AuthorName,CreatingDateTime")] ChatMessageRealm chatMessageRealm)
        {
            if (ModelState.IsValid)
            {
                db.ChatMessages.Add(chatMessageRealm);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(chatMessageRealm);
        }

        // GET: ChatMessageRealms/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ChatMessageRealm chatMessageRealm = db.ChatMessages.Find(id);
            if (chatMessageRealm == null)
            {
                return HttpNotFound();
            }
            return View(chatMessageRealm);
        }

        // POST: ChatMessageRealms/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Text,AuthorName,CreatingDateTime")] ChatMessageRealm chatMessageRealm)
        {
            if (ModelState.IsValid)
            {
                db.Entry(chatMessageRealm).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(chatMessageRealm);
        }

        // GET: ChatMessageRealms/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ChatMessageRealm chatMessageRealm = db.ChatMessages.Find(id);
            if (chatMessageRealm == null)
            {
                return HttpNotFound();
            }
            return View(chatMessageRealm);
        }

        // POST: ChatMessageRealms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            ChatMessageRealm chatMessageRealm = db.ChatMessages.Find(id);
            db.ChatMessages.Remove(chatMessageRealm);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
