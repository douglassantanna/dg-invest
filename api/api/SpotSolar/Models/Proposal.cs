using System;
using System.Collections.Generic;

namespace api.SpotSolar.Models;
public class Proposal
{
    public int Id { get; set; }
    public Customer Customer { get; set; } = new Customer();
    public Address Address { get; set; } = new Address();
    public List<Product> Products { get; set; } = new List<Product>();
    public EServiceType ServiceType { get; set; }
    public EWarrantyType WarrantyType { get; set; }
    public int WarrantyQtd { get; set; } = 0;
    public int ExcecutionTime { get; set; } = 0;
    public string Power { get; set; } = string.Empty;
    public decimal TotalPriceProducts { get; set; } = 0;
    public decimal LabourValue { get; set; } = 0;
    public decimal TotalPrice { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string PaymentMethods { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public decimal SumPriceProductsAndLabourValue()
    {
        return LabourValue + TotalPriceProducts;
    }
}
