using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace MyFinances.Models
{
    public class DashboardViewModel
    {
        public IEnumerable<DashboardDateRange> DateRanges { get; set; }

        public int CurrentYear { get; set; }

        public int StartYear { get; set; }

        public int EndYear { get; set; }
    }

    public class DashboardItem
    {
        public DashboardItem () { }

        public DashboardItem (Bill bill)
        {
            Id = bill.ID;
            Name = bill.Name;
            Date = bill.DueDate;
            Amount = bill.Amount;
            IsShared = bill.IsShared;
            SharedAmount = bill.SharedAmount;
            NotOwner = bill.NotOwner;
            Type = "Bill";
            IsPaid = false;
        }

        public DashboardItem (BillPayment payment)
        {
            Id = payment.Bill.ID;
            Name = payment.Bill.Name;
            Date = payment.DatePaid;
            Amount = payment.Amount;
            IsShared = payment.Bill.IsShared;
            SharedAmount = payment.SharedAmount;
            NotOwner = payment.Bill.NotOwner;
            Type = "Bill";
            IsPaid = true;
        }

        public DashboardItem (BillAverage average)
        {
            Date = average.Date;
            Amount = average.Average;
            IsShared = false;
            Type = "Bill";
            IsPaid = false;
        }

        public DashboardItem (LoanPayment payment)
        {
            Id = payment.Loan.ID;
            Name = payment.Loan.Name;
            Date = payment.DatePaid;
            Amount = payment.Payment;
            SharedAmount = payment.SharedAmount;
            IsShared = payment.Loan.IsShared;
            NotOwner = payment.Loan.NotOwner;
            Type = "Loan";
            IsPaid = true;
        }

        public DashboardItem (LoanOutlook outlook)
        {
            Id = outlook.Loan.ID;
            Name = outlook.Loan.Name;
            Date = outlook.Date;
            Amount = outlook.Payment;
            SharedAmount = outlook.SharedPayment;
            IsShared = outlook.Loan.IsShared;
            NotOwner = outlook.Loan.NotOwner;
            Type = "Loan";
            IsPaid = false;
        }

        [Display(Name = "Id")]
        public int Id { get; set; }

        [Display(Name = "Name")]
        public string Name { get; set; }

        [Display(Name = "Date"), DisplayFormat(DataFormatString = "{0:MMM. dd}")]
        public DateTime Date { get; set; }

        [Display(Name = "Amount"), DisplayFormat(DataFormatString = "{0:c}")]
        public decimal Amount { get; set; }

        [Display(Name = "You Pay"), DisplayFormat(DataFormatString = "{0:c}")]
        public decimal SharedAmount { get; set; }

        [Display(Name = "Due In"), DisplayFormat(DataFormatString = "{0} Days")]
        public double DueInDays { get { return (Date - new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day)).TotalDays; } }

        [Display(Name = "Paid")]
        public bool IsPaid { get; set; }

        public bool NotOwner { get; set; }

        [Display(Name = "Due In")]
        public string DueIn
        {
            get
            {
                if (IsPaid || (DueInDays < 0 && !IsPastDue))
                    return "Paid";
                else if (IsPastDue)
                    return "Past Due";
                else if (DueInDays == 0)
                    return "Today";
                else if (DueInDays == 1)
                    return "Tomorrow";
                else
                    return DueInDays.ToString() + " Days";
            }
        }

        public string Classes
        {
            get
            {
                if (IsPaid || (DueInDays < 0 && !IsPastDue))
                    return "alert-success";
                else if (IsPastDue)
                    return "alert-danger";
                else if (DueInDays < 5)
                    return "alert-warning";
                else
                    return "";
            }
        }

        public bool IsPastDue { get { return DueInDays < 0; } }

        public bool IsShared { get; set; }

        public string Type { get; set; }
    }

    public class DashboardDateRange
    {
        public DashboardDateRange (DateTime startDate, DateTime endDate)
        {
            StartDate = startDate;
            EndDate = endDate;
        }

        [Display(Name = "Start Date"), DisplayFormat(DataFormatString = "{0:MMM. d}")]
        public DateTime StartDate { get; set; }

        [Display(Name = "End Date"), DisplayFormat(DataFormatString = "{0:MMM. d}")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Total"), DisplayFormat(DataFormatString = "{0:c}")]
        public decimal Total
        {
            get
            {
                decimal total = Convert.ToDecimal(0.0);
                foreach (DashboardItem item in Items)
                {
                    total += item.Amount;
                }
                return total;
            }
        }

        [DisplayFormat(DataFormatString = "{0:c}")]
        public decimal SharedTotal
        {
            get
            {
                decimal sharedTotal = Convert.ToDecimal(0.0);
                foreach(DashboardItem item in Items)
                {
                    sharedTotal += item.SharedAmount;
                }
                return sharedTotal;
            }
        }

        [Display(Name = "To Pay"), DisplayFormat(DataFormatString = "{0:c}")]
        public decimal LeftToPay
        {
            get
            {
                decimal total = Convert.ToDecimal(0.0);
                foreach (DashboardItem item in Items)
                {
                    if (!item.IsPaid)
                    {
                        total += item.Amount;
                    }
                }
                return total;
            }
        }

        public IEnumerable<DashboardItem> Items { get; set; }
    }
}