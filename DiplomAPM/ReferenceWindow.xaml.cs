using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Configuration; // Не забудьте добавить using
namespace DiplomAPM
{
    public partial class ReferenceWindow : Window
    {
        private static string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        int selectedId = -1;

        public ReferenceWindow()
        {
            InitializeComponent();
        }

        // Метод для получения имени текущей таблицы из Tag выбранной вкладки
        private string GetCurrentTable()
        {
            return (tcReferences.SelectedItem as TabItem)?.Tag.ToString();
        }

        private void LoadData()
        {
            string table = GetCurrentTable();
            if (string.IsNullOrEmpty(table)) return;

            // Используем алиасы ID и Name, чтобы DataGrid подхватывал их автоматически для любой таблицы
            string idCol = table == "Categories" ? "CategoryID" : table == "Statuses" ? "StatusID" : "DepartmentID";
            string nameCol = table == "Categories" ? "CategoryName" : table == "Statuses" ? "StatusName" : "DepartmentName";

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = $"SELECT {idCol} as ID, {nameCol} as Name FROM {table}";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgData.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void tcReferences_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BtnNew_Click(null, null); // Сбрасываем поля при переключении вкладок
            LoadData();
        }

        private void dgData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgData.SelectedItem is DataRowView row)
            {
                selectedId = (int)row["ID"];
                txtValue.Text = row["Name"].ToString();
            }
        }

        // КНОПКА "НОВЫЙ" — Сбрасывает ID, чтобы Save сработал как INSERT
        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            selectedId = -1;
            txtValue.Clear();
            dgData.SelectedItem = null;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtValue.Text)) return;

            string table = GetCurrentTable();
            string idCol = table == "Categories" ? "CategoryID" : table == "Statuses" ? "StatusID" : "DepartmentID";
            string nameCol = table == "Categories" ? "CategoryName" : table == "Statuses" ? "StatusName" : "DepartmentName";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = (selectedId == -1)
                    ? $"INSERT INTO {table} ({nameCol}) VALUES (@val)"
                    : $"UPDATE {table} SET {nameCol} = @val WHERE {idCol} = @id";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@val", txtValue.Text.Trim());
                if (selectedId != -1) cmd.Parameters.AddWithValue("@id", selectedId);

                cmd.ExecuteNonQuery();
            }
            LoadData();
            BtnNew_Click(null, null);
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            // 1. Проверяем, выбрана ли запись
            if (selectedId == -1)
            {
                MessageBox.Show("Пожалуйста, выберите запись для удаления.");
                return;
            }

            string table = GetCurrentTable();
            // Определяем имя столбца ID для текущей таблицы
            string idCol = table == "Categories" ? "CategoryID" : table == "Statuses" ? "StatusID" : "DepartmentID";

            var result = MessageBox.Show($"Вы уверены, что хотите удалить эту запись из справочника '{table}'?",
                                         "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();

                        // 2. ПРОВЕРКА НА ИСПОЛЬЗОВАНИЕ: проверяем, нет ли заявок с этим ID
                        // В таблице Requests названия столбцов совпадают с нашими справочниками
                        string checkQuery = $"SELECT COUNT(*) FROM Requests WHERE {idCol} = @id";
                        SqlCommand checkCmd = new SqlCommand(checkQuery, con);
                        checkCmd.Parameters.AddWithValue("@id", selectedId);

                        int count = (int)checkCmd.ExecuteScalar();

                        if (count > 0)
                        {
                            MessageBox.Show($"Невозможно удалить запись, так как она используется в обращениях (найдено: {count}). " +
                                            "Сначала измените или удалите соответствующие обращения.", "Ошибка удаления",
                                            MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        // 3. УДАЛЕНИЕ: если проверок нет, удаляем
                        string deleteQuery = $"DELETE FROM {table} WHERE {idCol} = @id";
                        SqlCommand cmd = new SqlCommand(deleteQuery, con);
                        cmd.Parameters.AddWithValue("@id", selectedId);

                        cmd.ExecuteNonQuery();
                    }

                    // 4. Обновляем интерфейс
                    LoadData();
                    BtnNew_Click(null, null); // Очищаем поля ввода
                    MessageBox.Show("Запись успешно удалена.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении из базы данных: " + ex.Message);
                }
            }
        }
    }
}