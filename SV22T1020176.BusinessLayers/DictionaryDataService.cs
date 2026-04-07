using SV22T1020176.DataLayers.Interfaces;
using SV22T1020176.DataLayers.SqlServer;
using SV22T1020176.Models.DataDictionary;
using System.Threading.Tasks;

namespace SV22T1020176.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến từ điển dữ liệu
    /// </summary>
    public static class DictionaryDataService
    {
        private static readonly IDataDictionaryRepository<Province> provinceDB;

        /// <summary>
        /// Ctor
        /// </summary>
        static DictionaryDataService()
        {
            provinceDB = new ProvinceRepository(Configuration.ConnectionString);
        }
        /// <summary>
        /// Lấy danh sách tỉnh thành
        /// </summary>
        /// <returns></returns>
        public static async Task<List<Province>> ListProvincesAsync()
        {
            return await provinceDB.ListAsync();
        }
    }
}
