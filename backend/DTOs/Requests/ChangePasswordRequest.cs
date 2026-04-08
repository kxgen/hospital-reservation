namespace Backend.Dtos.Requests
{
    public class ChangePasswordRequest
    {
        public string Current { get; set; } = string.Empty;
        public string New { get; set; } = string.Empty;
    }
}
