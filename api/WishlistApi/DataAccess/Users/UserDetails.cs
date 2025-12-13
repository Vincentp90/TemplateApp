using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DataAccess.Users
{
    public class UserDetails
    {
        [Key]
        public int ID { get; set; }

        [ForeignKey("User")]
        public int UserID { get; set; }
        public required User User { get; set; }

        [Timestamp]
        public uint RowVersion { get; set; }


        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public string? Country { get; set; }
        public string? City { get; set; }
        public string? Address { get; set; }

    }
}
