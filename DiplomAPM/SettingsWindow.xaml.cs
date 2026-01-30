using System;
using System.Data.SqlClient;
using System.Windows;

namespace DiplomAPM
{
    public partial class SettingsWindow : Window
    {
        string connectionString = @"Server=localhost;Database=DiplomAPM;Trusted_Connection=True;";

        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void BtnChangePass_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text;
            string oldPass = pbOldPass.Password;
            string newPass = pbNewPass.Password;

            // 1. Проверка на пустоту
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(oldPass) || string.IsNullOrWhiteSpace(newPass))
            {
                MessageBox.Show("Заполните все поля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // 2. ХИТРЫЙ ЗАПРОС: Мы пытаемся обновить пароль, НО только если Логин и Старый пароль совпадают.
                    string query = "UPDATE Users SET Password = @new WHERE Login = @login AND Password = @old";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@login", login);
                    cmd.Parameters.AddWithValue("@old", oldPass);
                    cmd.Parameters.AddWithValue("@new", newPass);

                    // ExecuteNonQuery возвращает количество измененных строк
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Пароль успешно изменен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        // Очищаем поля
                        pbOldPass.Clear();
                        pbNewPass.Clear();
                    }
                    else
                    {
                        MessageBox.Show("Неверный логин или старый пароль.\nИзменения не сохранены.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Системная ошибка: " + ex.Message);
            }
        }
    }
}