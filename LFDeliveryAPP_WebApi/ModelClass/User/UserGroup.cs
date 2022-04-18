using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.Class.User
{
    public class UserGroup
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }   
        public string GroupDescription { get; set; }
        public List<GroupPermission> Permissions { get; set; }

    }
}
