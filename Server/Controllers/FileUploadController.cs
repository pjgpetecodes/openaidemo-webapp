using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using openaidemo_webapp.Shared;
using openaidemo_webapp.Server.Helpers;
using openaidemo_webapp.Client.Pages;

namespace YourProjectName.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileUploadController : ControllerBase
    {

        private readonly IConfiguration _config;

        public FileUploadController(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file received.");
                }

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    var pdfHelper = new PDFHelper(_config);

                    ExtractionResult extractionResult = await pdfHelper.ExtractParagraphs(stream, file.FileName);

                    var cognitiveSearchHelper = new CognitiveSearchHelper(_config);

                    extractionResult = await cognitiveSearchHelper.CreateOrUpdateIndex(extractionResult); 

                    return Ok(extractionResult);

                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed  
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing the file.");
            }
        }

        


    }
}
