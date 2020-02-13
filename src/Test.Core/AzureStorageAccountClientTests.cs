using Core;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Test.Core
{
    [TestClass]
    public class AzureStorageAccountClientTests
    {
        [TestMethod]
        public async Task AdHocHowToUseStorageAccount()
        {
            CloudStorageAccount dataIngestionStorageAccount = CloudStorageAccount.Parse(await AzureKeyVaultClient.GetStorageConnectionStringAsync());
            CloudBlobClient blobClient = dataIngestionStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("azuredevopscontainer");
            await container.CreateIfNotExistsAsync();

            CloudBlockBlob blockBlob = container.GetBlockBlobReference("project");
            await blockBlob.UploadFromFileAsync(@"c:\temp\what.txt");
        }

        /// <summary>
        /// This method validates auto rotation of storage key by getting a SAS token before it expires
        /// The expiration date is set up when creating a SAS secret which is documented here
        /// https://docs.microsoft.com/en-us/azure/key-vault/key-vault-ovw-storage-keys#step-by-step-instructions-on-how-to-use-key-vault-to-create-and-generate-sas-tokens
        /// </summary>
        [TestMethod]
        public async Task AutoRotateStorageKeyAndGetNewOneWhenExpired()
        {
            // This SAS token stored in KeyVault was set up to expire after one minute once a new token is retrieved by calling GetSecret
            KeyVaultClient keyVault = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(new AzureServiceTokenProvider().KeyVaultTokenCallback));
            SecretBundle sasToken = await keyVault.GetSecretAsync("https://litrakeyvault.vault.azure.net/secrets/litrastorage-ReadWrite");

            StorageCredentials accountSasCredential = new StorageCredentials(sasToken.Value);
            CloudStorageAccount accountWithSas = new CloudStorageAccount(accountSasCredential, new Uri("https://litrastorage.blob.core.windows.net/"), null, null, null);
            CloudBlobClient blobClientWithSas = accountWithSas.CreateCloudBlobClient();
            CloudBlobContainer container = blobClientWithSas.GetContainerReference("testcontainer");
            await container.CreateIfNotExistsAsync();

            Assert.IsNotNull(blobClientWithSas);
            Console.WriteLine(sasToken);
            Console.WriteLine(accountSasCredential);

            Console.WriteLine("Waiting for one minute starting now...");
            await Task.Delay(TimeSpan.FromMinutes(1));
            Console.WriteLine("Done waiting for one minute");

            Console.WriteLine("Trying to access storage again should fail because token expires after one minute");
            container = blobClientWithSas.GetContainerReference("testcontainer");
            await container.CreateIfNotExistsAsync();
        }

        [TestMethod]
        public async Task AuthUsingSASTokenWithRotation()
        {
            KeyVaultClient keyVault = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(new AzureServiceTokenProvider().KeyVaultTokenCallback));
            SecretBundle sasToken = await keyVault.GetSecretAsync("https://devdataingestion.vault.azure.net/secrets/devdataingestion-ReadWrite");

            StorageCredentials accountSasCredential = new StorageCredentials(sasToken.Value);
            CloudStorageAccount accountWithSas = new CloudStorageAccount(accountSasCredential, new Uri("https://deletemeanytime1.blob.core.windows.net/"), new Uri("https://deletemeanytime1.queue.core.windows.net/"), new Uri("https://deletemeanytime1.table.core.windows.net/"), new Uri("https://deletemeanytime1.file.core.windows.net/"));

            CloudBlobClient blobClient = accountWithSas.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("testcontainer");
            await container.CreateIfNotExistsAsync();

            CloudTableClient tableClient = accountWithSas.CreateCloudTableClient();
            CloudTable tableContainer = tableClient.GetTableReference("testcontainer");
            await tableContainer.CreateIfNotExistsAsync();
        }

        [TestMethod]
        public async Task GenerateSasTokenFromStorageKey()
        {
            KeyVaultClient keyVault = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(new AzureServiceTokenProvider().KeyVaultTokenCallback));
            SecretBundle storageConnectionString = await keyVault.GetSecretAsync("https://devdataingestion.vault.azure.net/secrets/DevDataIngestionStorageConnectionString");

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString.Value);

            SharedAccessAccountPolicy policy = new SharedAccessAccountPolicy()
            {
                Permissions = SharedAccessAccountPermissions.Read | SharedAccessAccountPermissions.Write | SharedAccessAccountPermissions.Create | SharedAccessAccountPermissions.Add,
                Services = SharedAccessAccountServices.Blob | SharedAccessAccountServices.Table,
                ResourceTypes = SharedAccessAccountResourceTypes.Container | SharedAccessAccountResourceTypes.Object | SharedAccessAccountResourceTypes.Service,
                SharedAccessStartTime = DateTimeOffset.UtcNow,
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddDays(7)
            };

            string sasToken = storageAccount.GetSharedAccessSignature(policy);

            StorageCredentials storageCredentials = new StorageCredentials(sasToken);
            CloudStorageAccount storageAccountWithSas = new CloudStorageAccount(storageCredentials, storageAccount.BlobEndpoint, storageAccount.QueueEndpoint, storageAccount.TableEndpoint, storageAccount.FileEndpoint);

            CloudBlobClient blobClient = storageAccountWithSas.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("testcontainer");
            await container.CreateIfNotExistsAsync();

            CloudTableClient tableClient = storageAccountWithSas.CreateCloudTableClient();
            CloudTable tableContainer = tableClient.GetTableReference("testcontainer");
            await tableContainer.CreateIfNotExistsAsync();
        }

        [TestMethod]
        public async Task Navigate_Blob_Storage_And_Get_Files_From_Folders()
        {
            // TODO: Use KeyVault and MSI to get secret next time
            string storageConnectionString = "INSERT CONNECTION STRING HERE";

            // Connect to storage account
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            // Get blob container
            CloudBlobContainer blobContainer = storageAccount.CreateCloudBlobClient().GetContainerReference("vstsanalytics");

            // Get list of top folders from directory 
            CloudBlobDirectory rootDirectory = blobContainer.GetDirectoryReference(string.Empty);
            BlobResultSegment listingResult = await rootDirectory.ListBlobsSegmentedAsync(useFlatBlobListing: false, BlobListingDetails.None, 5000, null, null, null);
            IEnumerable<IListBlobItem> listingResults = listingResult.Results;

            // List all the content files
            List<IListBlobItem> blobItems = new List<IListBlobItem>();

            foreach (IListBlobItem listing in listingResults)
            {
                CloudBlobDirectory cloudBlobDirectory = (CloudBlobDirectory)listing;
                rootDirectory = blobContainer.GetDirectoryReference($"{cloudBlobDirectory.Prefix}OpStoreRestStream/v1/Hourly/2019/08/01");
                listingResult = await rootDirectory.ListBlobsSegmentedAsync(useFlatBlobListing: true, BlobListingDetails.None, 5000, null, null, null);
                blobItems.AddRange(listingResult.Results);
            }

            Console.WriteLine($"There are {blobItems.Count} blob Uri's");

            // Generate blob URI with SAS
            SharedAccessAccountPolicy policy = new SharedAccessAccountPolicy()
            {
                Permissions = SharedAccessAccountPermissions.Read | SharedAccessAccountPermissions.List,
                Services = SharedAccessAccountServices.Blob,
                ResourceTypes = SharedAccessAccountResourceTypes.Container | SharedAccessAccountResourceTypes.Object | SharedAccessAccountResourceTypes.Service,
                SharedAccessStartTime = DateTimeOffset.UtcNow,
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddDays(7)
            };

            string sasToken = storageAccount.GetSharedAccessSignature(policy);

            // Add message on the queue
            CloudQueue queue = storageAccount.CreateCloudQueueClient().GetQueueReference("vstsanalytics");
            foreach (IListBlobItem blobItem in blobItems)
            {
                string blobUri = $"{blobItem.Uri}{sasToken}";

                string notificiationMessage = $"AzureBlob: 1.0\n{{\"BlobRecords\": [{{\"Path\": \"{blobUri}\"}}]}}";

                CloudQueueMessage queueMessage = new CloudQueueMessage(notificiationMessage);

                await queue.AddMessageAsync(queueMessage);

                Console.WriteLine(blobUri);
            }
        }
    }
}