using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoeShop
{
    public class ShoeModel
    {
        // Базовые данные, которые мы достанем из БД
        public string Title { get; set; }
        public string Manufacturer { get; set; }
        public string Category { get; set; }
        public string Composition { get; set; }
        public decimal BasePrice { get; set; }
        public int TotalQuantity { get; set; }
        public int OrdersLastMonth { get; set; } // Кол-во заказов за прошлый месяц
        public string ImagePath { get; set; }

        // 1. Алгоритм расчета цены (скидка 25%, если заказов в прошлом месяце не было)
        public decimal DiscountedPrice
        {
            get
            {
                if (OrdersLastMonth == 0)
                {
                    return BasePrice * 0.75m; // Скидка 25%
                }
                return BasePrice;
            }
        }

        // 2. Определение текста остатка ("много" или "мало")
        public string QuantityDisplay
        {
            get
            {
                if (TotalQuantity > 5) return "много";
                return "мало";
            }
        }

        // 3. Цвет фона (светло-красный, если остаток <= 3)
        public string BackgroundColor
        {
            get
            {
                if (TotalQuantity <= 3) return "#FFCCCC"; // Светло-красный
                return "#FFFFFF"; // Белый (стандартный)
            }
        }

        // 4. Подстановка заглушки, если картинки нет
        public string DisplayImage
        {
            get
            {
                // Если путь пустой, берем картинку из ресурсов
                if (string.IsNullOrWhiteSpace(ImagePath))
                {
                    return "/Resources/picture.png";
                }
                // Иначе возвращаем реальный путь из базы (добавь папку перед именем, если нужно)
                return $"/Resources/{ImagePath}";
            }
        }
    }
}
