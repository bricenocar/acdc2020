using Microsoft.WindowsAzure.Storage.Table;

namespace ProcessMessages
{
    public class MyTableEntity : TableEntity
    {        
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
        public int Rating { get; set; }
        public int RatingQuantity { get; set; }
    }
}
