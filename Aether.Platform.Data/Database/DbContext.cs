using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Data.Database
{
    public class DbContext
    {
        private readonly IDatabaseProvider _provider;

        public IDatabaseProvider Provider => _provider;
        public DatabaseMode Mode => _provider.Mode;
        public bool IsAvailable => _provider.IsAvailable;

        public DbContext(IDatabaseProvider provider) { _provider = provider; }
    }
}
