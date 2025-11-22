using System;

namespace GiaLaiOCOP.Api.Dtos;

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";            // thêm Role
    public int? EnterpriseId { get; set; }            // thêm EnterpriseId (nullable)
    public EnterpriseDto? Enterprise { get; set; }    // thêm Enterprise (nullable)
    public bool IsEmailVerified { get; set; } = false; // Trạng thái xác thực email
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? ShippingAddress { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Địa chỉ chi tiết
    public int? ProvinceId { get; set; }
    public int? DistrictId { get; set; }
    public int? WardId { get; set; }
    public string? AddressDetail { get; set; }
}
