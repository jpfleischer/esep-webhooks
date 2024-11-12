using System.Text;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using System.Net.Http;
using System.IO;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook
{
    public class Function
    {
        /// <summary>
        /// Function that receives GitHub webhook data, extracts the issue URL, and posts it to Slack.
        /// </summary>
        /// <param name="input">The GitHub webhook payload in JSON format.</param>
        /// <param name="context">The Lambda context for logging and environment details.</param>
        /// <returns>The response from Slack or an error message.</returns>
        public string FunctionHandler(string input, ILambdaContext context)
        {
            context.Logger.LogInformation($"FunctionHandler received: {input}");

            try
            {
                // Parse JSON input
                dynamic json = JsonConvert.DeserializeObject<dynamic>(input);

                // Extract the issue URL
                string issueUrl = json.issue?.html_url;
                if (string.IsNullOrEmpty(issueUrl))
                {
                    context.Logger.LogError("No issue URL found in the payload.");
                    return "Error: No issue URL found in the payload.";
                }

                context.Logger.LogInformation($"Issue URL: {issueUrl}");

                // Format payload for Slack
                string payload = $"{{\"text\":\"Issue Created: {issueUrl}\"}}";

                // Send POST request to Slack
                using (var client = new HttpClient())
                {
                    var webRequest = new HttpRequestMessage(HttpMethod.Post, Environment.GetEnvironmentVariable("SLACK_URL"))
                    {
                        Content = new StringContent(payload, Encoding.UTF8, "application/json")
                    };

                    var response = client.Send(webRequest);
                    response.EnsureSuccessStatusCode(); // Ensure we receive a successful status code

                    using var reader = new StreamReader(response.Content.ReadAsStream());
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error processing GitHub webhook: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }
    }
}
