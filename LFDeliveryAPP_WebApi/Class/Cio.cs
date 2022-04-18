using LFDeliveryAPP_WebApi.Class.DTOs;
using LFDeliveryAPP_WebApi.Model.Dispatch;
using LFDeliveryAPP_WebApi.Model.Other;
using LFDeliveryAPP_WebApi.Model.SQL_Ex;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.Class
{
    public class Cio
    {
        public string request { get; set; }
        public string LastErrorMessage { get; set; }
        public DTODispatch DTODispatch { get; set; }
        public List<IFormFile> AttahmentFile;
        public List<OINV_Ex> OINVs { get; set; }
        public List<INV1_Ex> INV1s { get; set; }
        public UploadModel uploadFile { get; set; }
        public List<Guid> Guids { get; set; }
        public string QueryUser { get; set; }
        public List<string> QueryDocEntries { get; set; }
        public string QueryDocEntry { get; set; }
        public string QueryDriver { get; set; }
    }
}
