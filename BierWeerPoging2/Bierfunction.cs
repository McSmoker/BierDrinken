using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Blob;
using BierWeerPoging2.Models;
using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;

namespace BierWeerPoging2
{
    public static class Bierfunction
    {
        [FunctionName("Bierfunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string cityName = req.Query["city"];
            //check of city echt is
            string openweatherapikey = Environment.GetEnvironmentVariable("WeatherKey");

            string url = String.Format("https://api.openweathermap.org/data/2.5/weather?q={0}&units=metric",
                cityName);

            HttpClient clientCheck = new HttpClient();

            clientCheck.DefaultRequestHeaders.Add("X-API-KEY", openweatherapikey);

            HttpResponseMessage response = await clientCheck.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Exception customException = new Exception("De ingevoerde naam bestaat niet");
                return new BadRequestObjectResult(customException);
            }


            //input transformatie
            cityName = cityName.ToLower();


            try
            {
                //storage account
                var azureStorageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
                //blob container aanmaken
                string blobReference = "bierweerblob";
                CloudBlobContainer blobContainer = await CreateBlobContainer(azureStorageAccount, blobReference);

                //daadwerkelijk aanmaken van blob
                string guid = Guid.NewGuid().ToString();
                string blobUrl = await CreateCloudBlockBlob(guid);

                CloudQueueMessage cloudQueueMessage = CreateApiMessage(cityName,blobUrl, guid);
                CloudQueueClient client = azureStorageAccount.CreateCloudQueueClient();
                await CreateQueueMessage(cloudQueueMessage, client);

                string result = String.Format("Kan er hier {0} biergedronken worden Deze link bevat het antwoord: \n {1} \n De image is 20 minuten beschikbaar \n Als de image niet gevonden kan worden wacht dan een paar seconden en voer de link opnieuw in", cityName,blobUrl);
                return new OkObjectResult(result);
            }
            catch(Exception e)
            {
                Exception customException = new Exception("er iets fout gegaan probeer het later opnieuw");
                return new BadRequestObjectResult(customException);
            }
        }

        private static async Task CreateQueueMessage(CloudQueueMessage cloudQueueMessage, CloudQueueClient client)
        {
            string weerTrigger = "trigger-weer-in";
            var cloudQueue = client.GetQueueReference(weerTrigger);
            await cloudQueue.CreateIfNotExistsAsync();

            await cloudQueue.AddMessageAsync(cloudQueueMessage);
        }

        private static CloudQueueMessage CreateApiMessage(string cityName, string blobUrl, string guid)
        {
            LocationMessage message = new LocationMessage
            {
                CityName = cityName,
                Blob = blobUrl,
                Guid = guid
            };

            var messageJson = JsonConvert.SerializeObject(message);
            var filledMessage = new CloudQueueMessage(messageJson);
            return filledMessage;
        }

        private static async Task<string> CreateCloudBlockBlob(string guid)
        {

            var azureStorageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            CloudBlobClient blobClient = azureStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference("blob-bier-weer");
            await blobContainer.CreateIfNotExistsAsync();

            //maakt het resultaat beschikbaar voor 20 minuten
            //ben niet 100% zeker van deze implementatie maar was geen harde eis
            var security = blobContainer.GetSharedAccessSignature(new SharedAccessBlobPolicy()
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(20)
            });

            //maken van een url die gebruikt kan worden voor het opvragen van het resultaat
            string imageName = String.Format("{0}.png", guid);
            CloudBlockBlob cloudBlockBlob = blobContainer.GetBlockBlobReference(imageName);
            cloudBlockBlob.Properties.ContentType = "image/png";
            string imageUrlLocation = string.Format("{0}/{1}{2}", blobContainer.StorageUri.PrimaryUri.AbsoluteUri, imageName, security);
            return imageUrlLocation;
        }

        private static async Task<CloudBlobContainer> CreateBlobContainer(CloudStorageAccount azureStorageAccount, string blobReference)
        {
            CloudBlobClient blobClient = azureStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(blobReference);
            await blobContainer.CreateIfNotExistsAsync();

            //zorgt ervoor dat de blob alleen geaccest kan worden door deze client
            BlobContainerPermissions security = new BlobContainerPermissions
            {
                PublicAccess = BlobContainerPublicAccessType.Off
            };
            //TODO zet aan of verwijder security hierboven
            await blobContainer.SetPermissionsAsync(security);
            return blobContainer;
        }

    }
}
