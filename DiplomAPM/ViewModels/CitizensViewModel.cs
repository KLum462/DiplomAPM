using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.ComponentModel;
using DiplomAPM.Models;

namespace DiplomAPM.ViewModels
{
    public class CitizensViewModel : ViewModelBase
    {
        private string connectionString;

        public ObservableCollection<Citizen> CitizensList { get; set; }
        public RelayCommand LoadDataCommand { get; set; }

        // НОВОЕ 1: Объявляем команду для кнопки добавления
        public RelayCommand AddCitizenCommand { get; set; }

        public CitizensViewModel()
        {
            CitizensList = new ObservableCollection<Citizen>();
            LoadDataCommand = new RelayCommand(obj => LoadCitizens());

            // НОВОЕ 2: Говорим, что при вызове команды нужно выполнить метод OpenAddCitizenWindow
            AddCitizenCommand = new RelayCommand(obj => OpenAddCitizenWindow());

            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                return;
            }

            connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            LoadCitizens();
        }

        // НОВОЕ 3: Сам метод открытия окна
        private void OpenAddCitizenWindow()
        {
            AddCitizenWindow addWin = new AddCitizenWindow();
            bool? result = addWin.ShowDialog();

            // Если окно вернуло true (гражданин успешно добавлен), обновляем таблицу
            if (result == true)
            {
                LoadCitizens();
            }
        }

        private void LoadCitizens()
        {
            CitizensList.Clear();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT CitizenID, FIO, Phone, Email FROM Citizens";
                    SqlCommand cmd = new SqlCommand(query, con);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            CitizensList.Add(new Citizen
                            {
                                ID = reader.GetInt32(0),
                                FIO = reader.GetString(1),
                                Phone = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                Email = reader.IsDBNull(3) ? "" : reader.GetString(3)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message);
            }
        }
    }
}