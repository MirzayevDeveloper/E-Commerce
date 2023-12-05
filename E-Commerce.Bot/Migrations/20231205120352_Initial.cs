using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_Commerce.Bot.Migrations
{
	/// <inheritdoc />
	public partial class Initial : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "Users",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "uuid", nullable: false),
					FirstName = table.Column<string>(type: "text", nullable: false),
					PhoneNumber = table.Column<string>(type: "text", nullable: false),
					TelegramChatId = table.Column<long>(type: "bigint", nullable: false),
					Language = table.Column<int>(type: "integer", nullable: false),
					IsVerified = table.Column<bool>(type: "boolean", nullable: false),
					SmsCode = table.Column<int>(type: "integer", nullable: true),
					SmsExpiredTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
					CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
					UpdatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Users", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "Addresses",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "uuid", nullable: false),
					UserId = table.Column<Guid>(type: "uuid", nullable: false),
					IsDelivered = table.Column<bool>(type: "boolean", nullable: false),
					Longitude = table.Column<double>(type: "double precision", nullable: false),
					Latitude = table.Column<double>(type: "double precision", nullable: false),
					CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
					UpdatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Addresses", x => x.Id);
					table.ForeignKey(
						name: "FK_Addresses_Users_UserId",
						column: x => x.UserId,
						principalTable: "Users",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_Addresses_UserId",
				table: "Addresses",
				column: "UserId");

			migrationBuilder.CreateIndex(
				name: "IX_Users_PhoneNumber",
				table: "Users",
				column: "PhoneNumber",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_Users_TelegramChatId",
				table: "Users",
				column: "TelegramChatId",
				unique: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "Addresses");

			migrationBuilder.DropTable(
				name: "Users");
		}
	}
}
