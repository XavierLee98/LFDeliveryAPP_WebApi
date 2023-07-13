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
        public string TruckNum { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public string LastModifiedUser { get; set; }
        public int UserGroupID { get; set; }
        public string GroupDesc { get; set; }
        public int UserRoleID { get; set; }
        public string RoleDesc { get; set; }
        public bool IsEnabledExchange { get; set; }
        public bool IsActive { get; set; }
        public string assigned_token { get; set; }
        public DateTime Last_Login { get; set; }
        public List<GroupPermission> Menus{ get; set; }
    }
}
