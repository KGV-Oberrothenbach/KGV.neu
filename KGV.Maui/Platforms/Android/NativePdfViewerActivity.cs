using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KGV.Maui.Platforms.Android
{
    [global::Android.App.Activity(Label = "NativePdfViewerActivity")]
    public class NativePdfViewerActivity : global::Android.App.Activity
    {
        private global::AndroidX.ViewPager2.Widget.ViewPager2 _viewPager;
        private global::Android.Widget.TextView _txtPageIndicator;
        private global::Android.Widget.ProgressBar _progressLoading;
        private global::Android.Widget.ImageButton _btnClose;

        private global::Android.Graphics.Pdf.PdfRenderer? _pdfRenderer;
        private global::Android.OS.ParcelFileDescriptor? _parcelFileDescriptor;
        private List<global::Android.Graphics.Bitmap> _pageBitmaps = new List<global::Android.Graphics.Bitmap>();

        protected override void OnCreate(global::Android.OS.Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_native_pdf_viewer);

            _viewPager = FindViewById<global::AndroidX.ViewPager2.Widget.ViewPager2>(Resource.Id.viewPager);
            _txtPageIndicator = FindViewById<global::Android.Widget.TextView>(Resource.Id.txtPageIndicator);
            _progressLoading = FindViewById<global::Android.Widget.ProgressBar>(Resource.Id.progressLoading);
            _btnClose = FindViewById<global::Android.Widget.ImageButton>(Resource.Id.btnClose);

            _btnClose.Click += (_, _) => Finish();

            var data = Intent.Data;
            if (data == null)
            {
                Finish();
                return;
            }

            // Load PDF on background thread and prepare adapter
            _progressLoading.Visibility = global::Android.Views.ViewStates.Visible;
            Task.Run(async () => await LoadPdfAsync(data)).ContinueWith(_ => RunOnUiThread(() => _progressLoading.Visibility = global::Android.Views.ViewStates.Gone));
        }

        private async Task LoadPdfAsync(global::Android.Net.Uri uri)
        {
            try
            {
                var ctx = global::Android.App.Application.Context;
                // Open ParcelFileDescriptor from content URI
                _parcelFileDescriptor = ctx.ContentResolver.OpenFileDescriptor(uri, "r");
                if (_parcelFileDescriptor == null)
                    return;

                _pdfRenderer = new global::Android.Graphics.Pdf.PdfRenderer(_parcelFileDescriptor);
                var pageCount = _pdfRenderer.PageCount;

                // Render each page to a bitmap (simple eager load)
                for (int i = 0; i < pageCount; i++)
                {
                    using var page = _pdfRenderer.OpenPage(i);
                    int width = Resources.DisplayMetrics.WidthPixels;
                    int height = Resources.DisplayMetrics.HeightPixels;
                    var bitmap = global::Android.Graphics.Bitmap.CreateBitmap(width, height, global::Android.Graphics.Bitmap.Config.Argb8888);
                    bitmap.EraseColor(global::Android.Graphics.Color.White);
                    page.Render(bitmap, null, null, global::Android.Graphics.Pdf.PdfRenderMode.ForDisplay);
                    _pageBitmaps.Add(bitmap);
                }

                RunOnUiThread(() =>
                {
                    _viewPager.Adapter = new PdfPagerAdapter(_pageBitmaps);
                    _viewPager.RegisterOnPageChangeCallback(new PageChangeCallback(this));
                    UpdatePageIndicator(1, _pageBitmaps.Count);
                });
            }
            catch (Exception)
            {
                RunOnUiThread(() => global::Android.Widget.Toast.MakeText(this, "PDF konnte nicht geladen werden.", global::Android.Widget.ToastLength.Long).Show());
            }
        }

        private void UpdatePageIndicator(int current, int total)
        {
            _txtPageIndicator.Text = $"{current}/{total}";
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            try
            {
                foreach (var bmp in _pageBitmaps)
                    bmp.Recycle();
                _pageBitmaps.Clear();
            }
            catch { }

            _pdfRenderer?.Close();
            try { _parcelFileDescriptor?.Close(); } catch { }
        }

        private class PdfPagerAdapter : global::AndroidX.RecyclerView.Widget.RecyclerView.Adapter
        {
            private readonly List<global::Android.Graphics.Bitmap> _bitmaps;

            public PdfPagerAdapter(List<global::Android.Graphics.Bitmap> bitmaps)
            {
                _bitmaps = bitmaps;
            }

            public override int ItemCount => _bitmaps.Count;

            public override global::AndroidX.RecyclerView.Widget.RecyclerView.ViewHolder OnCreateViewHolder(global::Android.Views.ViewGroup parent, int viewType)
            {
                var imageView = new global::Android.Widget.ImageView(parent.Context);
                imageView.LayoutParameters = new global::Android.Views.ViewGroup.LayoutParams(global::Android.Views.ViewGroup.LayoutParams.MatchParent, global::Android.Views.ViewGroup.LayoutParams.MatchParent);
                imageView.SetScaleType(global::Android.Widget.ImageView.ScaleType.FitCenter);

                return new ImageViewHolder(imageView);
            }

            public override void OnBindViewHolder(global::AndroidX.RecyclerView.Widget.RecyclerView.ViewHolder holder, int position)
            {
                if (holder is ImageViewHolder ivh)
                {
                    ivh.ImageView.SetImageBitmap(_bitmaps[position]);
                }
            }

            private class ImageViewHolder : global::AndroidX.RecyclerView.Widget.RecyclerView.ViewHolder
            {
                public global::Android.Widget.ImageView ImageView { get; }
                public ImageViewHolder(global::Android.Views.View itemView) : base(itemView)
                {
                    ImageView = (global::Android.Widget.ImageView)itemView;
                }
            }
        }

        private class PageChangeCallback : global::AndroidX.ViewPager2.Widget.ViewPager2.OnPageChangeCallback
        {
            private readonly NativePdfViewerActivity _parent;
            public PageChangeCallback(NativePdfViewerActivity parent) { _parent = parent; }
            public override void OnPageSelected(int position) => _parent.UpdatePageIndicator(position + 1, _parent._pageBitmaps.Count);
        }
    }
}
