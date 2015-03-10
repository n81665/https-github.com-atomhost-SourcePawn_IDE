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
    /// Interaction logic for ConVarDialog.xaml
    /// </summary>
    public partial class ConVarDialog : Window
    {
        public ConVar ConVarInfo;

        public ConVarDialog()
        {
            InitializeComponent();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            ConVarInfo = new ConVar(cmd.Text, Value.Text, desc.Text, UseMinVal.IsChecked.GetValueOrDefault(false).ToString(), MinVal.Text, UseMaxVal.IsChecked.GetValueOrDefault(false).ToString(), MaxVal.Text, handle.Text);
            this.DialogResult = true;
        }
    }
}
