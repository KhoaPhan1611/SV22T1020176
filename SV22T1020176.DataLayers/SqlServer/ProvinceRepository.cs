using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020176.DataLayers.Interfaces;
using SV22T1020176.Models.DataDictionary;
using System.Data;

namespace SV22T1020176.DataLayers.SqlServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu tỉnh thành phố trên SQL Server
    /// </summary>
    public class ProvinceRepository : BaseSqlDAL, IDataDictionaryRepository<Province>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString"></param>
        public ProvinceRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Lấy danh sách toàn bộ các tỉnh thành phố
        /// </summary>
        /// <returns></returns>
        public async Task<List<Province>> ListAsync()
        {
            using (var connection = GetConnection())
            {
                var sql = "SELECT * FROM Provinces ORDER BY ProvinceName";
                var data = (await connection.QueryAsync<Province>(sql)).ToList();
                return data;
            }
        }
    }
}
