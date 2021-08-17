using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace IceCreamRatingsService.Models
{
	public class User
	{
		[JsonProperty("userId")]
		public Guid UserId { get; set; }
		[JsonProperty("userName")]
		public String UserName { get; set; }
		[JsonProperty("fullName")]
		public String FullName { get; set; }
	}
}
