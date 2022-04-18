using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {

        ILogger<PaymentController> _logger;

        public TestController(ILogger<PaymentController> Logger)
        {
            _logger = Logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            _logger.LogInformation("Testing Log");
            return Ok("Ok");
        }
    }
}
