using Dapper;
using LFDeliveryAPP_WebApi.Class;
using LFDeliveryAPP_WebApi.Class.DTOs;
using LFDeliveryAPP_WebApi.Model.Dispatch;
using LFDeliveryAPP_WebApi.Model.SQL_Ex;
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
    public class DispatchController : ControllerBase
    {
        string _lastErrorMessage { get; set; } = string.Empty;
        string _dbMWConnectionStr = string.Empty;
        string _dbSAPConnectionStr = string.Empty;
        string _dbMWName = "DatabaseDeliveryAppMw";
        string _dbSAPName = "DatabaseSAP";
        readonly IConfiguration _configuration;
        ILogger _logger;
        FileLogger _fileLogger = new FileLogger();

        public DispatchController(IConfiguration configuration, ILogger<DispatchController> logger)
        {
            _logger = logger;
            _configuration = configuration;
            _dbMWConnectionStr = _configuration.GetConnectionString(_dbMWName);
            _dbSAPConnectionStr = _configuration.GetConnectionString(_dbSAPName);
        }

        [HttpPost]
        public IActionResult ActionPost(Cio bag)
        {
            try
            {
                _lastErrorMessage = string.Empty;

                switch (bag.request)
                {
                    case "GetInvoicesFromPickList":
                        {
                            return GetInvoicesFromPickList(bag);
                        }
                    case "GetINV1":
                        {
                            return GetINV1(bag);
                        }
                    case "CreateDispatchDoc":
                        {
                            return CreateDispatchDoc(bag);
                        }
                    case "CheckExistingDraft":
                        {
                            return CheckDriverExistingDraft(bag);
                        }
                    case "RemoveDispatchDraft":
                        {
                            return RemoveDispatchDraft(bag);
                        }
                    case "GetDispatchList":
                        {
                            return GetDispatchList(bag);
                        }
                    case "GetInvoiceHeaderList":
                        {
                            return GetInvoiceHeaderList(bag);
                        }
                    case "GetInvoiceDetails":
                        {
                            return GetInvoiceDetails(bag);
                        }
                    case "UpdateDipatchWithAttachment":
                        {
                            return UpdateDipatchWithAttachment(bag);
                        }
                }
                _lastErrorMessage = "Request is Empty.";
                return null;
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }

        IActionResult UpdateDipatchWithAttachment(Cio bag)
        {
            try
            {
                var dispatch = new SQL_Dispatch(_configuration, _dbMWConnectionStr, _dbSAPConnectionStr);

                var filterList = bag.DTODispatch.DispatchDetails.Where(x => x.Status == "Delivered").ToList();


                var result = dispatch.InsertAttachmentTable(bag.DTODispatch);
                if (result <= 0)
                {
                    _lastErrorMessage = dispatch.LastErrorMessage;
                    return BadRequest(bag);
                }

                if (filterList.Count == bag.DTODispatch.DispatchDetails.Count) return Ok(bag);

                result = dispatch.UpdateDispatchDetailStatus(bag.DTODispatch);
                if(result <=0)
                {
                    _lastErrorMessage = dispatch.LastErrorMessage;
                    return BadRequest(bag);
                }

                result = dispatch.CheckToUpdatedHeader(bag.DTODispatch);
                if (result <= 0)
                {
                    _lastErrorMessage = dispatch.LastErrorMessage;
                    return BadRequest(bag);
                }

                return Ok(bag);
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }

        IActionResult GetInvoiceDetails(Cio bag)
        {
            try
            {
                using (var conn = new SqlConnection(_dbSAPConnectionStr))
                {
                    var dispatch = new SQL_Dispatch(_configuration, _dbMWConnectionStr, _dbSAPConnectionStr);

                    var OINVs = dispatch.GetInvoiceHeaders(bag.QueryDocEntries);
                    if (OINVs == null)
                    {
                        BadRequest(bag);
                    }
                    bag.OINVs = OINVs;

                    var INV1s = dispatch.GetInvoiceDetails(bag.QueryDocEntries);
                    if (INV1s == null)
                    {
                        BadRequest(bag);
                    }
                    bag.INV1s = INV1s;

                    //var existingAttachment = dispatch.
                    return Ok(bag);
                }
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }
        IActionResult GetINV1(Cio bag)
        {
            try
            {
                using (var conn = new SqlConnection(_dbSAPConnectionStr))
                {
                    string query = "SELECT * FROM INV1 WHERE DocEntry = @DocEntry";

                    var inv1s = conn.Query<INV1_Ex>(query, new { DocEntry = bag.QueryDocEntry }).ToList();
                    bag.INV1s = inv1s;
                    return Ok(bag);
                }
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }


        IActionResult GetInvoicesFromPickList(Cio bag)
        {
            try
            {
                var dispatch = new SQL_Dispatch(_configuration, _dbMWConnectionStr, _dbSAPConnectionStr);


                bag.OINVs = dispatch.GetInvoiceFromOPKL(bag.QueryDocEntry, bag.QueryDriver);
                if (bag.OINVs == null || bag.OINVs.Count <=0)
                {
                    _lastErrorMessage = "Pick List not found or not belongs to you.";
                    _lastErrorMessage += dispatch.LastErrorMessage;
                    return BadRequest(_lastErrorMessage);
                }
                var result = dispatch.InsertDispatchLineDraft(bag.OINVs, bag.QueryDriver);
                if (result < 0)
                {
                    bag.LastErrorMessage = "Fail To Insert into Draft.";
                    return BadRequest(bag.LastErrorMessage);
                }

                return Ok(bag);
            }
            catch (Exception excep)
            {
                _lastErrorMessage = excep.ToString();
                return BadRequest(_lastErrorMessage);
            }
        }


        IActionResult GetInvoiceHeaderList(Cio bag)
        {
            try
            {
                string query = "SELECT * FROM DispatchDetail WHERE DocEntry = @DocEntry";
                using(var conn = new SqlConnection(_dbMWConnectionStr))
                {
                    var detailList = conn.Query<DispatchDetail>(query, new { DocEntry = bag.QueryDocEntry }).ToList();
                    var dto = new DTODispatch();
                    dto.DispatchDetails = detailList;
                    return Ok(dto);
                }
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }

        //IActionResult GetInvoiceDocDetail(Cio bag)
        //{
        //    try
        //    {
        //        return Ok();
        //    }
        //    catch (Exception excep)
        //    {
        //        Log($"{excep}", bag);
        //        return BadRequest($"{excep}");
        //    }
        //}

        IActionResult CheckDriverExistingDraft(Cio bag)
        {
            try
            {
                using var dispatch = new SQL_Dispatch(_configuration, _dbMWConnectionStr, _dbSAPConnectionStr);
                var result = dispatch.CheckExistingDraft(bag.QueryDriver);

                bag.OINVs = result;
                return Ok(bag);
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }

        IActionResult RemoveDispatchDraft(Cio bag)
        {
            try
            {
                using var dispatch = new SQL_Dispatch(_configuration, _dbMWConnectionStr, _dbSAPConnectionStr);
                var result = dispatch.RemoveDraft(bag.QueryDriver, bag.QueryDocEntry);
                if (result < 0)
                {
                    return BadRequest("Fail to remove draft");
                }
                return Ok();
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }

        IActionResult GetDispatchList(Cio bag)
        {
            try
            {
                using(var conn = new SqlConnection(_dbMWConnectionStr))
                {
                    string query = "Select * FROM DispatchHeader WHERE DriverCode = @QueryDriver";

                    var result = conn.Query<DispatchHeader>(query, new { QueryDriver = bag.QueryDriver }).ToList();

                    var dto = new DTODispatch();

                    dto.DispatchHeaders = result;
                    return Ok(dto);
                }
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }

        IActionResult CreateDispatchDoc(Cio bag)
        {
            try
            {
                var dispatch = new SQL_Dispatch(_configuration, _dbMWConnectionStr, _dbSAPConnectionStr);

                var result = dispatch.CreateDispatchDoc(bag.DTODispatch);

                if (result < 0)
                {
                    _lastErrorMessage = dispatch.LastErrorMessage;
                    return BadRequest(dispatch.LastErrorMessage);
                }

                result = dispatch.RemoveDraft(bag.QueryDriver, bag.QueryDocEntry);
                if (result < 0)
                {
                    _lastErrorMessage = dispatch.LastErrorMessage;
                    return BadRequest(dispatch.LastErrorMessage);
                }

                return Ok();
            }
            catch (Exception excep)
            {
                _lastErrorMessage = excep.ToString();
                return BadRequest(_lastErrorMessage);
            }
        }







        /// <summary>
        /// Logging error to log
        /// </summary>
        /// <param name="message"></param>
        /// <param name="obj"></param>
        void Log(string message, Cio bag)
        {
            _logger?.LogError(message, bag);
            _fileLogger.WriteLog(message);
        }
    }
}
