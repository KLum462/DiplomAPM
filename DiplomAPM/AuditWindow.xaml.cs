using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace DiplomAPM
{
    public partial class AuditWindow : Window
    {
        string connectionString = @"Server=localhost;Database=DiplomAPM;Trusted_Connection=True;";
        DataTable dtLogs = new DataTable(); // Храним данные для фильтрации

        public AuditWindow()
        {
            InitializeComponent();
            LoadLogs();
        }

        private void LoadLogs()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // JOIN нужен, чтобы получить имя пользователя вместо его ID
                    string query = @"
                        SELECT 
                            a.LogID, 
                            a.ActionDate, 
                            a.ActionType, 
                            a.Description,
                            ISNULL(u.FIO, 'Система/Неизвестно') as EmployeeFIO
                        FROM AuditLogs a
                        LEFT JOIN Users u ON a.UserID = u.UserID
                        ORDER BY a.ActionDate DESC";

                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    dtLogs = new DataTable();
                    da.Fill(dtLogs);
                    dgAudit.ItemsSource = dtLogs.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки логов: " + ex.Message);
            }
        }

        // "Живой" поиск и фильтрация без повторных запросов к БД
        private void Filter_Changed(object sender, EventArgs e)
        {
            if (dtLogs.Rows.Count == 0) return;

            string typeFilter = (cbActionType.SelectedItem as ComboBoxItem)?.Content.ToString();
            string searchFilter = txtSearch.Text.ToLower();

            string filterExpression = "";

            // Фильтр по типу
            if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "Все")
            {
                filterExpression += $"ActionType = '{typeFilter}'";
            }

            // Фильтр по тексту
            if (!string.IsNullOrEmpty(searchFilter))
            {
                if (filterExpression.Length > 0) filterExpression += " AND ";
                filterExpression += $"(Description LIKE '%{searchFilter}%' OR EmployeeFIO LIKE '%{searchFilter}%')";
            }

            dtLogs.DefaultView.RowFilter = filterExpression;
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            cbActionType.SelectedIndex = 0;
            dtLogs.DefaultView.RowFilter = "";
            LoadLogs(); // Обновить данные из БД
        }
    }
}