using DiplomAPM;
using System;
using System.Data;
using System.Data.SqlClient; 
using System.Windows;
using System.Windows.Input;
using Excel = Microsoft.Office.Interop.Excel;
using Word = Microsoft.Office.Interop.Word;
using System.IO;
using System.Configuration;
using System.Runtime.InteropServices;
namespace DiplomAPM
{
    public partial class DashboardWindow : Window
    {
        private static string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        private DataTable _dtRequests;
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
                    _dtRequests = new DataTable();
                    adapter.Fill(_dtRequests);
                    dgRequests.ItemsSource = _dtRequests.DefaultView;
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

            // Выносим переменные сюда, чтобы они были доступны в блоке finally
            Excel.Application excelApp = null;
            Excel.Workbook workbook = null;
            Excel.Worksheet worksheet = null;

            try
            {
                excelApp = new Excel.Application();
                excelApp.Visible = true;

                workbook = excelApp.Workbooks.Add();
                worksheet = (Excel.Worksheet)workbook.Sheets[1];

                DataTable dt = view.Table;

                // Заполняем заголовки
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    worksheet.Cells[1, i + 1] = dt.Columns[i].ColumnName;
                    worksheet.Cells[1, i + 1].Font.Bold = true; // Сделаем заголовки жирными для красоты
                }

                // Заполняем данные
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
            finally
            {
                // САМОЕ ВАЖНОЕ: Очистка памяти в обратном порядке (Лист -> Книга -> Приложение)
                // Блок finally выполнится ВСЕГДА, даже если произошла ошибка

                if (worksheet != null) Marshal.ReleaseComObject(worksheet);
                if (workbook != null) Marshal.ReleaseComObject(workbook);
                if (excelApp != null) Marshal.ReleaseComObject(excelApp);

                // Принудительно вызываем сборщик мусора
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            // 1. Проверяем, выбрана ли заявка
            if (dgRequests.SelectedItem is DataRowView row)
            {
                int requestId = (int)row["Номер"];

                // Получаем полные данные из БД (этот код оставляем как был)
                DataTable dtInfo = new DataTable();
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
        SELECT 
            r.RequestID, r.DateCreated, cit.FIO, cit.Phone, 
            cat.CategoryName, s.StatusName, r.Description,
            ISNULL(u.FIO, 'Не назначен') as Manager
        FROM Requests r
        JOIN Citizens cit ON r.CitizenID = cit.CitizenID
        JOIN Categories cat ON r.CategoryID = cat.CategoryID
        JOIN Statuses s ON r.StatusID = s.StatusID
        LEFT JOIN Users u ON r.UserID = u.UserID
        WHERE r.RequestID = @id";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@id", requestId);

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dtInfo);
                }

                if (dtInfo.Rows.Count == 0) return;
                DataRow r = dtInfo.Rows[0];

                // ВАЖНО: Выносим переменные COM-объектов ДО блока try
                Word.Application wordApp = null;
                Word.Document doc = null;
                Word.Paragraph title = null;
                Word.Paragraph datePar = null;
                Word.Paragraph descHeader = null;
                Word.Paragraph descBody = null;
                Word.Paragraph sign = null;

                try
                {
                    wordApp = new Word.Application();
                    wordApp.Visible = true;

                    doc = wordApp.Documents.Add();

                    // Заголовок
                    title = doc.Paragraphs.Add();
                    title.Range.Text = $"КАРТОЧКА ОБРАЩЕНИЯ №{r["RequestID"]}";
                    title.Range.Font.Bold = 1;
                    title.Range.Font.Size = 16;
                    title.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    title.Range.InsertParagraphAfter();

                    // Дата формирования
                    datePar = doc.Paragraphs.Add();
                    datePar.Range.Text = $"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}";
                    datePar.Range.Font.Size = 10;
                    datePar.Range.Font.Italic = 1;
                    datePar.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphRight;
                    datePar.Range.InsertParagraphAfter();

                    doc.Paragraphs.Add();

                    AddLine(doc, "Дата регистрации:", r["DateCreated"].ToString());
                    AddLine(doc, "Статус:", r["StatusName"].ToString());
                    AddLine(doc, "Ответственный:", r["Manager"].ToString());

                    doc.Paragraphs.Add();

                    AddLine(doc, "Заявитель:", r["FIO"].ToString());
                    AddLine(doc, "Телефон:", r["Phone"].ToString());

                    doc.Paragraphs.Add();

                    AddLine(doc, "Категория проблемы:", r["CategoryName"].ToString());

                    // Описание проблемы 
                    descHeader = doc.Paragraphs.Add();
                    descHeader.Range.Text = "Суть обращения:";
                    descHeader.Range.Font.Bold = 1;
                    descHeader.Range.InsertParagraphAfter();

                    descBody = doc.Paragraphs.Add();
                    descBody.Range.Text = r["Description"].ToString();
                    descBody.Range.InsertParagraphAfter();

                    doc.Paragraphs.Add();
                    doc.Paragraphs.Add();

                    // Подпись
                    sign = doc.Paragraphs.Add();
                    sign.Range.Text = "________________________ / ________________________";
                    sign.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphRight;
                    sign.Range.InsertParagraphAfter();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при экспорте в Word: " + ex.Message);
                }
                finally
                {
                    // САМОЕ ВАЖНОЕ: Очищаем память в обратном порядке (от абзацев к самому Ворду)
                    if (sign != null) Marshal.ReleaseComObject(sign);
                    if (descBody != null) Marshal.ReleaseComObject(descBody);
                    if (descHeader != null) Marshal.ReleaseComObject(descHeader);
                    if (datePar != null) Marshal.ReleaseComObject(datePar);
                    if (title != null) Marshal.ReleaseComObject(title);

                    if (doc != null) Marshal.ReleaseComObject(doc);
                    if (wordApp != null) Marshal.ReleaseComObject(wordApp);

                    // Принудительно вызываем сборщик мусора
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
            else
            {
                MessageBox.Show("Выберите обращение для печати.");
            }
        }

