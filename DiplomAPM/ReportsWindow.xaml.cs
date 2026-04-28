using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media; // Для цветов
using System.Windows.Shapes; // Для прямоугольников (Rectangle)
using System.Configuration; // Не забудьте добавить using
namespace DiplomAPM
{
    public partial class ReportsWindow : Window
    {
        private static string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        public ReportsWindow()
        {
            InitializeComponent();
            dpStart.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dpEnd.SelectedDate = DateTime.Now;
        }

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
                    // Запрос: Группировка по категориям
                    string query = @"
                        SELECT 
                            c.CategoryName AS [Категория], 
                            COUNT(r.RequestID) AS [Количество]
                        FROM Requests r
                        JOIN Categories c ON r.CategoryID = c.CategoryID
                        WHERE r.DateCreated BETWEEN @start AND @end
                        GROUP BY c.CategoryName
                        ORDER BY COUNT(r.RequestID) DESC"; // Сортируем от большего к меньшему

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@start", dpStart.SelectedDate.Value);
                    cmd.Parameters.AddWithValue("@end", dpEnd.SelectedDate.Value.AddDays(1).AddSeconds(-1));

                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    // 1. Заполняем таблицу
                    dgStats.ItemsSource = dt.DefaultView;

                    // 2. Рисуем график
                    DrawChart(dt);

                    if (dt.Rows.Count == 0) MessageBox.Show("За этот период данных нет.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void DrawChart(DataTable dt)
        {
            chartContainer.Children.Clear(); // Очищаем старый график

            if (dt.Rows.Count == 0)
            {
                txtNoData.Visibility = Visibility.Visible;
                return;
            }
            txtNoData.Visibility = Visibility.Collapsed;

            // Находим максимальное значение для масштабирования
            double maxVal = 0;
            foreach (DataRow row in dt.Rows)
            {
                double val = Convert.ToDouble(row["Количество"]);
                if (val > maxVal) maxVal = val;
            }

            // Цвета для столбиков
            Brush[] colors = { Brushes.DodgerBlue, Brushes.Orange, Brushes.MediumSeaGreen, Brushes.Tomato, Brushes.SlateBlue };
            int colorIndex = 0;

            // Строим столбики
            foreach (DataRow row in dt.Rows)
            {
                double val = Convert.ToDouble(row["Количество"]);
                string name = row["Категория"].ToString();

                // Вычисляем высоту столбика (максимум 200 пикселей)
                double barHeight = (val / maxVal) * 200;
                if (barHeight < 5) barHeight = 5; // Минимальная высота

                // Создаем вертикальную панель для одного столбика
                StackPanel colPanel = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(15, 0, 15, 0),
                    Width = 80
                };

                // 1. Текст значения (сверху)
                TextBlock txtVal = new TextBlock
                {
                    Text = val.ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 5)
                };

                // 2. Сам столбик
                Border bar = new Border
                {
                    Height = barHeight,
                    Background = colors[colorIndex % colors.Length],
                    CornerRadius = new CornerRadius(5, 5, 0, 0),
                    ToolTip = $"{name}: {val} заявок" // Всплывающая подсказка
                };

                // 3. Название категории (снизу)
                TextBlock txtName = new TextBlock
                {
                    Text = name,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    FontSize = 10,
                    Margin = new Thickness(0, 5, 0, 0),
                    Height = 40 // Фиксируем высоту текста, чтобы столбики стояли ровно
                };

                // Собираем всё вместе
                colPanel.Children.Add(txtVal);
                colPanel.Children.Add(bar);
                colPanel.Children.Add(txtName);

                // Добавляем в контейнер графика
                chartContainer.Children.Add(colPanel);

                colorIndex++;
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var data = dgStats.ItemsSource as DataView;
            if (data == null || data.Count == 0)
            {
                MessageBox.Show("Сначала сформируйте отчет!");
                return;
            }

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Категория;Количество");

                foreach (DataRowView row in data)
                {
                    sb.AppendLine($"{row["Категория"]};{row["Количество"]}");
                }

                string path = "Otchet_Analytics.csv";
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);

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