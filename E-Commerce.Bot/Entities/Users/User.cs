using E_Commerce.Bot.Entities.Addresses;
using E_Commerce.Bot.Entities.BaseEntities;

namespace E_Commerce.Bot.Entities.Users
{
	public class User : BaseAuditableEntity
	{
		public string FirstName { get; set; } = string.Empty;
		public string PhoneNumber { get; set; } = string.Empty;
		public long TelegramChatId { get; set; }
		public Language Language { get; set; }
		public bool IsVerified { get; set; } = true;
		public int? SmsCode { get; set; }
		public DateTimeOffset? SmsExpiredTime { get; set; }
		public virtual ICollection<Address>? Addresses { get; set; }
	}
}
