using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain
{
    public class User
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public Guid UUID { get; set; } = Guid.NewGuid();
        public required byte[] PasswordHash { get; set; }
        public required byte[] PasswordSalt { get; set; }
        public string Role { get; set; } = "User";
    }
}
