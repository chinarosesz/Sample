namespace Core.Models.AzureDevOps
{
    public class DevopsBuildEntity : CustomTableEntity
    {
        public int BuildId { get; set; }
        public string BuildNumber { get; set; }
        public int DefinitionId { get; set; }
        public string ProjectName { get; set; }
        public string RequestUrl { get; set; }
        public string Data { get; set; }
        public string BuildDefinitionName { get; set; }
    }
}
