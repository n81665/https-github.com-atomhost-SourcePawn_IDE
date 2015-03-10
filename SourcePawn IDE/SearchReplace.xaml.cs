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
    /// Interaction logic for SearchReplace.xaml
    /// </summary>
    public partial class SearchReplace : Window
    {
        public SearchReplace(bool search)
        {
            InitializeComponent();
            if (!search)
            {
                Search.SelectedIndex = 1;
            }
        }

        public event EventHandler<SearchTextEventArgs> SearchText;

        private void search1_Click(object sender, RoutedEventArgs e)
        {
            if (SearchText != null)
            {
                SearchText(this, new SearchTextEventArgs { Text = searchtext.Text, Replace = null, Direction = (int)direction.Value, CaseSensitive = case1.IsChecked.GetValueOrDefault(false) });
            }
        }

        private void Search_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Search.SelectedIndex == 0)
            {
                this.Height = 128;
            }
            else
            {
                this.Height = 160;
            }
        }

        private void replace_Click(object sender, RoutedEventArgs e)
        {
            if (SearchText != null)
            {
                SearchText(this, new SearchTextEventArgs { Text = searchtext1.Text, Replace = replacetext.Text, Direction = (int)direction1.Value, CaseSensitive = case2.IsChecked.GetValueOrDefault(false) });
            }
        }

        private void search2_Click(object sender, RoutedEventArgs e)
        {
            if (SearchText != null)
            {
                SearchText(this, new SearchTextEventArgs { Text = searchtext1.Text, Replace = null, Direction = (int)direction1.Value, CaseSensitive = case2.IsChecked.GetValueOrDefault(false) });
            }
        }

        private void replaceall_Click(object sender, RoutedEventArgs e)
        {
            if (SearchText != null)
            {
                SearchText(this, new SearchTextEventArgs { Text = searchtext1.Text, Replace = replacetext.Text, Direction = (int)direction1.Value, CaseSensitive = case2.IsChecked.GetValueOrDefault(false), All = true });
            }
        }
    }

    public class SearchTextEventArgs : EventArgs
    {
        public string Text { get; set; }
        public string Replace { get; set; }
        public int Direction { get; set; }
        public bool CaseSensitive { get; set; }
        public bool All { get; set; }
    }
}
