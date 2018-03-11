using System.Windows.Input;

namespace Nintendo_Relocatable_Module_Editor
{
    public static class ShortcutCommands
    {
        public static RoutedCommand Search_Command = new RoutedCommand();
        public static RoutedCommand Goto_Command = new RoutedCommand();
        public static RoutedCommand Save_Command = new RoutedCommand();

        public static void InitializeShortcuts()
        {
            Search_Command.InputGestures.Add(new KeyGesture(Key.F, ModifierKeys.Control));
            Goto_Command.InputGestures.Add(new KeyGesture(Key.G, ModifierKeys.Control));
            Save_Command.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
        }
    }
}
