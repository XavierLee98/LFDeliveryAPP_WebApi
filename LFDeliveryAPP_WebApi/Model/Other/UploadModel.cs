using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.Model.Other
{
    public class UploadModel
    {
        public string User { get; set; }
        public List<Guid> Guids { get; set; }
        public List<IFormFile> files { get; set; }
    }
}
