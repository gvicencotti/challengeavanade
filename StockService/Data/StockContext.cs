using Microsoft.EntityFrameworkCore;
using StockService.Models;

namespace StockService.Data
{
    public class StockContext : DbContext
    {
        public StockContext(DbContextOptions<StockContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
    }
}
