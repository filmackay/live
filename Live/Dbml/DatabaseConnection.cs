using System;
using System.Data.SqlClient;

namespace Vertigo.Live
{
    public class DatabaseConnection : IDisposable
    {
        public SqlConnection SqlConnection { get; private set; }
        public SqlTransaction SqlTransaction { get; private set; }

        public DatabaseConnection(string connectionString)
        {
            SqlConnection = new SqlConnection(connectionString);
            SqlConnection.Open();
        }

        public void BeginTransaction()
        {
            SqlTransaction = SqlConnection.BeginTransaction();
        }

        public void Rollback()
        {
            SqlTransaction.Rollback();
            SqlTransaction = null;
        }

        public void Dispose()
        {
            if (SqlTransaction != null)
                SqlTransaction.Commit();
            SqlConnection.Close();
        }

        public SqlCommand CreateCommand()
        {
            var cmd = SqlConnection.CreateCommand();
            if (SqlTransaction != null)
                cmd.Transaction = SqlTransaction;
            return cmd;
        }
    }
}