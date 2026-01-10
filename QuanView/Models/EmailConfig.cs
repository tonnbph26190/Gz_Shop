namespace QuanView.Models
{
    public class EmailConfig
    {
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public bool UseSSL { get; set; } = true;
        public string SenderEmail { get; set; } = "";
        public string SenderName { get; set; } = "Cửa hàng Quần Áo";
        public string Username { get; set; } = "";   
        public string Password { get; set; } = "";   
    }

}
