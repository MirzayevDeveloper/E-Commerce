namespace E_Commerce.Bot.Persistence.Options
{
	public class DataBaseOptions
	{
		public required string ConnectionString { get; set; }
		public int MaxRetryCount { get; set; }
		public int CommandTimeOut { get; set; }
		public bool EnableDetailedErrors { get; set; }
		public bool EnableSensitiveDataLogging { get; set; }
	}
}
