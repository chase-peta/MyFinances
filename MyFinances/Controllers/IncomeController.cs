using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MyFinances.Models;

namespace MyFinances.Controllers
{
    [Authorize]
    public class IncomeController : BaseController
    {
        public ActionResult Index(bool showInactive = false)
        {
            ViewBag.ShowInactive = showInactive;
            ViewBag.PrimaryIncome = GetPrimaryIncome();
            return View(GetIncomes(showInactive));
        }

        public ActionResult Details (int? id)
        {
            ViewBag.Action = "Details";
            Income income = GetIncome(id);
            if (income == null)
            {
                return HttpNotFound();
            }
            return View(income);
        }

        public ActionResult Create ()
        {
            ViewBag.Action = "Create";
            Income income = new Income()
            {
                PaymentFrequency = PaymentFrequency.Monthly,
                Date = DateTime.Now,
                SecondDate = DateTime.Now.AddMonths(1)
            };
            return View("Details", income);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create ([Bind(Include = "Name,PaymentFrequency,Amount,SecondAmount,Date,SecondDate")] Income income)
        {
            ViewBag.Action = "Create";
            if (ModelState.IsValid)
            {
                income.User = db.Users.Find(user.Id);
                db.Incomes.Add(income);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View("Details", income);
        }

        public ActionResult Edit (int? id)
        {
            ViewBag.Action = "Edit";
            Income income = GetIncome(id);
            if (income == null)
            {
                return HttpNotFound();
            }
            return View("Details", income);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit ([Bind(Include = "ID,Name,PaymentFrequency,Amount,SecondAmount,Date,SecondDate")] Income subIncome)
        {
            ViewBag.Action = "Edit";
            Income income = db.Incomes.Find(subIncome.ID);
            if (income == null)
            {
                return HttpNotFound();
            }
            income = income.MapSubmit(subIncome);
            if (ModelState.IsValid)
            {
                db.Entry(income).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", new { id = income.ID });
            }
            return View("Details", income);
        }

        public ActionResult Deactivate (int? id)
        {
            Income income = GetIncome(id);
            if (income == null)
            {
                return HttpNotFound();
            }
            income.IsActive = false;
            db.Entry(income).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Activate (int? id)
        {
            Income income = GetIncome(id);
            if (income == null)
            {
                return HttpNotFound();
            }
            income.IsActive = true;
            db.Entry(income).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Details", new { id = income.ID });
        }

        public ActionResult Delete (int? id)
        {
            Income income = GetIncome(id);
            if (income == null)
            {
                return HttpNotFound();
            }
            if (income.IncomePayments.Any())
            {
                db.IncomePayments.RemoveRange(income.IncomePayments);
            }
            db.Incomes.Remove(income);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult ChoosePrimaryIncome ()
        {
            ViewBag.Action = "Choose Primary Income";
            ViewBag.Incomes = GetIncomes(false);
            return View(GetPrimaryIncome());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChoosePrimaryIncome(int? incomeId)
        {
            ViewBag.Action = "Choose Primary Income";
            PrimaryIncome primaryIncome = GetPrimaryIncome();
            if (ModelState.IsValid && incomeId != null)
            {
                bool newEntry = (primaryIncome.ID == 0);
                primaryIncome.Income = db.Incomes.Find(incomeId);
                primaryIncome.User = db.Users.Find(user.Id);
                if (newEntry)
                {
                    db.PrimaryIncome.Add(primaryIncome);
                } else
                {
                    db.Entry(primaryIncome).State = EntityState.Modified;
                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Incomes = GetIncomes(false);
            return View(GetPrimaryIncome());
        }

        public ActionResult AddPayment (int? id)
        {
            ViewBag.Action = "Add Payment";
            Income income = GetIncome(id);
            if (income == null)
            {
                return HttpNotFound();
            }

            IncomePayment payment = new IncomePayment();
            if (income.UseSecond)
            {
                payment.Amount = income.SecondAmount;
                payment.Date = income.SecondDate;
            }
            else
            {
                payment.Amount = income.Amount;
                payment.Date = income.Date;
            }
            payment.Income = income;
            
            return View("Payment", payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddPayment (int? id, [Bind(Include = "Amount,Date")] IncomePayment incomePayment)
        {
            ViewBag.Action = "Add Payment";
            incomePayment.Income = GetIncome(id);
            if (incomePayment == null)
            {
                return HttpNotFound();
            }
            if (ModelState.IsValid)
            {
                incomePayment.User = db.Users.Find(user.Id);
                db.IncomePayments.Add(incomePayment);
                db.Entry(SaveIncomeForNextPayment(incomePayment)).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", new { id = incomePayment.Income.ID });
            }
            return View("Payment", incomePayment);
        }

        public ActionResult EditPayment (int? id)
        {
            ViewBag.Action = "Edit Payment";
            IncomePayment incomePayment = GetIncomePayment(id);
            if (incomePayment == null)
            {
                return HttpNotFound();
            }
            return View("Payment", incomePayment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPayment (int id, [Bind(Include = "Amount,Date")] IncomePayment incomePayment)
        {
            ViewBag.Action = "Edit Payment";
            IncomePayment ip = GetIncomePayment(id);
            if (ip == null)
            {
                return HttpNotFound();
            }
            if (ModelState.IsValid)
            {
                ip.Amount = incomePayment.Amount;
                ip.Date = incomePayment.Date;

                db.Entry(ip).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", new { id = ip.Income.ID });
            }
            return View("Payment", incomePayment);
        }

        public ActionResult DeletePayment (int? id)
        {
            IncomePayment incomePayment = GetIncomePayment(id);
            if (incomePayment == null)
            {
                return HttpNotFound();
            }
            Income income = incomePayment.Income;
            db.IncomePayments.Remove(incomePayment);
            db.SaveChanges();
            return RedirectToAction("Details", new { id = income.ID });
        }

        private Income SaveIncomeForNextPayment (IncomePayment incomePayment)
        {
            Income income = incomePayment.Income.Populate(user);
            if (income.Date == incomePayment.Date)
            {
                income.Date = income.GetNextDate(incomePayment.Date);
            }
            else if (income.SecondDate == incomePayment.Date)
            {
                income.SecondDate = income.GetNextSecondDate(incomePayment.Date);
            }

            return income;
        }
    }
}