using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using Domain.ValueObjects;

namespace Domain
{
    // User is aggregate root for User + UserDetails
    // In a real production app they should probably be separate roots because auth performance is much better when we can query User from a single table instead of joining User + UserDetails
    // I have no complex enough other domain models, so we keep it like this as an example of aggregate root
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

        public void UpdateDetails(FullName name, Address location)
        {
            Details.Update(name, location);
        }
    }
    
    public class UserDetails
    {
        public FullName Name { get; private set; } = new(null, null);
        public Address Location { get; private set; } = new(null, null, null);

        public uint RowVersion { get; private set; }

        public UserDetails() { }

        public UserDetails(FullName name, Address address, uint rowVersion)
        {
            Name = name;
            Location = address;
            RowVersion = rowVersion;
        }

        public void Update(FullName name, Address address)
        {
            Name = name;
            Location = address;
        }
    }
}
