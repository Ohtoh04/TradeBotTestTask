using System.Windows;
using System.Windows.Controls;

namespace TradeBotTestTask.Presentation.Views
{
    /// <summary>
    /// Interaction logic for ShellView.xaml
    /// </summary>
    public partial class ShellView : UserControl
    {
        public ShellView()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                Window window = Window.GetWindow(this);
                if (window != null)
                {
                    window.Width = 800;
                    window.Height = 600;
                    window.MinWidth = 800;
                    window.MinHeight = 600;
                }
            };
        }
    }
}
