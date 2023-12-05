using Microsoft.Extensions.Options;

namespace E_Commerce.Bot.Persistence.Options
{
	public class DataBaseOptionsSetup : IConfigureOptions<DataBaseOptions>
	{
		private const string DataBaseConfigurationOptions = "DataBaseOptions";
		private readonly IConfiguration configuration;

		public DataBaseOptionsSetup(IConfiguration configuration) =>
			this.configuration = configuration;

		public void Configure(DataBaseOptions options)
		{
			var connectionString = this.configuration
				.GetConnectionString("DefaultConnection");

			options.ConnectionString = connectionString!;
			this.configuration.Bind(DataBaseConfigurationOptions, options);
		}
	}
}
