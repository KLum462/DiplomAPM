using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DiplomAPM.ViewModels
{
    // Базовый класс, который реализует интерфейс INotifyPropertyChanged
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        // Метод, который говорит интерфейсу: "Эй, свойство изменилось, обновись!"
        protected void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}