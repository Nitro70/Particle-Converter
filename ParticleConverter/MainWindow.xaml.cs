using HelixToolkit.SharpDX.Core;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using ParticleConverter.Minecraft;
using ParticleConverter.util;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Media;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Particle = ParticleConverter.util.Particle;

namespace ParticleConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private readonly Dictionary<string, string> oldValues = new Dictionary<string, string>();
        private readonly util.ImageConverter ImageConverter = new util.ImageConverter();

        // CultureInfo.InvariantCultureでen-USの書式を取得
        private readonly NumberFormatInfo format = CultureInfo.InvariantCulture.NumberFormat;

        /// <summary>Suppresses change handlers while the constructor populates controls.</summary>
        private bool isInitialising = true;

        public MainWindow()
        {
            InitializeComponent();
            //カルチャ変更
            if (!CultureInfo.CurrentCulture.Name.Equals("ja-JP"))
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-US", false);
            }
            Load_Langugae();
            Load_McVersions();
            ColorCodeBox_TextChanged(ColorCodeBox, null);
            FolderPathBox.Text = Settings.Default.FolderPath;
            NamespaceBox.Text = Settings.Default.Namespace;
            ExportAsDatapackBox.IsChecked = Settings.Default.ExportAsDatapack;

            ParticleTypeBox.SelectionChanged += ParticleTypeBox_Changed;
            ParticleTypeBox.LostFocus += ParticleTypeBox_Changed;

            isInitialising = false;
            Refresh_ParticleTypes();
            Update_CommandPreview();
        }

        /// <summary>Fills the version dropdown and restores the last selection.</summary>
        private void Load_McVersions()
        {
            McVersionBox.ItemsSource = McVersionProfile.All;
            McVersionBox.SelectedItem = McVersionProfile.ById(Settings.Default.McVersion);
        }

        /// <summary>The version currently selected in the dropdown.</summary>
        private McVersionProfile SelectedVersion =>
            McVersionBox?.SelectedItem as McVersionProfile ?? McVersionProfile.Latest;
        private void Load_Langugae()
        {
            try
            {
                // AppContext.BaseDirectory rather than Assembly.Location, which is empty for a
                // single-file publish.
                DirectoryInfo di = new DirectoryInfo(System.IO.Path.Combine(AppContext.BaseDirectory, "lang"));
                FileInfo[] files =
                    di.GetFiles("*.xaml");
                foreach (FileInfo path in files)
                {
                    ComboBoxItem cbi = new ComboBoxItem
                    {
                        Content = System.IO.Path.GetFileNameWithoutExtension(path.Name)
                    };
                    LanguageBox.Items.Add(cbi);
                }

                LanguageBox.SelectedIndex = 0;

                // A saved choice wins; otherwise fall back to the OS language if we ship it.
                string preferred = !string.IsNullOrEmpty(Settings.Default.Language)
                    ? Settings.Default.Language
                    : System.Globalization.CultureInfo.CurrentCulture.Name;

                int index = 0;
                foreach (ComboBoxItem cbi in LanguageBox.Items)
                {
                    if (cbi.Content.Equals(preferred))
                    {
                        LanguageBox.SelectedIndex = index;
                    }
                    index++;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("言語ファイルの読み込みに失敗しました\nFailed to load language files.",
                    "エラー/Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Logger.WriteExceptionLog(e);
                this.Close();
            }
        }

        private void Minimize_Button_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void Close_Button_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            //OptionsView.MaxHeight = Options.ActualHeight - OptionsTitle.ActualHeight;
            //OptionsView.Height = Options.ActualHeight - OptionsTitle.ActualHeight;
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            OptionsView.ScrollToEnd();
        }


        /// <summary>
        /// フィルターつきのテキストボックスの更新
        /// </summary>
        /// <param name="tb"></param>
        /// <param name="str"></param>
        private void Update_FilterTextBox(TextBox tb, string str)
        {
            tb.Text = str;
            if (!oldValues[tb.Name].Equals(str))
            {
                oldValues[tb.Name] = str;
                Update_Preview();
            }
        }

        private void ColorCodeBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox sb = (TextBox)sender;
            try
            {
                Color color = (Color)System.Windows.Media.ColorConverter.ConvertFromString(ColorCodeBox.Text);
                oldValues[sb.Name] = sb.Text;
                Update_Preview();
            }
            catch
            {
                sb.Text = oldValues[sb.Name];
            }
        }

        private void NumlicBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox sb = (TextBox)sender;
            if (decimal.TryParse(sb.Text, out decimal d) && d > 0)
            {
                oldValues[sb.Name] = sb.Text;
            }
            else
            {
                SystemSounds.Beep.Play();
                sb.Text = oldValues[sb.Name];
            }
        }

        // Loaded fires again whenever WPF re-attaches an element to the visual tree, which the
        // Expander does to its content. These handlers therefore have to be idempotent: the
        // original code used Dictionary.Add and += unconditionally, so opening "More Settings"
        // threw a duplicate-key ArgumentException and took the whole app down with it.

        private void FilterTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            TextBox sb = (TextBox)sender;
            sb.KeyDown -= EnterKey_Down;
            sb.KeyDown += EnterKey_Down;

            // Keep the first value seen - it is the baseline a rejected edit reverts to.
            if (!oldValues.ContainsKey(sb.Name))
            {
                oldValues[sb.Name] = sb.Text;
            }
        }

        private void CheckBox_Loaded(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            cb.Checked -= CheckBox_Check_Changed;
            cb.Unchecked -= CheckBox_Check_Changed;
            cb.Checked += CheckBox_Check_Changed;
            cb.Unchecked += CheckBox_Check_Changed;
        }

        private void CheckBox_Check_Changed(object sender, RoutedEventArgs e)
        {
            Update_Preview();
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            cb.SelectionChanged -= Combobox_Selection_Changed;
            cb.SelectionChanged += Combobox_Selection_Changed;
        }

        private void Combobox_Selection_Changed(object sender, RoutedEventArgs e)
        {
            Update_Preview();
        }

        private void Update_Preview()
        {
            // The command preview does not depend on an image being loaded, so refresh it
            // regardless of whether the 3D preview below runs.
            Update_CommandPreview();

            try
            {
                if (ImageConverter.IsLoaded && UsePreviewBox.IsChecked.Value)
                {
                    int coord = CoordinateAxis.SelectedIndex;
                    int verAlig = VerticalAlignmentBox.SelectedIndex;
                    int horAlig = HorizontalAlignmentBox.SelectedIndex;
                    //Mat smat = ImageConverter.GetModifiedImage();
                    //System.Windows.Size size = ImageConverter.GetBlocks();
                    //Bitmap bitmap = smat.ToBitmap();
                    //var mb = new MeshBuilder(false, true);

                    //IList<Point3D> pnts = new List<Point3D>
                    //{
                    //    new Point3D(0, 0, 0),
                    //    new Point3D(size.Width, 0, 0),
                    //    new Point3D(size.Width, size.Height, 0),
                    //    new Point3D(0, size.Height, 0)
                    //};

                    //mb.AddPolygon(pnts);

                    //var mesh = mb.ToMesh(false);

                    //PointCollection pntCol = new PointCollection
                    //{
                    //    new System.Windows.Point(0, 0),
                    //    new System.Windows.Point(bitmap.Size.Width, 0),
                    //    new System.Windows.Point(bitmap.Size.Width, bitmap.Size.Height),
                    //    new System.Windows.Point(0, bitmap.Size.Height)
                    //};
                    //mesh.TextureCoordinates = pntCol;

                    //ImageBrush brush = new ImageBrush();

                    //using (Stream stream = new MemoryStream())
                    //{
                    //    bitmap.Save(stream, ImageFormat.Png);
                    //    stream.Seek(0, SeekOrigin.Begin);
                    //    BitmapImage img = new BitmapImage();
                    //    img.BeginInit();
                    //    img.CacheOption = BitmapCacheOption.OnLoad;
                    //    img.StreamSource = stream;
                    //    img.EndInit();
                    //    brush.ImageSource = img;
                    //}

                    //brush.TileMode = TileMode.Tile;
                    //brush.ViewportUnits = BrushMappingMode.Absolute;
                    //brush.ViewboxUnits = BrushMappingMode.Absolute;
                    //brush.Stretch = Stretch.None;
                    //brush.AlignmentX = AlignmentX.Left;
                    //brush.AlignmentY = AlignmentY.Top;
                    //brush.Viewport = new System.Windows.Rect(0, 0, brush.ImageSource.Width, brush.ImageSource.Height);
                    //DiffuseMaterial mat = new DiffuseMaterial(brush);

                    //GeometryModel3D gModel3D = new GeometryModel3D { Geometry = mesh, Material = mat, BackMaterial = mat };

                    //PreviewModel.Content = gModel3D;
                    Particle[] particles = ImageConverter.GetParticles(coord, verAlig, horAlig);
                    //ParticleModel.Children.Clear();
                    var points = new PointGeometry3D();
                    var vectors = new Vector3Collection();
                    var colors = new Color4Collection();
                    var ptIdx = new IntCollection();
                    int i = 0;
                    foreach (Particle particle in particles)
                    {
                        vectors.Add(new Vector3((float)particle.x, (float)particle.y, (float)particle.z));
                        if (UseStaticDustColor.IsChecked.Value)
                        {
                            Color c = (Color)ColorConverter.ConvertFromString(ColorCodeBox.Text);
                            colors.Add(new Color4(c.R / 255f, c.G / 255f, c.B / 255f, 1.0f));
                        }
                        else
                        {
                            colors.Add(new Color4(particle.r / 255f, particle.g / 255f, particle.b / 255f, 1.0f));
                        }
                        ptIdx.Add(i);
                        i++;
                    }
                    points.Positions = vectors;
                    points.Colors = colors;
                    points.Indices = ptIdx;
                    ParticleModel.Geometry = points;
                    double size = double.Parse(ParticleSizeBox.Text);
                    ParticleModel.Size = new System.Windows.Size(3 * Math.Sqrt(size), 3 * Math.Sqrt(size));
                    ParticleCounter.Text = $"Particles: {particles.Length}";
                    if (particles.Length >= 2000)
                    {
                        ParticleCounter.Foreground = new SolidColorBrush(Colors.Red);
                        CounterAlert.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        ParticleCounter.Foreground = new SolidColorBrush(Colors.Snow);
                        CounterAlert.Visibility = Visibility.Hidden;
                    }
                }
            }
            catch (Exception e)
            {
                // A preview failure is not fatal - report it and leave the window usable.
                Logger.WriteExceptionLog(e);
                MessageBox.Show($"プレビューの更新に失敗しました\nFailed to update preview.\n\n{e.Message}",
                    "エラー/Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void EnterKey_Down(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DependencyObject ancestor = ((Control)sender).Parent;
                //フォーカスできる親を探してフォーカス
                while (ancestor != null)
                {
                    if (ancestor is UIElement element && element.Focusable)
                    {
                        element.Focus();
                        break;
                    }

                    ancestor = VisualTreeHelper.GetParent(ancestor);
                }
            }
        }

        /// <summary>
        /// カラープレビューの更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorCodeBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox sb = (TextBox)sender;
            try
            {
                Color color = (Color)ColorConverter.ConvertFromString(sb.Text);
                Ellipse auter = new Ellipse
                {
                    Fill = System.Windows.Media.Brushes.Snow,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 25,
                    Height = 25
                };
                Ellipse inner = new Ellipse
                {
                    Fill = new SolidColorBrush(color),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(1, 1, 1, 1),
                    Width = 23,
                    Height = 23
                };
                if (ColorCanvas != null)
                {
                    ColorCanvas.Children.Clear();
                    ColorCanvas.Children.Add(auter);
                    ColorCanvas.Children.Add(inner);
                }
            }
            catch
            {
                //意図的に何もしない
            }
        }

        private void BrowsImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog of = new OpenFileDialog { Filter = "Image File|*.jpg;*.png;*.jpeg" };
            if (of.ShowDialog() == true)
            {
                FilePathBox.Text = of.FileName;
                ImageFileLoad();
            }
        }

        private void Sync_SizeBoxes()
        {
            System.Windows.Size size = ImageConverter.GetBlocks();
            Update_FilterTextBox(SizeWBox, size.Width.ToString("R", format));
            Update_FilterTextBox(SizeHBox, size.Height.ToString("R", format));
        }

        private void Sync_ResolutionBoxes()
        {
            Update_FilterTextBox(ResolutionWidthBox, ImageConverter.ResizedWidth.ToString());
            Update_FilterTextBox(ResolutionHeightBox, ImageConverter.ResizedHeight.ToString());
        }

        private void ImageFileLoad()
        {
            try
            {
                ImageConverter.Load(FilePathBox.Text);

                // Seed the function name from the image, the way the old export named its file.
                if (string.IsNullOrWhiteSpace(FunctionNameBox.Text))
                {
                    FunctionNameBox.Text = McResourceLocation.SanitizePath(
                        System.IO.Path.GetFileNameWithoutExtension(FilePathBox.Text));
                }

                if (AutoSizeBox.IsChecked.Value)
                {
                    Sync_SizeBoxes();
                }
                if (AutoResolutionBox.IsChecked.Value)
                {
                    Sync_ResolutionBoxes();
                }
                ImageConverter.ResizedHeight = int.Parse(ResolutionHeightBox.Text);
                ImageConverter.ResizedWidth = int.Parse(ResolutionWidthBox.Text);
                ImageConverter.IsFlip = ImageFlipBox.IsChecked.Value;
                ImageConverter.Density = double.Parse(ParticleDensityBox.Text);
                if (ImageConverter.ResizedHeight * ImageConverter.ResizedWidth >= 3000)
                {

                }
                else
                {
                    UsePreviewBox.IsChecked = true;
                }
            }
            catch (Exception e)
            {
                // An unreadable image should not close the app - the user can pick another one.
                MessageBox.Show($"画像ファイルの読み込みに失敗しました\nFailed to load an image file.\n\n{e.Message}",
                "エラー/Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
                Logger.WriteExceptionLog(e);
            }
        }

        private void AutoSizeBox_Checked(object sender, RoutedEventArgs e)
        {
            if (ImageConverter.IsLoaded)
            {
                Sync_SizeBoxes();
            }
        }

        private void AutoResolutionBox_Checked(object sender, RoutedEventArgs e)
        {
            if (ImageConverter.IsLoaded)
            {
                ResolutionWidthBox.Text = ImageConverter.SourseWidth.ToString();
                ResolutionHeightBox.Text = ImageConverter.SourseHeight.ToString();
                ImageConverter.ResizedWidth = int.Parse(ResolutionWidthBox.Text);
                ImageConverter.ResizedHeight = int.Parse(ResolutionHeightBox.Text);
                if (AutoSizeBox.IsChecked.Value)
                {
                    Sync_SizeBoxes();
                }
                else
                {
                    ImageConverter.Density = ImageConverter.ResizedWidth / double.Parse(SizeWBox.Text);
                }
            }
        }

        private void ParticleDensityBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ImageConverter.IsLoaded && double.TryParse(ParticleDensityBox.Text, out double density))
            {
                if (decimal.Parse(ParticleDensityBox.Text) >= 0)
                {
                    ImageConverter.Density = density;
                    Sync_SizeBoxes();
                }
            }
        }

        private void MenuButton_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {

            ContextMenu mi = this.Resources["Menu"] as ContextMenu;
            ((MenuItem)mi.Items[0]).Header = this.Resources["DeveloperTwitter"];
            ((MenuItem)mi.Items[1]).Header = this.Resources["BugReport"];
            ((MenuItem)mi.Items[2]).Header = this.Resources["About"];
            mi.IsOpen = true;
        }

        // 解像度ボックスのフォーカス外れた
        private void ResolutionBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox sb = (TextBox)sender;
            NumlicBox_LostFocus(sender, e);
            if (ImageConverter.IsLoaded)
            {
                if (sender.Equals(ResolutionWidthBox))
                {
                    int reheight = (int)(ImageConverter.SourseHeight * (double.Parse(sb.Text) / ImageConverter.SourseWidth));
                    Update_FilterTextBox(ResolutionHeightBox, reheight.ToString());
                }
                if (sender.Equals(ResolutionHeightBox))
                {
                    int rewidth = (int)(ImageConverter.SourseWidth * (double.Parse(sb.Text) / ImageConverter.SourseHeight));
                    Update_FilterTextBox(ResolutionWidthBox, rewidth.ToString());
                }
                ImageConverter.ResizedWidth = int.Parse(ResolutionWidthBox.Text);
                ImageConverter.ResizedHeight = int.Parse(ResolutionHeightBox.Text);
                if (AutoSizeBox.IsChecked.Value)
                {
                    Sync_SizeBoxes();
                }
                else
                {
                    ImageConverter.Density = ImageConverter.ResizedWidth / double.Parse(SizeWBox.Text);
                }
                Update_Preview();
            }
        }

        private void LanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string language = (string)((ComboBoxItem)LanguageBox.SelectedItem).Content;

            ResourceDictionary dictionary = new ResourceDictionary
            {
                Source = new Uri("lang/" + language + ".xaml", UriKind.Relative)
            };

            // リソースディクショナリを変更
            Resources.MergedDictionaries[0] = dictionary;

            if (!isInitialising)
            {
                Settings.Default.Language = language;
                Settings.Default.Save();
            }
        }

        private void SizeBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox sb = (TextBox)sender;
            NumlicBox_LostFocus(sender, e);
            if (ImageConverter.IsLoaded)
            {
                if (sender.Equals(SizeWBox))
                {
                    double reheight = ImageConverter.ResizedHeight / (ImageConverter.ResizedWidth / double.Parse(sb.Text));
                    Update_FilterTextBox(SizeHBox, reheight.ToString("R", format));
                    Update_FilterTextBox(ParticleDensityBox, (ImageConverter.ResizedWidth / double.Parse(sb.Text)).ToString("R", format));
                    ImageConverter.Density = ImageConverter.ResizedWidth / double.Parse(sb.Text);
                }
                if (sender.Equals(SizeHBox))
                {
                    double rewidth = ImageConverter.ResizedWidth / (ImageConverter.ResizedHeight / double.Parse(sb.Text));
                    Update_FilterTextBox(SizeWBox, rewidth.ToString("R", format));
                    Update_FilterTextBox(ParticleDensityBox, (ImageConverter.ResizedHeight / double.Parse(sb.Text)).ToString("R", format));
                    ImageConverter.Density = ImageConverter.ResizedWidth / double.Parse(sb.Text);
                }
            }
        }


        /// <summary>
        /// Validates the particle size against the range vanilla actually accepts.
        /// </summary>
        /// <remarks>
        /// This box used to reject anything above 1.00, which is why the usual advice was to
        /// export at 1.0 and find-and-replace the number in a text editor afterwards. The real
        /// dust scale range is 0.01 to 4.0; from 1.20.5 a value outside it is a parse error
        /// rather than being clamped, so the command would silently never run.
        /// </remarks>
        private void ParticleSizeBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox sb = (TextBox)sender;
            if (double.TryParse(sb.Text, NumberStyles.Float, format, out double d)
                && d >= ParticleCommandSettings.MinScale
                && d <= ParticleCommandSettings.MaxScale)
            {
                oldValues[sb.Name] = sb.Text;
                Update_Preview();
                Update_CommandPreview();
            }
            else
            {
                SystemSounds.Beep.Play();
                sb.Text = oldValues[sb.Name];
            }
        }

        private void McVersionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isInitialising) return;

            Settings.Default.McVersion = SelectedVersion.Id;
            Settings.Default.Save();
            Refresh_ParticleTypes();
            Update_CommandPreview();
        }

        private void ExportAsDatapackBox_Changed(object sender, RoutedEventArgs e)
        {
            if (isInitialising || DatapackNamePanel == null) return;

            bool asDatapack = ExportAsDatapackBox.IsChecked == true;
            DatapackNamePanel.IsEnabled = asDatapack;
            Settings.Default.ExportAsDatapack = asDatapack;
            Settings.Default.Save();
        }

        /// <summary>Forces namespace and function name to characters Minecraft allows.</summary>
        private void DatapackNameBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox sb = (TextBox)sender;
            sb.Text = sender == NamespaceBox
                ? McResourceLocation.SanitizeNamespace(sb.Text)
                : McResourceLocation.SanitizePath(sb.Text);

            if (sender == NamespaceBox && sb.Text.Length > 0)
            {
                Settings.Default.Namespace = sb.Text;
                Settings.Default.Save();
            }
        }

        private void ParticleOptionsBox_LostFocus(object sender, RoutedEventArgs e)
        {
            Update_CommandPreview();
        }

        private void ParticleTypeBox_Changed(object sender, RoutedEventArgs e)
        {
            Update_ParticleOptionsVisibility();
            Update_CommandPreview();
        }

        /// <summary>
        /// Repopulates the particle dropdown for the selected version, keeping the current
        /// selection if that particle still exists.
        /// </summary>
        private void Refresh_ParticleTypes()
        {
            string previous = ParticleTypeBox.Text;

            var ids = new List<string>();
            foreach (ParticleDefinition d in ParticleRegistry.ForVersion(SelectedVersion))
            {
                ids.Add(d.Id);
            }

            ParticleTypeBox.ItemsSource = ids;

            if (!string.IsNullOrEmpty(previous) && ids.Contains(previous))
            {
                ParticleTypeBox.SelectedItem = previous;
            }
            else
            {
                // dust is the only particle that can carry an image's colours.
                ParticleTypeBox.SelectedItem = ids.Contains("dust") ? "dust" : ids[0];
            }

            Update_ParticleOptionsVisibility();
        }

        /// <summary>The option kind the options box is currently configured for.</summary>
        private ParticleOptionKind currentOptionKind = ParticleOptionKind.None;

        /// <summary>
        /// Shows the options box only for particles that need a value this tool cannot derive
        /// from the image, relabels it for that particle, and seeds a valid default.
        /// </summary>
        private void Update_ParticleOptionsVisibility()
        {
            if (ParticleOptionsBox == null) return;

            ParticleOptionKind kind = ParticleRegistry.OptionKindOf(ParticleTypeBox.Text);
            bool needsInput = kind == ParticleOptionKind.BlockState
                              || kind == ParticleOptionKind.Item
                              || kind == ParticleOptionKind.Raw
                              || kind == ParticleOptionKind.DustColorTransition;

            ParticleOptionsBox.Visibility = needsInput ? Visibility.Visible : Visibility.Collapsed;

            if (!needsInput || kind == currentOptionKind) return;

            // The kind changed, so the previous value is meaningless here - relabel and reseed
            // rather than carrying "minecraft:stone" over into a colour field.
            currentOptionKind = kind;
            HintAssist.SetHint(ParticleOptionsBox, Resources[HintKeyFor(kind)]);
            ParticleOptionsBox.Text = DefaultOptionFor(kind);
        }

        private static string HintKeyFor(ParticleOptionKind kind) => kind switch
        {
            ParticleOptionKind.BlockState => "ParticleOptionsBlock",
            ParticleOptionKind.Item => "ParticleOptionsItem",
            ParticleOptionKind.DustColorTransition => "ParticleOptionsFade",
            _ => "ParticleOptions",
        };

        private static string DefaultOptionFor(ParticleOptionKind kind) => kind switch
        {
            ParticleOptionKind.BlockState => "minecraft:stone",
            ParticleOptionKind.Item => "minecraft:stone",
            ParticleOptionKind.DustColorTransition => "white",
            _ => "",
        };

        /// <summary>Parses a WPF colour string, falling back rather than throwing on bad input.</summary>
        private static McColor ParseColor(string value, Color fallback)
        {
            try
            {
                Color c = (Color)ColorConverter.ConvertFromString(value);
                return new McColor(c.R, c.G, c.B);
            }
            catch
            {
                return new McColor(fallback.R, fallback.G, fallback.B);
            }
        }

        /// <summary>Builds the command settings that both the preview and the export use.</summary>
        private ParticleCommandSettings BuildCommandSettings()
        {
            // An unparseable colour code falls back rather than blocking the preview.
            McColor fixedColor = ParseColor(ColorCodeBox.Text, Colors.Red);

            double scale = ParticleCommandSettings.MinScale;
            if (double.TryParse(ParticleSizeBox.Text, NumberStyles.Float, format, out double parsed))
            {
                scale = parsed;
            }

            string options = ParticleOptionsBox?.Text ?? "";
            ParticleOptionKind kind = ParticleRegistry.OptionKindOf(ParticleTypeBox.Text);

            return new ParticleCommandSettings
            {
                Version = SelectedVersion,
                ParticleId = ParticleTypeBox.Text,
                Scale = scale,
                UseFixedColor = UseStaticDustColor.IsChecked == true,
                FixedColor = fixedColor,
                BlockState = kind == ParticleOptionKind.BlockState && options.Length > 0 ? options : "minecraft:stone",
                Item = kind == ParticleOptionKind.Item && options.Length > 0 ? options : "minecraft:stone",
                RawOptions = kind == ParticleOptionKind.Raw ? options : "",
                TransitionToColor = kind == ParticleOptionKind.DustColorTransition
                    ? ParseColor(options, Colors.White)
                    : new McColor(255, 255, 255),
                CoordinateMode = (string)((ComboBoxItem)CoordinateModeBox.SelectedItem)?.Tag == "Local"
                    ? Minecraft.CoordinateMode.RelativeLocal
                    : Minecraft.CoordinateMode.RelativeWorld,
                DisplayMode = (string)((ComboBoxItem)DisplayModeBox.SelectedItem)?.Tag == "force"
                    ? ParticleDisplayMode.Force
                    : ParticleDisplayMode.Normal,
                Viewers = ParticleViewerBox.Text,
            };
        }

        /// <summary>Shows one representative command so the emitted syntax is visible before export.</summary>
        private void Update_CommandPreview()
        {
            if (isInitialising || CommandPreviewBox == null) return;

            try
            {
                ParticleCommandSettings settings = BuildCommandSettings();
                CommandPreviewBox.Text = ParticleCommand.Build(0, 1, 0, new McColor(255, 0, 0), settings);
            }
            catch (Exception ex)
            {
                CommandPreviewBox.Text = ex.Message;
            }
        }

        private void ImageFlipBox_Checked(object sender, RoutedEventArgs e)
        {
            ImageConverter.IsFlip = ImageFlipBox.IsChecked.Value;
        }

        private void ImageRotationBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (int.TryParse((string)((ComboBoxItem)ImageRotationBox.SelectedItem).Tag, out int angle))
            {
                ImageConverter.Angle = angle;
            }
        }

        private void UsePreviewBox_Checked(object sender, RoutedEventArgs e)
        {
            Update_Preview();
        }

        private void BrowsFolderButton_Click(object sender, RoutedEventArgs e)
        {
            // OpenFolderDialog is built into WPF from .NET 8, which is why the old
            // WindowsAPICodePack-Shell dependency is gone.
            var dialog = new OpenFolderDialog();

            if (dialog.ShowDialog() == true)
            {
                FolderPathBox.Text = dialog.FolderName;
                Settings.Default.FolderPath = dialog.FolderName;
                Settings.Default.Save();
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ImageConverter.IsLoaded)
            {
                SystemSounds.Beep.Play();
                return;
            }

            Set_ExportUiEnabled(false);
            ButtonProgressAssist.SetMaximum(ExportButton, 100);
            ButtonProgressAssist.SetValue(ExportButton, 0);
            ExportResultBox.Visibility = Visibility.Hidden;

            try
            {
                Particle[] particles = ImageConverter.GetParticles(
                    CoordinateAxis.SelectedIndex,
                    VerticalAlignmentBox.SelectedIndex,
                    HorizontalAlignmentBox.SelectedIndex);

                ParticleCommandSettings settings = BuildCommandSettings();
                McVersionProfile version = SelectedVersion;

                string functionName = McResourceLocation.SanitizePath(FunctionNameBox.Text);
                if (functionName.Length == 0)
                {
                    functionName = McResourceLocation.SanitizePath(
                        System.IO.Path.GetFileNameWithoutExtension(FilePathBox.Text));
                    FunctionNameBox.Text = functionName;
                }

                DatapackLayout layout = DatapackLayout.Resolve(
                    FolderPathBox.Text,
                    ExportAsDatapackBox.IsChecked == true,
                    NamespaceBox.Text,
                    functionName,
                    version);

                string[] header = Build_FunctionHeader(particles.Length, version, layout);
                var progress = new Progress<int>(p => ButtonProgressAssist.SetValue(ExportButton, p));

                await Task.Run(() => Write_Function(layout, version, particles, settings, header, progress));

                Show_ExportResult(layout);
                SystemSounds.Beep.Play();
            }
            catch (Exception exc)
            {
                MessageBox.Show($"ファイルの書き込みに失敗しました\nFailed to export a file.\n\n{exc.Message}",
                    "エラー/Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Logger.WriteExceptionLog(exc);
            }
            finally
            {
                ButtonProgressAssist.SetValue(ExportButton, 0);
                Set_ExportUiEnabled(true);
            }
        }

        private void Set_ExportUiEnabled(bool enabled)
        {
            ExportButton.IsEnabled = enabled;
            Options.IsEnabled = enabled;
            BrowsImageButton.IsEnabled = enabled;
            BrowsFolderButton.IsEnabled = enabled;
        }

        private string[] Build_FunctionHeader(int particleCount, McVersionProfile version, DatapackLayout layout)
        {
            string appVersion = FileVersionInfo.GetVersionInfo(typeof(App).Assembly.Location).FileVersion;

            var lines = new List<string>
            {
                "### Particle Image Function",
                $"### Generator: Particle-Converter {appVersion}",
                $"### Minecraft: {version.DisplayName} (pack_format {version.PackFormat}" +
                    (version.PackFormatMinor.HasValue ? $".{version.PackFormatMinor.Value}" : "") + ")",
                $"### Source: {System.IO.Path.GetFileName(FilePathBox.Text)}",
                $"### Resolution: {ImageConverter.ResizedWidth}x{ImageConverter.ResizedHeight}",
                $"### Particles: {particleCount}",
                $"### ParticleType: {ParticleTypeBox.Text}",
                "",
            };

            if (layout.FunctionReference != null)
            {
                lines.Add($"### Run with: /function {layout.FunctionReference}");
                lines.Add("");
            }

            lines.Add("### This file was generated by Kemo431's Particle-Converter.");
            lines.Add("### Download Link: https://github.com/kemo14331/Particle-Converter");
            lines.Add("");

            return lines.ToArray();
        }

        /// <summary>
        /// Writes the datapack and function. Runs off the UI thread; progress is reported as a
        /// percentage rather than per line, which is what made the original export slow.
        /// </summary>
        private static void Write_Function(
            DatapackLayout layout,
            McVersionProfile version,
            Particle[] particles,
            ParticleCommandSettings settings,
            string[] header,
            IProgress<int> progress)
        {
            if (layout.IsDatapack)
            {
                DatapackWriter.WritePackMeta(layout, version, "Particle images generated by Particle-Converter");
            }

            using (StreamWriter writer = DatapackWriter.OpenFunction(layout))
            {
                foreach (string line in header)
                {
                    writer.WriteLine(line);
                }

                int reportEvery = Math.Max(1, particles.Length / 100);
                for (int i = 0; i < particles.Length; i++)
                {
                    Particle p = particles[i];
                    writer.WriteLine(ParticleCommand.Build(p.x, p.y, p.z, new McColor(p.r, p.g, p.b), settings));

                    if (i % reportEvery == 0)
                    {
                        progress?.Report(particles.Length == 0 ? 100 : i * 100 / particles.Length);
                    }
                }
            }

            progress?.Report(100);
        }

        private void Show_ExportResult(DatapackLayout layout)
        {
            string label = (string)(layout.FunctionReference != null
                ? Resources["ExportedRunThis"]
                : Resources["ExportedWroteFile"]);

            ExportResultBox.Text = layout.FunctionReference != null
                ? $"{label} /function {layout.FunctionReference}"
                : $"{label} {layout.FunctionPath}";
            ExportResultBox.Visibility = Visibility.Visible;
        }

        private void Show_DevsTwitter(object sender, RoutedEventArgs e)
        {
            var ps = new ProcessStartInfo("https://twitter.com/newkemo431")
            {
                UseShellExecute = true,
                Verb = "open"
            };
            Process.Start(ps);
        }

        private void Show_BugReport(object sender, RoutedEventArgs e)
        {
            var ps = new ProcessStartInfo("https://github.com/kemo14331/Particle-Converter/issues")
            {
                UseShellExecute = true,
                Verb = "open"
            };
            Process.Start(ps);
        }
        private async void Show_About(object sender, RoutedEventArgs e)
        {
            var dialog = new dialogs.About();
            var result = await DialogHost.ShowDialog(dialog);
        }

    }
}
