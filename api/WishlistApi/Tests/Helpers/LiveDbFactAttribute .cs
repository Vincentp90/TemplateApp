using System;
using System.Collections.Generic;
using System.Text;

namespace Tests.Helpers
{
    //TODO this can be removed?
    public static class TestCategories
    {
        public const string LiveDb = "LiveDb";
    }

    public class LiveDbFactAttribute : FactAttribute
    {
        public LiveDbFactAttribute()
        {
            if (Environment.GetEnvironmentVariable("RUN_LIVE_DB_TESTS") != "1")
            {
                Skip = "Live DB tests disabled";
            }
        }
    }
}
