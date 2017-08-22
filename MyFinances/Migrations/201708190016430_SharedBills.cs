namespace MyFinances.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SharedBills : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SharedBills",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Bill_ID = c.Int(),
                        SharedWithUser_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Bills", t => t.Bill_ID)
                .ForeignKey("dbo.AspNetUsers", t => t.SharedWithUser_Id)
                .Index(t => t.Bill_ID)
                .Index(t => t.SharedWithUser_Id);
            
            CreateTable(
                "dbo.SharedBillPayments",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        BillPayment_ID = c.Int(),
                        SharedWithUser_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.BillPayments", t => t.BillPayment_ID)
                .ForeignKey("dbo.AspNetUsers", t => t.SharedWithUser_Id)
                .Index(t => t.BillPayment_ID)
                .Index(t => t.SharedWithUser_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.SharedBillPayments", "SharedWithUser_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.SharedBillPayments", "BillPayment_ID", "dbo.BillPayments");
            DropForeignKey("dbo.SharedBills", "SharedWithUser_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.SharedBills", "Bill_ID", "dbo.Bills");
            DropIndex("dbo.SharedBillPayments", new[] { "SharedWithUser_Id" });
            DropIndex("dbo.SharedBillPayments", new[] { "BillPayment_ID" });
            DropIndex("dbo.SharedBills", new[] { "SharedWithUser_Id" });
            DropIndex("dbo.SharedBills", new[] { "Bill_ID" });
            DropTable("dbo.SharedBillPayments");
            DropTable("dbo.SharedBills");
        }
    }
}
