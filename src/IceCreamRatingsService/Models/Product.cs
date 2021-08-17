using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace IceCreamRatingsService.Models
{
	public class Product
	{
		[JsonProperty("productId")]
		public Guid ProductId { get; set; }
		[JsonProperty("productName")]
		public String ProductName { get; set; }
		[JsonProperty("productDescription")]
		public String ProductDescription { get; set; }
	}
}
