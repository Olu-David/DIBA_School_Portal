using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Mvc;

namespace DIBA_School_Portal.Server.Controllers
{

    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        }

        [HttpPost("verify-flutterwave")]
        public async Task<IActionResult> VerifyFlutterwave([FromQuery] string transactionId, [FromQuery] string txRef)
        {
            var result = await _paymentService.VerifyAndCompleteFlutterwavePaymentAsync(transactionId, txRef);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
