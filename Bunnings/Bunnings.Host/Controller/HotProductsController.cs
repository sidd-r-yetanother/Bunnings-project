using Bunnings.Application.Interface;
using Bunnings.Application.RequestDto;
using Microsoft.AspNetCore.Mvc;

namespace Bunnings.Application.Controller;

[ApiController]
[Route("api/[controller]")]
public class HotProductsController : ControllerBase
{
    private readonly ILogger<HotProductsController> _logger;
    private readonly IHotProductCalculator _calculator;

    public HotProductsController(ILogger<HotProductsController> logger, IHotProductCalculator calculator)
    {
        _logger = logger;
        _calculator = calculator;
    }

    [HttpPost]
    public async Task<IActionResult> GetHotProduct([FromForm] HotProductRequestDto request)
    {
        try
        {
            return Ok(await _calculator.Calculate(
                request.OrdersFile.OpenReadStream(), 
                request.ProductsFile.OpenReadStream()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate hot products.");
            return Problem("An error occurred while calculating hot products.");
        }
    }
}