using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using BierWeerPoging2.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace BierWeerPoging2
{
    public static class KaartFunction
    {
        [FunctionName("KaartFunction")]
        public static async Task RunAsync([QueueTrigger("locations-openweather-out", Connection = "AzureWebjobsStorage")]string myQueueItem, ILogger log)
        {
            WeatherMessage message = (WeatherMessage)JsonConvert.DeserializeObject(myQueueItem, typeof(WeatherMessage));
            WeatherRoot weatherRoot = message.Weather;
            Coord coordinates = weatherRoot.Coord;

            string key = Environment.GetEnvironmentVariable("MapKey");
            string url = String.Format("https://atlas.microsoft.com/map/static/png?subscription-key={0}&api-version=1.0&center={1},{2}", key,
                coordinates.Lon, coordinates.Lat);
            try
            {

                var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
                CloudBlockBlob cloudBlockBlob = await GetCloudBlockBlob(message, storageAccount);

                HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    log.LogInformation("Response is binnen");

                    try
                    {
                        Stream responseContent = await response.Content.ReadAsStreamAsync();

                        string textToWrite = GenerateBeerText(weatherRoot);

                        ImageTextWriter imageTextWriter = new ImageTextWriter();
                        Stream renderedImage = imageTextWriter.WriteTextOnImage(responseContent, textToWrite);

                        log.LogInformation("Uploading response to blob");
                        await cloudBlockBlob.UploadFromStreamAsync(renderedImage);
                        log.LogInformation("Uploaded response to blob");
                    }
                    catch(Exception e)
                    {
                        var test = 1 ;
                    }
                }
                else
                {
                    log.LogInformation(response.StatusCode.ToString());
                }



            }
            catch
            {

            }
        }
        private static async Task<CloudBlockBlob> GetCloudBlockBlob(WeatherMessage message, CloudStorageAccount storageAccount)
        {
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference("beerweather-blobs");
            await blobContainer.CreateIfNotExistsAsync();
            string fileName = String.Format("{0}.png", message.Guid);
            CloudBlockBlob cloudBlockBlob = blobContainer.GetBlockBlobReference(fileName);
            cloudBlockBlob.Properties.ContentType = "image/png";
            return cloudBlockBlob;
        }

        private static string GenerateBeerText(WeatherRoot weather)
        {
            string maintext ="";
            if (weather.Main.Temp < 15)
            {
                maintext = "Brr het is een beetje te koud voor bier drink wat gore gluhwein ofzo";
            }
            else
            {
                maintext = "Wat een lekker weer voor een biertje";
            }
            return maintext;
        }
    }
}
