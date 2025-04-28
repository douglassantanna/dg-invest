using System.Net;
using api.Shared.Interfaces;
using api.Shared;
using api.Users.Repositories;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace api.Users.Commands;
public record UpdateUserPasswordCommand(int UserId, string CurrentPassword, string NewPassword) : IRequest<Response>;
public class UpdateUserPasswordCommandValidator : AbstractValidator<UpdateUserPasswordCommand>
{
    public UpdateUserPasswordCommandValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotNull().WithMessage("Password field is required!")
            .NotEmpty().WithMessage("Password field cannot be empty!")
            .Length(4, 20).WithMessage("Password field must be between 4 to 20 characters!");
    }
}
public class UpdateUserPasswordCommandHandler : IRequestHandler<UpdateUserPasswordCommand, Response>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UpdateUserPasswordCommandHandler> _logger;
    private readonly IPasswordHelper _passwordHelper;

    public UpdateUserPasswordCommandHandler(
        IUserRepository userRepository,
        ILogger<UpdateUserPasswordCommandHandler> logger,
        IPasswordHelper passwordHelper)
    {
        _userRepository = userRepository;
        _logger = logger;
        _passwordHelper = passwordHelper;
    }

    public async Task<Response> Handle(UpdateUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await ValidateRequestAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
            _logger.LogError("UpdateUserPasswordCommandHandler. Validation failed: {0}", errors);
            return new Response("Validation failed!", false, new { validationErrors = validationResult.Errors.Select(x => x.ErrorMessage).ToList(), HttpStatusCode = HttpStatusCode.BadRequest });
        }

        var userResult = await _userRepository.GetByIdAsync(request.UserId);
        if (!userResult.IsSuccess)
        {
            _logger.LogError("UpdateUserPasswordCommandHandler. User not found: {0}", request.UserId);
            return new Response("User not found!", false, new { HttpStatusCode = HttpStatusCode.NotFound });
        }

        if (!_passwordHelper.VerifyPassword(request.CurrentPassword, userResult.Value.Password ?? string.Empty))
        {
            _logger.LogError("UpdateUserPasswordCommandHandler. Password for user {0} is incorrect", request.UserId);
            return new Response("Current password is incorrect", false);
        }

        try
        {
            var newEncryptedPassword = _passwordHelper.EncryptPassword(request.NewPassword);
            userResult.Value.UpdatePassword(newEncryptedPassword);
            await _userRepository.UpdateAsync(userResult.Value);
            _logger.LogInformation("UpdateUserPasswordCommandHandler. Password updated for user: {0}", request.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError("UpdateUserPasswordCommandHandler. Error while updating user password: {0}. Error:{1}", request.UserId, ex.Message);
            return new Response("An error occured while updating your password. Please try again!", false, new { HttpStatusCode = HttpStatusCode.BadRequest });
        }

        return new Response("Password updated successfully.", true);
    }
    private async Task<ValidationResult> ValidateRequestAsync(UpdateUserPasswordCommand request)
    {
        var validation = new UpdateUserPasswordCommandValidator();
        return await validation.ValidateAsync(request);
    }
}