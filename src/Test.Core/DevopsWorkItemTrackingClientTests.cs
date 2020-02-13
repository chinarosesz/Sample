using Core;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Test.Core
{
    [TestClass]
    public class DevopsWorkItemTrackingClientTests
    {
        private const string Organization = "litradev";
        private const string Project = "litradev";

        [TestMethod]
        public async Task Verify_CreateWorkItemAsync()
        {
            // Setup
            string personalAccessToken = await AzureKeyVaultClient.GetVsoPersonalAccessTokenAsync();
            AzureDevOpsClients devopsClients = new AzureDevOpsClients(Organization, personalAccessToken);

            // Act
            WorkItem createdWorkItem = await devopsClients.WorkItemTrackingClient.CreateBugAsync("This is a test bug", Project);
            
            // Verify
            Assert.IsNotNull(createdWorkItem);
            Assert.IsTrue(createdWorkItem.Id > 0);

            // Cleanup
            WorkItemDelete deletedWorkItem = await devopsClients.WorkItemTrackingClient.DeleteWorkItemAsync(Project, createdWorkItem.Id.Value);
            Assert.IsNotNull(deletedWorkItem);
            Assert.AreEqual(deletedWorkItem.Id, createdWorkItem.Id);
        }
    }
}
