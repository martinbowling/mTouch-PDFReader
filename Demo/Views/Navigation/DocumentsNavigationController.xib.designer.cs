// ------------------------------------------------------------------------------
//  <autogenerated>
//      This code was generated by a tool.
//      Mono Runtime Version: 2.0.50727.1433
// 
//      Changes to this file may cause incorrect behavior and will be lost if 
//      the code is regenerated.
//  </autogenerated>
// ------------------------------------------------------------------------------

namespace mTouchPDFReader.Demo {
	
	
	// Base type probably should be MonoTouch.UIKit.UIViewController or subclass
	[MonoTouch.Foundation.Register("DocumentsNavigationController")]
	public partial class DocumentsNavigationController {
		
		private MonoTouch.UIKit.UINavigationBar __mt_view;
		
		#pragma warning disable 0169
		[MonoTouch.Foundation.Connect("view")]
		private MonoTouch.UIKit.UINavigationBar view {
			get {
				this.__mt_view = ((MonoTouch.UIKit.UINavigationBar)(this.GetNativeField("view")));
				return this.__mt_view;
			}
			set {
				this.__mt_view = value;
				this.SetNativeField("view", value);
			}
		}
	}
}
