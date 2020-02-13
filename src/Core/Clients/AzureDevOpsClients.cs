using Microsoft.Identity.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;
using System;

namespace Core
{
    public sealed class AzureDevOpsClients : IDisposable
    {
        public string OrganizationName { get; private set; }
        public VssConnection VssConnection { get; private set; }
        public AzureDevOpsWorkItemTrackingClient WorkItemTrackingClient { get; private set; }

        public AzureDevOpsClients(string organization, string personalAccessToken)
        {
            this.IntializeClients(organization, personalAccessToken);
        }

        public AzureDevOpsClients(string organization, AuthenticationResult authenticationResult)
        {
            this.IntializeClients(organization, authenticationResult: authenticationResult);
        }

        public void Dispose()
        {
            this.VssConnection?.Dispose();
        }

        // Init Devops clients with either a personal access token (basic auth) or pass in authentication result with an access token (bearer)
        private void IntializeClients(string organization, string personalAccessToken = null, AuthenticationResult authenticationResult = null)
        {
            Uri collectionUri = new Uri($"https://dev.azure.com/{organization}");
            this.OrganizationName = organization;
            VssCredentials vssCredentials = null;

            // Connect
            if (personalAccessToken != null)
            {
                VssBasicCredential basicCredential = new VssBasicCredential(string.Empty, personalAccessToken);
                vssCredentials = basicCredential;
                this.VssConnection = new VssConnection(collectionUri, vssCredentials);

            }
            else if (authenticationResult != null)
            {
                VssOAuthAccessTokenCredential oAuthCredentials = new VssOAuthAccessTokenCredential(authenticationResult.AccessToken);
                vssCredentials = oAuthCredentials;
                this.VssConnection = new VssConnection(collectionUri, vssCredentials);
            }

            // Configure timeout and retry on DevOps HTTP clients
            this.VssConnection.Settings.SendTimeout = TimeSpan.FromMinutes(5);
            this.VssConnection.Settings.MaxRetryRequest = 2;

            // Initialize clients
            this.WorkItemTrackingClient = new AzureDevOpsWorkItemTrackingClient(collectionUri, vssCredentials);
        }
    }
}
