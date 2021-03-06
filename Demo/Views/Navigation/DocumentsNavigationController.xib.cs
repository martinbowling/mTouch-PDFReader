//
// mTouch-PDFReader demo
// DocumentsNavigationController.cs
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
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace mTouchPDFReader.Demo
{
	public partial class DocumentsNavigationController : UINavigationController
	{
		#region Constructors
		public DocumentsNavigationController(IntPtr handle) : base(handle)
		{
		}

		[Export("initWithCoder:")]
		public DocumentsNavigationController(NSCoder coder) : base(coder)
		{
		}

		public DocumentsNavigationController() : base("DocumentsNavigationController", null)
		{
		}		
		#endregion	
		
		/// <summary>
		/// Calls when view has loaded
		/// </summary>
		public override void ViewDidLoad()
		{
			base.ViewDidLoad();
			NavigationBar.BarStyle = UIBarStyle.Black;
			PushViewController(new DocumentsTableController(), false);
		}
		
		/// <summary>
		/// Called when permission is shought to rotate
		/// </summary>
		public override bool ShouldAutorotateToInterfaceOrientation(UIInterfaceOrientation toInterfaceOrientation)
		{
			return true;
		}
	}
}

