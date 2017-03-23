﻿using System;
using Xamarin.Forms.Platform.Android;
using Android.Support.V7.Widget;
using Xamarin.Forms;
using Android.Content.Res;
using Android.Views;
using FormsGridView = XamarinFormsGridView.Controls.GridView;
using Android.Widget;
using XamarinFormsGridView.Droid.Renderers;
using System.Reflection;
using System.Collections;
using Android.Util;
using System.Collections.Specialized;
using Android.Content;
using Android.OS;
using System.Collections.Generic;
using System.Linq;

[assembly: ExportRenderer (typeof(FormsGridView), typeof(GridViewRenderer))]
namespace XamarinFormsGridView.Droid.Renderers
{
    #region GridViewRenderer

    /// <summary>
    /// Renderer for GridView control on Android.
    /// </summary>
    public class GridViewRenderer : ViewRenderer<FormsGridView, RecyclerView>
    {
        #region Fields

        readonly Android.Content.Res.Orientation _orientation = Android.Content.Res.Orientation.Undefined;

        ScrollRecyclerView _recyclerView;

        float _startEventY;
        float _heightChange;

        GridLayoutManager _layoutManager;
        GridViewAdapter _adapter;

        #endregion

        #region Overides

        protected override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            if (newConfig.Orientation != _orientation)
                OnElementChanged(new ElementChangedEventArgs<FormsGridView>(Element, Element));
        }

        protected override void OnElementChanged(ElementChangedEventArgs<FormsGridView> e)
        {
            base.OnElementChanged(e);
            if (e.NewElement != null)
            {
                DestroyRecyclerview();
                CreateRecyclerView();
                base.SetNativeControl(_recyclerView);
            }

            //TODO unset
            //			this.Unbind (e.OldElement);
            //			this.Bind (e.NewElement);
        }

        protected override void OnElementPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            if (e.PropertyName == "ItemsSource" || e.PropertyName == "IsGroupingEnabled") {
                _adapter.Items = Element.ItemsSource;
            }

            //			if (e.PropertyName == "IsScrollEnabled") {
            //				Device.BeginInvokeOnMainThread (() => {
            //					_recyclerView.Enabled = Element.IsScrollEnabled;
            ////					Debug.WriteLine ("scroll enabled changed to " + _gridCollectionView.ScrollEnabled);
            //				}
            //				);


