using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SharePointAddIn1Web.DataModels
{
    public class SharprAddRequest
    {
        public string ref_guid {get;set;}
        public string filename { get; set; }
        public long file_size { get; set; }
        public byte[] data { get; set; }
        public string classification { get; set; }

    }
}