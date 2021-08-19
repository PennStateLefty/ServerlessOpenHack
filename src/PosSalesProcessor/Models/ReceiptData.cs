using System;
using System.Collections.Generic;
using System.Text;

namespace PosSalesProcessor.Models
{

    public class ReceiptData
    {
        public int totalItems { get; set; }
        public decimal totalCost { get; set; }
        public string salesNumber { get; set; }
        public DateTime salesDate { get; set; }
        public string storeLocation { get; set; }
        public string receiptUrl { get; set; }
    }

}
