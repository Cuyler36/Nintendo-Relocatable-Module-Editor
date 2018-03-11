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

namespace Nintendo_Relocatable_Module_Editor
{
    /// <summary>
    /// Interaction logic for SearchWindow.xaml
    /// </summary>
    public partial class SearchWindow : Window
    {
        private MainWindow Main_Window_Reference;

        public SearchWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            Main_Window_Reference = mainWindow;
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SearchBox.Text))
            {
                Main_Window_Reference.Search_Field_TreeViewItems(SearchBox.Text, false);
            }
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            Main_Window_Reference.Search_Field_TreeViewItems(SearchBox.Text, true);
        }
    }
}
