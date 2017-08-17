using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using MyFinances.Models;

namespace MyFinances.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private ApplicationUserManager userManager;
        private ApplicationUser user;

        private void LoadUser()
        {
            userManager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            user = userManager.FindById(User.Identity.GetUserId());
        }

        public ActionResult Index()
        {
            LoadUser();
            DashboardViewModel viewModel = new DashboardViewModel();

            IEnumerable<Bill> bills = db.Bills.Where(x => x.UserId == user.Id && x.IsActive).Include(x => x.BillPayments).ToList().Populate().OrderBy(x => x.DueDate);
            IEnumerable<Loan> loans = db.Loans.Where(x => x.UserId == user.Id && x.IsActive).Include(x => x.LoanPayments).ToList().Populate().OrderBy(x => x.DueDate);

            DateTime startDate = DateTime.Now;
            foreach (Bill bill in bills) { startDate = (bill.DueDate <= startDate) ? bill.DueDate : startDate; }
            foreach (Loan loan in loans) { startDate = (loan.DueDate <= startDate) ? loan.DueDate : startDate; }

            startDate = GetUserStartDate(startDate);

            viewModel.DateRanges = InitiateDateRanges(startDate, false, true);

            foreach (DashboardDateRange range in viewModel.DateRanges)
            {
                List<DashboardItem> items = new List<DashboardItem>();
                items.AddRange(bills.GetDashboardItems(range, user));
                items.AddRange(loans.GetDashboardItems(range, user));
                range.Items = items.OrderBy(x => x.Date);
            }

            return View(viewModel);
        }

        public ActionResult History (int year = 0)
        {
            LoadUser();
            year = (year == 0) ? DateTime.Now.Year : year;

            DashboardViewModel viewModel = new DashboardViewModel();
            viewModel.CurrentYear = year;
            
            IEnumerable<BillPayment> billPayments = db.BillPayments.Where(x => x.UserId == user.Id).Include(x => x.Bill).OrderBy(x => x.DatePaid);
            IEnumerable<LoanPayment> loanPayments = db.LoanPayments.Where(x => x.UserId == user.Id).Include(x => x.Loan).OrderBy(x => x.DatePaid);

            int maxYear = 0;
            int minYear = DateTime.Now.Year;
            if (billPayments.Any())
            {
                int maxYearBill = billPayments.Max(x => x.DatePaid).Year;
                int minYearBill = billPayments.Min(x => x.DatePaid).Year;
                maxYear = (maxYearBill > maxYear) ? maxYearBill : maxYear;
                minYear = (minYearBill < minYear) ? minYearBill : minYear;
            }
            if (loanPayments.Any())
            {
                int maxYearLoan = loanPayments.Max(x => x.DatePaid).Year;
                int minYearLoan = loanPayments.Min(x => x.DatePaid).Year;
                maxYear = (maxYearLoan > maxYear) ? maxYearLoan : maxYear;
                minYear = (minYearLoan < minYear) ? minYearLoan : minYear;
            }
            
            viewModel.StartYear = minYear;
            viewModel.EndYear = maxYear;

            IEnumerable<BillPayment> yearBillPayments = billPayments.Where(x => x.DatePaid.Year == year);
            IEnumerable<LoanPayment> yearLoanPayments = loanPayments.Where(x => x.DatePaid.Year == year);

            if (yearBillPayments.Any() || yearLoanPayments.Any())
            {
                DateTime startDate = DateTime.Now;
                if (yearBillPayments.Any())
                {
                    DateTime billStartDate = yearBillPayments.Min(x => x.DatePaid);
                    startDate = (billStartDate < startDate) ? billStartDate : startDate;
                }
                if (yearLoanPayments.Any())
                {
                    DateTime loanStartDate = yearLoanPayments.Min(x => x.DatePaid);
                    startDate = (loanStartDate < startDate) ? loanStartDate : startDate;
                }

                startDate = GetUserStartDate(startDate);
                viewModel.DateRanges = InitiateDateRanges(startDate, true);
                viewModel.DateRanges = (user.PaycheckFrequency == PaymentFrequency.SemiMonthly) ? viewModel.DateRanges.OrderByDescending(x => x.StartDate.Month) : viewModel.DateRanges.OrderByDescending(x => x.StartDate);

                foreach (DashboardDateRange range in viewModel.DateRanges)
                {
                    List<DashboardItem> items = new List<DashboardItem>();
                    items.AddRange(billPayments.Where(x => x.DatePaid >= range.StartDate && x.DatePaid <= range.EndDate).Select(x => new DashboardItem(x)));
                    items.AddRange(loanPayments.Where(x => x.DatePaid >= range.StartDate && x.DatePaid <= range.EndDate).Select(x => new DashboardItem(x)));
                    range.Items = items.OrderBy(x => x.Date);
                }
                viewModel.DateRanges = viewModel.DateRanges.Where(x => x.Items.Any());
            }

            return View(viewModel);
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private DateTime GetEndDate (DateTime startDate)
        {
            switch (user.PaycheckFrequency)
            {
                case PaymentFrequency.Weekly:
                    return startDate.AddDays(7);
                case PaymentFrequency.BiWeekly:
                    return startDate.AddDays(14);
                case PaymentFrequency.SemiMonthly:
                    return (startDate.Day == user.FirstDate.Day) ?
                    new DateTime(startDate.Year, startDate.Month, user.SecondDate.Day) :
                    new DateTime(startDate.Year, startDate.Month, user.FirstDate.Day).AddMonths(1);
                case PaymentFrequency.Monthly:
                    return startDate.AddMonths(1);
                case PaymentFrequency.BiMonthly:
                    return startDate.AddMonths(2);
                case PaymentFrequency.SemiYearly:
                    return startDate.AddMonths(6);
                case PaymentFrequency.Yearly:
                    return startDate.AddYears(1);
                default:
                    return startDate;
            }
        }

        private DateTime GetUserStartDate (DateTime startDate)
        {
            DateTime userDate = user.FirstDate;

            if (user.PaycheckFrequency == PaymentFrequency.Weekly || user.PaycheckFrequency == PaymentFrequency.BiWeekly)
            {
                while (userDate < startDate)
                {
                    if (user.PaycheckFrequency == PaymentFrequency.Weekly)
                    {
                        userDate = userDate.AddDays(7);
                    }
                    else if (user.PaycheckFrequency == PaymentFrequency.BiWeekly)
                    {
                        userDate = userDate.AddDays(14);
                    }
                }
                while (userDate >= startDate)
                {
                    if (user.PaycheckFrequency == PaymentFrequency.Weekly)
                    {
                        userDate = userDate.AddDays(-7);
                    }
                    else if (user.PaycheckFrequency == PaymentFrequency.BiWeekly)
                    {
                        userDate = userDate.AddDays(-14);
                    }
                }
            }
            else if (user.PaycheckFrequency == PaymentFrequency.SemiMonthly || user.PaycheckFrequency == PaymentFrequency.Monthly || user.PaycheckFrequency == PaymentFrequency.BiMonthly || user.PaycheckFrequency == PaymentFrequency.SemiYearly || user.PaycheckFrequency == PaymentFrequency.Yearly)
            {
                userDate = new DateTime(startDate.Year, startDate.Month, userDate.Day);
            }

            return userDate;
        }

        private IEnumerable<DashboardDateRange> InitiateDateRanges (DateTime startDate, bool restOfYear = false, bool updateUser = false)
        {
            int year = startDate.Year;
            if (startDate.Month == 12)
            {
                year++;
            }
            DateTime endDate = GetEndDate(startDate).AddDays(-1);
            if (updateUser)
            {
                user.FirstDate = startDate;
                user.SecondDate = endDate.AddDays(1);
                userManager.Update(user);
            }

            List<DashboardDateRange> dateRanges = new List<DashboardDateRange>();

            int count = 0;
            while ((restOfYear && (startDate.Year == year || endDate.Year == year)) || (count < 10))
            {
                count++;
                dateRanges.Add(new DashboardDateRange(
                    startDate,
                    endDate
                ));

                startDate = endDate.AddDays(1);
                endDate = GetEndDate(startDate).AddDays(-1);
            }
            return dateRanges.OrderBy(x => x.StartDate);
        }
    }
}