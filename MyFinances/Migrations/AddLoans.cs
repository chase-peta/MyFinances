namespace MyFinances.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLoans : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.LoanPayments",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Additional = c.Decimal(nullable: false, storeType: "money"),
                        Base = c.Decimal(nullable: false, storeType: "money"),
                        DatePaid = c.DateTime(nullable: false),
                        Escrow = c.Decimal(nullable: false, storeType: "money"),
                        Interest = c.Decimal(nullable: false, storeType: "money"),
                        Version = c.Int(nullable: false),
                        CreationDate = c.DateTime(nullable: false),
                        ModifyDate = c.DateTime(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        UserId = c.String(),
                        Loan_ID = c.Int(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Loans", t => t.Loan_ID)
                .Index(t => t.Loan_ID);
            
            CreateTable(
                "dbo.Loans",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        LoanAmount = c.Decimal(nullable: false, storeType: "money"),
                        InterestRate = c.Decimal(nullable: false, precision: 18, scale: 5),
                        FirstPaymentDate = c.DateTime(nullable: false),
                        Additional = c.Decimal(nullable: false, storeType: "money"),
                        Escrow = c.Decimal(nullable: false, storeType: "money"),
                        Term = c.Int(nullable: false),
                        PaymentInterestRate = c.Decimal(nullable: false, precision: 18, scale: 5),
                        InterestCompound = c.Int(nullable: false),
                        Version = c.Int(nullable: false),
                        CreationDate = c.DateTime(nullable: false),
                        ModifyDate = c.DateTime(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        UserId = c.String(),
                    })
                .PrimaryKey(t => t.ID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.LoanPayments", "Loan_ID", "dbo.Loans");
            DropIndex("dbo.LoanPayments", new[] { "Loan_ID" });
            DropTable("dbo.Loans");
            DropTable("dbo.LoanPayments");
        }
    }
}
