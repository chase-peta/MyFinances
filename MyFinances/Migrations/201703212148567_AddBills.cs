namespace MyFinances.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddBills : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.BillPayments",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        DatePaid = c.DateTime(nullable: false),
                        Amount = c.Decimal(nullable: false, storeType: "money"),
                        Payee = c.String(nullable: false),
                        Version = c.Int(nullable: false),
                        CreationDate = c.DateTime(nullable: false),
                        ModifyDate = c.DateTime(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        UserId = c.String(),
                        Bill_ID = c.Int(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Bills", t => t.Bill_ID)
                .Index(t => t.Bill_ID);
            
            CreateTable(
                "dbo.Bills",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        DueDate = c.DateTime(nullable: false),
                        Amount = c.Decimal(nullable: false, storeType: "money"),
                        Payee = c.String(nullable: false),
                        IsShared = c.Boolean(nullable: false),
                        IsStaysSame = c.Boolean(nullable: false),
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
            DropForeignKey("dbo.BillPayments", "Bill_ID", "dbo.Bills");
            DropIndex("dbo.BillPayments", new[] { "Bill_ID" });
            DropTable("dbo.Bills");
            DropTable("dbo.BillPayments");
        }
    }
}
