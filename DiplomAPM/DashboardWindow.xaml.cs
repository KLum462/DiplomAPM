using DiplomAPM;
using System;
using System.Data;
using System.Data.SqlClient; 
using System.Windows;
using System.Windows.Input;
using Excel = Microsoft.Office.Interop.Excel;

namespace DiplomAPM
{
    public partial class DashboardWindow : Window
    {
        // Твоя строка подключения (проверь, чтобы совпадала с той, что в окне авторизации)
        string connectionString = @"Server=localhost;Database=DiplomAPM;Trusted_Connection=True;";

        public DashboardWindow()
        {
            InitializeComponent();
            LoadData(); // Загружаем данные сразу при старте окна
        }

        // Метод загрузки данных в таблицу
        private void LoadData()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
            
                    string query = @"
                        SELECT 
                            r.RequestID AS [Номер],
                            r.DateCreated AS [Дата],
                            cat.CategoryName AS [Категория],
                            cit.FIO AS [Заявитель],
                            r.Description AS [Описание проблемы],
                            s.StatusName AS [Статус],
                            u.FIO AS [Ответственный]
                        FROM Requests r
                        JOIN Citizens cit ON r.CitizenID = cit.CitizenID
                        JOIN Categories cat ON r.CategoryID = cat.CategoryID
                        JOIN Statuses s ON r.StatusID = s.StatusID
                        LEFT JOIN Users u ON r.UserID = u.UserID";
                    // LEFT JOIN используется для сотрудника, т.к. сотрудник может быть еще не назначен (NULL)

                    SqlCommand cmd = new SqlCommand(query, con);
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    dgRequests.ItemsSource = dt.DefaultView; // Привязываем данные к DataGrid
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке данных: " + ex.Message);
            }
        }
        private void BtnDetails_Click(object sender, RoutedEventArgs e)
        {
      
            if (dgRequests.SelectedItem is DataRowView row)
            {

                int id = (int)row["Номер"];


                RequestDetailsWindow detailsWin = new RequestDetailsWindow(id);


                this.Opacity = 0.5;


                detailsWin.ShowDialog();


                this.Opacity = 1;
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите обращение из списка для просмотра деталей.");
            }
        }
        private void dgRequests_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            BtnDetails_Click(sender, e); // Вызываем тот же метод, что и при клике на кнопку
        }

        private void BtnReferences_Click(object sender, RoutedEventArgs e)
        {
            ReferenceWindow refWin = new ReferenceWindow();
            refWin.ShowDialog(); // Открываем как модальное окно
        }
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            this.Opacity = 0.5;


            RequestWindow addForm = new RequestWindow();


            bool? result = addForm.ShowDialog();


            this.Opacity = 1;

      
            if (result == true)
            {
                LoadData(); 
                MessageBox.Show("Заявка успешно добавлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void BtnCitizens_Click(object srender, RoutedEventArgs e)
        {
            CitizensWindow citWin = new CitizensWindow();
            citWin.Show();
        }
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
     
            if (dgRequests.SelectedItem is DataRowView row)
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить эту запись?", "Удаление", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
           
                        int id = (int)row["Номер"];

                        using (SqlConnection con = new SqlConnection(connectionString))
                        {
                            con.Open();
                            SqlCommand cmd = new SqlCommand("DELETE FROM Requests WHERE RequestID = @id", con);
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                            AuditLogger.Log("Удаление", $"Удалена заявка №{id}.");
                        }
                        LoadData(); // Обновляем таблицу
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка удаления: " + ex.Message);
                    }
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите строку для удаления.");
            }
        }
        // Кнопка ЭКСПОРТ В EXCEL
        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
           
            DataView view = dgRequests.ItemsSource as DataView;

            if (view == null || view.Table.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
        
                Excel.Application excelApp = new Excel.Application();
                excelApp.Visible = true; 

    
                Excel.Workbook workbook = excelApp.Workbooks.Add();
                Excel.Worksheet worksheet = (Excel.Worksheet)workbook.Sheets[1];


                DataTable dt = view.Table;
                for (int i = 0; i < dt.Columns.Count; i++)
                {
     
                    worksheet.Cells[1, i + 1] = dt.Columns[i].ColumnName;

              
                    worksheet.Cells[1, i + 1] = true;
                }

        
                for (int r = 0; r < dt.Rows.Count; r++)
                {
                    for (int c = 0; c < dt.Columns.Count; c++)
                    {
                     
                        worksheet.Cells[r + 2, c + 1] = dt.Rows[r][c].ToString();
                    }
                }

   
                worksheet.Columns.AutoFit(); 
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при экспорте: " + ex.Message);
            }
        }
        private void BtnReports_Click(object sender, EventArgs e)
        {
            ReportsWindow repWin = new ReportsWindow();
            repWin.Show();
        }
        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow setWin = new SettingsWindow();
            setWin.ShowDialog(); // ShowDialog блокирует главное окно, пока настройки не закроются
        }
        private void BtnAdmin_Click(object sender, RoutedEventArgs e)
        {
            AdminWindow adminWin = new AdminWindow();
            adminWin.ShowDialog();
        }
        private void BtnAudit_Click(object sender, RoutedEventArgs e)
        {
            AuditWindow audit = new AuditWindow();
            audit.Show();
        }
        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
   
            if (dgRequests.SelectedItem is DataRowView row)
            {
       
                int id = (int)row["Номер"];

                this.Opacity = 0.5;

 
                RequestWindow editWin = new RequestWindow(id);

                bool? result = editWin.ShowDialog();

                this.Opacity = 1;

                if (result == true)
                {
                    LoadData(); // Обновляем таблицу
                    MessageBox.Show("Данные успешно обновлены!");
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите строку для редактирования.");
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
     
            MainWindow login = new MainWindow();
            login.Show();
            this.Close();
        }
    }
}