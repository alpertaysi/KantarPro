using System;
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

        public static bool TestConnection()
        {
            try
            {
                using (var context = Create())
                {
                    context.Database.Connection.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                App.LogError("SQL baglanti testi", ex);
                return false;
            }
        }
    }
}
