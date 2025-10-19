using System.ComponentModel.DataAnnotations;


namespace DataAccess.Users
{
    public class User
    {
        [Key]
        public int id { get; set; }

        [Required]
        public Guid uuid { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public string username { get; set; } = string.Empty;

        [Required]
        public byte[] passwordhash { get; set; } = Array.Empty<byte>();

        [Required]
        public byte[] passwordsalt { get; set; } = Array.Empty<byte>();

        public DateTime createdat { get; set; } = DateTime.UtcNow;

        public string role { get; set; } = "User";
    }
}
