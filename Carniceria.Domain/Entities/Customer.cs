using Carniceria.Domain.Common;
using System.Drawing;

namespace Carniceria.Domain.Entities;

public class Customer : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? Address { get; private set; }          
    public decimal DiscountPercent { get; private set; }  
    public bool IsActive { get; private set; } = true;
    public string Color { get; private set; } = "#6366f1";
    public string? Emoji { get; private set; }


    private Customer() { }

    public static Customer Create(string name, string? phone, string? address,
        decimal discountPercent, string color = "#6366f1", string? emoji = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Customer name is required.");
        if (discountPercent < 0 || discountPercent > 100)
            throw new DomainException("Discount must be between 0 and 100.");

        return new Customer
        {
            Name = name,
            Phone = phone,
            Address = address,
            DiscountPercent = discountPercent,
            Color = color,
            Emoji = emoji
        };
    }

    public void Update(string name, string? phone, string? address, 
        decimal discountPercent, string color, string? emoji)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Customer name is required.");
        if (discountPercent < 0 || discountPercent > 100)
            throw new DomainException("Discount must be between 0 and 100.");

        Name = name;
        Phone = phone;
        Address = address;
        DiscountPercent = discountPercent;
        Color = color; 
        Emoji = emoji; 
        SetUpdated();
    }

    public void Deactivate() { IsActive = false; SetUpdated(); }

    public void TouchActivity()
    {
        SetUpdated(); 
    }
}