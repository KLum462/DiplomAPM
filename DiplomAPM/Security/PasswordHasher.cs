using System;
using System.Security.Cryptography;
using System.Text;

namespace DiplomAPM.Helpers // Убедись, что тут твое пространство имен
{
    public static class PasswordHasher
    {
        // Метод, который превращает обычный пароль в защищенный хеш
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return string.Empty;

            using (SHA256 sha256 = SHA256.Create())
            {
                // Переводим строку пароля в массив байтов
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Преобразуем массив байтов обратно в строку (шестнадцатеричный формат)
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                // Возвращаем зашифрованную строку
                return builder.ToString();
            }
        }
    }
}