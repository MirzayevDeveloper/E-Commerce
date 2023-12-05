using E_Commerce.Bot.Services.Updates;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace E_Commerce.Bot.Controllers
{
	public class BotController : ControllerBase
	{
		[HttpPost]
		public async Task<IActionResult> Post(
			[FromBody] Update update,
			[FromServices] HandleUpdateService handleUpdateService,
			CancellationToken cancellationToken)
		{
			await handleUpdateService.EchoAsync(update, cancellationToken);

			return Ok();
		}
	}
}
