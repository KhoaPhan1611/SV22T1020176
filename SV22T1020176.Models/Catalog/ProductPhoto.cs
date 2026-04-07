using System.ComponentModel.DataAnnotations;

namespace SV22T1020176.Models.Catalog
{
    /// <summary>
    /// Ảnh của mặt hàng
    /// </summary>
    public class ProductPhoto
    {
        /// <summary>
        /// Mã ảnh
        /// </summary>
        public long PhotoID { get; set; }
        /// <summary>
        /// Mã mặt hàng
        /// </summary>
        public int ProductID { get; set; }
        /// <summary>
        /// Tên file ảnh
        /// </summary>
        /// [Required] - Removed because file upload is handled manually in Controller via IFormFile
        [StringLength(255)]
        public string Photo { get; set; } = string.Empty;
        /// <summary>
        /// Mô tả ảnh
        /// </summary>
        [Required(ErrorMessage = "Mô tả ảnh không được để trống")]
        [StringLength(255, ErrorMessage = "Mô tả ảnh không được vượt quá 255 ký tự")]
        public string Description { get; set; } = string.Empty;
        /// <summary>
        /// Thứ tự hiển thị (giá trị nhỏ sẽ hiển thị trước)
        /// </summary>
        public int DisplayOrder { get; set; }
        /// <summary>
        /// Có ẩn ảnh đối với khách hàng hay không?
        /// </summary>
        public bool IsHidden { get; set; }
    }
}