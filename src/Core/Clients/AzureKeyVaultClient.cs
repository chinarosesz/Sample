using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Services.AppAuthentication;
using System;
using System.Threading.Tasks;

namespace Core
{
    public static class AzureKeyVaultClient
    {
        private static string vsoPeronsalAccessToken = null;
        private static string aadSecret = null;
        private static string dataIngestionStorageAccountConnectionString = null;
        private static string devopsAadSecret = null;
        private static string lilaServiceApplicationSecret = null;

        public static async Task<string> GetVsoPersonalAccessTokenAsync()
        {
            if (vsoPeronsalAccessToken != null)
            {
                return vsoPeronsalAccessToken;
            }

            vsoPeronsalAccessToken = Environment.GetEnvironmentVariable("PersonalAccessToken");
            if (vsoPeronsalAccessToken == null)
            {
                SecretBundle secretBundle = await GetSecret("https://onfireingestion.vault.azure.net/secrets/DevopsPersonalAccessToken");
                vsoPeronsalAccessToken = secretBundle.Value;
            }
            return vsoPeronsalAccessToken;
        }

        public static async Task<string> GetLilaClientAppSecretAsync()
        {
            if (aadSecret == null)
            {
                SecretBundle result = await GetSecret("https://autobuggerservice.vault.azure.net/secrets/LilaClientAadSecret");
                aadSecret = result.Value;
            }

            return aadSecret;
        }

        public static async Task<string> GetLilaServiceApplicationSecretAsync()
        {
            if (lilaServiceApplicationSecret == null)
            {
                SecretBundle result = await GetSecret("https://litrakeyvault.vault.azure.net/secrets/LilaServiceApplicationSecret");
                aadSecret = result.Value;
            }

            return aadSecret;
        }

        public static async Task<string> GetAzureDevOpsAadSecretAsync()
        {
            if (devopsAadSecret == null)
            {
                SecretBundle result = await GetSecret("https://litrakeyvault.vault.azure.net/secrets/CloudMineAzureDevOpsServicePrincipalSecret");
                devopsAadSecret = result.Value;
            }

            return devopsAadSecret;
        }

        public static async Task<string> GetStorageConnectionStringAsync()
        {
            if (dataIngestionStorageAccountConnectionString == null)
            {
                SecretBundle result = await GetSecret("https://onfireingestion.vault.azure.net/secrets/StorageConnectionString");
                dataIngestionStorageAccountConnectionString = result.Value;
            }

            return dataIngestionStorageAccountConnectionString;
        }

        private static async Task<SecretBundle> GetSecret(string secretUrl)
        {
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();

            using (KeyVaultClient keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback)))
            {
                SecretBundle secretBundle = await keyVaultClient.GetSecretAsync(secretUrl);
                return secretBundle;
            }
        }
    }
}