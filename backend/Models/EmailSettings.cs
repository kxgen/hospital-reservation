namespace Backend.Models
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string EmailPassword { get; set; } = string.Empty;
        public List<string> AllowedEmails { get; set; } = new();
    }
}
