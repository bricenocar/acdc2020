using Microsoft.WindowsAzure.Storage.Table;

namespace ProcessMessages
{
    public class MyTableEntity : TableEntity
    {
        public string DeviceId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Picture { get; set; }
        public string Location { get; set; }
        public string Type { get; set; }
        public string MinValue { get; set; }
        public string MaxValue { get; set; }
    }
}
