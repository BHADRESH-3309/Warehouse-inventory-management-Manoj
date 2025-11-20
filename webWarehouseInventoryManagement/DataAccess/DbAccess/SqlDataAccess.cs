using System.Data.SqlClient;
using System.Data;
using Dapper;

namespace webWarehouseInventoryManagement.DataAccess.DbAccess
{
    public class SqlDataAccess : ISqlDataAccess
    {
        private readonly string _connectionString;
        private readonly string _amazoncustomorderConnectionString;
        private readonly int _timeout;

        public SqlDataAccess(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("CN-webWarehouseInventoryManagement");
            _amazoncustomorderConnectionString = config.GetConnectionString("CN-AmazonOrderSystem");
            _timeout = 60 * 3; //Seconds 60 * 3 = 180 = 3 minutes
        }

        //Load data into model using sql query
        public async Task<IEnumerable<T>> GetData<T, U>(string sqlQuery, U parameters)
        {
            using (IDbConnection connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<T>(sqlQuery, parameters, commandTimeout: _timeout);
            }
        }

        public async Task<IEnumerable<T>> GetDataSP<T, U>(string storeProcedure, U parameters)
        {
            using (IDbConnection connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<T>(storeProcedure, parameters, commandType: CommandType.StoredProcedure, commandTimeout: _timeout);
            }
        }

        public async Task ExecuteDML<T>(string sqlQuery, T parameters)
        {
            using (IDbConnection connection = new SqlConnection(_connectionString))
            {
                await connection.ExecuteAsync(sqlQuery, parameters, commandTimeout: _timeout);
            }
        }
        public async Task ExecuteDMLSP<T>(string storeProcedure, T parameters)
        {
            using (IDbConnection connection = new SqlConnection(_connectionString))
            {
                await connection.ExecuteAsync(storeProcedure, parameters, commandType: CommandType.StoredProcedure, commandTimeout: _timeout);
            }
        }


