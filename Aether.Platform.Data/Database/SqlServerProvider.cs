using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Models;
using Dapper;

namespace Aether.Platform.Data.Database
{
    public class SqlServerProvider : IDatabaseProvider
    {
        private readonly string _connectionString;
        private SqlConnection _connection;

        public DatabaseMode Mode => DatabaseMode.SqlServerOnly;
        public bool IsAvailable
        {
            get
            {
                try
                {
                    using (var conn = new SqlConnection(_connectionString)) { conn.Open(); return true; }
                }
                catch { return false; }
            }
        }

        public SqlServerProvider(string connectionString) { _connectionString = connectionString; }

        private IDbConnection GetConnection()
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
            {
                _connection = new SqlConnection(_connectionString);
                _connection.Open();
            }
            return _connection;
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                return await conn.QueryAsync<T>(sql, param);
            }
        }

        public async Task<int> ExecuteAsync(string sql, object param = null)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                return await conn.ExecuteAsync(sql, param);
            }
        }

        public async Task<T> QuerySingleAsync<T>(string sql, object param = null)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                return await conn.QuerySingleOrDefaultAsync<T>(sql, param);
            }
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
        }
    }
}
