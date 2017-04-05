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

namespace Finances.Controllers
{
    [Authorize]
    public class BillsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        
        public ActionResult Index(bool showInactive = false)
        {
            ViewBag.ShowInactive = showInactive;
            string userId = User.Identity.GetUserId();
            if (showInactive)
            {
                return View(db.Bills.Where(x => x.UserId == userId).Include(x => x.BillPayments).OrderBy(x => x.IsActive).ThenBy(x => x.DueDate).ToList().Populate());
            }
            else
            {
                return View(db.Bills.Where(x => x.UserId == userId && x.IsActive).Include(x => x.BillPayments).OrderBy(x => x.DueDate).ToList().Populate());
            }
        }
        
        public ActionResult Details(int? id)
        {
            ViewBag.Action = "Details";
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Bill bill = db.Bills.Find(id).Populate();
            if (bill == null)
            {
                return HttpNotFound();
            }
            return View(bill);
        }
        
        public ActionResult Create()
        {
            ViewBag.Action = "Create";
            return View("Details");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Name,DueDate,Amount,Payee,IsDueDateStaysSame,IsAmountStaysSame,IsShared")] Bill bill)
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

        public ActionResult Edit(int? id)
        {
            ViewBag.Action = "Edit";
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Bill bill = db.Bills.Find(id).Populate();
            if (bill == null)
            {
                return HttpNotFound();
            }
            return View("Details", bill);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit ([Bind(Include = "ID,Name,DueDate,Amount,Payee,IsDueDateStaysSame,IsAmountStaysSame,IsShared")] Bill subBill)
        {
            ViewBag.Action = "Edit";
            Bill bill = db.Bills.Find(subBill.ID).MapSubmit(subBill).Populate();
            if (ModelState.IsValid)
            {
                db.Entry(bill).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", new { id = bill.ID });
            }
            return View("Details", bill);
        }

        public ActionResult Deactivate(int id)
        {
            Bill bill = db.Bills.Find(id);
            bill.IsActive = false;
            db.Entry(bill).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Activate (int id)
        {
            Bill bill = db.Bills.Find(id);
            bill.IsActive = true;
            db.Entry(bill).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Details", new { id = bill.ID });
        }
        
        public ActionResult Delete(int id)
        {
            Bill bill = db.Bills.Find(id).Populate();
            if (bill.BillPayments.Any())
            {
                bill.BillPayments.ToList().ForEach(x => db.BillPayments.Remove(x));
            }
            db.Bills.Remove(bill);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult AddPayment (int id)
        {
            ViewBag.Action = "Add Payment";
            Bill bill = db.Bills.Find(id).Populate();

            BillPayment payment = new BillPayment();
            payment.Amount = bill.Amount;
            payment.DatePaid = bill.DueDate;
            payment.Payee = bill.Payee;
            payment.Bill = bill;

            return View("Payment", payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddPayment (int id, [Bind(Include = "Amount,DatePaid,Payee")] BillPayment billPayment)
        {
            ViewBag.Action = "Add Payment";
            billPayment.Bill = db.Bills.Find(id).Populate();
            if (ModelState.IsValid)
            {
                db.BillPayments.Add(billPayment);
                db.Entry(saveBillForNextMonth(billPayment)).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", new { id = billPayment.Bill.ID });
            }
            return View("Payment", billPayment);
        }

        public ActionResult EditPayment (int id)
        {
            ViewBag.Action = "Edit Payment";
            BillPayment payment = db.BillPayments.Find(id);
            payment.Bill = payment.Bill.Populate();
            return View("Payment", payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPayment (int id, [Bind(Include = "Amount,DatePaid,Payee")] BillPayment billPayment)
        {
            ViewBag.Action = "Edit Payment";
            BillPayment bp = db.BillPayments.Find(id);
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

        public ActionResult DeletePayment (int id)
        {
            BillPayment bp = db.BillPayments.Find(id);
            Bill bill = bp.Bill;
            db.BillPayments.Remove(bp);
            db.SaveChanges();
            return RedirectToAction("Details", new { id = bill.ID });
        }

        public ActionResult Paid (int id, string r = "Details")
        {
            Bill bill = db.Bills.Find(id);
            BillPayment billPayment = new BillPayment();
            billPayment.Bill = bill;

            billPayment.Amount = bill.Amount;
            billPayment.DatePaid = bill.DueDate;
            billPayment.Payee = bill.Payee;

            db.BillPayments.Add(billPayment);
            db.Entry(saveBillForNextMonth(billPayment)).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction(r, new { id = bill.ID });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private Bill saveBillForNextMonth(BillPayment billPayment)
        {
            Bill bill = billPayment.Bill;
            DateTime nextDate = billPayment.DatePaid.AddMonths(1);
            if (nextDate > bill.LastPaymentDate)
            {
                BillAverage avg = bill.BillAverages.Where(x => x.Date.Month == nextDate.Month).FirstOrDefault();
                if (bill.IsDueDateStaysSame || avg == null)
                {
                    bill.DueDate = nextDate;
                }
                else
                {
                    bill.DueDate = avg.Date;
                }
                if (!bill.IsAmountStaysSame && avg != null)
                {
                    bill.Amount = avg.Average;
                }
            }
            return bill;
        }
    }
}
