using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using openaidemo_webapp.Shared;
using openaidemo_webapp.Server.Helpers;
using openaidemo_webapp.Client.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using openaidemo_webapp.Server.Hubs;

namespace YourProjectName.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class FileUploadController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IHubContext<ChatHub> _hubContext;

        public FileUploadController(IConfiguration config, IHubContext<ChatHub> hubContext)
        {
            _config = config;
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string connectionId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file received.");
                }

                using (var stream = new MemoryStream())
                {

                    ISingleClientProxy signalRClient = _hubContext.Clients.Client(connectionId);

                    await signalRClient.SendAsync("UpdateFileUploadStatus", $"Beginning processing of {file.FileName}");

                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    await signalRClient.SendAsync("UpdateFileUploadStatus", $"Extracting contents from {file.FileName}");
                    
                    var pdfHelper = new PDFHelper(_config);

                    ExtractionResult extractionResult = await pdfHelper.ExtractParagraphs(stream, file.FileName);

                    await signalRClient.SendAsync("UpdateFileUploadStatus", $"Contents extracted successfully from {file.FileName}");

                    var cognitiveSearchHelper = new CognitiveSearchHelper(_config);

                    extractionResult = await cognitiveSearchHelper.CreateOrUpdateIndex(extractionResult, signalRClient);

                    await signalRClient.SendAsync("UpdateFileUploadStatus", $"Updated contents of {file.FileName} in Cognitive Search Index");

                    // Clear the extracted paragraphs as it takes too much memory and bandwidth.
                    extractionResult.ExtractedParagraphs = new List<ExtractedParagraph>();

                    return Ok(extractionResult);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while processing the file: {ex.Message}");
            }
        }
    }
}
