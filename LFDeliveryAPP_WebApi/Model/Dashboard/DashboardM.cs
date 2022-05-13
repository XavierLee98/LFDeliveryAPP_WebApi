using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.Model.Dashboard
{
    public class DashboardM
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }

        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string Status { get; set; }
        public DateTime LoadTime { get; set; }
        public DateTime InTransitTime { get; set; }
        public DateTime DeliveredTime { get; set; }
    }
}
