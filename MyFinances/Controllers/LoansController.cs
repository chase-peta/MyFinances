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
using Microsoft.AspNet.Identity.Owin;

namespace MyFinances.Controllers
{
    [Authorize]
    public class LoansController : BaseController
    {
        public ActionResult Index(bool showInactive = false)
        {
            ViewBag.ShowInactive = showInactive;
            return View(GetLoans(showInactive));
        }
        
        public ActionResult Details(int? id)
        {
            ViewBag.Action = "Details";
            ViewBag.Calculate = false;
            Loan loan = GetLoan(id);
            if (id == null)
            {
                return HttpNotFound();
            }
            return View(loan);
        }

        public ActionResult Create ()
        {
            ViewBag.Action = "Create";
            ViewBag.Calculate = false;
            ViewBag.Users = GetAvailableUsers();
            return View("Details", new Loan());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(string button, string[] payeeId, [Bind(Include = "Name,LoanAmount,InterestRate,FirstPaymentDate,Additional,Escrow,Term,InterestCompound,PaymentInterestRate,IsShared")] Loan loan)
        {
            ViewBag.Action = "Create";
            ViewBag.Calculate = false;
            if (ModelState.IsValid)
            {
                if (button == "Save")
                {
                    loan.User = db.Users.Find(user.Id);
                    db.Loans.Add(loan);
                    UpdateLoanShared(loan, payeeId);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                ViewBag.Calculate = true;
                loan = loan.Populate(user);
            }
            ViewBag.Users = GetAvailableUsers();
            return View("Details", loan);
        }
        
        public ActionResult Edit(int? id)
        {
            ViewBag.Action = "Edit";
            ViewBag.Calculate = false;
            Loan loan = GetLoan(id);
            if (loan == null || loan.User.Id != user.Id)
            {
                return HttpNotFound();
            }
            ViewBag.Users = GetAvailableUsers();
            return View("Details", loan);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string button, string[] payeeId, [Bind(Include = "ID,Name,LoanAmount,InterestRate,FirstPaymentDate,Additional,Escrow,Term,InterestCompound,PaymentInterestRate,IsShared")] Loan subLoan)
        {
            ViewBag.Action = "Edit";
            ViewBag.Calculate = false;
            Loan loan = db.Loans.Find(subLoan.ID);
            if (loan == null || loan.User.Id != user.Id)
            {
                return HttpNotFound();
            }
            loan = loan.MapSubmit(subLoan).Populate(user);
            if (ModelState.IsValid)
            {
                if (button == "Save")
                {
                    db.Entry(loan).State = EntityState.Modified;
                    UpdateLoanShared(loan, payeeId);
                    db.SaveChanges();
                    return RedirectToAction("Details", new { id = loan.ID });
                }
                ViewBag.Calculate = true;
            }
            ViewBag.Users = GetAvailableUsers();
            return View("Details", loan);
        }

        public ActionResult Deactivate (int? id)
        {
            Loan loan = GetLoan(id);
            if (loan == null || loan.User.Id != user.Id)
            {
                return HttpNotFound();
            }
            loan.IsActive = false;
            db.Entry(loan).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Activate (int? id)
        {
            Loan loan = db.Loans.Find(id);
            loan.IsActive = true;
            db.Entry(loan).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Details", new { id = loan.ID });
        }

        public ActionResult Delete (int? id)
        {
            Loan loan = GetLoan(id);
            if (loan == null || loan.User.Id != user.Id)
            {
                return HttpNotFound();
            }
            db.SharedLoan.RemoveRange(loan.SharedWith);
            if (loan.LoanPayments.Any())
            {
                loan.LoanPayments.ToList().ForEach(x => db.SharedLoanPayment.RemoveRange(x.SharedWith));
                db.LoanPayments.RemoveRange(loan.LoanPayments);
            }
            db.Loans.Remove(loan);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult AddPayment (int? id)
        {
            ViewBag.Action = "Add Payment";
            ViewBag.Calculate = false;
            Loan loan = GetLoan(id);
            if (loan == null || loan.User.Id != user.Id)
            {
                return HttpNotFound();
            }

            LoanPayment payment = new LoanPayment();
            payment.DatePaid = loan.NextPayment.Date;
            payment.Base = loan.NextPayment.Base;
            payment.Additional = loan.NextPayment.Additional;
            payment.Escrow = loan.NextPayment.Escrow;
            payment.Interest = loan.NextPayment.Interest;
            payment.Loan = loan;
            ViewBag.Users = GetAvailableUsers(loan);
            return View("Payment", payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddPayment (int? id, string[] payeeId, [Bind(Include = "DatePaid,Base,Additional,Escrow,Interest")] LoanPayment loanPayment)
        {
            ViewBag.Action = "Add Payment";
            ViewBag.Calculate = false;
            loanPayment.Loan = GetLoan(id);
            if (loanPayment.Loan == null || loanPayment.Loan.User.Id != user.Id)
            {
                return HttpNotFound();
            }
            if (ModelState.IsValid)
            {
                loanPayment.User = db.Users.Find(user.Id);
                db.LoanPayments.Add(loanPayment);
                UpdateLoanPaymentShared(loanPayment, payeeId);
                db.SaveChanges();
                return RedirectToAction("Details", new { id = loanPayment.Loan.ID });
            }
            ViewBag.Users = GetAvailableUsers(loanPayment.Loan);
            return View("Payment", loanPayment);
        }

        public ActionResult EditPayment (int? id)
        {
            ViewBag.Action = "Edit Payment";
            ViewBag.Calculate = false;
            LoanPayment loanPayment = GetLoanPayment(id);
            if (loanPayment == null)
            {
                return HttpNotFound();
            }
            ViewBag.Users = GetAvailableUsers(loanPayment.Loan);
            return View("Payment", loanPayment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPayment (int? id, string[] payeeId, [Bind(Include = "DatePaid,Base,Additional,Escrow,Interest")] LoanPayment loanPayment)
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
                UpdateLoanPaymentShared(lp, payeeId);
                db.SaveChanges();
                return RedirectToAction("Details", new { id = lp.Loan.ID });
            }
            ViewBag.Users = GetAvailableUsers(loanPayment.Loan);
            return View("Payment", lp);
        }

        public ActionResult DeletePayment (int? id)
        {
            LoanPayment loanPayment = GetLoanPayment(id);
            Loan loan = loanPayment.Loan;
            db.SharedLoanPayment.RemoveRange(loanPayment.SharedWith);
            db.LoanPayments.Remove(loanPayment);
            db.SaveChanges();
            return RedirectToAction("Details", new { id = loan.ID });
        }
    }
}
