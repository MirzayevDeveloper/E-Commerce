using System.Text.Json;

namespace E_Commerce.Bot.Services.Sms
{
	public class SmsService
	{
		private static string message = "E-Commerce sms code: {0}";
		public static async Task<string> GetToken(string email, string password)
		{
			var client = new HttpClient();
			var request = new HttpRequestMessage(HttpMethod.Post, "https://notify.eskiz.uz/api/auth/login");
			var content = new MultipartFormDataContent();
			content.Add(new StringContent(email), "email");
			content.Add(new StringContent(password), "password");
			request.Content = content;
			var response = await client.SendAsync(request);
			response.EnsureSuccessStatusCode();
			string jsonContent = await response.Content.ReadAsStringAsync();

			var body = JsonSerializer.Deserialize<AuthBody>(jsonContent);

			return body?.data?.token ?? "";
		}

		public static async Task SendSms(string token, string phoneNumber, string code)
		{
			var client = new HttpClient();
			var request = new HttpRequestMessage(HttpMethod.Post, "https://notify.eskiz.uz/api/message/sms/send");
			request.Headers.Add("Authorization", $"Bearer {token}");
			var content = new MultipartFormDataContent();
			content.Add(new StringContent(phoneNumber), "mobile_phone");
			content.Add(new StringContent(string.Format(message, code)), "message");
			content.Add(new StringContent("4546"), "from");
			content.Add(new StringContent("http://0000.uz/test.php"), "callback_url");
			request.Content = content;
			var response = await client.SendAsync(request);
			response.EnsureSuccessStatusCode();
		}
	}

	public class Data
	{
		public string token { get; set; }
	}

	public class AuthBody
	{
		public string message { get; set; }
		public Data data { get; set; }
		public string token_type { get; set; }
	}
}
