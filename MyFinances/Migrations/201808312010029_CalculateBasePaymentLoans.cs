namespace MyFinances.Migrations
{
    using MyFinances.Models;
    using System;
    using System.Data.Entity.Migrations;
    using System.Linq;

    public partial class CalculateBasePaymentLoans : DbMigration
    {
        public override void Up()
        {
            ApplicationDbContext db = new ApplicationDbContext();
            foreach (Loan loan in db.Loans.Where(x => x.BasePayment == 0.0M))
            {
                loan.BasePayment = loan.CalculateMonthlyPayment();
            }
            db.SaveChanges();
        }
        
        public override void Down()
        {
            ApplicationDbContext db = new ApplicationDbContext();
            foreach (Loan loan in db.Loans)
            {
                loan.BasePayment = 0.0M;
            }
            db.SaveChanges();
        }
    }
}