            //			}
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);
            UpdateGridLayout();
        }

        #endregion

        #region Methods

        void DestroyRecyclerview()
        {
            if (_recyclerView != null)
            {
                //TODO
                _recyclerView.Touch -= _recyclerView_Touch;
            }
        }

        void CreateRecyclerView()
        {
            _recyclerView = new ScrollRecyclerView(Android.App.Application.Context);
            _recyclerView.Touch += _recyclerView_Touch;
            var scrollListener = new GridViewScrollListener(Element, _recyclerView);
            _recyclerView.AddOnScrollListener(scrollListener);

            _recyclerView.SetItemAnimator(null);
            _recyclerView.HorizontalScrollBarEnabled = false;
            _recyclerView.VerticalScrollBarEnabled = true;

            _adapter = new GridViewAdapter(Element.ItemsSource, _recyclerView, Element, Resources.DisplayMetrics);
            _recyclerView.SetAdapter(_adapter);

            UpdateGridLayout();
        }

        /// <summary>
        /// Update the grid layout manager instance.
        /// </summary>
        void UpdateGridLayout()
        {
            //Get the span count.
            var spanCount = (int)Math.Max(1, Math.Floor(Element.Width / Element.MinItemWidth));

            //I found that if I don't re-iniitalize a new layout manager 
            //each time the span count should change then the items don't render.
            _layoutManager = new GridLayoutManager(Context, spanCount);
            _layoutManager.SetSpanSizeLookup(new GroupedGridSpanSizeLookup(_recyclerView.GetAdapter() as GridViewAdapter, spanCount));

            _recyclerView.SetLayoutManager(_layoutManager);
        }

        #endregion

        #region Events

        void _recyclerView_Touch(object sender, TouchEventArgs e)
        {

            var ev = e.Event;
            MotionEventActions action = ev.Action & MotionEventActions.Mask;
            switch (action) {
                case MotionEventActions.Down:
                    _startEventY = ev.GetY();
                    _heightChange = 0;
                    Element.RaiseOnStartScroll();
                    break;
                case MotionEventActions.Move:
                    float delta = (ev.GetY() + _heightChange) - _startEventY;
                    Element.RaiseOnScroll(delta, _recyclerView.GetVerticalScrollOffset());
                    break;
                case MotionEventActions.Up:
                    Element.RaiseOnStopScroll();
                    break;
            }
            e.Handled = false;

        }

        #endregion
    }

    #endregion

    public class GroupedGridSpanSizeLookup : GridLayoutManager.SpanSizeLookup
    {
        GridViewAdapter _adapter;
        int _spanCount;

        public GroupedGridSpanSizeLookup(GridViewAdapter adapter, int spanCount)
        {
            _adapter = adapter;
            _spanCount = spanCount;
        }

        public override int GetSpanSize(int position)
        {
            return _adapter.GetItemViewType(position) == 0 ? 1 : _spanCount;
        }
    }


    #region ScrollRecyclerView

    public class ScrollRecyclerView : RecyclerView
	{
		public ScrollRecyclerView (IntPtr javaReference, Android.Runtime.JniHandleOwnership transfer) : base (javaReference, transfer)
		{
		}

		public ScrollRecyclerView (Android.Content.Context context, Android.Util.IAttributeSet attrs, int defStyle) : base (context, attrs, defStyle)
		{
		}

		public ScrollRecyclerView (Android.Content.Context context, Android.Util.IAttributeSet attrs) : base (context, attrs)
		{
		}

		public ScrollRecyclerView (Android.Content.Context context) : base (context)
		{
		}

		public int GetVerticalScrollOffset ()
		{
			return ComputeVerticalScrollOffset ();
		}

		public int GetHorizontalScrollOffset ()
		{
			return ComputeHorizontalScrollOffset ();
		}
	}

    #endregion

    #region GridViewScrollListener

    public class GridViewScrollListener : RecyclerView.OnScrollListener
	{
        FormsGridView _gridView;

		ScrollRecyclerView _recyclerView;

		public GridViewScrollListener (FormsGridView gridView, ScrollRecyclerView recyclerView)
		{
			_gridView = gridView;
			_recyclerView = recyclerView;
		}

		public override void OnScrolled (RecyclerView recyclerView, int dx, int dy)
		{
			base.OnScrolled (recyclerView, dx, dy);
			_gridView.RaiseOnScroll (dy, _recyclerView.GetVerticalScrollOffset ());
			//Console.WriteLine (">>>>>>>>> {0},{1}", dy, _recyclerView.GetVerticalScrollOffset ());
		}
	}

    #endregion

    #region GridViewAdapter

    public class GridViewAdapter : RecyclerView.Adapter
    {
        RecyclerView _recyclerView;
        IEnumerable _items;

        /// <summary>
        /// Holds a reference for each group and associated global index.
        /// </summary>
        Dictionary<int, int> _groupIndexDictionary = new Dictionary<int, int>();

        private static PropertyInfo _platform;
        public static PropertyInfo PlatformProperty
        {
            get
            {
                return _platform ?? (
                    _platform = typeof(Element).GetProperty("Platform", BindingFlags.NonPublic | BindingFlags.Instance));
            }
        }

        DisplayMetrics _displayMetrics;

        FormsGridView _gridView;

        FormsGridView Element { get; set; }

        /// <summary>
        /// Contains the flatened collection of items.
        /// </summary>
        IEnumerable<object> _flattenedItems;

        public IEnumerable Items
        {
            get
            {
                return _items;
            }
            set
            {
                var oldColleciton = _items as INotifyCollectionChanged;
                if (oldColleciton != null)
                {
                    oldColleciton.CollectionChanged -= NewColleciton_CollectionChanged;
                }
                _items = value;
                var newColleciton = _items as INotifyCollectionChanged;
                if (newColleciton != null)
                {
                    newColleciton.CollectionChanged += NewColleciton_CollectionChanged;
                }
                NotifyDataSetChanged();

                //if (Items.OfType<IEnumerable>().Any() && _gridView.IsItemsSourceGrouped)
                //{
                //    _flattenedItems = Items.Cast<IEnumerable>().SelectMany((r =>
                //     {
                //         var childItems = r.Cast<object>().ToList();
                //         childItems.Insert(0, r);
                //         return childItems;
                //     }));
                //}
            }

        }

        void NewColleciton_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewItems != null)
                {
                    //TODO handle changes here, reload data etc.. if required.. 
                    //or brute force.. or something else
                    foreach (var item in e.NewItems)
                    {
                        var index = IndexOf(_items, item);
                        NotifyItemInserted(index);
                        
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        var index = IndexOf(_items, item);
                        NotifyItemRemoved(index);
                    }
                }
            }
            else
            {
                NotifyDataSetChanged();

            }
        }

        public GridViewAdapter(IEnumerable items, RecyclerView recyclerView, FormsGridView gridView, DisplayMetrics displayMetrics)
        {
            Items = items;
            _recyclerView = recyclerView;
            Element = gridView;
            _displayMetrics = displayMetrics;
            _gridView = gridView;
        }

        public class GridViewCell : RecyclerView.ViewHolder
        {
            public GridViewCellContainer ViewCellContainer { get; set; }

            public GridViewCell(GridViewCellContainer view) : base(view)
            {
                ViewCellContainer = view;
            }
        }

        public override int GetItemViewType(int position)
        {
            //Default type is item
            return _gridView.IsGroupingEnabled && _groupIndexDictionary.ContainsValue(position) ? 1 : 0;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var gridViewCell = viewType == 0 ?
                Element.ItemTemplate.CreateContent() as XamarinFormsGridView.Controls.FastGridCell :
                Element.GroupHeaderTemplate.CreateContent() as XamarinFormsGridView.Controls.FastGridCell;

            // reflection method of setting isplatformenabled property
            // We are going to re-set the Platform here because in some cases (headers mostly) its possible this is unset and
            // when the binding context gets updated the measure passes will all fail. By applying this here the Update call
            // further down will result in correct layouts.
            var p = PlatformProperty.GetValue(Element);
            PlatformProperty.SetValue(gridViewCell, p);

            var initialCellSize = new Xamarin.Forms.Size(Element.MinItemWidth, Element.RowHeight);
            var view = new GridViewCellContainer(parent.Context, gridViewCell, parent, initialCellSize);

            //Only add the click handler for items (not group headers.)
            if (viewType == 0)
            {
                view.Click += mMainView_Click;
            }

            var height = viewType == 0 ? Convert.ToInt32(Element.RowHeight) : Convert.ToInt32(gridViewCell.RenderHeight);
            var width = Convert.ToInt32(Element.MinItemWidth);
            var dpW = ConvertDpToPixels(width);
            var dpH = ConvertDpToPixels(height);

            //If there are no minimums then the view doesn't render at all.
            view.SetMinimumWidth(dpW);
            view.SetMinimumHeight(dpH);

            //If not set to match parent then the view doesn't stretch to fill. 
            //This is not necessary anymore with the plaform fix above.
            //view.LayoutParameters = new GridLayoutManager.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);

            //Wrap the view.
            GridViewCell myView = new GridViewCell(view);

            //return the view.
            return myView;
        }

        private int ConvertDpToPixels(float dpValue)
        {
            var pixels = (int)((dpValue) * _displayMetrics.Density);
            return pixels;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            GridViewCell myHolder = holder as GridViewCell;

            //var item = !_gridView.IsItemsSourceGrouped ?
            //    Items.Cast<object>().ElementAt(position) :
            //    Items.Cast<IEnumerable>().SelectMany((r =>
            //    {
            //        var childItems = r.Cast<object>().ToList();
            //        childItems.Insert(0, r);
            //        return childItems;
            //    })).ElementAt(position);

            var item = !_gridView.IsGroupingEnabled ? 
                Items.Cast<object>().ElementAt(position) : 
                _flattenedItems.ElementAt(position);

            myHolder.ViewCellContainer.Update(item);
        }

        void mMainView_Click(object sender, EventArgs e)
        {
            int position = _recyclerView.GetChildAdapterPosition((Android.Views.View)sender);

            var item = !_gridView.IsGroupingEnabled ?
                Items.Cast<object>().ElementAt(position) :
                _flattenedItems.ElementAt(position);

            Element.InvokeItemSelectedEvent(this, item);
        }

        public override int ItemCount
        {

            get
            {
                int count = 0;
                int group = 0;

                _groupIndexDictionary.Clear();

                if (_gridView.IsGroupingEnabled) // Items.Any(IEnumerable)
                {
                    _flattenedItems = Items.Cast<IEnumerable>().SelectMany((r =>
                    {
                        _groupIndexDictionary.Add(group++, count++);
                        var childItems = r.Cast<object>().ToList();
                        count += childItems.Count;
                        childItems.Insert(0, r);
                        return childItems;
                    }));

                    //Force execution.
                    count = _flattenedItems.Count();
                }
                else
                {
                    count = Items.Cast<object>().Count();
                }

                //foreach (var itemGroup in Items)
                //{
                //    //Store the group and global position (flattened index.)
                //    _groupIndexDictionary.Add(group++, count++);

                //    if (_gridView.IsItemsSourceGrouped && itemGroup is IEnumerable)
                //    {
                //        count += (itemGroup as IEnumerable).Cast<object>().Count();
                //    }
                //}

                return count;
            }

        }

        public static int IndexOf(IEnumerable collection, object element, IEqualityComparer comparer = null)
        {
            int i = 0;
            comparer = comparer ?? EqualityComparer<object>.Default;
            foreach (var currentElement in collection)
            {
                if (comparer.Equals(currentElement, element))
                {
                    return i;
                }

                i++;
            }

            return -1;
        }
    }

    #endregion

    #region GridViewCellContainer

    public class GridViewCellContainer : ViewGroup
    {
        global::Android.Views.View _parent;
        XamarinFormsGridView.Controls.FastGridCell _viewCell;
        ViewGroup _nativeView;
        Xamarin.Forms.Size _previousSize;
        bool _isLaidOut;

        public Element Element
        {
            get { return _viewCell; }
        }

        public GridViewCellContainer(Context context, XamarinFormsGridView.Controls.FastGridCell viewCell, global::Android.Views.View parent, Xamarin.Forms.Size initialCellSize) : base(context)
        {
            using (var h = new Handler(Looper.MainLooper))
            {
                h.Post(() =>
                {
                    _parent = parent;
                    _viewCell = viewCell;
                    _viewCell.PrepareCell(initialCellSize);
                    var renderer = Platform.GetRenderer(viewCell.View);
                    if (renderer == null)
                    {
                        renderer = Platform.CreateRenderer(viewCell.View);
                        Platform.SetRenderer(viewCell.View, renderer);

                    }
                    _nativeView = renderer.ViewGroup;
                    AddView(_nativeView);
                });
            }
        }

        public void Update(object bindingContext)
        {
            using (var h = new Handler(Looper.MainLooper))
            {
                h.Post(() => {
                    _viewCell.BindingContext = bindingContext;
                });
            }
        }

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            using (var h = new Handler(Looper.MainLooper))
            {
                h.Post(() =>
                {
                    double width = base.Context.FromPixels((double)(right - left));
                    double height = base.Context.FromPixels((double)(bottom - top));
                    var size = new Xamarin.Forms.Size(width, height);

                    var msw = MeasureSpec.MakeMeasureSpec(right - left, MeasureSpecMode.Exactly);
                    var msh = MeasureSpec.MakeMeasureSpec(bottom - top, MeasureSpecMode.Exactly);
                    _nativeView.Measure(msw, msh);
                    _nativeView.Layout(0, 0, right - left, bottom - top);

                    if (size != _previousSize || !_isLaidOut)
                    {
                        var layout = _viewCell.View as Layout<Xamarin.Forms.View>;

                        if (layout != null)
                        {
                            layout.Layout(new Rectangle(0, 0, width, height));
                            layout.ForceLayout();
                            FixChildLayouts(layout);
                            _isLaidOut = true;
                        }
                    }

                    _previousSize = size;
                });
            }
        }

        void FixChildLayouts(Layout<Xamarin.Forms.View> layout)
        {
            foreach (var child in layout.Children)
            {
                if (child is Layout<Xamarin.Forms.View>)
                {
                    ((Layout<Xamarin.Forms.View>)child).ForceLayout();
                    FixChildLayouts(child as Layout<Xamarin.Forms.View>);
                }
            }
        }
    }

    #endregion
}
