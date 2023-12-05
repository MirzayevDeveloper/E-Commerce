using SixLaborsCaptcha.Core;

namespace E_Commerce.Bot.Helpers
{

	public static class GetSmsKey
	{
		private static readonly char[] chars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

		public static string Get()
		{
			return Extensions.GetUniqueKey(4, chars);
		}
	}
}
