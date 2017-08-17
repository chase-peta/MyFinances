using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace MyFinances.Models
{
    public class Bill : BaseFinances
    {
        [Required]
        public string Name { get; set; }

        [Display(Name = "Due Date"), DataType(DataType.Date),  DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true), Required]
        public DateTime DueDate { get; set; }

        [Display(Name = "Due Date - Stays Same")]
        public bool IsDueDateStaysSame { get; set; }

        [Column(TypeName = "money"), DataType(DataType.Currency), Required]
        public decimal Amount { get; set; }

        [Display(Name = "Amount - Stays Same")]
        public bool IsAmountStaysSame { get; set; }

        [Required]
        public string Payee { get; set; }

        [Display(Name = "Payment Frequency"), Required]
        public PaymentFrequency PaymentFrequency { get; set; }

        [Display(Name = "Shared")]
        public bool IsShared { get; set; }

        public virtual ICollection<BillPayment> BillPayments { get; set; }

        [NotMapped]
        public virtual ICollection<BillAverage> BillAverages
        {
            get
            {
                var list = new List<BillAverage>();
                for (var i = 1; i <= 12; i++)
                {
                    list.Add(new BillAverage(this, i));
                }
                return list;
            }
        }

        [NotMapped, Display(Name = "Average"), DisplayFormat(DataFormatString = "{0:c}")]
        public decimal AveragePaid { get; set; }

        [NotMapped, Display(Name = "Min Paid"), DisplayFormat(DataFormatString = "{0:c}")]
        public decimal MinPaid { get; set; }

        [NotMapped, Display(Name = "Max Paid"), DisplayFormat(DataFormatString = "{0:c}")]
        public decimal MaxPaid { get; set; }

        [NotMapped, Display(Name = "Last Payment Date"), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime LastPaymentDate { get; set; }

        [NotMapped, Display(Name = "Last Payment Date"), DisplayFormat(DataFormatString = "{0:c}")]
        public decimal LastPaymentAmount { get; set; }

        [NotMapped]
        public int MinYear { get; set; }

        [NotMapped]
        public int MaxYear { get; set; }
    }

    public static class BillHelpers
    {
        public static List<Bill> Populate (this List<Bill> bills)
        {
            List<Bill> newBills = new List<Bill>();
            foreach (Bill bill in bills)
            {
                newBills.Add(bill.Populate());
            }
            return newBills;
        }

        public static List<DashboardItem> GetDashboardItems (this IEnumerable<Bill> bills, DashboardDateRange range, ApplicationUser user)
        {
            List<DashboardItem> items = new List<DashboardItem>();
            foreach (Bill bill in bills)
            {
                if (bill.DueDate >= range.StartDate && bill.DueDate <= range.EndDate)
                {
                    items.Add(new DashboardItem(bill));
                }

                DateTime date = bill.DueDate;
                while (date <= range.EndDate)
                {
                    Tuple<DateTime, decimal> newDateAmount = bill.GetDateAmount(bill.GetNextDate(date, user),  bill.Amount);
                    date = newDateAmount.Item1;
                    if (newDateAmount.Item1 >= range.StartDate && date <= range.EndDate)
                    {
                        DashboardItem item = new DashboardItem(bill);
                        item.Date = date;
                        item.Amount = newDateAmount.Item2;
                        items.Add(item);
                    }
                }

                if (bill.BillPayments.Any())
                {
                    items.AddRange(bill.BillPayments.Where(x => x.DatePaid >= range.StartDate && x.DatePaid <= range.EndDate).Select(x => new DashboardItem(x)));
                }
            }

            return items;
        }

        public static Bill Populate(this Bill bill)
        {
            if (bill.BillPayments != null && bill.BillPayments.Any())
            {
                BillPayment lastPayment = bill.BillPayments.OrderBy(x => x.DatePaid).LastOrDefault();
                if (lastPayment != null)
                {
                    bill.LastPaymentDate = lastPayment.DatePaid;
                    bill.LastPaymentAmount = lastPayment.Amount;
                }

                bill.AveragePaid = bill.BillPayments.Average(x => x.Amount);
                bill.MinPaid = bill.BillPayments.Min(x => x.Amount);
                bill.MaxPaid = bill.BillPayments.Max(x => x.Amount);
                bill.MinYear = bill.BillPayments.Min(x => x.DatePaid.Year);
                bill.MaxYear = bill.BillPayments.Min(x => x.DatePaid.Year);
            }

            double dueInDays = (bill.DueDate - new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day)).TotalDays;
            bool isPastDue = dueInDays < 0;
            bill.DueIn =  GetDueIn(bill.IsActive, isPastDue, dueInDays);
            bill.Classes = GetClasses(bill.IsActive, isPastDue, dueInDays);

            return bill;
        }

        public static Bill MapSubmit (this Bill bill, Bill b)
        {
            bill.Name = b.Name;
            bill.DueDate = b.DueDate;
            bill.Amount = b.Amount;
            bill.Payee = b.Payee;
            bill.IsDueDateStaysSame = b.IsDueDateStaysSame;
            bill.IsAmountStaysSame = b.IsAmountStaysSame;
            bill.IsShared = b.IsShared;
            bill.PaymentFrequency = b.PaymentFrequency;
            return bill;
        }

        public static Tuple<DateTime, decimal> GetDateAmount(this Bill bill, DateTime date, decimal amount)
        {
            if ((!bill.IsDueDateStaysSame || !bill.IsAmountStaysSame) && bill.PaymentFrequency >= PaymentFrequency.Monthly && bill.BillPayments.Any())
            {
                IEnumerable<BillPayment> bp = bill.BillPayments.Where(x => x.DatePaid.Month == date.Month);
                if (bp.Count() == 0)
                {
                    IEnumerable<BillPayment> orderBp = bill.BillPayments.OrderBy(x => x.DatePaid);
                    bp = orderBp.Skip(Math.Max(0, orderBp.Count() - 4));
                }
                if (bp.Count() > 1)
                {
                    if (!bill.IsDueDateStaysSame)
                    {
                        date = new DateTime(date.Year, date.Month, Convert.ToInt16(bp.Average(x => x.DatePaid.Day)));
                    }
                    if (!bill.IsAmountStaysSame)
                    {
                        amount = bp.Average(x => x.Amount);
                    }
                }
            }
            
            return Tuple.Create(date, amount);
        }

        public static DateTime GetNextDate (this Bill bill, DateTime date, ApplicationUser user)
        {
            switch (bill.PaymentFrequency)
            {
                case PaymentFrequency.Weekly:
                    date = date.AddDays(7);
                    break;
                case PaymentFrequency.BiWeekly:
                    date = date.AddDays(14);
                    break;
                case PaymentFrequency.SemiMonthly:
                    DateTime fDate = new DateTime(date.Year, date.Month, user.FirstDate.Day);
                    DateTime sDate = new DateTime(date.Year, date.Month, user.SecondDate.Day);
                    if (date == fDate)
                    {
                        date = sDate;
                    } else if (date == sDate)
                    {
                        date = fDate.AddMonths(1);
                    } else if (date < sDate)
                    {
                        date = sDate.AddDays((date - fDate).Days);
                    } else
                    {
                        date = fDate.AddMonths(1).AddDays((date - sDate).Days);
                    }
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

    public class BillAverage
    {
        public BillAverage(Bill bill, int month)
        {
            IsActive = false;
            if (bill.BillPayments != null)
            {
                var billPayments = bill.BillPayments.Where(x => x.DatePaid.Month == month);
                if (billPayments.Any())
                {
                    IsActive = true;
                    var day = Convert.ToInt16(billPayments.Average(x => x.DatePaid.Day));
                    Date = new DateTime(((month < bill.DueDate.Month) ? bill.DueDate.Year + 1 : bill.DueDate.Year), month, day);
                    Max = billPayments.Max(x => x.Amount);
                    Min = billPayments.Min(x => x.Amount);
                    Average = billPayments.Average(x => x.Amount);
                }
            }
        }

        [Column(TypeName = "money"), DataType(DataType.Currency)]
        public decimal Average { get; set; }

        [Column(TypeName = "money"), DataType(DataType.Currency)]
        public decimal Min { get; set; }

        [Column(TypeName = "money"), DataType(DataType.Currency)]
        public decimal Max { get; set; }

        [DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:MMMM, d}", ApplyFormatInEditMode = true)]
        public DateTime Date { get; set; }

        public bool IsActive { get; set; }
    }

    public class BillPayment : BaseFinances
    {
        [Display(Name = "Date Paid"), DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true), Required]
        public DateTime DatePaid { get; set; }

        [Column(TypeName = "money"), DataType(DataType.Currency), Required]
        public decimal Amount { get; set; }

        [Required]
        public string Payee { get; set; }

        public virtual Bill Bill { get; set; }
    }
}
 