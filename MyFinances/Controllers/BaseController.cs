using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using MyFinances.Models;

namespace MyFinances.Controllers
{
    public class BaseController : Controller
    {
        protected ApplicationDbContext db = new ApplicationDbContext ();
        protected ApplicationUser user;

        public BaseController()
        {
            user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());
            ViewBag.User = user;
        }
        
        protected override void Dispose (bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        protected List<ApplicationUser> GetAvailableUsers()
        {
            return db.Users.Where(x => x.Id != user.Id).OrderBy(x => new { x.FirstName, x.LastName }).ToList();
        }

        protected List<ApplicationUser> GetAvailableUsers(Bill bill)
        {
            return bill.SharedWith.Select(x => x.SharedWithUser).ToList();
        }

        protected List<ApplicationUser> GetAvailableUsers(Loan loan)
        {
            return loan.SharedWith.Select(x => x.SharedWithUser).ToList();
        }



        protected List<Bill> GetBills(bool showInactive)
        {
            if (showInactive)
            {
                return db.Bills.Where(x => x.User.Id == user.Id).Include(x => x.BillPayments).OrderBy(x => x.IsActive).ThenBy(x => x.DueDate).ToList().Populate(user);
            }

            return db.Bills.Where(x => x.User.Id == user.Id && x.IsActive).Include(x => x.BillPayments).OrderBy(x => x.DueDate).ToList().Populate(user);
        }

        protected Bill GetBill(int? id)
        {
            Bill bill = db.Bills.Find(id);
            if (bill == null)
            {
                return null;
            }
            else if (bill.User.Id == user.Id)
            {
                bill = bill.Populate(user);
            }
            else if (bill.SharedWith.ToList().Find(x => x.SharedWithUser.Id == user.Id) != null)
            {
                bill = bill.Populate(user, true);
            } else
            {
                return null;
            }
            return bill;
        }

        protected BillPayment GetBillPayment(int? id)
        {
            BillPayment billPayment = db.BillPayments.Find(id);
            if (billPayment == null || billPayment.User.Id != user.Id)
            {
                return null;
            }
            billPayment.Bill = billPayment.Bill.Populate(billPayment.User);
            return billPayment;
        }



        protected List<Loan> GetLoans(bool showInactive)
        {
            if (showInactive)
            {
                return db.Loans.Where(x => x.User.Id == user.Id).Include(x => x.LoanPayments).OrderBy(x => x.IsActive).ThenBy(x => x.DueDate).ToList().Populate(user);
            }

            return db.Loans.Where(x => x.User.Id == user.Id && x.IsActive).Include(x => x.LoanPayments).OrderBy(x => x.DueDate).ToList().Populate(user);
        }

        protected Loan GetLoan (int? id)
        {
            Loan loan = db.Loans.Find(id);
            if (loan == null)
            {
                return null;
            }
            else if (loan.User.Id == user.Id)
            {
                loan = loan.Populate(user);
            }
            else if (loan.SharedWith.ToList().Find(x => x.SharedWithUser.Id == user.Id) != null)
            {
                loan = loan.Populate(user, true);
            }
            else
            {
                return null;
            }
            return loan;
        }

        protected LoanPayment GetLoanPayment(int? id)
        {
            LoanPayment loanPayment = db.LoanPayments.Find(id);
            if (loanPayment == null || loanPayment.User.Id != user.Id)
            {
                return null;
            }
            loanPayment.Loan = loanPayment.Loan.Populate(loanPayment.User);
            return loanPayment;
        }


        
        protected void UpdateBillShared (Bill bill, string[] payeeId)
        {
            if (bill.SharedWith != null)
            {
                db.SharedBill.RemoveRange(bill.SharedWith);
            }
            if (payeeId != null)
            {
                payeeId.ToList().ForEach(x => db.SharedBill.Add(new SharedBill(bill, db.Users.Find(x))));
            }
        }

        protected void UpdateBillPaymentShared (BillPayment billPayment, string[] payeeId)
        {
            if (billPayment.SharedWith != null)
            {
                db.SharedBillPayment.RemoveRange(billPayment.SharedWith);
            }
            if (payeeId != null)
            {
                payeeId.ToList().ForEach(x => db.SharedBillPayment.Add(new SharedBillPayment(billPayment, db.Users.Find(x))));
            }
        }


        protected void UpdateLoanShared(Loan loan, string[] payeeId)
        {
            if (loan.SharedWith != null)
            {
                db.SharedLoan.RemoveRange(loan.SharedWith);
            }
            if (payeeId != null)
            {
                payeeId.ToList().ForEach(x => db.SharedLoan.Add(new SharedLoan(loan, db.Users.Find(x))));
            }
        }

        protected void UpdateLoanPaymentShared (LoanPayment loanPayment, string[] payeeId)
        {
            if (loanPayment.SharedWith != null)
            {
                db.SharedLoanPayment.RemoveRange(loanPayment.SharedWith);
            }
            if (payeeId != null)
            {
                payeeId.ToList().ForEach(x => db.SharedLoanPayment.Add(new SharedLoanPayment(loanPayment, db.Users.Find(x))));
            }
        }
    }
}