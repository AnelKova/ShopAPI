using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopAPI.Context;
using ShopAPI.Models;
using System.Net.Http.Headers;

namespace ShopAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagesController : ControllerBase
    {
        private readonly ItemContext Context;

        public ImagesController(ItemContext context)
        {
            Context = context;
        }


        [HttpPost, DisableRequestSizeLimit]
        public async Task<ActionResult> UploadImage()
        {
            try
            {
                var formCollection = await Request.ReadFormAsync();
                var file = formCollection.Files.First();

                var folderName = Path.Combine("wwwroot", "images");
                var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);

                if (file.Length > 0)
                {
                    var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                    var fullPath = Path.Combine(pathToSave, fileName);
                    var dbPath = Path.Combine(folderName, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }

                    var itemId = int.Parse(fileName.Substring(0, fileName.IndexOf('.')));
                    var fileType = fileName.Substring(fileName.IndexOf('.') + 1, fileName.Length - fileName.IndexOf('.') - 1);
                    var image = await Context.Images.FirstOrDefaultAsync(img => img.ItemId == itemId);
                    if (image == null)
                    {
                        image = new Image()
                        {
                            ItemId = itemId,
                            FileName = fileName,
                            FileType = fileType
                        };
                        await Context.Images.AddAsync(image);
                    }
                    else
                    {
                        image.FileName = fileName;
                        image.FileType = fileType;
                    }

                    await Context.SaveChangesAsync();

                    return Ok(new { dbPath });
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }
    }
}
