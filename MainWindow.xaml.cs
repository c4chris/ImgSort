using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
            string folderPath = "C:\\Users\\iseli\\Downloads\\IMAGES_231210\\00";
            LoadImages(folderPath);
        }

        private void LoadImages(string folderPath)
        {
            ImagesRepository.GetImages(folderPath);
            var numImages = ImagesRepository.Images.Count();
            ImageInfoBar.Message = $"{numImages} have loaded";
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
                Image image = new();
                image.Source = new BitmapImage(new Uri(imageInfo.FullName, UriKind.Absolute));
                Window window = new()
                {
                    Title = imageInfo.Name,
                    Content = image
                };
                SetWindowSize(window, 800, 250);
                window.Activate();
            }
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
        }
        public string Name { get; set; }
        public string FullName { get; set; }
        public int ImageClass
        {
            get
            {
                if (FullName.Length > 6)
                {
                    string end = FullName.Substring(FullName.Length - 6);
                    if (end[0] == '_')
                    {
                        switch (end[1])
                        {
                            case 'e':
                                return 1;
                            case 'g':
                                return 2;
                            case 'm':
                                return 3;
                        }
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
                OnPropertyChanged(nameof(ImageClassColor));
            }
        }
        public string ImageClassColor
        {
            get
            {
                switch(ImageClass)
                {
                    case 1: return "White";
                    case 2: return "Green";
                    case 3: return "Red";
                    default: return "Gold";
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
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
