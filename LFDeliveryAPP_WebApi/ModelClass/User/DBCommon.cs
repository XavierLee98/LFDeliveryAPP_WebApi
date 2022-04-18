using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.Class.SAP
{
    public class DBCommon
    {
        public int Id { get; set; }
        public string CompanyID { get; set; }
        public string CompanyName { get; set; }
        public string Server { get; set; }
        public string WarehouseCompanyDB { get; set; }
        public string CompanyDB { get; set; }
        public string DbUserName { get; set; }
        public string DbPassword { get; set; }
        public int DbServerType { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string SLDServer { get; set; }
    }
}
