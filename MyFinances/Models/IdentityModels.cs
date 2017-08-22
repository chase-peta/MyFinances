using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace MyFinances.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }

        [Display(Name = "Paycheck Frequency")]
        public PaymentFrequency PaycheckFrequency { get; set; }

        [Display(Name = "First Name")]
        public string FirstName { get; set; }
        [Display(Name = "Last Name")]
        public string LastName { get; set; }
        
        [Display(Name = "First Date"), DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true), Required]
        public DateTime FirstDate { get; set; }
        [Display(Name = "Second Date"), DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true), Required]
        public DateTime SecondDate { get; set; }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext () : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public DbSet<Bill> Bills { get; set; }
        public DbSet<SharedBill> SharedBill { get; set; }
        public DbSet<BillPayment> BillPayments { get; set; }
        public DbSet<SharedBillPayment> SharedBillPayment { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<SharedLoan> SharedLoan { get; set;}
        public DbSet<LoanPayment> LoanPayments { get; set; }
        public DbSet<SharedLoanPayment> SharedLoanPayment { get; set; }

        protected override void OnModelCreating (DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Loan>().Property(x => x.InterestRate).HasPrecision (18, 5);
            modelBuilder.Entity<Loan>().Property(x => x.PaymentInterestRate).HasPrecision(18, 5);
            base.OnModelCreating(modelBuilder);
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        public override int SaveChanges ()
        {
            AddTimestamps();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync ()
        {
            AddTimestamps();
            return await base.SaveChangesAsync();
        }

        private void AddTimestamps ()
        {
            var baseEntities = ChangeTracker.Entries().Where(x => x.Entity is BaseFinances && (x.State == EntityState.Added || x.State == EntityState.Modified));

            if (baseEntities.Any())
            {
                foreach (var entity in baseEntities)
                {
                    if (entity.State == EntityState.Added)
                    {
                        ((BaseFinances) entity.Entity).CreationDate = DateTime.UtcNow;
                        ((BaseFinances) entity.Entity).Version = 1;
                        ((BaseFinances) entity.Entity).IsActive = true;
                    }
                    else
                    {
                        ((BaseFinances) entity.Entity).Version++;
                    }

                    ((BaseFinances) entity.Entity).ModifyDate = DateTime.UtcNow;
                }
            }

            var userEntries = ChangeTracker.Entries().Where(x => x.Entity is ApplicationUser && x.State == EntityState.Added);

            if (userEntries.Any())
            {
                foreach (var entity in userEntries)
                {
                    if (entity.State == EntityState.Added)
                    {
                        ((ApplicationUser) entity.Entity).FirstDate = DateTime.Now.AddDays(1);
                        ((ApplicationUser) entity.Entity).SecondDate = DateTime.Now.AddDays(2);
                    }
                }
            }
        }
    }

    public class IndexViewModel
    {
        public bool HasPassword { get; set; }
        public bool BrowserRemembered { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Email")]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "First Name"), Required]
        public string FirstName { get; set; }

        [Display(Name = "Last Name"), Required]
        public string LastName { get; set; }
    }

    public abstract class BaseFinances
    {
        public int ID { get; set; }
        public int Version { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ModifyDate { get; set; }
        public bool IsActive { get; set; }
        public virtual ApplicationUser User { get; set; }

        [NotMapped, Display(Name = "Due In")]
        public string DueIn { get; set; }

        [NotMapped]
        public string Classes { get; set; }
    }

    public abstract class BaseShare
    {
        public int ID { get; set; }

        public virtual ApplicationUser SharedWithUser { get; set; }
    }

    public enum PaymentFrequency
    {
        Weekly, BiWeekly, SemiMonthly, Monthly, BiMonthly, SemiYearly, Yearly
    }

    public enum InterestCompound
    {
        Monthly, Daily
    }
}