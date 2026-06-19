using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Models;
using Dapper;

namespace Aether.Platform.Data.Database
{
    public class AccessProvider : IDatabaseProvider
    {
        private readonly string _connectionString;
        private OleDbConnection _connection;

        public DatabaseMode Mode => DatabaseMode.AccessOnly;
        public bool IsAvailable
        {
            get
            {
                try
                {
                    using (var conn = new OleDbConnection(_connectionString)) { conn.Open(); return true; }
                }
                catch { return false; }
            }
        }

        public AccessProvider(string connectionString) { _connectionString = connectionString; }

        private IDbConnection GetConnection()
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
            {
                _connection = new OleDbConnection(_connectionString);
                _connection.Open();
            }
            return _connection;
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null)
        {
            using (var conn = new OleDbConnection(_connectionString))
            {
                conn.Open();
                return await conn.QueryAsync<T>(sql, param);
            }
        }

        public async Task<int> ExecuteAsync(string sql, object param = null)
        {
            using (var conn = new OleDbConnection(_connectionString))
            {
                conn.Open();
                return await conn.ExecuteAsync(sql, param);
            }
        }

        public async Task<T> QuerySingleAsync<T>(string sql, object param = null)
        {
            using (var conn = new OleDbConnection(_connectionString))
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
