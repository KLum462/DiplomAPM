using System;
using System.Data.SqlClient;
using System.Windows;

namespace DiplomAPM
{
    public static class AuditLogger
    {
        // Строка подключения (такая же, как в других окнах)
        private static string connectionString = @"Server=localhost;Database=DiplomAPM;Trusted_Connection=True;";

        public static void Log(string actionType, string description)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "INSERT INTO AuditLogs (UserID, ActionDate, ActionType, Description) VALUES (@uid, GETDATE(), @type, @desc)";

                    SqlCommand cmd = new SqlCommand(query, con);

                    // Если пользователь авторизован, пишем его ID. Если нет (этап логина) — пишем NULL (DBNull.Value)
                    if (UserSession.UserId != 0)
                        cmd.Parameters.AddWithValue("@uid", UserSession.UserId);
                    else
                        cmd.Parameters.AddWithValue("@uid", DBNull.Value);

                    cmd.Parameters.AddWithValue("@type", actionType);
                    cmd.Parameters.AddWithValue("@desc", description);

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                // Логирование не должно ломать программу, поэтому просто выводим в консоль или игнорируем
                System.Diagnostics.Debug.WriteLine("Ошибка аудита: " + ex.Message);
            }
        }
    }
}