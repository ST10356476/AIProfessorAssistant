using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace AIProfessorAssistant
{
    public class IndexModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public IndexModel(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new ArgumentException("API key is missing or invalid.");
            }
        }

        [BindProperty]
        public string Query { get; set; }

        public string Response { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var requestBody = new
            {
                prompt = Query,
                max_tokens = 100
            };

            var requestContent = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/engines/davinci/completions", requestContent);
            if (!response.IsSuccessStatusCode)
            {
                Response = $"Error: {response.ReasonPhrase}";
                return Page();
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(responseBody))
            {
                Response = "Error: Empty response from API.";
                return Page();
            }

            var result = JsonSerializer.Deserialize<CompletionResponse>(responseBody);
            if (result == null || result.Choices == null || result.Choices.Length == 0)
            {
                Response = "No response received or no choices available.";
                return Page();
            }

            Response = result.Choices[0].Text.Trim();

            return Page();
        }

        public class CompletionResponse
        {
            public Choice[] Choices { get; set; }

            public class Choice
            {
                public string Text { get; set; }
            }
        }
    }
}
