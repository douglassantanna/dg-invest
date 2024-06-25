using api.Shared;
using FluentValidation;
using MediatR;

namespace api.Users.Commands;
public record UpdateUserPasswordCommand(int UserId, string CurrentPassword, string NewPassword, string ConfirmNewPassword) : IRequest<Response>;
public class UpdateUserPasswordCommandValidator : AbstractValidator<UpdateUserPasswordCommand>
{
    public UpdateUserPasswordCommandValidator()
    {

    }
}
public class UpdateUserPasswordCommandHandler : IRequestHandler<UpdateUserPasswordCommand, Response>
{
    public Task<Response> Handle(UpdateUserPasswordCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}