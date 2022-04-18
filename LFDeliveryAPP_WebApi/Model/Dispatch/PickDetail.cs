using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.Model.Dispatch
{
    public class PickDetail
    {
        public int PickNo { get; set; }
        public int InvoiceDocEntry { get; set; }
        public int InvoiceDocNum { get; set; }
        public DateTime DocDate { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public decimal Quantity { get; set; }
    }
}
