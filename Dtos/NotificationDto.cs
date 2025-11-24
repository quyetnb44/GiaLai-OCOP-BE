using System;

namespace GiaLaiOCOP.Api.Dtos
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool Read { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Link { get; set; }
        public int? EnterpriseId { get; set; }
        public int? UserId { get; set; }
        public int? ProductId { get; set; }
        public int? OrderId { get; set; }
        public string? ProductName { get; set; }
        public string? OrderCode { get; set; }
    }
}

