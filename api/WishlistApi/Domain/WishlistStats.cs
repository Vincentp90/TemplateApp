using System;
using System.Collections.Generic;
using System.Text;

namespace Domain
{
    //TODO this is more like a dto, move to application
    public record WishlistStats
    {
        public TimeSpan AvgTimeAdded { get; set; }
        public TimeSpan AvgTimeBetweenAdded { get; set; }
        public required string OldestItem { get; set; }
        public required string MostCommonCharacter { get; set; }
    }
}
