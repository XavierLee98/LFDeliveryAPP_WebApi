using LFDeliveryAPP_WebApi.Model.Dispatch;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.Class.DTOs
{
    public class DTODispatch
    {
        public DispatchHeader DispatchHeader { get; set; }
        public List<DispatchDetail> DispatchDetails { get; set; } = new List<DispatchDetail>();

        public List<DispatchHeader> DispatchHeaders { get; set; } = new List<DispatchHeader>();

        public string QueryStatus { get; set; }


    }
}
