using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.Class.User
{
    public class SSO
    {
        public int Id { get; set; }
        public string CompanyId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string DisplayName { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public string LastModifiedUser { get; set; }
        public int UserGroup { get; set; }
        public bool IsEnabledExchange { get; set; }
        public string  UserGroupDesc { get; set; }
        public string assigned_token { get; set; }
        public DateTime Last_Login { get; set; }
    }
}
