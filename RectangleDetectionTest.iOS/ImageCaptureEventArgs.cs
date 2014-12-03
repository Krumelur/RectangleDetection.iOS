using System;
using CoreGraphics;

namespace RectangleDetectionTest.iOS
{
	/// <summary>
	/// Image capture event arguments.
	/// </summary>
	public class ImageCaptureEventArgs : EventArgs
	{
		public ImageCaptureEventArgs ()
		{
		}

		/// <summary>
		/// The image that was captured.
		/// </summary>
		/// <value>The image.</value>
		public CGImage Image { get; set; }

		/// <summary>
		/// Timestamp.
		/// </summary>
		/// <value>The captured at.</value>
		public DateTime CapturedAt { get; set; }
	}
}

