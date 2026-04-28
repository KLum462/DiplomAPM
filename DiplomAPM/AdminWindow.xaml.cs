using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Configuration; // Не забудьте добавить using
namespace DiplomAPM
{
    public partial class AdminWindow : Window
    {
        private static string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        int selectedUserId = -1; // Для определения, редактируем мы или создаем нового

        public AdminWindow()
        {
            InitializeComponent();
            LoadRoles();
            LoadUsers();
        }

        private void LoadRoles()
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Roles", con);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cbRoles.ItemsSource = dt.DefaultView;
            }
        }

        private void LoadUsers()
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT u.UserID, u.FIO, u.Login, r.RoleName, u.RoleID FROM Users u JOIN Roles r ON u.RoleID = r.RoleID";
                SqlDataAdapter da = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgUsers.ItemsSource = dt.DefaultView;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtLogin.Text) || cbRoles.SelectedValue == null)
            {
                MessageBox.Show("Заполните логин и выберите роль!");
                return;
            }

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query;
                if (selectedUserId == -1) // Новый пользователь
                {
                    query = "INSERT INTO Users (FIO, Login, Password, RoleID) VALUES (@fio, @log, @pass, @rid)";
                }
                else // Редактирование существующего
                {
                    query = "UPDATE Users SET FIO=@fio, Login=@log, Password=@pass, RoleID=@rid WHERE UserID=@id";
                }

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@fio", txtFIO.Text);
                cmd.Parameters.AddWithValue("@log", txtLogin.Text);
                // Вызываем наш метод хеширования перед сохранением в БД
                cmd.Parameters.AddWithValue("@pass", DiplomAPM.Helpers.PasswordHasher.HashPassword(pbPassword.Password));
                cmd.Parameters.AddWithValue("@rid", cbRoles.SelectedValue);
                if (selectedUserId != -1) cmd.Parameters.AddWithValue("@id", selectedUserId);

                cmd.ExecuteNonQuery();
            }
            LoadUsers();
            BtnClear_Click(null, null);
            MessageBox.Show("Данные сохранены.");
        }

        private void dgUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgUsers.SelectedItem is DataRowView row)
            {
                selectedUserId = (int)row["UserID"];
                txtFIO.Text = row["FIO"].ToString();
                txtLogin.Text = row["Login"].ToString();
                cbRoles.SelectedValue = row["RoleID"];
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUserId == -1) return;

            var res = MessageBox.Show("Удалить сотрудника?", "Внимание", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.Yes)
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("DELETE FROM Users WHERE UserID=@id", con);
                    cmd.Parameters.AddWithValue("@id", selectedUserId);
                    cmd.ExecuteNonQuery();
                }
                LoadUsers();
                BtnClear_Click(null, null);
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            selectedUserId = -1;
            txtFIO.Clear();
            txtLogin.Clear();
            pbPassword.Clear();
            cbRoles.SelectedIndex = -1;
        }
    }
}