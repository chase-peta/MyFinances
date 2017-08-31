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

        public ApplicationUser PaidFrom { get; set; }

        public Income Income { get; set; }
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

                if (income.PaymentFrequency == PaymentFrequency.SemiMonthly && lastPayment.Date == income.Date)
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
    }
}