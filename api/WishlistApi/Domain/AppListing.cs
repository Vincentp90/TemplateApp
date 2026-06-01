using System;
using System.Collections.Generic;
using System.Text;

namespace Domain
{
    public class AppListing
    {
        public int Id { get; private set; }
        public string Name { get; private set; }

        public AppListing(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
