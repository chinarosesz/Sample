using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace Core.Models
{
    public class CustomTableEntity : TableEntity
    {
        public DateTime EtlIngestDate { get; set; }
    }
}
