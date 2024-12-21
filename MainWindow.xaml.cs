using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using System.Windows.Input;
using System.Xml;
using System.Windows.Threading;
using System.CodeDom;
using Microsoft.Web.WebView2.Core;
using System.Reflection;
using System.Windows.Shapes;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Diagnostics.Eventing.Reader;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace TeXnically
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        System.Windows.Forms.NotifyIcon trayIcon;
        private WindowState storedWindowState = WindowState.Normal;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void trayIcon_Click(object? sender, EventArgs e)
        {
            Show();
            WindowState = storedWindowState;
        }

        const string baselineHtml = "<html><head><script src=\"https://polyfill.io/v3/polyfill.min.js?features=es6\"></script>\r\n<script id=\"MathJax-script\" async src=\"https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-mml-chtml.js\"></script></head><body><p>***</p></body></html>";
        bool isLoaded = false;
        string svgContent = "";
        Regex rgGetSvgPart = new Regex("<svg.+?</svg>");

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialise the UI
            Wpf.Ui.Appearance.Theme.Apply(Wpf.Ui.Appearance.ThemeType.Light, Wpf.Ui.Appearance.BackgroundType.Acrylic, false);

            // Load the minimise-to-tray UI
            trayIcon = new System.Windows.Forms.NotifyIcon();
            trayIcon.Text = "TeXnically";
            trayIcon.Icon = new System.Drawing.Icon("texnically.ico");
            trayIcon.Click += new EventHandler(trayIcon_Click);
        }

        private void postedSvgFromWeb(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            // Pull the raw SVG from the MathJax
            svgContent = rgGetSvgPart.Match(e.TryGetWebMessageAsString()).Value;
        }

        private void btnCompile_Click(object sender, RoutedEventArgs e)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(LatexTools.TagSvgWithLatex(tbInput.Text, svgContent));
            System.IO.MemoryStream stream = new System.IO.MemoryStream(bytes, true);
            Clipboard.SetData("image/svg+xml", stream);
            stream.Close();
            stream.Dispose();
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.IO.MemoryStream dataStream = (MemoryStream)Clipboard.GetData("image/svg+xml");
                tbInput.Text = LatexTools.ReadLatexSvg(Encoding.UTF8.GetString(dataStream.ToArray()));

                // Refresh the status bar
                tbkStatus.Text = "Loaded from clipboard; " + Math.Floor(sldZoom.Value).ToString() + "% zoom";
                tbkStatus.FontWeight = FontWeights.Regular;
                tbkStatus.Foreground = new SolidColorBrush(Colors.Black);
            }
            catch
            {
                // Fail somewhat quietly
                throwError("Unable to read valid SVG from clipboard");
            }
        }

        private async void tbInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (wbPreview != null && wbPreview.CoreWebView2 != null)
            {
                string sanitisedInput = System.Web.HttpUtility.JavaScriptStringEncode(tbInput.Text);
                var funcall = "convert(\"" + sanitisedInput + "\")";
                await wbPreview.CoreWebView2.ExecuteScriptAsync(funcall);
            }

            // Refresh the status bar
            tbkStatus.Text = "Ready; " + Math.Floor(sldZoom.Value).ToString() + "% zoom";
            tbkStatus.FontWeight = FontWeights.Regular;
            tbkStatus.Foreground = new SolidColorBrush(Colors.Black);
        }

        private void throwError(string errorText)
        {
            tbkStatus.Text = errorText;
            tbkStatus.FontWeight = FontWeights.Bold;
            tbkStatus.Foreground = new SolidColorBrush(Colors.Red);
        }

        private void loaded(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if(!isLoaded && wbPreview != null && wbPreview.CoreWebView2 != null)
            {
                string path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "boilerplate.html");
                wbPreview.CoreWebView2.Navigate(@"file:///" + path);
                wbPreview.CoreWebView2.WebMessageReceived += postedSvgFromWeb;

                isLoaded = true;
            }

        }

        private void Grid_DragEnter(object sender, DragEventArgs e)
        {
            // Prepare the grid to show it's ready to receive a drop
            rectReady.Visibility = Visibility.Visible;
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            try
            {
                System.IO.MemoryStream dataStream = (MemoryStream)e.Data.GetData("image/svg+xml");
                tbInput.Text = LatexTools.ReadLatexSvg(Encoding.UTF8.GetString(dataStream.ToArray()));
            }
            catch
            {
                // Fail somewhat quietly
                throwError("Unable to read valid SVG from clipboard");
            }
            finally
            {
                rectReady.Visibility = Visibility.Hidden;
            }
        }

        private void Grid_DragLeave(object sender, DragEventArgs e)
        {
            rectReady.Visibility = Visibility.Hidden;
        }

        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            // Prepare the grid to show it's ready to receive a drop
            rectReady.Visibility = Visibility.Visible;
        }

        private void sldZoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (wbPreview != null && wbPreview.CoreWebView2 != null)
            {
                wbPreview.ZoomFactor = sldZoom.Value / 100;

                // Refresh the status bar
                tbkStatus.Text = "Ready; " + Math.Floor(sldZoom.Value).ToString() + "% zoom";
                tbkStatus.FontWeight = FontWeights.Regular;
                tbkStatus.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            sldZoom.Value = 100;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                Hide();
            else
                storedWindowState = WindowState;
        }

        void CheckTrayIcon()
        {
            ShowTrayIcon(!IsVisible);
        }

        void ShowTrayIcon(bool show)
        {
            if (trayIcon != null)
               trayIcon.Visible = show;
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            CheckTrayIcon();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            trayIcon.Dispose();
            trayIcon = null;
        }
    }
}
