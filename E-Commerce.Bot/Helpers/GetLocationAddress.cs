using Newtonsoft.Json.Linq;
using RestSharp;

namespace E_Commerce.Bot.Helpers
{
	public class GetLocationAddress
	{
		public static string GetAddress(double lat, double lon)
		{
			string key = "AIzaSyAllxwY_m-kR7VYiSFZBcYtXmhnY6AokY0";

			string address = GetAddressFromGeocodingAPI(lat, lon, key);

			return address;
		}

		static string GetAddressFromGeocodingAPI(double latitude, double longitude, string apiKey)
		{
			string apiUrl = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={latitude},{longitude}&key={apiKey}";

			var client = new RestClient(apiUrl);
			var request = new RestRequest();

			RestResponse response = client.Execute(request);

			if (response.IsSuccessful)
			{
				JObject jsonResponse = JObject.Parse(response.Content);
				JToken results = jsonResponse["results"];

				if (results.HasValues)
				{
					string formattedAddress = results[0]["formatted_address"].ToString();
					return formattedAddress;
				}
				else
				{
					return "Address not found";
				}
			}
			else
			{
				return "Error getting address";
			}
		}

	}
}
