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

        public bool DisplayMonth { get; set; }

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
            if (bill.IsShared && bill.SharedWith.Any())
            {
                SharedWith = bill.SharedWith.Select(x => x.SharedWithUser).ToList();
            }
            Type = "Bill";
            IsPaid = false;
        }

        public DashboardItem (BillPayment payment, ApplicationUser user)
        {
            Id = payment.Bill.ID;
            Name = payment.Bill.Name;
            Date = payment.DatePaid;
            Amount = payment.Amount;
            IsShared = payment.Bill.IsShared;
            SharedAmount = payment.SharedAmount;
            NotOwner = payment.User.Id != user.Id;
            if (payment.SharedWith.Any())
            {
                SharedWith = payment.SharedWith.Select(x => x.SharedWithUser).ToList();
            }
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

        public DashboardItem (LoanPayment payment, ApplicationUser user)
        {
            Id = payment.Loan.ID;
            Name = payment.Loan.Name;
            Date = payment.DatePaid;
            Amount = payment.Payment;
            SharedAmount = payment.SharedAmount;
            IsShared = payment.Loan.IsShared;
            NotOwner = payment.User.Id != user.Id;
            if (payment.SharedWith.Any())
            {
                SharedWith = payment.SharedWith.Select(x => x.SharedWithUser).ToList();
            }
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
            if (outlook.Loan.SharedWith.Any())
            {
                SharedWith = outlook.Loan.SharedWith.Select(x => x.SharedWithUser).ToList();
            }
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

        public List<ApplicationUser> SharedWith { get; set; }

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
                if (NotOwner)
                    return "alert-info not-owner";
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

    public class DashboardIncomeItem
    {
        public DashboardIncomeItem(DateTime date, decimal paycheck)
        {
            Name = "Paycheck";
            Date = date;
            Amount = paycheck;
        }

        public DashboardIncomeItem(string name, DateTime date, decimal amount)
        {
            Name = name;
            Date = date;
            Amount = amount;
        }

        [Display(Name = "Name")]
        public string Name { get; set; }

        [Display(Name = "Date") DisplayFormat(DataFormatString = "{0:MMM. dd}")]
        public DateTime Date { get; set; }

        [Display(Name = "Amount") DisplayFormat(DataFormatString = "{0:c}")]
        public decimal Amount { get; set; }
    }

    public class DashboardDateRange
    {
        public DashboardDateRange (DateTime startDate, DateTime endDate, decimal paycheck)
        {
            StartDate = startDate;
            EndDate = endDate;
            IncomeItems = new List<DashboardIncomeItem>();
            IncomeItems.Add(new DashboardIncomeItem(startDate, paycheck));
        }

        [Display(Name = "Start Date"), DisplayFormat(DataFormatString = "{0:MMM. d}")]
        public DateTime StartDate { get; set; }

        [Display(Name = "End Date"), DisplayFormat(DataFormatString = "{0:MMM. d}")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Total"), DisplayFormat(DataFormatString = "{0:c}")]
        public decimal Total { get { return Items.Sum(x => x.Amount); } }

        [Display(Name = "Total"), DisplayFormat(DataFormatString = "{0:c}")]
        public decimal IncomeTotal { get { return IncomeItems.Sum(x => x.Amount); } }

        [Display(Name = "Shared Total"), DisplayFormat(DataFormatString = "{0:c}")]
        public decimal NotOwnerTotal { get { return Items.Where(x => x.NotOwner).Sum(x => x.SharedAmount); } }

        [DisplayFormat(DataFormatString = "{0:c}")]
        public decimal SharedTotal { get { return Items.Sum(x => x.SharedAmount); } }
        
        public IEnumerable<DashboardItem> Items { get; set; }

        public List<DashboardIncomeItem> IncomeItems { get; set; }
    }
}