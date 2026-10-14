using System;

namespace ShoeShop
{
    public class ShoeModel
    {
        public int ProductId { get; set; }
        public string Title { get; set; }
        public string Manufacturer { get; set; }
        public string Category { get; set; }
        public string Composition { get; set; }
        public string Description { get; set; }
        public decimal BasePrice { get; set; }
        public int TotalQuantity { get; set; }
        public int OrdersLastMonth { get; set; }
        public string ImagePath { get; set; }

        // Скидка 25%, если заказов в прошлом месяце не было
        public decimal DiscountedPrice
        {
            get
            {
                if (OrdersLastMonth == 0)
                    return BasePrice * 0.75m;
                return BasePrice;
            }
        }

        // "много" если больше 5, иначе "мало"
        public string QuantityDisplay
        {
            get
            {
                if (TotalQuantity > 5) return "В наличии: много";
                return "В наличии: мало";
            }
        }

        // Светло-красный фон, если остаток <= 3
        public string BackgroundColor
        {
            get
            {
                if (TotalQuantity <= 3) return "#FFCCCC";
                return "#FFFFFF";
            }
        }

        // Заглушка, если картинки нет
        public string DisplayImage
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ImagePath))
                    return "/Resources/picture.png";
                return $"/Resources/{ImagePath}";
            }
        }
    }
}