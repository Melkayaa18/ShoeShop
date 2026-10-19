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
        private readonly string conn = @"Server=localhost;Database=ShoeShopDb;Trusted_Connection=True;TrustServerCertificate=True;";
        int productId;
        public ProductWindow(int id)
        {
            InitializeComponent();
            productId = id;
            LoadData();
        }
        private void LoadData()
        {
            try
            {
                using var con = new SqlConnection(conn); con.Open();
                // 1. Товар
                using var cmd1 = new SqlCommand("SELECT p.Title, m.ManufacturerName, c.CategoryName, p.Price, p.Composition, p.ImageName, p.Description" +
                    "FROM Products p LEFT JOIN Manufacturers m ON p.ManufacturerId = m.ManufacturerId" +
                    "LEFT JOIN Categories c ON p.CategoryId = c.CategoryId WHERE p.ProductId = @Id", con);
                cmd1.Parameters.AddWithValue("@Id", productId);
                using var r1 = cmd1.ExecuteReader();
                if (r1.Read())
                {
                    TitleText.Text = r1["Title"].ToString();
                    ManufacturerText.Text = "Производство: " + r1["ManufacturerName"];
                    CategoryText.Text = "Категория: " + r1["CategoryName"];
                    CompositionText.Text = r1["Composition"].ToString();
                    DescriptionText.Text = r1["Description"] != DBNull.Value ? r1["Description"].ToString() : "Нет";
                    PriceText.Text = $"Цена: {Convert.ToDecimal(r1["Price"]):N2} руб.";
                    string img = r1["ImageName"]?.ToString();
                    if (!string.IsNullOrEmpty(img)) ProductImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri($"pack://application:,,,/Resources/{img}", UriKind.Absolute));
                }

                // 2. Размеры
                using var cmd2 = new SqlCommand("SELECT s.StockItemId, sz.SizeValue, s.Quantity FROM StockItems s JOIN Sizes sz" +
                    "ON s.SizeId = sz.SizeId WHERE s.ProductId = @Id AND s.Quantity > 0", con);
                cmd2.Parameters.AddWithValue("@Id", productId);
                var sizes = new List<SizeModel>();
                using var r2 = cmd2.ExecuteReader();
                while (r2.Read()) sizes.Add(new SizeModel { StockItemId = Convert.ToInt32(r2["StockItemId"]), DisplaySize = $"Размер: {r2["SizeValue"]} (Доступно: {r2["Quantity"]})", MaxQuantity = Convert.ToInt32(r2["Quantity"]) });
                SizeComboBox.ItemsSource = sizes;
            }
            catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
        }
        private void QuantityTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) => e.Handled = !char.IsDigit(e.Text, e.Text.Length - 1);
        private void CancelButton_Click(object sender, RoutedEventArgs e) => this.Close();

        private void OrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (SizeComboBox.SelectedItem is not SizeModel size) {MessageBox.Show("Выберите размер", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (!int.TryParse(QuantityTextBox.Text, out int qty) || qty <= 0) { MessageBox.Show("Неверное количество", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (qty > size.MaxQuantity) { MessageBox.Show($"Доступно только: {size.MaxQuantity}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            var app = (App)Application.Current;
            if (app.UserId == null) return;

            try
            {
                using var con = new SqlConnection(conn); con.Open();
                using var tran = con.BeginTransaction();
                try
                {
                    using var cmd1 = new SqlCommand("INSERT INTO Orders (UserId, OrderDate) OUTPUT INSERTED.OrderId VALUES (@UserId, GETDATE())", con, tran);
                    cmd1.Parameters.AddWithValue("@UserId", app.UserId);
                    int orderId = (int)cmd1.ExecuteScalar();

                    using var cmd2 = new SqlCommand("INSERT INTO OrderItems (OrderId, StockItemId, Quantity) VALUES (@OrderId, @StockItemId, @Quantity)", con, tran);
                    cmd2.Parameters.AddWithValue("@OrderId", orderId);
                    cmd2.Parameters.AddWithValue("@StockItemId", size.StockItemId);
                    cmd2.Parameters.AddWithValue("@Quantity", qty);
                    cmd2.ExecuteNonQuery();

                    using var cmd3 = new SqlCommand("UPDATE StockItems SET Quantity = Quantity - @Quantity WHERE StockItemId = @StockItemId", con, tran);
                    cmd3.Parameters.AddWithValue("@Quantity", qty);
                    cmd3.Parameters.AddWithValue("@StockItemId", size.StockItemId);
                    cmd3.ExecuteNonQuery();

                    tran.Commit();
                    MessageBox.Show("Заказ оформлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                catch { tran.Rollback(); throw; }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка БД: " + ex.Message); }
        }
    }
    public class SizeModel { public int StockItemId { get; set; } public string DisplaySize { get; set; } public int MaxQuantity { get; set; } }
}
