namespace MyFinances.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SharedPercentage : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.SharedBills", "SharedPercentage", c => c.Int(nullable: false));
            AddColumn("dbo.SharedBillPayments", "SharedPercentage", c => c.Int(nullable: false));
            AddColumn("dbo.SharedLoans", "SharedPercentage", c => c.Int(nullable: false));
            AddColumn("dbo.SharedLoanPayments", "SharedPercentage", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.SharedLoanPayments", "SharedPercentage");
            DropColumn("dbo.SharedLoans", "SharedPercentage");
            DropColumn("dbo.SharedBillPayments", "SharedPercentage");
            DropColumn("dbo.SharedBills", "SharedPercentage");
        }
    }
}
