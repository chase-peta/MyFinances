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
            viewModel.DateRanges = GetDateRanges();
            ViewBag.ShowViewMore = true;
            return View(viewModel);
        }

        public ActionResult More()
        {
            DashboardViewModel viewModel = new DashboardViewModel();
            viewModel.DateRanges = GetDateRanges(true);
            ViewBag.ShowViewMore = false;
            return View("Index", viewModel);
        }

        public ActionResult History (int year = 0, bool displayMonth = false)
        {
            year = (year == 0) ? DateTime.Now.Year : year;

            DashboardViewModel viewModel = new DashboardViewModel();
            viewModel.CurrentYear = year;
            viewModel.DisplayMonth = displayMonth;
            
            IEnumerable<BillPayment> billPayments = db.BillPayments.Where(x => x.User.Id == user.Id || x.SharedWith.Where(y => y.SharedWithUser.Id == user.Id).Any()).Include(x => x.Bill).Include(x => x.User).Include(x => x.SharedWith.Select(y => y.SharedWithUser)).OrderBy(x => x.DatePaid);
            IEnumerable<LoanPayment> loanPayments = db.LoanPayments.Where(x => x.User.Id == user.Id || x.SharedWith.Where(y => y.SharedWithUser.Id == user.Id).Any()).Include(x => x.Loan).Include(x => x.User).Include(x => x.SharedWith.Select(y => y.SharedWithUser)).OrderBy(x => x.DatePaid);
            IEnumerable<IncomePayment> incomePayments = db.IncomePayments.Where(x => x.User.Id == user.Id).Include(x => x.Income).Include(x => x.User).OrderBy(x => x.Date);

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
            if (incomePayments.Any())
            {
                int maxYearIncome = incomePayments.Max(x => x.Date).Year;
                int minYearIncome = incomePayments.Min(x => x.Date).Year;
                maxYear = (maxYearIncome > maxYear) ? maxYearIncome : maxYear;
                minYear = (minYearIncome < minYear) ? minYearIncome : minYear;
            }
            
            viewModel.StartYear = minYear;
            viewModel.EndYear = maxYear;

            IEnumerable<BillPayment> yearBillPayments = billPayments.Where(x => x.DatePaid.Year == year);
            IEnumerable<LoanPayment> yearLoanPayments = loanPayments.Where(x => x.DatePaid.Year == year);
            IEnumerable<IncomePayment> yearIncomePayments = incomePayments.Where(x => x.Date.Year == year);

            if (yearBillPayments.Any() || yearLoanPayments.Any() || yearIncomePayments.Any())
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
                if (yearIncomePayments.Any())
                {
                    DateTime incomeStartDate = yearIncomePayments.Min(x => x.Date);
                    startDate = (incomeStartDate < startDate) ? incomeStartDate : startDate;
                }

                startDate = GetUserStartDate(startDate, viewModel.DisplayMonth);
                viewModel.DateRanges = InitiateDateRanges(startDate, viewModel.DisplayMonth, true);
                viewModel.DateRanges = (user.PrimaryIncome.PaymentFrequency == PaymentFrequency.SemiMonthly) ? viewModel.DateRanges.OrderByDescending(x => x.StartDate.Month) : viewModel.DateRanges.OrderByDescending(x => x.StartDate);

                foreach (DashboardDateRange range in viewModel.DateRanges)
                {
                    List<DashboardItem> items = new List<DashboardItem>();
                    items.AddRange(yearBillPayments.Where(x => x.DatePaid >= range.StartDate && x.DatePaid <= range.EndDate).Select(x => new DashboardItem(x, user)));
                    items.AddRange(yearLoanPayments.Where(x => x.DatePaid >= range.StartDate && x.DatePaid <= range.EndDate).Select(x => new DashboardItem(x, user)));
                    range.Items = items.OrderBy(x => x.Date);

                    List<DashboardIncomeItem> incomeItems = new List<DashboardIncomeItem>();
                    incomeItems.AddRange(yearIncomePayments.Where(x => x.Date >= range.StartDate && x.Date <= range.EndDate).Select(x => new DashboardIncomeItem(x)));
                    range.IncomeItems = incomeItems.OrderBy(x => x.Date).ToList();
                }
                viewModel.DateRanges = viewModel.DateRanges.Where(x => x.Items.Any() || x.IncomeItems.Any());
            }

            return View(viewModel);
        }

        private IEnumerable<DashboardDateRange> GetDateRanges(bool restOfYear = false)
        {
            DateTime startDate = DateTime.Now;

            IEnumerable<Bill> bills = db.Bills.Where(x => x.IsActive && (x.User.Id == user.Id || x.SharedWith.Where(y => y.SharedWithUser.Id == user.Id).Any())).Include(x => x.BillPayments).ToList().Populate(user).OrderBy(x => x.DueDate);
            IEnumerable<Loan> loans = db.Loans.Where(x => x.IsActive && (x.User.Id == user.Id || x.SharedWith.Where(y => y.SharedWithUser.Id == user.Id).Any())).Include(x => x.LoanPayments).ToList().Populate(user).OrderBy(x => x.DueDate);
            IEnumerable<Income> incomes = db.Incomes.Where(x => x.User.Id == user.Id && x.IsActive).Include(x => x.IncomePayments).ToList().Populate(user).OrderBy(x => x.Date);

            if (bills.Any())
            {
                DateTime billStartDate = bills.Min(x => x.DueDate);
                if (billStartDate < startDate) { startDate = billStartDate; }
            }
            if (loans.Any())
            {
                DateTime loanStartDate = loans.Min(x => x.DueDate);
                if (loanStartDate < startDate) { startDate = loanStartDate; }
            }
            if (incomes.Any())
            {
                DateTime incomeStartDate = incomes.Min(x => x.Date);
                DateTime incomeSecondStartDate = incomes.Where(x => x.PaymentFrequency == PaymentFrequency.SemiMonthly).Min(x => x.SecondDate);
                if (incomeStartDate < startDate) { startDate = incomeStartDate; }
                if (incomeSecondStartDate < startDate) { startDate = incomeSecondStartDate; }
            }
            
            startDate = GetUserStartDate(startDate, false);

            IEnumerable<DashboardDateRange> dateRanges = InitiateDateRanges(startDate, false, restOfYear, true);

            foreach (DashboardDateRange range in dateRanges)
            {
                if (bills.Any() || loans.Any())
                {
                    List<DashboardItem> items = new List<DashboardItem>();
                    if (bills.Any()) { items.AddRange(bills.GetDashboardItems(range, user)); }
                    if (loans.Any()) { items.AddRange(loans.GetDashboardItems(range, user)); }
                    range.Items = items.OrderBy(x => x.Date);
                }
                if (incomes.Any())
                {
                    List<DashboardIncomeItem> incomeItems = new List<DashboardIncomeItem>();
                    incomeItems.AddRange(incomes.GetDashboardItems(range));
                    range.IncomeItems.AddRange(incomeItems);
                }
            }
            return dateRanges;
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
            if (user.PrimaryIncome != null)
            {
                DateTime userDate = user.PrimaryIncome.Date;

                if (!displayMonth && (user.PrimaryIncome.PaymentFrequency == PaymentFrequency.Weekly || user.PrimaryIncome.PaymentFrequency == PaymentFrequency.BiWeekly))
                {
                    while (userDate < startDate)
                    {
                        if (user.PrimaryIncome.PaymentFrequency == PaymentFrequency.Weekly)
                        {
                            userDate = user.PrimaryIncome.Date.AddDays(7);
                        }
                        else if (user.PrimaryIncome.PaymentFrequency == PaymentFrequency.BiWeekly)
                        {
                            userDate = user.PrimaryIncome.Date.AddDays(14);
                        }
                    }
                    while (userDate >= startDate)
                    {
                        if (user.PrimaryIncome.PaymentFrequency == PaymentFrequency.Weekly)
                        {
                            userDate = user.PrimaryIncome.Date.AddDays(-7);
                        }
                        else if (user.PrimaryIncome.PaymentFrequency == PaymentFrequency.BiWeekly)
                        {
                            userDate = user.PrimaryIncome.Date.AddDays(-14);
                        }
                    }
                }
                else if (displayMonth || user.PrimaryIncome.PaymentFrequency == PaymentFrequency.SemiMonthly || user.PrimaryIncome.PaymentFrequency == PaymentFrequency.Monthly || user.PrimaryIncome.PaymentFrequency == PaymentFrequency.BiMonthly || user.PrimaryIncome.PaymentFrequency == PaymentFrequency.SemiYearly || user.PrimaryIncome.PaymentFrequency == PaymentFrequency.Yearly)
                {
                    userDate = new DateTime(startDate.Year, startDate.Month, user.PrimaryIncome.Date.Day);
                }
                return userDate;
            } else
            {
                return startDate;
            }
        }

        private IEnumerable<DashboardDateRange> InitiateDateRanges (DateTime startDate, bool displayMonth, bool restOfYear = false, bool updateUser = false)
        {
            int year = startDate.Year;
            if (startDate.Month == 12)
            {
                year++;
            }
            DateTime endDate = GetEndDate(startDate, displayMonth).AddDays(-1);

            List<DashboardDateRange> dateRanges = new List<DashboardDateRange>();

            int count = 0;
            while ((restOfYear && (startDate.Year == year || endDate.Year == year)) || (count < 4))
            {
                count++;
                dateRanges.Add(new DashboardDateRange(
                    startDate,
                    endDate
                ));

                startDate = endDate.AddDays(1);
                endDate = GetEndDate(startDate, displayMonth).AddDays(-1);
            }
            return dateRanges.OrderBy(x => x.StartDate);
        }
    }
}