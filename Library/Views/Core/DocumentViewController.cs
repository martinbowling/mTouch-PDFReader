//
// mTouch-PDFReader library
// DocumentViewController.cs (Document view controller implementation)
//
//  Author:
//       Alexander Matsibarov (macasun) <amatsibarov@gmail.com>
//
//  Copyright (c) 2012 Alexander Matsibarov
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.CoreGraphics;
using MonoTouch.UIKit;
using mTouchPDFReader.Library.Utils;
using mTouchPDFReader.Library.Interfaces;
using mTouchPDFReader.Library.Data.Objects;
using mTouchPDFReader.Library.XViews;
using mTouchPDFReader.Library.Views.Management;
using mTouchPDFReader.Library.Managers;

namespace mTouchPDFReader.Library.Views.Core
{
	public class DocumentViewController : UIViewController
	{			
		#region Constants		
		private const int MaxPageViewsCount = 3;
		private const float BarPaddingH = 5.0f;
		private const float BarPaddingV = 5.0f;
		private const float ToolbarHeight = 50.0f;
		private const float BottombarHeight = 45.0f;
		private const float FirstToolButtonLeft = 20.0f;
		private const float FirstToolButtonTop = 7.0f;
		private const float ToolButtonSize = 36.0f;
		private readonly SizeF PageNumberLabelSize = new SizeF(75.0f, 35.0f);		
		#endregion
		
		#region Fields			
		/// <summary>
		/// The document id.
		/// </summary>
		private int _DocumentId;

		/// <summary>
		/// The document name.
		/// </summary>
		private string _DocumentName;

		/// <summary>
		/// The document path.
		/// </summary>
		private string _DocumentPath;
			
		/// <summary>
		/// The book PageView controller.
		/// </summary>
		private UIPageViewController _BookPageViewController;

		/// <summary>
		/// The toolbar view.
		/// </summary>
		private UIView _Toolbar;
		
		/// <summary>
		/// The open page button.
		/// </summary>
		private UIButton _BtnNavigateToPage;
			
		/// <summary>
		/// The open note view button. 
		/// </summary>
		private UIButton _BtnNote;
		
		/// <summary>
		/// The open bookmarsks list view button.
		/// </summary>
		private UIButton _BtnBookmarksList;
		
		/// <summary>
		/// The open thumbs view button.
		/// </summary>
		private UIButton _BtnThumbs;
		
		/// <summary>
		/// The bottom bar view.
		/// </summary>
		private UIView _BottomBar;
		
		/// <summary>
		/// The slider view.
		/// </summary>
		private UISlider _Slider;
		
		/// <summary>
		/// The page number label.
		/// </summary>
		private UILabel _PageNumberLabel;
		#endregion
			
		#region Initialization
		/// <summary>
		/// Default.
		/// </summary>
		public DocumentViewController(int docId, string docName, string docPath) : base(null, null)
		{
			_DocumentId = docId;
			_DocumentName = docName;
			_DocumentPath = docPath;
		}
		#endregion
		
		#region UIViewController	
		/// <summary>
		/// Calls after the controller’s view did loaded into memory.
		/// </summary>
		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			// Load document
			PDFDocument.OpenDocument(_DocumentName, _DocumentPath);
			
			// Init View	
			Title = _DocumentName;
			View.BackgroundColor = UIColor.ScrollViewTexturedBackgroundColor;
	
			// Create toolbar
			if (MgrAccessor.OptionsMgr.Options.ToolbarVisible) {
				_Toolbar = CreateToolbar();
				if (_Toolbar != null) {
					View.AddSubview(_Toolbar);
				}
			}
			
			// Create bottom bar (with slider)
			if (MgrAccessor.OptionsMgr.Options.BottombarVisible) {
				_BottomBar = CreateBottomBar();
				if (_BottomBar != null) {
					View.AddSubview(_BottomBar);
				} else {
					if (_Slider != null) {
						_Slider.RemoveFromSuperview();
						_Slider.Dispose();
					}
					if (_PageNumberLabel != null) {
						_PageNumberLabel.RemoveFromSuperview();
						_PageNumberLabel.Dispose();
					}
				}
			}

