using Microsoft.EntityFrameworkCore;
using RedisCaching.Models;

namespace RedisCaching.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext (DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            var initialProducts = new List<Product>
            {
                new Product
                {
                    Id = 1,
                    Name = "Apple iPhone 14",
                    Category = "Electronics",
                    Price = 999,
                    Quantity = 50
                },
                new Product
                {
                    Id = 2,
                    Name = "Samsung Galaxy S22",
                    Category = "Electronics",
                    Price = 899,
                    Quantity = 40
                },
                new Product
                {
                    Id = 3,
                    Name = "Sony WH-1000XM4 Headphones",
                    Category = "Electronics",
                    Price = 349,
                    Quantity = 30
                },
                new Product
                {
                    Id = 4,
                    Name = "Nike Air Zoom Pegasus",
                    Category = "Footwear",
                    Price = 120,
                    Quantity = 100
                },
                new Product
                {
                    Id = 5,
                    Name = "Adidas Ultraboost",
                    Category = "Footwear",
                    Price = 180,
                    Quantity = 80
                },
                new Product
                {
                    Id = 6,
                    Name = "Organic Apples (1kg)",
                    Category = "Groceries",
                    Price = 4,
                    Quantity = 200
                },
                new Product
                {
                    Id = 7,
                    Name = "Organic Bananas (1 Dozen)",
                    Category = "Groceries",
                    Price = 3,
                    Quantity = 150
                }
            };
            modelBuilder.Entity<Product>().HasData(initialProducts);
        }
        public DbSet<Product> Products { get; set; }
    }


}
