using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RedisCaching.Migrations
{
    /// <inheritdoc />
    public partial class mig1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Category", "Name", "Price", "Quantity" },
                values: new object[,]
                {
                    { 1, "Electronics", "Apple iPhone 14", 999, 50 },
                    { 2, "Electronics", "Samsung Galaxy S22", 899, 40 },
                    { 3, "Electronics", "Sony WH-1000XM4 Headphones", 349, 30 },
                    { 4, "Footwear", "Nike Air Zoom Pegasus", 120, 100 },
                    { 5, "Footwear", "Adidas Ultraboost", 180, 80 },
                    { 6, "Groceries", "Organic Apples (1kg)", 4, 200 },
                    { 7, "Groceries", "Organic Bananas (1 Dozen)", 3, 150 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
