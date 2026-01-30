using System;
using System.Data;
using System.Data.SqlClient;
using System.IO; // Нужно для сохранения файла
using System.Text; // Нужно для кодировки
using System.Windows;

namespace DiplomAPM
{
    public partial class ReportsWindow : Window
    {
        string connectionString = @"Server=localhost;Database=DiplomAPM;Trusted_Connection=True;";

        public ReportsWindow()
        {
            InitializeComponent();

            // Ставим даты по умолчанию (Например, текущий месяц)
            dpStart.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1); // 1-е число месяца
            dpEnd.SelectedDate = DateTime.Now; // Сегодня
        }

        // 1. Формирование отчета
        private void BtnShow_Click(object sender, RoutedEventArgs e)
        {
            if (dpStart.SelectedDate == null || dpEnd.SelectedDate == null)
            {
                MessageBox.Show("Выберите период!");
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // ХИТРЫЙ ЗАПРОС: Считаем количество (COUNT) и группируем по Названию Категории
                    string query = @"
                        SELECT 
                            c.CategoryName AS [Категория], 
                            COUNT(r.RequestID) AS [Количество заявок]
                        FROM Requests r
                        JOIN Categories c ON r.CategoryID = c.CategoryID
                        WHERE r.DateCreated BETWEEN @start AND @end
                        GROUP BY c.CategoryName";

                    SqlCommand cmd = new SqlCommand(query, con);
                    // Добавляем время к дате конца, чтобы захватить весь последний день (23:59:59)
                    cmd.Parameters.AddWithValue("@start", dpStart.SelectedDate.Value);
                    cmd.Parameters.AddWithValue("@end", dpEnd.SelectedDate.Value.AddDays(1).AddSeconds(-1));

                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    dgStats.ItemsSource = dt.DefaultView;

                    if (dt.Rows.Count == 0) MessageBox.Show("За этот период данных нет.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        // 2. Экспорт в Excel (через CSV)
        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            // Берем данные из таблицы
            var data = dgStats.ItemsSource as DataView;
            if (data == null || data.Count == 0)
            {
                MessageBox.Show("Сначала сформируйте отчет (нажмите 'Показать статистику')!");
                return;
            }

            try
            {
                // Создаем строку для записи в файл
                StringBuilder sb = new StringBuilder();

                // 1. Пишем заголовки (Категория; Количество)
                sb.AppendLine("Категория;Количество заявок");

                // 2. Пишем строки данных
                foreach (DataRowView row in data)
                {
                    sb.AppendLine($"{row["Категория"]};{row["Количество заявок"]}");
                }

                // 3. Сохраняем файл
                string path = "Otchet_IAC.csv"; // Файл сохранится рядом с .exe программой

                // Используем кодировку UTF8, чтобы русский язык не ломался
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);

                // 4. Открываем этот файл сразу в Excel (или блокноте)
                var process = new System.Diagnostics.Process();
                process.StartInfo = new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true };
                process.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта: " + ex.Message);
            }
        }
    }
}