using Dapper;
using LFDeliveryAPP_WebApi.Class;
using LFDeliveryAPP_WebApi.Class.User;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
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
                    string query = @"SELECT T0.[UserName], T0.UserGroupID , T1.[GroupDescription] [GroupDesc],  T0.[CompanyId], T0.UserRoleID, T2.[RoleDescription] [RoleDesc], T0.IsEnabledExchange, T0.* FROM SSO T0
                                      INNER JOIN[UserGroup] T1 ON T0.UserGroupID = T1.GroupId
                                      INNER JOIN[UserRole] T2 ON T0.UserRoleID = T2.RoleId
                                      WHERE T0.UserName = @UserName";

                    var param = new { UserName = username };
                    var user = conn.Query<SSO>(query, param).FirstOrDefault();

                    if (user == null) return false;
                    string decrytedPw = new MD5EnDecrytor().Decrypt(pw, true, secKey);

                    if (!user.Password.Equals(decrytedPw)) return false;

                    this.user = user;
                    this.user.assigned_token = GetJWTToken(secKey);

                    var result = UpdateUserRow(user.Id, this.user.assigned_token.ToString());

                    if (result < 0)
                    {
                        return false;
                    }

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

        public string GetJWTToken(string _secretKey)
        {
            try
            {
                // authentication successful so generate jwt token
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_secretKey);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.Role, user.RoleDesc),
                        new Claim(ClaimTypes.Name, user.UserName),
                        new Claim(ClaimTypes.AuthenticationMethod, $"{Guid.NewGuid()}")
                    }),
                    Expires = DateTime.UtcNow.AddDays(1),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                // prepare the access token
                var token = tokenHandler.CreateToken(tokenDescriptor);

                return tokenHandler.WriteToken(token);
            }
            catch (Exception excep)
            {
                lastErrMsg = excep.ToString();
                return null;
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
