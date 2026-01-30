using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace DiplomAPM
{
    public partial class RequestDetailsWindow : Window
    {
        // Используем ту же строку подключения, что и в остальных окнах
        string connectionString = @"Server=localhost;Database=DiplomAPM;Trusted_Connection=True;";
        int currentRequestId;

        public RequestDetailsWindow(int requestId)
        {
            InitializeComponent();
            currentRequestId = requestId;
            LoadRequestInfo();
            LoadHistory();
        }

        private void LoadRequestInfo()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"SELECT r.RequestID, cit.FIO, cat.CategoryName, r.Description 
                                     FROM Requests r
                                     JOIN Citizens cit ON r.CitizenID = cit.CitizenID
                                     JOIN Categories cat ON r.CategoryID = cat.CategoryID
                                     WHERE r.RequestID = @id";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@id", currentRequestId);
                    SqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        txtTitle.Text = $"Обращение №{dr["RequestID"]}";
                        txtApplicant.Text = $"Заявитель: {dr["FIO"]}";
                        txtCategory.Text = $"Категория: {dr["CategoryName"]}";
                        txtDescription.Text = $"Суть: {dr["Description"]}";
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void LoadHistory()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Собираем историю с именами сотрудников и названиями статусов
                    string query = @"SELECT h.ChangeDate, s.StatusName, u.FIO as EmployeeFIO, h.Comment
                                     FROM RequestHistory h
                                     JOIN Statuses s ON h.StatusID = s.StatusID
                                     JOIN Users u ON h.UserID = u.UserID
                                     WHERE h.RequestID = @id
                                     ORDER BY h.ChangeDate DESC";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@id", currentRequestId);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    lvHistory.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}