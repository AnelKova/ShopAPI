using Microsoft.EntityFrameworkCore;
using ShopAPI.Models;

namespace ShopAPI.Context
{
    public class ItemContext : DbContext
    {
        public ItemContext(DbContextOptions options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Image> Images { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Product>().ToTable("products");
        

        }

        internal Task Getproducts()
        {
            throw new NotImplementedException();
        }
    }
}
