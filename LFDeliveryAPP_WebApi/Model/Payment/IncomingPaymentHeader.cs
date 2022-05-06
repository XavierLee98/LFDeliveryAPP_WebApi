using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.Model.Payment
{
    public class IncomingPaymentHeader
    {
        public int Id { get; set; }
        public string CompanyId { get; set; }
        public Guid Guid { get; set; }
        public string DriverCode { get; set; }
        public string DriverName { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public DateTime CreatedDate { get; set; }
        public string attachmentnameList { get; set; }
    }
}
