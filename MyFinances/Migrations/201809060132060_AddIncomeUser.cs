namespace MyFinances.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddIncomeUser : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.IncomePayments", "PaidFrom_Id", "dbo.AspNetUsers");
            DropIndex("dbo.IncomePayments", new[] { "PaidFrom_Id" });
            CreateTable(
                "dbo.IncomeUserPayments",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Amount = c.Decimal(nullable: false, storeType: "money"),
                        Date = c.DateTime(nullable: false),
                        Version = c.Int(nullable: false),
                        CreationDate = c.DateTime(nullable: false),
                        ModifyDate = c.DateTime(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        IncomeUser_ID = c.Int(),
                        User_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.IncomeUsers", t => t.IncomeUser_ID)
                .ForeignKey("dbo.AspNetUsers", t => t.User_Id)
                .Index(t => t.IncomeUser_ID)
                .Index(t => t.User_Id);
            
            CreateTable(
                "dbo.IncomeUsers",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        PaymentFrequency = c.Int(nullable: false),
                        Date = c.DateTime(nullable: false),
                        Version = c.Int(nullable: false),
                        CreationDate = c.DateTime(nullable: false),
                        ModifyDate = c.DateTime(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        PayeeUser_Id = c.String(maxLength: 128),
                        User_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.AspNetUsers", t => t.PayeeUser_Id)
                .ForeignKey("dbo.AspNetUsers", t => t.User_Id)
                .Index(t => t.PayeeUser_Id)
                .Index(t => t.User_Id);
            
            DropColumn("dbo.IncomePayments", "PaidFrom_Id");
        }
        
        public override void Down()
        {
            AddColumn("dbo.IncomePayments", "PaidFrom_Id", c => c.String(maxLength: 128));
            DropForeignKey("dbo.IncomeUserPayments", "User_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.IncomeUsers", "User_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.IncomeUsers", "PayeeUser_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.IncomeUserPayments", "IncomeUser_ID", "dbo.IncomeUsers");
            DropIndex("dbo.IncomeUsers", new[] { "User_Id" });
            DropIndex("dbo.IncomeUsers", new[] { "PayeeUser_Id" });
            DropIndex("dbo.IncomeUserPayments", new[] { "User_Id" });
            DropIndex("dbo.IncomeUserPayments", new[] { "IncomeUser_ID" });
            DropTable("dbo.IncomeUsers");
            DropTable("dbo.IncomeUserPayments");
            CreateIndex("dbo.IncomePayments", "PaidFrom_Id");
            AddForeignKey("dbo.IncomePayments", "PaidFrom_Id", "dbo.AspNetUsers", "Id");
        }
    }
}
