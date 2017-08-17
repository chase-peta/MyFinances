using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using MyFinances.Models;
using Microsoft.AspNet.Identity;
using MyFinances;
using Microsoft.AspNet.Identity.Owin;

namespace Finances.Controllers
{
    [Authorize]
    public class BillsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private ApplicationUserManager userManager;
        private ApplicationUser user;

        private void LoadUser ()
        {
            userManager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            user = userManager.FindById(User.Identity.GetUserId());
        }

        public ActionResult Index (bool showInactive = false)
        {
            ViewBag.ShowInactive = showInactive;
            LoadUser();
            if (showInactive)
            {
                return View(db.Bills.Where(x => x.UserId == user.Id).Include(x => x.BillPayments).OrderBy(x => x.IsActive).ThenBy(x => x.DueDate).ToList().Populate());
            }
            else
            {
                return View(db.Bills.Where(x => x.UserId == user.Id && x.IsActive).Include(x => x.BillPayments).OrderBy(x => x.DueDate).ToList().Populate());
            }
        }

        public ActionResult Details (int? id)
        {
            ViewBag.Action = "Details";
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LoadUser();
            Bill bill = db.Bills.Find(id).Populate();
            if (bill == null || bill.UserId != user.Id)
            {
                return HttpNotFound();
            }
            return View(bill);
        }

        public ActionResult Create ()
        {
            ViewBag.Action = "Create";
            return View("Details");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create ([Bind(Include = "Name,DueDate,Amount,Payee,IsDueDateStaysSame,IsAmountStaysSame,IsShared,PaymentFrequency")] Bill bill)
        {
            ViewBag.Action = "Create";
            if (ModelState.IsValid)
            {
                db.Bills.Add(bill);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View("Details", bill);
        }

        public ActionResult Edit (int? id)
        {
            ViewBag.Action = "Edit";
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LoadUser();
            Bill bill = db.Bills.Find(id).Populate();
            if (bill == null || bill.UserId != user.Id)
            {
                return HttpNotFound();
            }
            return View("Details", bill);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit ([Bind(Include = "ID,Name,DueDate,Amount,Payee,IsDueDateStaysSame,IsAmountStaysSame,IsShared,PaymentFrequency")] Bill subBill)
        {
            ViewBag.Action = "Edit";
            LoadUser();
            Bill bill = db.Bills.Find(subBill.ID);
            if (bill == null || bill.UserId != user.Id)
            {
                return HttpNotFound();
            }
            bill = bill.MapSubmit(subBill).Populate();
            if (ModelState.IsValid)
            {
                db.Entry(bill).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", new { id = bill.ID });
            }
            return View("Details", bill);
        }

        public ActionResult Deactivate(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LoadUser();
            Bill bill = db.Bills.Find(id);
            if (bill == null || bill.UserId != user.Id)
            {
                return HttpNotFound();
            }
            bill.IsActive = false;
            db.Entry(bill).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Activate (int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LoadUser();
            Bill bill = db.Bills.Find(id);
            if (bill == null || bill.UserId != user.Id)
            {
                return HttpNotFound();
            }
            bill.IsActive = true;
            db.Entry(bill).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Details", new { id = bill.ID });
        }
        
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LoadUser();
            Bill bill = db.Bills.Find(id).Populate();
            if (bill == null || bill.UserId != user.Id)
            {
                return HttpNotFound();
            }
            if (bill.BillPayments.Any())
            {
                bill.BillPayments.ToList().ForEach(x => db.BillPayments.Remove(x));
            }
            db.Bills.Remove(bill);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult AddPayment (int? id)
        {
            ViewBag.Action = "Add Payment";
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LoadUser();
            Bill bill = db.Bills.Find(id).Populate();
            if (bill == null || bill.UserId != user.Id)
            {
                return HttpNotFound();
            }

            BillPayment payment = new BillPayment();
            payment.Amount = bill.Amount;
            payment.DatePaid = bill.DueDate;
            payment.Payee = bill.Payee;
            payment.Bill = bill;

            return View("Payment", payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddPayment (int? id, [Bind(Include = "Amount,DatePaid,Payee")] BillPayment billPayment)
        {
            ViewBag.Action = "Add Payment";
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LoadUser();
            billPayment.Bill = db.Bills.Find(id).Populate();
            if (billPayment == null || billPayment.Bill.UserId != user.Id)
            {
                return HttpNotFound();
            }
            if (ModelState.IsValid)
            {
                db.BillPayments.Add(billPayment);
                db.Entry(saveBillForNextPayment(billPayment)).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", new { id = billPayment.Bill.ID });
            }
            return View("Payment", billPayment);
        }

        public ActionResult EditPayment (int? id)
        {
            ViewBag.Action = "Edit Payment";
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LoadUser();
            BillPayment billPayment = db.BillPayments.Find(id);
            if (billPayment == null || billPayment.UserId != user.Id)
            {
                return HttpNotFound();
            }
            billPayment.Bill = billPayment.Bill.Populate();
            return View("Payment", billPayment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPayment (int id, [Bind(Include = "Amount,DatePaid,Payee")] BillPayment billPayment)
        {
            ViewBag.Action = "Edit Payment";
            LoadUser();
            BillPayment bp = db.BillPayments.Find(id);
            if (bp == null || bp.UserId != user.Id)
            {
                return HttpNotFound();
            }
            if (ModelState.IsValid)
            {
                bp.Amount = billPayment.Amount;
                bp.DatePaid = billPayment.DatePaid;
                bp.Payee = billPayment.Payee;
                db.Entry(bp).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", new { id = bp.Bill.ID });
            }
            return View("Payment", billPayment);
        }

        public ActionResult DeletePayment (int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LoadUser();
            BillPayment billPayment = db.BillPayments.Find(id);
            if (billPayment == null || billPayment.UserId != user.Id)
            {
                return HttpNotFound();
            }
            Bill bill = billPayment.Bill;
            db.BillPayments.Remove(billPayment);
            db.SaveChanges();
            return RedirectToAction("Details", new { id = bill.ID });
        }

        public ActionResult Paid (int? id, string r = "Details")
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LoadUser();
            Bill bill = db.Bills.Find(id);
            if (bill == null || bill.UserId != user.Id)
            {
                return HttpNotFound();
            }
            BillPayment billPayment = new BillPayment();
            billPayment.Bill = bill;

            billPayment.Amount = bill.Amount;
            billPayment.DatePaid = bill.DueDate;
            billPayment.Payee = bill.Payee;

            db.BillPayments.Add(billPayment);
            db.Entry(saveBillForNextPayment(billPayment)).State = EntityState.Modified;
            db.SaveChanges();
            if (r == "Dashboard")
            {
                return RedirectToAction("Index", "Dashboard");
            }
            else
            {
                return RedirectToAction(r, new { id = bill.ID });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private Bill saveBillForNextPayment(BillPayment billPayment)
        {
            Bill bill = billPayment.Bill;

            Tuple<DateTime, decimal> newDateAmount = bill.GetDateAmount(
                bill.GetNextDate(billPayment.DatePaid, user),
                bill.Amount);

            bill.DueDate = newDateAmount.Item1;
            bill.Amount = newDateAmount.Item2;

            return bill;
        }
    }
}
