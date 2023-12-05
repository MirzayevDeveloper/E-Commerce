
using E_Commerce.Bot.Configurations.Log;
using E_Commerce.Bot.Configurations.Webhook;
using E_Commerce.Bot.Helpers;
using E_Commerce.Bot.Models;
using E_Commerce.Bot.Persistence;
using E_Commerce.Bot.Persistence.Interceptor;
using E_Commerce.Bot.Persistence.Options;
using E_Commerce.Bot.Services.Updates;
using E_Commerce.Bot.Services.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using Telegram.Bot;

namespace E_Commerce.Bot
{
	public class Program
	{
		private const string DataBaseConfigurationOptions = "DataBaseOptions";

		public static void Main(string[] args)
		{
			AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

			var builder = WebApplication.CreateBuilder(args);

			var botConfiguration = new BotConfiguration();
			builder.Configuration.Bind("BotConfiguration", botConfiguration);

			builder.Services.ConfigureOptions<DataBaseOptionsSetup>();

			builder.Services.Configure<DataBaseOptions>(
				builder.Configuration.GetSection(DataBaseConfigurationOptions));

			Log.Logger = ConfigureLogs.SetLogConfiguration(builder.Configuration)
			   .CreateLogger();

			RegisterCors(builder.Services);
			builder.Host.UseSerilog();

			builder.Services.AddHttpClient("ecommerce")
				.AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
				{
					var options = new TelegramBotClientOptions(botConfiguration.Token);

					return new TelegramBotClient(options, httpClient);
				});

			builder.Services.AddScoped<AuditableEntitySaveChangesInterceptor>();
			builder.Services.AddScoped<HandleUpdateService>();
			builder.Services.AddHostedService<ConfigureWebhook>();

			builder.Services.AddDbContext<ApplicationDbContext>(
				(serviceProvider, dbContextOptionsBuilder) =>
				{
					var dataBaseOptions = serviceProvider
						.GetService<IOptions<DataBaseOptions>>()!.Value;

					dbContextOptionsBuilder.UseNpgsql(dataBaseOptions.ConnectionString,
						builder =>
						{
							builder.EnableRetryOnFailure(dataBaseOptions.MaxRetryCount);
							builder.CommandTimeout(dataBaseOptions.CommandTimeOut);
						});

					dbContextOptionsBuilder.EnableDetailedErrors(
						dataBaseOptions.EnableDetailedErrors);

					dbContextOptionsBuilder.EnableSensitiveDataLogging(
						dataBaseOptions.EnableSensitiveDataLogging);
				});

			builder.Services.AddMemoryCache();
			builder.Services.AddScoped<TokenService>();
			builder.Services.AddScoped<IUserService, UserService>();
			builder.Services.AddControllers().AddNewtonsoftJson();

			var app = builder.Build();

			if (app.Environment.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseRouting();

			app.UseCors("AnyOrigin");

			app.MapControllerRoute(
				name: "ecommerce",
				pattern: $"bot/{botConfiguration.Token}",
				new { controller = "Bot", action = "Post" });

			app.MapControllers();

			app.Run();
		}

		private static void RegisterCors(IServiceCollection services)
		{
			services.AddCors(options =>
			{
				options.AddPolicy(
					"AnyOrigin",
					builder =>
					{
						builder
							.AllowAnyOrigin()
							.AllowAnyMethod()
							.AllowAnyHeader();
						//.WithExposedHeaders("Content-Disposition");
					}
				);
			});
		}
	}
}
