using System.Collections.Generic;

namespace OidcClientsTest
{
    public class ConsoleConfig
    {
        public string Authority { get; set; } = "https://localhost:5001";

        public IList<ClientConfig> Clients { get; set; } = new List<ClientConfig>();

    }

    public record ClientConfig
    {
        public string Id { get; init; }
        public string Password { get; init; }
        public IList<string> AllowedScopes { get; init; } = new List<string>();
        public IList<string> RedirectUris { get; init; } = new List<string>();
        public IList<string> GrantTypes { get; init; } = new List<string>();
        public IList<string> ApiTests { get; init; } = new List<string>();
    }
}