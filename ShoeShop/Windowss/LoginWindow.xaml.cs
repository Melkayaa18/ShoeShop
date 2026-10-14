using System;
using System.Windows;
using Microsoft.Data.SqlClient;

namespace ShoeShop.Windowss
{
    public partial class LoginWindow : Window
    {
        private readonly string _connectionString =
            @"Server=localhost;Database=ShoeShopDb;Trusted_Connection=True;TrustServerCertificate=True;";

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(login))
            {
                ShowError("Поле логина не может быть пустым. Пожалуйста, введите ваш логин.");
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Склеиваем SurName + Name + Patronymic в одно поле FullName
                    string query = @"
                        SELECT UserId, 
                               CONCAT(SurName, ' ', Name, ' ', Patronymic) AS FullName 
                        FROM Users 
                        WHERE Login = @Login";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Login", login);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var app = (App)Application.Current;
                                app.CurrentUserId = Convert.ToInt32(reader["UserId"]);
                                app.CurrentUserFullName = reader["FullName"].ToString();

                                MainWindow mainWindow = new MainWindow();
                                mainWindow.Show();
                                this.Close();
                            }
                            else
                            {
                                ShowError("Пользователь с таким логином не найден. Проверьте правильность ввода.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка подключения к базе данных: {ex.Message}\nПроверьте, запущен ли SQL Server.",
                    "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}