using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RedisCaching.Data;
using RedisCaching.Models;
using System.Text.Json;

namespace RedisCaching.Controllers
{
 
    //public IActionResult Index()
    //{
    //    return View();
    //}

    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;

        // Inject ApplicationDbContext and IDistributedCache via constructor.
        public ProductsController(ApplicationDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }



        // GET: api/products/all
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var cacheKey = "GET_ALL_PRODUCTS";
            List<Product> products;

            try
            {
                // Attempt to retrieve the product list from Redis cache.
                var cachedData = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    // Deserialize JSON string back to List<Product>.
                    products = JsonSerializer.Deserialize<List<Product>>(cachedData) ?? new List<Product>();
                }
                else
                {
                    // Cache miss: fetch products from the database.
                    products = await _context.Products.AsNoTracking().ToListAsync();

                    if (products != null)
                    {
                        // Serialize the product list to a JSON string.
                        var serializedData = JsonSerializer.Serialize(products);
                        // Define cache options (using sliding expiration).
                        var cacheOptions = new DistributedCacheEntryOptions()
                            .SetSlidingExpiration(TimeSpan.FromMinutes(5));

                        // Store the serialized data in Redis.
                        await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);
                    }
                }

                return Ok(products);
            }
            catch (Exception ex)
            {
                // Return a 500 response if any error occurs.
                return StatusCode(500, new { message = "An error occurred while retrieving products.", details = ex.Message });
            }
        }

        //var listCacheKey = "GET_ALL_PRODUCTS";
        //var cachedList = await _cache.GetStringAsync(listCacheKey);

        //if (!string.IsNullOrEmpty(cachedList))
        //{
        //var productList = JsonSerializer.Deserialize<List<Product>>(cachedList);

        //    var existing = productList.FirstOrDefault(p => p.Id == id);
        //if (existing != null)
        //{
        //    existing.Name = updatedProduct.Name;
        //    existing.Category = updatedProduct.Category;
        //    existing.Price = updatedProduct.Price;
        //    existing.Quantity = updatedProduct.Quantity;
        //}

        //await _cache.SetStringAsync(listCacheKey, JsonSerializer.Serialize(productList),
        //    new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(5) });
        //}


        // GET: api/products/Category?Category=Fruits
        [HttpGet("Category")]
        public async Task<IActionResult> GetProductByCategory(string Category)
        {
            // Cache key includes the category to ensure unique cache entries.
            var cacheKey = $"PRODUCTS_{Category}";
            List<Product> products;

            try
            {
                var cachedData = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    products = JsonSerializer.Deserialize<List<Product>>(cachedData) ?? new List<Product>();
                }
                else
                {
                    // Cache miss: fetch from the database by matching category.
                    products = await _context.Products
                        .Where(prd => prd.Category.ToLower() == Category.ToLower())
                        .AsNoTracking()
                        .ToListAsync();

                    if (products != null)
                    {
                        var serializedData = JsonSerializer.Serialize(products);
                        // Use absolute expiration so that the cache entry expires after 5 minutes.
                        var cacheOptions = new DistributedCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                        await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);
                    }
                }

                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving products.", details = ex.Message });
            }
        }

        // GET: api/products/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            // Cache key for a single product.
            var cacheKey = $"Product_{id}";
            Product? product;

            try
            {
                var cachedData = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    product = JsonSerializer.Deserialize<Product>(cachedData) ?? new Product();
                }
                else
                {
                    // Fetch from database if not present in cache.
                    product = await _context.Products.FindAsync(id);
                    if (product == null)
                        return NotFound($"Product with ID {id} not found.");

                    var serializedData = JsonSerializer.Serialize(product);
                    await _cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromMinutes(5)
                    });
                }

                return Ok(product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the product.", details = ex.Message });
            }
        }

        // POST: api/products
        [HttpPost]
        public async Task<IActionResult> AddProducts([FromBody] object input)
        {
            try
            {
                List<Product> productsToAdd = new();

                // Determine if input is a single Product or a list
                if (input is JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.Object)
                    {
                        // Single product
                        var singleProduct = element.Deserialize<Product>();
                        if (singleProduct != null) productsToAdd.Add(singleProduct);
                    }
                    else if (element.ValueKind == JsonValueKind.Array)
                    {
                        // Multiple products
                        productsToAdd = element.Deserialize<List<Product>>() ?? new List<Product>();
                    }
                    else
                    {
                        return BadRequest("Invalid JSON format.");
                    }
                }

                if (productsToAdd.Count == 0)
                    return BadRequest("No valid products to add.");

                // Add products to DB and cache
                foreach (var product in productsToAdd)
                {
                    _context.Products.Add(product);

                    // Cache individual product
                    var cacheKey = $"Product_{product.Id}";
                    var serializedData = JsonSerializer.Serialize(product);
                    await _cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromMinutes(5)
                    });
                }

                await _context.SaveChangesAsync();

                // Invalidate cached list of all products
                await _cache.RemoveAsync("GET_ALL_PRODUCTS");

                return Ok(productsToAdd);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while adding products.", details = ex.Message });
            }
        }


        [HttpPost("bulk")]
        public async Task<IActionResult> AddProducts([FromBody] List<Product> newProducts)
        {
            try
            {
                foreach (var product in newProducts)
                {
                    _context.Products.Add(product);

                    // Cache each product individually
                    var cacheKey = $"Product_{product.Id}";
                    var serializedData = JsonSerializer.Serialize(product);
                    await _cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromMinutes(5)
                    });
                }

                // Save all at once
                await _context.SaveChangesAsync();

                // Invalidate GET_ALL_PRODUCTS cache
                await _cache.RemoveAsync("GET_ALL_PRODUCTS");

                return Ok(newProducts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while adding products.", details = ex.Message });
            }
        }



        // PUT: api/products/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product updatedProduct)
        {
            if (id != updatedProduct.Id)
            {
                return BadRequest("Product ID mismatch.");
            }

            try
            {
                var existingProduct = await _context.Products.FindAsync(id);
                if (existingProduct == null)
                    return NotFound($"Product with ID {id} not found.");

                // Update product details in the database.
                _context.Entry(existingProduct).CurrentValues.SetValues(updatedProduct);
                await _context.SaveChangesAsync();

                // Update the cache for this product.
                var cacheKey = $"Product_{id}";
                await _cache.RemoveAsync(cacheKey);
                await _cache.RemoveAsync("GET_ALL_PRODUCTS");
                var serializedData = JsonSerializer.Serialize(updatedProduct);
                await _cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(5)
                });

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the product.", details = ex.Message });
            }
        }

        // DELETE: api/products/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return NotFound($"Product with ID {id} not found.");

                // Remove product from the database.
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                // Remove product from the cache.
                var cacheKey = $"Product_{id}";
                await _cache.RemoveAsync("GET_ALL_PRODUCTS");
                await _cache.RemoveAsync(cacheKey);

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the product.", details = ex.Message });
            }
        }
    }
     
}
