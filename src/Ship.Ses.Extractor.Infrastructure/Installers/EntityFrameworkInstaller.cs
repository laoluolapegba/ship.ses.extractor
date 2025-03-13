using Ship.Ses.Extractor.Infrastructure.Persistance.MsSql;
using Ship.Ses.Extractor.Infrastructure.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ship.Ses.Extractor.Infrastructure.Installers
{
    public static class EntityFrameworkInstaller
    {
        public static void InstallEntityFramework(this WebApplicationBuilder builder)
        {
            var appSettings = builder.Configuration.GetSection(nameof(AppSettings)).Get<AppSettings>();
            if (appSettings != null)
            {
                var msSqlSettings = appSettings.MsSql;
                builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(msSqlSettings.ConnectionString));
                builder.Services.AddScoped<IAppDbContext>(provider => provider.GetService<AppDbContext>());
            }
        }

        public static void SeedDatabase(AppDbContext appDbContext)
        {
            appDbContext.Database.Migrate();
        }
    }
}
