namespace MyFinances.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UserObject : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.BillPayments", "User_Id", c => c.String(maxLength: 128));
            AddColumn("dbo.Bills", "User_Id", c => c.String(maxLength: 128));
            AddColumn("dbo.LoanPayments", "User_Id", c => c.String(maxLength: 128));
            AddColumn("dbo.Loans", "User_Id", c => c.String(maxLength: 128));
            CreateIndex("dbo.BillPayments", "User_Id");
            CreateIndex("dbo.Bills", "User_Id");
            CreateIndex("dbo.LoanPayments", "User_Id");
            CreateIndex("dbo.Loans", "User_Id");
            AddForeignKey("dbo.Bills", "User_Id", "dbo.AspNetUsers", "Id");
            AddForeignKey("dbo.BillPayments", "User_Id", "dbo.AspNetUsers", "Id");
            AddForeignKey("dbo.Loans", "User_Id", "dbo.AspNetUsers", "Id");
            AddForeignKey("dbo.LoanPayments", "User_Id", "dbo.AspNetUsers", "Id");
            Sql("UPDATE dbo.BillPayments SET User_Id = UserId");
            Sql("UPDATE dbo.Bills SET User_Id = UserId");
            Sql("UPDATE dbo.LoanPayments SET User_Id = UserId");
            Sql("UPDATE dbo.Loans SET User_Id = UserId");
            DropColumn("dbo.BillPayments", "UserId");
            DropColumn("dbo.Bills", "UserId");
            DropColumn("dbo.LoanPayments", "UserId");
            DropColumn("dbo.Loans", "UserId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Loans", "UserId", c => c.String());
            AddColumn("dbo.LoanPayments", "UserId", c => c.String());
            AddColumn("dbo.Bills", "UserId", c => c.String());
            AddColumn("dbo.BillPayments", "UserId", c => c.String());
            Sql("UPDATE dbo.BillPayments SET UserId = User_Id");
            Sql("UPDATE dbo.Bills SET UserId = User_Id");
            Sql("UPDATE dbo.LoanPayments SET UserId = User_Id");
            Sql("UPDATE dbo.Loans SET UserId = User_Id");
            DropForeignKey("dbo.LoanPayments", "User_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.Loans", "User_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.BillPayments", "User_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.Bills", "User_Id", "dbo.AspNetUsers");
            DropIndex("dbo.Loans", new[] { "User_Id" });
            DropIndex("dbo.LoanPayments", new[] { "User_Id" });
            DropIndex("dbo.Bills", new[] { "User_Id" });
            DropIndex("dbo.BillPayments", new[] { "User_Id" });
            DropColumn("dbo.Loans", "User_Id");
            DropColumn("dbo.LoanPayments", "User_Id");
            DropColumn("dbo.Bills", "User_Id");
            DropColumn("dbo.BillPayments", "User_Id");
        }
    }
}