			// Update Slider Max value
			UpdateSliderMaxValue();
			
			// Create the book PageView controller
			_BookPageViewController = new UIPageViewController(
				MgrAccessor.OptionsMgr.Options.PageTransitionStyle,
				MgrAccessor.OptionsMgr.Options.PageNavigationOrientation, 
				UIPageViewControllerSpineLocation.Min);		
			_BookPageViewController.View.Frame = GetBookPageViewFrameRect();
			_BookPageViewController.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
			_BookPageViewController.View.BackgroundColor = UIColor.GroupTableViewBackgroundColor;
			_BookPageViewController.GetNextViewController = GetNextPageViewController;
			_BookPageViewController.GetPreviousViewController = GetPreviousPageViewController;
			_BookPageViewController.GetSpineLocation = GetSpineLocation;
			_BookPageViewController.DidFinishAnimating += delegate(object sender, UIPageViewFinishedAnimationEventArgs e) {
				PageFinishedAnimation(e.Completed, e.Finished, e.PreviousViewControllers);
			};
			_BookPageViewController.SetViewControllers(
				new UIViewController[] { GetPageViewController(1) }, 
				UIPageViewControllerNavigationDirection.Forward, 
				false,
				s => ExecAfterOpenPageActions());	

			AddChildViewController(_BookPageViewController);
			_BookPageViewController.DidMoveToParentViewController(this);
			View.AddSubview(_BookPageViewController.View);
		}

		/// <summary>
		/// Calls after the controller’s view the did layout subviews.
		/// </summary>
		public override void ViewDidLayoutSubviews()
		{
			base.ViewDidLayoutSubviews();
			UpdatePageViewFrameRectAndZoomScale();
		}

		/// <summary>
		/// Calls the possibility rotate the view.
		/// </summary>
		public override bool ShouldAutorotateToInterfaceOrientation(UIInterfaceOrientation toInterfaceOrientation)
		{
			return true;
		}

