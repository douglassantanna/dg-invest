using System.Collections.Generic;
using function_api.SpotSolar.Models;

namespace function_api.SpotSolar.Dtos;
public record ProposalDto(int Id,
                          CustomerDto Customer,
                          AddressDto Address,
                          List<ProductDto> Products,
                          EWarrantyType WarrantyType,
                          EServiceType ServiceType,
                          int WarrantyQtd,
                          int ExcecutionTime,
                          string PaymentMethods,
                          decimal TotalPriceProducts,
                          decimal LabourValue,
                          decimal TotalPrice,
                          string? Power = null,
                          string? Notes = null);

public record CustomerDto(string Name,
                          string Email,
                          string Phone);

public record AddressDto(string Street,
                         string Number,
                         string City,
                         string State,
                         string ZipCode);

public record ProductDto(string Name, int Quantity);