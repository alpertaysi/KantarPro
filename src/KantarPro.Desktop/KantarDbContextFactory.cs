using KantarPro.Infrastructure.Data;

namespace KantarPro.Desktop
{
    public static class KantarDbContextFactory
    {
        public static KantarDbContext Create()
        {
            var settings = StationSettingsStore.Load();
            return new KantarDbContext(settings.BuildConnectionString());
        }
    }
}