		/// <summary>
		/// Calls when object is disposing.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			PDFDocument.CloseDocument();
			base.Dispose(disposing);
		}		
		#endregion
		
		#region UIPageViewController	
		/// <summary>
		/// Gets the previous PageView controller.
		/// </summary>
		/// <param name='pageController'>The book PageView controller.</param>
		/// <param name='referenceViewController'>The current (opened) PageView controller.</param>
		/// <returns>The previous page view controller.</returns>
		private UIViewController GetPreviousPageViewController(UIPageViewController pageController, UIViewController referenceViewController)
		{			
			var curPageCntr = referenceViewController as PageViewController;
			if (curPageCntr.PageNumber == 0) {
				return null;				
			} 
			int pageNumber = curPageCntr.PageNumber - 1;
			return GetPageViewController(pageNumber);	
		}

		/// <summary>
		/// Gets the next PageView controller.
		/// </summary>
		/// <param name='pageController'>The book PageView controller.</param>
		/// <param name='referenceViewController'>The current (opened) PageView controller.</param>
		/// <returns>The next page view controller.</returns>
		private UIViewController GetNextPageViewController(UIPageViewController pageController, UIViewController referenceViewController)
		{			
			var curPageCntr = referenceViewController as PageViewController;
			if (curPageCntr.PageNumber == (PDFDocument.PageCount)) {				
				return _BookPageViewController.SpineLocation == UIPageViewControllerSpineLocation.Mid
					? _GetEmptyPageContentVC()
					: null;	
			} else if (curPageCntr.PageNumber == -1) { 
				return null;
			}
			int pageNumber = curPageCntr.PageNumber + 1;
			return GetPageViewController(pageNumber);	
		}	

		/// <summary>
		/// Gets the spine location.
		/// </summary>
		/// <param name='pageController'>The book PageView controller.</param>
		/// <param name='orientation'>The device orientation.</param>
		/// <returns>The spine location.</returns>
		private UIPageViewControllerSpineLocation GetSpineLocation(UIPageViewController pageViewController, UIInterfaceOrientation orientation)
		{
			var currentPageVC = _GetCurrentPageContentVC();
			pageViewController.DoubleSided = false;
			pageViewController.SetViewControllers(new UIViewController[] { currentPageVC }, UIPageViewControllerNavigationDirection.Forward, true, s => { });
			return UIPageViewControllerSpineLocation.Min;
		}

		/// <summary>
		/// Calls after finished open page animation.
		/// </summary>
		/// <param name='completed'>If set to <c>true</c> completed.</param>
		/// <param name='finished'>If set to <c>true</c> finished.</param>
		/// <param name='previousViewControllers'>Previous view controllers.</param>
		private void PageFinishedAnimation(bool completed, bool finished, UIViewController[] previousViewControllers)
		{
			if (completed) {
				ExecAfterOpenPageActions();
			}
		}
		#endregion
				
		#region UI Logic		
		/// <summary>
		/// Presents popover
		/// </summary>
		private void PresentPopover(UIViewControllerWithPopover viewCtrl, RectangleF frame)
		{
			var popoverController = new UIPopoverController(viewCtrl);
			viewCtrl.PopoverController = popoverController;
			popoverController.PresentFromRect(frame, View, UIPopoverArrowDirection.Any, true);
		}	
		
		/// <summary>
		/// Gets current device orientation
		/// </summary>
		/// <returns>Current device orientation</returns>
		private UIInterfaceOrientation GetDeviceOrientation()
		{
			switch (UIDevice.CurrentDevice.Orientation) {
				case UIDeviceOrientation.LandscapeLeft:
					return UIInterfaceOrientation.LandscapeLeft;
				case UIDeviceOrientation.LandscapeRight:
					return UIInterfaceOrientation.LandscapeRight;
				case UIDeviceOrientation.Portrait:
					return UIInterfaceOrientation.Portrait;
				case UIDeviceOrientation.PortraitUpsideDown:
					return UIInterfaceOrientation.PortraitUpsideDown;
			}
			return UIInterfaceOrientation.Portrait;
		}
		
		/// <summary>
		/// Update slider max value
		/// </summary>
		private void UpdateSliderMaxValue()
		{
			if (_Slider != null) {
				_Slider.MaxValue = PDFDocument.PageCount;
			}
		}		
		
		/// <summary>
		/// Creates toolbar
		/// </summary>
		/// <returns>Toolbar view</returns>
		protected virtual UIView CreateToolbar()
		{
			// Create toolbar
			var toolBarFrame = View.Bounds;
			toolBarFrame.X += BarPaddingH;
			toolBarFrame.Y += BarPaddingV;
			toolBarFrame.Width -= BarPaddingH * 2;
			toolBarFrame.Height = ToolbarHeight;
			var toolBar = new UIXToolbarView(toolBarFrame, 0.92f, 0.32f, 0.8f);
			toolBar.AutoresizingMask = UIViewAutoresizing.FlexibleBottomMargin | UIViewAutoresizing.FlexibleWidth;
			
			// Create toolbar buttons
			var btnFrame = new RectangleF(FirstToolButtonLeft, FirstToolButtonTop, ToolButtonSize, ToolButtonSize);
			CreateToolbarButton(toolBar, ref btnFrame, "Images/Toolbar/NavigateToFirst48.png", delegate {
				OpenFirstPage();
			});
			CreateToolbarButton(toolBar, ref btnFrame, "Images/Toolbar/NavigateToPrior48.png", delegate {
				OpenPriorPage();
			});
			_BtnNavigateToPage = CreateToolbarButton(toolBar, ref btnFrame, "Images/Toolbar/NavigateToPage48.png", delegate {
				var view = new GotoPageViewController(p => OpenDocumentPage((int)p));
				PresentPopover(view, _BtnNavigateToPage.Frame);
			});
			CreateToolbarButton(toolBar, ref btnFrame, "Images/Toolbar/NavigateToNext48.png", delegate {
				OpenNextPage();
			});
			CreateToolbarButton(toolBar, ref btnFrame, "Images/Toolbar/NavigateToLast48.png", delegate {
				OpenLastPage();
			});
			CreateToolbarButton(toolBar, ref btnFrame, "Images/Toolbar/ZoomOut48.png", delegate {
				ZoomOut();
			});
			CreateToolbarButton(toolBar, ref btnFrame, "Images/Toolbar/ZoomIn48.png", delegate {
				ZoomIn(); 
			});
			if (MgrAccessor.OptionsMgr.Options.NoteBtnVisible) {
				_BtnNote = CreateToolbarButton(toolBar, ref btnFrame, "Images/Toolbar/Note48.png", delegate {
					var note = MgrAccessor.DocumentNoteMgr.Load(_DocumentId);
					var view = new NoteViewController(note, null);
					PresentPopover(view, _BtnNote.Frame);
				});
			}
			if (MgrAccessor.OptionsMgr.Options.BookmarksBtnVisible) {
				_BtnBookmarksList = CreateToolbarButton(toolBar, ref btnFrame, "Images/Toolbar/BookmarksList48.png", delegate {
					var bookmarks = MgrAccessor.DocumentBookmarkMgr.LoadList(_DocumentId);
					var view = new BookmarksViewController(_DocumentId, bookmarks, PDFDocument.CurrentPageNumber, p => OpenDocumentPage((int)p));
					PresentPopover(view, _BtnBookmarksList.Frame);
				});
			}
			if (MgrAccessor.OptionsMgr.Options.ThumbsBtnVisible) {
				_BtnThumbs = CreateToolbarButton(toolBar, ref btnFrame, "Images/Toolbar/Thumbs32.png", delegate {
					var view = new ThumbsViewController(View.Bounds.Width, p => {
						OpenDocumentPage((int)p); });
					PresentPopover(view, _BtnThumbs.Frame);
					view.InitThumbs();
				});
			}
			return toolBar;
		}
		
		/// <summary>
		/// Creates toolbar button
		/// </summary>
		/// <param name="toolbar">Toolbar view</param>
		/// <param name="frame">Button frame</param>
		/// <param name="imageUrl">Image button url</param>
		/// <param name="action">Assoutiated button action</param>
		/// <returns>Button view</returns>
		protected virtual UIButton CreateToolbarButton(UIView toolbar, ref RectangleF frame, string imageUrl, Action action)
		{
			var btn = new UIButton(frame);
			btn.SetImage(UIImage.FromFile(imageUrl), UIControlState.Normal);
			btn.TouchUpInside += delegate { 
				action();
			};
			toolbar.AddSubview(btn);
			frame.Offset(44, 0);
			return btn;
		}
		
		/// <summary>
		/// Creates bottom bar
		/// </summary>
		/// <returns>Bottom bar view</returns>
		protected virtual UIView CreateBottomBar()
		{
			// Create bottom bar	
			var bottomBarFrame = View.Bounds;
			bottomBarFrame.X += BarPaddingH;
			bottomBarFrame.Y = bottomBarFrame.Size.Height - BarPaddingV - BottombarHeight;
			bottomBarFrame.Width -= BarPaddingH * 2;
			bottomBarFrame.Height = BottombarHeight;
			var bottomBar = new UIXToolbarView(bottomBarFrame, 0.92f, 0.32f, 0.8f);
			bottomBar.AutoresizingMask = UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleWidth;
			
			// Create slider
			float sliderWidth = bottomBarFrame.Width - 15;
			if (MgrAccessor.OptionsMgr.Options.PageNumberVisible) {
				sliderWidth -= PageNumberLabelSize.Width;
			}
			var pageSliderFrame = new RectangleF(5, 10, sliderWidth, 20);
			_Slider = new UISlider(pageSliderFrame);
			_Slider.MinValue = 1;
			_Slider.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			_Slider.ValueChanged += delegate {
				if (_PageNumberLabel != null) {
					_PageNumberLabel.Text = string.Format(@"{0}/{1}", (int)_Slider.Value, PDFDocument.PageCount);
				}
			};
			_Slider.TouchUpInside += delegate(object sender, EventArgs e) {
				OpenDocumentPage((int)_Slider.Value);
			};
			bottomBar.AddSubview(_Slider);

			// Create page number view		
			if (MgrAccessor.OptionsMgr.Options.PageNumberVisible) {
				var pageNumberViewFrame = new RectangleF(pageSliderFrame.Width + 10, 5, PageNumberLabelSize.Width, PageNumberLabelSize.Height);
				var pageNumberView = new UIView(pageNumberViewFrame);
				pageNumberView.AutosizesSubviews = false;
				pageNumberView.UserInteractionEnabled = false;
				pageNumberView.AutoresizingMask = UIViewAutoresizing.FlexibleLeftMargin;
				pageNumberView.BackgroundColor = UIColor.FromWhiteAlpha(0.4f, 0.5f);
				pageNumberView.Layer.CornerRadius = 5.0f;
				pageNumberView.Layer.ShadowOffset = new SizeF(0.0f, 0.0f);
				pageNumberView.Layer.ShadowPath = UIBezierPath.FromRect(pageNumberView.Bounds).CGPath;
				pageNumberView.Layer.ShadowRadius = 2.0f;
				pageNumberView.Layer.ShadowOpacity = 1.0f;
				// Create page number label
				var pageNumberLabelFrame = RectangleFExtensions.Inset(pageNumberView.Bounds, 4.0f, 2.0f);
				_PageNumberLabel = new UILabel(pageNumberLabelFrame);
				_PageNumberLabel.AutosizesSubviews = false;
				_PageNumberLabel.AutoresizingMask = UIViewAutoresizing.None;
				_PageNumberLabel.TextAlignment = UITextAlignment.Center;
				_PageNumberLabel.BackgroundColor = UIColor.Clear;
				_PageNumberLabel.TextColor = UIColor.White;
				_PageNumberLabel.Font = UIFont.SystemFontOfSize(16.0f);
				_PageNumberLabel.ShadowOffset = new SizeF(0.0f, 1.0f);
				_PageNumberLabel.ShadowColor = UIColor.Black;
				_PageNumberLabel.AdjustsFontSizeToFitWidth = true;
				_PageNumberLabel.MinimumFontSize = 12.0f;
				pageNumberView.AddSubview(_PageNumberLabel);
				bottomBar.AddSubview(pageNumberView);
			}			
			return bottomBar;
		}
		#endregion

		#region PDFDocument logic
		/// <summary>
		/// Gets the book PageView frame rect.
		/// </summary>
		/// <returns>The PageView frame rect.</returns>
		private RectangleF GetBookPageViewFrameRect()
		{
			var rect = View.Bounds;
			if (_Toolbar != null) {
				rect.Y = _Toolbar.Frame.Bottom;
				rect.Height -= _Toolbar.Bounds.Height + BarPaddingV;
			}
			if (_BottomBar != null) {
				rect.Height -= _BottomBar.Bounds.Height + BarPaddingV;
			}
			return rect;
		}
		
		/// <summary>
		/// Gets the PageView frame rect.
		/// </summary>
		/// <returns>The PageView frame rect.</returns>
		private RectangleF GetPageViewFrameRect()
		{
			return _BookPageViewController.View.Bounds; 
		}

		/// <summary>
		/// Updates the PageView frame rect and zoom scale.
		/// </summary>
		private void UpdatePageViewFrameRectAndZoomScale()
		{
			foreach (var pageVC in _BookPageViewController.ViewControllers.Cast<PageViewController>()) {
				if (pageVC != null && pageVC.IsNotEmptyPage) {
					pageVC.PageView.Frame = GetPageViewFrameRect();
					pageVC.PageView.UpdateMinimumMaximumZoom();
					pageVC.PageView.ZoomReset();
				}
			}
		}		

		/// <summary>
		/// Gets the get current PageView controller.
		/// </summary>
		/// <returns>The current PageView controller.</returns>
		private PageViewController _GetCurrentPageContentVC()
		{
			return (PageViewController)_BookPageViewController.ViewControllers[0];
		}

		/// <summary>
		/// Gets the empty PageView controller.
		/// </summary>
		/// <returns>The empty PageView controller.</returns>
		private PageViewController _GetEmptyPageContentVC()
		{
			return new PageViewController(GetPageViewFrameRect(), -1);
		}

		/// <summary>
		/// Gets the PageView controller by <see cref="pageNumber"/>.
		/// </summary>
		/// <param name='pageNumber'>The page number.</param>
		/// <returns>The PageView controller.</returns>
		private PageViewController GetPageViewController(int pageNumber)
		{
			if (!PDFDocument.DocumentHasLoaded || pageNumber < 1 || pageNumber > PDFDocument.PageCount || pageNumber == PDFDocument.CurrentPageNumber) {
				return null;
			}
			return new PageViewController(GetPageViewFrameRect(), pageNumber);
		}

		/// <summary>
		/// Gets the page increment value.
		/// </summary>
		/// <returns>The page increment value.</returns>
		private int _GetPageIncValue()
		{
			return _BookPageViewController.SpineLocation == UIPageViewControllerSpineLocation.Mid ? 2 : 1;
		}

		/// <summary>
		/// Opens the document page.
		/// </summary>
		/// <param name="pageNumber">The document page number.</param>
		public virtual void OpenDocumentPage(int pageNumber)
		{
			if (pageNumber < 1 || pageNumber > PDFDocument.PageCount) {
				return;
			}

			// Calc navigation direction
			var navDirection = pageNumber < PDFDocument.CurrentPageNumber  
				? UIPageViewControllerNavigationDirection.Reverse
				: UIPageViewControllerNavigationDirection.Forward;

			// Create single PageView
			var pageVC = GetPageViewController(pageNumber);

			// Open page
			_BookPageViewController.SetViewControllers(
				new UIViewController[] { pageVC }, 
				navDirection, 
				true, 
				s => { ExecAfterOpenPageActions(); });
		}		

		/// <summary>
		/// Executes the after open page actions.
		/// </summary>
		private void ExecAfterOpenPageActions()
		{
			// Set current page
			PDFDocument.CurrentPageNumber = _GetCurrentPageContentVC().PageNumber;	
			
			// Update PageNumber label
			if (_PageNumberLabel != null) {
				_PageNumberLabel.Text = string.Format(@"{0}/{1}", PDFDocument.CurrentPageNumber, PDFDocument.PageCount);
			}
			
			// Update slider position
			if (_Slider != null) {
				_Slider.Value = PDFDocument.CurrentPageNumber;
			}
		}
		#endregion
		
		#region Events			
		/// <summary>
		/// Opens first page of the document
		/// </summary>
		protected virtual void OpenFirstPage()
		{
			OpenDocumentPage(1);
		}
		
		/// <summary>
		/// Opens prior page of the document
		/// </summary>
		protected virtual void OpenPriorPage()
		{
			OpenDocumentPage(PDFDocument.CurrentPageNumber - _GetPageIncValue());
		}
		
		/// <summary>
		/// Opens next page of the document
		/// </summary>
		protected virtual void OpenNextPage()
		{
			OpenDocumentPage(PDFDocument.CurrentPageNumber + _GetPageIncValue());
		}
		
		/// <summary>
		/// Opens last page of the document
		/// </summary>
		protected virtual void OpenLastPage()
		{
			OpenDocumentPage(PDFDocument.PageCount);
		}
		
		/// <summary>
		/// Zooming out
		/// </summary>
		protected virtual void ZoomOut()
		{
			var pageVC = _GetCurrentPageContentVC();
			if (PDFDocument.DocumentHasLoaded && pageVC.IsNotEmptyPage) {
				pageVC.PageView.ZoomDecrement();
			}
		}
		
		/// <summary>
		/// Zooming in
		/// </summary>
		protected virtual void ZoomIn()
		{
			var pageVC = _GetCurrentPageContentVC();
			if (PDFDocument.DocumentHasLoaded && pageVC.IsNotEmptyPage) {
				pageVC.PageView.ZoomIncrement();
			}
		}		
		#endregion
	}
}