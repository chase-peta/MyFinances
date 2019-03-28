using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace MyFinances.Models
{
    public class IncomeUser : BaseFinances
    {
        public virtual ApplicationUser PayeeUser { get; set; }

        [Display(Name = "Payment Frequency"), Required]
        public PaymentFrequency PaymentFrequency { get; set; }

        [DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true), Required]
        public DateTime Date { get; set; }

        public virtual ICollection<IncomeUserPayment> IncomeUserPayments { get; set; }

        [NotMapped, Display(Name = "Last Payment Date"), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime LastPaymentDate { get; set; }

        [NotMapped, Display(Name = "Last Payment Amount"), DisplayFormat(DataFormatString = "{0:c}")]
        public decimal LastPaymentAmount { get; set; }

        [NotMapped]
        public int MinYear { get; set; }

        [NotMapped]
        public int MaxYear { get; set; }

        [NotMapped]
        public List<SharedBill> Bills { get; set; }

        [NotMapped]
        public List<SharedBillPayment> BillPayments { get; set; }

        [NotMapped]
        public List<SharedLoan> Loans { get; set; }

        [NotMapped]
        public List<SharedLoanPayment> LoanPayments { get; set; }

        [NotMapped]
        public List<NotPaidIncomeUserPayemnt> NotPaidIncomeUserPayments { get; set; }

        [NotMapped]
        public int NotPaidMinYear { get; set; }

        [NotMapped]
        public int NotPaidMaxYear { get; set; }
    }

    public static class IncomeUserHelpers
    {
        public static List<IncomeUser> Populate(this List<IncomeUser> incomeUsers, ApplicationUser user)
        {
            List<IncomeUser> newIncomeUsers = new List<IncomeUser>();
            foreach (IncomeUser incomeUser in incomeUsers)
            {
                newIncomeUsers.Add(incomeUser.Populate(user));
            }
            return newIncomeUsers;
        }

        public static IncomeUser Populate(this IncomeUser incomeUser, ApplicationUser user)
        {
            if (incomeUser.IncomeUserPayments != null && incomeUser.IncomeUserPayments.Any())
            {
                IncomeUserPayment lastPayment = incomeUser.IncomeUserPayments.OrderBy(x => x.Date).LastOrDefault();
                if (lastPayment != null)
                {
                    incomeUser.LastPaymentDate = lastPayment.Date;
                    incomeUser.LastPaymentAmount = lastPayment.Amount;
                }
                incomeUser.MinYear = incomeUser.IncomeUserPayments.Min(x => x.Date.Year);
                incomeUser.MaxYear = incomeUser.IncomeUserPayments.Max(x => x.Date.Year);
            }

            double dueInDays = (incomeUser.Date - new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day)).TotalDays;
            bool isPastDue = dueInDays < 0;
            incomeUser.DueIn = GetDueIn(incomeUser.IsActive, isPastDue, dueInDays);
            incomeUser.Classes = GetClasses(incomeUser.IsActive, isPastDue, dueInDays);

            return incomeUser;
        }

        public static IncomeUser MapSubmit(this IncomeUser incomeUser, IncomeUser i)
        {
            incomeUser.PaymentFrequency = i.PaymentFrequency;
            incomeUser.Date = i.Date;

            return incomeUser;
        }

        private static string GetDueIn(bool isActive, bool isPastDue, double dueInDays)
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

        private static string GetClasses(bool isActive, bool isPastDue, double dueInDays)
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

        public static DateTime GetNextDate(this IncomeUser incomeUser, DateTime date)
        {
            switch (incomeUser.PaymentFrequency)
            {
                case PaymentFrequency.Weekly:
                    date = date.AddDays(7);
                    break;
                case PaymentFrequency.BiWeekly:
                    date = date.AddDays(14);
                    break;
                case PaymentFrequency.SemiMonthly:
                    date = incomeUser.Date.AddMonths(1);
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
    }

    public class IncomeUserPayment : BaseFinances
    {
        [Column(TypeName = "money"), DataType(DataType.Currency), Required]
        public decimal Amount { get; set; }

        [DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true), Required]
        public DateTime Date { get; set; }

        public virtual IncomeUser IncomeUser { get; set; }

        [NotMapped, Display(Name = "Shared Total"), DataType(DataType.Currency)]
        public decimal SharedTotal { get { return BillPayments.Sum(x => x.Amount) + LoanPayments.Sum(x => x.Amount); } }

        [NotMapped]
        public List<SharedBillPayment> BillPayments { get { return (IncomeUser != null ? IncomeUser.BillPayments.Where(x => x.BillPayment.DatePaid.Year == Date.Year && x.BillPayment.DatePaid.Month == Date.Month).ToList() : new List<SharedBillPayment>()); } }

        [NotMapped]
        public List<SharedLoanPayment> LoanPayments { get { return (IncomeUser != null ? IncomeUser.LoanPayments.Where(x => x.LoanPayment.DatePaid.Year == Date.Year && x.LoanPayment.DatePaid.Month == Date.Month).ToList() : new List<SharedLoanPayment>()); } }
    }

    public class NotPaidIncomeUserPayemnt
    {
        [DataType(DataType.Currency)]
        public decimal Amount { get { return BillPayments.Sum(x => x.Amount) + LoanPayments.Sum(x => x.Amount); } }

        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime Date { get; set; }

        [NotMapped]
        public List<SharedBillPayment> BillPayments { get; set; }

        [NotMapped]
        public List<SharedLoanPayment> LoanPayments { get; set; }
    }
}