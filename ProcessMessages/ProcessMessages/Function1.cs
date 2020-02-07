using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;
using IoTHubTrigger = Microsoft.Azure.WebJobs.EventHubTriggerAttribute;

namespace ProcessMessages
{
    public static class Function1
    {
        private static CloudTable mytable = null;

        [FunctionName("Function1")]
        public static void Run([IoTHubTrigger("samples-workitems", Connection = "ACDC2020-IOTHUB_events_IOTHUB")]EventData message, ILogger log)
        {
            log.LogInformation($"C# IoT Hub trigger function processed a message: {Encoding.UTF8.GetString(message.Body.Array)}");
            var storageConnectionString = Environment.GetEnvironmentVariable("ACDC2020StorageConnectionString");
            try
            {
                var payload = Encoding.ASCII.GetString(message.Body.Array,
                    message.Body.Offset,
                    message.Body.Count);

                var deviceId = message.SystemProperties["iothub-connection-device-id"];
                var sensorData = JsonConvert.DeserializeObject<TempHumidity>(payload);
                sensorData.DeviceId = deviceId.ToString();
                mytable = AuthTable(storageConnectionString);
                Task.Run(async () =>
                {
                    await Create(sensorData);
                });
            }
            catch (Exception e)
            {
                log.LogError(e.Message);
            }
        }
        private static CloudTable AuthTable(string storageConnectionString)
        {
            try
            {
                CloudStorageAccount storageAccount = CreateStorageAccountFromConnectionString(storageConnectionString);

                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

                mytable = tableClient.GetTableReference("DeviceInputManager");

                return mytable;
            }
            catch
            {
                return null;
            }
        }

        public static CloudStorageAccount CreateStorageAccountFromConnectionString(string storageConnectionString)
        {
            CloudStorageAccount storageAccount;
            try
            {
                storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            }
            catch (FormatException)
            {
                Console.WriteLine("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file - then restart the application.");
                throw;
            }
            catch (ArgumentException)
            {
                Console.WriteLine("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file - then restart the sample.");
                Console.ReadLine();
                throw;
            }

            return storageAccount;
        }

        private static async Task Create(TempHumidity value)
        {
            await CreateOrUpdate(new MyTableEntity
            {
                PartitionKey = "Triksterne bryggeri",
                RowKey = Guid.NewGuid().ToString(),
                DeviceId = value.DeviceId,
                Value1 = value.Temperature,
                Value2 = value.Humidity,
                Value3 = value.GassFlow
            });
        }

        private static async Task CreateOrUpdate(MyTableEntity myTableOperation)
        {
            var operation = TableOperation.InsertOrReplace(myTableOperation);
            await mytable.ExecuteAsync(operation);
        }

        public class MyTableEntity : TableEntity
        {
            public string DeviceId { get; set; }
            public double Value1 { get; set; }
            public double Value2 { get; set; }
            public double Value3 { get; set; }
        }
        public class TempHumidity
        {
            public string DeviceId { get; set; }
            public double Temperature { get; set; }
            public double Humidity { get; set; }
            public double GassFlow { get; set; }
        }

    }
}