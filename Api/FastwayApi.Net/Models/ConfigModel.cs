using Microsoft.Extensions.Configuration;

namespace myFastway.ApiClient.Models
{
    public class ConfigModel
    {
        public OAuthConfig OAuth { get; }
        public ApiConfig Api { get; }

        public ConfigModel(IConfigurationRoot config)
        {
            OAuth = new OAuthConfig(config);
            Api = new ApiConfig(config);
        }

        public ConfigModel(string authority, string clientId, string secret, string scope, bool requireHttps, string baseAddress, string apiVersion)
        {
            OAuth = new OAuthConfig(authority, clientId, secret, scope, requireHttps);
            Api = new ApiConfig(baseAddress, apiVersion);
        }
    }

    public class OAuthConfig
    {
        public string Authority { get; set; }
        public string ClientId { get; set; }
        public string Secret { get; set; }
        public string Scope { get; set; }
        public bool RequireHttps { get; set; }

        public OAuthConfig(IConfigurationRoot config)
        {
            Authority = config["oauth:authority"];
            ClientId = config["oauth:client_id"];
            Secret = config["oauth:secret"];
            Scope = config["oauth:scope"];
            RequireHttps = bool.Parse(config["oauth:requireHttps"]);
        }

        public OAuthConfig(string authority, string clientId, string secret, string scope, bool requireHttps)
        {
            Authority = authority;
            ClientId = clientId;
            Secret = secret;
            Scope = scope;
            RequireHttps = requireHttps;
        }
    }

    public class ApiConfig
    {
        public string BaseAddress { get; set; }
        public string ApiVersion { get; set; }

        public ApiConfig(IConfigurationRoot config)
        {
            BaseAddress = config["api:base-address"];
            ApiVersion = config["api:version"];
        }

        public ApiConfig(string baseAddress, string apiVersion)
        {
            BaseAddress = baseAddress;
            ApiVersion = apiVersion;
        }
    }
}



