using Azure;
using Azure.AI.Language.QuestionAnswering;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace Labb1Azure
{
    internal class Program
    {
        private static string translatorEndpoint = "https://api.cognitive.microsofttranslator.com";
        private static string cogSvcKey;
        private static string cogSvcRegion;

        static async Task Main(string[] args)
        {
            // Get config settings from AppSettings
            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            IConfigurationRoot configuration = builder.Build();
            cogSvcKey = configuration["CognitiveServiceKey"];
            cogSvcRegion = configuration["CognitiveServiceRegion"];


            //Config for the language service
            Uri endpoint = new Uri("https://qnaconsolewesteurope.cognitiveservices.azure.com/"); //Language Endpoint
            AzureKeyCredential credential = new AzureKeyCredential("2e5e39f0f9a445d8a124541ff74a27b3"); // Language Key
            string projectName = "LearnFAQ"; // Project name
            string deploymentName = "production";

            QuestionAnsweringClient client = new QuestionAnsweringClient(endpoint, credential);
            QuestionAnsweringProject project = new QuestionAnsweringProject(projectName, deploymentName);

            string input = "";
            string translatedText = "";

            do
            {

                Console.Write("Ask your question: ");
                input = Console.ReadLine();
                string question = input;

                //Detect the langugage
                string language = await GetLanguage(question);
                Console.WriteLine("Language: " + language);

                // Translate if not already English
                if (language != "en")
                {
                    translatedText = await Translate(question, language);
                    Console.WriteLine("\nTranslation:\n" + translatedText);
                }


                Response<AnswersResult> response = client.GetAnswers(translatedText, project);

                foreach (KnowledgeBaseAnswer answer in response.Value.Answers)
                {
                    Console.WriteLine($"Q:{question}");
                    Console.WriteLine($"A:{answer.Answer}");
                    Console.WriteLine($"({answer.Confidence})");
                }

                Console.WriteLine("Hit enter to proceed or write 'quit' to exit");
                input = Console.ReadLine();

                Console.Clear();

            } while (input.ToLower() != "quit");
        }
        static async Task<string> GetLanguage(string text)
        {
            // Default language is English
            string language = "en";

            // Use the Translator detect function
            object[] body = new object[] { new { Text = text } };
            var requestBody = JsonConvert.SerializeObject(body);
            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage())
                {
                    // Build the request
                    string path = "/detect?api-version=3.0";
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(translatorEndpoint + path);
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    request.Headers.Add("Ocp-Apim-Subscription-Key", cogSvcKey);
                    request.Headers.Add("Ocp-Apim-Subscription-Region", cogSvcRegion);

                    // Send the request and get response
                    HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                    // Read response as a string
                    string responseContent = await response.Content.ReadAsStringAsync();

                    // Parse JSON array and get language
                    JArray jsonResponse = JArray.Parse(responseContent);
                    language = (string)jsonResponse[0]["language"];
                }
            }


            // return the language
            return language;
        }
        static async Task<string> Translate(string text, string sourceLanguage)
        {
            string translation = "";

            // Use the Translator translate function
            object[] body = new object[] { new { Text = text } };
            var requestBody = JsonConvert.SerializeObject(body);
            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage())
                {
                    // Build the request
                    string path = "/translate?api-version=3.0&from=" + sourceLanguage + "&to=en";
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(translatorEndpoint + path);
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    request.Headers.Add("Ocp-Apim-Subscription-Key", cogSvcKey);
                    request.Headers.Add("Ocp-Apim-Subscription-Region", cogSvcRegion);

                    // Send the request and get response
                    HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                    // Read response as a string
                    string responseContent = await response.Content.ReadAsStringAsync();

                    // Parse JSON array and get translation
                    JArray jsonResponse = JArray.Parse(responseContent);
                    translation = (string)jsonResponse[0]["translations"][0]["text"];
                }
            }


            // Return the translation
            return translation;
        }
    }
}