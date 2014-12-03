using System;

namespace RectangleDetectionTest.iOS
{
	/// <summary>
	/// Capture error event arguments.
	/// </summary>
	public class CaptureErrorEventArgs : EventArgs
	{
		public CaptureErrorEventArgs ()
		{
		}

		/// <summary>
		/// Gets or sets the error message.
		/// </summary>
		/// <value>The error message.</value>
		public string ErrorMessage { get; set; }

		/// <summary>
		/// The exception.
		/// </summary>
		public Exception Exception { get; set; }
	}
}

