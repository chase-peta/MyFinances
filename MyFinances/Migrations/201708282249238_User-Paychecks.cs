namespace MyFinances.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UserPaychecks : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "FirstPaycheck", c => c.Decimal(nullable: false, storeType: "money"));
            AddColumn("dbo.AspNetUsers", "SecondPaycheck", c => c.Decimal(nullable: false, storeType: "money"));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "SecondPaycheck");
            DropColumn("dbo.AspNetUsers", "FirstPaycheck");
        }
    }
}
