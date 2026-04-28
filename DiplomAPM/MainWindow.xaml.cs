using DiplomAPM;
using System;
using System.Data;
using System.Data.SqlClient; 
using System.Windows;
using System.Configuration; // Не забудьте добавить using
namespace DiplomAPM
{
    public partial class MainWindow : Window
    {

        string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

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
            string password = (txtPasswordVisible.Visibility == Visibility.Visible)
                              ? txtPasswordVisible.Text
                              : txtPassword.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ShowError("Введите логин и пароль");
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // ВАЖНО: В запросе обязательно нужно достать UserID, чтобы записать его в сессию!
                    string query = "SELECT UserID, RoleID, FIO FROM Users WHERE Login=@u AND Password=@p";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@u", login);
                    // Хешируем введенный логин, чтобы сравнить его с хешем в БД
                    cmd.Parameters.AddWithValue("@p", DiplomAPM.Helpers.PasswordHasher.HashPassword(password));

                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                    {
                        reader.Read(); // Читаем первую строку

                        // 1. Сохраняем данные в глобальную сессию (чтобы ушла ошибка в AuditLogger)
                        UserSession.UserId = reader.GetInt32(0); // 0 - это индекс колонки UserID
                                                                 // RoleID пропускаем (индекс 1), если он пока не нужен
                        UserSession.FIO = reader.GetString(2);   // 2 - это индекс колонки FIO

                        // 2. Логируем вход (Теперь ошибки не будет, т.к. UserSession заполнен)
                        AuditLogger.Log("Вход", $"Пользователь {UserSession.FIO} вошел в систему");

                        // 3. Только ТЕПЕРЬ открываем главное окно
                        DashboardWindow dash = new DashboardWindow();
                        dash.Show();
                        this.Close();
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