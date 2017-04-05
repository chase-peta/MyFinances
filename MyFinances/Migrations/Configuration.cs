namespace MyFinances.Migrations
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using Models;

    internal sealed class Configuration : DbMigrationsConfiguration<MyFinances.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            ContextKey = "MyFinances.Models.ApplicationDbContext";
        }

        protected override void Seed(MyFinances.Models.ApplicationDbContext context)
        {

        }
    }
}
