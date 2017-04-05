﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace MyFinances.Models
{


    public class Loan : BaseFinances
    {
        [Required]
        public string Name { get; set; }

        [Display(Name = "Loan Amount"), Column(TypeName = "money"), DataType(DataType.Currency), Required]
        public decimal LoanAmount { get; set; }

        [Display(Name = "Interest Rate"), DisplayFormat(DataFormatString = "{0:0.0####}", ApplyFormatInEditMode = true), Required]
        public decimal InterestRate { get; set; }

        [Display(Name = "First Payment Date"), DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true), Required]
        public DateTime FirstPaymentDate { get; set; }

        [Column(TypeName = "money"), DataType(DataType.Currency), Required]
        public decimal Additional { get; set; }

        [Column(TypeName = "money"), DataType(DataType.Currency), Required]
        public decimal Escrow { get; set; }

        [Required, DisplayFormat(DataFormatString = "{0} Months")]
        public int Term { get; set; }

        [Display(Name = "Payment Interest Rate"), DisplayFormat(DataFormatString = "{0:0.0####}", ApplyFormatInEditMode = true), Required]
        public decimal PaymentInterestRate { get; set; }

        [Display(Name = "Interest Compound"), Required]
        public InterestCompound InterestCompound { get; set; }

        public virtual ICollection<LoanPayment> LoanPayments { get; set; }

        [NotMapped, DisplayFormat(DataFormatString = "{0:c}")]
        public decimal Principal { get; set; }

        [NotMapped, Display(Name = "Base Payment"), DisplayFormat(DataFormatString = "{0:c}")]
        public decimal BasePayment { get; set; }

        [NotMapped, Display(Name = "Monthly Payment"), DisplayFormat(DataFormatString = "{0:c}")]
        public decimal MonthlyPayment { get; set; }

        [NotMapped, Display(Name = "Due Date"), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime DueDate { get; set; }

        [NotMapped, Display(Name = "Last Payment Date"), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime LastPaymentDate { get; set; }

        [NotMapped, Display(Name = "Last Payment Date"), DisplayFormat(DataFormatString = "{0:c}")]
        public decimal LastPaymentAmount { get; set; }

        [NotMapped, Display(Name = "Payments Remaining"), DisplayFormat(DataFormatString = "{0} Months")]
        public int PaymentsRemaining { get; set; }

        [NotMapped]
        public LoanOutlook NextPayment { get; set; }

        [NotMapped]
        public int PaymentMinYear { get; set; }

        [NotMapped]
        public int PaymentMaxYear { get; set; }

        [NotMapped]
        public int OutlookMinYear { get; set; }

        [NotMapped]
        public int OutlookMaxYear { get; set; }

        [NotMapped]
        public ICollection<LoanOutlook> LoanOutlook { get; set; }
    }

    public static class LoanHelpers
    {
        public static List<Loan> Populate (this List<Loan> loans)
        {
            List<Loan> newLoans = new List<Loan>();
            foreach (Loan loan in loans)
            {
                newLoans.Add(loan.Populate());
            }
            return newLoans;
        }

        public static Loan Populate (this Loan loan)
        {
            loan.DueDate = loan.FirstPaymentDate;
            loan.BasePayment = loan.CalculateMonthlyPayment();
            loan.MonthlyPayment = loan.BasePayment + loan.Additional + loan.Escrow;
            loan.Principal = loan.LoanAmount;
            DateTime lastPaymentDate = loan.FirstPaymentDate.AddMonths(-1);

            if (loan.LoanPayments != null && loan.LoanPayments.Any())
            {
                foreach(LoanPayment lp in loan.LoanPayments.OrderBy(x => x.DatePaid))
                {
                    loan.Principal -= lp.Base + lp.Additional;
                    lp.Principal = loan.Principal;
                }
                loan.PaymentMinYear = loan.LoanPayments.Min(x => x.DatePaid.Year);
                loan.PaymentMaxYear = loan.LoanPayments.Max(x => x.DatePaid.Year);

                LoanPayment lastPayment = loan.LoanPayments.OrderBy(x => x.DatePaid).LastOrDefault();
                if (lastPayment != null)
                {
                    lastPaymentDate = lastPayment.DatePaid;
                    loan.DueDate = lastPayment.DatePaid.AddMonths(1);
                    loan.LastPaymentDate = lastPayment.DatePaid;
                    loan.LastPaymentAmount = lastPayment.Additional + lastPayment.Base + lastPayment.Escrow + lastPayment.Interest;
                }
            }

            loan.LoanOutlook = loan.GetLoanOutlook(lastPaymentDate);
            if (loan.LoanOutlook.Any()) {
                loan.PaymentsRemaining = loan.LoanOutlook.Count();
                loan.OutlookMinYear = loan.LoanOutlook.Min(x => x.Date.Year);
                loan.OutlookMaxYear = loan.LoanOutlook.Max(x => x.Date.Year);
                loan.NextPayment = loan.LoanOutlook.FirstOrDefault();
            }

            double dueInDays = (loan.DueDate - new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day)).TotalDays;
            bool isPastDue = dueInDays < 0;

            loan.DueIn = GetDueIn(loan.IsActive, isPastDue, dueInDays);
            loan.Classes = GetClasses(loan.IsActive, isPastDue, dueInDays);
            return loan;
        }

        public static Loan MapSubmit (this Loan loan, Loan l)
        {
            loan.Name = l.Name;
            loan.LoanAmount = l.LoanAmount;
            loan.InterestRate = l.InterestRate;
            loan.FirstPaymentDate = l.FirstPaymentDate;
            loan.Additional = l.Additional;
            loan.Escrow = l.Escrow;
            loan.Term = l.Term;
            loan.PaymentInterestRate = l.PaymentInterestRate;
            loan.InterestCompound = l.InterestCompound;
            return loan;
        }

        private static decimal CalculateMonthlyPayment(this Loan loan)
        {
            double mp = 0.0;

            if (loan.PaymentInterestRate > 0 || loan.InterestRate > 0)
            {
                double rate = 0.0;
                if (loan.PaymentInterestRate > 0)
                {
                    rate = (Convert.ToDouble(loan.PaymentInterestRate) / 100) / 12;
                }
                else
                {
                    rate = (Convert.ToDouble(loan.InterestRate) / 100) / 12;
                }
                double factor = (rate + (rate / (Math.Pow(rate + 1, loan.Term) - 1)));
                mp = Convert.ToDouble(loan.LoanAmount) * factor;
            }
            else
            {
                mp = Convert.ToDouble(loan.LoanAmount) / Convert.ToDouble(loan.Term);
            }
            if (mp > 0)
            {
                return Convert.ToDecimal(Math.Ceiling(mp * 100) / 100);
            }
            return 0.0M;
        }

        private static ICollection<LoanOutlook> GetLoanOutlook(this Loan loan, DateTime lastPaymentDate)
        {
            List<LoanOutlook> outlook = new List<LoanOutlook>();
            double principal = Convert.ToDouble(loan.Principal);
            DateTime date = lastPaymentDate;

            double inte = Convert.ToDouble(loan.InterestRate);
            if (loan.InterestCompound == InterestCompound.Monthly)
            {
                inte = (inte / 100 / 12);
            }
            else if (loan.InterestCompound == InterestCompound.Daily)
            {
                inte = (inte / 100 / (DateTime.IsLeapYear(lastPaymentDate.Year) ? 366 : 365));
            }

            while (principal > 0.00)
            {
                if (loan.InterestCompound == InterestCompound.Monthly)
                {
                    date = date.AddMonths(1);
                    double interest = Math.Round(inte * Convert.ToDouble(principal), 2);

                    double baseAmount = Convert.ToDouble(loan.BasePayment) - interest;
                    baseAmount = (baseAmount > principal) ? principal : baseAmount;
                    principal -= baseAmount;

                    double add = Convert.ToDouble(loan.Additional);
                    add = (principal - add > 0) ? add : principal;
                    principal -= add;

                    if (principal <= 10)
                    {
                        add += principal;
                        principal = 0.00;
                    }

                    LoanOutlook item = new LoanOutlook(date, Convert.ToDecimal(interest), Convert.ToDecimal(baseAmount), Convert.ToDecimal(add), loan.Escrow, Convert.ToDecimal(principal));
                    outlook.Add(item);
                }
                else
                {
                    DateTime lastDate = date;
                    date = date.AddMonths(1);
                    date = new DateTime(date.Year, date.Month, loan.FirstPaymentDate.Day);
                    if (date.DayOfWeek == DayOfWeek.Saturday)
                    {
                        date = date.AddDays(2);
                    }
                    else if (date.DayOfWeek == DayOfWeek.Sunday)
                    {
                        date = date.AddDays(1);
                    }
                    double interest = 0.0;
                    if (lastDate.Year == date.Year)
                    {
                        interest = inte * Convert.ToDouble(principal) * ((date - lastDate).TotalDays);
                    }
                    else
                    {
                        interest = inte * Convert.ToDouble(principal) * ((new DateTime(lastDate.Year, 12, 31) - lastDate).TotalDays + 0.5);
                        inte = (Convert.ToDouble(loan.InterestRate) / 100 / (DateTime.IsLeapYear(date.Year) ? 366 : 365));
                        interest += inte * Convert.ToDouble(principal) * ((date - new DateTime(date.Year, 1, 1)).TotalDays + 0.5);
                    }
                    interest = Math.Floor(interest * 100) / 100;

                    double baseAmount = Convert.ToDouble(loan.BasePayment) - interest;
                    baseAmount = (baseAmount > principal) ? principal : baseAmount;
                    principal -= baseAmount;

                    double add = Convert.ToDouble(loan.Additional);
                    add = (principal - add > 0) ? add : principal;
                    principal -= add;

                    if (principal <= 10)
                    {
                        add += principal;
                        principal = 0.00;
                    }

                    LoanOutlook item = new LoanOutlook(date, Convert.ToDecimal(interest), Convert.ToDecimal(baseAmount), Convert.ToDecimal(add), loan.Escrow, Convert.ToDecimal(principal));
                    outlook.Add(item);
                }
            }

            return outlook.OrderBy(x => x.Date).ToList();
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

    public class LoanOutlook
    {
        public LoanOutlook (DateTime date, decimal interest, decimal baseA, decimal additional, decimal escrow, decimal principal)
        {
            Date = date;
            Interest = interest;
            Base = baseA;
            Additional = additional;
            Escrow = escrow;
            Principal = principal;
        }

        [Display(Name = "Date"), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime Date { get; set; }

        [Display(Name = "Interest"), DisplayFormat(DataFormatString = "{0:c}")]
        public decimal Interest { get; set; }

        [Display(Name = "Base"), DisplayFormat(DataFormatString = "{0:c}")]
        public decimal Base { get; set; }

        [Display(Name = "Additional"), DisplayFormat(DataFormatString = "{0:c}")]
        public decimal Additional { get; set; }

        [Display(Name = "Escrow"), DisplayFormat(DataFormatString = "{0:c}")]
        public decimal Escrow { get; set; }

        [Display(Name = "Payment"), DisplayFormat(DataFormatString = "{0:c}")]
        public decimal Payment { get { return Interest + Base + Additional + Escrow; } }

        [Display(Name = "Principal"), DisplayFormat(DataFormatString = "{0:c}")]
        public decimal Principal { get; set; }
    }

    public class LoanPayment : BaseFinances
    {
        [Column(TypeName = "money"), DataType(DataType.Currency), Required]
        public decimal Additional { get; set; }

        [Column(TypeName = "money"), DataType(DataType.Currency), Required]
        public decimal Base { get; set; }

        [Display(Name = "Date Paid"), DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true), Required]
        public DateTime DatePaid { get; set; }

        [Column(TypeName = "money"), DataType(DataType.Currency), Required]
        public decimal Escrow { get; set; }

        [Column(TypeName = "money"), DataType(DataType.Currency), Required]
        public decimal Interest { get; set; }

        public virtual Loan Loan { get; set; }

        [NotMapped, DisplayFormat(DataFormatString = "{0:c}")]
        public decimal Principal { get; set; }

        [NotMapped, DisplayFormat(DataFormatString = "{0:c}")]
        public decimal Payment { get { return Additional + Base + Escrow + Interest; } }
    }
}