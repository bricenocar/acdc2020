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
using Microsoft.WindowsAzure.Storage;

namespace ProcessMessages
{
    public static class AddEditDevice
    {

        [FunctionName("AddEditDevice")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var storageConnectionString = Environment.GetEnvironmentVariable("ACDC2020StorageConnectionString");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string deviceId = data?.deviceid;
            string name = data?.name;
            string description = data?.description;
            string picture = data?.picture;
            string location = data?.location;
            string type = data?.type;
            string minvalue1 = data?.minvalue;
            string maxvalue1 = data?.maxvalue;
            string minvalue2 = data?.minvalue;
            string maxvalue2 = data?.maxvalue;
            string minvalue3 = data?.minvalue;
            string maxvalue3 = data?.maxvalue;
            TableUtils utils = new TableUtils(storageConnectionString);
            try
            {
                TableResult device = await utils.GetDevice(deviceId);
                if (device.Result != null)
                {
                    await utils.Update(new MyTableEntity
                    {
                        PartitionKey = "Triksterne bryggeri",
                        RowKey = deviceId,
                        ETag = "*",
                        Name = name,
                        Description = description,
                        Picture = picture,
                        Location = location,
                        Type = type,
                        MinValue1 = minvalue1,
                        MaxValue1 = maxvalue1,
                        MinValue2 = minvalue2,
                        MaxValue2 = maxvalue2,
                        MinValue3 = minvalue3,
                        MaxValue3 = maxvalue3
                    });
                }
            }
            catch (Exception e)
            {
                log.LogError(e.Message);
            }            

            return deviceId != null
                ? (ActionResult)new OkObjectResult($"Device with ID: {deviceId} has been successfully updated")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }        
    }
}
