using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace IceCreamRatingsService.Models
{
    public class Rating
    {
        public Guid id { get; set; }
        public DateTime Timestamp { get; set; }
        public Guid UserId { get; set; }
        public Guid ProductId { get; set; }
        public string LocationName { get; set; }
        [JsonProperty("rating")]
        public int RatingValue { get; set; }
        public string UserNotes { get; set; }
    }
}
