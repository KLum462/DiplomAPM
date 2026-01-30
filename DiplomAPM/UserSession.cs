using System;
using System.Collections.Generic;
using System.Text;

namespace DiplomAPM
{
   
        public static class UserSession
        {
            // Храним ID и ФИО текущего сотрудника
            public static int UserId { get; set; }
            public static string FIO { get; set; }
        }
    
}
