using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Gnoss.Web.OAuth.Application.Settings
{
    public static class StartupValidator
    {
        private static readonly string[] SupportedProviders = ["0", "1", "2"];

        public static void Validate(IConfiguration configuration)
        {
            var errors = new List<string>();

            ValidateConnectionStrings(configuration, errors);
            ValidatePorts(configuration, errors);
            ValidateDatabaseProvider(configuration, errors);

            if (errors.Count > 0)
                throw new InvalidOperationException(
                    "La aplicación no puede arrancar — errores de configuración:\n" +
                    string.Join("\n", errors.Select(e => $"  • {e}")));
        }

        private static void ValidateConnectionStrings(IConfiguration cfg, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(cfg.GetValue<string>("oauth")))
                errors.Add("oauth es obligatorio.");

            if (string.IsNullOrWhiteSpace(cfg.GetValue<string>("redis:redis:ip:master")))
                errors.Add("redis:ip:master es obligatorio.");
        }

        private static void ValidatePorts(IConfiguration cfg, List<string> errors)
        {
            var api = cfg.GetValue("ApiPort", 8080);
            var management = cfg.GetValue("ManagementPort", 8081);

            if (api < 1 || api > 65535)
                errors.Add($"ApiPort debe estar entre 1 y 65535 (valor actual: {api}).");

            if (management < 1 || management > 65535)
                errors.Add($"ManagementPort debe estar entre 1 y 65535 (valor actual: {management}).");

            if (api == management && api is >= 1 and <= 65535)
                errors.Add($"ApiPort y ManagementPort deben ser distintos (ambos son {api}).");
        }

        private static void ValidateDatabaseProvider(IConfiguration cfg, List<string> errors)
        {
            var provider = cfg.GetValue<string>("connectionType") ?? "2";

            if (!SupportedProviders.Any(p => p.Equals(provider, StringComparison.OrdinalIgnoreCase)))
                errors.Add(
                    $"connectionType '{provider}' no está soportado. " +
                    $"Valores válidos: {string.Join(", ", SupportedProviders)}.");
        }
    }

}
