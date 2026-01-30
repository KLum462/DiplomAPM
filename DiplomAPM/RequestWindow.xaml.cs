using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;

namespace DiplomAPM // Убедись, что тут твое название проекта
{
    public partial class RequestWindow : Window
    {
        string connectionString = @"Server=localhost;Database=DiplomAPM;Trusted_Connection=True;";
        private int _requestId = 0;
        // Переменная для хранения ID редактируемой заявки (если null — значит новая)
        private int? _currentRequestId = null;

        // Конструктор 1: ДЛЯ СОЗДАНИЯ (пустой)
        public RequestWindow()
        {
            InitializeComponent();
            LoadFormData();
        }

        // Конструктор 2: ДЛЯ РЕДАКТИРОВАНИЯ (принимает ID)
        public RequestWindow(int id)
        {
            InitializeComponent();
            LoadFormData();

            _currentRequestId = id; // Запоминаем ID
            Title = "Редактирование заявки"; // Меняем заголовок окна
            LoadRequestData(id); // Загружаем данные этой заявки
        }

        private void LoadFormData()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Грузим граждан
                    SqlDataAdapter daCit = new SqlDataAdapter("SELECT CitizenID, FIO FROM Citizens", con);
                    DataTable dtCit = new DataTable();
                    daCit.Fill(dtCit);
                    cbCitizens.ItemsSource = dtCit.DefaultView;

                    // Грузим категории
                    SqlDataAdapter daCat = new SqlDataAdapter("SELECT CategoryID, CategoryName FROM Categories", con);
                    DataTable dtCat = new DataTable();
                    daCat.Fill(dtCat);
                    cbCategories.ItemsSource = dtCat.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки списков: " + ex.Message);
            }
        }

        // Метод загрузки данных конкретной заявки (для редактирования)
        private void LoadRequestData(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT CitizenID, CategoryID, Description FROM Requests WHERE RequestID = @id";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@id", id);

                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        // Подставляем данные в поля
                        cbCitizens.SelectedValue = reader["CitizenID"];
                        cbCategories.SelectedValue = reader["CategoryID"];
                        txtDescription.Text = reader["Description"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при чтении заявки: " + ex.Message);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (cbCitizens.SelectedValue == null || cbCategories.SelectedValue == null || string.IsNullOrWhiteSpace(txtDescription.Text))
            {
                MessageBox.Show("Заполните все поля!");
                return;
            }
            if (_requestId == 0)
                AuditLogger.Log("Создание", "Создана новая заявка");
            else
                AuditLogger.Log("Редактирование", $"Изменены данные заявки №{_requestId}");
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = con;

                    if (_currentRequestId == null)
                    {
                        // РЕЖИМ ДОБАВЛЕНИЯ (INSERT)
                        cmd.CommandText = @"INSERT INTO Requests (DateCreated, Description, CitizenID, CategoryID, StatusID) 
                                            VALUES (GETDATE(), @desc, @cit, @cat, 1)";
                    }
                    else
                    {
                        // РЕЖИМ ОБНОВЛЕНИЯ (UPDATE)
                        cmd.CommandText = @"UPDATE Requests SET 
                                            Description = @desc, 
                                            CitizenID = @cit, 
                                            CategoryID = @cat 
                                            WHERE RequestID = @id";
                        cmd.Parameters.AddWithValue("@id", _currentRequestId);
                    }

                    cmd.Parameters.AddWithValue("@desc", txtDescription.Text);
                    cmd.Parameters.AddWithValue("@cit", cbCitizens.SelectedValue);
                    cmd.Parameters.AddWithValue("@cat", cbCategories.SelectedValue);

                    cmd.ExecuteNonQuery();
                }

                this.DialogResult = true;
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

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }
    }
}