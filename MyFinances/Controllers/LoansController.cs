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
            ViewBag.SharedPercentage = Enum.GetValues(typeof(SharedPercentage));
            Loan loan = new Loan();
            loan.FirstPaymentDate = DateTime.Today;
            return View("Details", loan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(string button, string[] payeeId, string[] sharedPercent, [Bind(Include = "Name,LoanAmount,InterestRate,FirstPaymentDate,Additional,Escrow,Term,InterestCompound,PaymentInterestRate,IsShared")] Loan loan)
        {
            ViewBag.Action = "Create";
            ViewBag.Calculate = false;
            if (ModelState.IsValid)
            {
                loan.BasePayment = loan.CalculateMonthlyPayment();
                loan.User = db.Users.Find(user.Id);
                if (button == "Save")
                {
                    db.Loans.Add(loan);
                    UpdateLoanShared(loan, payeeId, sharedPercent);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                ViewBag.Calculate = true;
                loan = loan.Populate(user);
            }
            ViewBag.Users = GetAvailableUsers();
            ViewBag.SharedPercentage = Enum.GetValues(typeof(SharedPercentage));
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
            ViewBag.SharedPercentage = Enum.GetValues(typeof(SharedPercentage));
            return View("Details", loan);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string button, string[] payeeId, string[] sharedPercent, [Bind(Include = "ID,Name,LoanAmount,InterestRate,FirstPaymentDate,Additional,Escrow,Term,InterestCompound,PaymentInterestRate,IsShared")] Loan subLoan)
        {
            ViewBag.Action = "Edit";
            ViewBag.Calculate = false;
            Loan loan = db.Loans.Where(x => x.ID == subLoan.ID).Include(x => x.SharedWith).FirstOrDefault();
            if (loan == null || loan.User.Id != user.Id)
            {
                return HttpNotFound();
            }
            loan = loan.MapSubmit(subLoan);
            if (ModelState.IsValid)
            {
                if (button == "Save")
                {
                    db.Entry(loan).State = EntityState.Modified;
                    UpdateLoanShared(loan, payeeId, sharedPercent);
                    db.SaveChanges();
                    return RedirectToAction("Details", new { id = loan.ID });
                }
                ViewBag.Calculate = true;
            }
            ViewBag.Users = GetAvailableUsers();
            ViewBag.SharedPercentage = Enum.GetValues(typeof(SharedPercentage));
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

            LoanPayment payment = new LoanPayment()
            {
                DatePaid = loan.NextPayment.Date,
                Base = loan.NextPayment.Base,
                Additional = loan.NextPayment.Additional,
                Escrow = loan.NextPayment.Escrow,
                Interest = loan.NextPayment.Interest,
                Loan = loan
            };
            ViewBag.Users = GetAvailableUsers(loan);
            ViewBag.SharedPercentage = Enum.GetValues(typeof(SharedPercentage));
            return View("Payment", payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddPayment (int? id, string[] payeeId, string[] sharedPercent, [Bind(Include = "DatePaid,Base,Additional,Escrow,Interest")] LoanPayment loanPayment)
        {
            ViewBag.Action = "Add Payment";
            ViewBag.Calculate = false;
            loanPayment.Loan = GetLoan(id).Populate(user);
            if (loanPayment.Loan == null || loanPayment.Loan.User.Id != user.Id)
            {
                return HttpNotFound();
            }
            if (ModelState.IsValid)
            {
                IEnumerable<LoanPayment> payments = db.LoanPayments.Where(x => x.Loan.ID == loanPayment.Loan.ID && x.DatePaid < loanPayment.DatePaid).OrderBy(x => x.DatePaid);
                if (payments != null && payments.Any())
                {
                    loanPayment.Principal = payments.LastOrDefault().Principal - loanPayment.Base - loanPayment.Additional;
                } else
                {
                    loanPayment.Principal = loanPayment.Loan.LoanAmount - loanPayment.Base - loanPayment.Additional;
                }
                loanPayment.User = db.Users.Find(user.Id);
                db.LoanPayments.Add(loanPayment);
                UpdateLoanPaymentShared(loanPayment, payeeId, sharedPercent);
                db.SaveChanges();
                return RedirectToAction("Details", new { id = loanPayment.Loan.ID });
            }
            ViewBag.Users = GetAvailableUsers(loanPayment.Loan);
            ViewBag.SharedPercentage = Enum.GetValues(typeof(SharedPercentage));
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
            ViewBag.SharedPercentage = Enum.GetValues(typeof(SharedPercentage));
            return View("Payment", loanPayment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPayment (int? id, string[] payeeId, string[] sharedPercent, [Bind(Include = "DatePaid,Base,Additional,Escrow,Interest")] LoanPayment loanPayment)
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
                IEnumerable<LoanPayment> payments = db.LoanPayments.Where(x => x.Loan.ID == lp.Loan.ID && x.DatePaid < lp.DatePaid).OrderBy(x => x.DatePaid);
                if (payments != null && payments.Any())
                {
                    lp.Principal = payments.LastOrDefault().Principal - loanPayment.Base - loanPayment.Additional;
                }
                else
                {
                    lp.Principal = loanPayment.Loan.LoanAmount - loanPayment.Base - loanPayment.Additional;
                }
                db.Entry(lp).State = EntityState.Modified;
                UpdateLoanPaymentShared(lp, payeeId, sharedPercent);
                db.SaveChanges();
                return RedirectToAction("Details", new { id = lp.Loan.ID });
            }
            ViewBag.Users = GetAvailableUsers(loanPayment.Loan);
            ViewBag.SharedPercentage = Enum.GetValues(typeof(SharedPercentage));
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
