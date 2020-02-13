namespace Core.Helpers
{
    public class Constants
    {
        /// <summary>
        /// Microsoft tenant Id is unique and is not expected to change
        /// </summary>
        public static readonly string MicrosoftTenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
        
        /// <summary>
        /// Service AAD client ID
        /// </summary>
        public static readonly string LilaServiceApplicationId = "2f921921-ec2c-4ed4-b872-4d9df98aae6f";

        /// <summary>
        /// Client AAD client Id
        /// </summary>
        public static readonly string LilaClientAppId = "54d41a04-0dbb-4a89-98d5-984833cd19a9";

        /// <summary>
        /// Address of the authority for AAD logging in to Microsoft
        /// </summary>
        public static readonly string MicrosoftAuthAuthority = "https://login.windows.net/microsoft.onmicrosoft.com";
    }
}
