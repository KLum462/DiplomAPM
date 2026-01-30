using System;
using System.Data;
using System.Data.SqlClient; 
using System.Windows;

namespace DiplomAPM
{
    public partial class MainWindow : Window
    {
      
        string connectionString = @"Server=localhost;Database=DiplomAPM;Trusted_Connection=True;";

        public MainWindow()
        {
            InitializeComponent();
        }
        private void TogglePassword_Click(object sender, RoutedEventArgs e)
        {
            // Если текстовое поле скрыто (пароль в виде точек)
            if (txtPasswordVisible.Visibility == Visibility.Collapsed)
            {
                // Копируем пароль в текстовое поле
                txtPasswordVisible.Text = txtPassword.Password;
                // Переключаем видимость
                txtPasswordVisible.Visibility = Visibility.Visible;
                txtPassword.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Копируем текст обратно в PasswordBox
                txtPassword.Password = txtPasswordVisible.Text;
                // Переключаем видимость обратно
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Visible;
            }
        }
        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();

            // Выбираем актуальный пароль в зависимости от того, какое поле сейчас активно
            string password = (txtPasswordVisible.Visibility == Visibility.Visible)
                              ? txtPasswordVisible.Text
                              : txtPassword.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                txtError.Text = "Введите логин и пароль";
                txtError.Visibility = Visibility.Visible;
                return;
            }
            // Открываем Dashboard
            DashboardWindow dash = new DashboardWindow();
            dash.Show();
            this.Close(); // Закрываем окно логина
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Используем параметры (@u, @p) - это защита от SQL-инъекций (Комиссия спросит!)
                    string query = "SELECT RoleID, FIO FROM Users WHERE Login=@u AND Password=@p";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@u", login);
                    cmd.Parameters.AddWithValue("@p", password);

                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                    {
               
                        reader.Read();
                        int roleId = reader.GetInt32(0);
                        string fio = reader.GetString(1);

     
                        MessageBox.Show($"Добро пожаловать, {fio}!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                  
                    }
                    else
                    {
                        ShowError("Неверный логин или пароль");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка подключения к БД: " + ex.Message);
            }
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }
    }
}