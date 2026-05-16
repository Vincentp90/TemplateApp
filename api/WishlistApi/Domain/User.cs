using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;

namespace Domain
{
    public class User
    {
        public int Id { get; }
        public string Username { get; }
        public Guid UUID { get; } = Guid.NewGuid();
        public byte[] PasswordHash { get; }
        public byte[] PasswordSalt { get; }
        public string Role { get; set; } = "User";
        public UserDetails Details { get; }

        public User(string username, byte[] passwordHash, byte[] passwordSalt)
        {
            Username = username;
            PasswordHash = passwordHash;
            PasswordSalt = passwordSalt;
            Details = new UserDetails();
        }

        public User(int id, string username, Guid uuid, byte[] passwordHash, byte[] passwordSalt, string role, UserDetails details)
        {
            Id = id;
            Username = username;
            UUID = uuid;
            PasswordHash = passwordHash;
            PasswordSalt = passwordSalt;
            Role = role;
            Details = details;
        }

        public void UpdateDetails(string? firstName, string? lastName, string? country, string? city, string? address)
        {
            Details.Update(firstName, lastName, country, city, address);
        }
    }

    public class UserDetails
    {
        public string? FirstName { get; private set; }
        public string? LastName { get; private set; }
        public string? Country { get; private set; }
        public string? City { get; private set; }
        public string? Address { get; private set; }//TODO as value object

        public uint RowVersion { get; private set; }

        public UserDetails() { }

        public UserDetails(
            string? firstName,
            string? lastName,
            string? country,
            string? city,
            string? address,
            uint rowVersion)
        {
            FirstName = firstName;
            LastName = lastName;
            Country = country;
            City = city;
            Address = address;
            RowVersion = rowVersion;
        }

        public void Update(string? firstName, string? lastName, string? country, string? city, string? address)
        {
            FirstName = firstName;
            LastName = lastName;
            Country = country;
            City = city;
            Address = address;
        }
    }
}
