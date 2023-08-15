using function_api.SpotSolar.Dtos;
using MediatR;

namespace function_api.SpotSolar.Commands;
public record GetProposalById(int ProposalId) : IRequest<ProposalDto>;