        // Вспомогательный метод для добавления жирного заголовка и обычного текста
        private void AddLine(Word.Document doc, string title, string value)
        {
            Word.Paragraph p = doc.Paragraphs.Add();
            // Хитрость: пишем всё вместе, потом делаем первые N символов жирными
            p.Range.Text = $"{title} {value}";

            // Делаем заголовок жирным
            object start = p.Range.Start;
            object end = p.Range.Start + title.Length;
            Word.Range boldRange = doc.Range(ref start, ref end);
            boldRange.Font.Bold = 1;

            p.Range.InsertParagraphAfter();
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
        // Метод загружает список категорий в ComboBox фильтра
        private void LoadCategoriesForFilter()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlDataAdapter da = new SqlDataAdapter("SELECT CategoryName FROM Categories", con);
                    DataTable dtCat = new DataTable();
                    da.Fill(dtCat);

                    // Добавляем пустую строку "Все категории", чтобы можно было не выбирать конкретную
                    DataRow emptyRow = dtCat.NewRow();
                    emptyRow["CategoryName"] = "Все категории";
                    dtCat.Rows.InsertAt(emptyRow, 0);

                    cmbFilterCategory.ItemsSource = dtCat.DefaultView;
                    cmbFilterCategory.SelectedIndex = 0; // По умолчанию выбрано "Все категории"
                }
            }
            catch (Exception ex) { /* игнорируем ошибку при инициализации */ }
        }

        // Кнопка НАЙТИ (Применение фильтров)
        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            if (_dtRequests == null) return;

            // Создаем список условий
            System.Collections.Generic.List<string> filters = new System.Collections.Generic.List<string>();

            // 1. Поиск по ФИО (ищем совпадение куска текста)
            if (!string.IsNullOrWhiteSpace(txtFilterFio.Text))
            {
                filters.Add($"[Заявитель] LIKE '%{txtFilterFio.Text.Trim()}%'");
            }

            // 2. Поиск по категории (если не выбрана "Все категории")
            if (cmbFilterCategory.SelectedIndex > 0)
            {
                // Берем текст выбранной категории
                string cat = ((DataRowView)cmbFilterCategory.SelectedItem)["CategoryName"].ToString();
                filters.Add($"[Категория] = '{cat}'");
            }

            // 3. Поиск по дате ОТ
            if (dpFilterFrom.SelectedDate.HasValue)
            {
                // Специальный формат даты для RowFilter
                string dateFrom = dpFilterFrom.SelectedDate.Value.ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                filters.Add($"[Дата] >= #{dateFrom}#");
            }

            // 4. Поиск по дате ДО
            if (dpFilterTo.SelectedDate.HasValue)
            {
                string dateTo = dpFilterTo.SelectedDate.Value.ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                filters.Add($"[Дата] <= #{dateTo} 23:59:59#"); // До конца дня
            }

            // Склеиваем все условия через И (AND) и применяем к таблице
            string finalFilter = string.Join(" AND ", filters);
            _dtRequests.DefaultView.RowFilter = finalFilter;
        }

        // Кнопка СБРОСИТЬ
        private void BtnResetFilter_Click(object sender, RoutedEventArgs e)
        {
            // Очищаем визуальные поля
            txtFilterFio.Clear();
            cmbFilterCategory.SelectedIndex = 0;
            dpFilterFrom.SelectedDate = null;
            dpFilterTo.SelectedDate = null;

            // Снимаем фильтр с таблицы (показываем все записи)
            if (_dtRequests != null)
            {
                _dtRequests.DefaultView.RowFilter = string.Empty;
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