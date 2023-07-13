using Dapper;
using LFDeliveryAPP_WebApi.Class;
using LFDeliveryAPP_WebApi.Class.SAP;
using LFDeliveryAPP_WebApi.Class.User;
using LFDeliveryAPP_WebApi.SQL_Object.Login;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("[controller]")]
    public class LoginController : ControllerBase
    {
        readonly string _dbMidwareName = "DatabaseDeliveryAppMw";
        string _dbMWConnectionStr;
        readonly IConfiguration _configuration;
        ILogger<PaymentController> _logger;
        //FileLogger _fileLogger = new FileLogger();
        string _lastErrorMessage = string.Empty;
        public LoginController(IConfiguration configuration, ILogger<PaymentController> logger)
        {
            _logger = logger;
            _configuration = configuration;
            _dbMWConnectionStr = _configuration.GetConnectionString(_dbMidwareName);
        }

        /// <summary>
        /// Controller entry point
        /// </summary>
        /// <param name="cio"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        public IActionResult ActionPost(Cio bag)
        {
            try
            {
                switch (bag.request)
                {
                    case "Login":
                        {
                            return ProcessLogin(bag);
                        }

                }
                return BadRequest($"Invalid request, please try again later. Thanks");
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }

        IActionResult ProcessLogin(Cio cio)
        {
            try
            {
                var appName = _configuration.GetSection("AppSettings").GetSection("AppName").Value;
                var secKey = _configuration.GetSection("AppSettings").GetSection("Secret").Value;
                var user = new mwUser(_dbMWConnectionStr);

                bool isVerify = user.VerifyUser(cio.username, cio.password, secKey);
                if (!isVerify)
                {
                    Log($"User login fail \n{user.lastErrMsg}", cio);
                    return Unauthorized($"User login fail \n{user.lastErrMsg}");
                }

                if (!CheckActive(cio.username))
                {
                    return Unauthorized($"Account {cio.username} is inactive.");
                }

                if(getcurrentDB(user.user.CompanyId) == null)
                {
                    return BadRequest($"Fail to get DBCommon.");
                }
                cio.currentDB = (getcurrentDB(user.user.CompanyId));
                cio.CurrentUser = user.user;
                cio.CurrentUser.Menus = getMenuPermission(cio.CurrentUser);
                cio.CurrentUser.assigned_token = user.user.assigned_token;
                cio.CurrentUser.Password = user.GetEncrytedPw(secKey);
                cio.CurrentUser.GroupDesc = GetGroupDesc(cio.CurrentUser.UserGroupID);

                return Ok(cio);
            }
            catch (Exception excep)
            {
                Log($"{excep}", cio);
                return BadRequest($"{excep}");
            }
        }

        public string GetGroupDesc(int GroupID)
        {
            try
            {
                var conn = new SqlConnection(_dbMWConnectionStr);
                string query = "SELECT GroupDescription FROM UserGroup WHERE GroupId = @GroupId";
                return conn.Query<string>(query, new { GroupID = GroupID }).FirstOrDefault();
            }
            catch (Exception excep)
            {
                Log($"{excep}", null);
                return null;
            }
        }

        public List<GroupPermission> getMenuPermission(SSO user)
        {
            try
            {
                var conn = new SqlConnection(_dbMWConnectionStr);
                string query = "SELECT * FROM GroupPermission WHERE GroupId = @GroupId AND companyId = @companyID AND IsAuthorised = 1";

                if(user.UserRoleID == 0)
                    query = "SELECT * FROM GroupPermission WHERE GroupId = @GroupId AND companyId = @companyID";

                return conn.Query<GroupPermission>(query, new { GroupId = user.UserGroupID, companyID = user.CompanyId }).ToList();
            }
            catch (Exception excep)
            {
                Log($"{excep}", null);
                return null;
            }
        }

        public bool CheckActive(string username)
        {
            try
            {
                var conn = new SqlConnection(_dbMWConnectionStr);
                string query = "SELECT IsActive FROM SSO Where UserName = @UserName";
                return conn.Query<bool>(query, new { UserName = username }).FirstOrDefault();
            }
            catch (Exception excep)
            {
                Log($"{excep}", null);
                return false;
            }
        }

        public DBCommon getcurrentDB(string companyID)
        {
            try
            {
                var conn = new SqlConnection(_dbMWConnectionStr);
                string query = "SELECT * FROM DBCommon WHERE CompanyID = @companyID";
                return conn.Query<DBCommon>(query, new { companyID = companyID }).FirstOrDefault();
            }
            catch (Exception excep)
            {
                Log($"{excep}", null);
                return null;
            }
        }

        void Log(string message, Cio bag)
        {
            _logger.LogError(message, bag);
        }
    }
}
