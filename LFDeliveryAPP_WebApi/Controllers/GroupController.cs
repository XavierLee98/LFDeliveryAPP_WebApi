using Dapper;
using LFDeliveryAPP_WebApi.Class;
using LFDeliveryAPP_WebApi.Class.User;
using LFDeliveryAPP_WebApi.ModelClass.User;
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
    public class GroupController : Controller
    {
        string _lastErrorMessage { get; set; } = string.Empty;
        string _dbMWConnectionStr = string.Empty;
        string _dbMWName = "DatabaseDeliveryAppMw";
        readonly IConfiguration _configuration;
        ILogger _logger;
        FileLogger _fileLogger = new FileLogger();

        public GroupController(IConfiguration configuration, ILogger<GroupController> logger)
        {
            _logger = logger;
            _configuration = configuration;
            _dbMWConnectionStr = _configuration.GetConnectionString(_dbMWName);
        }

        [Authorize(Roles = "SuperAdmin, Admin, User")]
        [HttpPost]
        public IActionResult Post(Cio bag)
        {
            try
            {
                switch (bag.request)
                {

                    case "GetGroupPermissionTemplate":
                        {
                            return GetGroupPermissionTemplate(bag);
                        }
                    case "GetAllUserGroup":
                        {
                            return GetAllUserGroup(bag);
                        }
                    case "ModifyPermission":
                        {
                            return ModifyPermission(bag);
                        }
                    case "InsertNewGroup":
                        {
                            return AddNewGroup(bag);
                        }
                    //case "DeleteGroupAndPermission":
                    //    {
                    //        return DeleteGroup(bag);
                    //    }
                }
                _lastErrorMessage = "Request is Empty.";
                return null;
            }
            catch (Exception excep)
            {
                return BadRequest(excep.ToString());
            }
        }

        //public IActionResult DeleteGroup(Cio bag)
        //{
        //    try
        //    {
        //        var userGroup = new SQL_UserGroup(_configuration, _dbMWConnectionStr);
        //        var isUsed = userGroup.CheckGroupNameIsUsed(bag.userGroup);
        //        if (!isUsed)
        //        {
        //            return BadRequest(userGroup.LastErrorMessage);
        //        }

        //        var result = userGroup.DeleteGroup(bag.userGroup);
        //        if (result < 0)
        //        {
        //            return BadRequest(userGroup.LastErrorMessage);
        //        }

        //        return Ok();

        //    }
        //    catch (Exception excep)
        //    {
        //        _lastErrorMessage = excep.ToString();
        //        Log(_lastErrorMessage, bag);
        //        return BadRequest(_lastErrorMessage);
        //    }
        //}

        public IActionResult AddNewGroup(Cio bag)
        {
            try
            {
                var userGroup = new SQL_UserGroup(_configuration, _dbMWConnectionStr);
                var isAvailable = userGroup.CheckGroupNameAvailable(bag.userGroup);
                if (!isAvailable)
                {
                    return BadRequest(userGroup.LastErrorMessage);
                }

                var result = userGroup.InsertNewGroupAndPermission(bag.userGroup);
                if (result < 0)
                {
                    return BadRequest(userGroup.LastErrorMessage);
                }

                return Ok();

            }
            catch (Exception excep)
            {
                _lastErrorMessage = excep.ToString();
                Log(_lastErrorMessage, bag);
                return BadRequest(_lastErrorMessage);
            }
        }

        public IActionResult ModifyPermission(Cio bag)
        {
            try
            {
                var userGroup = new SQL_UserGroup(_configuration, _dbMWConnectionStr);
                var result = userGroup.DeleteGroupPermission(bag.userGroup);
                if(result < 0)
                {
                    return BadRequest(userGroup.LastErrorMessage);
                }

                result = userGroup.InsertGroupPermission(bag.userGroup);
                if (result < 0)
                {
                    return BadRequest(userGroup.LastErrorMessage);
                }
                return Ok();
            }
            catch (Exception excep)
            {
                _lastErrorMessage = excep.ToString();
                Log(_lastErrorMessage, bag);
                return BadRequest(_lastErrorMessage);
            }
        }

        public IActionResult GetAllUserGroup(Cio bag)
        {
            try
            {
                var usergroup = new SQL_UserGroup(_configuration,_dbMWConnectionStr);
                bag.UserGroups = usergroup.GetAllUserGroup(bag.currentDB);

                return Ok(bag);
            }
            catch (Exception excep)
            {
                _lastErrorMessage = excep.ToString();
                Log(_lastErrorMessage, bag);
                return BadRequest(_lastErrorMessage);
            }
        }

        public IActionResult GetGroupPermissionTemplate(Cio bag)
        {
            try
            {
                var conn = new SqlConnection(_dbMWConnectionStr);
                string query = "SELECT * FROM GroupPermissionTemplate";

                var permissions = conn.Query<GroupPermissionTemplate>(query).ToList();
                bag.PermissionTemplates = permissions;
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
