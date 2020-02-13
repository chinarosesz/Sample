using System;

namespace Core.Models.AzureDevOps
{
    public class DevopsProjectEntity : CustomTableEntity
    {
        public Guid Id { get; set; }
        public string Abbreviation { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string State { get; set; }
        public long Revision { get; set; }
        public string Visibility { get; set; }
        public string DefaultTeamImageUrl { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public string Data { get; set; }
    }
}
