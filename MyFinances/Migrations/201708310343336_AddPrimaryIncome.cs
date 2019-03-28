namespace MyFinances.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPrimaryIncome : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PrimaryIncomes",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Income_ID = c.Int(),
                        User_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Incomes", t => t.Income_ID)
                .ForeignKey("dbo.AspNetUsers", t => t.User_Id)
                .Index(t => t.Income_ID)
                .Index(t => t.User_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PrimaryIncomes", "User_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.PrimaryIncomes", "Income_ID", "dbo.Incomes");
            DropIndex("dbo.PrimaryIncomes", new[] { "User_Id" });
            DropIndex("dbo.PrimaryIncomes", new[] { "Income_ID" });
            DropTable("dbo.PrimaryIncomes");
        }
    }
}
