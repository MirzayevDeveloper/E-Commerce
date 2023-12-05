using E_Commerce.Bot.Entities.BaseEntities;
using E_Commerce.Bot.Entities.Users;

namespace E_Commerce.Bot.Entities.Addresses
{
	public class Address : BaseAuditableEntity
	{
		public Guid UserId { get; set; }
		public virtual User? User { get; set; }
		public double Longitude { get; set; }
		public double Latitude { get; set; }
		public bool IsActive { get; set; } = false;
	}
}
