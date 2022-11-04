using Dapper;
using DbClass;
using LFDeliveryAPP_WebApi.Class;
using LFDeliveryAPP_WebApi.Model.Other;
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
    public class SharedController : ControllerBase
    {
        string _lastErrorMessage { get; set; } = string.Empty;
        string _dbMWConnectionStr = string.Empty;
        string _dbSAPConnectionStr = string.Empty;
        string _dbMWName = "DatabaseDeliveryAppMw";
        string _dbSAPName = "DatabaseSAP";
        readonly IConfiguration _configuration;
        ILogger _logger;
        FileLogger _fileLogger = new FileLogger();

        public SharedController(IConfiguration configuration, ILogger<PaymentController> logger)
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
                    case "LoadCustomer":
                        {
                            return GetCustomerList(bag);
                        }
                    case "LoadTruck":
                        {
                            return GetTruckList(bag);
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

        public IActionResult GetTruckList(Cio bag)
        {
            try
            {
                using (var conn = new SqlConnection(_dbSAPConnectionStr))
                {
                    string query = $"SELECT Code [TruckCode], Name [TruckName] FROM {bag.currentDB.CompanyDB}..[@TRUCK]";
                    var result = conn.Query<Truck>(query).ToList();

                    bag.Trucks = result;

                }
                return Ok(bag);
            }
            catch (Exception excep)
            {
                _lastErrorMessage = excep.ToString();
                Log(_lastErrorMessage, bag);
                return BadRequest(_lastErrorMessage);
            }
        }

        public IActionResult GetCustomerList(Cio bag)
        {
            try
            {
                using (var conn = new SqlConnection(_dbSAPConnectionStr))
                {
                    string query = $"SELECT CardCode, CardName FROM {bag.currentDB.CompanyDB}..OCRD WHERE CardType = 'C' and validFor = 'Y'";
                    var result = conn.Query<OCRD>(query).ToList();

                    bag.BPResultList = result;

                }
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
