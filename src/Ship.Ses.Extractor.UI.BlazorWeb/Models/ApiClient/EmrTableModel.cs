namespace Ship.Ses.Extractor.UI.BlazorWeb.Models.ApiClient
{
    public class EmrTableModel
    {
        public string Name { get; set; }
        public List<EmrColumnModel> Columns { get; set; } = new List<EmrColumnModel>();
    }

    public class EmrColumnModel
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
    }
}
