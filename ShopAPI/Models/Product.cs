using static System.Net.Mime.MediaTypeNames;

namespace ShopAPI.Models
{
    public class Product
    {
        public string Code { get; set; } = null!;
        public int Id { get; set; }

        public string Name { get; set; }

        public decimal Price { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }

        public string? ImgPath { get; set; }
    }
}
