using System;

namespace PosSalesProcessor
{
    public class Order
    {
        public Header header { get; set; }
        public Details[] details { get; set; }
    }

    public class Header
    {
        public string salesNumber { get; set; }
        public DateTime dateTime { get; set; }
        public string locationId { get; set; }
        public string locationName { get; set; }
        public string locationAddress { get; set; }

        public string locationPostcode { get; set; }
        public Decimal totalCost { get; set; }
        public Decimal totalTax { get; set; }
        public string receiptUrl { get; set; }
    }

    public class Details
    {
        public string productId { get; set; }
        public int quantity { get; set; }
        public Decimal unitCost { get; set; }
        public Decimal totalCost { get; set; }
        public Decimal totalTax { get; set; }
        public string productName { get; set; }
        public string productDescription { get; set; }
    }
}