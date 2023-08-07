using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/crypto-tracker")]
public class CryptoTrackerController : ControllerBase
{
    private readonly ILogger<CryptoTrackerController> _logger;

    public CryptoTrackerController(ILogger<CryptoTrackerController> logger)
    {
        _logger = logger;
    }
}
