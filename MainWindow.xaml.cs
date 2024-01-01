using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Windows.Storage.Pickers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ImgSort
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        ImagesRepository ImagesRepository { get; } = new();
        public MainWindow()
        {
            this.InitializeComponent();
            _ = TrySetMicaBackdrop(false);
            this.ExtendsContentIntoTitleBar = true;  // enable custom titlebar
            this.SetTitleBar(AppTitleBar);      // set user ui element as titlebar
            string folderPath = "C:\\Users\\iseli\\Downloads\\IMAGES_231210\\01";
            LoadImages(folderPath);
        }

        private void LoadImages(string folderPath)
        {
            ImagesRepository.GetImages(folderPath);
            var numImages = ImagesRepository.Images.Count();
            ImageInfoBar.Message = $"{numImages} have loaded from {folderPath}";
            ImageInfoBar.IsOpen = true;
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                LoadImages(folder.Path);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var imageInfo = (sender as Button)?.DataContext as ImageInfo;
            if (imageInfo != null)
            {
                if (imageInfo.DetailWindow != null)
                {
                    imageInfo.DetailWindow.Activate();
                    return;
                }
                // initialize the details if we already have some info
                string pattern = @"_[0-9a-f]{5}\.png$";
                Match m = Regex.Match(imageInfo.FullName, pattern, RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var v = Convert.ToUInt32(m.Value.Substring(1, 5),16);
                    for (int i = 0; i < 10; i++)
                    {
                        imageInfo.detail[9-i] = v & 3;
                        v >>= 2;
                    }
                }
                // Create the binding description.
                Binding b = new Binding();
                b.Mode = BindingMode.OneWay;
                b.Source = imageInfo.RectLeft;
                Canvas canvas = new();
                canvas.Width = 850;
                canvas.Height = 250;
                canvas.DataContext = imageInfo;
                canvas.KeyUp += Canvas_KeyUp;
                Image image = new();
                image.Source = new BitmapImage(new Uri(imageInfo.FullName, UriKind.Absolute));
                image.SetValue(Canvas.TopProperty, 4);
                image.SetValue(Canvas.LeftProperty, 4);
                canvas.Children.Add(image);
                var rect = new Rectangle();
                rect.Stroke = ImageInfo.ColorBrush[4];
                rect.StrokeThickness = 4;
                rect.Width = 104;
                rect.Height = 104;
                rect.SetValue(Canvas.TopProperty, 0);
                rect.SetBinding(Canvas.LeftProperty, b);
                canvas.Children.Add(rect);
                for (int i = 0; i < 10; i++)
                {
                    var r = new Rectangle();
                    r.Fill = ImageInfo.ColorBrush[imageInfo.detail[i]];
                    r.Width = 80;
                    r.Height = 12;
                    r.SetValue(Canvas.LeftProperty, i*80 + 4);
                    r.SetValue(Canvas.TopProperty, 108);
                    canvas.Children.Add(r);
                }
                Window window = new()
                {
                    Title = imageInfo.Name,
                    Content = canvas
                };
                SetWindowSize(window, 1200, 250);
                window.Activate();
                window.Closed += Window_Closed;
                imageInfo.DetailWindow = window;
            }
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            var window = sender as Window;
            var canvas = window.Content as Canvas;
            var imageInfo = canvas?.DataContext as ImageInfo;
            imageInfo.DetailWindow = null;
            //Debug.WriteLine("### Set DetailWindow to null");
        }

        private static void SetWindowSize(Window window, int width, int height)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
        }

        SpringVector3NaturalMotionAnimation _springAnimation;

        private void CreateOrUpdateSpringAnimation(float finalValue)
        {
            if (_springAnimation == null)
            {
                Compositor compositor = this.Compositor;
                if (compositor is not null)
                {
                    _springAnimation = this.Compositor.CreateSpringVector3Animation();
                    _springAnimation.Target = "Scale";
                }
            }

            _springAnimation.FinalValue = new Vector3(finalValue);
        }

        private void element_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // Scale up to 1.5
            CreateOrUpdateSpringAnimation(1.05f);

            (sender as UIElement).StartAnimation(_springAnimation);
        }

        private void element_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            // Scale back down to 1.0
            CreateOrUpdateSpringAnimation(1.0f);

            (sender as UIElement).StartAnimation(_springAnimation);
        }

        // Option 2 - Implement Mica with codebehind.
        // Allows for toggling backdrops as shown in sample.
        bool TrySetMicaBackdrop(bool useMicaAlt)
        {
            if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                Microsoft.UI.Xaml.Media.MicaBackdrop micaBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
                micaBackdrop.Kind = useMicaAlt ? Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt : Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base;
                this.SystemBackdrop = micaBackdrop;

                return true; // Succeeded.
            }

            return false; // Mica is not supported on this system.
        }

        private async void AppBarButton_Click_1(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog();

            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
            dialog.XamlRoot = (sender as Button).XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = "Image Sorter";
            dialog.CloseButtonText = "Close";
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.Content = "Some interesting text";

            var result = await dialog.ShowAsync();
        }

        private void Btn_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            var imageInfo = (sender as Button)?.DataContext as ImageInfo;
            if (imageInfo != null)
            {
                var options = new FindNextElementOptions()
                {
                    SearchRoot = ImageScrollViewer,
                    XYFocusNavigationStrategyOverride = XYFocusNavigationStrategyOverride.Projection
                };
                DependencyObject candidate = null;
                if (e.Key == Windows.System.VirtualKey.A)
                {
                    imageInfo.ImageClass = 1;
                    candidate = FocusManager.FindNextElement(FocusNavigationDirection.Down, options);
                }
                if (e.Key == Windows.System.VirtualKey.E) { imageInfo.ImageClass = 1; }
                if (e.Key == Windows.System.VirtualKey.S)
                {
                    imageInfo.ImageClass = 2;
                    candidate = FocusManager.FindNextElement(FocusNavigationDirection.Down, options);
                }
                if (e.Key == Windows.System.VirtualKey.G) { imageInfo.ImageClass = 2; }
                if (e.Key == Windows.System.VirtualKey.D)
                {
                    imageInfo.ImageClass = 3;
                    candidate = FocusManager.FindNextElement(FocusNavigationDirection.Down, options);
                }
                if (e.Key == Windows.System.VirtualKey.M) { imageInfo.ImageClass = 3; }
                if (e.Key == Windows.System.VirtualKey.U) { imageInfo.ImageClass = 0; }
                if (candidate != null && candidate is Control)
                {
                    (candidate as Control).Focus(FocusState.Keyboard);
                }
            }
        }
        private void Canvas_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            var canvas = (Canvas)sender;
            var imageInfo = canvas?.DataContext as ImageInfo;
            if (imageInfo != null)
            {
                if (e.Key == Windows.System.VirtualKey.Tab)
                {
                    imageInfo.NextRect();
                    e.Handled = true;
                    Canvas.SetLeft(canvas.Children[1], imageInfo.RectLeft);
                }
                if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    string start;
                    string pattern = @"_[0-9a-f]{5}\.png$";
                    Match m = Regex.Match(imageInfo.FullName, pattern, RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        start = imageInfo.FullName.Substring(0, m.Index);
                    }
                    else
                    {
                        string p2 = @"_[egm]\.png$";
                        Match m2 = Regex.Match(imageInfo.FullName, p2, RegexOptions.IgnoreCase);
                        if (m2.Success)
                        {
                            start = imageInfo.FullName.Substring(0, m2.Index);
                        }
                        else
                        {
                            start = imageInfo.FullName.Substring(0, imageInfo.FullName.Length - 4);
                        }
                    }
                    uint v = 0;
                    for (int i = 0; i < 10; i++)
                    {
                        v <<= 2;
                        v |= imageInfo.detail[i];
                    }
                    string code = $"_{v:x5}";
                    string desiredName = start + code + ".png";
                    File.Move(imageInfo.FullName, desiredName);
                    imageInfo.FullName = desiredName;
                    imageInfo.DetailWindow.Close();
                }
                if (e.Key == Windows.System.VirtualKey.Escape)
                {
                    imageInfo.DetailWindow.Close();
                }
                if (e.Key == Windows.System.VirtualKey.A)
                {
                    imageInfo.detail[imageInfo.RectIdx] = 1;
                    (canvas.Children[imageInfo.RectIdx + 2] as Rectangle).Fill = ImageInfo.ColorBrush[1];
                    imageInfo.NextRect();
                    e.Handled = true;
                    Canvas.SetLeft(canvas.Children[1], imageInfo.RectLeft);
                }
                if (e.Key == Windows.System.VirtualKey.E)
                {
                    imageInfo.detail[imageInfo.RectIdx] = 1;
                    (canvas.Children[imageInfo.RectIdx + 2] as Rectangle).Fill = ImageInfo.ColorBrush[1];
                    e.Handled = true;
                }
                if (e.Key == Windows.System.VirtualKey.S)
                {
                    imageInfo.detail[imageInfo.RectIdx] = 2;
                    (canvas.Children[imageInfo.RectIdx + 2] as Rectangle).Fill = ImageInfo.ColorBrush[2];
                    imageInfo.NextRect();
                    e.Handled = true;
                    Canvas.SetLeft(canvas.Children[1], imageInfo.RectLeft);
                }
                if (e.Key == Windows.System.VirtualKey.G)
                {
                    imageInfo.detail[imageInfo.RectIdx] = 2;
                    (canvas.Children[imageInfo.RectIdx + 2] as Rectangle).Fill = ImageInfo.ColorBrush[2];
                    e.Handled = true;
                }
                if (e.Key == Windows.System.VirtualKey.D)
                {
                    imageInfo.detail[imageInfo.RectIdx] = 3;
                    (canvas.Children[imageInfo.RectIdx + 2] as Rectangle).Fill = ImageInfo.ColorBrush[3];
                    imageInfo.NextRect();
                    e.Handled = true;
                    Canvas.SetLeft(canvas.Children[1], imageInfo.RectLeft);
                }
                if (e.Key == Windows.System.VirtualKey.M)
                {
                    imageInfo.detail[imageInfo.RectIdx] = 3;
                    (canvas.Children[imageInfo.RectIdx + 2] as Rectangle).Fill = ImageInfo.ColorBrush[3];
                    e.Handled = true;
                }
                if (e.Key == Windows.System.VirtualKey.U)
                {
                    imageInfo.detail[imageInfo.RectIdx] = 0;
                    (canvas.Children[imageInfo.RectIdx + 2] as Rectangle).Fill = ImageInfo.ColorBrush[0];
                    e.Handled = true;
                }
            }
        }
    }
    class WindowsSystemDispatcherQueueHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        struct DispatcherQueueOptions
        {
            internal int dwSize;
            internal int threadType;
            internal int apartmentType;
        }

        [DllImport("CoreMessaging.dll")]
        private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

        object m_dispatcherQueueController = null;
        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
            {
                // one already exists, so we'll just use it.
                return;
            }

            if (m_dispatcherQueueController == null)
            {
                DispatcherQueueOptions options;
                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
                options.threadType = 2;    // DQTYPE_THREAD_CURRENT
                options.apartmentType = 2; // DQTAT_COM_STA

                CreateDispatcherQueueController(options, ref m_dispatcherQueueController);
            }
        }
    }
    // The ItemsSource used is a list of custom-class Bar objects called BarItems

    public class ImageInfo : INotifyPropertyChanged
    {
        public ImageInfo(string fullName, string name)
        {
            FullName = fullName;
            Name = name;
            detail = new uint[10];
        }
        public string Name { get; set; }
        public string FullName
        {
            get { return fullName; }
            set
            {
                if (value != fullName)
                {
                    fullName = value;
                    OnPropertyChanged("FullName");
                    OnPropertyChanged(nameof(ImageClass));
                    OnPropertyChanged(nameof(ImageClassColor));
                }
            }
        }
        public int ImageClass
        {
            get
            {
                string pattern = @"_[0-9a-f]{5}\.png$";
                Match m = Regex.Match(FullName, pattern, RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var v = Convert.ToUInt32(m.Value.Substring(1,5),16);
                    int[] cnt = new int[4];
                    for (int i = 0; i < 10; i++)
                    {
                        uint t = v & 3;
                        v >>= 2;
                        cnt[t] += 1;
                    }
                    if (cnt[0] > 0) return 0;
                    if (cnt[3] > 0) return 3;
                    if (cnt[2] > 0) return 2;
                    return 1;
                }
                string p2 = @"_[egm]\.png$";
                Match m2 = Regex.Match(FullName, p2, RegexOptions.IgnoreCase);
                if (m2.Success)
                {
                    switch (m2.Value[1])
                    {
                        case 'e':
                            return 1;
                        case 'g':
                            return 2;
                        case 'm':
                            return 3;
                    }
                }
                return 0;
            }
            set
            {
                if (value < 0 || value > 3) return;
                if (value == ImageClass) return;
                if (FullName.Length <= 6) return;
                string end = FullName.Substring(FullName.Length - 6);
                string start;
                if (end[0] == '_')
                {
                    start = FullName.Substring(0, FullName.Length - 6);
                }
                else
                {
                    start = FullName.Substring(0, FullName.Length - 4);
                }
                string code = "";
                switch (value)
                {
                    case 1:
                        code = "_e";
                        break;
                    case 2:
                        code = "_g";
                        break;
                    case 3:
                        code = "_m";
                        break;
                }
                string desiredName = start + code + ".png";
                File.Move(FullName, desiredName);
                FullName = desiredName;
            }
        }
        public string ImageClassColor
        {
            get
            {
                switch(ImageClass)
                {
                    case 1: return "White";
                    case 2: return "SpringGreen";
                    case 3: return "Red";
                    default: return "Gold";
                }
            }
        }
        public int RectIdx
        {
            get { return rectIdx; }
            set
            {
                //Debug.WriteLine($"Rect moved from {rectLeft}");
                rectIdx = value;
                OnPropertyChanged(nameof(RectIdx));
                //Debug.WriteLine($"Rect moved to {rectLeft}");
            }
        }
        public void NextRect ()
        {
            var next = RectIdx + 1;
            if (next > 9) next = 0;
            RectIdx = next;
        }
        public int RectLeft
        {
            get
            {
                var pos = rectIdx * 80 - 8;
                if (pos < 0) pos = 0;
                if (pos > 704) pos = 704;
                return pos;
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private int rectIdx = 0;
        public static SolidColorBrush[] ColorBrush = {
            new SolidColorBrush(Colors.Gold),
            new SolidColorBrush(Colors.White),
            new SolidColorBrush(Colors.SpringGreen),
            new SolidColorBrush(Colors.Red),
            new SolidColorBrush(Colors.Blue),
        };
        private Window detailWindow = null;
        public Window DetailWindow
        {
            get { return detailWindow; }
            set {
                detailWindow = value;
                OnPropertyChanged(nameof(DetailWindow));
            }
        }
        public uint[] detail;
        private string fullName;
    }

    public class ImagesRepository
    {
        public ObservableCollection<ImageInfo> Images;

        public void GetImages(string folderPath)
        {
            if (Images == null)
            {
                Images = new ObservableCollection<ImageInfo>();
            }
            else
            {
                Images.Clear();
            }
            var di = new DirectoryInfo(folderPath);
            var files = di.GetFiles("*.png");
            foreach (var file in files)
            {
                Images.Add(new ImageInfo(file.FullName, file.Name));
            }
        }
    }
}
