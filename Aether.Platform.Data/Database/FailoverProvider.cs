using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Data.Database
{
    public class FailoverProvider : IDatabaseProvider
    {
        private readonly string _sqlConnectionString;
        private readonly string _accessConnectionString;
        private IDatabaseProvider _activeProvider;
        private bool _sqlAvailable;

        public DatabaseMode Mode => DatabaseMode.SqlServerWithAccessFallback;
        public bool IsAvailable
        {
            get
            {
                if (_sqlAvailable) return true;
                var ap = new AccessProvider(_accessConnectionString);
                return ap.IsAvailable;
            }
        }

        public FailoverProvider(string sqlConnectionString, string accessConnectionString)
        {
            _sqlConnectionString = sqlConnectionString;
            _accessConnectionString = accessConnectionString;
            _sqlAvailable = TestSqlServer();
            _activeProvider = _sqlAvailable
                ? (IDatabaseProvider)new SqlServerProvider(_sqlConnectionString)
                : new AccessProvider(_accessConnectionString);
        }

        private bool TestSqlServer()
        {
            try
            {
                using (var conn = new SqlConnection(_sqlConnectionString))
                {
                    conn.Open();
                    return true;
                }
            }
            catch { return false; }
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null)
        {
            if (_sqlAvailable)
            {
                try { return await ((SqlServerProvider)_activeProvider).QueryAsync<T>(sql, param); }
                catch { _sqlAvailable = false; _activeProvider = new AccessProvider(_accessConnectionString); }
            }
            return await ((AccessProvider)_activeProvider).QueryAsync<T>(sql, param);
        }

        public async Task<int> ExecuteAsync(string sql, object param = null)
        {
            if (_sqlAvailable)
            {
                try { return await ((SqlServerProvider)_activeProvider).ExecuteAsync(sql, param); }
                catch { _sqlAvailable = false; _activeProvider = new AccessProvider(_accessConnectionString); }
            }
            return await ((AccessProvider)_activeProvider).ExecuteAsync(sql, param);
        }

        public async Task<T> QuerySingleAsync<T>(string sql, object param = null)
        {
            if (_sqlAvailable)
            {
                try { return await ((SqlServerProvider)_activeProvider).QuerySingleAsync<T>(sql, param); }
                catch { _sqlAvailable = false; _activeProvider = new AccessProvider(_accessConnectionString); }
            }
            return await ((AccessProvider)_activeProvider).QuerySingleAsync<T>(sql, param);
        }

        public void Dispose() { _activeProvider?.Dispose(); }
    }
}
