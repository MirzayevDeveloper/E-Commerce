using E_Commerce.Bot.Entities.Addresses;
using E_Commerce.Bot.Entities.Users;
using E_Commerce.Bot.Helpers;
using E_Commerce.Bot.Services.Sms;
using E_Commerce.Bot.Services.Users;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace E_Commerce.Bot.Services.Updates
{
	public class HandleUpdateService
	{
		private readonly ITelegramBotClient botClient;
		private readonly IUserService userService;
		private readonly TokenService tokenService;

		public HandleUpdateService(
			ITelegramBotClient botClient,
			IUserService userService,
			TokenService tokenService)
		{
			this.botClient = botClient;
			this.userService = userService;
			this.tokenService = tokenService;
		}

		public async Task EchoAsync(
			Update update, CancellationToken cancellationToken)
		{
			if (update.Message is null) return;

			var handler = update.Type switch
			{
				UpdateType.Message => BotOnMessageReceived(
					update.Message!, cancellationToken),

				UpdateType.CallbackQuery => BotOnCallbackQueryReceived(
					update.CallbackQuery!, cancellationToken),

				_ => UnknownUpdateTypeHandler(update, cancellationToken)
			};

			try
			{
				await handler;
			}
			catch (Exception ex)
			{
				await HandlerErrorAsync(ex, cancellationToken);
			}
		}

		private async Task BotOnMessageReceived(
			Message message, CancellationToken cancellationToken)
		{
			if (message is null) return;

			var isVerified = await this.userService
				.CheckUserIsVerifiedByChatIdAsync(message.Chat.Id);

			if (isVerified is null)
			{
				await ActionForNewUser(message, cancellationToken);
			}
			else if (isVerified is false && message.Text is not null)
			{
				if (int.TryParse(message.Text, out _))
				{
					bool isTrue = await ActionOnUserEnterVerificationCode(
						message, cancellationToken);

					if (isTrue)
					{
						await UnknownMessageTypeHandler(message, cancellationToken);
					}
				}
				else if (message.Text == "Resend code")
				{
					await HandleUserEnterResendCode(message, cancellationToken);
				}
				else if (message.Text == "Change phone number")
				{
					await HandleUserEnterChangePhoneNumber(
						message.Chat.Id, cancellationToken);
				}
				else
				{
					await this.botClient.SendTextMessageAsync(
						chatId: message.Chat.Id,
						text: "Invalid code");
				}
			}
			else
			{
				var action = message.Type switch
				{
					MessageType.Text => BotOnMessageTypeText(
						message, cancellationToken),

					MessageType.Contact => HandleUserContactSent(
						message, cancellationToken),

					MessageType.Location => HandleUserLocationSent(
						message, cancellationToken),

					_ => UnknownMessageTypeHandler(
						message, cancellationToken)
				};

				await action;
			}
		}

		private async Task HandleUserLocationSent(Message message, CancellationToken cancellationToken)
		{
			var action = NearestBranchOption(message, cancellationToken);

			await action;
		}

		private async Task HandleUserEnterChangePhoneNumber(
			long id, CancellationToken cancellationToken)
		{
			await this.userService.DeleteUserByChatIdAsync(id);

			await SendContactRequest(id, cancellationToken);
		}

		private async Task HandleUserEnterResendCode(Message message, CancellationToken cancellationToken)
		{
			var maybeUser = await this.userService
				.GetUserByChatIdAsync(message.Chat.Id);

			DateTimeOffset till = maybeUser.SmsExpiredTime ?? new DateTimeOffset();
			DateTimeOffset now = DateTime.UtcNow;

			if (till.AddMinutes(1) > now)
			{
				int seconds = Convert.ToInt32((now - till.AddSeconds(60)).TotalSeconds);

				await this.botClient.SendTextMessageAsync(
					chatId: message.Chat.Id,
					text: $"Please, wait and try again {seconds * -1}s later...");
			}
			else
			{
				await SendSmsCodeAsync(
					phoneNumber: maybeUser.PhoneNumber,
					cancellationToken: cancellationToken);

				await SendResendSmsCodeOrChangeNumberRequest(
					message, cancellationToken);
			}
		}

		private async Task<bool> ActionOnUserEnterVerificationCode(Message message, CancellationToken cancellationToken)
		{
			var code = int.Parse(message.Text!);
			var chatId = message.Chat.Id;

			if (await this.userService.CheckSmsCodeAsync(chatId, code)) // sms experition vaqtini tekshirirsh lozim
			{
				await this.userService.VerifyUserAsync(chatId);

				return true;
			}
			else
			{
				await this.botClient.SendTextMessageAsync(
					chatId: message.Chat.Id,
					text: "Invalid code");

				return false;
			}
		}

		private async Task ActionForNewUser(
			Message message, CancellationToken cancellationToken)
		{
			if (message.Contact is not null)
			{
				bool isTrue = await HandleUserContactSent(
					message, cancellationToken);

				if (isTrue)
				{
					await SendResendSmsCodeOrChangeNumberRequest(
						message, cancellationToken);
				}
			}
			else
			{
				await SendContactRequest(
					message.Chat.Id, cancellationToken);
			}
		}

		private async Task SendResendSmsCodeOrChangeNumberRequest(
			Message message, CancellationToken cancellationToken)
		{
			var markup = new ReplyKeyboardMarkup(
				new KeyboardButton[][]
			{
				  new KeyboardButton[]
				  {
					 new KeyboardButton("Change phone number"),
				  },
				  new KeyboardButton[]
				  {
					 new KeyboardButton("Resend code")
				  }
			 });

			markup.OneTimeKeyboard = true;
			markup.ResizeKeyboard = true;

			await botClient.SendTextMessageAsync(
				 chatId: message.Chat.Id,
				 text: "Code sent. Enter it to activate your account",
				 replyMarkup: markup,
				 cancellationToken: cancellationToken);
		}

		private async Task<bool> HandleUserContactSent(
			Message message, CancellationToken cancellationToken)
		{
			var newUser = new Entities.Users.User
			{
				Id = Guid.NewGuid(),
				FirstName = message.Chat.FirstName!,
				Language = Language.English,
				PhoneNumber = message.Contact!.PhoneNumber,
				TelegramChatId = message.Chat.Id,
				IsVerified = false
			};

			await this.userService.AddUserAsync(newUser);

			var smsCode = await SendSmsCodeAsync(
				phoneNumber: newUser.PhoneNumber,
				cancellationToken);

			return smsCode is not 0;
		}

		private async Task BotOnMessageTypeText(
			Message message, CancellationToken cancellationToken)
		{
			if (message.Text is not { } messageText) return;

			Log.Information($"Message keldi: {message.Type}");

			var action = messageText switch
			{
				"🛍 Order" => HandlerOrderOption(message, cancellationToken),
				"✍️ Leave feedback" => LeaveFeedbackOption(message, cancellationToken),
				"☎️ Contact us" => ContactUsOption(message, cancellationToken),
				"ℹ️ Information" => InformationOption(message, cancellationToken),
				"⚙️ Settings" => SettingsOption(message, cancellationToken),

				"🚖 Delivery" => DeliveryOption(message, cancellationToken),
				"🏃 Pick up" => PickUpOption(message, cancellationToken),

				"📍 Nearest branch" => NearestBranchOption(message, cancellationToken),

				"🇬🇧 Select language" => SelectLanguageOptions(message, cancellationToken),
				"Edit number" => EditNumberOption(message, cancellationToken),
				"Edit name" => EditNameOption(message, cancellationToken),

				"✅ Confirm" => ConfirmOption(message, cancellationToken),

				"📥 Basket" => OthersComingSoon(message, cancellationToken),
				"🚖 Place an order" => OthersComingSoon(message, cancellationToken),
				"👕 Clothes" => OthersComingSoon(message, cancellationToken),
				"👞 Shoes" => OthersComingSoon(message, cancellationToken),
				"🎧 Electronics" => OthersComingSoon(message, cancellationToken),
				"🎒 Accessories" => OthersComingSoon(message, cancellationToken),
				"💅🏻 Beauty" => OthersComingSoon(message, cancellationToken),
				"❤️ Health" => OthersComingSoon(message, cancellationToken),
				//"⬅️ Back" => BackOption(message, cancellationToken),
				_ => UnknownMessageTypeHandler(message, cancellationToken)
			};

			await action;
		}

		private async Task OthersComingSoon(
			Message message, CancellationToken cancellationToken)
		{
			await botClient.SendTextMessageAsync(
				 chatId: message.Chat.Id,
				 text: $"{message.Text} is coming soon...",
				 cancellationToken: cancellationToken);

			await MainMenu(message, cancellationToken);
		}

		private async Task MainMenu(
			Message message, CancellationToken cancellationToken)
		{
			var markup = new ReplyKeyboardMarkup(
				new KeyboardButton[][]
			{
				  new KeyboardButton[]
				  {
					 new KeyboardButton("📥 Basket"),
					 new KeyboardButton("🚖 Place an order")
				  },
				  new KeyboardButton[]
				  {
					 new KeyboardButton("👕 Clothes"),
					 new KeyboardButton("👞 Shoes"),
				  },
				  new KeyboardButton[]
				  {
					 new KeyboardButton("🎧 Electronics"),
					 new KeyboardButton("🎒 Accessories")
				  },
				  new KeyboardButton[]
				  {
					 new KeyboardButton("💅🏻 Beauty"),
					 new KeyboardButton("❤️ Health")
				  },
				  new KeyboardButton[]
				  {
					 new KeyboardButton("coming soon..."),
					 new KeyboardButton("⬅️ Back")
				  }
			 });

			markup.OneTimeKeyboard = true;
			markup.ResizeKeyboard = true;

			if (message.Location is null)
			{
				await botClient.SendTextMessageAsync(
				 chatId: message.Chat.Id,
				 text: $"Where do we start?",
				 replyMarkup: markup,
				 cancellationToken: cancellationToken);
			}
			else
			{
				var result = GetLocationAddress.GetAddress(
				message.Location!.Latitude, message.Location!.Longitude);

				await botClient.SendTextMessageAsync(
					 chatId: message.Chat.Id,
					 text: $"Your address: {result}\nIncorrect location?\n📍 Resend location",
					 replyMarkup: markup,
					 cancellationToken: cancellationToken);
			}
		}

		private async Task ConfirmOption(
			Message message, CancellationToken cancellationToken)
		{
			await this.userService.ConfirmUserAddress(message.Chat.Id);

			await MainMenu(message, cancellationToken);
		}

		private async Task NearestBranchOption(Message message, CancellationToken cancellationToken)
		{
			var maybeAddress =
				await this.userService.GetUserAddressByChatId(message.Chat.Id);

			if (maybeAddress is not null)
			{
				await this.userService.DeleteAddressById(maybeAddress.Id);
			}

			var user = await this.userService
				.GetUserByChatIdAsync(message.Chat.Id);

			var latitude = message.Location!.Latitude;
			var longitude = message.Location!.Longitude;

			var address = new Address
			{
				Id = Guid.NewGuid(),
				User = user,
				IsActive = false,
				Latitude = latitude,
				Longitude = longitude
			};

			await this.userService.AddUserAddressAsync(address);

			await SendToUserConfirmOrNotRequest(message, cancellationToken);
		}

		private async Task SendToUserConfirmOrNotRequest(Message message, CancellationToken cancellationToken)
		{
			var markup = new ReplyKeyboardMarkup(
				new KeyboardButton[][]
			{
				  new KeyboardButton[]
				  {
					 KeyboardButton.WithRequestLocation("📍 Resend location"),
				  },
				  new KeyboardButton[]
				  {
					 new KeyboardButton("✅ Confirm"),
				  },
				  new KeyboardButton[]
				  {
					 new KeyboardButton("⬅️ Back"),
				  }
			 });

			markup.OneTimeKeyboard = true;
			markup.ResizeKeyboard = true;

			var result = GetLocationAddress.GetAddress(
				message.Location!.Latitude, message.Location!.Longitude);

			await botClient.SendTextMessageAsync(
				 chatId: message.Chat.Id,
				 text: $"Your address: {result}\nIncorrect location?\n📍 Resend location",
				 replyMarkup: markup,
				 cancellationToken: cancellationToken);
		}

		private async Task PickUpOption(Message message, CancellationToken cancellationToken)
		{
			await botClient.SendLocationAsync(
				 chatId: message.Chat.Id,
				 latitude: 41.326508,
				 longitude: 69.228459,
				 cancellationToken: cancellationToken);

			await MainMenu(message, cancellationToken);
		}

		private async Task DeliveryOption(Message message, CancellationToken cancellationToken)
		{
			var markup = new ReplyKeyboardMarkup(
				new KeyboardButton[][]
			{
				  new KeyboardButton[]
				  {
					 KeyboardButton.WithRequestLocation("📍 Nearest branch"),
				  },
				  new KeyboardButton[]
				  {
					 new KeyboardButton("⬅️ Back"),
				  }
			 });

			markup.OneTimeKeyboard = true;
			markup.ResizeKeyboard = true;

			await botClient.SendTextMessageAsync(
				 chatId: message.Chat.Id,
				 text: "Where do you want your order to be delivered?",
				 replyMarkup: markup,
				 cancellationToken: cancellationToken);
		}

		private async Task EditNameOption(Message message, CancellationToken cancellationToken)
		{
			await botClient.SendTextMessageAsync(
				 chatId: message.Chat.Id,
				 text: "Sorry, coming soon...",
				 cancellationToken: cancellationToken);
		}

		private async Task EditNumberOption(Message message, CancellationToken cancellationToken)
		{
			await botClient.SendTextMessageAsync(
				 chatId: message.Chat.Id,
				 text: "Sorry, coming soon...",
				 cancellationToken: cancellationToken);
		}

		private async Task SelectLanguageOptions(Message message, CancellationToken cancellationToken)
		{
			await botClient.SendTextMessageAsync(
				 chatId: message.Chat.Id,
				 text: "Sorry, coming soon...",
				 cancellationToken: cancellationToken);
		}

		private async Task SettingsOption(Message message, CancellationToken cancellationToken)
		{
			var markup = new ReplyKeyboardMarkup(
				new KeyboardButton[][]
			{
				  new KeyboardButton[]
				  {
					 new KeyboardButton("Edit name"),
					 new KeyboardButton("Edit number")
				  },
				  new KeyboardButton[]
				  {
					 new KeyboardButton("🇬🇧 Select language"),
				  },
				  new KeyboardButton[]
				  {
					 new KeyboardButton("⬅️ Back"),
				  }
			 });

			markup.OneTimeKeyboard = true;
			markup.ResizeKeyboard = true;

			await botClient.SendTextMessageAsync(
				 chatId: message.Chat.Id,
				 text: "Okay, delivery?",
				 replyMarkup: markup,
				 cancellationToken: cancellationToken);
		}

		private async Task InformationOption(Message message, CancellationToken cancellationToken)
		{
			await botClient.SendTextMessageAsync(
				 chatId: message.Chat.Id,
				 text: "Information: \n09: 24:00\nSome informations...",
				 cancellationToken: cancellationToken);
		}

		private async Task ContactUsOption(Message message, CancellationToken cancellationToken)
		{
			await botClient.SendTextMessageAsync(
				 chatId: message.Chat.Id,
				 text: "You can call to us if you have questions: +998 90-150-20-04",
				 cancellationToken: cancellationToken);
		}

		private async Task LeaveFeedbackOption(Message message, CancellationToken cancellationToken)
		{
			var markup = new ReplyKeyboardMarkup(
				new KeyboardButton[][]
			{
				  new KeyboardButton[]
				  {
					 new KeyboardButton("⭐️⭐️⭐️⭐️⭐️"),
				  },
				  new KeyboardButton[]
				  {
					 new KeyboardButton("⭐️⭐️⭐️⭐️"),
				  },
				  new KeyboardButton[]
				  {
					 new KeyboardButton("⭐️⭐️⭐️"),
				  },
				  new KeyboardButton[]
				  {
					 new KeyboardButton("⭐️⭐️"),
				  },
				  new KeyboardButton[]
				  {
					 new KeyboardButton("⭐️"),
				  },
				  new KeyboardButton[]
				  {
					 new KeyboardButton("⬅️ Back"),
				  }
			 });

			markup.OneTimeKeyboard = true;
			markup.ResizeKeyboard = true;

			await botClient.SendTextMessageAsync(
				 chatId: message.Chat.Id,
				 text: "Thank you for choosing E-Commerce!\r\nWe will be happy if you help to improve the quality of our service!\r\nRate our work on a 5 point scale.",
				 replyMarkup: markup,
				 cancellationToken: cancellationToken);
		}

		private async Task HandlerOrderOption(Message message, CancellationToken cancellationToken)
		{
			var markup = new ReplyKeyboardMarkup(
				new KeyboardButton[][]
			{
				  new KeyboardButton[]
				  {
					 new KeyboardButton("🚖 Delivery"),
					 new KeyboardButton("🏃 Pick up")
				  },
				  new KeyboardButton[]
				  {
					 new KeyboardButton("⬅️ Back"),
				  }
			 });

			markup.OneTimeKeyboard = true;
			markup.ResizeKeyboard = true;

			await botClient.SendTextMessageAsync(
				 chatId: message.Chat.Id,
				 text: "Okay, delivery or pick up?",
				 replyMarkup: markup,
				 cancellationToken: cancellationToken);
		}

		private async Task<int> SendSmsCodeAsync(
			string phoneNumber, CancellationToken cancellationToken)
		{
			var maybeUser = await this.userService
				.GetUserByPhoneNumberAsync(phoneNumber);

			var smsCode = GetSmsKey.Get();
			maybeUser!.SmsCode = int.Parse(smsCode);

			maybeUser.SmsExpiredTime =
				DateTimeOffset.UtcNow.AddMinutes(1);

			await this.userService.UpdateUserAsync(maybeUser);

			string token = await this.tokenService.GetSmsTokenAsync();
			await SmsService.SendSms(token, phoneNumber, smsCode);

			return maybeUser.SmsCode ?? default;
		}

		private async Task SendContactRequest(
			long id, CancellationToken cancellationToken)
		{
			var markup = new ReplyKeyboardMarkup(
			KeyboardButton.WithRequestContact("📱 Send my number"))
			{
				ResizeKeyboard = true,
				OneTimeKeyboard = true
			};

			await this.botClient.SendTextMessageAsync(
				chatId: id,
				text: "Please click button, which is 📱 Send my number",
				replyMarkup: markup);
		}

		private async Task UnknownMessageTypeHandler(
			Message message, CancellationToken cancellationToken)
		{
			var markup = new ReplyKeyboardMarkup(
				new KeyboardButton[][]
			{
				  new KeyboardButton[]
				  {
					 new KeyboardButton("🛍 Order"),
				  },
				  new KeyboardButton[]
				  {
					 new KeyboardButton("✍️ Leave feedback"),
					 new KeyboardButton("☎️ Contact us")
				  },
				  new KeyboardButton[]
				  {
					 new KeyboardButton("ℹ️ Information"),
					 new KeyboardButton("⚙️ Settings")
				  }
			 });

			markup.OneTimeKeyboard = true;
			markup.ResizeKeyboard = true;

			await botClient.SendTextMessageAsync(
				 chatId: message.Chat.Id,
				 text: "Great! Let’s place an order together? 😃",
				 replyMarkup: markup,
				 cancellationToken: cancellationToken);
		}

		private async Task BotOnCallbackQueryReceived(
			CallbackQuery callbackQuery, CancellationToken cancellationToken)
		{
			await botClient.SendTextMessageAsync(
				chatId: callbackQuery.Message!.Chat.Id,
				text: $"{callbackQuery.Data}");
		}

		private Task UnknownUpdateTypeHandler(
			Update update, CancellationToken cancellationToken)
		{
			Log.Information($"Unknow update type: {update.Type}");

			return Task.CompletedTask;
		}

		private Task HandlerErrorAsync(
			Exception ex, CancellationToken cancellationToken)
		{
			var errorMessage = ex switch
			{
				ApiRequestException apiRequestException =>
					$"Telegram API Error:\n{apiRequestException.Message}",
				_ => ex.Message.ToString()
			};

			Log.Error(errorMessage);

			return Task.CompletedTask;
		}
	}
}
