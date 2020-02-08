using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Devices;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Serialization;
using Microsoft.WindowsAzure.Storage.Table;

namespace ProcessMessages
{
    public static class GetAllLastDataFromDevices
    {
        private static CloudTable mytable = null;
        static RegistryManager registryManager;
        private static List<DeviceEntity> listOfDevices;

        [FunctionName("GetAllLastDataFromDevices")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var appHubconnString = Environment.GetEnvironmentVariable("ACDC2020EventHubConnectionString");
            var storageConnectionString = Environment.GetEnvironmentVariable("ACDC2020StorageConnectionString");
            listOfDevices = new List<DeviceEntity>();
            // Get list of devices available
            List<SensorFullData> iotDevices = new List<SensorFullData>();
            registryManager = RegistryManager.CreateFromConnectionString(appHubconnString);
            var devices = await GetDevicesInfo();
            foreach (var device in devices)
            {
                TableUtils utils = new TableUtils(storageConnectionString);
                TableResult iotDevice = await utils.GetDevice(device.Id);
                IList<MessagesEntity> deviceData = await utils.GetLastDataFromDevice(device.Id);
                foreach (var item in deviceData)
                {
                    iotDevices.Add(new SensorFullData()
                    {
                        DeviceId = item.DeviceId,
                        Name = ((MyTableEntity)iotDevice.Result).Name,
                        Description = ((MyTableEntity)iotDevice.Result).Description,
                        Picture = ((MyTableEntity)iotDevice.Result).Picture,
                        Location = ((MyTableEntity)iotDevice.Result).Location,
                        Type = ((MyTableEntity)iotDevice.Result).Type,
                        MinValue1 = ((MyTableEntity)iotDevice.Result).MinValue1,
                        MinValue2 = ((MyTableEntity)iotDevice.Result).MinValue2,
                        MinValue3 = ((MyTableEntity)iotDevice.Result).MinValue3,
                        MaxValue1 = ((MyTableEntity)iotDevice.Result).MaxValue1,
                        MaxValue2 = ((MyTableEntity)iotDevice.Result).MaxValue2,
                        MaxValue3 = ((MyTableEntity)iotDevice.Result).MaxValue3,
                        Value1 = item.Value1,
                        Value2 = item.Value2,
                        Value3 = item.Value3,
                    });
                }
            }

            var jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            var jsonToReturn = JsonConvert.SerializeObject(iotDevices, jsonSerializerSettings);


            return (ActionResult)new OkObjectResult(jsonToReturn);
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

        public class SensorFullData : TableEntity
        {
            public string DeviceId { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Picture { get; set; }
            public string Location { get; set; }
            public string Type { get; set; }
            public string MinValue1 { get; set; }
            public string MaxValue1 { get; set; }
            public string MinValue2 { get; set; }
            public string MaxValue2 { get; set; }
            public string MinValue3 { get; set; }
            public string MaxValue3 { get; set; }
            public double Value1 { get; set; }
            public double Value2 { get; set; }
            public double Value3 { get; set; }
        }
    }
}
