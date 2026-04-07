namespace SV22T1020176.Shop.Models
{
    public class CartItem
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = "";
        public string Photo { get; set; } = "";
        public string Unit { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; } = 1;

        public decimal TotalPrice => Price * Quantity;
    }
}
