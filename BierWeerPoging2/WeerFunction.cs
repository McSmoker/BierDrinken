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
        public static async Task RunAsync([QueueTrigger("locations-openweather-in", Connection = "AzureWebJobsStorage")]string myQueueItem, ILogger log)
        {

            try
            {
                string openweatherapikey = Environment.GetEnvironmentVariable("WeatherKey");

                LocationMessage message = JsonConvert.DeserializeObject<LocationMessage>(myQueueItem);

                //Todo delete this en fix hierboven
                if(message.CityName == null)
                {
                    message.CityName = "haarlem";
                }
                try
                {
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
                        await PostMessageToQueue(cloudQueueMessage, storageAccount);
                    }
                    else
                    {
                        log.LogInformation("What a terrible night for a curse");
                    }
                }
                catch (Exception e)
                {
                    log.LogInformation(e.Data.ToString());
                }
            }
            catch (Exception e)
            {
                log.LogInformation(e.Data.ToString());
            }

        }

        private static async Task PostMessageToQueue(CloudQueueMessage cloudQueueMessage, CloudStorageAccount storageAccount)
        {
            var cloudClient = storageAccount.CreateCloudQueueClient();
            var queue = cloudClient.GetQueueReference("locations-openweather-out");
            await queue.CreateIfNotExistsAsync();

            await queue.AddMessageAsync(cloudQueueMessage);
        }

        private static CloudQueueMessage CreateCloudQueueMessage(LocationMessage messageParam, string content)
        {
            WeatherMessage message = CreateBlobTriggerMessage(messageParam, content);
            var messageAsJson = JsonConvert.SerializeObject(message);
            var cloudQueueMessage = new CloudQueueMessage(messageAsJson);
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
