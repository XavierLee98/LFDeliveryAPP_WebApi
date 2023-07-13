using Dapper;
using LFDeliveryAPP_WebApi.Class;
using LFDeliveryAPP_WebApi.Class.User;
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
    public class UserController : ControllerBase
    {
        string _lastErrorMessage { get; set; } = string.Empty;
        string _dbMWConnectionStr = string.Empty;
        string _dbMWName = "DatabaseDeliveryAppMw";
        readonly IConfiguration _configuration;
        ILogger _logger;
        FileLogger _fileLogger = new FileLogger();

        public UserController(IConfiguration configuration, ILogger<UserController> logger)
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
                    case "ResetPassword":
                        {
                            return ResetPassword(bag);
                        }
                    case "UserList":
                        {
                            return UserList(bag);
                        }
                    case "UserGroupList":
                        {
                            return UserGroupList(bag);
                        }
                    case "UpdateDriverTruck":
                        {
                            return UpdateDriverTruck(bag);
                        }
                    case "UpdateUserDetails":
                        {
                            return UpdateUserDetails(bag);
                        }
                    case "InsertNewUser":
                        {
                            return InsertNewUser(bag);
                        }
                }
                _lastErrorMessage = "Request is Empty.";
                return null;
            }
            catch (Exception excep)
            {
                Log(excep.ToString(), bag);
                return null;
            }
        }

        IActionResult InsertNewUser(Cio bag)
        {
            try
            {
                var secKey = _configuration.GetSection("AppSettings").GetSection("Secret").Value;

                var sqlUser = new SQL_User(_configuration, _dbMWConnectionStr);

                var IsAvailable = sqlUser.CheckUsernameAvailable(bag.QuerySSO);
                if (!IsAvailable)
                {
                    Log(sqlUser.LastErrorMessage, bag);
                    return BadRequest(sqlUser.LastErrorMessage);
                }

                var result = sqlUser.InsertNewUser(bag.QuerySSO, secKey, bag.CurrentUser.UserName);
                if (result < 0)
                {
                    Log(sqlUser.LastErrorMessage, bag);
                    return BadRequest(sqlUser.LastErrorMessage);
                }

                return Ok(bag);
            }
            catch (Exception excep)
            {
                Log(excep.ToString(), bag);
                return BadRequest(excep.ToString());
            }
        }

        IActionResult UpdateUserDetails(Cio bag)
        {
            try
            {
                var secKey = _configuration.GetSection("AppSettings").GetSection("Secret").Value;

                var sqlUser = new SQL_User(_configuration, _dbMWConnectionStr);

                var result = sqlUser.UpdateUserDetails(bag.QuerySSO, secKey, bag.CurrentUser.UserName);
                if (result < 0)
                {
                    Log(sqlUser.LastErrorMessage, bag);
                    return BadRequest(sqlUser.LastErrorMessage);
                }

                return Ok(bag);
            }
            catch (Exception excep)
            {
                Log(excep.ToString(), bag);
                return BadRequest(excep.ToString());
            }
        }

        IActionResult UpdateDriverTruck(Cio bag)
        {
            try
            {
                var conn = new SqlConnection(_dbMWConnectionStr);

                var sqlUser = new SQL_User(_configuration, _dbMWConnectionStr);

                //var checkTruckAvailable = sqlUser.CheckTruckAvailable(bag.CurrentUser.TruckNum, bag.CurrentUser.UserName);

                //if (!checkTruckAvailable)
                //{
                //    return  BadRequest(sqlUser.LastErrorMessage);
                //}

                string updateQuery = "Update SSO SET TruckNum = @TruckNum Where UserName = @UserName And UserGroupID = 0";

                var usergroup = conn.Execute(updateQuery, new { TruckNum = bag.CurrentUser.TruckNum, UserName = bag.CurrentUser.UserName});
                return Ok(bag);
            }
            catch (Exception excep)
            {
                Log(excep.ToString(), bag);
                return BadRequest(excep.ToString());
            }
        }
        IActionResult UserGroupList(Cio bag)
        {
            try
            {
                var conn = new SqlConnection(_dbMWConnectionStr);
                string selectQuery = "SELECT * FROM UserGroup";

                var usergroup = conn.Query<UserGroup>(selectQuery).ToList();
                bag.UserGroups = usergroup;
                return Ok(bag);
            }
            catch (Exception excep)
            {
                Log(excep.ToString(), bag);
                return BadRequest(excep.ToString());
            }
        }

        IActionResult UserList(Cio bag)
        {
            try
            {
                var conn = new SqlConnection(_dbMWConnectionStr);
                string selectQuery = @"SELECT T1.[GroupDescription] [GroupDesc], T2.[RoleDescription] [RoleDesc], T0.* FROM SSO T0
                                      INNER JOIN[UserGroup] T1 ON T0.UserGroupID = T1.GroupId
                                      INNER JOIN[UserRole] T2 ON T0.UserRoleID = T2.RoleId
                                      WHERE T0.UserGroupID = @UserGroupID AND Username != 'Admin'";

                var user = conn.Query<SSO>(selectQuery, new { UserGroupID = bag.QueryGroup}).ToList();
                bag.userList = user;
                return Ok(bag);
            }
            catch (Exception excep)
            {
                Log(excep.ToString(), bag);
                return BadRequest(excep.ToString());
            }
        }

        IActionResult ResetPassword(Cio bag)
        {
            try
            {
                var secKey = _configuration.GetSection("AppSettings").GetSection("Secret").Value;

                var sqlUser = new SQL_User(_configuration, _dbMWConnectionStr);

                var result = sqlUser.ResetPassword(bag.CurrentUser.UserName, bag.password, bag.NewPassword, secKey);
                if (result < 0)
                {
                    Log($"{sqlUser.LastErrorMessage}", bag);
                    return BadRequest($"{sqlUser.LastErrorMessage}");
                }

                return Ok(bag);
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }

        void Log(string message, Cio bag)
        {
            _logger?.LogError(message, bag);
            _fileLogger.WriteLog(message);
        }
    }
}
