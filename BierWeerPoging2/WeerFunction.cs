using System;
using System.Net.Http;
using System.Threading.Tasks;
using BierWeerPoging2.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace BierWeerPoging2
{
    public static class WeerFunction
    {
        [FunctionName("WeerFunction")]
        public static async Task RunAsync([QueueTrigger("trigger-weer-in", Connection = "AzureWebJobsStorage")]string myQueueItem, ILogger log)
        {

            string openweatherapikey = Environment.GetEnvironmentVariable("WeatherKey");
            
            LocationMessage message = JsonConvert.DeserializeObject<LocationMessage>(myQueueItem);
            
            string url = String.Format("https://api.openweathermap.org/data/2.5/weather?q={0}&units=metric",
                message.CityName);
            
            HttpClient client = new HttpClient();
            
            client.DefaultRequestHeaders.Add("X-API-KEY",openweatherapikey);
            
            HttpResponseMessage response = await client.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                CloudQueueMessage cloudQueueMessage = CreateCloudQueueMessage(message, content);
            
                var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
                await CreateQueueMessage(cloudQueueMessage, storageAccount);
            }
            else
            {
                //error handling voor als de weerapi kapot is
                string content = "wat jammer nou de weer api is kapot";
                CloudQueueMessage cloudQueueMessage = CreateCloudQueueMessage(message, content);
            
                var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
                await CreateQueueMessage(cloudQueueMessage, storageAccount);
            }
            
        }

        private static async Task CreateQueueMessage(CloudQueueMessage queueMessage, CloudStorageAccount azureStorageAccount)
        {
            var cloudClient = azureStorageAccount.CreateCloudQueueClient();
            var queu = cloudClient.GetQueueReference("trigger-kaart-in");
            await queu.CreateIfNotExistsAsync();

            await queu.AddMessageAsync(queueMessage);
        }

        private static CloudQueueMessage CreateCloudQueueMessage(LocationMessage messageParam, string content)
        {
            WeatherMessage message = CreateBlobTriggerMessage(messageParam, content);
            var messageJson = JsonConvert.SerializeObject(message);
            var cloudQueueMessage = new CloudQueueMessage(messageJson);
            return cloudQueueMessage;
        }

        private static WeatherMessage CreateBlobTriggerMessage(LocationMessage messageParam, string content)
        {
            WeatherRoot weather = (WeatherRoot)JsonConvert.DeserializeObject(content, typeof(WeatherRoot));
            WeatherMessage message = new WeatherMessage(weather)
            {
                CityName = messageParam.CityName,
                Blob = messageParam.Blob,
                Guid = messageParam.Guid,
            };
            return message;
        }
    }
}