        public void BulkinsertSKU(DataTable dt_temp, string TableName)
        {
            SqlConnection connection = new SqlConnection(_connectionString);
            SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(connection);

            // Set the timeout.
            sqlBulkCopy.BulkCopyTimeout = 700;

            //Set the database table name
            sqlBulkCopy.DestinationTableName = TableName;
            sqlBulkCopy.ColumnMappings.Add("Warehouse_SKU", "WarehouseSKU");
            //sqlBulkCopy.ColumnMappings.Add("DateAdd", "DateAdd");

            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }
            sqlBulkCopy.WriteToServer(dt_temp);
            connection.Close();

        }
        public void BulkinsertAdditionalSKU(DataTable dt_temp, string TableName)
        {
            SqlConnection connection = new SqlConnection(_connectionString);
            SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(connection);

            // Set the timeout.
            sqlBulkCopy.BulkCopyTimeout = 700;

            //Set the database table name
            sqlBulkCopy.DestinationTableName = TableName;
            sqlBulkCopy.ColumnMappings.Add("SKU", "SKU");
            sqlBulkCopy.ColumnMappings.Add("Tag", "Tag");

            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }
            sqlBulkCopy.WriteToServer(dt_temp);
            connection.Close();

        }

        public void BulkinsertMappingSKU(DataTable dt_temp, string TableName)
        {
            SqlConnection connection = new SqlConnection(_connectionString);
            SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(connection);

            // Set the timeout.
            sqlBulkCopy.BulkCopyTimeout = 700;

            //Set the database table name
            sqlBulkCopy.DestinationTableName = TableName;
            sqlBulkCopy.ColumnMappings.Add("idSKU", "idSKU");
            sqlBulkCopy.ColumnMappings.Add("SKU", "SKU");
            sqlBulkCopy.ColumnMappings.Add("Type", "Type");
            sqlBulkCopy.ColumnMappings.Add("DateAdd", "DateAdd");

            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }
            sqlBulkCopy.WriteToServer(dt_temp);
            connection.Close();

        }
        public void BulkinsertAdditionalMappingSKU(DataTable dt_temp, string TableName)
        {
            SqlConnection connection = new SqlConnection(_connectionString);
            SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(connection);

            // Set the timeout.
            sqlBulkCopy.BulkCopyTimeout = 700;

            //Set the database table name
            sqlBulkCopy.DestinationTableName = TableName;
            sqlBulkCopy.ColumnMappings.Add("idAdditionalAmazonSKU", "idAdditionalAmazonSKU");
            sqlBulkCopy.ColumnMappings.Add("SKU", "SKU");

            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }
            sqlBulkCopy.WriteToServer(dt_temp);
            connection.Close();

        }

        public string GetSingleValue(string sqlQuery)
        {
            string retval = string.Empty;
           
            SqlConnection connection = new SqlConnection(_connectionString);
            SqlCommand cmd = new SqlCommand();

            cmd.Connection = connection;

            if (connection.State == ConnectionState.Closed)
                connection.Open();

            cmd.CommandText = sqlQuery;
            if (cmd.ExecuteScalar() == null)
                return retval;

            retval = cmd.ExecuteScalar().ToString().Trim();

            cmd.Dispose();
            connection.Close();

            return retval;
        }
        public string GetSingleValueForAMZ(string sqlQuery)
        {
            string retval = string.Empty;
           
            SqlConnection connection = new SqlConnection(_amazoncustomorderConnectionString);
            SqlCommand cmd = new SqlCommand();

            cmd.Connection = connection;

            if (connection.State == ConnectionState.Closed)
                connection.Open();

            cmd.CommandText = sqlQuery;
            if (cmd.ExecuteScalar() == null)
                return retval;

            retval = cmd.ExecuteScalar().ToString().Trim();

            cmd.Dispose();
            connection.Close();

            return retval;
        }

        //Bulkupdate Warehouse Quantity
        public void BulkUpdateWarehouseQuantity(DataTable dt_temp, string TableName)
        {
            string tempTable = "TmpTableBulkUpdate";
            string query = string.Empty, updatequery = string.Empty, dropquery = string.Empty;

            ExecuteDML($"IF OBJECT_ID('{tempTable}', 'U') IS NOT NULL DROP TABLE {tempTable}", new { }).GetAwaiter().GetResult();

            query = @$"CREATE TABLE {tempTable}([WarehouseSKU] [nvarchar](50) NULL, [WarehouseQty] [int] NULL)";

            ExecuteDML(query, new { }).GetAwaiter().GetResult();


            SqlConnection connection = new SqlConnection(_connectionString);
            SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(connection);

            // Set the timeout.
            sqlBulkCopy.BulkCopyTimeout = 700;

            //Set the database table name
            sqlBulkCopy.DestinationTableName = tempTable;
            sqlBulkCopy.ColumnMappings.Add("WarehouseSKU", "WarehouseSKU");
            sqlBulkCopy.ColumnMappings.Add("WarehouseQty", "WarehouseQty");

            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }
            sqlBulkCopy.WriteToServer(dt_temp);
            connection.Close();

            updatequery = @$"UPDATE M SET M.WarehouseQty = T.WarehouseQty,
                  M.UpdatedDate = getdate() FROM tblProduct AS M 
                  INNER JOIN {tempTable} AS T ON M.WarehouseSKU = T.WarehouseSKU";

            ExecuteDML(updatequery, new { }).GetAwaiter().GetResult();

            ExecuteDML($"DROP TABLE {tempTable}", new { }).GetAwaiter().GetResult();
        }

        //Bulkinsert Logs
        public void BulkInsertLogs(DataTable dt_temp, string TableName)
        {
            SqlConnection connection = new SqlConnection(_connectionString);
            SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(connection);

            // Set the timeout.
            sqlBulkCopy.BulkCopyTimeout = 700;

            //Set the database table name
            sqlBulkCopy.DestinationTableName = TableName;
            sqlBulkCopy.ColumnMappings.Add("idProduct", "idProduct");
            sqlBulkCopy.ColumnMappings.Add("LogDetails", "LogDetails");
            sqlBulkCopy.ColumnMappings.Add("DateAdd", "DateAdd");

            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }
            sqlBulkCopy.WriteToServer(dt_temp);
            connection.Close();
        }
        public void BulkInsertUserActivityLog(DataTable dt_temp, string TableName)
        {
            SqlConnection connection = new SqlConnection(_connectionString);
            SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(connection);

            // Set the timeout.
            sqlBulkCopy.BulkCopyTimeout = 700;

            //Set the database table name
            sqlBulkCopy.DestinationTableName = TableName;
            sqlBulkCopy.ColumnMappings.Add("UserName", "UserName");
            sqlBulkCopy.ColumnMappings.Add("Section", "Section");
            sqlBulkCopy.ColumnMappings.Add("LogText", "LogText");

            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }
            sqlBulkCopy.WriteToServer(dt_temp);
            connection.Close();
        }
        public void BulkInsertTblSupplier(DataTable dt_temp, string TableName)
        {
            SqlConnection connection = new SqlConnection(_connectionString);
            SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(connection);

            // Set the timeout.
            sqlBulkCopy.BulkCopyTimeout = 900;

            //Set the database table name
            sqlBulkCopy.DestinationTableName = TableName;
            sqlBulkCopy.ColumnMappings.Add("idProduct", "idProduct");
            sqlBulkCopy.ColumnMappings.Add("Supplier", "Supplier");
            sqlBulkCopy.ColumnMappings.Add("ProductCode", "ProductCode");
            sqlBulkCopy.ColumnMappings.Add("Colour", "Colour");
            sqlBulkCopy.ColumnMappings.Add("Size", "Size");
            sqlBulkCopy.ColumnMappings.Add("DateAdd", "DateAdd");

            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }
            sqlBulkCopy.WriteToServer(dt_temp);
            connection.Close();
        }

        #region Amazon orders System Database Functions 

        public async Task AmazonCustomOrderExecuteDML<T>(string sqlQuery, T parameters)
        {
            using (IDbConnection connection = new SqlConnection(_amazoncustomorderConnectionString))
            {
                await connection.ExecuteAsync(sqlQuery, parameters, commandTimeout: _timeout);
            }
        }

        public async Task<IEnumerable<T>> AmazonCustomOrderGetData<T, U>(string sqlQuery, U parameters)
        {
            using (IDbConnection connection = new SqlConnection(_amazoncustomorderConnectionString))
            {
                return await connection.QueryAsync<T>(sqlQuery, parameters, commandTimeout: _timeout);
            }
        }
        #endregion
    }
}
