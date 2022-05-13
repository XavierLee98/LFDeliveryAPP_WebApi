using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.Model.Dispatch
{
    public class DispatchHeader
    {
        public int Id { get; set; }
        public int DocEntry { get; set; }
        public Guid Guid { get; set; }
        public string CompanyID { get; set; }
        public string DriverCode{ get; set; }
        public string DriverName { get; set; }
        public string DocStatus { get; set; }
        public DateTime LastUpdatedDate { get; set; }
        public DateTime CreatedDate{ get; set; }
    }
}
