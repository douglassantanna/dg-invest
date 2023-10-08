using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using api.Data;
using api.Shared;
using api.SpotSolar.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.SpotSolar.Commands;
public record GetProposalById(int ProposalId) : IRequest<Response>;
public class GetProposalByIdHandler : IRequestHandler<GetProposalById, Response>
{
    private readonly DataContext _content;

    public GetProposalByIdHandler(DataContext content)
    {
        _content = content;
    }

    public async Task<Response> Handle(GetProposalById request, CancellationToken cancellationToken)
    {
        var proposal = await _content.Proposals.FirstOrDefaultAsync(x => x.Id == request.ProposalId);
        if (proposal is null)
            return new Response("Proposal not found", false);

        List<ProductDto> products = proposal.Products
                                            .Select(x => new ProductDto(x.Name, x.Quantity))
                                            .ToList();

        var proposalDto = new ProposalDto(
            proposal.Id,
            new CustomerDto(proposal.Customer.Name,
                            proposal.Customer.Email,
                            proposal.Customer.Phone),
            new AddressDto(proposal.Address.Street,
                           proposal.Address.Number,
                           proposal.Address.City,
                           proposal.Address.State,
                           proposal.Address.ZipCode),
            products,
            proposal.WarrantyType,
            proposal.ServiceType,
            proposal.WarrantyQtd,
            proposal.ExcecutionTime,
            proposal.PaymentMethods,
            proposal.TotalPriceProducts,
            proposal.LabourValue,
            proposal.TotalPrice,
            proposal.Power,
            proposal.Notes);

        return new Response("", true, proposalDto);
    }
}
