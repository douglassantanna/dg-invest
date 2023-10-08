using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using api.Data;
using api.Shared;
using api.SpotSolar.Dtos;
using api.SpotSolar.Models;
using MediatR;

namespace api.SpotSolar.Commands;

public record CreateProposalCommand(CustomerDto Customer,
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
                             string Power,
                             string Notes) : IRequest<Response>;

public class CreateProposalCommandValidator : AbstractValidator<CreateProposalCommand>
{
    public CreateProposalCommandValidator()
    {
        RuleFor(x => x.Customer.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(255).WithMessage("Name must be less than 255 characters");

        RuleFor(x => x.Customer.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email is invalid");

        RuleFor(x => x.Customer.Phone)
            .NotEmpty().WithMessage("Phone is required")
            .Length(11).WithMessage("Phone must be 11 characters");

        When(x => x.Address is null, () =>
        {
            RuleFor(x => x.Address.ZipCode)
                .NotEmpty().WithMessage("ZipCode is required")
                .Length(8).WithMessage("ZipCode must be 8 characters");
        });

        RuleFor(x => x.Address.Street)
            .NotEmpty().WithMessage("Street is required")
            .MaximumLength(255).WithMessage("Street must be less than 255 characters");

        RuleFor(x => x.Address.Number)
            .NotEmpty().WithMessage("Number is required")
            .MaximumLength(255).WithMessage("Number must be less than 255 characters");

        RuleFor(x => x.Address.City)
            .NotEmpty().WithMessage("City is required")
            .MaximumLength(255).WithMessage("City must be less than 255 characters");

        RuleFor(x => x.Address.State)
            .NotEmpty().WithMessage("State is required")
            .MaximumLength(255).WithMessage("State must be less than 255 characters");

        RuleFor(x => x.Address.ZipCode)
                .NotEmpty().WithMessage("ZipCode is required")
                .Length(8).WithMessage("ZipCode must be 8 characters");

        RuleFor(x => x.Products)
            .NotEmpty().WithMessage("Products is required");

        RuleFor(x => x.TotalPrice)
            .NotEmpty().WithMessage("TotalPrice is required")
            .GreaterThan(0).WithMessage("TotalPrice must be greater than 0");

        RuleFor(x => x.LabourValue)
            .NotEmpty().WithMessage("LabourValue is required")
            .GreaterThan(0).WithMessage("LabourValue must be greater than 0");

        RuleFor(x => x.TotalPriceProducts)
            .NotEmpty().WithMessage("TotalPriceProducts is required")
            .GreaterThan(0).WithMessage("TotalPriceProducts must be greater than 0");
    }
}

public class CreateProposalCommandHandler : IRequestHandler<CreateProposalCommand, Response>
{
    private readonly DataContext _context;

    public CreateProposalCommandHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Response> Handle(CreateProposalCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await ValidateRequestAsync(request);
        if (!validationResult.IsValid)
            return new Response("Validation failed", false, validationResult.Errors.Select(x => x.ErrorMessage).ToList());

        if (!CalculateTotalPrice(request))
            return new Response("TotalPrice is invalid", false);

        var products = request.Products.Select(p => new Product
        {
            Name = p.Name,
            Quantity = p.Quantity
        }).ToList();

        var proposal = new Proposal
        {
            Customer = new Customer
            {
                Name = request.Customer.Name,
                Email = request.Customer.Email,
                Phone = request.Customer.Phone
            },
            Address = new Address
            {
                Street = request.Address.Street,
                Number = request.Address.Number,
                City = request.Address.City,
                State = request.Address.State,
                ZipCode = request.Address.ZipCode
            },
            Products = products,
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

        await _context.Proposals.AddAsync(proposal);
        await _context.SaveChangesAsync();
        return new Response("ok", true, proposal.Id);
    }

    private async Task<ValidationResult> ValidateRequestAsync(CreateProposalCommand request)
    {
        var validation = new CreateProposalCommandValidator();
        return await validation.ValidateAsync(request);
    }

    private bool CalculateTotalPrice(CreateProposalCommand request)
    {
        var calculatedTotalPrice = request.TotalPriceProducts + request.LabourValue;
        const decimal tolerance = 0.0001M;

        return Math.Abs(request.TotalPrice - calculatedTotalPrice) <= tolerance;
    }
}
