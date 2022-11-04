using LFDeliveryAPP_WebApi.Class;
using LFDeliveryAPP_WebApi.SQL_Object;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SummaryReportController : ControllerBase
    {
        string _lastErrorMessage { get; set; } = string.Empty;
        string _dbMWConnectionStr = string.Empty;
        string _dbSAPConnectionStr = string.Empty;
        string _dbMWName = "DatabaseDeliveryAppMw";
        string _dbSAPName = "DatabaseSAP";
        readonly IConfiguration _configuration;
        ILogger _logger;
        FileLogger _fileLogger = new FileLogger();
        public SummaryReportController(IConfiguration configuration, ILogger<PaymentController> logger)
        {
            _logger = logger;
            _configuration = configuration;
            _dbMWConnectionStr = _configuration.GetConnectionString(_dbMWName);
            _dbSAPConnectionStr = _configuration.GetConnectionString(_dbSAPName);
        }

        [Authorize(Roles = "SuperAdmin, Admin, User")]
        [HttpPost]
        public IActionResult Post(Cio bag)
        {
            try
            {
                switch (bag.request)
                {
                    case "RequestingReport":
                        {
                            return RequestingReport(bag);
                        }
                    case "GetReportPath":
                        {
                            return GetReportPath(bag);
                        }

                }
                _lastErrorMessage = "Request is Empty.";
                return null;
            }
            catch (Exception excep)
            {
                Log(excep.ToString(), bag);
                return BadRequest(excep.ToString());
            }
        }

        IActionResult RequestingReport(Cio bag)
        {
            try
            {
                var summaryrpt = new SQL_SummaryReport(_configuration, _dbMWConnectionStr);

                var result = summaryrpt.InsertSummaryRequest(bag.summaryReport);

                return Ok();
            }
            catch (Exception excep)
            {
                _lastErrorMessage = excep.ToString();
                Log(_lastErrorMessage, bag);
                return BadRequest(_lastErrorMessage);
            }
        }

        IActionResult GetReportPath(Cio bag)
        {
            try
            {
                var summaryrpt = new SQL_SummaryReport(_configuration, _dbMWConnectionStr);
                var pathresult = summaryrpt.GetReportPath(bag.summaryReport);

                if (string.IsNullOrEmpty(pathresult.Path))
                {
                    return BadRequest("Please onhold on a while. You may need click generate to try again.");
                }

                return Ok(pathresult);

            }
            catch (Exception excep)
            {
                _lastErrorMessage = excep.ToString();
                Log(_lastErrorMessage, bag);
                return BadRequest(_lastErrorMessage);
            }
        }

        void Log(string message, Cio bag)
        {
            _logger?.LogError(message, bag);
            _fileLogger.WriteLog(message);
        }

    }
}
