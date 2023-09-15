using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace YourProjectName.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileUploadController : ControllerBase
    {
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

                    using (PdfDocument document = PdfDocument.Open(stream))
                    {
                        List<ExtractedParagraph> extractedParagraphs = new List<ExtractedParagraph>();

                        for (int pageIndex = 0; pageIndex < document.NumberOfPages; pageIndex++)
                        {

                            Page page = document.GetPage(pageIndex + 1);

                            var words = NearestNeighbourWordExtractor.Instance.GetWords(page.Letters);
                            var blocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);

                            string text = page.Text;

                            // Extract paragraphs from the text  
                            var paragraphs = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                            for (int paragraphIndex = 0; paragraphIndex < paragraphs.Length; paragraphIndex++)
                            {
                                extractedParagraphs.Add(new ExtractedParagraph
                                {
                                    Id = $"{pageIndex + 1}.{paragraphIndex + 1}",
                                    Title = $"Page {pageIndex + 1} - Paragraph {paragraphIndex + 1}",
                                    Content = paragraphs[paragraphIndex]
                                });
                            }
                        }

                        return Ok(extractedParagraphs);
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }

        public class ExtractedParagraph
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Content { get; set; }
        }


    }
}
