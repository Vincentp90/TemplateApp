using System.ComponentModel.DataAnnotations;


namespace DataAccess.Users
{
    public class User
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public Guid UUID { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

        [Required]
        public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

        public DateTime CreateDate { get; set; } = DateTime.UtcNow;

        public string Role { get; set; } = "User";
    }
}
