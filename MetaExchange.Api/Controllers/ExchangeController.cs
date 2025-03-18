using MetaExchange.Core;
using MetaExchange.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace MetaExchange.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExchangeController(IMetaExchange metaExchange, IExchangeLoader exchangeLoader) : ControllerBase
{
    [HttpPost("execute")]
    public ActionResult<List<ExecutionOrder>> ExecuteOrder([FromBody] OrderRequest request)
    {
        try
        {
            var exchanges = exchangeLoader.LoadExchanges();
            var orders = metaExchange.ProcessOrder(exchanges, request);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}