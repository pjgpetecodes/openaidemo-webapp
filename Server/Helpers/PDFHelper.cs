using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
using openaidemo_webapp.Shared;

namespace openaidemo_webapp.Server.Helpers
{
    public class PDFHelper
    {
        private readonly IConfiguration _config;

        public PDFHelper(IConfiguration config)
        {
            _config = config;
        }

        public async Task<ExtractionResult> ExtractParagraphs(Stream stream, String filename)
        {
            // Get the Company Name from the file name  
            string[] parts = filename.Split('-');
            string companyName = "";
            string year = "";

            if (parts.Length >= 3)
            {
                companyName = parts[0];
                year = parts[1];
            }
            else
            {
                // Handle the case where the filename doesn't follow the expected format    
            }

            // Extract the paragraphs of text from the document  
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

                        // If the paragraph has less than 250 words, store it in pendingContent    
                        if (wordCount < 250)
                        {
                            pendingContent = paragraphContent;
                        }
                        else
                        {
                            // Check if the paragraphContent exceeds 7000 characters  
                            while (paragraphContent.Length > 7000)
                            {
                                // Split the paragraphContent into smaller paragraphs  
                                var subParagraph = paragraphContent.Substring(0, 7000);
                                int lastSpaceIndex = subParagraph.LastIndexOf(' ');
                                subParagraph = subParagraph.Substring(0, lastSpaceIndex);

                                // Add the smaller paragraph to the extractedParagraphs list  
                                extractedParagraphs.Add(new ExtractedParagraph
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    Location = $"{pageIndex + 1}-{paragraphIndex + 1}",
                                    Title = $"Page {pageIndex + 1} - Paragraph {paragraphIndex + 1}",
                                    Content = subParagraph
                                });
                                paragraphIndex++;

                                // Update paragraphContent  
                                paragraphContent = paragraphContent.Substring(lastSpaceIndex + 1);
                            }

                            // Add the remaining paragraphContent to the extractedParagraphs list  
                            extractedParagraphs.Add(new ExtractedParagraph
                            {
                                Id = Guid.NewGuid().ToString(),
                                Location = $"{pageIndex + 1}-{paragraphIndex + 1}",
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
                        Id = Guid.NewGuid().ToString(),
                        Location = $"{document.NumberOfPages}-{extractedParagraphs.Count + 1}",
                        Title = $"Page {document.NumberOfPages} - Paragraph {extractedParagraphs.Count + 1}",
                        Content = pendingContent
                    });
                }

                ExtractionResult extractionResults = new ExtractionResult();
                extractionResults.FileName = filename;
                extractionResults.Company = companyName;
                extractionResults.Year = year;
                extractionResults.ExtractedParagraphs = extractedParagraphs;

                System.Diagnostics.Debug.Print(extractionResults.ToString());
                return await Task.FromResult(extractionResults);
            }
        }

       
    }
}