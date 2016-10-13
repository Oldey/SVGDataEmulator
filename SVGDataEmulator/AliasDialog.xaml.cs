using System;
using System.Collections.Generic;
using System.IO;
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

namespace SVGDataEmulator
{
    /// <summary>
    /// Interaction logic for AliasWindow.xaml
    /// </summary>
    public partial class AliasWindow : Window
    {
        public string defaultAlias = String.Empty;

        public AliasWindow(string path)
        {
            InitializeComponent();
            labelAlias.Text += path + "\":";
            defaultAlias = new DirectoryInfo(System.IO.Path.GetDirectoryName(path)).Name;
            textBoxAlias.Text = defaultAlias;
        }

        public string ResponseText
        {
            get { return textBoxAlias.Text; }
            set { textBoxAlias.Text = value; }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

    }
}
