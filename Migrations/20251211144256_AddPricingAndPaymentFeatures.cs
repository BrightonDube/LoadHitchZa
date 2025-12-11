using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace t12Project.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingAndPaymentFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeightLbs",
                table: "Loads");

            migrationBuilder.RenameColumn(
                name: "MaxWeightLbs",
                table: "DriverRoutes",
                newName: "MaxWeightKg");

            migrationBuilder.AddColumn<decimal>(
                name: "CalculatedPrice",
                table: "Loads",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomerOfferPrice",
                table: "Loads",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DistanceKm",
                table: "Loads",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DriverEarnings",
                table: "Loads",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalPrice",
                table: "Loads",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentId",
                table: "Loads",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeightKg",
                table: "Loads",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LoadId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<string>(type: "text", nullable: false),
                    DriverId = table.Column<string>(type: "text", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PlatformFee = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    DriverPayout = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TransactionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CardLast4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    CardBrand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ReleasedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payment_AspNetUsers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Payment_AspNetUsers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Payment_Loads_LoadId",
                        column: x => x.LoadId,
                        principalTable: "Loads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Loads_PaymentId",
                table: "Loads",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_CustomerId",
                table: "Payment",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_DriverId",
                table: "Payment",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_LoadId",
                table: "Payment",
                column: "LoadId");

            migrationBuilder.AddForeignKey(
                name: "FK_Loads_Payment_PaymentId",
                table: "Loads",
                column: "PaymentId",
                principalTable: "Payment",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Loads_Payment_PaymentId",
                table: "Loads");

            migrationBuilder.DropTable(
                name: "Payment");

            migrationBuilder.DropIndex(
                name: "IX_Loads_PaymentId",
                table: "Loads");

            migrationBuilder.DropColumn(
                name: "CalculatedPrice",
                table: "Loads");

            migrationBuilder.DropColumn(
                name: "CustomerOfferPrice",
                table: "Loads");

            migrationBuilder.DropColumn(
                name: "DistanceKm",
                table: "Loads");

            migrationBuilder.DropColumn(
                name: "DriverEarnings",
                table: "Loads");

            migrationBuilder.DropColumn(
                name: "FinalPrice",
                table: "Loads");

            migrationBuilder.DropColumn(
                name: "PaymentId",
                table: "Loads");

            migrationBuilder.DropColumn(
                name: "WeightKg",
                table: "Loads");

            migrationBuilder.RenameColumn(
                name: "MaxWeightKg",
                table: "DriverRoutes",
                newName: "MaxWeightLbs");

            migrationBuilder.AddColumn<double>(
                name: "WeightLbs",
                table: "Loads",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
