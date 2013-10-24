﻿using Milkman.Imaging.Common;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Milkman.Imaging
{
    public partial class FlipTileTemplateWide : UserControl
    {
        public FlipTileTemplateWide()
        {
            InitializeComponent();
        }

        public void RenderLiveTileImage(string filename, string title, string content)
        {
            this.txtTitle.Text = title;
            this.txtContent.Text = content;

            this.Measure(new Size(691, 336));
            this.Arrange(new Rect(0, 0, 691, 336));
            
            WriteableBitmap image = new WriteableBitmap(691, 336);
            image.Render(this, null);
            image.Invalidate();

            using (IsolatedStorageFile output = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var stream = output.OpenFile(filename, System.IO.FileMode.OpenOrCreate))
                {
                    image.SavePng(stream);
                }
            }
        }
    }
}