using System;
using System.Data.SqlClient; // Не забудь про SQL
using System.Windows;
using System.Windows.Input;

namespace DiplomAPM
{
    public partial class AddCitizenWindow : Window
    {
        // Проверь строку подключения!
        string connectionString = @"Server=localhost;Database=DiplomAPM;Trusted_Connection=True;";

        public AddCitizenWindow()
        {
            InitializeComponent();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Проверка: ФИО и Телефон обязательны
            if (string.IsNullOrWhiteSpace(txtFIO.Text) || string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                MessageBox.Show("Пожалуйста, укажите ФИО и Телефон!");
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Запрос на добавление
                    string query = "INSERT INTO Citizens (FIO, Phone, Email) VALUES (@fio, @phone, @email)";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@fio", txtFIO.Text);
                    cmd.Parameters.AddWithValue("@phone", txtPhone.Text);
                    // Если Email пустой, запишем туда просто пустую строку или NULL
                    cmd.Parameters.AddWithValue("@email", txtEmail.Text);

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Гражданин успешно добавлен!");
                this.DialogResult = true; // Сообщаем, что всё ок
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        // Чтобы окно можно было перетаскивать мышкой (т.к. мы убрали рамку)
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }
    }
}