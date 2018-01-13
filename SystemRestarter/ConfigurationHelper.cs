using Microsoft.Extensions.Configuration;

namespace SystemRestarter
{
	class ConfigurationHelper
	{
		public static IConfiguration GetConfig()
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(System.AppContext.BaseDirectory)
				.AddJsonFile("config.json",
				optional: true,
				reloadOnChange: true);

			return builder.Build();
		}
	}
}
