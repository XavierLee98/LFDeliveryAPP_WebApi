using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.Class.User
{
    public class GroupPermission
    {
        public int Id { get; set; }
        public int ScreenId { get; set; }
        public int GroupId { get; set; }
        public string companyId { get; set; }
        public string title { get; set; }
        public string dscrptn { get; set; }
        public int IsAuthorised { get; set; }
        public DateTime lastModiDate { get; set; }
        public string lastModiUser { get; set; }
        public string appName { get; set; }
    }
}
