using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyFinances.Models;
using Microsoft.AspNet.Identity;
using MyFinances;
using MyFinances.Controllers;

namespace Finances.Controllers
{
    [Authorize]
    public class BillsController : BaseController
    {
        public ActionResult Index (bool showInactive = false)
        {
            ViewBag.ShowInactive = showInactive;
            return View(GetBills(showInactive));
        }

        public ActionResult Details (int? id)
        {
            ViewBag.Action = "Details";
            Bill bill = GetBill(id);
            if (bill == null)
            {
                return HttpNotFound();
            }
            return View(bill);
        }

        public ActionResult Create ()
        {
            ViewBag.Action = "Create";
            ViewBag.Users = GetAvailableUsers();
            return View("Details", new Bill());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create (string[] payeeId, [Bind(Include = "Name,DueDate,Amount,Payee,IsDueDateStaysSame,IsAmountStaysSame,IsShared,PaymentFrequency")] Bill bill)
        {
            ViewBag.Action = "Create";
            if (ModelState.IsValid)
            {
                bill.User = db.Users.Find(user.Id);
                db.Bills.Add(bill);
                UpdateBillShared(bill, payeeId);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Users = GetAvailableUsers();
            return View("Details", bill);
        }

        public ActionResult Edit (int? id)
        {
            ViewBag.Action = "Edit";
            Bill bill = GetBill(id);
            if (bill == null || bill.User.Id != user.Id)
            {
                return HttpNotFound();
            }
            ViewBag.Users = GetAvailableUsers();
            return View("Details", bill);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit (string[] payeeId, [Bind(Include = "ID,Name,DueDate,Amount,Payee,IsDueDateStaysSame,IsAmountStaysSame,IsShared,PaymentFrequency")] Bill subBill)
        {
            ViewBag.Action = "Edit";
            Bill bill = db.Bills.Find(subBill.ID);
            if (bill == null || bill.User.Id != user.Id)
            {
                return HttpNotFound();
            }
            bill = bill.MapSubmit(subBill).Populate(user);
            if (ModelState.IsValid)
            {
                db.Entry(bill).State = EntityState.Modified;
                UpdateBillShared(bill, payeeId);
                db.SaveChanges();
                return RedirectToAction("Details", new { id = bill.ID });
            }
            ViewBag.Users = GetAvailableUsers();
            return View("Details", bill);
        }

        public ActionResult Deactivate(int? id)
        {
            Bill bill = GetBill(id);
            if (bill == null || bill.User.Id != user.Id)
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
            Bill bill = GetBill(id);
            if (bill == null || bill.User.Id != user.Id)
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
            Bill bill = GetBill(id);
            if (bill == null || bill.User.Id != user.Id)
            {
                return HttpNotFound();
            }
            bill.SharedWith.ToList().ForEach(x => db.SharedBill.Remove(x));
            if (bill.BillPayments.Any())
            {
                bill.BillPayments.ToList().ForEach(x => db.SharedBillPayment.RemoveRange(x.SharedWith));
                db.BillPayments.RemoveRange(bill.BillPayments);
            }
            db.Bills.Remove(bill);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult AddPayment (int? id)
        {
            ViewBag.Action = "Add Payment";
            Bill bill = GetBill(id);
            if (bill == null || bill.User.Id != user.Id)
            {
                return HttpNotFound();
            }

            BillPayment payment = new BillPayment();
            payment.Amount = bill.Amount;
            payment.DatePaid = bill.DueDate;
            payment.Payee = bill.Payee;
            payment.Bill = bill;
            
            ViewBag.Users = GetAvailableUsers(payment.Bill);
            return View("Payment", payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddPayment (int? id, string[] payeeId, [Bind(Include = "Amount,DatePaid,Payee")] BillPayment billPayment)
        {
            ViewBag.Action = "Add Payment";
            billPayment.Bill = GetBill(id);
            if (billPayment == null || billPayment.Bill.User.Id != user.Id)
            {
                return HttpNotFound();
            }
            if (ModelState.IsValid)
            {
                billPayment.User = db.Users.Find(user.Id);
                db.BillPayments.Add(billPayment);
                db.Entry(saveBillForNextPayment(billPayment)).State = EntityState.Modified;
                UpdateBillPaymentShared(billPayment, payeeId);
                db.SaveChanges();
                return RedirectToAction("Details", new { id = billPayment.Bill.ID });
            }
            ViewBag.Users = GetAvailableUsers(billPayment.Bill);
            return View("Payment", billPayment);
        }

        public ActionResult EditPayment (int? id)
        {
            ViewBag.Action = "Edit Payment";
            BillPayment billPayment = GetBillPayment(id);
            if (billPayment == null || billPayment.User.Id != user.Id)
            {
                return HttpNotFound();
            }
            ViewBag.Users = GetAvailableUsers(billPayment.Bill);
            return View("Payment", billPayment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPayment (int id, string[] payeeId, [Bind(Include = "Amount,DatePaid,Payee")] BillPayment billPayment)
        {
            ViewBag.Action = "Edit Payment";
            BillPayment bp = GetBillPayment(id);
            if (bp == null || bp.User.Id != user.Id)
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
                UpdateBillPaymentShared(bp, payeeId);
                db.SaveChanges();
                return RedirectToAction("Details", new { id = bp.Bill.ID });
            }
            return View("Payment", billPayment);
        }

        public ActionResult DeletePayment (int? id)
        {
            BillPayment billPayment = GetBillPayment(id);
            if (billPayment == null)
            {
                return HttpNotFound();
            }
            Bill bill = billPayment.Bill;
            db.SharedBillPayment.RemoveRange(billPayment.SharedWith);
            db.SaveChanges();
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
            Bill bill = GetBill(id);
            if (bill == null || bill.User.Id != user.Id)
            {
                return HttpNotFound();
            }
            BillPayment billPayment = new BillPayment();
            billPayment.Bill = bill;

            billPayment.Amount = bill.Amount;
            billPayment.DatePaid = bill.DueDate;
            billPayment.Payee = bill.Payee;
            billPayment.User = db.Users.Find(user.Id);

            db.BillPayments.Add(billPayment);
            if (billPayment.Bill.IsShared && billPayment.Bill.SharedWith.Count() > 0)
            {
                GetAvailableUsers(bill).ForEach(x => db.SharedBillPayment.Add(new SharedBillPayment(billPayment, x)));
            }
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
