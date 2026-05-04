using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Domain
{
    public class WishlistStats
    {
        public TimeSpan AvgTimeAdded { get; set; }
        public TimeSpan AvgTimeBetweenAdded { get; set; }
        public string OldestItem { get; set; }
        public string MostCommonCharacter { get; set; }
    }
}
