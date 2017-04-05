using System;
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

        public ActionResult Index()
        {
            string userId = User.Identity.GetUserId();
            DashboardViewModel viewModel = new DashboardViewModel();

            IEnumerable<Bill> bills = db.Bills.Where(x => x.UserId == userId && x.IsActive).Include(x => x.BillPayments).ToList().Populate().OrderBy(x => x.DueDate);
            IEnumerable<Loan> loans = db.Loans.Where(x => x.UserId == userId && x.IsActive).Include(x => x.LoanPayments).ToList().Populate().OrderBy(x => x.DueDate);

            DateTime startDate = DateTime.Now;
            foreach (Bill bill in bills) { startDate = (bill.DueDate <= startDate) ? bill.DueDate : startDate; }
            foreach (Loan loan in loans) { startDate = (loan.DueDate <= startDate) ? loan.DueDate : startDate; }

            viewModel.DateRanges = InitiateDateRanges(startDate);

            foreach (DashboardDateRange range in viewModel.DateRanges)
            {
                List<DashboardItem> items = new List<DashboardItem>();

                foreach (Bill bill in bills)
                {
                    if (bill.DueDate >= range.StartDate && bill.DueDate <= range.EndDate)
                    {
                        items.Add(new DashboardItem(bill));
                    }
                    if ((bill.IsDueDateStaysSame && bill.IsAmountStaysSame) || !bill.BillPayments.Any())
                    {
                        for (int i = 1; i <= 3; i++)
                        {
                            DateTime date = bill.DueDate.AddMonths(i);
                            if (date >= range.StartDate && date <= range.EndDate)
                            {
                                DashboardItem item = new DashboardItem(bill);
                                item.Date = date;
                                items.Add(item);
                            }
                        }
                    }
                    else
                    {
                        foreach (BillAverage average in bill.BillAverages)
                        {
                            if (average.Date >= range.StartDate && average.Date <= range.EndDate && average.Date.Month != bill.DueDate.Month)
                            {
                                DashboardItem item = new DashboardItem(average);
                                item.Id = bill.ID;
                                item.Name = bill.Name;
                                items.Add(item);
                            }
                        }
                    }

                    if (bill.BillPayments.Any())
                    {
                        foreach (BillPayment payment in bill.BillPayments)
                        {
                            if (payment.DatePaid >= range.StartDate && payment.DatePaid <= range.EndDate)
                            {
                                items.Add(new DashboardItem(payment));
                            }
                        }
                    }
                }

                foreach (Loan loan in loans)
                {
                    foreach (LoanOutlook outlook in loan.LoanOutlook)
                    {
                        if (outlook.Date >= range.StartDate && outlook.Date <= range.EndDate)
                        {
                            DashboardItem dbItem = new DashboardItem(outlook);
                            dbItem.Name = loan.Name;
                            dbItem.Id = loan.ID;
                            items.Add(dbItem);
                        }
                    }
                    foreach (LoanPayment payment in loan.LoanPayments)
                    {
                        if (payment.DatePaid >= range.StartDate && payment.DatePaid <= range.EndDate)
                        {
                            items.Add(new DashboardItem(payment));
                        }
                    }
                }

                range.Items = items.OrderBy(x => x.Date);
            }

            return View(viewModel);
        }

        private DateTime GetEndDate (DateTime startDate, ApplicationUser user)
        {
            if (user.PaycheckFrequency == PaymentFrequency.Weekly)
            {
                return startDate.AddDays(6);
            } else if (user.PaycheckFrequency == PaymentFrequency.BiWeekly)
            {
                return startDate.AddDays(13);
            } else if (user.PaycheckFrequency == PaymentFrequency.SemiMonthly)
            {
                return (startDate.Day == user.FirstDate.Day) ?
                    new DateTime(startDate.Year, startDate.Month, user.SecondDate.Day - 1) :
                    new DateTime(startDate.Year, startDate.Month + 1, user.FirstDate.Day).AddDays(-1);
            } else if (user.PaycheckFrequency == PaymentFrequency.Monthly)
            {
                return startDate.AddMonths(1).AddDays(-1);
            } else if (user.PaycheckFrequency == PaymentFrequency.BiMonthly)
            {
                return startDate.AddMonths(2).AddDays(-1);
            } else if (user.PaycheckFrequency == PaymentFrequency.SemiYearly)
            {
                return startDate.AddMonths(6).AddDays(-1);
            } else
            {
                return startDate.AddYears(1).AddDays(-1);
            }
        }

        private IEnumerable<DashboardDateRange> InitiateDateRanges (DateTime startDate)
        {
            ApplicationUserManager userManager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            string userId = User.Identity.GetUserId();
            ApplicationUser user = userManager.FindById(userId);
            DateTime userDate = user.FirstDate;

            if (user.PaycheckFrequency == PaymentFrequency.Weekly || user.PaycheckFrequency == PaymentFrequency.BiWeekly)
            {
                while (userDate < startDate)
                {
                    if (user.PaycheckFrequency == PaymentFrequency.Weekly) {
                        userDate = userDate.AddDays(7);
                    }
                    else if (user.PaycheckFrequency == PaymentFrequency.BiWeekly) {
                        userDate = userDate.AddDays(14);
                    }
                }
                while (userDate >= startDate)
                {
                    if (user.PaycheckFrequency == PaymentFrequency.Weekly) {
                        userDate = userDate.AddDays(-7);
                    } else if (user.PaycheckFrequency == PaymentFrequency.BiWeekly) {
                        userDate = userDate.AddDays(-14);
                    }
                }
            }
            else if (user.PaycheckFrequency == PaymentFrequency.SemiMonthly || user.PaycheckFrequency == PaymentFrequency.Monthly || user.PaycheckFrequency == PaymentFrequency.BiMonthly || user.PaycheckFrequency == PaymentFrequency.SemiYearly || user.PaycheckFrequency == PaymentFrequency.Yearly)
            {
                userDate = new DateTime(startDate.Year, startDate.Month, userDate.Day);
            }
            startDate = userDate;
            DateTime endDate = GetEndDate(startDate, user);
            user.FirstDate = startDate;
            user.SecondDate = endDate.AddDays(1);
            userManager.Update(user);

            List<DashboardDateRange> dateRanges = new List<DashboardDateRange>();
            for (int i = 1; i <= 6; i++)
            {
                dateRanges.Add(new DashboardDateRange(
                    startDate,
                    endDate
                ));

                startDate = endDate.AddDays(1);
                endDate = GetEndDate(startDate, user);
            }
            return dateRanges.OrderBy(x => x.StartDate);
        }
    }
}