using System.Text.RegularExpressions;

namespace E_Commerce.Bot.Helpers
{
	public static class PhoneMatchRegex
	{
		private static Regex regex = new Regex("998[0-9]{9}", RegexOptions.IgnoreCase);
		public static bool Check(string phone)
		{
			return phone.Length == 12 && regex.Match(phone).Success;
		}
	}
}
