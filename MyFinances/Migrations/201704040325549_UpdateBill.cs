namespace MyFinances.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    using MyFinances.Models;
    
    public partial class UpdateBill : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Bills", "IsDueDateStaysSame", c => c.Boolean(nullable: false, defaultValue: true));
            AddColumn("dbo.Bills", "IsAmountStaysSame", c => c.Boolean(nullable: false, defaultValue: true));
            AddColumn("dbo.Bills", "PaymentFrequency", c => c.Int(nullable: false, defaultValue: 3));
            DropColumn("dbo.Bills", "IsStaysSame");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Bills", "IsStaysSame", c => c.Boolean(nullable: false));
            DropColumn("dbo.Bills", "PaymentFrequency");
            DropColumn("dbo.Bills", "IsAmountStaysSame");
            DropColumn("dbo.Bills", "IsDueDateStaysSame");
        }
    }
}
