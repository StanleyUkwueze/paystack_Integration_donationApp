using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PaymentGateway.Models;
using PaymentGateway.Repository;
using PayStack.Net;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PaymentGateway.Controllers
{
    public class DonateController : Controller
    {
        private readonly IConfiguration _config;
        private readonly string token;
        private PayStackApi Paystack { get; set; }

        private readonly AppDbContext _cxt;

        public DonateController(IConfiguration config, AppDbContext cxt )
        {
          _config = config;
            token = _config["Payment:PaystackSk"];
            Paystack = new PayStackApi(token);
            _cxt = cxt;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Index(DonateViewModel donate)
        {
            TransactionInitializeRequest request = new()
            {
                AmountInKobo = donate.Amount * 100,
                Email = donate.Email,
                Reference = Generate().ToString(),
                Currency = "NGN",
                CallbackUrl = "http://localhost:27293/Donate/Verify"

            };

            TransactionInitializeResponse response = Paystack.Transactions.Initialize(request);
            if (response.Status)
            {
                var transaction = new TransactionModel()
                {
                    Amount = donate.Amount,
                    Email = donate.Email,
                    TransactionReference = request.Reference,
                    Name = donate.Name,
                };
                await _cxt.Transactions.AddAsync(transaction);
                await _cxt.SaveChangesAsync();
               return Redirect(response.Data.AuthorizationUrl);
            }
            ViewData["error"] = response.Message;
            return View();
        }
        [HttpGet]
        public IActionResult DonateItem()
        {
            var transactions =  _cxt.Transactions.Where(x => x.Status == true).ToList();
            ViewData["transactions"] = transactions;
            return View();
        }
        public IActionResult Donations()
        {

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Verify(string reference)
        {
            TransactionVerifyResponse response = Paystack.Transactions.Verify(reference);
            if(response.Data.Status == "success")
            {
                var transaction =  _cxt.Transactions.Where(a => a.TransactionReference
                == reference).FirstOrDefault();
                if(transaction != null)
                {
                    transaction.Status = true;
                    _cxt.Transactions.Update(transaction); 
                    await _cxt.SaveChangesAsync();
                    return RedirectToAction("DonateItem");
                }
            }
            ViewData["error"] = response.Data.GatewayResponse;
            return RedirectToAction("Index");
        }
        public static int Generate()
        {
            Random rnd = new Random((int)DateTime.Now.Ticks);
            return rnd.Next(100000000,999999990);
        }
    }
}
