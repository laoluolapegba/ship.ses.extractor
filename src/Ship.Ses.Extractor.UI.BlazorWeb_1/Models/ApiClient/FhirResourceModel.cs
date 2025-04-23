namespace Ship.Ses.Extractor.UI.BlazorWeb.Models.ApiClient
{
    public class FhirResourceTypeModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Structure { get; set; }
    }

    public class FhirFieldModel
    {
        public string Path { get; set; }
        public string DisplayName { get; set; }
        public string DataType { get; set; }
        public bool IsRequired { get; set; }
        public List<FhirFieldModel> Children { get; set; } = new List<FhirFieldModel>();
    }
}
