using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Drawing;
using MonoTouch.ObjCRuntime;

using PSPDFKit;


namespace PSPDFCatalog
{
	public class CustomAnnotationProvider : NSObject, IPSPDFAnnotationProvider
	{
		NSTimer timer;
		List<PSPDFAnnotation> annotations;
		static readonly Random rnd = new Random ();

		// MUST HAVE ctor when Subclassing!!! It will crash otherwise.
		public CustomAnnotationProvider (IntPtr handle) : base (handle)
		{
		}

		public CustomAnnotationProvider (int pages) : base ()
		{
			annotations = new PSPDFAnnotation[pages].ToList ();

			// add timer in a way so it works while we're dragging pages
			timer = NSTimer.CreateTimer (1, this, new Selector ("timerFired:"), null, true);
			NSRunLoop.Current.AddTimer (timer, NSRunLoopMode.Common); 
		}

		// You must mannually Export OPTIONAL Messages/Properties from the PSPDFAnnotationProvider Protocol (aka IPSPDFAnnotationProvider)
		IPSPDFAnnotationProviderChangeNotifier providerDelegate;
		[Export]
		public IPSPDFAnnotationProviderChangeNotifier ProviderDelegate {
			get {
				return providerDelegate;
			}
			set {
				if (value != providerDelegate) {
					providerDelegate = value;

					// null out timer to allow object to deallocate itself.
					if (providerDelegate == null) {
						timer.Invalidate ();
						timer = null;
					}
				}
			}
		}

		public PSPDFAnnotation[] AnnotationsForPage (uint page)
		{
			// it's important that this method is:
			// - fast
			// - thread safe
			// - and caches annotations (don't always create new objects!)
			lock (this) {
				if (annotations[(int)page] == null) {
					// create new note annotation and add it to the dict.
					var documentProvider = ProviderDelegate.ParentDocumentProvider ();
					var pageInfo = documentProvider.Document.PageInfoForPage (page);
					var noteAnnotation = new PSPDFNoteAnnotation () {
						Page = page,
						DocumentProvider = documentProvider,
						Contents = string.Format ("Annotation from the custom annotationProvider for page {0}.", page + 1),
						// place it top left (PDF coordinate space starts from bottom left)
						BoundingBox = new RectangleF (100, pageInfo.RotatedPageRect.Size.Height - 100, 32, 32),
						Editable = false
					};
					annotations [(int)page] = noteAnnotation;
				}
			}
			return new PSPDFAnnotation[] { annotations[(int)page] };
		}

		[Export ("timerFired:")]
		void TimerFired (NSTimer timer)
		{
			UIColor color = RandomColor ();
			lock (this) {
				foreach (var annotation in annotations)
					if (annotation != null)
						annotation.Color = color;
				ProviderDelegate.UpdateAnnotations (annotations.Where (a => a != null).ToArray ()  , true);				
			}
		}

		static UIColor RandomColor () 
		{
			return UIColor.FromRGBA (rnd.Next (0, 255), rnd.Next (0,255), rnd.Next(0,255), 1);
		}
	}
}

