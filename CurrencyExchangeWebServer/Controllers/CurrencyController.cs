using Microsoft.AspNetCore.Mvc;
using BankOfIsrael.Service;

[ApiController]
[Route("[controller]")]
public class CurrencyController : ControllerBase
{
    private readonly ILogger<CurrencyController> _logger;

    public CurrencyController(ILogger<CurrencyController> logger)
    {
        _logger = logger;
    }

    [HttpGet("Exchange")]
    public async Task<IActionResult> Get([FromQuery] string from, [FromQuery] string to, [FromQuery] int amount = 1)
    {
        var result = await CurrencyExchange.ConvertAsync(from, to, amount);

        if (result == null)
        {
            return NotFound($"No exchange information found for {from} to {to}.");
        }
        
        var midnightTomorrow = DateTime.Today.AddDays(1).AddDays(1);
        var durationUntilMidnightTomorrow = midnightTomorrow.Subtract(DateTime.Now);
        
        var maxAge = (int)durationUntilMidnightTomorrow.TotalSeconds;
        Response.Headers["Cache-Control"] = $"public, max-age={maxAge}";

        return Ok(result);
    }

    [HttpGet("Rates")]
    public async Task<IActionResult> GetExchangeRate()
    {
        var rate = await CurrencyExchange.GetRatesAsync();

        return Ok(rate);
    }
}