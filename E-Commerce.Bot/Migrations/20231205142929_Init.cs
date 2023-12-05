using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_Commerce.Bot.Migrations
{
	/// <inheritdoc />
	public partial class Init : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.RenameColumn(
				name: "IsDelivered",
				table: "Addresses",
				newName: "IsActive");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.RenameColumn(
				name: "IsActive",
				table: "Addresses",
				newName: "IsDelivered");
		}
	}
}
