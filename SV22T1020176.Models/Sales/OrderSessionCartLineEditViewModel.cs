namespace SV22T1020176.Models.Sales
{
    /// <summary>
    /// Dùng cho modal sửa dòng giỏ hàng khi lập đơn mới (session, chưa có OrderID).
    /// </summary>
    public class OrderSessionCartLineEditViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = "";
        public string Unit { get; set; } = "";
        public string Photo { get; set; } = "";
        public int Quantity { get; set; }
        public decimal SalePrice { get; set; }
    }
}
