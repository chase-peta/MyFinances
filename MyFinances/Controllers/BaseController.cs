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
            user.PrimaryIncome = GetPrimaryIncome().Income;
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

        protected List<ApplicationUser> GetAllAvailableUsers()
        {
            return db.Users.Where(x => x.Id != user.Id).OrderBy(x => new { x.FirstName, x.LastName }).ToList();
        }

        protected List<ApplicationUser> GetAvailableUsers()
        {
            List<string> userIds = db.IncomeUsers.Where(x => x.User.Id == user.Id).Select(x => x.PayeeUser.Id).ToList();
            return db.Users.Where(x => userIds.Contains(x.Id)).OrderBy(x => new { x.FirstName, x.LastName }).ToList();
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
            else if (bill.User.Id == user.Id || bill.SharedWith.ToList().Find(x => x.SharedWithUser.Id == user.Id) != null)
            {
                bill = bill.Populate(user);
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
                return db.Loans.Where(x => x.User.Id == user.Id).Include(x => x.LoanPayments).ToList().Populate(user).OrderBy(x => x.IsActive).ThenBy(x => x.DueDate).ToList();
            }

            return db.Loans.Where(x => x.User.Id == user.Id && x.IsActive).Include(x => x.LoanPayments).ToList().Populate(user).OrderBy(x => x.DueDate).ToList();
        }

        protected Loan GetLoan (int? id)
        {
            Loan loan = db.Loans.Find(id);
            if (loan == null)
            {
                return null;
            }
            else if (loan.User.Id == user.Id || loan.SharedWith.ToList().Find(x => x.SharedWithUser.Id == user.Id) != null)
            {
                loan = loan.Populate(user);
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



        protected List<Income> GetIncomes(bool showInactive)
        {
            if (showInactive)
            {
                return db.Incomes.Where(x => x.User.Id == user.Id).Include(x => x.IncomePayments).ToList().Populate(user).OrderBy(x => x.IsActive).ThenBy(x => x.Date).ToList();
            }

            return db.Incomes.Where(x => x.User.Id == user.Id && x.IsActive).Include(x => x.IncomePayments).ToList().Populate(user).OrderBy(x => x.Date).ToList();
        }

        protected Income GetIncome(int? id)
        {
            Income income = db.Incomes.Find(id);
            if (income == null)
            {
                return null;
            }
            else if (income.User.Id == user.Id)
            {
                income = income.Populate(user);
            }
            else
            {
                return null;
            }
            return income;
        }

        protected IncomePayment GetIncomePayment (int? id)
        {
            IncomePayment incomePayment = db.IncomePayments.Find(id);
            if (incomePayment == null || incomePayment.User.Id != user.Id)
            {
                return null;
            }
            incomePayment.Income = incomePayment.Income.Populate(incomePayment.User);
            return incomePayment;
        }

        protected PrimaryIncome GetPrimaryIncome()
        {
            PrimaryIncome primaryIncome = db.PrimaryIncome.Where(x => x.User.Id == user.Id).Include(x => x.Income).FirstOrDefault();
            if (primaryIncome == null)
            {
                return new PrimaryIncome
                {
                    User = user
                };
            }
            primaryIncome.Income = primaryIncome.Income.Populate(user);
            return primaryIncome;
        }


        protected IncomeUser GetIncomeUser(int? id)
        {
            IncomeUser incomeUser = db.IncomeUsers.Find(id);
            if (incomeUser == null)
            {
                return null;
            }
            else if (incomeUser.User.Id == user.Id)
            {
                incomeUser = incomeUser.Populate(user);
                incomeUser.Bills = db.SharedBill.Where(x => x.SharedWithUser.Id == incomeUser.PayeeUser.Id && x.Bill.User.Id == user.Id).OrderBy(x => x.Bill.Name).Include(x => x.Bill).ToList();
                incomeUser.Loans = db.SharedLoan.Where(x => x.SharedWithUser.Id == incomeUser.PayeeUser.Id && x.Loan.User.Id == user.Id).OrderBy(x => x.Loan.Name).Include(x => x.Loan).ToList();
                incomeUser.BillPayments = db.SharedBillPayment.Where(x => x.SharedWithUser.Id == incomeUser.PayeeUser.Id && x.BillPayment.User.Id == user.Id).OrderBy(x => x.BillPayment.Bill.Name).Include(x => x.BillPayment).ToList();
                incomeUser.LoanPayments = db.SharedLoanPayment.Where(x => x.SharedWithUser.Id == incomeUser.PayeeUser.Id && x.LoanPayment.User.Id == user.Id).OrderBy(x => x.LoanPayment.Loan.Name).Include(x => x.LoanPayment).ToList();
                
                List<int> bpIds = new List<int>();
                List<int> lpIds = new List<int>();
                foreach(IncomeUserPayment p in incomeUser.IncomeUserPayments)
                {
                    p.BillPayments.ForEach(x => bpIds.Add(x.BillPayment.ID));
                    p.LoanPayments.ForEach(x => lpIds.Add(x.LoanPayment.ID));
                }
                List<SharedBillPayment> bp = incomeUser.BillPayments.Where(x => !bpIds.Contains(x.BillPayment.ID)).ToList();
                List<SharedLoanPayment> lp = incomeUser.LoanPayments.Where(x => !lpIds.Contains(x.LoanPayment.ID)).ToList();
                if (bp.Any() || lp.Any())
                {
                    DateTime minDate = DateTime.Now; 
                    DateTime maxDate = DateTime.Now; 
                    if (bp.Any())
                    {
                        DateTime billMinDate = bp.Min(x => x.BillPayment.DatePaid);
                        DateTime billMaxDate = bp.Max(x => x.BillPayment.DatePaid);
                        if (billMinDate < minDate) { minDate = billMinDate; }
                        if (billMaxDate > maxDate) { maxDate = billMaxDate; }
                    }
                    if (lp.Any())
                    {
                        DateTime loanMinDate = lp.Min(x => x.LoanPayment.DatePaid);
                        DateTime loanMaxDate = lp.Max(x => x.LoanPayment.DatePaid);
                        if (loanMinDate < minDate) { minDate = loanMinDate; }
                        if (loanMaxDate > maxDate) { maxDate = loanMaxDate; }
                    }
                    minDate = new DateTime(minDate.Year, minDate.Month, incomeUser.Date.Day);
                    maxDate = maxDate.AddMonths(1);
                    maxDate = new DateTime(maxDate.Year, maxDate.Month, incomeUser.Date.Day);
                    incomeUser.NotPaidMinYear = minDate.Year;
                    incomeUser.NotPaidMaxYear = maxDate.Year;
                    incomeUser.NotPaidIncomeUserPayments = new List<NotPaidIncomeUserPayemnt>();
                    while(minDate < maxDate)
                    {
                        incomeUser.NotPaidIncomeUserPayments.Add( new NotPaidIncomeUserPayemnt
                        {
                            Date = minDate,
                            BillPayments = bp.Where(x => x.BillPayment.DatePaid.Year == minDate.Year && x.BillPayment.DatePaid.Month == minDate.Month).ToList(),
                            LoanPayments = lp.Where(x => x.LoanPayment.DatePaid.Year == minDate.Year && x.LoanPayment.DatePaid.Month == minDate.Month).ToList()
                        });
                        minDate = minDate.AddMonths(1);
                    }
                }
            }
            else
            {
                return null;
            }
            return incomeUser;
        }

        protected IncomeUserPayment GetIncomeUserPayment(int? id)
        {
            IncomeUserPayment incomeUserPayment = db.IncomeUserPayments.Find(id);
            if (incomeUserPayment == null || incomeUserPayment.User.Id != user.Id)
            {
                return null;
            }
            incomeUserPayment.IncomeUser = GetIncomeUser(incomeUserPayment.IncomeUser.ID).Populate(user);
            return incomeUserPayment;
        }



        protected void UpdateBillShared (Bill bill, string[] payeeId, string[] sharedPercent)
        {
            if (bill.SharedWith != null)
            {
                db.SharedBill.RemoveRange(bill.SharedWith);
            }
            if (payeeId != null && sharedPercent != null)
            {
                foreach (string id in payeeId)
                {
                    string percent = GetPercentById(id, sharedPercent);
                    percent = (percent == "" ? "Half" : percent);
                    db.SharedBill.Add(
                        new SharedBill(
                            bill,
                            db.Users.Find(id),
                            (SharedPercentage)Enum.Parse(typeof(SharedPercentage), percent)
                        )
                    );
                }
            }
        }

        protected void UpdateBillPaymentShared (BillPayment billPayment, string[] payeeId, string[] sharedPercent)
        {
            if (billPayment.SharedWith != null)
            {
                db.SharedBillPayment.RemoveRange(billPayment.SharedWith);
            }
            if (payeeId != null && sharedPercent != null)
            {
                foreach (string id in payeeId)
                {
                    string percent = GetPercentById(id, sharedPercent);
                    percent = (percent == "" ? "Half" : percent);
                    db.SharedBillPayment.Add(
                        new SharedBillPayment(
                            billPayment,
                            db.Users.Find(id),
                            (SharedPercentage)Enum.Parse(typeof(SharedPercentage), percent)
                        )
                    );
                }
            }
        }

        protected void UpdateLoanShared(Loan loan, string[] payeeId, string[] sharedPercent)
        {
            if (loan.SharedWith != null)
            {
                db.SharedLoan.RemoveRange(loan.SharedWith);
            }
            if (payeeId != null && sharedPercent != null)
            {
                foreach (string id in payeeId)
                {
                    string percent = GetPercentById(id, sharedPercent);
                    percent = (percent == "" ? "Half" : percent);
                    db.SharedLoan.Add(
                        new SharedLoan(
                            loan,
                            db.Users.Find(id),
                            (SharedPercentage)Enum.Parse(typeof(SharedPercentage), percent)
                        )
                    );
                }
            }
        }

        protected void UpdateLoanPaymentShared (LoanPayment loanPayment, string[] payeeId, string[] sharedPercent)
        {
            if (loanPayment.SharedWith != null)
            {
                db.SharedLoanPayment.RemoveRange(loanPayment.SharedWith);
            }
            if (payeeId != null && sharedPercent != null)
            {
                foreach (string id in payeeId)
                {
                    string percent = GetPercentById(id, sharedPercent);
                    percent = (percent == "" ? "Half" : percent);
                    db.SharedLoanPayment.Add(
                        new SharedLoanPayment(
                            loanPayment,
                            db.Users.Find(id),
                            (SharedPercentage)Enum.Parse(typeof(SharedPercentage), percent)
                        )
                    );
                }
            }
        }

        protected void UpdateIncomeUserBills(IncomeUser incomeUser, int[] billIds, string[] sharedPercentBills)
        {
            foreach(int billId in billIds)
            {
                Bill bill = db.Bills.Where(x => x.ID == billId).Include(x => x.SharedWith).FirstOrDefault();
                if (bill != null)
                {
                    db.SharedBill.RemoveRange(bill.SharedWith.Where(x => x.SharedWithUser.Id == incomeUser.PayeeUser.Id));
                    string percent = GetPercentById(billId.ToString(), sharedPercentBills);
                    percent = (percent == "" ? "Half" : percent);
                    db.SharedBill.Add(
                        new SharedBill(
                            bill,
                            incomeUser.PayeeUser,
                            (SharedPercentage)Enum.Parse(typeof(SharedPercentage), percent)
                        )
                    );
                }
            }
        }
        
        protected void UpdateIncomeUserLoans(IncomeUser incomeUser, int[] loanIds, string[] sharedPercentLoans)
        {
            foreach (int loanId in loanIds)
            {
                Loan loan = db.Loans.Where(x => x.ID == loanId).Include(x => x.SharedWith).FirstOrDefault();
                if (loan != null)
                {
                    db.SharedLoan.RemoveRange(loan.SharedWith.Where(x => x.SharedWithUser.Id == incomeUser.PayeeUser.Id));
                    string percent = GetPercentById(loanId.ToString(), sharedPercentLoans);
                    percent = (percent == "" ? "Half" : percent);
                    db.SharedLoan.Add(
                        new SharedLoan(
                            loan,
                            incomeUser.PayeeUser,
                            (SharedPercentage)Enum.Parse(typeof(SharedPercentage), percent)
                        )
                    );
                }
            }
        }

        protected string GetPercentById(string id, string[] sharedPercent)
        {
            foreach (string str in sharedPercent)
            {
                string[] idPercent = str.Split('|');
                if (id == idPercent[0])
                {
                    return idPercent[1];
                }
            }
            return "";
        }
    }
}