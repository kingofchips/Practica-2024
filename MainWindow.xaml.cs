using Microsoft.Win32;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ImageEditor
{
    public partial class MainWindow : Window
    {
        // Fields to store the original image and the editable bitmap
        private BitmapImage _originalImage;
        private Bitmap _editableBitmap;

        // Fields for storing adjustment values
        private float _brightness = 0;
        private float _contrast = 0;
        private string _textToAdd = "";
        private float _fontSize = 20;
        private int _positionX = 0;
        private int _positionY = 0;
        private bool _isSelecting = false;
        private System.Windows.Point _startPoint;
        private System.Windows.Point _endPoint;


        // Constructor
        public MainWindow()
        {
            InitializeComponent();
            DisplayedImage.MouseLeftButtonDown += DisplayedImage_MouseLeftButtonDown;
            DisplayedImage.MouseLeftButtonUp += DisplayedImage_MouseLeftButtonUp;
            DisplayedImage.MouseMove += DisplayedImage_MouseMove;
        }

        // Event handler for loading an image
        private void LoadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png";
            if (openFileDialog.ShowDialog() == true)
            {
                _originalImage = new BitmapImage(new Uri(openFileDialog.FileName));
                DisplayedImage.Source = _originalImage;
                _editableBitmap = new Bitmap(openFileDialog.FileName);
                WidthSlider.Value = _editableBitmap.Width;
                HeightSlider.Value = _editableBitmap.Height;
            }
        }

         // Event handler for saving the edited image
        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PNG Image|*.png";
            if (saveFileDialog.ShowDialog() == true)
            {
                _editableBitmap.Save(saveFileDialog.FileName, ImageFormat.Png);
            }
        }

        // Event handler for resetting the image to its original state
        private void ResetImage_Click(object sender, RoutedEventArgs e)
        {
            if (_originalImage != null)
            {
                _editableBitmap = BitmapImageToBitmap(_originalImage);
                UpdateDisplayedImage();
            }
        }

        // Helper method to convert BitmapImage to Bitmap
        private Bitmap BitmapImageToBitmap(BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                encoder.Save(outStream);
                Bitmap bitmap = new Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }

        // Event handler for rotating the image
        private void RotateImage_Click(object sender, RoutedEventArgs e)
        {
            _editableBitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
            UpdateDisplayedImage();
        }

        // Event handler for applying a sepia filter
        private void ApplySepia_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilter(GetSepiaColorMatrix());
        }

        // Event handler for applying a black-and-white filter
        private void ApplyBlackAndWhite_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilter(GetBlackAndWhiteColorMatrix());
        }

        // Event handler for the brightness slider value change
        private void BrightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _brightness = (float)BrightnessSlider.Value;
            ApplyBrightness(_brightness);
        }

        // Event handler for the contrast slider value change
        private void ContrastSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _contrast = (float)ContrastSlider.Value;
            ApplyContrast(_contrast);
        }

        // Event handler for the width slider value change
        private void WidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_editableBitmap != null)
            {
                ResizeImage((int)WidthSlider.Value, _editableBitmap.Height);
            }
        }

        // Event handler for the height slider value change
        private void HeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_editableBitmap != null)
            {
                ResizeImage(_editableBitmap.Width, (int)HeightSlider.Value);
            }
        }

        // Method to update the displayed image after applying any adjustments
        private void UpdateDisplayedImage()
        {
            using (MemoryStream memory = new MemoryStream())
            {
                _editableBitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                DisplayedImage.Source = bitmapImage;
            }
        }

        // Method to apply a color matrix filter
        private void ApplyFilter(ColorMatrix colorMatrix)
        {
            using (Graphics g = Graphics.FromImage(_editableBitmap))
            {
                ImageAttributes attributes = new ImageAttributes();
                attributes.SetColorMatrix(colorMatrix);
                g.DrawImage(_editableBitmap, new Rectangle(0, 0, _editableBitmap.Width, _editableBitmap.Height),
                            0, 0, _editableBitmap.Width, _editableBitmap.Height, GraphicsUnit.Pixel, attributes);
            }
            UpdateDisplayedImage();
        }

        // Method to apply brightness adjustment
        private void ApplyBrightness(float brightness)
        {
            float b = brightness / 255.0f;
            ColorMatrix colorMatrix = new ColorMatrix(new float[][]
            {
                new float[] {1, 0, 0, 0, 0},
                new float[] {0, 1, 0, 0, 0},
                new float[] {0, 0, 1, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {b, b, b, 0, 1}
            });

            ApplyFilter(colorMatrix);
        }

        // Method to apply contrast adjustment
        private void ApplyContrast(float contrast)
        {
            float scale = contrast / 100.0f + 1.0f;
            float offset = 0.5f * (1.0f - scale);

            ColorMatrix colorMatrix = new ColorMatrix(new float[][]
            {
                new float[] {scale, 0, 0, 0, 0},
                new float[] {0, scale, 0, 0, 0},
                new float[] {0, 0, scale, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {offset, offset, offset, 0, 1}
            });

            ApplyFilter(colorMatrix);
        }

        // Method to get a sepia color matrix
        private ColorMatrix GetSepiaColorMatrix()
        {
            return new ColorMatrix(new float[][]
            {
                new float[] { .393f, .349f, .272f, 0, 0 },
                new float[] { .769f, .686f, .534f, 0, 0 },
                new float[] { .189f, .168f, .131f, 0, 0 },
                new float[] { 0, 0, 0, 1, 0 },
                new float[] { 0, 0, 0, 0, 1 }
            });
        }

        // Method to get a black-and-white color matrix
        private ColorMatrix GetBlackAndWhiteColorMatrix()
        {
            return new ColorMatrix(new float[][]
            {
                new float[] { .3f, .3f, .3f, 0, 0 },
                new float[] { .59f, .59f, .59f, 0, 0 },
                new float[] { .11f, .11f, .11f, 0, 0 },
                new float[] { 0, 0, 0, 1, 0 },
                new float[] { 0, 0, 0, 0, 1 }
            });
        }

        // Method to modify the image's height and width
        private void ResizeImage(int newWidth, int newHeight)
        {
            Bitmap resizedBitmap = new Bitmap(newWidth, newHeight);
            using (Graphics g = Graphics.FromImage(resizedBitmap))
            {
                g.DrawImage(_editableBitmap, 0, 0, newWidth, newHeight);
            }
            _editableBitmap = resizedBitmap;
            UpdateDisplayedImage();
        }

        // Event handler for the text box value change
        private void TextToAdd_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            _textToAdd = TextToAdd.Text;
        }

        // Event handler for the font size slider value change
        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _fontSize = (float)FontSizeSlider.Value;
        }

        // Event handler for the position X slider value change
        private void PositionXSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _positionX = (int)PositionXSlider.Value;
        }

        // Event handler for the position Y slider value change
        private void PositionYSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _positionY = (int)PositionYSlider.Value;
        }

        // Event handler for adding text to the image
        private void AddText_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_textToAdd))
            {
                MessageBox.Show("Text to add cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (Graphics g = Graphics.FromImage(_editableBitmap))
            {
                using (Font font = new Font("Arial", _fontSize))
                {
                    using (SolidBrush brush = new SolidBrush(System.Drawing.Color.White))
                    {
                        g.DrawString(_textToAdd, font, brush, new PointF(_positionX, _positionY));
                    }
                }
            }
            UpdateDisplayedImage();
        }

        // Event handler for the start of the image's cropping
        private void DisplayedImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isSelecting = true;
            _startPoint = e.GetPosition(DisplayedImage);
            SelectionRectangle.Visibility = Visibility.Visible;
        }
        
        // Event handler for the end of the image's cropping
        private void DisplayedImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelecting)
            {
                _isSelecting = false;
                _endPoint = e.GetPosition(DisplayedImage);
                CropImage();
                SelectionRectangle.Visibility = Visibility.Collapsed;
            }
        }

        // Event handler for getting the dimensions of the newly cropped image
        private void DisplayedImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isSelecting)
            {
                var currentPoint = e.GetPosition(DisplayedImage);

                double x = Math.Min(currentPoint.X, _startPoint.X);
                double y = Math.Min(currentPoint.Y, _startPoint.Y);
                double width = Math.Abs(currentPoint.X - _startPoint.X);
                double height = Math.Abs(currentPoint.Y - _startPoint.Y);

                SelectionRectangle.SetValue(Canvas.LeftProperty, x);
                SelectionRectangle.SetValue(Canvas.TopProperty, y);
                SelectionRectangle.Width = width;
                SelectionRectangle.Height = height;
            }
        }

        // Event handler for cropping the image
        private void CropImage_Click(object sender, RoutedEventArgs e)
        {
            SelectionRectangle.Visibility = Visibility.Visible;
        }

        // Method to crop a part of the image
        private void CropImage()
        {
            int x = (int)_startPoint.X;
            int y = (int)_startPoint.Y;
            int width = (int)Math.Abs(_endPoint.X - _startPoint.X);
            int height = (int)Math.Abs(_endPoint.Y - _startPoint.Y);

            Rectangle cropRect = new Rectangle(x, y, width, height);
            Bitmap croppedBitmap = new Bitmap(cropRect.Width, cropRect.Height);

            using (Graphics g = Graphics.FromImage(croppedBitmap))
            {
                g.DrawImage(_editableBitmap, new Rectangle(0, 0, croppedBitmap.Width, croppedBitmap.Height),
                            cropRect, GraphicsUnit.Pixel);
            }
            _editableBitmap = croppedBitmap;
            UpdateDisplayedImage();
        }
    }
}
