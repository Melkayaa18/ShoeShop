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
        private readonly string _connectionString =
            @"Server=localhost;Database=ShoeShopDb;Trusted_Connection=True;TrustServerCertificate=True;";

        private List<ShoeModel> _allShoes = new List<ShoeModel>();
        public MainWindow()
        {
            InitializeComponent(); // ОБЯЗАТЕЛЬНО ПЕРВОЙ СТРОКОЙ!

            // Отображаем ФИО авторизованного пользователя
            var app = (App)Application.Current;

            // Обращаемся строго к UserFullNameText (никаких UserSurNameText!)
            UserFullNameText.Text = app.CurrentUserFullName;

            LoadShoesData();
        }
        private void LoadShoesData()
        {
            _allShoes.Clear();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Все названия столбцов — английские, после переименования в БД
                    string query = @"
                        SELECT 
                            p.ProductId,
                            p.Title, 
                            m.ManufacturerName AS Manufacturer, 
                            c.CategoryName AS Category, 
                            p.Composition, 
                            p.Price AS BasePrice,
                            p.ImageName,
                            ISNULL(SUM(s.Quantity), 0) AS TotalQuantity,
                            (SELECT COUNT(*) 
                             FROM OrderItems oi 
                             JOIN Orders o ON oi.OrderId = o.OrderId 
                             WHERE oi.StockItemId IN (
                                 SELECT StockItemId FROM StockItems WHERE ProductId = p.ProductId)
                             AND MONTH(o.OrderDate) = MONTH(DATEADD(month, -1, GETDATE())) 
                             AND YEAR(o.OrderDate) = YEAR(DATEADD(month, -1, GETDATE()))
                            ) AS OrdersLastMonth
                        FROM Products p
                        LEFT JOIN Manufacturers m ON p.ManufacturerId = m.ManufacturerId
                        LEFT JOIN Categories c ON p.CategoryId = c.CategoryId
                        LEFT JOIN StockItems s ON p.ProductId = s.ProductId
                        GROUP BY p.ProductId, p.Title, m.ManufacturerName, c.CategoryName, 
                                 p.Composition, p.Price, p.ImageName;";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _allShoes.Add(new ShoeModel
                            {
                                ProductId = Convert.ToInt32(reader["ProductId"]),
                                Title = reader["Title"].ToString(),
                                Manufacturer = reader["Manufacturer"].ToString(),
                                Category = reader["Category"].ToString(),
                                Composition = reader["Composition"].ToString(),
                                BasePrice = Convert.ToDecimal(reader["BasePrice"]),
                                ImagePath = reader["ImageName"] != DBNull.Value
                                    ? reader["ImageName"].ToString() : string.Empty,
                                TotalQuantity = Convert.ToInt32(reader["TotalQuantity"]),
                                OrdersLastMonth = Convert.ToInt32(reader["OrdersLastMonth"])
                            });
                        }
                    }
                }

                // Заполняем выпадающий список категорий
                var categories = _allShoes.Select(s => s.Category).Distinct().OrderBy(c => c).ToList();
                CategoryFilterComboBox.Items.Clear();
                CategoryFilterComboBox.Items.Add("Все категории");
                foreach (var cat in categories)
                    CategoryFilterComboBox.Items.Add(cat);
                CategoryFilterComboBox.SelectedIndex = 0;

                // Выбираем первый пункт сортировки
                SortComboBox.SelectedIndex = 0;

                ApplyFiltersAndSort();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при загрузке данных: {ex.Message}\nПроверьте, запущен ли SQL Server и существует ли БД ShoeShopDb.",
                    "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        // Метод для применения поиска, фильтрации и сортировки в реальном времени
        private void ApplyFiltersAndSort()
        {
            // Защита: если элементы окна ещё не созданы, выходим
            if (ShoeCatalogList == null) return;

            string searchText = SearchTextBox.Text.Trim().ToLower();
            string selectedCategory = CategoryFilterComboBox.SelectedItem?.ToString();
            string selectedSort = (SortComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            var filtered = _allShoes.AsEnumerable();

            // Поиск по наименованию и составу
            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = filtered.Where(s =>
                    s.Title.ToLower().Contains(searchText) ||
                    s.Composition.ToLower().Contains(searchText));
            }

            // Фильтр по категории
            if (selectedCategory != null && selectedCategory != "Все категории")
            {
                filtered = filtered.Where(s => s.Category == selectedCategory);
            }

            // Сортировка
            if (selectedSort == "По цене (возрастание)")
                filtered = filtered.OrderBy(s => s.DiscountedPrice);
            else if (selectedSort == "По цене (убывание)")
                filtered = filtered.OrderByDescending(s => s.DiscountedPrice);

            ShoeCatalogList.ItemsSource = filtered.ToList();
        }
        // Обработчики событий для реального времени
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFiltersAndSort();
        private void CategoryFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFiltersAndSort();
        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFiltersAndSort();

        private void ShoeCatalogList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ShoeCatalogList.SelectedItem is ShoeModel selectedShoe)
            {
                var productWindow = new ProductWindow(selectedShoe.ProductId);
                productWindow.ShowDialog();
                LoadShoesData(); // Обновляем данные после возврата из заказа
            }
        }
    }
}