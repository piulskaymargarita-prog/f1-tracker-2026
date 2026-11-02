using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace F1Tracker.Views
{
    /// <summary>
    /// Логика взаимодействия для DataWindow.xaml
    /// </summary>
    public partial class DataWindow : Window
    {
        public string InitialTab { get; set; } = "teams";
        public string InitialFilter { get; set; } = "";

        public DataWindow()
        {
            InitializeComponent();
            Loaded += (_, _) => StubText.Text = $"Вкладка: {InitialTab} · Фильтр: «{InitialFilter}»";
        }
    }
}
