using System.Windows.Controls;

namespace tmkbCompanion.MVVM.View
{
    public partial class RunScriptView : System.Windows.Controls.UserControl
    {
        public RunScriptView()
        {
            InitializeComponent();
        }

        private void ConsoleLogBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                textBox.ScrollToEnd();
            }
        }
    }
}
