using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020176.DataLayers.Interfaces;
using SV22T1020176.Models.Catalog;
using SV22T1020176.Models.Common;
using System.Data;

namespace SV22T1020176.DataLayers.SqlServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu mặt hàng trên SQL Server
    /// </summary>
    public class ProductRepository : BaseSqlDAL, IProductRepository
    {
        public ProductRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<int> AddAsync(Product data)
        {
            using (var connection = GetConnection())
            {
                var sql = @"INSERT INTO Products(ProductName, ProductDescription, SupplierID, CategoryID, Unit, Price, Photo, IsSelling)
                            VALUES(@ProductName, @ProductDescription, @SupplierID, @CategoryID, @Unit, @Price, @Photo, @IsSelling);
                            SELECT SCOPE_IDENTITY();";
                var parameters = new
                {
                    data.ProductName,
                    data.ProductDescription,
                    data.SupplierID,
                    data.CategoryID,
                    data.Unit,
                    data.Price,
                    data.Photo,
                    data.IsSelling
                };
                var id = await connection.ExecuteScalarAsync<int>(sql, parameters);
                return id;
            }
        }

        public async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            using (var connection = GetConnection())
            {
                var sql = @"INSERT INTO ProductAttributes(ProductID, AttributeName, AttributeValue, DisplayOrder)
                            VALUES(@ProductID, @AttributeName, @AttributeValue, @DisplayOrder);
                            SELECT SCOPE_IDENTITY();";
                var id = await connection.ExecuteScalarAsync<long>(sql, data);
                return id;
            }
        }

        public async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            using (var connection = GetConnection())
            {
                var sql = @"INSERT INTO ProductPhotos(ProductID, Photo, Description, DisplayOrder, IsHidden)
                            VALUES(@ProductID, @Photo, @Description, @DisplayOrder, @IsHidden);
                            SELECT SCOPE_IDENTITY();";
                var id = await connection.ExecuteScalarAsync<long>(sql, data);
                return id;
            }
        }

        public async Task<bool> DeleteAsync(int productID)
        {
            using (var connection = GetConnection())
            {
                var sql = @"DELETE FROM ProductPhotos WHERE ProductID = @ProductID;
                            DELETE FROM ProductAttributes WHERE ProductID = @ProductID;
                            DELETE FROM Products WHERE ProductID = @ProductID";
                var rowsAffected = await connection.ExecuteAsync(sql, new { ProductID = productID });
                return rowsAffected > 0;
            }
        }

        public async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            using (var connection = GetConnection())
            {
                var sql = @"DELETE FROM ProductAttributes WHERE AttributeID = @AttributeID";
                var rowsAffected = await connection.ExecuteAsync(sql, new { AttributeID = attributeID });
                return rowsAffected > 0;
            }
        }

        public async Task<bool> DeletePhotoAsync(long photoID)
        {
            using (var connection = GetConnection())
            {
                var sql = @"DELETE FROM ProductPhotos WHERE PhotoID = @PhotoID";
                var rowsAffected = await connection.ExecuteAsync(sql, new { PhotoID = photoID });
                return rowsAffected > 0;
            }
        }

        public async Task<Product?> GetAsync(int productID)
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT * FROM Products WHERE ProductID = @ProductID";
                var data = await connection.QueryFirstOrDefaultAsync<Product>(sql, new { ProductID = productID });
                return data;
            }
        }

        public async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT * FROM ProductAttributes WHERE AttributeID = @AttributeID";
                var data = await connection.QueryFirstOrDefaultAsync<ProductAttribute>(sql, new { AttributeID = attributeID });
                return data;
            }
        }

        public async Task<ProductPhoto?> GetPhotoAsync(long photoID)
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT * FROM ProductPhotos WHERE PhotoID = @PhotoID";
                var data = await connection.QueryFirstOrDefaultAsync<ProductPhoto>(sql, new { PhotoID = photoID });
                return data;
            }
        }

        public async Task<bool> IsUsedAsync(int productID)
        {
            using (var connection = GetConnection())
            {
                var sql = @"IF EXISTS(SELECT * FROM OrderDetails WHERE ProductID = @ProductID)
                                SELECT 1
                            ELSE
                                SELECT 0";
                var result = await connection.ExecuteScalarAsync<int>(sql, new { ProductID = productID });
                return result == 1;
            }
        }

        public async Task<PagedResult<Product>> ListAsync(ProductSearchInput input)
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT COUNT(*) FROM Products
                            WHERE ((@SearchValue = N'') OR (ProductName LIKE @SearchValue))
                                AND (@CategoryID = 0 OR CategoryID = @CategoryID)
                                AND (@SupplierID = 0 OR SupplierID = @SupplierID)
                                AND (Price >= @MinPrice) AND (@MaxPrice <= 0 OR Price <= @MaxPrice);

                            SELECT * FROM Products
                            WHERE ((@SearchValue = N'') OR (ProductName LIKE @SearchValue))
                                AND (@CategoryID = 0 OR CategoryID = @CategoryID)
                                AND (@SupplierID = 0 OR SupplierID = @SupplierID)
                                AND (Price >= @MinPrice) AND (@MaxPrice <= 0 OR Price <= @MaxPrice)
                            ORDER BY ProductName
                            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";
                
                var parameters = new
                {
                    SearchValue = $"%{input.SearchValue}%",
                    input.CategoryID,
                    input.SupplierID,
                    input.MinPrice,
                    input.MaxPrice,
                    input.Offset,
                    input.PageSize
                };

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    var rowCount = await multi.ReadFirstAsync<int>();
                    var data = (await multi.ReadAsync<Product>()).ToList();

                    return new PagedResult<Product>
                    {
                        Page = input.Page,
                        PageSize = input.PageSize,
                        RowCount = rowCount,
                        DataItems = data
                    };
                }
            }
        }

        public async Task<List<ProductAttribute>> ListAttributesAsync(int productID)
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT * FROM ProductAttributes WHERE ProductID = @ProductID ORDER BY DisplayOrder";
                var data = (await connection.QueryAsync<ProductAttribute>(sql, new { ProductID = productID })).ToList();
                return data;
            }
        }

        public async Task<List<ProductPhoto>> ListPhotosAsync(int productID)
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT * FROM ProductPhotos WHERE ProductID = @ProductID ORDER BY DisplayOrder";
                var data = (await connection.QueryAsync<ProductPhoto>(sql, new { ProductID = productID })).ToList();
                return data;
            }
        }

        public async Task<bool> UpdateAsync(Product data)
        {
            using (var connection = GetConnection())
            {
                var sql = @"UPDATE Products 
                            SET ProductName = @ProductName,
                                ProductDescription = @ProductDescription,
                                SupplierID = @SupplierID,
                                CategoryID = @CategoryID,
                                Unit = @Unit,
                                Price = @Price,
                                Photo = @Photo,
                                IsSelling = @IsSelling
                            WHERE ProductID = @ProductID";
                var rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }

        public async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            using (var connection = GetConnection())
            {
                var sql = @"UPDATE ProductAttributes 
                            SET AttributeName = @AttributeName,
                                AttributeValue = @AttributeValue,
                                DisplayOrder = @DisplayOrder
                            WHERE AttributeID = @AttributeID";
                var rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }

        public async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            using (var connection = GetConnection())
            {
                var sql = @"UPDATE ProductPhotos 
                            SET Photo = @Photo,
                                Description = @Description,
                                DisplayOrder = @DisplayOrder,
                                IsHidden = @IsHidden
                            WHERE PhotoID = @PhotoID";
                var rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }
    }
}
