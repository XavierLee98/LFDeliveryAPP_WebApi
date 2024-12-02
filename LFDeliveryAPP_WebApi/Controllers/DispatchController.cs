﻿using Dapper;
using LFDeliveryAPP_WebApi.Class;
using LFDeliveryAPP_WebApi.Class.DTOs;
using LFDeliveryAPP_WebApi.Model.Dispatch;
using LFDeliveryAPP_WebApi.Model.SQL_Ex;
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

        [Authorize(Roles = "SuperAdmin, Admin, User")]
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
                    case "GetDispatchList_SuperVisor":
                        {
                            return GetDispatchList_SuperVisor(bag);
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
                    case "GetDipatchHistoryList":
                        {
                            return GetHistoryList(bag);
                        }
                    case "GetInvoiceDetailLine":
                        {
                            return GetInvoiceDetailLine(bag);
                        }
                    case "GetInvoicesPath":
                        {
                            return GetInvoicesPath(bag);
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

        IActionResult GetInvoicesPath(Cio bag)
        {
            try
            {
                var dispatch = new SQL_Dispatch(_configuration, _dbMWConnectionStr, bag.currentDB);

                bag.dispatchDetail.InvoiceReportURL = dispatch.GetInvoiceReportURL(bag.dispatchDetail);

                return Ok(bag);
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }

        IActionResult GetInvoiceDetailLine(Cio bag)
        {
            try
            {
                var dispatch = new SQL_Dispatch(_configuration, _dbMWConnectionStr, bag.currentDB);
                var detailLine = dispatch.GetInvoiceDetailLine(bag.QueryDocEntry);
                if (detailLine == null )
                {
                    return BadRequest(dispatch.LastErrorMessage);
                }
                bag.INV1s = detailLine;

                var attachmentStr = dispatch.GetAttachmentListStr(bag.QueryDocEntry, bag.currentDB.CompanyID);
                if (attachmentStr == null)
                {
                    return BadRequest(dispatch.LastErrorMessage);
                }
                bag.attachmentListStr = attachmentStr;

                return Ok(bag);
            }
            catch (Exception excep)
            { 
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }

        IActionResult GetHistoryList(Cio bag)
        {
            try
            {
                var dispatch = new SQL_Dispatch(_configuration, _dbMWConnectionStr, bag.currentDB);

                var InvoiceDocList = dispatch.GetAllDeliveredInvoice(bag.QueryDriver, bag.QueryStartTime, bag.QueryEndTime, bag.QueryDocEntry);
                if(InvoiceDocList == null)
                {
                    return BadRequest(dispatch.LastErrorMessage);
                }
                if(InvoiceDocList.Count <= 0)
                {
                    return Ok(bag);
                }

                var oinvs = dispatch.GetInvoiceHeaders(InvoiceDocList);
                if (oinvs == null || oinvs.Count <= 0)
                {
                    return BadRequest(dispatch.LastErrorMessage);
                }

                bag.OINVs = oinvs;
                return Ok(bag);
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
                var dispatch = new SQL_Dispatch(_configuration, _dbMWConnectionStr, bag.currentDB);

                var filterList = bag.DTODispatch.DispatchDetails.Where(x => x.Status == "Delivered").ToList();


                var result = dispatch.InsertAttachmentTable(bag.DTODispatch);
                if (result <= 0)
                {
                    _lastErrorMessage = dispatch.LastErrorMessage;
                    return BadRequest(bag);
                }

                if (filterList.Count == bag.DTODispatch.DispatchDetails.Count) return Ok(bag);

                result = dispatch.UpdateDispatchDetailStatus(bag.DTODispatch);
                if(result < 0)
                {
                    _lastErrorMessage = dispatch.LastErrorMessage;
                    return BadRequest(bag);
                }

                result = dispatch.CheckToUpdatedHeader(bag.DTODispatch);
                if (result < 0)
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
                    var dispatch = new SQL_Dispatch(_configuration, _dbMWConnectionStr, bag.currentDB);

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
                var _currentDB = bag.currentDB.CompanyDB;
                using (var conn = new SqlConnection(_dbMWConnectionStr))
                {
                    string query = "SELECT * FROM [" + _currentDB + "].[dbo].[INV1] WHERE DocEntry = @DocEntry";

                    var inv1s = conn.Query<INV1_Ex>(query, new { DocEntry = bag.QueryDocEntry}).ToList();
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
                var dispatch = new SQL_Dispatch(_configuration, _dbMWConnectionStr, bag.currentDB);

                var checkduplicate = dispatch.CheckPickListDuplicate(bag.QueryDocEntry);

                if (!checkduplicate)
                {
                    _lastErrorMessage = "Pick List already exist in other dispatch. Please try other pick no.";
                    return BadRequest(_lastErrorMessage);
                }

                bag.pKL1s = dispatch.GetPKL1(bag.QueryDocEntry);
                if (bag.pKL1s == null || bag.pKL1s.Count <= 0)
                {
                    _lastErrorMessage = "Pick List Item Not Found. Please Try Again.";
                    _lastErrorMessage += dispatch.LastErrorMessage;
                    return NotFound(_lastErrorMessage);
                }

                bag.OINVs = dispatch.GetInvoiceFromOPKL(bag.QueryDocEntry, bag.QueryTruck);
                if (bag.OINVs == null || bag.OINVs.Count <=0)
                {
                    _lastErrorMessage = "Pick List not found or not belongs to you.";
                    _lastErrorMessage += dispatch.LastErrorMessage;
                    return NotFound(_lastErrorMessage);
                }

                var result = dispatch.InsertDispatchLineDraft(bag.OINVs, bag.QueryDriver, bag.QueryDriverName,bag.QueryTruck);
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

        IActionResult CheckDriverExistingDraft(Cio bag)
        {
            try
            {
                using var dispatch = new SQL_Dispatch(_configuration, _dbMWConnectionStr, bag.currentDB);
                var result = dispatch.CheckExistingDraft(bag.QueryDriver);

                if (result == null) return Ok(bag);

                bag.OINVs = result;

                var pickNoDistinct = result.GroupBy(x => x.PickIdNo).Select(y => y.Key).ToList();

                bag.pKL1s = dispatch.GetPKL1s(pickNoDistinct);

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
                using var dispatch = new SQL_Dispatch(_configuration, _dbMWConnectionStr, bag.currentDB);
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
                    string query = "Select * FROM DispatchHeader WHERE DriverCode = @QueryDriver AND CompanyID = @CompanyID";

                    var result = conn.Query<DispatchHeader>(query, new { QueryDriver = bag.QueryDriver, CompanyID = bag.currentDB.CompanyID }).ToList();

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

        IActionResult GetDispatchList_SuperVisor(Cio bag)
        {
            try
            {
                using (var conn = new SqlConnection(_dbMWConnectionStr))
                {
                    string query = "Select * FROM DispatchHeader WHERE CompanyID = @CompanyID AND cast(CreatedDate as date) = cast(@SelectedDate as date)";

                    var result = conn.Query<DispatchHeader>(query, new { CompanyID = bag.currentDB.CompanyID, SelectedDate = bag.QueryStartTime }).ToList();

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
                var dispatch = new SQL_Dispatch(_configuration, _dbMWConnectionStr, bag.currentDB);

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


                result = dispatch.InsertInvoiceReporting(bag.DTODispatch.DispatchDetails);

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
