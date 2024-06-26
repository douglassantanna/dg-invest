using System.Net;
using api.Shared;
using api.Users.Repositories;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace api.Users.Commands;
public record UpdateUserProfileCommand(int UserId, string Fullname, string Email) : IRequest<Response>;
public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
  public UpdateUserProfileCommandValidator()
  {
    RuleFor(x => x.Fullname)
              .NotNull().WithMessage("FullName can't be null.")
              .NotEmpty().WithMessage("FullName can't be empty.")
              .Length(0, 255).WithMessage("Name must contain 0 to 255 characters.");

    RuleFor(x => x.Email)
        .EmailAddress().WithMessage("E-mail can't be empty.")
        .Length(0, 255).WithMessage("E-mail must contain 0 to 255 characters.");
  }
}
public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, Response>
{
  private readonly IUserRepository _userRepository;
  private readonly ILogger<UpdateUserProfileCommandHandler> _logger;

  public UpdateUserProfileCommandHandler(IUserRepository userRepository, ILogger<UpdateUserProfileCommandHandler> logger)
  {
    _userRepository = userRepository;
    _logger = logger;
  }

  public async Task<Response> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
  {
    var validationResult = await ValidateRequestAsync(request);
    if (!validationResult.IsValid)
    {
      var errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
      _logger.LogInformation("UpdateUserProfileCommandHandler. Validation failed: {0}", errors);
      return new Response("Validation failed!", false, new { validationErrors = validationResult.Errors.Select(x => x.ErrorMessage).ToList(), HttpStatusCode = HttpStatusCode.BadRequest });
    }

    var user = await _userRepository.GetByIdAsync(request.UserId);
    if (user == null)
    {
      _logger.LogInformation("UpdateUserProfileCommandHandler. User not found: {0}", request.UserId);
      return new Response("User not found!", false, new { HttpStatusCode = HttpStatusCode.NotFound });
    }

    try
    {
      user.Update(request.Fullname, request.Email);
      await _userRepository.UpdateAsync(user);
      _logger.LogInformation("UpdateUserProfileCommandHandler. Profile updated for user: {0}", request.UserId);
    }
    catch (Exception ex)
    {
      _logger.LogInformation("UpdateUserProfileCommandHandler. Error while updating user profile: {0}. Error:{1}", request.UserId, ex.Message);
      return new Response("An error occured while updating your profile. Please try again!", false, new { HttpStatusCode = HttpStatusCode.BadRequest });
    }

    return new Response("Profile updated successfully.", true);
  }
  private async Task<ValidationResult> ValidateRequestAsync(UpdateUserProfileCommand request)
  {
    var validation = new UpdateUserProfileCommandValidator();
    return await validation.ValidateAsync(request);
  }
}
