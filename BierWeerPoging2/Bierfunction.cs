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
            //input validatie
            if (req.Query["city"]== "")
            {
                Exception customException = new Exception("de city naam was leeg voer een astublieft een naam in");
                return new BadRequestObjectResult(customException);
            }
            string cityName = req.Query["city"];

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

                log.LogInformation("Created cloud blob: {0}.png", guid);

                CloudQueueMessage cloudQueueMessage = CreateApiMessage(cityName,blobUrl, guid);
                CloudQueueClient client = azureStorageAccount.CreateCloudQueueClient();
                await AddMessageToQueue(cloudQueueMessage, client);

                log.LogInformation("Posted object in queue locations-openweather-in");
                string result = String.Format("KAN JE HIER IN {0} BIERDRINKEN DEZE LINK GAAT JE DAT VERTELLEN G HIJ IS NIET AL TE LANG BESCHIKBAAR DUS WEES ER SNEL BIJ \n{1}", cityName,blobUrl);
                return new OkObjectResult(result);
            }
            catch(Exception e)
            {
                Exception customException = new Exception("er iets fout gegaan probeer het later opnieuw");
                return new BadRequestObjectResult(customException);
            }
        }

        private static async Task AddMessageToQueue(CloudQueueMessage cloudQueueMessage, CloudQueueClient client)
        {
            string openWeatherIn = "locations-openweather-in";
            var cloudQueue = client.GetQueueReference(openWeatherIn);
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

            var messageAsJson = JsonConvert.SerializeObject(message);
            var cloudQueueMessage = new CloudQueueMessage(messageAsJson);
            return cloudQueueMessage;
        }

        private static async Task<string> CreateCloudBlockBlob(string guid)
        {
            //Todo dit opzetten (misschien weghalen van blobcontainer deze wordt ook hier gemaakt?
            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference("beerweather-blobs");
            await blobContainer.CreateIfNotExistsAsync();

            //maakt het resultaat beschikbaar voor 20 minuten
            var sas = blobContainer.GetSharedAccessSignature(new SharedAccessBlobPolicy()
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(20)
            });

            //maken van een url die gebruikt kan worden voor het opvragen van het resultaat
            string fileName = String.Format("{0}.png", guid);
            CloudBlockBlob cloudBlockBlob = blobContainer.GetBlockBlobReference(fileName);
            cloudBlockBlob.Properties.ContentType = "image/png";
            string imageUrl = string.Format("{0}/{1}{2}", blobContainer.StorageUri.PrimaryUri.AbsoluteUri, fileName, sas);
            return imageUrl;
        }

        private static async Task<CloudBlobContainer> CreateBlobContainer(CloudStorageAccount azureStorageAccount, string blobReference)
        {
            CloudBlobClient blobClient = azureStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(blobReference);
            await blobContainer.CreateIfNotExistsAsync();

            //zorgt ervoor dat de blob alleen geaccest kan worden door deze client
            BlobContainerPermissions permissions = new BlobContainerPermissions
            {
                PublicAccess = BlobContainerPublicAccessType.Off
            };

            //await blobContainer.SetPermissionsAsync(permissions);
            return blobContainer;
        }
    }
}
