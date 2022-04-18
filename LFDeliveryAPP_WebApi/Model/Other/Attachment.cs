using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.Model.Other
{
    public class Attachment
    {
        public int id { get; set; }
        public string fileSavePath { get; set; }
        public string fileExtension { get; set; }
        public string headerGuid { get; set; } 
        public string fileName { get; set; }
        public string fileSize { get; set; }
        public DateTime createDt { get; set; }
        public string fileGuid { get; set; } 
        public string pictureType { get; set; }
    }
}
