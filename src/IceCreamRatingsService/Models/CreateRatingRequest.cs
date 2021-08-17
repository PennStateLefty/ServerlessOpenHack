using System;
using System.Collections.Generic;
using System.Text;

namespace IceCreamRatingsService.Models
{
    public class CreateRatingRequest
    {
        public Guid UserId { get; set; }
        public Guid ProductId { get; set; }
        public string LocationName { get; set; }
        public int Rating { get; set; }
        public string UserNotes { get; set; }
    }
}
