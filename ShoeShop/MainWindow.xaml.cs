using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Data.SqlClient;
using ShoeShop.Windowss;


namespace ShoeShop
{

    public partial class MainWindow : Window
    {
        private readonly string conn =
            @"Server=localhost;Database=ShoeShopDb;Trusted_Connection=True;TrustServerCertificate=True;";

        List<ShoeModel> allShoes = new List<ShoeModel>();
        public MainWindow()
        {
            InitializeComponent(); // ОБЯЗАТЕЛЬНО ПЕРВОЙ СТРОКОЙ!

            // Отображаем ФИО авторизованного пользователя
            var app = (App)Application.Current;

            // Обращаемся строго к UserFullNameText (никаких UserSurNameText!)
            UserNameText.Text = ((App)Application.Current).UserName;
            LoadData();
        }
        private void LoadData()
        {
            try
            {
                using var con = new SqlConnection(conn); con.Open();
                string sql = @"SELECT p.ProductId, p.Title, m.ManufacturerName AS Manufacturer,
            c.CategoryName AS Category, p.Composition, p.Price AS BasePrice,
            p.ImageName, ISNULL(SUM(s.Quantity), 0) AS TotalQuantity,
             (SELECT COUNT(*) FROM OrderItems oi 
            JOIN Orders o ON oi.OrderId = o.OrderId WHEREoi.StockItemId
            IN (SELECT StockItemId FROM StockItems WHERE ProductId = p.ProductId)
            AND MONTH(o.OrderDate) = MONTH(DATEADD(month, -1, GETDATE()))
            AND YEAR(o.OrderDate) = YEAR(DATEADD(month, -1, GETDATE()))) AS OrdersLastMonth
            FROM Products p LEFT JOIN Manufacturers m ON p.ManufacturerId = m.ManufacturerId
            LEFT JOIN Categories c ON p.CategoryId = c.CategoryId LEFT JOIN StockItems s
            ON p.ProductId = s.ProductId GROUP BY p.ProductId, p.Title, m.ManufacturerName, c.CategoryName, p.Composition, p.Price, p.ImageName";

                using var cmd = new SqlCommand(sql, con);
                using var reader = cmd.ExecuteReader();
                allShoes.Clear();
                while (reader.Read())
                {
                    allShoes.Add(new ShoeModel
                    {
                        ProductId = Convert.ToInt32(reader["ProductId"]),
                        Title = reader["Title"].ToString(),
                        Manufacturer = reader["Manufacturer"].ToString(),
                        Category = reader["Category"].ToString(),
                        Composition = reader["Composition"].ToString(),
                        BasePrice = Convert.ToDecimal(reader["BasePrice"]),
                        ImagePath = reader["ImageName"] != DBNull.Value ? reader["ImageName"].ToString() : "",
                        TotalQuantity = Convert.ToInt32(reader["TotalQuantity"]),
                        OrdersLastMonth = Convert.ToInt32(reader["OrdersLastMonth"])
                    });
                }

                CategoryFilter.Items.Clear(); CategoryFilter.Items.Add("Все категории");
                foreach (var cat in allShoes.Select(s => s.Category).Distinct().OrderBy(c => c)) CategoryFilter.Items.Add(cat);
                CategoryFilter.SelectedIndex = 0; SortFilter.SelectedIndex = 0;
                FilterAndSort(null, null);
            }
            catch (Exception ex) { MessageBox.Show("Ошибка БД: " + ex.Message); }
        }

        private void FilterAndSort(object sender, EventArgs e)
        {
            if (ShoeList == null) return;
            var q = allShoes.AsEnumerable();

            string txt = SearchTextBox.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(txt)) q = q.Where(s => s.Title.ToLower().Contains(txt) || s.Composition.ToLower().Contains(txt));

            string cat = CategoryFilter.SelectedItem?.ToString();
            if (cat != null && cat != "Все категории") q = q.Where(s => s.Category == cat);

            string sort = (SortFilter.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (sort == "По цене (возрастание)") q = q.OrderBy(s => s.DiscountedPrice);
            else if (sort == "По цене (убывание)") q = q.OrderByDescending(s => s.DiscountedPrice);

            ShoeList.ItemsSource = q.ToList();
        }

        private void ShoeList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ShoeList.SelectedItem is ShoeModel shoe)
            {
                new ProductWindow(shoe.ProductId).ShowDialog();
                LoadData(); // Обновляем остатки после заказа
            }
        }
    }
}