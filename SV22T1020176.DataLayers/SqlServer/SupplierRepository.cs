using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020176.DataLayers.Interfaces;
using SV22T1020176.Models.Partner;
using SV22T1020176.Models.Common;
using System.Data;

namespace SV22T1020176.DataLayers.SqlServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu nhà cung cấp trên SQL Server
    /// </summary>
    public class SupplierRepository : BaseSqlDAL, ISupplierRepository
    {
        public SupplierRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<int> AddAsync(Supplier data)
        {
            using (var connection = GetConnection())
            {
                var sql = @"INSERT INTO Suppliers(SupplierName, ContactName, Province, Address, Phone, Email)
                            VALUES(@SupplierName, @ContactName, @Province, @Address, @Phone, @Email);
                            SELECT SCOPE_IDENTITY();";
                var id = await connection.ExecuteScalarAsync<int>(sql, data);
                return id;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = GetConnection())
            {
                var sql = @"DELETE FROM Suppliers WHERE SupplierID = @SupplierID";
                var rowsAffected = await connection.ExecuteAsync(sql, new { SupplierID = id });
                return rowsAffected > 0;
            }
        }

        public async Task<Supplier?> GetAsync(int id)
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT * FROM Suppliers WHERE SupplierID = @SupplierID";
                var data = await connection.QueryFirstOrDefaultAsync<Supplier>(sql, new { SupplierID = id });
                return data;
            }
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = GetConnection())
            {
                var sql = @"IF EXISTS(SELECT * FROM Products WHERE SupplierID = @SupplierID)
                                SELECT 1
                            ELSE
                                SELECT 0";
                var result = await connection.ExecuteScalarAsync<int>(sql, new { SupplierID = id });
                return result == 1;
            }
        }

        public async Task<PagedResult<Supplier>> ListAsync(PaginationSearchInput input)
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT COUNT(*) FROM Suppliers
                            WHERE (@SearchValue = N'') 
                               OR (SupplierName LIKE @SearchValue) 
                               OR (ContactName LIKE @SearchValue)
                               OR (Phone LIKE @SearchValue)
                               OR (Email LIKE @SearchValue)
                               OR (Address LIKE @SearchValue);

                            SELECT * FROM Suppliers
                            WHERE (@SearchValue = N'') 
                               OR (SupplierName LIKE @SearchValue) 
                               OR (ContactName LIKE @SearchValue)
                               OR (Phone LIKE @SearchValue)
                               OR (Email LIKE @SearchValue)
                               OR (Address LIKE @SearchValue)
                            ORDER BY SupplierName
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
                    var data = (await multi.ReadAsync<Supplier>()).ToList();

                    return new PagedResult<Supplier>
                    {
                        Page = input.Page,
                        PageSize = input.PageSize,
                        RowCount = rowCount,
                        DataItems = data
                    };
                }
            }
        }

        public async Task<bool> UpdateAsync(Supplier data)
        {
            using (var connection = GetConnection())
            {
                var sql = @"UPDATE Suppliers 
                            SET SupplierName = @SupplierName, 
                                ContactName = @ContactName, 
                                Province = @Province, 
                                Address = @Address, 
                                Phone = @Phone, 
                                Email = @Email
                            WHERE SupplierID = @SupplierID";
                var rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }
    }
}
