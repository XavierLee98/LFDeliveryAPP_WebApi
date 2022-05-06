using Dapper;
using LFDeliveryAPP_WebApi.Class;
using LFDeliveryAPP_WebApi.Class.User;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.SQL_Object.Login
{
    public class mwUser : IDisposable
    {

        public SSO user { get; set; } = null;
        public string lastErrMsg { get; set; } = string.Empty;

        public void Dispose() => GC.Collect();
        string _MWconnString { get; set; }
        public mwUser(string MW_connString)
        {
            try
            {
                _MWconnString = MW_connString;
            }
            catch (Exception exception)
            {
                lastErrMsg = exception.ToString();
            }
        }

        public bool VerifyUser(string username, string pw, string secKey)
        {
            try
            {
                using (var conn = new SqlConnection(_MWconnString))
                {
                    conn.Open();
                    string query = "SELECT T0.*, T1.GroupDescription [UserGroupDesc] " +
                        " FROM SSO T0 " +
                        "INNER JOIN UserGroup T1 ON T0.UserGroup = T1.GroupId " +
                        " WHERE UserName = @userIdName ";

                    var param = new { userIdName = username };
                    var user = conn.Query<SSO>(query, param).FirstOrDefault();

                    if (user == null) return false;
                    string decrytedPw = new MD5EnDecrytor().Decrypt(pw, true, secKey);

                    if (!user.Password.Equals(decrytedPw)) return false;

                    this.user = user;
                    this.user.assigned_token = Guid.NewGuid().ToString();
                    UpdateUserRow(user.Id, this.user.assigned_token.ToString());

                    return true;
                }
            }
            catch (Exception excep)
            {
                lastErrMsg = excep.ToString();
                return false;
            }

            int UpdateUserRow(int id, string assignedToken)
            {
                try
                {
                    string updateSql = "UPDATE SSO " +
                        " SET Last_Login = GETDATE() " +
                        " ,assigned_token = @assignedToken " +
                        " WHERE Id = @id";


                    using (var conn = new SqlConnection(_MWconnString))
                    {
                        return conn.Execute(updateSql, new { assignedToken, id });
                    }
                }
                catch (Exception excep)
                {
                    lastErrMsg = excep.ToString();
                    return -1;
                }
            }
        }

        public string GetEncrytedGUID(string seckey)
        {
            if (user == null) return string.Empty;
            using (var decrytor = new MD5EnDecrytor())
            {
                return decrytor.Encrypt(user?.assigned_token.ToString(), true, seckey);
            }
        }       

        public string GetEncrytedPw(string seckey)
        {
            if (user == null) return string.Empty;

            using (var decrytor = new MD5EnDecrytor())
            {
                return decrytor.Encrypt(user?.Password, true, seckey);
            }
        }

    }
}
