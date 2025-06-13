namespace Application.DTOs
{
    public class UpdateUserRequest
    {
        public string Name { get; set; } = default!;
        public string Password { get; set; } = default!;
    }
}
