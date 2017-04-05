using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using MyFinances.Models;

namespace MyFinances.Controllers
{
    [Authorize]
    public class LoansController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        
        public ActionResult Index(bool showInactive = false)
        {
            ViewBag.ShowInactive = showInactive;
            string userId = User.Identity.GetUserId();
            if (showInactive)
            {
                return View(db.Loans.Where(x => x.UserId == userId).Include(x => x.LoanPayments).ToList().Populate().OrderBy(x => x.DueDate));
            }
            else
            {
                return View(db.Loans.Where(x => x.UserId == userId && x.IsActive).Include(x => x.LoanPayments).ToList().Populate().OrderBy(x => x.DueDate));
            }
        }
        
        public ActionResult Details(int? id)
        {
            ViewBag.Action = "Details";
            ViewBag.Calculate = false;
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Loan loan = db.Loans.Find(id).Populate();
            if (loan == null)
            {
                return HttpNotFound();
            }
            return View(loan);
        }

        public ActionResult Create ()
        {
            ViewBag.Action = "Create";
            ViewBag.Calculate = false;
            return View("Details");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(string button, [Bind(Include = "Name,LoanAmount,InterestRate,FirstPaymentDate,Additional,Escrow,Term,InterestCompound,PaymentInterestRate")] Loan loan)
        {
            ViewBag.Action = "Create";
            ViewBag.Calculate = false;
            if (ModelState.IsValid)
            {
                if (button == "Save")
                {
                    db.Loans.Add(loan);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                ViewBag.Calculate = true;
                loan = loan.Populate();
            }

            return View("Details", loan);
        }
        
        public ActionResult Edit(int? id)
        {
            ViewBag.Action = "Edit";
            ViewBag.Calculate = false;
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Loan loan = db.Loans.Find(id).Populate();
            if (loan == null)
            {
                return HttpNotFound();
            }
            return View("Details", loan);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string button, [Bind(Include = "ID,Name,LoanAmount,InterestRate,FirstPaymentDate,Additional,Escrow,Term,InterestCompound,PaymentInterestRate")] Loan subLoan)
        {
            ViewBag.Action = "Edit";
            ViewBag.Calculate = false;
            Loan loan = db.Loans.Find(subLoan.ID).MapSubmit(subLoan).Populate();
            if (ModelState.IsValid)
            {
                if (button == "Save")
                {
                    db.Entry(loan).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Details", new { id = loan.ID });
                }
                ViewBag.Calculate = true;
            }
            return View("Details", loan);
        }

        public ActionResult Deactivate (int id)
        {
            Loan loan = db.Loans.Find(id);
            loan.IsActive = false;
            db.Entry(loan).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Activate (int id)
        {
            Loan loan = db.Loans.Find(id);
            loan.IsActive = true;
            db.Entry(loan).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Details", new { id = loan.ID });
        }

        public ActionResult Delete (int id)
        {
            Loan loan = db.Loans.Find(id).Populate();
            if (loan.LoanPayments.Any())
            {
                loan.LoanPayments.ToList().ForEach(x => db.LoanPayments.Remove(x));
            }
            db.Loans.Remove(loan);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult AddPayment (int id)
        {
            ViewBag.Action = "Add Payment";
            ViewBag.Calculate = false;
            Loan loan = db.Loans.Find(id).Populate();

            LoanPayment payment = new LoanPayment();
            payment.DatePaid = loan.NextPayment.Date;
            payment.Base = loan.NextPayment.Base;
            payment.Additional = loan.NextPayment.Additional;
            payment.Escrow = loan.NextPayment.Escrow;
            payment.Interest = loan.NextPayment.Interest;
            payment.Loan = loan;

            return View("Payment", payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddPayment (int id, [Bind(Include = "DatePaid,Base,Additional,Escrow,Interest")] LoanPayment loanPayment)
        {
            ViewBag.Action = "Add Payment";
            ViewBag.Calculate = false;
            loanPayment.Loan = db.Loans.Find(id).Populate();
            if (ModelState.IsValid)
            {
                db.LoanPayments.Add(loanPayment);
                db.SaveChanges();
                return RedirectToAction("Details", new { id = loanPayment.Loan.ID });
            }
            return View("Payment", loanPayment);
        }

        public ActionResult EditPayment (int id)
        {
            ViewBag.Action = "Edit Payment";
            ViewBag.Calculate = false;
            LoanPayment payment = db.LoanPayments.Find(id);
            payment.Loan = payment.Loan.Populate();
            return View("Payment", payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPayment (int id, [Bind(Include = "DatePaid,Base,Additional,Escrow,Interest")] LoanPayment loanPayment)
        {
            ViewBag.Action = "Edit Payment";
            ViewBag.Calculate = false;
            LoanPayment lp = db.LoanPayments.Find(id);
            if (ModelState.IsValid)
            {
                lp.DatePaid = loanPayment.DatePaid;
                lp.Base = loanPayment.Base;
                lp.Additional = loanPayment.Additional;
                lp.Escrow = loanPayment.Escrow;
                lp.Interest = loanPayment.Interest;
                db.Entry(lp).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", new { id = lp.Loan.ID });
            }
            return View("Payment", lp);
        }

        public ActionResult DeletePayment (int id)
        {
            LoanPayment lp = db.LoanPayments.Find(id);
            Loan loan = lp.Loan;
            db.LoanPayments.Remove(lp);
            db.SaveChanges();
            return RedirectToAction("Details", new { id = loan.ID });
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
