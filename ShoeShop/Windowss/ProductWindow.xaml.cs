using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ShoeShop.Windowss
{
    /// <summary>
    /// Логика взаимодействия для ProductWindow.xaml
    /// </summary>
    public partial class ProductWindow : Window
    {
        private readonly string _connectionString = @"Server=localhost;Database=ShoeShopDb;Trusted_Connection=True;TrustServerCertificate=True;";
        private readonly int _productId;
        public ProductWindow(int productId)
        {
            InitializeComponent();
            _productId = productId;
            LoadProductDetails();
        }
        private void LoadProductDetails()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Загружаем данные товара (все столбцы на английском)
                    string productQuery = @"
                        SELECT p.Title, m.ManufacturerName, c.CategoryName, 
                               p.Price, p.Composition, p.ImageName, p.Description
                        FROM Products p
                        LEFT JOIN Manufacturers m ON p.ManufacturerId = m.ManufacturerId
                        LEFT JOIN Categories c ON p.CategoryId = c.CategoryId
                        WHERE p.ProductId = @ProductId";

                    using (SqlCommand cmd = new SqlCommand(productQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@ProductId", _productId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                TitleText.Text = reader["Title"].ToString();
                                ManufacturerText.Text = "Производство: " + reader["ManufacturerName"];
                                CategoryText.Text = "Категория: " + reader["CategoryName"];
                                CompositionText.Text = reader["Composition"].ToString();
                                DescriptionText.Text = reader["Description"] != DBNull.Value
                                    ? reader["Description"].ToString() : "Нет описания";
                                PriceText.Text = $"Цена: {Convert.ToDecimal(reader["Price"]):N2} руб.";

                                string imgName = reader["ImageName"]?.ToString();
                                if (!string.IsNullOrWhiteSpace(imgName))
                                {
                                    try
                                    {
                                        ProductImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                                            new Uri($"pack://application:,,,/Resources/{imgName}", UriKind.Absolute));
                                    }
                                    catch { /* Если картинка не найдена, просто не показываем */ }
                                }
                            }
                        }
                    }

                    // Загружаем доступные размеры (SizeValue и Quantity — английские)
                    string sizesQuery = @"
                        SELECT s.StockItemId, sz.SizeValue, s.Quantity
                        FROM StockItems s
                        JOIN Sizes sz ON s.SizeId = sz.SizeId
                        WHERE s.ProductId = @ProductId AND s.Quantity > 0
                        ORDER BY sz.SizeValue";

                    using (SqlCommand cmd = new SqlCommand(sizesQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@ProductId", _productId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            var sizes = new List<SizeModel>();
                            while (reader.Read())
                            {
                                sizes.Add(new SizeModel
                                {
                                    StockItemId = Convert.ToInt32(reader["StockItemId"]),
                                    DisplaySize = $"Размер: {reader["SizeValue"]} (Доступно: {reader["Quantity"]} шт.)",
                                    MaxQuantity = Convert.ToInt32(reader["Quantity"])
                                });
                            }
                            SizeComboBox.ItemsSource = sizes;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных товара: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        // Разрешаем вводить только цифры в поле количества
        // Разрешаем вводить только цифры
        private void QuantityTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, e.Text.Length - 1);
        }

        private void QuantityTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                foreach (char c in text)
                    if (!char.IsDigit(c)) { e.CancelCommand(); return; }
            }
            else
            {
                e.CancelCommand();
            }
        }
        

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OrderButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Валидация ввода
            if (SizeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите размер обуви из списка.",
                    "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Количество должно быть целым числом больше нуля.",
                    "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedSize = (SizeModel)SizeComboBox.SelectedItem;
            if (quantity > selectedSize.MaxQuantity)
            {
                MessageBox.Show($"Недостаточно товара на складе. Доступно только: {selectedSize.MaxQuantity} шт.",
                    "Ошибка количества", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var app = (App)Application.Current;
            if (app.CurrentUserId == null)
            {
                MessageBox.Show("Ошибка авторизации. Войдите в систему заново.",
                    "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // Оформление заказа в транзакции
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();

                    try
                    {
                        // Создаём заказ
                        string insertOrder = "INSERT INTO Orders (UserId, OrderDate) OUTPUT INSERTED.OrderId VALUES (@UserId, GETDATE())";
                        int orderId;
                        using (SqlCommand cmd = new SqlCommand(insertOrder, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@UserId", app.CurrentUserId.Value);
                            orderId = (int)cmd.ExecuteScalar();
                        }

                        // Добавляем позицию в заказ
                        string insertItem = "INSERT INTO OrderItems (OrderId, StockItemId, Quantity) VALUES (@OrderId, @StockItemId, @Quantity)";
                        using (SqlCommand cmd = new SqlCommand(insertItem, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@OrderId", orderId);
                            cmd.Parameters.AddWithValue("@StockItemId", selectedSize.StockItemId);
                            cmd.Parameters.AddWithValue("@Quantity", quantity);
                            cmd.ExecuteNonQuery();
                        }

                        // Уменьшаем остаток на складе
                        string updateStock = "UPDATE StockItems SET Quantity = Quantity - @Quantity WHERE StockItemId = @StockItemId";
                        using (SqlCommand cmd = new SqlCommand(updateStock, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Quantity", quantity);
                            cmd.Parameters.AddWithValue("@StockItemId", selectedSize.StockItemId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        MessageBox.Show("Заказ успешно оформлен! Спасибо за покупку.",
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        this.Close();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось оформить заказ: {ex.Message}",
                    "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Вспомогательный класс для ComboBox размеров
    public class SizeModel
    {
        public int StockItemId { get; set; }
        public string DisplaySize { get; set; }
        public int MaxQuantity { get; set; }
    }
}