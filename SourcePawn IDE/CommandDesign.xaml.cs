using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SourcePawn_IDE
{
    /// <summary>
    /// Interaction logic for CommandDesign.xaml
    /// </summary>
    public partial class CommandDesign : Window
    {
        public Command command = new Command();
        public CommandDesign()
        {
            InitializeComponent();
            cmdtype.SelectionChanged+=new SelectionChangedEventHandler(cmdtype_SelectionChanged);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            command.command = cmd.Text;
            command.Callback = callback.Text;
            command.Description = desc.Text;
            command.Flags = admflags.Text;
            command.Group = group.Text;
            command.CommandType = (CMDTYPE)cmdtype.SelectedIndex;
            this.DialogResult = true;
        }

        private void cmdtype_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (cmdtype.SelectedIndex)
            {
                case 2:
                    tabItem2.Visibility = Visibility.Visible;
                    desc.IsEnabled = true;
                    break;
                case 3:
                    tabItem2.Visibility = Visibility.Hidden;
                    desc.IsEnabled = false;
                    break;
                default:
                    tabItem2.Visibility = Visibility.Hidden;
                    desc.IsEnabled = true;
                    break;
            }
        }

        private void BtnFlags_Click(object sender, RoutedEventArgs e)
        {
            AdmFlags fl = new AdmFlags(admflags.Text);
            if (fl.ShowDialog().GetValueOrDefault(false))
            {
                admflags.Text = fl.Flags;
            }
        }
    }
}
