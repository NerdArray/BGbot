using System.Net.Http;
using System.Threading.Tasks;

namespace JudgettaBot.Services
{
    public class JokeService
    {
        private readonly IHttpClientFactory _clientFactory;

        public JokeService(IHttpClientFactory clientFactory)
            => _clientFactory = clientFactory;

        public async Task<string> GetJokeAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://icanhazdadjoke.com");
            request.Headers.Add("Accept", "application/json");

            using (var client = _clientFactory.CreateClient())
            {
                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    return null;
                }
            }
        }

        public async Task<string> GetMamaJokeAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.yomomma.info/");
            request.Headers.Add("Accept", "application/json");

            using (var client = _clientFactory.CreateClient())
            {
                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
