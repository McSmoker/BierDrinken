using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;

namespace BierWeerPoging2
{
    public static class HomePage
    {
        [FunctionName("HomePage")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {


            // Option 1, coded HTML
            // Option 2, txtfilehtml
            //maar hoe werkt dat dan op server?
            // option 1 prima

            string html = @"
            <html>
                <head><title>BIER?!WAAAAAT</title></head>
                    <body>
                        <h1>VUL IN WAAR JE WOONT</h1>
                            <form action=""/api/bierfunction"">
                            WAAR WOON JE: <input type = ""text"" name = ""city"" >
                       <input type = ""submit"" value = ""DRINK BIER?"" >
                      </form >
                 </body >
            </html > 
            ";

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(html);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            return response;
        }
    }
}
