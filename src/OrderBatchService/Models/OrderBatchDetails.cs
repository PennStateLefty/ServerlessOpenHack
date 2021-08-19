using System;
using System.Collections.Generic;
using System.Text;

namespace OrderBatchService.Models
{
    public class OrderBatchDetails
    {
        public string BatchId {get;set;}
        public string File { get; set; }
        public string FileName { get; set; }
    }
}
