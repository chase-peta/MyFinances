namespace MyFinances.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddIncomes : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.IncomePayments",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Amount = c.Decimal(nullable: false, storeType: "money"),
                        Date = c.DateTime(nullable: false),
                        Version = c.Int(nullable: false),
                        CreationDate = c.DateTime(nullable: false),
                        ModifyDate = c.DateTime(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        Income_ID = c.Int(),
                        PaidFrom_Id = c.String(maxLength: 128),
                        User_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Incomes", t => t.Income_ID)
                .ForeignKey("dbo.AspNetUsers", t => t.PaidFrom_Id)
                .ForeignKey("dbo.AspNetUsers", t => t.User_Id)
                .Index(t => t.Income_ID)
                .Index(t => t.PaidFrom_Id)
                .Index(t => t.User_Id);
            
            CreateTable(
                "dbo.Incomes",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        Amount = c.Decimal(nullable: false, storeType: "money"),
                        SecondAmount = c.Decimal(nullable: false, storeType: "money"),
                        PaymentFrequency = c.Int(nullable: false),
                        Date = c.DateTime(nullable: false),
                        SecondDate = c.DateTime(nullable: false),
                        Version = c.Int(nullable: false),
                        CreationDate = c.DateTime(nullable: false),
                        ModifyDate = c.DateTime(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        User_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.AspNetUsers", t => t.User_Id)
                .Index(t => t.User_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.IncomePayments", "User_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.IncomePayments", "PaidFrom_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.Incomes", "User_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.IncomePayments", "Income_ID", "dbo.Incomes");
            DropIndex("dbo.Incomes", new[] { "User_Id" });
            DropIndex("dbo.IncomePayments", new[] { "User_Id" });
            DropIndex("dbo.IncomePayments", new[] { "PaidFrom_Id" });
            DropIndex("dbo.IncomePayments", new[] { "Income_ID" });
            DropTable("dbo.Incomes");
            DropTable("dbo.IncomePayments");
        }
    }
}
