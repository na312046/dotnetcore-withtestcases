namespace todo
{
    using System.Threading.Tasks;
    using todo.Models;
    using todo.Services;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Cosmos.Fluent;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Azure.KeyVault;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
         

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddSingleton<ICosmosDbService>(InitializeCosmosClientInstanceAsync(Configuration.GetSection("CosmosDb")).GetAwaiter().GetResult());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Item}/{action=Index}/{id?}");
            });
        }

        // <InitializeCosmosClientInstanceAsync>        
        /// <summary>
        /// Creates a Cosmos DB database and a container with the specified partition key. 
        /// </summary>
        /// <returns></returns>
        private static async Task<CosmosDbService> InitializeCosmosClientInstanceAsync(IConfigurationSection configurationSection)
        {
            //string databaseName = configurationSection.GetSection("DatabaseName").Value;
            //string containerName = configurationSection.GetSection("ContainerName").Value;
            //string account = configurationSection.GetSection("Account").Value;
            //string key = configurationSection.GetSection("Key").Value;


            string uriAsAccount =  await GetKeyVaultSecret("cosmosdbURI");
            string primaryKeys =await GetKeyVaultSecret("cosmosdbKeys");
            string databaseName =await GetKeyVaultSecret("cosmosDBDatabaseName");
            string containerName = await GetKeyVaultSecret("cosmosDBContainerName");
            CosmosClientBuilder clientBuilder = new CosmosClientBuilder(uriAsAccount, primaryKeys);
            CosmosClient client = clientBuilder
                                .WithConnectionModeGateway()
                                .Build();
            CosmosDbService cosmosDbService = new CosmosDbService(client, databaseName, containerName);

            DatabaseResponse database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");

            return cosmosDbService;
        }
        // </InitializeCosmosClientInstanceAsync>

            /// <summary>
            /// method to get cosmos db connection details as secrets from azure key vaults.
            /// </summary>
            /// <param name="secretfor"></param>
            /// <returns></returns>
        private static async Task<string> GetKeyVaultSecret(string secretfor)
        {

            // This is the ID which can be found as "Application (client) ID" when selecting the registered app under "Azure Active Directory" -> "App registrations".
            const string APP_CLIENT_ID = "dc44b372-369b-4894-9673-da3ec8d7038c";

            // This is the client secret from the app registration process.
            const string APP_CLIENT_SECRET = "6pxtYxl4_UIh08ueFRSIhorwCTXgL:[=";

            // This is available as "DNS Name" from the overview page of the Key Vault.
            const string KEYVAULT_BASE_URI = "https://cosmosdbkeys.vault.azure.net/";



            // Use the client SDK to get access to the key vault. To authenticate we use the identity app we registered and
            // use the client ID and the client secret to make our claim.
            var kvc = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(
                async (string authority, string resource, string scope) => {
                    var authContext = new AuthenticationContext(authority);
                    var credential = new ClientCredential(APP_CLIENT_ID, APP_CLIENT_SECRET);
                    AuthenticationResult result = await authContext.AcquireTokenAsync(resource, credential);
                    if (result == null)
                    {
                        throw new System.InvalidOperationException("Failed to retrieve JWT token");
                    }
                    return result.AccessToken;
                }
            ));
            // Calling GetSecretAsync will trigger the authentication code above and eventually
            // retrieve the secret which we can then read.
            var secretBundle = await kvc.GetSecretAsync(KEYVAULT_BASE_URI, secretfor);

            return secretBundle.Value;
        }
    }
}
