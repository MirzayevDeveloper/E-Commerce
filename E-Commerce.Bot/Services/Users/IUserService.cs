using E_Commerce.Bot.Entities.Addresses;
using E_Commerce.Bot.Entities.Users;

namespace E_Commerce.Bot.Services.Users
{
	public interface IUserService
	{
		Task AddUserAsync(User user);
		Task UpdateUserAsync(User user);
		Task<User?> GetUserByChatIdAsync(long chatId);
		Task<User?> GetUserByPhoneNumberAsync(string phoneNumber);
		Task<bool?> CheckUserIsVerifiedByPhoneNumberAsync(string phoneNumber);
		Task<bool?> CheckUserIsVerifiedByChatIdAsync(long id);
		Task<bool> CheckSmsCodeAsync(long chatId, int smsCode);
		Task VerifyUserAsync(long chatId, bool isVerified = true);
		Task DeleteUserByChatIdAsync(long chatId);
		Task AddUserAddressAsync(Address address);
		Task UpdateUserAddress(long chatId, int latitude, int longitude);
		Task ConfirmUserAddress(long chatId);
		Task<Address> GetUserAddressByChatId(long chatId);
		Task DeleteAddressById(Guid id);
	}
}
