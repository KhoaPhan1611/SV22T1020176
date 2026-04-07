using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020176.DataLayers.Interfaces;
using SV22T1020176.Models.Catalog;
using SV22T1020176.Models.Common;
using System.Data;

namespace SV22T1020176.DataLayers.SqlServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu loại hàng trên SQL Server
    /// </summary>
    public class CategoryRepository : BaseSqlDAL, ICategoryRepository
    {
        public CategoryRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<int> AddAsync(Category data)
        {
            using (var connection = GetConnection())
            {
                var sql = @"INSERT INTO Categories(CategoryName, Description)
                            VALUES(@CategoryName, @Description);
                            SELECT SCOPE_IDENTITY();";
                var id = await connection.ExecuteScalarAsync<int>(sql, data);
                return id;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = GetConnection())
            {
                var sql = @"DELETE FROM Categories WHERE CategoryID = @CategoryID";
                var rowsAffected = await connection.ExecuteAsync(sql, new { CategoryID = id });
                return rowsAffected > 0;
            }
        }

        public async Task<Category?> GetAsync(int id)
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT * FROM Categories WHERE CategoryID = @CategoryID";
                var data = await connection.QueryFirstOrDefaultAsync<Category>(sql, new { CategoryID = id });
                return data;
            }
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = GetConnection())
            {
                var sql = @"IF EXISTS(SELECT * FROM Products WHERE CategoryID = @CategoryID)
                                SELECT 1
                            ELSE
                                SELECT 0";
                var result = await connection.ExecuteScalarAsync<int>(sql, new { CategoryID = id });
                return result == 1;
            }
        }

        public async Task<PagedResult<Category>> ListAsync(PaginationSearchInput input)
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT COUNT(*) FROM Categories
                            WHERE (@SearchValue = N'') OR (CategoryName LIKE @SearchValue);

                            SELECT * FROM Categories
                            WHERE (@SearchValue = N'') OR (CategoryName LIKE @SearchValue)
                            ORDER BY CategoryName
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
                    var data = (await multi.ReadAsync<Category>()).ToList();

                    return new PagedResult<Category>
                    {
                        Page = input.Page,
                        PageSize = input.PageSize,
                        RowCount = rowCount,
                        DataItems = data
                    };
                }
            }
        }

        public async Task<bool> UpdateAsync(Category data)
        {
            using (var connection = GetConnection())
            {
                var sql = @"UPDATE Categories 
                            SET CategoryName = @CategoryName, Description = @Description 
                            WHERE CategoryID = @CategoryID";
                var rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }
    }
}
