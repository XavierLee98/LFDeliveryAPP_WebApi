using Dapper;
using DbClass;
using LFDeliveryAPP_WebApi.Class;
using LFDeliveryAPP_WebApi.Class.DTOs;
using LFDeliveryAPP_WebApi.Model.Payment;
using LFDeliveryAPP_WebApi.Model.SQL_Ex;
using LFDeliveryAPP_WebApi.SAP_DIAPI;
using LFDeliveryAPP_WebApi.SQL_Object;
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
    public class PaymentController : ControllerBase
    {
        string _lastErrorMessage { get; set; } = string.Empty;
        string _dbMWConnectionStr = string.Empty;
        string _dbSAPConnectionStr = string.Empty;
        string _dbMWName = "DatabaseDeliveryAppMw";
        string _dbSAPName = "DatabaseSAP";
        readonly IConfiguration _configuration;
        ILogger _logger;
        FileLogger _fileLogger = new FileLogger();

        public PaymentController(IConfiguration configuration, ILogger<PaymentController> logger)
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
                switch(bag.request)
                {
                    case "GetInvoiceList":
                        {
                            return GetInvoiceList(bag);
                        }
                    case "GetBankInfo":
                        {
                            return GetBankInfo(bag);
                        }
                    case "CreatePayment":
                        {
                            return CreatePayment(bag);
                        }
                    case "GetPaymentHeaderHistory":
                        {
                            return GetPaymentHeaderHistory(bag);
                        }
                    case "GetPaymentDetails":
                        {
                            return GetPaymentDetails(bag);
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

        public IActionResult GetPaymentDetails(Cio bag)
        {
            try
            {
                var payment = new SQL_Payment(_configuration, _dbMWConnectionStr, _dbSAPConnectionStr, bag.currentDB.CompanyDB);

                var PaymentLines = payment.GetPaymentInvoiceLines(bag.QueryGuid);
                if(PaymentLines == null && PaymentLines.Count==0)
                {
                    Log(payment.LastErrorMessage, bag);
                    return BadRequest(payment.LastErrorMessage);
                }

                var PaymentMeans = payment.GetPaymentInvoiceMeans(bag.QueryGuid);
                if(PaymentMeans == null && PaymentMeans.Count == 0)
                {
                    Log(payment.LastErrorMessage, bag);
                    return BadRequest(payment.LastErrorMessage);
                }

                var attachmentstr = payment.GetPaymentAttachment(bag.QueryGuid);
                if (attachmentstr == null)
                {
                    return BadRequest(payment.LastErrorMessage);
                }

                bag.DTOPayment = new DTOPayment { IncomingPaymentDetails = PaymentLines, PaymentMeans = PaymentMeans, attachmentListStr = attachmentstr };
                return Ok(bag);
            }
            catch (Exception excep)
            {
                Log(excep.ToString(), bag);
                return BadRequest(excep.ToString());
            }
        }

        public IActionResult GetPaymentHeaderHistory(Cio bag)
        {
            try
            {
                var conn = new SqlConnection(_dbMWConnectionStr);
                string query = "SELECT * FROM IncomingPaymentHeader where DriverCode = @DriverCode " +
                               $"AND CAST(CreatedDate as date) >= CAST(@StartTime as date) AND CAST(CreatedDate as date) <= CAST(@EndTime as date)";

                var result = conn.Query<IncomingPaymentHeader>(query, new { DriverCode = bag.QueryDriver, StartTime = bag.QueryStartTime, EndTime = bag.QueryEndTime }).ToList();
                bag.IncomingPaymentHeaders = result;
                return Ok(bag);
            }
            catch (Exception excep)
            {
                Log(excep.ToString(), bag);
                return BadRequest(excep.ToString()); 
            }
        }

        public IActionResult CreatePayment(Cio bag)
        {
            try
            {
                var payment = new SQL_Payment(_configuration, _dbMWConnectionStr, _dbSAPConnectionStr, bag.currentDB.CompanyDB);

                var result = payment.InsertPayment(bag.DTOPayment);
                if (result < 0)
                {
                    Log(payment.LastErrorMessage, bag);
                    return BadRequest(payment.LastErrorMessage);
                }

                result = payment.InsertAttachmentTable(bag.DTOPayment.IncomingPaymentHeader);
                if (result < 0)
                {
                    Log(payment.LastErrorMessage, bag);
                    return BadRequest(payment.LastErrorMessage);
                }

                //Doing Posting
                var diapi = new Payment_Diapi(_configuration, _dbMWConnectionStr, _dbSAPConnectionStr);

                diapi.PaymentHeader = bag.DTOPayment.IncomingPaymentHeader;
                diapi.PaymentDetails = bag.DTOPayment.IncomingPaymentDetails;
                diapi.PaymentMeans = bag.DTOPayment.PaymentMeans;
                diapi.CompanyDB = bag.currentDB.CompanyDB;

                var docNo = diapi.PostToPaymentDoc();
                if (docNo < 0)
                {
                    Log(diapi.LastErrorMessage, bag);
                    return BadRequest(diapi.LastErrorMessage);
                }

                result = payment.UpdateHeader(bag.DTOPayment.IncomingPaymentHeader);
                if (result < 0)
                {
                    Log(payment.LastErrorMessage, bag);
                    return BadRequest(payment.LastErrorMessage);
                }

                return Ok();
            }
            catch (Exception excep)
            {
                Log(excep.ToString(), bag);
                return BadRequest(excep.ToString());
            }
        }

        public IActionResult GetBankInfo(Cio bag)
        {
            try
            {
                var conn = new SqlConnection(_dbSAPConnectionStr);
                string query = $"SELECT t0.* FROM {bag.currentDB.CompanyDB}..ODSC t0 INNER JOIN {bag.currentDB.CompanyDB}..DSC1 t1 ON t0.BankCode = t1.BankCode ";

                var result = conn.Query<ODSC>(query).ToList();
                bag.ODSCs = result;
               return Ok(bag);
            }
            catch (Exception excep)
            {
                Log(excep.ToString(), bag);
                return BadRequest(excep.ToString());
            }
        }

        public IActionResult GetInvoiceList(Cio bag)
        {
            try
            {
                var invoice = new SQL_Payment(_configuration,_dbMWConnectionStr,_dbSAPConnectionStr,bag.currentDB.CompanyDB);

                var invoicenoList = invoice.GetAvailableInvoiceFromPick(bag.QueryCardCode);
                if (invoice.LastErrorMessage !=null && invoice.LastErrorMessage.Length>0)
                {
                    Log(invoice.LastErrorMessage, bag);
                    BadRequest(invoice.LastErrorMessage);
                }

                var oinvs = invoice.GetInvoices(invoicenoList);
                if (invoice.LastErrorMessage != null && invoice.LastErrorMessage.Length > 0)
                {
                    Log(invoice.LastErrorMessage, bag);
                    BadRequest(invoice.LastErrorMessage);
                }
                bag.OINVs = oinvs;

                return Ok(bag);
            }
            catch (Exception excep)
            {
                Log(excep.ToString(), bag);
                return BadRequest(excep.ToString());
            }
        }

        void Log(string message, Cio bag)
        {
            _logger?.LogError(message, bag);
            _fileLogger.WriteLog(message);
        }
    }
}
