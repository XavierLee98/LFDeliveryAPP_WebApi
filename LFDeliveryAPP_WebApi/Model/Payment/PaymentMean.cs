using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.Model.Payment
{
    public class PaymentMean
    {
        public int Id { get; set; }
        public string PaymentMethod { get; set; }
        public string BankName { get; set; }
        public string BankCode { get; set; }
        public string CheqNum { get; set; }
        public decimal Amount { get; set; }
        public Guid Guid { get; set; }
        public DateTime DueDate { get; set; }
        public string Reference { get; set; }
    }
}
