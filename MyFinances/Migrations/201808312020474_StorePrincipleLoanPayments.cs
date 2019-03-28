namespace MyFinances.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class StorePrincipleLoanPayments : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.LoanPayments", "Principal", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            DropColumn("dbo.LoanPayments", "Principal");
        }
    }
}
