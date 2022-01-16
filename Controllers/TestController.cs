using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ReckonAPITest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : Controller
    {
        private HttpClient _client;

        public TestController()
        {
            _client = new HttpClient();
        }

        [HttpGet("processTextToSearch")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ProcessTextToSearch()
        {
            var textToSearch = await GetTextToSearch();
            var subTexts = await GetSubTexts();

            ResultFormat resultFormat = new ResultFormat
            {
                Candidate = "Samuel Marvin Aguilos",
                Text = textToSearch,
            };

            foreach (var subText in subTexts)
            {
                var matchedIndex = MatchedIndexes(subText, textToSearch);

                if (matchedIndex.Count == 0)
                {
                    resultFormat.Results.Add(new StringResult {
                        Subtext = subText,
                        Result = "<No Output>"
                    });
                }
                else
                {
                    resultFormat.Results.Add(new StringResult
                    {
                        Subtext = subText,
                        Result = string.Join(", ", matchedIndex)
                    });
                }

            }

            await SendResultFormat(resultFormat);

            return Ok(resultFormat);
        }

        private List<int> MatchedIndexes(string toSearch, string source)
        {
            var toSearchFirstCharacter = toSearch.ToLower()[0];
            var sourceCharArray = source.ToLower().ToCharArray();
            List<int> matchedIndex = new List<int>();

            for (int i = 0; i < sourceCharArray.Length; i++)
            {
                if (sourceCharArray[i] == toSearchFirstCharacter)
                {
                    if (toSearch.ToLower() == StringGenerator(sourceCharArray, i, toSearch.Length))
                    {
                        matchedIndex.Add(i + 1);
                        i += toSearch.Length;
                    }
                }
            }

            return matchedIndex;
        }

        private string StringGenerator(char[] characters, int index, int size)
        {
            string result = string.Empty;

            for (int i = index; i < index + size; i++)
            {
                result += characters[i];
            }

            return result;
        }

        private async Task SendResultFormat(ResultFormat resultFormat)
        {
            bool isSuccess;
            int retryCount = 0;
            var responseString = string.Empty;

            do
            {
                var json = JsonConvert.SerializeObject(resultFormat);
                var data = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _client.PostAsync("https://join.reckon.com/test2/submitResults", data);
                isSuccess = response.IsSuccessStatusCode;
                retryCount++;
                responseString = await response.Content.ReadAsStringAsync();

            } while (!isSuccess && retryCount <= 5);
        }

        private async Task<string> GetTextToSearch()
        {
            bool isSuccess;
            int retryCount = 0;
            var responseString = string.Empty;

            do
            {
                HttpResponseMessage response = await _client.GetAsync("https://join.reckon.com/test2/textToSearch");
                isSuccess = response.IsSuccessStatusCode;
                retryCount++;
                responseString = await response.Content.ReadAsStringAsync();

            } while (!isSuccess && retryCount <= 5);

            return JsonConvert.DeserializeObject<JToken>(responseString)["text"].ToString();
        }

        private async Task<string[]> GetSubTexts()
        {
            bool isSuccess;
            int retryCount = 0;
            var responseString = string.Empty;

            do
            {
                HttpResponseMessage response = await _client.GetAsync("https://join.reckon.com/test2/subTexts");
                isSuccess = response.IsSuccessStatusCode;
                retryCount++;
                responseString = await response.Content.ReadAsStringAsync();

            } while (!isSuccess && retryCount <= 5);

            return ((IEnumerable)JsonConvert.DeserializeObject<JToken>(responseString)["subTexts"]).Cast<object>()
                             .Select(x => x.ToString())
                             .ToArray();
        }
    }

    public class ResultFormat
    {
        public ResultFormat()
        {
            Results = new List<StringResult>();
        }
        public string Candidate { get; set; }
        public string Text { get; set; }
        public List<StringResult> Results { get; }
    }

    public class StringResult
    {
        public string Subtext { get; set; }
        public string Result { get; set; }
    }
}
