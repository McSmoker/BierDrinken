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
        public static async Task RunAsync([QueueTrigger("trigger-kaart-in", Connection = "AzureWebjobsStorage")]string myQueueItem, ILogger log)
        {

            WeatherMessage message = (WeatherMessage)JsonConvert.DeserializeObject(myQueueItem, typeof(WeatherMessage));
            //weatherdata nodig om de locatie te bepalen
            WeatherRoot weatherRoot = message.Weather;
            Coord coordinates = weatherRoot.Coord;
            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            CloudBlockBlob cloudBlockBlob = await GetBlobStorage(message, storageAccount);
            try
            {
                Stream responseContent = null;
                try
                {
                    string key = Environment.GetEnvironmentVariable("MapKey");
                    string url = String.Format("https://atlas.microsoft.com/map/static/png?subscription-key={0}&api-version=1.0&center={1},{2}", key,
                        coordinates.Lon, coordinates.Lat);
                }
                catch
                {
                    using (System.Net.WebClient webClient = new System.Net.WebClient())
                    {
                        using (Stream stream = webClient.OpenRead("https://upload.wikimedia.org/wikipedia/commons/thumb/0/08/Un1.svg/120px-Un1.svg.png"))
                        {
                            
                            await cloudBlockBlob.UploadFromStreamAsync(stream);
                        }
                    }
                }
                try
                {
                    string key = Environment.GetEnvironmentVariable("MapKey");
                    string url = String.Format("https://atlas.microsoft.com/map/static/png?subscription-key={0}&api-version=1.0&center={1},{2}", key,
                        coordinates.Lon, coordinates.Lat);
                    HttpClient client = new HttpClient();
                    HttpResponseMessage response = await client.GetAsync(url);

                    responseContent = await response.Content.ReadAsStreamAsync();
                }
                catch
                {
                    using (System.Net.WebClient webClient = new System.Net.WebClient())
                    {
                        using (Stream stream = webClient.OpenRead("https://upload.wikimedia.org/wikipedia/commons/6/61/NYCS-bull-trans-2.svg"))
                        {

                            await cloudBlockBlob.UploadFromStreamAsync(stream);
                        }
                    }
                }

                using (System.Net.WebClient webClient = new System.Net.WebClient())
                {
                    using (Stream stream = webClient.OpenRead("http://pngimg.com/uploads/number3/number3_PNG14965.png"))
                    {

                        await cloudBlockBlob.UploadFromStreamAsync(stream);
                    }
                }
                //string textToWrite = GenerateBeerText(weatherRoot);

                //ImageTextWriter imageTextWriter = new ImageTextWriter();
                //Stream renderedImage = imageTextWriter.WriteTextOnImage(responseContent, textToWrite);

                //await cloudBlockBlob.UploadFromStreamAsync(renderedImage);
            }
            catch (Exception e)
            {
                //mocht er iets misgaan in het ophalen van de image (azure maps doet weer eens moeilijk) upload dan deze image
                //dan krijgt de client een beter error dan"geen image in deze blob"
                using (System.Net.WebClient webClient = new System.Net.WebClient())
                {
                    using (Stream stream = webClient.OpenRead("http://www.ajeforum.com/wp-content/uploads/2018/04/Error_Culture_Florent_Darrault2-640x478.jpg"))
                    {
                        string textToWrite = e.Message.ToString();

                        ImageTextWriter imageTextWriter = new ImageTextWriter();
                        Stream renderedImage = imageTextWriter.WriteTextOnImage(stream, textToWrite);
                        await cloudBlockBlob.UploadFromStreamAsync(renderedImage);
                    }
                }
            }
        }
        private static async Task<CloudBlockBlob> GetBlobStorage(WeatherMessage message, CloudStorageAccount storageAccount)
        {
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference("blob-bier-weer");
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
