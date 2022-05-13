using DbClass;
using LFDeliveryAPP_WebApi.Model.Other;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.Model.SQL_Ex
{
    public class OINV_Ex : OINV
    {
        public Guid Guid { get; set; } 
        public string PickIdNo { get; set; }
        public string ContactPerson { get; set; }
        public string ContactNo { get; set; }
        public DateTime DeliveredDate { get; set; }
    }
}
