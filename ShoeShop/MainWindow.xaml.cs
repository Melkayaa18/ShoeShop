using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;


namespace ShoeShop
{

    public partial class MainWindow : Window
    {
        string connectionString = @"Server=localhost;Database=ShoeShopDb;Trusted_Connection=True;TrustServerCertificate=True;";
        public MainWindow()
        {
            InitializeComponent();
            LoadShoesData(); // Запускаем загрузку сразу при открытии окна
        }
        private void LoadShoesData()
        {
            List<ShoeModel> myListOfShoes = new List<ShoeModel>();

            // Запрос адаптрован под MS SQL Server:
            string query = @"
    SELECT 
        p.[Наименование_товара] AS Title, 
        p.[Производство] AS Manufacturer, 
        p.[Категория] AS Category, 
        p.[Состав] AS Composition, 
        p.[Цена] AS BasePrice,
        p.[Изображение] AS ImagePath,
        -- Преобразуем текст (nvarchar) в число (INT), чтобы функция SUM смогла его сложить:
        ISNULL(SUM(CAST(s.[Количество_доступное_для_заказа] AS INT)), 0) AS TotalQuantity,
        
        (SELECT COUNT(*) 
         FROM OrderItems oi 
         JOIN Orders o ON oi.OrderId = o.OrderId 
         WHERE oi.StockItemId IN (SELECT StockItemId FROM StockItems WHERE ProductId = p.ProductId)
         -- Проверка за прошлый календарный месяц:
         AND MONTH(o.[Дата_заказа]) = MONTH(DATEADD(month, -1, GETDATE())) 
         AND YEAR(o.[Дата_заказа]) = YEAR(DATEADD(month, -1, GETDATE()))
        ) AS OrdersLastMonth

    FROM Products p
    LEFT JOIN StockItems s ON p.ProductId = s.ProductId
    GROUP BY 
        p.ProductId, 
        p.[Наименование_товара], 
        p.[Производство], 
        p.[Категория], 
        p.[Состав], 
        p.[Цена], 
        p.[Изображение];";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(query, connection);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ShoeModel shoe = new ShoeModel
                            {
                                Title = reader["Title"].ToString(),
                                Manufacturer = reader["Manufacturer"].ToString(),
                                Category = reader["Category"].ToString(),
                                Composition = reader["Composition"].ToString(),
                                BasePrice = Convert.ToDecimal(reader["BasePrice"]),
                                ImagePath = reader["ImagePath"] != DBNull.Value ? reader["ImagePath"].ToString() : string.Empty,
                                TotalQuantity = Convert.ToInt32(reader["TotalQuantity"]),
                                OrdersLastMonth = Convert.ToInt32(reader["OrdersLastMonth"])
                            };
                            myListOfShoes.Add(shoe);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка подключения к БД: " + ex.Message);
                }
            }

            ShoeCatalogList.ItemsSource = myListOfShoes;
        }
    }
}