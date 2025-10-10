﻿namespace GiaLaiOCOP.Api.Models;

public class Transaction
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string PaymentMethod { get; set; } = "Cash";
}
