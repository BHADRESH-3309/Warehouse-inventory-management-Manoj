using System.Data;

namespace webWarehouseInventoryManagement.DataAccess.DbAccess
{
    public interface ISqlDataAccess
    {
        public Task<IEnumerable<T>> GetData<T, U>(string sqlQuery, U parameters);
        public Task<IEnumerable<T>> GetDataSP<T, U>(string storeProcedure, U parameters);
        
        public Task ExecuteDML<T>(string sqlQuery, T parameters);
        public Task ExecuteDMLSP<T>(string storeProcedure, T parameters);

        public void BulkinsertSKU(DataTable dt_temp, string TableName);
        public void BulkinsertAdditionalSKU(DataTable dt_temp, string TableName);
        public void BulkinsertMappingSKU(DataTable dt_temp, string TableName);
        public void BulkinsertAdditionalMappingSKU(DataTable dt_temp, string TableName);
        public string GetSingleValue(string sqlQuery);
        public string GetSingleValueForAMZ(string sqlQuery);
        public void BulkUpdateWarehouseQuantity(DataTable dt_temp, string TableName);
        public void BulkInsertLogs(DataTable dt_temp, string TableName);
        void BulkInsertUserActivityLog(DataTable dt_temp, string TableName);
        public void BulkInsertTblSupplier(DataTable dt_temp, string TableName);

        public Task<IEnumerable<T>> AmazonCustomOrderGetData<T, U>(string sqlQuery, U parameters);
        public Task AmazonCustomOrderExecuteDML<T>(string sqlQuery, T parameters);

    }
}