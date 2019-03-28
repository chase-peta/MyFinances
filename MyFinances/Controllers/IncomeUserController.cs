using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MyFinances.Models;

namespace MyFinances.Controllers
{
    [Authorize]
    public class IncomeUserController : BaseController
    {
        public ActionResult Details (int? id)
        {
            ViewBag.Action = "Details";
            IncomeUser incomeUser = GetIncomeUser(id);
            if (incomeUser == null)
            {
                return HttpNotFound();
            }
            return View(incomeUser);
        }

        public ActionResult Create ()
        {
            ViewBag.Action = "Create";
            IncomeUser incomeUser = new IncomeUser()
            {
                PaymentFrequency = PaymentFrequency.Monthly,
                Date = DateTime.Now
            };
            ViewBag.Users = GetAllAvailableUsers();
            return View("Details", incomeUser);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create (string userId, [Bind(Include = "PaymentFrequency,Date")] IncomeUser incomeUser)
        {
            ViewBag.Action = "Create";
            if (ModelState.IsValid)
            {
                incomeUser.User = db.Users.Find(user.Id);
                incomeUser.PayeeUser = db.Users.Find(userId);
                db.IncomeUsers.Add(incomeUser);
                db.SaveChanges();
                return RedirectToAction("Edit", new { id = incomeUser.ID });
            }
            return View("Details", incomeUser);
        }

        public ActionResult Edit (int? id)
        {
            ViewBag.Action = "Edit";
            IncomeUser incomeUser = GetIncomeUser(id);
            if (incomeUser == null)
            {
                return HttpNotFound();
            }
            ViewBag.Bills = db.Bills.Where(x => x.User.Id == user.Id && x.IsActive).OrderBy(x => x.Name).ToList();
            ViewBag.Loans = db.Loans.Where(x => x.User.Id == user.Id && x.IsActive).OrderBy(x => x.Name).ToList();
            ViewBag.SharedPercentage = Enum.GetValues(typeof(SharedPercentage));
            return View("Details", incomeUser);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit (int[] billId, string[] sharedPercentBill, int[] loanId, string[] sharedPercentLoan, [Bind(Include = "ID,PaymentFrequency,Date")] IncomeUser subIncomeUser)
        {
            ViewBag.Action = "Edit";
            IncomeUser incomeUser = db.IncomeUsers.Find(subIncomeUser.ID);
            if (incomeUser == null)
            {
                return HttpNotFound();
            }
            incomeUser = incomeUser.MapSubmit(subIncomeUser);
            if (ModelState.IsValid)
            {
                db.Entry(incomeUser).State = EntityState.Modified;
                UpdateIncomeUserBills(incomeUser, billId, sharedPercentBill);
                UpdateIncomeUserLoans(incomeUser, loanId, sharedPercentLoan);
                db.SaveChanges();
                return RedirectToAction("Details", new { id = incomeUser.ID });
            }
            ViewBag.Bills = db.Bills.Where(x => x.User.Id == user.Id && x.IsActive).OrderBy(x => x.Name).ToList();
            ViewBag.Loans = db.Loans.Where(x => x.User.Id == user.Id && x.IsActive).OrderBy(x => x.Name).ToList();
            ViewBag.SharedPercentage = Enum.GetValues(typeof(SharedPercentage));
            return View("Details", incomeUser);
        }

        public ActionResult Deactivate (int? id)
        {
            IncomeUser incomeUser = GetIncomeUser(id);
            if (incomeUser == null)
            {
                return HttpNotFound();
            }
            incomeUser.IsActive = false;
            db.Entry(incomeUser).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Activate (int? id)
        {
            IncomeUser incomeUser = GetIncomeUser(id);
            if (incomeUser == null)
            {
                return HttpNotFound();
            }
            incomeUser.IsActive = true;
            db.Entry(incomeUser).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Details", new { id = incomeUser.ID });
        }

        public ActionResult Delete (int? id)
        {
            IncomeUser incomeUser = GetIncomeUser(id);
            if (incomeUser == null)
            {
                return HttpNotFound();
            }
            if (incomeUser.IncomeUserPayments.Any())
            {
                db.IncomeUserPayments.RemoveRange(incomeUser.IncomeUserPayments);
            }
            db.IncomeUsers.Remove(incomeUser);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult AddPayment (int? id)
        {
            ViewBag.Action = "Add Payment";
            IncomeUser incomeUser = GetIncomeUser(id);
            if (incomeUser == null)
            {
                return HttpNotFound();
            }
            NotPaidIncomeUserPayemnt np = incomeUser.NotPaidIncomeUserPayments.OrderBy(x => x.Date).FirstOrDefault();
            IncomeUserPayment payment = new IncomeUserPayment()
            {
                Date = np.Date,
                Amount = np.Amount,
                IncomeUser = incomeUser
            };
            return View("Payment", payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddPayment (int? id, [Bind(Include = "Amount,Date,IncomeUser.ID")] IncomeUserPayment incomeUserPayment)
        {
            ViewBag.Action = "Add Payment";
            incomeUserPayment.IncomeUser = GetIncomeUser(id);
            if (incomeUserPayment == null)
            {
                return HttpNotFound();
            }
            if (ModelState.IsValid)
            {
                incomeUserPayment.User = db.Users.Find(user.Id);
                db.IncomeUserPayments.Add(incomeUserPayment);
                db.SaveChanges();
                return RedirectToAction("Details", new { id = incomeUserPayment.IncomeUser.ID });
            }
            return View("Payment", incomeUserPayment);
        }

        public ActionResult EditPayment (int? id)
        {
            ViewBag.Action = "Edit Payment";
            IncomeUserPayment incomeUserPayment = GetIncomeUserPayment(id);
            if (incomeUserPayment == null)
            {
                return HttpNotFound();
            }
            return View("Payment", incomeUserPayment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPayment (int id, [Bind(Include = "Amount,Date")] IncomeUserPayment incomeUserPayment)
        {
            ViewBag.Action = "Edit Payment";
            IncomeUserPayment ip = GetIncomeUserPayment(id);
            if (ip == null)
            {
                return HttpNotFound();
            }
            if (ModelState.IsValid)
            {
                ip.Amount = incomeUserPayment.Amount;
                ip.Date = incomeUserPayment.Date;

                db.Entry(ip).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", new { id = ip.IncomeUser.ID });
            }
            return View("Payment", incomeUserPayment);
        }

        public ActionResult DeletePayment (int? id)
        {
            IncomeUserPayment incomeUserPayment = GetIncomeUserPayment(id);
            if (incomeUserPayment == null)
            {
                return HttpNotFound();
            }
            IncomeUser incomeUser = incomeUserPayment.IncomeUser;
            db.IncomeUserPayments.Remove(incomeUserPayment);
            db.SaveChanges();
            return RedirectToAction("Details", new { id = incomeUser.ID });
        }
    }
}