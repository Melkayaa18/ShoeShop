using System;
using System.Windows;
using Microsoft.Data.SqlClient;

namespace ShoeShop.Windowss
{
    public partial class LoginWindow : Window
    {
        private readonly string conn =
            @"Server=localhost;Database=ShoeShopDb;Trusted_Connection=True;TrustServerCertificate=True;";

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(LoginTextBox.Text))
            {
                ErrorText.Text = "Введите логин"; ErrorText.Visibility = Visibility.Visible; return;
            }
            try
            {
                using var con = new SqlConnection(conn); con.Open();
                using var cmd = new SqlCommand("SELECT UserId, CONCAT(SurName, ' ', Name, ' ', Patronymic) AS FIO FROM Users WHERE Login = @Login", con);
                cmd.Parameters.AddWithValue("@Login", LoginTextBox.Text.Trim());

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var app = (App)Application.Current;
                    app.UserId = Convert.ToInt32(reader["UserId"]);
                    app.UserName = reader["FIO"].ToString();
                    new MainWindow().Show(); this.Close();
                }
                else
                {
                    ErrorText.Text = "Логин не найден"; ErrorText.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка БД: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}