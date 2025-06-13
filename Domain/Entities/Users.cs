namespace Domain.Entities
{
    public class Users
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
        public string Role { get; set; } = "user"; // o "admin"
        public bool EmailConfirmed { get; set; } = false;
        public ICollection<Book> Books { get; set; } = new List<Book>();

    }
}
