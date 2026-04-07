using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020176.DataLayers.Interfaces;
using SV22T1020176.Models.Partner;
using SV22T1020176.Models.Common;
using System.Data;

namespace SV22T1020176.DataLayers.SqlServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu người giao hàng trên SQL Server
    /// </summary>
    public class ShipperRepository : BaseSqlDAL, IShipperRepository
    {
        public ShipperRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<int> AddAsync(Shipper data)
        {
            using (var connection = GetConnection())
            {
                var sql = @"INSERT INTO Shippers(ShipperName, Phone)
                            VALUES(@ShipperName, @Phone);
                            SELECT SCOPE_IDENTITY();";
                var id = await connection.ExecuteScalarAsync<int>(sql, data);
                return id;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = GetConnection())
            {
                var sql = @"DELETE FROM Shippers WHERE ShipperID = @ShipperID";
                var rowsAffected = await connection.ExecuteAsync(sql, new { ShipperID = id });
                return rowsAffected > 0;
            }
        }

        public async Task<Shipper?> GetAsync(int id)
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT * FROM Shippers WHERE ShipperID = @ShipperID";
                var data = await connection.QueryFirstOrDefaultAsync<Shipper>(sql, new { ShipperID = id });
                return data;
            }
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = GetConnection())
            {
                var sql = @"IF EXISTS(SELECT * FROM Orders WHERE ShipperID = @ShipperID)
                                SELECT 1
                            ELSE
                                SELECT 0";
                var result = await connection.ExecuteScalarAsync<int>(sql, new { ShipperID = id });
                return result == 1;
            }
        }

        public async Task<PagedResult<Shipper>> ListAsync(PaginationSearchInput input)
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT COUNT(*) FROM Shippers
                            WHERE (@SearchValue = N'') OR (ShipperName LIKE @SearchValue) OR (Phone LIKE @SearchValue);

                            SELECT * FROM Shippers
                            WHERE (@SearchValue = N'') OR (ShipperName LIKE @SearchValue) OR (Phone LIKE @SearchValue)
                            ORDER BY ShipperName
                            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                var parameters = new
                {
                    SearchValue = $"%{input.SearchValue}%",
                    input.Offset,
                    input.PageSize
                };

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    var rowCount = await multi.ReadFirstAsync<int>();
                    var data = (await multi.ReadAsync<Shipper>()).ToList();

                    return new PagedResult<Shipper>
                    {
                        Page = input.Page,
                        PageSize = input.PageSize,
                        RowCount = rowCount,
                        DataItems = data
                    };
                }
            }
        }

        public async Task<bool> UpdateAsync(Shipper data)
        {
            using (var connection = GetConnection())
            {
                var sql = @"UPDATE Shippers 
                            SET ShipperName = @ShipperName, Phone = @Phone 
                            WHERE ShipperID = @ShipperID";
                var rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }
    }
}
