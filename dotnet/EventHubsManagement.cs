
namespace EventHubsForKafkaSample
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Management.ResourceManager;
    using Microsoft.Azure.Management.EventHub;
    using Microsoft.Azure.Management.EventHub.Models;
    using Microsoft.Extensions.Configuration;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Microsoft.Rest;

    public static class EventHubManagementSample
    {
        private static readonly IConfigurationRoot SettingsCache;
        private static string tokenValue = string.Empty;
        private static DateTime tokenExpiresAtUtc = DateTime.MinValue;

        static EventHubManagementSample()
        {
            SettingsCache = new ConfigurationBuilder()
                             .AddJsonFile("appsettings.json", true, true)
                             .Build();
        }

        public static void Run()
        {
            CreateEventHub().ConfigureAwait(false).GetAwaiter().GetResult();
            CreateConsumerGroup().ConfigureAwait(false).GetAwaiter().GetResult();

            Console.WriteLine("Press a key to exit.");
            Console.ReadLine();
        }

        private static async Task CreateEventHub()
        {
            try
            {
                var resourceGroupName = SettingsCache["resourceGroupName"];
                var namespaceName = SettingsCache["namespaceName"];
                var eventHubName = SettingsCache["EH_NAME"];
                var numOfPartitions = SettingsCache["NumOfPartitions"];

                if (string.IsNullOrEmpty(resourceGroupName))
                {
                    throw new Exception("Resource Group name is empty!");
                }

                if (string.IsNullOrEmpty(namespaceName))
                {
                    throw new Exception("Namespace name is empty!");
                }

                if (string.IsNullOrEmpty(eventHubName))
                {
                    throw new Exception("Event Hub name is empty!");
                }

                var token = await GetToken();

                var creds = new TokenCredentials(token);
                var ehClient = new EventHubManagementClient(creds)
                {
                    SubscriptionId = SettingsCache["SubscriptionId"]
                };

                var ehParams = new Eventhub() { PartitionCount = string.IsNullOrEmpty(numOfPartitions) ? 10 : Convert.ToInt64(numOfPartitions) };

                Console.WriteLine("Creating Event Hub...");
                await ehClient.EventHubs.CreateOrUpdateAsync(resourceGroupName, namespaceName, eventHubName, ehParams);
                Console.WriteLine("Created Event Hub successfully.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not create an Event Hub...");
                Console.WriteLine(e.Message);
                throw e;
            }
        }

        private static async Task CreateConsumerGroup()
        {
            try
            {
                var resourceGroupName = SettingsCache["resourceGroupName"];
                var namespaceName = SettingsCache["namespaceName"];
                var eventHubName = SettingsCache["EH_NAME"];
                var consumerGroupName = SettingsCache["CONSUMER_GROUP"];

                if (string.IsNullOrEmpty(namespaceName))
                {
                    throw new Exception("Namespace name is empty!");
                }

                var token = await GetToken();

                var creds = new TokenCredentials(token);
                var ehClient = new EventHubManagementClient(creds)
                {
                    SubscriptionId = SettingsCache["SubscriptionId"]
                };

                var consumerGroupParams = new ConsumerGroup() { };

                Console.WriteLine("Creating Consumer Group...");
                await ehClient.ConsumerGroups.CreateOrUpdateAsync(resourceGroupName, namespaceName, eventHubName, consumerGroupName, consumerGroupParams);
                Console.WriteLine("Created Consumer Group successfully.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not create a Consumer Group...");
                Console.WriteLine(e.Message);
                throw e;
            }
        }

        private static async Task<string> GetToken()
        {
            try
            {
                // Check to see if the token has expired before requesting one.
                // We will go ahead and request a new one if we are within 2 minutes of the token expiring.
                if (tokenExpiresAtUtc < DateTime.UtcNow.AddMinutes(-2))
                {
                    Console.WriteLine("Renewing token...");

                    var tenantId = SettingsCache["TenantId"];
                    var clientId = SettingsCache["ClientId"];
                    var clientSecret = SettingsCache["ClientSecret"];

                    var context = new AuthenticationContext($"https://login.windows.net/{tenantId}");

                    var result = await context.AcquireTokenAsync(
                        "https://management.core.windows.net/",
                        new ClientCredential(clientId, clientSecret)
                    );

                    // If the token isn't a valid string, throw an error.
                    if (string.IsNullOrEmpty(result.AccessToken))
                    {
                        throw new Exception("Token result is empty!");
                    }

                    tokenExpiresAtUtc = result.ExpiresOn.UtcDateTime;
                    tokenValue = result.AccessToken;
                    Console.WriteLine("Token renewed successfully.");
                }

                return tokenValue;
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not get a new token...");
                Console.WriteLine(e.Message);
                throw e;
            }
        }
    }
}
