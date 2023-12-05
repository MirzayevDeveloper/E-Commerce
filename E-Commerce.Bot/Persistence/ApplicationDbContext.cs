using E_Commerce.Bot.Entities.Addresses;
using E_Commerce.Bot.Entities.Users;
using E_Commerce.Bot.Persistence.Interceptor;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Bot.Persistence
{
	public class ApplicationDbContext : DbContext
	{
		private readonly AuditableEntitySaveChangesInterceptor interceptor;

		public ApplicationDbContext(
			DbContextOptions<ApplicationDbContext> options,
			AuditableEntitySaveChangesInterceptor interceptor) : base(options)
		{
			this.interceptor = interceptor;
		}

		public DbSet<User> Users { get; set; }
		public DbSet<Address> Addresses { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<User>()
				.HasIndex(u => u.TelegramChatId)
				.IsUnique();

			modelBuilder.Entity<User>()
				.HasIndex(u => u.PhoneNumber)
				.IsUnique();

			modelBuilder.Entity<Address>()
				.HasOne<User>(d => d.User)
				.WithMany(u => u.Addresses)
				.HasForeignKey(d => d.UserId);

			base.OnModelCreating(modelBuilder);
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.AddInterceptors(this.interceptor);

			base.OnConfiguring(optionsBuilder);
		}
	}
}
