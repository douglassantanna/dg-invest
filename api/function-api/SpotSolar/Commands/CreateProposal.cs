using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using function_api.Data;
using function_api.Shared;
using function_api.SpotSolar.Dtos;
using function_api.SpotSolar.Models;
using MediatR;

namespace function_api.SpotSolar.Commands;

public record CreateProposal(CustomerDto Customer,
                             AddressDto Address,
                             List<ProductDto> Products,
                             EWarrantyType WarrantyType,
                             EServiceType ServiceType,
                             int WarrantyQtd,
                             int ExcecutionTime,
                             string PaymentMethods,
                             int TotalPriceProducts,
                             int LabourValue,
                             int TotalPrice,
                             string? Power = null,
                             string? Notes = null) : IRequest<Response>;

public class CreateProposalHandler : IRequestHandler<CreateProposal, Response>
{
    private readonly DataContext _context;
    public CreateProposalHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Response> Handle(CreateProposal request, CancellationToken cancellationToken)
    {
        var proposal = new Proposal()
        {
            Customer = new() { },
            Address = new() { },
            Products = request.Products.Select(x => new Product { Name = x.Name, Quantity = x.Quantity }).ToList(),
            ServiceType = request.ServiceType,
            WarrantyType = request.WarrantyType,
            WarrantyQtd = request.WarrantyQtd,
            ExcecutionTime = request.ExcecutionTime,
            Power = request.Power,
            TotalPriceProducts = request.TotalPriceProducts,
            LabourValue = request.LabourValue,
            TotalPrice = request.TotalPrice,
            Notes = request.Notes,
            PaymentMethods = request.PaymentMethods
        };

        await _context.AddAsync(proposal);
        return new Response("ok", true);
    }
}
