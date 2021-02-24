using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using BankAccounts.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace BankAccounts.Controllers
{
    public class HomeController : Controller
    {
        private MyContext dbContext;
        public HomeController(MyContext context)
        {
            dbContext = context;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("register")]
        public IActionResult Register(User newUser)
        {
            if (ModelState.IsValid)
            {
                // If a User exists with provided email
                if (dbContext.Users.Any(u => u.Email == newUser.Email))
                {
                    // Manually add a ModelState error to the Email field
                    ModelState.AddModelError("Email", "Email already in use!");
                    return View("Index");
                }
                // Initializing a PasswordHasher object, providing our User class as its type
                PasswordHasher<User> Hasher = new PasswordHasher<User>();
                newUser.Password = Hasher.HashPassword(newUser, newUser.Password);
                dbContext.Users.Add(newUser);
                //Save your user object to the database
                dbContext.SaveChanges();
                // creating a session to stay logged in
                HttpContext.Session.SetInt32("UserId", newUser.UserId);
                return RedirectToAction("Account");
            }
            else
            {
                return View("Index");
            }
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            return View("Login");
        }
        [HttpPost("login")]
        public IActionResult LoginProcess(LoginUser userSubmission)
        {
            if (ModelState.IsValid)
            {
                // If initial ModelState is valid, query for a user with provided email
                var userInDb = dbContext.Users.FirstOrDefault(u => u.Email == userSubmission.Email);
                if (userInDb == null)
                {
                    ModelState.AddModelError("Email", "Invalid Email");
                    return View("Login");
                }
                // Initialize hasher object
                var hasher = new PasswordHasher<LoginUser>();
                // verify provided password against hash stored in db

                var result = hasher.VerifyHashedPassword(userSubmission, userInDb.Password, userSubmission.Password);
                // result can be compared to 0 for failure (1 for success)
                if (result == 0)
                {
                    ModelState.AddModelError("Password", "Invalid Password");
                    return View("Login");
                }
                // if the password matches, store the UserId in session
                HttpContext.Session.SetInt32("UserId", userInDb.UserId);
                return RedirectToAction("Account");
            }
            return View("Login");
        }
        [HttpGet("account")]
        public IActionResult Account()
        {
            // int? CurrentUserId = HttpContext.Session.GetInt32("UserId");

            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return View("Index");
            }
            int CurrentUserId = (int)HttpContext.Session.GetInt32("UserId");

            ViewBag.CurrentUser = dbContext.Users.FirstOrDefault(a => a.UserId == CurrentUserId);
            // ViewBag.CurrentUser = CurrentUser;

            List<Transaction> CurrentUserTransaction = dbContext.Transactions
                .Where(curr => curr.UserId == CurrentUserId)
                .OrderByDescending(c => c.CreatedAt)
                .ToList();
            ViewBag.CurrentUserTransaction = CurrentUserTransaction;

            // to display current balance (use .Sum and then .ToString to display)
            decimal Balance = CurrentUserTransaction.Sum(a => a.Amount);
            ViewBag.Balance = Balance;
            // ViewBag.Balance = Balance.ToString("0.00");

            return View("Account");
        }

        [HttpPost("Transaction")]
        public IActionResult Transaction(Transaction transaction)
        {
            Console.WriteLine("***Inside the transaction method");
            int CurrentUserId = (int)HttpContext.Session.GetInt32("UserId");

            User CurrentUser = dbContext.Users.FirstOrDefault(a => a.UserId == CurrentUserId);
            ViewBag.CurrentUser = CurrentUser;

            List<Transaction> CurrentUserTransaction = dbContext.Transactions
                .Where(curr => curr.UserId == CurrentUserId)
                .OrderByDescending(c => c.CreatedAt)
                .ToList();
            ViewBag.CurrentUserTransaction = CurrentUserTransaction;

            // to display current balance (use .Sum and then .ToString to display)
            decimal Balance = CurrentUserTransaction.Sum(a => a.Amount);
            ViewBag.Balance = Balance;
            // ViewBag.Balance = Balance.ToString("0.00");
            transaction.UserId = CurrentUserId; //adding it to the current user (or have a hidden input field in the form)
            
            if (transaction.Amount + Balance < 0)
            {
                ModelState.AddModelError("Amount", "You can't withdraw more than your current balance");
            }
            
            if (ModelState.IsValid)
            {
                // transaction.UserId = CurrentUserId; //adding it to the current user (or have a hidden input field in the form)
                dbContext.Transactions.Add(transaction);
                dbContext.SaveChanges();
                return RedirectToAction("Account");
            }
            Console.WriteLine("at the end of transaction");
            return View("Account");
        }


        [HttpGet("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }


    }
}
