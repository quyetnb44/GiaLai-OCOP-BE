using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GiaLaiOCOP.Api.Models
{
    public class Wallet
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Số dư không được âm")]
        public decimal Balance { get; set; } = 0;

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "VND";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
    }
}

