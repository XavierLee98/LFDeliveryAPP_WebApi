using Dapper;
using DbClass;
using LFDeliveryAPP_WebApi.Class;
using LFDeliveryAPP_WebApi.Model.Dashboard;
using LFDeliveryAPP_WebApi.SQL_Object;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DashboardController : ControllerBase
    {
        string _lastErrorMessage { get; set; } = string.Empty;
        string _dbMWConnectionStr = string.Empty;
        string _dbSAPConnectionStr = string.Empty;
        string _dbMWName = "DatabaseDeliveryAppMw";
        string _dbSAPName = "DatabaseSAP";
        readonly IConfiguration _configuration;
        ILogger _logger;
        FileLogger _fileLogger = new FileLogger();
        public DashboardController(IConfiguration configuration, ILogger<PaymentController> logger)
        {
            _logger = logger;
            _configuration = configuration;
            _dbMWConnectionStr = _configuration.GetConnectionString(_dbMWName);
            _dbSAPConnectionStr = _configuration.GetConnectionString(_dbSAPName);
        }

        [HttpPost]
        public IActionResult Post(Cio bag)
        {
            try
            {
                switch (bag.request)
                {
                    case "GetDashboardResult":
                        {
                            return GetDashboardResult(bag);
                        }
                    case "GetDashboardResultCount":
                        {
                            return GetDashboardResultCount(bag);
                        }
                }
                _lastErrorMessage = "Request is Empty.";
                return null;
            }
            catch (Exception excep)
            {
                return BadRequest(excep.ToString());
            }
        }


        public IActionResult GetDashboardResult(Cio bag)
        {
            try
            {
                var dashboard = new SQL_Dashboard(_configuration, _dbMWConnectionStr,_dbSAPConnectionStr);

                bag.DashboardResults = dashboard.GetDashboardResult(bag);
                if (dashboard.LastErrorMessage.Length > 0)
                {
                    BadRequest(_lastErrorMessage);
                }
                return Ok(bag);
            }
            catch (Exception excep)
            {
                _lastErrorMessage = excep.ToString();
                return BadRequest(_lastErrorMessage);
            }
        }

        public IActionResult GetDashboardResultCount(Cio bag)
        {
            try
            {
                var dashboard = new SQL_Dashboard(_configuration, _dbMWConnectionStr, _dbSAPConnectionStr);

                bag = dashboard.GetDashboardResultCount(bag);
                if (dashboard.LastErrorMessage.Length > 0)
                {
                    BadRequest(_lastErrorMessage);
                }
                return Ok(bag);
            }
            catch (Exception excep)
            {
                _lastErrorMessage = excep.ToString();
                return BadRequest(_lastErrorMessage);
            }
        }



    }
}
