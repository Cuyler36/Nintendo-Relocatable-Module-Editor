using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Threading;

namespace Nintendo_Relocatable_Module_Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SearchWindow Search_Window;
        private GotoWindow Goto_Window;
        private System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
        private Map_Info[] Info;
        private List<TreeViewItem> Field_TreeViewItems;
        private byte[] File_Buffer;
        private string Display_Type_Selected = "Byte";
        private REL Module;
        public int Selected_Index = 0;

        public MainWindow()
        {
            InitializeComponent();
            Search_Window = new SearchWindow(this);
            ShortcutCommands.InitializeShortcuts();
            CommandBindings.Add(new CommandBinding(ShortcutCommands.Search_Command, Search));
            CommandBindings.Add(new CommandBinding(ShortcutCommands.Goto_Command, Show_Goto));
        }

        private void Display_Content()
        {
            dataContent.Text = "";
            string New_Text = "";
            var Selected_Info = Info[Selected_Index];
            if (Selected_Info == null)
                return;

            switch (Display_Type_Selected)
            {
                case "Byte":
                    for (int i = 0; i < Selected_Info.Data.Length; i++)
                    {
                        if (i > 0 && i % 8 == 0)
                            New_Text += "\n";

                        New_Text += Selected_Info.Data[i].ToString("X2") + " ";
                    }
                    break;
                case "Short":
                    for (int i = 0; i < Selected_Info.Data.Length; i += 2)
                    {
                        if (i > 0 && i % 8 == 0)
                            New_Text += "\n";

                        if (i + 1 >= Selected_Info.Data.Length)
                            New_Text += Selected_Info.Data[i].ToString("X2");
                        else
                            New_Text += Selected_Info.Data[i].ToString("X2") + Selected_Info.Data[i + 1].ToString("X2") + " ";
                    }
                    break;
                case "Int":
                    for (int i = 0; i < Selected_Info.Data.Length; i += 4)
                    {
                        if (i > 0 && i % 8 == 0)
                            New_Text += "\n";

                        New_Text += Selected_Info.Data[i].ToString("X2") + (i + 1 >= Selected_Info.Data.Length ? "" : Selected_Info.Data[i + 1].ToString("X2"))
                            + (i + 2 >= Selected_Info.Data.Length ? "" : Selected_Info.Data[i + 2].ToString("X2"))
                            + (i + 3 >= Selected_Info.Data.Length ? "" : Selected_Info.Data[i + 3].ToString("X2")) + " ";
                    }
                    break;
                case "Float":
                    for (int i = 0; i < Selected_Info.Data.Length; i += 4)
                    {
                        if (i > 0 && i % 8 == 0)
                            New_Text += "\n";

                        var Data = Selected_Info.Data.Skip(i).Take(i + 3 >= Selected_Info.Data.Length ? Selected_Info.Data.Length - i : 4).ToArray();
                        Array.Resize(ref Data, 4);
                        Data = Data.Reverse().ToArray();
                        var Float = BitConverter.ToSingle(Data, 0);
                        New_Text += Float.ToString() + " ";
                    }
                    break;
                case "Text":
                    New_Text = Encoding.ASCII.GetString(Selected_Info.Data);
                    break;
                case "Animal Crossing Character Set": // TODO: Switch to a file loading character set
                    for (int i = 0; i < Selected_Info.Data.Length; i++)
                    {
                        if (CharacterSets.Animal_Crossing_Character_Map.ContainsKey(Selected_Info.Data[i]))
                        {
                            New_Text += CharacterSets.Animal_Crossing_Character_Map[Selected_Info.Data[i]];
                        }
                        else
                        {
                            New_Text = Encoding.ASCII.GetString(Selected_Info.Data);
                        }
                    }
                    break;
                case "Dōbutsu no Mori Character Set":
                    for (int i = 0; i < Selected_Info.Data.Length; i++)
                    {
                        if (CharacterSets.Doubutsu_no_Mori_Plus_Character_Map.ContainsKey(Selected_Info.Data[i]))
                        {
                            New_Text += CharacterSets.Doubutsu_no_Mori_Plus_Character_Map[Selected_Info.Data[i]];
                        }
                        else
                        {
                            New_Text = Encoding.ASCII.GetString(Selected_Info.Data);
                        }
                    }
                    break;
            }

            dataContent.Text = New_Text;
        }

        private void DataType_Button_Checked(object sender, RoutedEventArgs e)
        {
            var Radio_Button = sender as RadioButton;
            if (Radio_Button != null && File_Buffer != null)
            {
                Display_Type_Selected = (string)Radio_Button.Content;
                Display_Content();
            }
        }

        private void On_Closed(object sender, EventArgs e)
        {
            try
            {
                Search_Window.Close();
            }
            catch { }
        }

        public void Goto(uint Offset)
        {
            //HexEditor.SetPosition(Offset, 0);
            for (int i = 0; i < Info.Length; i++)
            {
                Map_Info Current_Info = Info[i];
                uint End = Current_Info.Offset + Current_Info.Size;

                if (Offset >= Current_Info.Offset && Offset <= End)
                {
                    var Old_Stream = HexEditor.Stream;

                    Selected_Index = i;
                    currentlyEditing.Text = @"Currently Editing: [" + Current_Info.Section_Name + "] - " + Current_Info.Parent_Name + @"/" + Current_Info.File_Name
                        + " - File Offset: 0x" + Current_Info.Offset.ToString("X");
                    if (Current_Info.Data == null)
                        Current_Info.Data = File_Buffer.Skip((int)Current_Info.Offset).Take((int)Current_Info.Size).ToArray();

                    HexEditor.Stream = new MemoryStream(Current_Info.Data); // "new MemoryStream(Data)" is probably a memory leak
                    HexEditor.SetPosition(Offset - Current_Info.Offset, 0);
                    Display_Content();

                    if (Old_Stream != null)
                        Old_Stream.Dispose();
                    return;
                }
            }
        }

        private void Show_Goto(object sender, RoutedEventArgs e)
        {
            if (Goto_Window == null || !Goto_Window.IsLoaded)
                Goto_Window = new GotoWindow(this);

            Goto_Window.Show();
        }

        private bool ContainsTextCaseInsensitive(string Text, string Match)
        {
            return Text.IndexOf(Match, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        public void Search_Field_TreeViewItems(string Text, bool Previous)
        {
            if (Previous)
            {
                for (int i = Selected_Index - 1; i >= 0; i--)
                {
                    if (ContainsTextCaseInsensitive((string)Field_TreeViewItems[i].Header, Text))
                    {
                        (Field_TreeViewItems[i].Parent as TreeViewItem).IsExpanded = true;
                        Field_TreeViewItems[i].IsExpanded = true;
                        Field_TreeViewItems[i].IsSelected = true;
                        Field_TreeViewItems[i].BringIntoView();
                        RelView_Item_Clicked(i);
                        return;
                    }
                }
            }
            else
            {
                for (int i = Selected_Index + 1; i < Field_TreeViewItems.Count; i++) // + 1 to skip the current index
                {
                    if (ContainsTextCaseInsensitive((string)Field_TreeViewItems[i].Header, Text))
                    {
                        (Field_TreeViewItems[i].Parent as TreeViewItem).IsExpanded = true;
                        Field_TreeViewItems[i].IsExpanded = true;
                        Field_TreeViewItems[i].IsSelected = true;
                        Field_TreeViewItems[i].BringIntoView();
                        RelView_Item_Clicked(i);
                        return;
                    }
                }
            }

            MessageBox.Show("No further occurances found in this search direction!");
        }

        private void RelView_Item_Clicked(int Index)
        {
            Selected_Index = Index;
            Map_Info This_Info = Info[Index];
            currentlyEditing.Text = @"Currently Editing: [" + This_Info.Section_Name + "] - " + This_Info.Parent_Name + @"/" + This_Info.File_Name
                + " - File Offset: 0x" + This_Info.Offset.ToString("X");
            if (This_Info.Data == null)
                This_Info.Data = File_Buffer.Skip((int)This_Info.Offset).Take((int)This_Info.Size).ToArray();

            if (This_Info.Section_Name.Equals(".text"))
            {
                uint[] Data = new uint[This_Info.Data.Length / 4];
                for (int i = 0; i < This_Info.Data.Length; i += 4)
                {
                    Data[i / 4] = BitConverter.ToUInt32(This_Info.Data.Skip(i).Take(4).Reverse().ToArray(), 0);
                }
                GekkoAssembly.DisassembleHex(Data);
            }

            HexEditor.Stream = new MemoryStream(This_Info.Data); // "new MemoryStream(Data)" is probably a memory leak
            HexEditor.SetPosition(0, 0);
            Display_Content();
            //HexEditor.SetPosition(This_Info.Offset, This_Info.Size);
        }

        private void Reload_Tree_View(Dictionary<string, List<Map_Info>> MapInfo)
        {
            RelView.ItemsSource = null;
            List<Map_Info> MInfo = new List<Map_Info>();
            Field_TreeViewItems = new List<TreeViewItem>();
            var Section_TreeViewItems = new List<TreeViewItem>();

            // Load TreeViewItems
            RelView.ItemsSource = Section_TreeViewItems;
            try
            {
                foreach (KeyValuePair<string, List<Map_Info>> SectionInfo in MapInfo)
                {
                    TreeViewItem Section_Item = new TreeViewItem
                    {
                        Header = SectionInfo.Key
                    };

                    var Object_TreeViewItems = new Dictionary<string, TreeViewItem>();

                    for (int i = 0; i < SectionInfo.Value.Count; i++)
                    {
                        // Don't add .bss items since they can't be edited

                        if (!SectionInfo.Key.Equals(".bss"))
                        {
                            if (!Object_TreeViewItems.ContainsKey(SectionInfo.Value[i].Parent_Name))
                            {
                                TreeViewItem Object_Item = new TreeViewItem
                                {
                                    Header = SectionInfo.Value[i].Parent_Name,
                                };
                                Object_TreeViewItems.Add(SectionInfo.Value[i].Parent_Name, Object_Item);
                                Section_Item.Items.Add(Object_Item);
                            }

                            TreeViewItem Method_Item = new TreeViewItem
                            {
                                Header = SectionInfo.Value[i].File_Name,
                                Tag = MInfo.Count
                            };
                            Method_Item.Selected += new RoutedEventHandler((object s, RoutedEventArgs e) => RelView_Item_Clicked((int)Method_Item.Tag));
                            Object_TreeViewItems[SectionInfo.Value[i].Parent_Name].Items.Add(Method_Item);
                            Field_TreeViewItems.Add(Method_Item);
                        }

                        MInfo.Add(SectionInfo.Value[i]);
                    }
                    Section_TreeViewItems.Add(Section_Item);
                }
                Info = MInfo.ToArray();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                MessageBox.Show(e.StackTrace);
            }
        }

        private void Search(object sender, RoutedEventArgs e)
        {
            if (Search_Window == null || !Search_Window.IsLoaded)
                Search_Window = new SearchWindow(this);

            Search_Window.Show();
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            openFileDialog.Filter = "Relocatable Module|*.rel|Binary File|*.bin|All Files|*.*";
            openFileDialog.DefaultExt = ".rel";

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string fileLocation = openFileDialog.FileName;
                if (File.Exists(fileLocation))
                {
                    //HexEditor.FileName = fileLocation;
                    File_Buffer = File.ReadAllBytes(fileLocation);
                    Title = "Nintendo Reloctable Module Editor - " + Path.GetFileName(fileLocation);
                    HexEditor.Stream = new MemoryStream(File_Buffer);
                    Module = new REL(File_Buffer, fileLocation);
                }
            }
        }

        private void Add_Map_File_Click(object sender, RoutedEventArgs e)
        {
            openFileDialog.Filter = "Data Map File|*.map";
            openFileDialog.DefaultExt = ".map";

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string mapLocation = openFileDialog.FileName;
                if (File.Exists(mapLocation))
                {
                    string[] Map_Lines = File.ReadAllLines(mapLocation);
                    Dictionary<string, List<Map_Info>> Sorted_Map_Info = MapUtility.ParseMapFile(Map_Lines, File_Buffer, Module.Section_Entries, Module.Header.BSS_Section); // A0 for animal crossing
                    Reload_Tree_View(Sorted_Map_Info);
                }
            }
        }

        private void ReportDumpProgress(int Index)
        {
            progressBar.Value = (Index / (double)Info.Length) * 100;
            if (Index < Info.Length)
                progressLabel.Content = Index + "/" + Info.Length + " - Dumping " + Info[Index].File_Name + ".bin";
            else
                progressLabel.Content = "Dumping Finished Successfully";
        }

        private async void Dump_Click(object sender, RoutedEventArgs e)
        {
            if (Info == null)
            {
                MessageBox.Show("You must import a valid map file before dumping!", "Dump Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var openFolderDialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                SelectedPath = ""
            };

            if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && Directory.Exists(openFolderDialog.SelectedPath))
            {
                string SelectedPath = openFolderDialog.SelectedPath;
                int index = 0;

                await Task.Run(() =>
                {
                    foreach (Map_Info info in Info)
                    {
                        Dispatcher.Invoke(new Action(() => ReportDumpProgress(index)));
                        string CurrentPath = SelectedPath + "\\" + info.Section_Name;

                        // Create section folder if it doesn't exist
                        Directory.CreateDirectory(CurrentPath);

                        // Create object folder if it doesn't exist
                        if (!string.IsNullOrEmpty(info.Parent_Name))
                        {
                            CurrentPath += "\\" + info.Parent_Name;
                            Directory.CreateDirectory(CurrentPath);
                        }

                        // Create the file
                        using (var stream = File.Create(CurrentPath + "\\" + info.File_Name + ".bin"))
                        {
                            if (info.Data != null)
                            {
                                stream.Write(info.Data, 0, info.Data.Length);
                            }
                            else if (info.Offset < File_Buffer.Length && info.Offset + info.Size < File_Buffer.Length)
                            {
                                if (info.Section_Name.Equals(".bss"))
                                {
                                    stream.Write(new byte[info.Size], 0, (int)info.Size); // .bss section's space is allocated at program launch, usually to zero'ed data.
                                }
                                else
                                {
                                    stream.Write(File_Buffer, (int)info.Offset, (int)info.Size);
                                }
                            }
                            stream.Flush();
                        }

                        index++;
                    }
                    Dispatcher.Invoke(new Action(() => ReportDumpProgress(index)));
                });
            }
        }

        // From https://stackoverflow.com/questions/2241367/wpf-treeview-how-to-scroll-so-expanded-branch-is-visible
        private void TreeViewSelectedItemChanged(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = sender as TreeViewItem;
            if (item != null)
            {
                item.BringIntoView();
                e.Handled = true;
            }
        }
    }
}
