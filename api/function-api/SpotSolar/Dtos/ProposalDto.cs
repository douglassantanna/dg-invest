using System.Collections.Generic;
using function_api.SpotSolar.Models;

namespace function_api.SpotSolar.Dtos;
public record ProposalDto(CustomerDto Customer,
                          AddressDto Address,
                          ProductDto Product,
                          EWarrantyType WarrantyType,
                          EServiceType ServiceType,
                          int WarrantyQtd,
                          int ExcecutionTime,
                          List<string> PaymentMethods,
                          int TotalPriceProducts,
                          int LabourValue,
                          int TotalPrice,
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