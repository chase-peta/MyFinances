namespace MyFinances.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SharedLoans : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SharedLoans",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Loan_ID = c.Int(),
                        SharedWithUser_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Loans", t => t.Loan_ID)
                .ForeignKey("dbo.AspNetUsers", t => t.SharedWithUser_Id)
                .Index(t => t.Loan_ID)
                .Index(t => t.SharedWithUser_Id);
            
            CreateTable(
                "dbo.SharedLoanPayments",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        LoanPayment_ID = c.Int(),
                        SharedWithUser_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.LoanPayments", t => t.LoanPayment_ID)
                .ForeignKey("dbo.AspNetUsers", t => t.SharedWithUser_Id)
                .Index(t => t.LoanPayment_ID)
                .Index(t => t.SharedWithUser_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.SharedLoanPayments", "SharedWithUser_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.SharedLoanPayments", "LoanPayment_ID", "dbo.LoanPayments");
            DropForeignKey("dbo.SharedLoans", "SharedWithUser_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.SharedLoans", "Loan_ID", "dbo.Loans");
            DropIndex("dbo.SharedLoanPayments", new[] { "SharedWithUser_Id" });
            DropIndex("dbo.SharedLoanPayments", new[] { "LoanPayment_ID" });
            DropIndex("dbo.SharedLoans", new[] { "SharedWithUser_Id" });
            DropIndex("dbo.SharedLoans", new[] { "Loan_ID" });
            DropTable("dbo.SharedLoanPayments");
            DropTable("dbo.SharedLoans");
        }
    }
}
