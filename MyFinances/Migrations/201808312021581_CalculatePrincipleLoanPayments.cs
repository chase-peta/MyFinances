namespace MyFinances.Migrations
{
    using MyFinances.Models;
    using System;
    using System.Data.Entity.Migrations;
    using System.Linq;

    public partial class CalculatePrincipleLoanPayments : DbMigration
    {
        public override void Up()
        {
            ApplicationDbContext db = new ApplicationDbContext();
            foreach (Loan loan in db.Loans.Include("LoanPayments"))
            {
                decimal principle = loan.LoanAmount;
                foreach (LoanPayment lp in loan.LoanPayments.OrderBy(x => x.DatePaid))
                {
                    principle -= lp.Base + lp.Additional;
                    lp.Principal = principle;
                }
            }
            db.SaveChanges();
        }

        public override void Down()
        {
            ApplicationDbContext db = new ApplicationDbContext();
            foreach (LoanPayment lp in db.LoanPayments)
            {
                lp.Principal = 0.0M;
            }
            db.SaveChanges();
        }
    }
}
