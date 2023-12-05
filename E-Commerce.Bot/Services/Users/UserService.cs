using E_Commerce.Bot.Entities.Addresses;
using E_Commerce.Bot.Entities.Users;
using E_Commerce.Bot.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace E_Commerce.Bot.Services.Users
{
	public class UserService : IUserService
	{
		private readonly ApplicationDbContext dbContext;
		private readonly IMemoryCache cache;

		public UserService(
			ApplicationDbContext dbContext,
			IMemoryCache cache)
		{
			this.dbContext = dbContext;
			this.cache = cache;
		}

		public async Task UpdateUserAsync(User user)
		{
			this.dbContext.Users.Update(user);
			await this.dbContext.SaveChangesAsync();
		}

		public async Task<User?> GetUserByChatIdAsync(long chatId) =>
			await this.dbContext.Users.FirstOrDefaultAsync(u => u.TelegramChatId.Equals(chatId));

		public async Task<User?> GetUserByPhoneNumberAsync(string phoneNumber) =>
			await this.dbContext.Users.FirstOrDefaultAsync(u => u.PhoneNumber.Equals(phoneNumber));

		public async Task AddUserAsync(User user)
		{
			this.dbContext.Users.Add(user);
			await this.dbContext.SaveChangesAsync();
		}

		public async Task<bool?> CheckUserIsVerifiedByPhoneNumberAsync(string phoneNumber)
		{
			var maybeUser = await this.dbContext.Users.FirstOrDefaultAsync(
				u => u.PhoneNumber.Equals(phoneNumber));

			if (maybeUser is null) return null;

			return maybeUser.IsVerified;
		}

		public async Task<bool?> CheckUserIsVerifiedByChatIdAsync(long id)
		{
			if (cache.TryGetValue(id, out bool? isVerified))
			{
				return isVerified;
			}

			var maybeUser = await this.dbContext.Users.FirstOrDefaultAsync(
				u => u.TelegramChatId.Equals(id));

			if (maybeUser is null) return null;

			var cacheOptions = new MemoryCacheEntryOptions()
					.SetAbsoluteExpiration(TimeSpan.FromDays(20));

			cache.Set(id, maybeUser.IsVerified, cacheOptions);

			return maybeUser.IsVerified;
		}

		public async Task<bool> CheckSmsCodeAsync(long chatId, int smsCode)
		{
			var maybeUser = await this.dbContext.Users.FirstOrDefaultAsync(
				u => u.TelegramChatId.Equals(chatId));

			return maybeUser!.SmsCode == smsCode;
		}

		public async Task VerifyUserAsync(long chatId, bool isVerified = true)
		{
			var maybeUser = await this.dbContext.Users.FirstOrDefaultAsync(
				x => x.TelegramChatId.Equals(chatId));

			maybeUser!.IsVerified = isVerified;
			this.cache.Remove(chatId);
			this.cache.Set(chatId, isVerified);
			this.dbContext.Users.Update(maybeUser);
			await this.dbContext.SaveChangesAsync();
		}

		public async Task DeleteUserByChatIdAsync(long chatId)
		{
			var maybeUser = await this.dbContext.Users
				.FirstOrDefaultAsync(u => u.TelegramChatId.Equals(chatId));

			if (maybeUser is not null)
			{
				this.cache.Remove(chatId);
				this.dbContext.Users.Remove(maybeUser);
				await this.dbContext.SaveChangesAsync();
			}
		}

		public async Task AddUserAddressAsync(Address address)
		{
			this.dbContext.Addresses.Add(address);
			await this.dbContext.SaveChangesAsync();
		}

		public async Task UpdateUserAddress(long chatId, int latitude, int longitude)
		{
			var address = await GetUserAddressByChatId(chatId);

			address.Latitude = latitude;
			address.Longitude = longitude;

			await this.dbContext.SaveChangesAsync();
		}

		public async Task<Address> GetUserAddressByChatId(long chatId)
		{
			var user = await this.GetUserByChatIdAsync(chatId);

			var address = await this.dbContext.Addresses.FirstOrDefaultAsync(a =>
				a.User.Id.Equals(user.Id));

			return address;
		}


		public async Task ConfirmUserAddress(long chatId)
		{
			var address = await this.GetUserAddressByChatId(chatId);

			if (address is not null)
			{
				address.IsActive = true;
				await this.dbContext.SaveChangesAsync();
			}
		}

		public async Task DeleteAddressById(Guid id)
		{
			var address = await this.dbContext.Addresses
				.FirstOrDefaultAsync(a => a.Id.Equals(id));

			if (address is not null)
			{
				this.dbContext.Addresses.Remove(address);
			}

			await this.dbContext.SaveChangesAsync();
		}
	}
}
