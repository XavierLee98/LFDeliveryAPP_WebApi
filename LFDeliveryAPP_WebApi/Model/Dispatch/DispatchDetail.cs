using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.Model.Dispatch
{
    public class DispatchDetail
    {
        public int Id { get; set; }
        public int DocEntry { get; set; }
        public Guid Guid { get; set; }
        public Guid LineGuid { get; set; }
        public int PickNo { get; set; }
        public int InvoiceDocEntry { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LoadTime { get; set; }
        public DateTime InTransitTime { get; set; }
        public DateTime DeliveredTime { get; set; }
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
        public string attachmentnameList { get; set; }

    }
}
