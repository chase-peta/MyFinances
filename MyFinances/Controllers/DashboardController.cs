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
    public class DashboardController : BaseController
    {        
        public ActionResult Index()
        {
            DashboardViewModel viewModel = new DashboardViewModel();

            IEnumerable<Bill> bills = db.Bills.Where(x => x.IsActive && (x.User.Id == user.Id || x.SharedWith.Where(y => y.SharedWithUser.Id == user.Id).Any())).Include(x => x.BillPayments).ToList().Populate(user).OrderBy(x => x.DueDate);
            IEnumerable<Loan> loans = db.Loans.Where(x => x.IsActive && (x.User.Id == user.Id || x.SharedWith.Where(y => y.SharedWithUser.Id == user.Id ).Any())).Include(x => x.LoanPayments).ToList().Populate(user).OrderBy(x => x.DueDate);

            DateTime startDate = DateTime.Now;
            foreach (Bill bill in bills) { startDate = (bill.DueDate <= startDate) ? bill.DueDate : startDate; }
            foreach (Loan loan in loans) { startDate = (loan.DueDate <= startDate) ? loan.DueDate : startDate; }

            startDate = GetUserStartDate(startDate, false);

            viewModel.DateRanges = InitiateDateRanges(startDate, false, false, true);

            foreach (DashboardDateRange range in viewModel.DateRanges)
            {
                List<DashboardItem> items = new List<DashboardItem>();
                items.AddRange(bills.GetDashboardItems(range, user));
                items.AddRange(loans.GetDashboardItems(range, user));
                range.Items = items.OrderBy(x => x.Date);
                
                List<DashboardIncomeItem> incomeItems = new List<DashboardIncomeItem>();
                List<ApplicationUser> payees = range.Items.Where(x => x.SharedWith != null && x.SharedWith.Any() && x.SharedWith.Where(y => y.Id != user.Id).Any()).SelectMany(x => x.SharedWith).Distinct().OrderBy(x => x.FirstDate).ThenBy(x => x.LastName).ToList();
                foreach(ApplicationUser payee in payees)
                {
                    incomeItems.Add(new DashboardIncomeItem(payee.FirstName + " " + payee.LastName, range.StartDate, range.Items.Where(x => x.SharedWith != null && x.SharedWith.Any() && x.SharedWith.Where(y => y.Id == payee.Id).Any()).Sum(x => x.SharedAmount)));
                }

                range.IncomeItems.AddRange(incomeItems);
            }

            return View(viewModel);
        }

        public ActionResult History (int year = 0, bool displayMonth = false)
        {
            year = (year == 0) ? DateTime.Now.Year : year;

            DashboardViewModel viewModel = new DashboardViewModel();
            viewModel.CurrentYear = year;
            viewModel.DisplayMonth = displayMonth;
            
            IEnumerable<BillPayment> billPayments = db.BillPayments.Where(x => x.User.Id == user.Id || x.SharedWith.Where(y => y.SharedWithUser.Id == user.Id).Any()).Include(x => x.Bill).Include(x => x.User).Include(x => x.SharedWith).OrderBy(x => x.DatePaid);
            IEnumerable<LoanPayment> loanPayments = db.LoanPayments.Where(x => x.User.Id == user.Id || x.SharedWith.Where(y => y.SharedWithUser.Id == user.Id).Any()).Include(x => x.Loan).Include(x => x.User).Include(x => x.SharedWith).OrderBy(x => x.DatePaid);

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

                startDate = GetUserStartDate(startDate, viewModel.DisplayMonth);
                viewModel.DateRanges = InitiateDateRanges(startDate, viewModel.DisplayMonth, true);
                viewModel.DateRanges = (user.PaycheckFrequency == PaymentFrequency.SemiMonthly) ? viewModel.DateRanges.OrderByDescending(x => x.StartDate.Month) : viewModel.DateRanges.OrderByDescending(x => x.StartDate);

                foreach (DashboardDateRange range in viewModel.DateRanges)
                {
                    List<DashboardItem> items = new List<DashboardItem>();
                    items.AddRange(billPayments.Where(x => x.DatePaid >= range.StartDate && x.DatePaid <= range.EndDate).Select(x => new DashboardItem(x, user)));
                    items.AddRange(loanPayments.Where(x => x.DatePaid >= range.StartDate && x.DatePaid <= range.EndDate).Select(x => new DashboardItem(x, user)));
                    range.Items = items.OrderBy(x => x.Date);
                }
                viewModel.DateRanges = viewModel.DateRanges.Where(x => x.Items.Any());
            }

            return View(viewModel);
        }

        private DateTime GetEndDate (DateTime startDate, bool displayMonth)
        {
            if (displayMonth)
            {
                return startDate.AddMonths(1);
            }
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

        private DateTime GetUserStartDate (DateTime startDate, bool displayMonth)
        {
            DateTime userDate = user.FirstDate;

            if (!displayMonth && (user.PaycheckFrequency == PaymentFrequency.Weekly || user.PaycheckFrequency == PaymentFrequency.BiWeekly))
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
            else if (displayMonth || user.PaycheckFrequency == PaymentFrequency.SemiMonthly || user.PaycheckFrequency == PaymentFrequency.Monthly || user.PaycheckFrequency == PaymentFrequency.BiMonthly || user.PaycheckFrequency == PaymentFrequency.SemiYearly || user.PaycheckFrequency == PaymentFrequency.Yearly)
            {
                userDate = new DateTime(startDate.Year, startDate.Month, userDate.Day);
            }

            return userDate;
        }

        private IEnumerable<DashboardDateRange> InitiateDateRanges (DateTime startDate, bool displayMonth, bool restOfYear = false, bool updateUser = false)
        {
            int year = startDate.Year;
            if (startDate.Month == 12)
            {
                year++;
            }
            DateTime endDate = GetEndDate(startDate, displayMonth).AddDays(-1);
            if (updateUser)
            {
                user.FirstDate = startDate;
                user.SecondDate = endDate.AddDays(1);
                //userManager.Update(user);
            }

            List<DashboardDateRange> dateRanges = new List<DashboardDateRange>();

            int count = 0;
            while ((restOfYear && (startDate.Year == year || endDate.Year == year)) || (count < 10))
            {
                count++;
                decimal paycheck = user.FirstPaycheck;
                if (user.PaycheckFrequency == PaymentFrequency.SemiMonthly && startDate.Day == user.SecondDate.Day)
                {
                    paycheck = user.SecondPaycheck;
                }
                dateRanges.Add(new DashboardDateRange(
                    startDate,
                    endDate,
                    paycheck
                ));

                startDate = endDate.AddDays(1);
                endDate = GetEndDate(startDate, displayMonth).AddDays(-1);
            }
            return dateRanges.OrderBy(x => x.StartDate);
        }
    }
}