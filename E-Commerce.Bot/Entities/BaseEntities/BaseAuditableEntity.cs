namespace E_Commerce.Bot.Entities.BaseEntities
{
	public class BaseAuditableEntity : BaseEntity
	{
		public DateTimeOffset CreatedDate { get; set; }
		public DateTimeOffset UpdatedDate { get; set; }
	}
}
