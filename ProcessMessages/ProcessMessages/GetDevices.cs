using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.WindowsAzure.Storage.Table;

namespace ProcessMessages
{
    public static class GetDevices
    {
        static RegistryManager registryManager;
        private static List<DeviceEntity> listOfDevices;

        [FunctionName("GetDevices")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var connString = Environment.GetEnvironmentVariable("ACDC2020StorageConnectionString");
            // Get list of devices available
            listOfDevices = new List<DeviceEntity>();
            registryManager = RegistryManager.CreateFromConnectionString(connString);
            var devices = await GetDevicesInfo();

            // Check that devices already exists in table
            TableUtils utils = new TableUtils(connString);
            foreach (var device in devices)
            {
                try
                {
                    TableResult iotDevice = await utils.GetDevice(device.Id);
                    if (iotDevice.Result == null)
                    {
                        await utils.Insert(new MyTableEntity
                        {
                            PartitionKey = "Triksterne bryggeri",
                            RowKey = device.Id,
                            ETag = "*"
                        });
                    }
                }
                catch (Exception e)
                {
                    log.LogError(e.Message);
                }
            }

            var jsonToReturn = JsonConvert.SerializeObject(devices);

            return devices != null
                ? (ActionResult)new OkObjectResult(jsonToReturn)
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }

        public static async Task<List<DeviceEntity>> GetDevicesInfo()
        {
            try
            {
                DeviceEntity deviceEntity;
                IQuery query = registryManager.CreateQuery("select * from devices", null); ;

                while (query.HasMoreResults)
                {
                    IEnumerable<Twin> page = await query.GetNextAsTwinAsync();
                    foreach (Twin twin in page)
                    {
                        deviceEntity = new DeviceEntity()
                        {
                            Id = twin.DeviceId,
                            ConnectionState = twin.ConnectionState.ToString(),
                            LastActivityTime = twin.LastActivityTime,
                            LastStateUpdatedTime = twin.StatusUpdatedTime,
                            MessageCount = twin.CloudToDeviceMessageCount,
                            State = twin.Status.ToString(),
                            SuspensionReason = twin.StatusReason,

                        };

                        deviceEntity.PrimaryThumbPrint = twin.X509Thumbprint?.PrimaryThumbprint;
                        deviceEntity.SecondaryThumbPrint = twin.X509Thumbprint?.SecondaryThumbprint;

                        listOfDevices.Add(deviceEntity);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return listOfDevices;
        }
        public class DeviceEntity : IComparable<DeviceEntity>
        {
            public string Id { get; set; }
            public string PrimaryKey { get; set; }
            public string SecondaryKey { get; set; }
            public string PrimaryThumbPrint { get; set; }
            public string SecondaryThumbPrint { get; set; }
            public string ConnectionString { get; set; }
            public string ConnectionState { get; set; }
            public DateTime? LastActivityTime { get; set; }
            public DateTime? LastConnectionStateUpdatedTime { get; set; }
            public DateTime? LastStateUpdatedTime { get; set; }
            public int? MessageCount { get; set; }
            public string State { get; set; }
            public string SuspensionReason { get; set; }

            public int CompareTo(DeviceEntity other)
            {
                return string.Compare(this.Id, other.Id, StringComparison.OrdinalIgnoreCase);
            }

            public override string ToString()
            {
                return $"Device ID = {this.Id}, Primary Key = {this.PrimaryKey}, Secondary Key = {this.SecondaryKey}, Primary Thumbprint = {this.PrimaryThumbPrint}, Secondary Thumbprint = {this.SecondaryThumbPrint}, ConnectionString = {this.ConnectionString}, ConnState = {this.ConnectionState}, ActivityTime = {this.LastActivityTime}, LastConnState = {this.LastConnectionStateUpdatedTime}, LastStateUpdatedTime = {this.LastStateUpdatedTime}, MessageCount = {this.MessageCount}, State = {this.State}, SuspensionReason = {this.SuspensionReason}\r\n";
            }
        }

    }
}
