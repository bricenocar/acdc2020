using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Table;

namespace ProcessMessages
{
    public static class RateDevices
    {
        [FunctionName("RateDevices")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var storageConnectionString = Environment.GetEnvironmentVariable("ACDC2020StorageConnectionString");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string deviceId = data?.deviceId;
            int rating = data?.rating;
            TableUtils utils = new TableUtils(storageConnectionString);
            try
            {
                TableResult device = await utils.GetDevice(deviceId);
                if (device.Result != null)
                {
                    int newRatingQuantity = ((MyTableEntity)device.Result).RatingQuantity + 1;
                    await utils.Merge(new MyTableEntity
                    {
                        PartitionKey = "Triksterne bryggeri",
                        RowKey = deviceId,
                        ETag = "*",
                        Rating = (((MyTableEntity)device.Result).Rating + rating),
                        RatingQuantity = newRatingQuantity
                    });
                }
            }
            catch (Exception e)
            {
                log.LogError(e.Message);
            }

            return (ActionResult)new OkObjectResult($"Successfully updated.");
        }
    }
}
