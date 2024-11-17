using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace WebApplication2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TextFilesController : ControllerBase
    {
        [HttpPost("process")]
        public async Task<IActionResult> ProcessFiles([FromBody] List<string> fileUrls, CancellationToken cancellationToken)
        {
            if(fileUrls == null || fileUrls.Count == 0)
            {
                return BadRequest("список ссылок пуст.");
            }

            var uniqueWords = new ConcurrentDictionary<string, byte>();
            string longestWord = string.Empty;

            try
            {
                var tasks = fileUrls.Select(async url =>
                {
                    using var client = new HttpClient();
                    var response = await client.GetStringAsync(url, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(response))
                    {
                        var words = Regex.Split(response, @"\W+")
                                         .Where(w => !string.IsNullOrWhiteSpace(w));

                        foreach (var word in words)
                        {
                            uniqueWords.TryAdd(word.ToLower(), 0);

                            if (word.Length > longestWord.Length)
                            {
                                longestWord = word;
                            }
                        }
                    }
                });

                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, "Запрос отменён пользователем");
            }
            catch (Exception ex) 
            {
                return StatusCode(500,$"Ошибка обработки: {ex.Message}");
            }
            return Ok(new
            {
                UniqueWordsCount = uniqueWords.Count,
                LongestWord = longestWord,
            });

        }

    }
}
