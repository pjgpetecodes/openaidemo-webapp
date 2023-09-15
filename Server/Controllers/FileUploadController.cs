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

                    // ...  
                    using (PdfDocument document = PdfDocument.Open(stream))
                    {
                        List<ExtractedParagraph> extractedParagraphs = new List<ExtractedParagraph>();
                        string pendingContent = ""; // Add this variable to store content that needs to be appended  

                        for (int pageIndex = 0; pageIndex < document.NumberOfPages; pageIndex++)
                        {
                            Page page = document.GetPage(pageIndex + 1);

                            // Extract words and blocks    
                            var words = NearestNeighbourWordExtractor.Instance.GetWords(page.Letters);
                            var blocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);

                            // Combine words in each block into paragraphs    
                            int paragraphIndex = 0;
                            foreach (var block in blocks)
                            {
                                var paragraphContent = string.Join(" ", block.Text);

                                // Check if the pending content should be appended  
                                if (!string.IsNullOrEmpty(pendingContent))
                                {
                                    paragraphContent = pendingContent + " " + paragraphContent;
                                    pendingContent = "";
                                }

                                // Count the number of words in the paragraph  
                                int wordCount = paragraphContent.Split(' ').Length;

                                // If the paragraph has less than 50 words, store it in pendingContent  
                                if (wordCount < 50)
                                {
                                    pendingContent = paragraphContent;
                                }
                                else
                                {
                                    // Add the paragraph to the extractedParagraphs list  
                                    extractedParagraphs.Add(new ExtractedParagraph
                                    {
                                        Id = $"{pageIndex + 1}.{paragraphIndex + 1}",
                                        Title = $"Page {pageIndex + 1} - Paragraph {paragraphIndex + 1}",
                                        Content = paragraphContent
                                    });
                                    paragraphIndex++;
                                }
                            }
                        }

                        // Add any remaining pending content as the last paragraph  
                        if (!string.IsNullOrEmpty(pendingContent))
                        {
                            extractedParagraphs.Add(new ExtractedParagraph
                            {
                                Id = $"{document.NumberOfPages}.{extractedParagraphs.Count + 1}",
                                Title = $"Page {document.NumberOfPages} - Paragraph {extractedParagraphs.Count + 1}",
                                Content = pendingContent
                            });
                        }

                        return Ok(extractedParagraphs);
                    }

                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed  
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing the file.");
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
