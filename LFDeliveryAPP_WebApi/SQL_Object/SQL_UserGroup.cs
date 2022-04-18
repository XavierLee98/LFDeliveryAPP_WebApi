using Dapper;
using LFDeliveryAPP_WebApi.Class.SAP;
using LFDeliveryAPP_WebApi.Class.User;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.SQL_Object
{
    public class SQL_UserGroup
    {
        public string LastErrorMessage { get; set; } = string.Empty;
        SqlConnection conn;
        SqlTransaction trans;

        public void Dispose() => GC.Collect();
        string _MWdbConnectionStr;
        IConfiguration _configuration;

        public SQL_UserGroup(IConfiguration configuration, string MWdbConnectionStr)
        {
            _MWdbConnectionStr = MWdbConnectionStr;
            _configuration = configuration;
        }

        //public int DeleteGroup(UserGroup userGroup)
        //{
        //    try
        //    {

        //        string DeleteQuery = "Delete From UserGroup where Id = @GroupId ";

        //        conn = new SqlConnection(_MWdbConnectionStr);

        //        var result = conn.Query<int>(DeleteQuery, new { GroupId = userGroup.GroupId }).FirstOrDefault();


        //        return result;
        //    }
        //    catch (Exception excep)
        //    {
        //        LastErrorMessage = excep.ToString();
        //        return -1;
        //    }
        //}

        public bool CheckGroupNameIsUsed(UserGroup userGroup)
        {
            try
            {
                string selectQuery = "Select count(1) from SSO where UserGroupID = @GroupId";

                conn = new SqlConnection(_MWdbConnectionStr);

                var resultCount = conn.Query<int>(selectQuery, new { GroupId = userGroup.GroupId }).FirstOrDefault();
                if (resultCount > 0)
                {
                    LastErrorMessage = "The group is assigned to other user. Please remove before delete it.";
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

        public bool CheckGroupNameAvailable(UserGroup userGroup)
        {
            try
            {
                string selectQuery = "select count(1) from UserGroup Where GroupName = @GroupName;";

                conn = new SqlConnection(_MWdbConnectionStr);

                var resultCount = conn.Query<int>(selectQuery, new { GroupName = userGroup.GroupName }).FirstOrDefault();
                if (resultCount > 0)
                {
                    LastErrorMessage = "Group name is already exist. Please try another group name";
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

        int GenerateGroupId()
        {
            try
            {
                string selectQuery = "select count(1) from UserGroup;";

                conn = new SqlConnection(_MWdbConnectionStr);

                var result = conn.Query<int>(selectQuery).FirstOrDefault();
                return result;
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return -1;
            }
        }

        public int InsertNewGroupAndPermission(UserGroup userGroup)
        {
            try
            {
                int result = -1;
                conn = new SqlConnection(_MWdbConnectionStr);

                int id = GenerateGroupId();
                if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                using var trans = conn.BeginTransaction();

                string insertQuery = @"INSERT INTO [dbo].[UserGroup] 
                                        ( [GroupId] ,
                                         [GroupName] ,
                                         [GroupDescription])
                                        VALUES
                                        (
                                         @GroupId,
                                         @GroupName,
                                         @GroupDescription
                                        )";

                result = conn.Execute(insertQuery, new {GroupId = id, GroupName = userGroup.GroupName, GroupDescription = userGroup.GroupDescription }, trans);


                string insertQuery2 = @"INSERT INTO [dbo].[GroupPermission]
                                      ([ScreenId]
                                      ,[GroupId]
                                      ,[companyId]
                                      ,[title]
                                      ,[dscrptn]
                                      ,[IsAuthorised]
                                      ,[lastModiDate]
                                      ,[lastModiUser]
                                      ,[appName])
                                       VALUES (
                                        @ScreenId,
                                        @GroupId,
                                        @companyId,
                                        @title,
                                        @dscrptn,
                                        @IsAuthorised,
                                        GETDATE(),
                                        @lastModiUser,
                                        @appName )";


                foreach (var line in userGroup.Permissions)
                {
                    result = conn.Execute(insertQuery2,
                        new
                        {
                            ScreenId = line.ScreenId,
                            GroupId = id,
                            companyId = line.companyId,
                            title = line.title,
                            dscrptn = line.dscrptn,
                            IsAuthorised = line.IsAuthorised,
                            lastModiUser = line.lastModiUser,
                            appName = line.appName
                        },trans);
                }

                trans.Commit();

                return result;
            }
            catch (Exception excep)
            {
                trans.Rollback();
                LastErrorMessage = excep.ToString();
                return -1;
            }
        }

        public int DeleteGroupPermission(UserGroup selectedGroup)
        {
            try
            {
                string deleteQuery = "DELETE FROM [dbo].[GroupPermission] WHERE GroupId = @GroupId AND companyId = @CompanyId";

                conn = new SqlConnection(_MWdbConnectionStr);

                var result = conn.Execute(deleteQuery, new { GroupId = selectedGroup.GroupId , CompanyId  = selectedGroup.Permissions.Select(x=>x.companyId).FirstOrDefault()});
                return result;
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return -1;
            }
        }

        public int InsertGroupPermission(UserGroup selectedGroup)
        {
            try
            {
                string insertQuery = @"INSERT INTO [dbo].[GroupPermission]
                                      ([ScreenId]
                                      ,[GroupId]
                                      ,[companyId]
                                      ,[title]
                                      ,[dscrptn]
                                      ,[IsAuthorised]
                                      ,[lastModiDate]
                                      ,[lastModiUser]
                                      ,[appName])
                                       VALUES (
                                        @ScreenId,
                                        @GroupId,
                                        @companyId,
                                        @title,
                                        @dscrptn,
                                        @IsAuthorised,
                                        GETDATE(),
                                        @lastModiUser,
                                        @appName )";


                conn = new SqlConnection(_MWdbConnectionStr);
                int result = -1;

                foreach(var line in selectedGroup.Permissions)
                {
                    result = conn.Execute(insertQuery, 
                        new { 
                            ScreenId = line.ScreenId, 
                            GroupId = selectedGroup.GroupId, 
                            companyId = line.companyId, 
                            title = line.title, 
                            dscrptn = line.dscrptn, 
                            IsAuthorised = line.IsAuthorised,
                            lastModiUser = line.lastModiUser,
                            appName = line.appName
                        });
                }
                return result;
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return -1;
            }
        }

        public List<UserGroup> GetAllUserGroup(DBCommon dBCommon)
        {
            string selectQuery = " SELECT T0.* FROM UserGroup T0; " +
                                 "SELECT * FROM GroupPermission T0  WHERE T0.CompanyID = @CompanyID; ";

            List<UserGroup> userGroups = new List<UserGroup>();
            conn = new SqlConnection(_MWdbConnectionStr);
            using (var multi = conn.QueryMultiple(selectQuery, new { CompanyID = dBCommon.CompanyID } ))
            {
                userGroups = multi.Read<UserGroup>().ToList();
                var permissions = multi.Read<GroupPermission>().ToList();

                foreach(var line in userGroups)
                {
                    line.Permissions = new List<GroupPermission>();
                    foreach(var permission in permissions)
                    {
                        if(permission.GroupId.ToString() == line.GroupId.ToString())
                         line.Permissions.Add(permission);
                    }
                }
            };
            return userGroups;
        }

    }
}
