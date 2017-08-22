namespace MyFinances.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class LoansIsShared : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Loans", "IsShared", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Loans", "IsShared");
        }
    }
}
