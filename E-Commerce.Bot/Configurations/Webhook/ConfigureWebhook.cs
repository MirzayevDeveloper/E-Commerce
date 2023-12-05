using E_Commerce.Bot.Models;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace E_Commerce.Bot.Configurations.Webhook
{
	public class ConfigureWebhook : IHostedService
	{
		private readonly IServiceProvider serviceProvider;
		private readonly BotConfiguration botConfiguration;

		public ConfigureWebhook(
			IServiceProvider serviceProvider,
			IConfiguration configuration)
		{
			this.serviceProvider = serviceProvider;
			this.botConfiguration = new BotConfiguration();
			configuration.Bind("BotConfiguration", this.botConfiguration);
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			using var scope = this.serviceProvider.CreateScope();

			var botClient = scope.ServiceProvider
				.GetRequiredService<ITelegramBotClient>();

			var webhookAddress =
				$"{this.botConfiguration.HostAddress}/bot/{this.botConfiguration.Token}";

			await botClient.SendTextMessageAsync(
				chatId: 990118828,
				text: "Bot starting...");

			await botClient.SetWebhookAsync(
				url: webhookAddress,
				allowedUpdates: Array.Empty<UpdateType>(),
				cancellationToken: cancellationToken);
		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			using var scope = this.serviceProvider.CreateScope();

			var botClient = scope.ServiceProvider
				.GetRequiredService<ITelegramBotClient>();

			await botClient.SendTextMessageAsync(
				chatId: 990118828,
				text: "Bot finishing...");

			await botClient.DeleteWebhookAsync(cancellationToken: cancellationToken);
		}
	}
}
