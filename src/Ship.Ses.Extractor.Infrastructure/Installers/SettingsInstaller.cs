using Ship.Ses.Extractor.Infrastructure.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Ship.Ses.Extractor.Infrastructure.Installers
{
    public static class SettingsInstaller
    {
        public static void InstallApplicationSettings(this WebApplicationBuilder builder)
        {
            builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(nameof(AppSettings)));
        }
    }

}
