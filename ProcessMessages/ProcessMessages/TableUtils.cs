using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ProcessMessages
{
    public class TableUtils
    {
        private static CloudTable mytable = null;
        private static string _connString;
        private string DEFAULT_PARTITION = "Triksterne bryggeri";

        public TableUtils(string storageConnectionString)
        {
            _connString = storageConnectionString;
        }

        private CloudTable AuthTable()
        {
            try
            {
                CloudStorageAccount storageAccount = CreateStorageAccountFromConnectionString(_connString);

                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

                mytable = tableClient.GetTableReference("DeviceManager");

                return mytable;
            }
            catch
            {
                return null;
            }
        }

        public CloudStorageAccount CreateStorageAccountFromConnectionString(string storageConnectionString)
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
        public async Task<TableResult> GetDevice(string value)
        {
            AuthTable();
            var operation = TableOperation.Retrieve(DEFAULT_PARTITION, value);
            TableResult result = await mytable.ExecuteAsync(operation);
            if (result != null)
            {
                return result;
            }

            return null;
        }

        public async Task Update(MyTableEntity value)
        {
            var operation = TableOperation.Replace(value);
            await mytable.ExecuteAsync(operation);
        }
        public async Task Insert(MyTableEntity value)
        {
            var operation = TableOperation.InsertOrReplace(value);
            await mytable.ExecuteAsync(operation);
        }

    }
}
