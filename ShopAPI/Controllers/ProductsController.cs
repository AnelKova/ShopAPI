using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopAPI.Context;
using ShopAPI.Models;

namespace ShopAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ItemContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProductsController(ItemContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products.ToListAsync();
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return product;
        }

        // PUT: api/Products/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            if (id != product.Id)
            {
                return BadRequest();
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Products
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProduct", new { id = product.Id }, product);
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
        public async Task<ActionResult> UploadImage()
        {
            bool Results = false;
            try
            {
                var _uploadedFiles = Request.Form.Files;
                foreach (IFormFile source in _uploadedFiles)
                {
                    string Filename = source.FileName;
                    string FilePath=GetFilePath(Filename);

                    if(!System.IO.Directory.Exists(FilePath))
                    {
                        System.IO.Directory.CreateDirectory(FilePath);
                    }
                    string imagepath = FilePath + "\\image.png";
                    if (!System.IO.File.Exists(imagepath))
                    {
                        System.IO.File.Delete(imagepath);
                    }
                    using(FileStream stream =System.IO.File.Create(imagepath)) 
                    {
                        await source.CopyToAsync(stream);
                        Results= true;
                    }
                }
            }
            catch (Exception ex)
            {


            }
                return Ok(Results);
            }
        [NonAction]
        private string GetFilePath(string ProductCode)
        {
            return _environment.WebRootPath + "\\Uploads\\Product" + ProductCode;
        }
        private string GetImageByProduct(string productcode)
        {
            string ImageUrl = string.Empty;
            string HostUrl = "http://localhost:4200/product/";
            string filepath = GetFilePath(productcode);
            string ImagePath = filepath + "\\image.png";
            if (!System.IO.File.Exists(ImagePath))
            {
                ImageUrl = HostUrl + "/uploads/common/noimage.png";
            }
            else
            {
                ImageUrl = HostUrl + "/uploads/Product/" + productcode + "/image.png";
            }
            return ImageUrl;
        }
        }
    }

