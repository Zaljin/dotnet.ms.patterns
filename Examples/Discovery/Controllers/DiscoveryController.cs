using Discovery.Validation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace Discovery.Controllers;

[ApiController]
[Route("[controller]")]
public partial class DiscoveryController : ControllerBase
{
    private readonly IDiscoveryService _discoveryService;
    private readonly ILogger<DiscoveryController> _logger;

    public DiscoveryController(IDiscoveryService discoveryService,
                               ILogger<DiscoveryController> logger)
    {
        _discoveryService = discoveryService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<DiscoveryAPIModel> Get([FromQuery] GetDiscoveryInputModel item)
    {
        var validator = new GetDiscoveryValidator();
        var validationResult = await validator.ValidateAsync(item);
        VerifyModelInvalid(validationResult);

        var model = _discoveryService.GetDiscovery(item.Name, item.Version);

        var apiModel = new DiscoveryAPIModel()
        {
            Name = model.Name,
            Version = model.Version,
            Endpoint = model.Endpoint
        };

        return apiModel;
    }

    [HttpPut]
    public async Task Put(UpdateDiscoveryInputModel item)
    {
        var validator = new UpdateDiscoveryValidator();
        var validationResult = await validator.ValidateAsync(item);
        VerifyModelInvalid(validationResult);

        var model = new DiscoveryModel()
        {
            Name = item.Name,
            Version = item.Version,
            Endpoint = item.Endpoint
        };

        _discoveryService.AddDiscovery(model);
    }

    private void VerifyModelInvalid(ValidationResult? result)
    {
        if (result == null)
        {
            throw new ArgumentException();
        }

        if (!result.IsValid)
        {
            var error = result.Errors.First();
            throw new ArgumentException(error.ErrorMessage);
        }
    }
}
