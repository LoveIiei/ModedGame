using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ModedGame.ViewModels
{
    public class MCFileTypes
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class MCFileTypeViewModel : INotifyPropertyChanged
    {
        private List<MCFileTypes> _mcFileType;
        private MCFileTypes _selectedType;

        public List<MCFileTypes> MCFileType
        {
            get => _mcFileType;
            set
            {
                _mcFileType = value;
                OnPropertyChanged();
            }
        }

        public MCFileTypes SelectedType
        {
            get => _selectedType;
            set
            {
                _selectedType = value;
                OnPropertyChanged();
            }
        }

        public MCFileTypeViewModel()
        {
            MCFileType = new List<MCFileTypes>
        {
            new MCFileTypes { Id = 1, Name = "Mod"}, 
            new MCFileTypes { Id = 2, Name = "ResourcePack" },
            new MCFileTypes { Id = 3, Name = "Shaders" },
        };
            SelectedType = MCFileType[0]; // Select first item
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}