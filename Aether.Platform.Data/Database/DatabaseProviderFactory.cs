using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Data.Database
{
    public class DatabaseProviderFactory
    {
        public static IDatabaseProvider Create(DatabaseMode mode, string sqlConnStr, string accessConnStr)
        {
            switch (mode)
            {
                case DatabaseMode.SqlServerOnly:
                    return new SqlServerProvider(sqlConnStr);
                case DatabaseMode.AccessOnly:
                    return new AccessProvider(accessConnStr);
                case DatabaseMode.SqlServerWithAccessFallback:
                    return new FailoverProvider(sqlConnStr, accessConnStr);
                default:
                    return new AccessProvider(accessConnStr);
            }
        }
    }
}
