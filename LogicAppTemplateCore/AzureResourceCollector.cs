using Microsoft.Identity.Client;

using Newtonsoft.Json.Linq;

using System.Net;
using System.Net.Http.Headers;

using static System.Formats.Asn1.AsnWriter;


namespace LogicAppTemplate
{
    public class AzureResourceCollector : IResourceCollector
    {
        private IPublicClientApplication _publicClientApplication;
        public string DebugOutputFolder = "";
        public string token;


        public AzureResourceCollector()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        public async Task<string> LoginAsync(string tenantName)
        {
            tenantName = string.IsNullOrEmpty(tenantName) ? "common" : tenantName;

            _publicClientApplication = PublicClientApplicationBuilder.Create(Constants.ClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, tenantName)
                .WithRedirectUri("http://localhost")
                //.WithRedirectUri(Constants.RedirectUrl) // Update as needed, or use a constant
                .Build();

            String[] _scopes = ["https://management.azure.com//.default"];
            AuthenticationResult result = null;
            try
            {
                // Attempt to get a token from the cache, or login silently if possible.
                result = await _publicClientApplication.AcquireTokenSilent(_scopes, _publicClientApplication.GetAccountsAsync().Result.FirstOrDefault())
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                // If a token cannot be acquired silently, trigger an interactive sign-in.
                try
                {
                    result = await _publicClientApplication.AcquireTokenInteractive(_scopes)
                        .ExecuteAsync();
                }
                catch (MsalException msalex)
                {
                    // Handle exception when acquiring token interactively fails.
                    Console.WriteLine($"MSAL Exception Encountered: {msalex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                // Handle or log other exceptions accordingly.
                Console.WriteLine($"General Exception Encountered: {ex.Message}");
                throw;
            }

            return result?.AccessToken;
        }



        public string Login(string tenantName)
        {
            token = LoginAsync(tenantName).GetAwaiter().GetResult();
            return token;
        }
        private static HttpClient client = new HttpClient() { BaseAddress = new Uri("https://management.azure.com") };

        public async Task<JObject> GetResource(string resourceId, string apiVersion = null, string suffix = "")
        {
            return JObject.Parse(await GetRawResource(resourceId, apiVersion, suffix));
        }
        public async Task<string> GetRawResource(string resourceId, string apiVersion = null, string suffix = "")
        {
            if (resourceId.ToLower().Contains("integrationserviceenvironment"))
            {
                apiVersion = "2018-07-01-preview";
            }

            string url = resourceId + (string.IsNullOrEmpty(apiVersion) ? "" : "?api-version=" + apiVersion) + (string.IsNullOrEmpty(suffix) ? "" : $"&{suffix}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new Exception("Resource Not found, resource: " + resourceId);
            }
            else if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new Exception("Authorization failed, httpstatus: " + response.StatusCode);
            }
            var responseContent = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(DebugOutputFolder))
            {
                System.IO.File.WriteAllText(DebugOutputFolder + "\\" + resourceId.Split('/').SkipWhile((a) => { return a != "providers" && a != "integrationAccounts"; }).Aggregate<string>((b, c) => { return b + "-" + c; }) + ".json", responseContent);
            }
            return responseContent;
        }
        
        public async Task<JArray> GetRoles(string scope, string filter, string apiVersion)
        {
            return JObject.Parse(await GetRawRoles(scope, apiVersion, filter)).Value<JArray>("value");
        }

        public async Task<string> GetRawRoles(string scope, string apiVersion, string filter = null)
        {
            var url = $"https://management.azure.com/{scope}/providers/Microsoft.Authorization/roleAssignments?api-version={(string.IsNullOrEmpty(apiVersion) ? "2022-04-01" : apiVersion)}{(string.IsNullOrEmpty(filter) ? "" : $"&$filter={filter}")}";
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new Exception("Roles Not found, resource: " + scope);
            }
            else if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new Exception("Authorization failed, httpstatus: " + response.StatusCode);
            }
            var responseContent = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(DebugOutputFolder))
            {
                System.IO.File.WriteAllText(DebugOutputFolder + "\\" + scope.Split('/').SkipWhile((a) => { return a != "providers" && a != "integrationAccounts"; }).Aggregate<string>((b, c) => { return b + "-" + c; }) + ".json", responseContent);
            }
            
            return responseContent;
        }
    }
}