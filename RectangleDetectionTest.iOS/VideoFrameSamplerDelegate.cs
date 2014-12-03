using System;
using AVFoundation;
using CoreMedia;
using CoreVideo;
using CoreGraphics;

namespace RectangleDetectionTest.iOS
{
	/// <summary>
	/// Implementation of AVCaptureVideoDataOutputSampleBufferDelegate that notifies via an event about new grabbed images.
	/// </summary>
	public class VideoFrameSamplerDelegate : AVCaptureVideoDataOutputSampleBufferDelegate 
	{ 	
		public VideoFrameSamplerDelegate ()
		{
		}

		/// <summary>
		/// Fires if a new image was captures.
		/// </summary>
		public EventHandler<ImageCaptureEventArgs> ImageCaptured;

		/// <summary>
		/// Trigger the ImageCaptured event.
		/// </summary>
		/// <param name="image">Image.</param>
		void OnImageCaptured( CGImage image )
		{
			var handler = this.ImageCaptured;
			if ( handler != null )
			{
				var args = new ImageCaptureEventArgs();
				args.Image = image;
				args.CapturedAt = DateTime.Now;
				handler( this, args );
			}
		}

		/// <summary>
		/// Fires if an error occurs.
		/// </summary>
		public EventHandler<CaptureErrorEventArgs> CaptureError;

		/// <summary>
		/// Triggers the CaptureError event.
		/// </summary>
		/// <param name="errorMessage">Error message.</param>
		/// <param name="ex">Ex.</param>
		 void OnCaptureError( string errorMessage, Exception ex )
		{
			var handler = this.CaptureError;
			if ( handler != null )
			{
				try
				{
					var args = new CaptureErrorEventArgs();	
					args.ErrorMessage = errorMessage;
					args.Exception = ex;
					handler(this, args);
				}
				catch(Exception fireEx)
				{
					Console.WriteLine ("Failed to fire CaptureError event: " + fireEx);
				}
			}
		}

		/// <summary>
		/// Gets called by the video session if a new image is available.
		/// </summary>
		/// <param name="captureOutput">Capture output.</param>
		/// <param name="sampleBuffer">Sample buffer.</param>
		/// <param name="connection">Connection.</param>
		public override void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
		{
			try 
			{
				// Convert the raw image data into a CGImage.
				using(CGImage sourceImage = GetImageFromSampleBuffer(sampleBuffer))
				{
					this.OnImageCaptured( sourceImage );
				}

				// Make sure AVFoundation does not run out of buffers
				sampleBuffer.Dispose ();

			} 
			catch (Exception ex)
			{
				string errorMessage =  string.Format("Failed to process image capture: {0}", ex);
				this.OnCaptureError( errorMessage, ex );
			}
		}

		/// <summary>
		/// Converts raw image data from a CMSampleBugger into a CGImage.
		/// </summary>
		/// <returns>The image from sample buffer.</returns>
		/// <param name="sampleBuffer">Sample buffer.</param>
		static CGImage GetImageFromSampleBuffer (CMSampleBuffer sampleBuffer)
		{
			// Get the CoreVideo image
			using (var pixelBuffer = sampleBuffer.GetImageBuffer() as CVPixelBuffer)
			{
				pixelBuffer.Lock (0);

				var baseAddress = pixelBuffer.BaseAddress;
				int bytesPerRow = (int)pixelBuffer.BytesPerRow;
				int width = (int)pixelBuffer.Width;
				int height = (int)pixelBuffer.Height;
				var flags = CGBitmapFlags.PremultipliedFirst | CGBitmapFlags.ByteOrder32Little;

				// Create a CGImage on the RGB colorspace from the configured parameter above
				using (var cs = CGColorSpace.CreateDeviceRGB ())
				using (var context = new CGBitmapContext (baseAddress, width, height, 8, bytesPerRow, cs, (CGImageAlphaInfo) flags))
				{
					var cgImage = context.ToImage ();
					pixelBuffer.Unlock (0);
					return cgImage;
				}
			}
		}
	}
}
