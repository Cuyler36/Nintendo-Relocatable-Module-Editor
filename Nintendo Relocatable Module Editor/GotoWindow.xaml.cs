using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace Nintendo_Relocatable_Module_Editor
{
    /// <summary>
    /// Interaction logic for GotoWindow.xaml
    /// </summary>
    public partial class GotoWindow : Window
    {
        private MainWindow Main_Window_Reference;

        public GotoWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            Main_Window_Reference = mainWindow;
        }

        private void Goto_Click(object sender, RoutedEventArgs e)
        {
            if (uint.TryParse(GotoBox.Text, NumberStyles.AllowHexSpecifier, null, out uint Offset))
            {
                Main_Window_Reference.Goto(Offset);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
