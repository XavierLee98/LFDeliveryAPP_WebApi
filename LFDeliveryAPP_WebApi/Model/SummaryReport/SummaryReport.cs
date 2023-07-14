using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.Model.SummaryReport
{
    public class SummaryReportModel
    {
        public int Id { get; set; }
        public string CompanyID { get; set; }
        public string TruckNum { get; set; }
        public string DriverCode { get; set; }
        public Guid Guid { get; set; }
        public string DocType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }
        public bool IsPost { get; set; }
        public int IsTry { get; set; }
        public string Path { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
