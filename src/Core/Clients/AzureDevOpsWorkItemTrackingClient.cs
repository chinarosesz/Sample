using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System;
using System.Threading.Tasks;

namespace Core
{
    public class AzureDevOpsWorkItemTrackingClient : WorkItemTrackingHttpClient
    {
        public AzureDevOpsWorkItemTrackingClient(Uri baseUrl, VssCredentials credentials) : base(baseUrl, credentials)
        {
        }

        public async Task<WorkItem> CreateBugAsync(string title, string project)
        {
            JsonPatchDocument jsonPatchDocument = new JsonPatchDocument
            {
                new JsonPatchOperation
                {
                    Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                    Path = "/fields/System.Title",
                    Value = "Test bug"
                },
            };

            WorkItem createdWorkItem = await this.CreateWorkItemAsync(jsonPatchDocument, project, "Issue");

            return createdWorkItem;
        }
    }
}
