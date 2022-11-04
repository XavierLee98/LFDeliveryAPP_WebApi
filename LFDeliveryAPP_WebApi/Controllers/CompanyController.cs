using Dapper;
using LFDeliveryAPP_WebApi.Class;
using LFDeliveryAPP_WebApi.Class.SAP;
using LFDeliveryAPP_WebApi.Class.User;
using Microsoft.AspNetCore.Authorization;
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
    public class CompanyController : Controller
    {
        string _lastErrorMessage { get; set; } = string.Empty;
        string _dbMWConnectionStr = string.Empty;
        string _dbSAPConnectionStr = string.Empty;
        string _dbMWName = "DatabaseDeliveryAppMw";
        string _dbSAPName = "DatabaseSAP";
        readonly IConfiguration _configuration;
        ILogger _logger;
        FileLogger _fileLogger = new FileLogger();

        public CompanyController(IConfiguration configuration, ILogger<PaymentController> logger)
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
                    case "GetCompanyConnection":
                        {
                            return GetCompanyConnection(bag);
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

        public IActionResult GetCompanyConnection(Cio bag)
        {
            try
            {
                var conn = new SqlConnection(_dbMWConnectionStr);
                string query = "SELECT * FROM DBCommon";

                var companyList = conn.Query<DBCommon>(query).ToList();
                bag.dBCommonList = companyList;
                return Ok(bag);
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
