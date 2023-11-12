using Azure.Core;
using EekiBooks.DataAcess.Repository.IRepository;
using EekiBooks.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;




namespace EekiBooksOnline.Areas.Admin.Controllers
{
    [Route("webhook")]
    [ApiController]
    public class StripeWebhookController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly IConfiguration _configuration;

        [BindProperty]
        public OrderVM OrderVM { get; set; }

        public StripeWebhookController(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> Index()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                var StripeWebhookSecret = _configuration["Stripe:WebhookSecret"];
                var stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], StripeWebhookSecret);

                if (stripeEvent.Type == Events.PaymentIntentSucceeded)
                {
                    var session = stripeEvent.Data.Object as Session;
                    var paymentIntent = session.PaymentIntent.Id;

                    // Retrieve the OrderHeader from the database
                    var orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id);
                    if (orderHeader != null)
                    {
                        // Update the payment_intent in the database
                        orderHeader.PaymentIntent = paymentIntent.ToString();
                        _unitOfWork.OrderHeader.Update(orderHeader);
                        _unitOfWork.Save();

                        // Handle other logic related to the completed session
                    }
                    else
                    {
                        // Handle the case where the order is not found
                        // Log or perform appropriate actions
                        Console.WriteLine("Unhandled event type: {0}", stripeEvent.Type);
                    }
                }
            }
            catch (StripeException e)
            {
                // Handle the exception
                return BadRequest();
            }

            return Ok();
        }
    }
}
