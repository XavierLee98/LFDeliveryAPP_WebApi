using Dapper;
using LFDeliveryAPP_WebApi.Class;
using LFDeliveryAPP_WebApi.Class.User;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.SQL_Object
{
    public class SQL_User : IDisposable
    {
        public string LastErrorMessage { get; set; } = string.Empty;
        SqlConnection conn;
        SqlTransaction trans;
        public void Dispose() => GC.Collect();
        string _MWdbConnectionStr;
        IConfiguration _configuration;
        public SQL_User(IConfiguration configuration, string MWdbConnectionStr)
        {
            _MWdbConnectionStr = MWdbConnectionStr;
            _configuration = configuration;
        }

        public bool CheckUsernameAvailable(SSO newuser)
        {
            try
            {
                conn = new SqlConnection(_MWdbConnectionStr);
                string selectquery = @"SELECT count(1)
                                 FROM [dbo].[SSO]
                                 WHERE UserName = @UserName";
                var usercount = conn.Query<int>(selectquery, new { UserName = newuser.UserName }).FirstOrDefault();

                if (usercount > 0)
                {
                    LastErrorMessage = "username is already exist. Please try another username";
                    return false;
                }
                return true;
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return false;
            }
        }


        public bool CheckTruckAvailable(string truckNum,string userName)
        {
            try
            {
                conn = new SqlConnection(_MWdbConnectionStr);
                var selectQuery = "SELECT Count(1) FROM SSO WHERE TruckNum = @TruckNum AND UserName != @UserName AND IsActive = 1;";
                var countResult = conn.Query<int>(selectQuery, new { TruckNum = truckNum, UserName = userName }).FirstOrDefault();

                if(countResult > 0)
                {
                    LastErrorMessage = "Truck is chosen by other driver. Please Select Other.";
                    return false;
                }
                return true;
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return false;
            }

        }
        public int InsertNewUser(SSO QueryUser, string secKey, string currentUser)
        {
            try
            {
                conn = new SqlConnection(_MWdbConnectionStr);
                string decrytedPw = new MD5EnDecrytor().Decrypt(QueryUser.Password, true, secKey);

                string insertQuery = @" INSERT INTO [dbo].[SSO]
                                        ([CompanyId]
                                        ,[UserName]
                                        ,[Password]
                                        ,[DisplayName]
                                        ,[LastModifiedDate]
                                        ,[LastModifiedUser]
                                        ,[UserGroupID]
                                        ,[UserRoleID]
                                        ,[IsEnabledExchange]
                                        ,[IsActive]
                                        )
                                        VALUES
                                        (
                                            @CompanyId,
                                            @UserName,
                                            @Password,
                                            @DisplayName,
                                            GETDATE(),
                                            @currentUser,
                                            @UserGroupID,
                                            @UserRoleID,
                                            @IsEnabledExchange,
                                            1
                                        )";

                var result = conn.Execute(insertQuery,
                                     new
                                     {
                                         CompanyId = QueryUser.CompanyId,
                                         UserName = QueryUser.UserName.ToUpper(),
                                         Password = decrytedPw,
                                         DisplayName = QueryUser.DisplayName.ToUpper(),
                                         UserGroupID = QueryUser.UserGroupID,
                                         UserRoleID = QueryUser.UserRoleID,
                                         IsEnabledExchange = QueryUser.IsEnabledExchange,
                                         currentUser = currentUser
                                     });
                return result;
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return -1;
            }
        }

        public int UpdateUserDetails(SSO QueryUser, string secKey, string currentUser)
        {
            try
            {
                conn = new SqlConnection(_MWdbConnectionStr);
                string decrytedPw = new MD5EnDecrytor().Decrypt(QueryUser.Password, true, secKey);

                string updateQuery = @" UPDATE [dbo].[SSO] SET Password = @Pwd, UserGroupID = @GroupID, UserRoleID = @RoleID, IsEnabledExchange = @IsEnabledExchange,
                                        LastModifiedDate = GETDATE(), LastModifiedUser = @currentUser, CompanyID = @CompanyID, DisplayName = @DisplayName, TruckNum = @TruckNum, IsActive = @IsActive
                                        WHERE UserName = @UserName";

                var result = conn.Execute(updateQuery, 
                                     new { 
                                           Pwd = decrytedPw ,
                                           GroupID = QueryUser.UserGroupID, 
                                           RoleID = QueryUser.UserRoleID,
                                           IsEnabledExchange = QueryUser.IsEnabledExchange, 
                                           currentUser = currentUser, 
                                           UserName = QueryUser.UserName,
                                           CompanyID = QueryUser.CompanyId,
                                           DisplayName = QueryUser.DisplayName,
                                           IsActive = QueryUser.IsActive,
                                           TruckNum = QueryUser.TruckNum,
                                         });

                return result;
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return -1;
            }
        }

        public int ResetPassword(string UserName, string OldPassword, string newPassword, string secKey)
        {
            try
            {
                conn = new SqlConnection(_MWdbConnectionStr);
                string decrytedOldPw = new MD5EnDecrytor().Decrypt(OldPassword, true, secKey);
                string decrytedNewPw = new MD5EnDecrytor().Decrypt(newPassword, true, secKey);

                string selectquery = @"SELECT [Id]
                                 ,[CompanyId]
                                 ,[UserName]
                                 ,[Password]
                                 ,[DisplayName]
                                 ,[UserGroupID]
                                 ,[IsEnabledExchange]
                                 ,[assigned_token]
                                 FROM [dbo].[SSO]
                                 WHERE UserName = @UserName";
                var user = conn.Query<SSO>(selectquery, new { UserName = UserName }).FirstOrDefault();

                if(user == null)
                {
                    LastErrorMessage = "User not found.";
                    return -1;
                }

                if(!user.Password.Equals(decrytedOldPw))
                {
                    LastErrorMessage = "The current password is incorrect.";
                    return -1;
                }

                string updatequery = @"UPDATE [dbo].[SSO] SET Password = @Pwd, LastModifiedUser = @UserName, LastModifiedDate = GETDATE()
                                       WHERE UserName = @UserName";

                var result = conn.Execute(updatequery, new { Pwd = decrytedNewPw, UserName = UserName });

                return result;
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return -1;
            }
        }

    }
}
