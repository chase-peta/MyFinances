using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace MyFinances.Models
{
    public class Income : BaseFinances
    {
        [Required]
        public string Name { get; set; }

        [Column(TypeName = "money"), DataType(DataType.Currency), Required]
        public decimal Amount { get; set; }

        [Display(Name = "Second Amount"), Column(TypeName = "money"), DataType(DataType.Currency), Required]
        public decimal SecondAmount { get; set; }

        [Display(Name = "Payment Frequency"), Required]
        public PaymentFrequency PaymentFrequency { get; set; }

        [DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true), Required]
        public DateTime Date { get; set; }

        [Display(Name = "Second Date"), DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true), Required]
        public DateTime SecondDate { get; set; }

        public virtual ICollection<IncomePayment> IncomePayments { get; set; }
        
        [NotMapped, Display(Name = "Last Payment Date"), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime LastPaymentDate { get; set; }

        [NotMapped, Display(Name = "Last Payment Amount"), DisplayFormat(DataFormatString = "{0:c}")]
        public decimal LastPaymentAmount { get; set; }

        [NotMapped]
        public int MinYear { get; set; }

        [NotMapped]
        public int MaxYear { get; set; }

        [NotMapped]
        public bool UseSecond { get; set; }
    }

    public class IncomePayment : BaseFinances
    {
        [Column(TypeName = "money"), DataType(DataType.Currency), Required]
        public decimal Amount { get; set; }

        [DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true), Required]
        public DateTime Date { get; set; }

        public virtual Income Income { get; set; }
    }

    public class PrimaryIncome
    {
        public int ID { get; set; }
        public Income Income { get; set; }
        public ApplicationUser User { get; set; }
    }

    public static class IncomeHelpers
    {
        public static List<Income> Populate (this List<Income> incomes, ApplicationUser user)
        {
            List<Income> newIncomes = new List<Income>();
            foreach (Income income in incomes)
            {
                newIncomes.Add(income.Populate(user));
            }
            return newIncomes;
        }

        public static Income Populate (this Income income, ApplicationUser user)
        {
            if (income.IncomePayments != null && income.IncomePayments.Any())
            {
                IncomePayment lastPayment = income.IncomePayments.OrderBy(x => x.Date).LastOrDefault();
                if (lastPayment != null)
                {
                    income.LastPaymentDate = lastPayment.Date;
                    income.LastPaymentAmount = lastPayment.Amount;
                }
                income.MinYear = income.IncomePayments.Min(x => x.Date.Year);
                income.MaxYear = income.IncomePayments.Max(x => x.Date.Year);

                if (income.PaymentFrequency == PaymentFrequency.SemiMonthly && income.SecondDate < income.Date)
                {
                    income.UseSecond = true;
                }
            }

            double dueInDays = ((income.UseSecond ? income.SecondDate : income.Date) - new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day)).TotalDays;
            bool isPastDue = dueInDays < 0;
            income.DueIn = GetDueIn(income.IsActive, isPastDue, dueInDays);
            income.Classes = GetClasses(income.IsActive, isPastDue, dueInDays);

            return income;
        }

        public static List<DashboardIncomeItem> GetDashboardItems(this IEnumerable<Income> incomes, DashboardDateRange range)
        {
            List<DashboardIncomeItem> items = new List<DashboardIncomeItem>();
            foreach (Income income in incomes)
            {
                DateTime date = income.Date;
                Decimal amount = income.Amount;
                if (income.Date >= range.StartDate && income.Date <= range.EndDate)
                {
                    items.Add(new DashboardIncomeItem(income));
                }

                if (income.PaymentFrequency == PaymentFrequency.SemiMonthly && income.SecondDate >= range.StartDate && income.SecondDate <= range.EndDate)
                {
                    date = income.SecondDate;
                    DashboardIncomeItem item = new DashboardIncomeItem(income)
                    {
                        Date = income.SecondDate,
                        Amount = income.SecondAmount
                    };
                    items.Add(item);
                }
                
                while (date <= range.EndDate)
                {
                    if (income.PaymentFrequency != PaymentFrequency.SemiMonthly)
                    {
                        date = income.GetNextDate(date);
                    }
                    else
                    {
                        if (date.Date.Day == income.Date.Day)
                        {
                            date = new DateTime(date.Year, date.Month, income.SecondDate.Day);
                            amount = income.SecondAmount;
                        }
                        else
                        {
                            date = date.AddMonths(1);
                            date = new DateTime(date.Year, date.Month, income.Date.Day);
                            amount = income.Amount;
                        }
                    }
                    if (date >= range.StartDate && date <= range.EndDate)
                    {
                        DashboardIncomeItem item = new DashboardIncomeItem(income)
                        {
                            Date = date,
                            Amount = amount
                        };
                        items.Add(item);
                    }
                }

                if (income.IncomePayments.Any())
                {
                    items.AddRange(income.IncomePayments.Where(x => x.Date >= range.StartDate && x.Date <= range.EndDate)
                        .Select(x => new DashboardIncomeItem(x)));

                }
            }

            return items;
        }

        public static Income MapSubmit (this Income income, Income i)
        {
            income.Name = i.Name;
            income.PaymentFrequency = i.PaymentFrequency;
            income.Amount = i.Amount;
            income.SecondAmount = i.SecondAmount;
            income.Date = i.Date;
            income.SecondDate = i.SecondDate;

            return income;
        }

        private static string GetDueIn (bool isActive, bool isPastDue, double dueInDays)
        {
            if (!isActive)
                return "Not Active";
            if (isPastDue)
                return "Past Due";
            else if (dueInDays < 0)
                return "Paid";
            else if (dueInDays == 0)
                return "Today";
            else if (dueInDays == 1)
                return "Tomorrow";
            else
                return dueInDays.ToString() + " Days";
        }

        private static string GetClasses (bool isActive, bool isPastDue, double dueInDays)
        {
            if (!isActive)
                return "not-active";
            if (isPastDue)
                return "alert-danger";
            else if (dueInDays < 0)
                return "alert-success";
            else if (dueInDays < 5)
                return "alert-warning";
            else
                return "";
        }

        public static DateTime GetNextDate (this Income income, DateTime date)
        {
            switch (income.PaymentFrequency)
            {
                case PaymentFrequency.Weekly:
                    date = date.AddDays(7);
                    break;
                case PaymentFrequency.BiWeekly:
                    date = date.AddDays(14);
                    break;
                case PaymentFrequency.SemiMonthly:
                    date = income.Date.AddMonths(1);
                    break;
                case PaymentFrequency.Monthly:
                    date = date.AddMonths(1);
                    break;
                case PaymentFrequency.BiMonthly:
                    date = date.AddMonths(2);
                    break;
                case PaymentFrequency.SemiYearly:
                    date = date.AddMonths(6);
                    break;
                case PaymentFrequency.Yearly:
                    date = date.AddYears(1);
                    break;
            }
            return date;
        }

        public static DateTime GetNextSecondDate(this Income income, DateTime date)
        {
            if (income.PaymentFrequency == PaymentFrequency.SemiMonthly)
            {
                date = income.SecondDate.AddMonths(1);
            }
            return date;
        }
    }
}