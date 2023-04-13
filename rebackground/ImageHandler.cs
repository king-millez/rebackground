using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

namespace rebackground
{
    internal class ImageHandler
    {
        [Serializable]
        public class RemoveBGException : Exception
        {
            public RemoveBGException() { }

            public RemoveBGException(string message)
                : base(message) { }

            public RemoveBGException(string message, Exception inner)
                : base(message, inner) { }
        }

        private static string GetEnvironmentVariable(string v)
        {
            return Environment.GetEnvironmentVariable(v)
                ?? throw new ArgumentNullException($"[-] {v} environment variable is not set.");
        }

        private static readonly string _removebg_key = GetEnvironmentVariable("REMOVEBG_API_KEY");
        private static readonly string _openai_key = GetEnvironmentVariable("OPENAI_API_KEY");

        public async Task<Tuple<string, Image>> RemoveBackgroundAsync(
            string path,
            byte[] imgContent,
            string taskFolder
        )
        {
            await Console.Out.WriteLineAsync($"[+] Removing background from {path}");
            using HttpClient client = new HttpClient();
            using MultipartFormDataContent formData = new MultipartFormDataContent();
            formData.Headers.Add("X-Api-Key", _removebg_key);
            formData.Add(
                new ByteArrayContent(imgContent),
                "image_file",
                $"{Path.GetFileName(path)}"
            );
            formData.Add(new StringContent("auto"), "size");
            HttpResponseMessage response = await client.PostAsync(
                "https://api.remove.bg/v1.0/removebg",
                formData
            );

            if (response.IsSuccessStatusCode)
            {
                string FileName = Path.Join(taskFolder, "no-bg.png");
                using FileStream fileStream = new FileStream(
                    FileName,
                    FileMode.Create,
                    FileAccess.ReadWrite,
                    FileShare.None
                );
                await response.Content.CopyToAsync(fileStream);
                await Console.Out.WriteLineAsync($"[+] Wrote {FileName}");
                fileStream.Position = 0;
                return Tuple.Create(FileName, Image.Load(fileStream));
            }
            else
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new RemoveBGException($"[-] Error: {error}");
            }
        }

        public async Task FillBackgroundAsync(
            byte[] input,
            byte[] bgRemoved,
            string inputName,
            string maskName,
            string output,
            string prompt,
            int n,
            string size
        )
        {
            HttpClient httpClient = new HttpClient();
            await Console.Out.WriteLineAsync($"[+] Filling background with the prompt '{prompt}'");
            using ByteArrayContent maskContent = new ByteArrayContent(bgRemoved);
            maskContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");

            using ByteArrayContent imageContent = new ByteArrayContent(input);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");

            using MultipartFormDataContent formData = new MultipartFormDataContent
            {
                { imageContent, "image", inputName },
                { maskContent, "mask", maskName },
                { new StringContent(prompt), "prompt" },
                { new StringContent(n.ToString()), "n" },
                { new StringContent(size), "size" },
            };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                _openai_key
            );

            HttpResponseMessage response = await httpClient.PostAsync(
                "https://api.openai.com/v1/images/edits",
                formData
            );

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"[-] Request to OpenAI API failed with status code: {response.StatusCode}\n\n{await response.Content.ReadAsStringAsync()}"
                );
            }

            string responseContent = await response.Content.ReadAsStringAsync();
            JObject jsonResponse = JObject.Parse(responseContent);

            string url = jsonResponse["data"]![0]!["url"]!.ToString();
            await Console.Out.WriteLineAsync($"[+] Saving result to {output}");
            await Task.Run(() =>
            {
                Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            });
            using FileStream fileStream = new FileStream(
                output,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None
            );
            HttpClient nHttpClient = new HttpClient();
            await nHttpClient.GetAsync(url).Result.Content.CopyToAsync(fileStream);
            await Console.Out.WriteLineAsync($"[+] Wrote {output}");
        }
    }
}
