using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace DiplomAPM
{
    public partial class CitizensWindow : Window
    {
        string connectionString = @"Server=localhost;Database=DiplomAPM;Trusted_Connection=True;";

        public CitizensWindow()
        {
            InitializeComponent();
            LoadCitizens(); // Грузим при старте
        }

        private void LoadCitizens()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Простой запрос: берем все поля из таблицы граждан
                    string query = "SELECT CitizenID AS [ID], FIO AS [ФИО], Phone AS [Телефон], Email AS [Почта] FROM Citizens";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    dgCitizens.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message);
            }
        }


        private void BtnAddCitizen_Click(object sender, RoutedEventArgs e)
        {
         
            AddCitizenWindow addWin = new AddCitizenWindow();
            bool? result = addWin.ShowDialog(); 

  
            if (result == true)
            {
                LoadCitizens(); 
            }
        }
    }
}