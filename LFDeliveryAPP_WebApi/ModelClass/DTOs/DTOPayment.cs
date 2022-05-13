using LFDeliveryAPP_WebApi.Model.Payment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.Class.DTOs
{
    public class DTOPayment
    {
        public IncomingPaymentHeader IncomingPaymentHeader { get; set; }
        public List<IncomingPaymentDetail> IncomingPaymentDetails { get; set; }
        public List<PaymentMean> PaymentMeans { get; set; }
        public string attachmentListStr { get; set; }
    }
}
