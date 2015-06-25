using System;
using UIKit;
using CoreImage;
using CoreGraphics;
using Foundation;
using AVFoundation;
using CoreFoundation;
using CoreVideo;
using CoreMedia;
using System.Collections.Generic;

namespace RectangleDetectionTest.iOS
{
	/// <summary>
	/// Shows a raw view of the video stream captured from the camera. Adds to smaller views
	/// which shows detected QR code and perepsctive corrected versions of the image.
	/// </summary>
	public class QrDetectController : UIViewController
	{
		/// <summary>
		/// Defines with how many frames per second the detection is performed.
		/// </summary>
		const int DETECTION_FPS = 20;

		/// <summary>
		/// Defines the width of the preview windows that show detected codes.
		/// </summary>
		const float PREVIEW_VIEW_WIDTH = 160f;

		/// <summary>
		/// Defines the height of the preview windows that show detected codes.
		/// </summary>
		const float PREVIEW_VIEW_HEIGHT = 100f;

		public QrDetectController ()
		{
		}

		DispatchQueue sessionQueue;
		VideoFrameSamplerDelegate sampleBufferDelegate;
		AVCaptureVideoPreviewLayer videoLayer;
		UIImageView imageViewOverlay;
		UIImageView imageViewPerspective;
		CIDetector detector;
		UILabel mainWindowLbl;
		UILabel detectionWindowLbl;
		UILabel perspectiveWindowLbl;

		NSMutableDictionary videoSettingsDict;

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			this.View.BackgroundColor = UIColor.White;

			NSError error;

			// Setup detector options.
			var options = new CIDetectorOptions {
				TrackingEnabled = true
			};

			// Create a QR detector. Note that you can also create QR detector or a face detector.
			// Most of this code will also work with other detectors (like streaming to a preview layer and grabbing images).
			this.detector = CIDetector.CreateQRDetector(context: null, detectorOptions: options);

			// Create the session. The AVCaptureSession is the managing instance of the whole video handling.
			var captureSession = new AVCaptureSession ();

			// Find a suitable AVCaptureDevice for video input.
			var device = AVCaptureDevice.DefaultDeviceWithMediaType(AVMediaType.Video);
			if (device == null)
			{
				// This will not work on the iOS Simulator - there is no camera. :-)
				throw new InvalidProgramException ("Failed to get AVCaptureDevice for video input!");
			}

			// Create a device input with the device and add it to the session.
			var videoInput = AVCaptureDeviceInput.FromDevice (device, out error);
			if (videoInput == null)
			{
				throw new InvalidProgramException ("Failed to get AVCaptureDeviceInput from AVCaptureDevice!");
			}

			// Let session read from the input, this is our source.
			captureSession.AddInput (videoInput);

			// Create output for the video stream. This is the destination.
			var videoOutput = new AVCaptureVideoDataOutput () {
				AlwaysDiscardsLateVideoFrames = true
			};

			// Define the video format we want to use. Note that Xamarin exposes the CompressedVideoSetting and UncompressedVideoSetting 
			// properties on AVCaptureVideoDataOutput un Unified API, but I could not get these to work. The VideoSettings property is deprecated,
			// so I use the WeakVideoSettings instead which takes an NSDictionary as input.
			this.videoSettingsDict = new NSMutableDictionary ();
			this.videoSettingsDict.Add (CVPixelBuffer.PixelFormatTypeKey, NSNumber.FromUInt32((uint)CVPixelFormatType.CV32BGRA));
			videoOutput.WeakVideoSettings = this.videoSettingsDict;

			// Create a delegate to report back to us when an image has been captured.
			// We want to grab the camera stream and feed it through a AVCaptureVideoDataOutputSampleBufferDelegate
			// which allows us to get notified if a new image is availeble. An implementation of that delegate is VideoFrameSampleDelegate in this project.
			this.sampleBufferDelegate = new VideoFrameSamplerDelegate ();

			// Processing happens via Grand Central Dispatch (GCD), so we need to provide a queue.
			// This is pretty much like a system managed thread (see: http://zeroheroblog.com/ios/concurrency-in-ios-grand-central-dispatch-gcd-dispatch-queues).
			this.sessionQueue =  new DispatchQueue ("AVSessionQueue");

			// Assign the queue and the delegate to the output. Now all output will go through the delegate.
			videoOutput.SetSampleBufferDelegate(this.sampleBufferDelegate, this.sessionQueue);

			// Add output to session.
			captureSession.AddOutput(videoOutput);

			// We also want to visualize the input stream. The raw stream can be fed into an AVCaptureVideoPreviewLayer, which is a subclass of CALayer.
			// A CALayer can be added to a UIView. We add that layer to the controller's main view.
			var layer = this.View.Layer;
			this.videoLayer = AVCaptureVideoPreviewLayer.FromSession (captureSession);
			this.videoLayer.Frame = layer.Bounds;
			layer.AddSublayer (this.videoLayer);

			// All setup! Start capturing!
			captureSession.StartRunning ();

			// This is just for information and allows you to get valid values for the detection framerate. 
			Console.WriteLine ("Available capture framerates:");
			var rateRanges = device.ActiveFormat.VideoSupportedFrameRateRanges;
			foreach (var r in rateRanges)
			{
				Console.WriteLine (r.MinFrameRate + "; " + r.MaxFrameRate + "; " + r.MinFrameDuration + "; " + r.MaxFrameDuration);
			}

			// Configure framerate. Kind of weird way of doing it but the only one that works.
			device.LockForConfiguration (out error);
			// CMTime constructor means: 1 = one second, DETECTION_FPS = how many samples per unit, which is 1 second in this case.
			device.ActiveVideoMinFrameDuration = new CMTime(1, DETECTION_FPS);
			device.ActiveVideoMaxFrameDuration = new CMTime(1, DETECTION_FPS);
			device.UnlockForConfiguration ();

			// Put a small image view at the top left that shows the live image with the detected code(s).
			this.imageViewOverlay = new UIImageView
			{ 
				ContentMode = UIViewContentMode.ScaleAspectFit,
				BackgroundColor = UIColor.Gray
			};
			this.imageViewOverlay.Layer.BorderColor = UIColor.Red.CGColor;
			this.imageViewOverlay.Layer.BorderWidth = 3f;
			this.Add (this.imageViewOverlay);

			// Put another image view top right that shows the image with perspective correction.
			this.imageViewPerspective = new UIImageView
			{ 
				ContentMode = UIViewContentMode.ScaleAspectFit,
				BackgroundColor = UIColor.Gray
			};
			this.imageViewPerspective.Layer.BorderColor = UIColor.Red.CGColor;
			this.imageViewPerspective.Layer.BorderWidth = 3f;
			this.Add (this.imageViewPerspective);

			// Add some lables for information.
			this.mainWindowLbl = new UILabel
			{
				Text = "Live stream from camera. Point camera to a QR Code.",
				TextAlignment = UITextAlignment.Center
			};
			this.Add (this.mainWindowLbl);

			this.detectionWindowLbl = new UILabel
			{
				Text = "Detected QR code overlay",
				TextAlignment = UITextAlignment.Center
			};
			this.Add (this.detectionWindowLbl);

			this.perspectiveWindowLbl = new UILabel
			{
				Text = "Perspective corrected",
				TextAlignment = UITextAlignment.Center
			};
			this.Add (this.perspectiveWindowLbl);
		}

		public override void ViewWillAppear (bool animated)
		{
			base.ViewWillAppear (animated);

			this.imageViewOverlay.Frame = new CGRect (20, 20, PREVIEW_VIEW_WIDTH, PREVIEW_VIEW_HEIGHT);
			this.imageViewPerspective.Frame = new CGRect (this.View.Bounds.Width - PREVIEW_VIEW_WIDTH - 20, 20, PREVIEW_VIEW_WIDTH, PREVIEW_VIEW_HEIGHT);


			this.mainWindowLbl.Frame = new CGRect (20, this.View.Bounds.Height - 30, this.View.Bounds.Width - 40, 20);
			this.detectionWindowLbl.Frame = new CGRect (this.imageViewOverlay.Frame.X, this.imageViewOverlay.Frame.Bottom + 5, this.imageViewOverlay.Frame.Width, 20);
			this.perspectiveWindowLbl.Frame = new CGRect (this.imageViewPerspective.Frame.X, this.imageViewPerspective.Frame.Bottom + 5, this.imageViewPerspective.Frame.Width, 20);

			// Wire up event handlers.
			this.sampleBufferDelegate.ImageCaptured += this.HandleImageCaptured;
			this.sampleBufferDelegate.CaptureError += this.HandleImageCaptureError;
		}

		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);
			// Adjust the rotation of the vide layer. It does not rotate automatically. Also see WillRotate() method.
			this.videoLayer.Connection.VideoOrientation = ConvertUiOrientation (UIApplication.SharedApplication.StatusBarOrientation);
		}

		public override void ViewWillDisappear (bool animated)
		{
			base.ViewWillDisappear (animated);

			this.sampleBufferDelegate.ImageCaptured -= this.HandleImageCaptured;
			this.sampleBufferDelegate.CaptureError -= this.HandleImageCaptureError;
		}


		/// <summary>
		/// Gets called by the VideoFrameSamplerDelegate if a new image has been captured. Does the QR detection.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">Event arguments</param>
		void HandleImageCaptured(object sender, ImageCaptureEventArgs e)
		{
			// Detect the QR codes in the captured image.
			// Important: case CGImage to CIImage. There is an implicit cast operator from CGImage to CIImage, but if we
			// pass the CGImage in to FeaturesInImage(), many many (implicit) CIImage instance will be created because this 
			// method is called very often. The garbage collector cannot keep up with that and we runn out of memory.
			// By casting manually and using() the CIImage, it will be disposed immediately, freeing up memory.
			using (CIImage inputCIImage = (CIImage)e.Image)
			{
				// Let the detector do its work on the image.
				var featuresInImage = detector.FeaturesInImage (inputCIImage);

				//Console.WriteLine ("Found " + featuresInImage.Length + " features in image.");

				var qrFeatures = new List<CIQRCodeFeature>();

				foreach(CIFeature feature in featuresInImage)
				{
					var qrCodeFeature = feature as CIQRCodeFeature;
					if(feature == null)
					{
						//Console.WriteLine("Skipping non-QR feature: " + feature);
						continue;
					}

					qrFeatures.Add(qrCodeFeature);

					//Console.WriteLine ("Found QR: " + qrCodeFeature);
				}

				if (qrFeatures.Count <= 0)
				{
				/*	this.InvokeOnMainThread (() => {
						this.imageViewOverlay.Image = null;
						this.imageViewPerspective.Image = null;
					});
					return;*/

					return;
				}

				// Handle first found code only to keep it simple.
				var firstQrCode = qrFeatures[0];

				Console.WriteLine("Found QR Code: " + firstQrCode.MessageString);

				// We are not on the main thread here.
				this.InvokeOnMainThread (() => {

					// Adjust the overlay image to the corners of the detected QR code with CIPerspectiveTransformWithExtent.
					using(var dict = new NSMutableDictionary ())
					{
						dict.Add (key: new NSString ("inputExtent"), value: new CIVector (inputCIImage.Extent));
						dict.Add (key: new NSString ("inputTopLeft"), value: new CIVector (firstQrCode.TopLeft));
						dict.Add (key: new NSString ("inputTopRight"), value: new CIVector (firstQrCode.TopRight));
						dict.Add (key: new NSString ("inputBottomLeft"), value: new CIVector (firstQrCode.BottomLeft));
						dict.Add (key: new NSString ("inputBottomRight"), value: new CIVector (firstQrCode.BottomRight)); 

						// Create a semi-transparent CIImage which will show the detected QR code.
						using(var overlayCIImage = new CIImage (color: CIColor.FromRgba (red: 1.0f, green: 0f, blue: 0f, alpha: 0.5f))
							// Size it to the source image.
							.ImageByCroppingToRect (inputCIImage.Extent)
							// Apply perspective distortion to the overlay rectangle to map it to the current camera picture.
							.CreateByFiltering ("CIPerspectiveTransformWithExtent", dict)
							// Place overlay on the image.
							.CreateByCompositingOverImage (inputCIImage))
						{
							// Must convert the CIImage into a CGImage and from there into a UIImage. 
							// Could go directly from CIImage to UIImage but when assigning the result to a UIImageView, the ContentMode of
							// the image view will be ignored and no proper aspect scaling will take place.
							using(var ctx = CIContext.FromOptions(null))
							using(CGImage convertedCGImage = ctx.CreateCGImage(overlayCIImage,  overlayCIImage.Extent))
							// This crashes with Xamarin.iOS
							//using(UIImage convertedUIImage = UIImage.FromImage(convertedCGImage, 1f, UIApplication.SharedApplication.StatusBarOrientation == UIInterfaceOrientation.LandscapeLeft ? UIImageOrientation.DownMirrored : UIImageOrientation.UpMirrored))
							// This works.
							using(UIImage convertedUIImage = UIImage.FromImage(convertedCGImage))
							{
								// Show converted image in UI.
								this.imageViewOverlay.Image = convertedUIImage;
							}
						}
					}

					// Apply a perspective correction with CIPerspectiveCorrection to the detected rectangle and display in another UIImageView.
					using(var dict = new NSMutableDictionary ())
					{
						dict.Add (key: new NSString ("inputTopLeft"), value: new CIVector (firstQrCode.TopLeft));
						dict.Add (key: new NSString ("inputTopRight"), value: new CIVector (firstQrCode.TopRight));
						dict.Add (key: new NSString ("inputBottomLeft"), value: new CIVector (firstQrCode.BottomLeft));
						dict.Add (key: new NSString ("inputBottomRight"), value: new CIVector (firstQrCode.BottomRight)); 

						// Use again CIImage -> CGImage -> UIImage to prevent scaling issues (see above).
						using(var perspectiveCorrectedImage = inputCIImage.CreateByFiltering ("CIPerspectiveCorrection", dict))
						using(var ctx = CIContext.FromOptions(null))
						using(CGImage convertedCGImage = ctx.CreateCGImage(perspectiveCorrectedImage, perspectiveCorrectedImage.Extent))
						using(UIImage convertedUIImage = UIImage.FromImage(convertedCGImage))
						{
							this.imageViewPerspective.Image = convertedUIImage;
						}
					}
				});
			}
				
			Console.WriteLine ("---------------------");
		}

		/// <summary>
		/// Gets called by VideoFrameSamplerDelegate if a capture error occurs.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">Event arguments.</param>
		void HandleImageCaptureError(object sender, CaptureErrorEventArgs e)
		{
			Console.WriteLine ("-----> ERROR: " + e.ErrorMessage);
		}

		public override void WillRotate (UIInterfaceOrientation toInterfaceOrientation, double duration)
		{
			base.WillRotate (toInterfaceOrientation, duration);

			// Rotate the video layer if the device is rotated.
			videoLayer.Connection.VideoOrientation = ConvertUiOrientation (toInterfaceOrientation);
		}

		/// <summary>
		/// Helper to convert UI orientation to AVCaptureVideoOrientation
		/// </summary>
		/// <returns>The user interface orientation.</returns>
		/// <param name="orientation">AVCaptureVideoOrientation</param>
		static AVCaptureVideoOrientation ConvertUiOrientation(UIInterfaceOrientation orientation)
		{
			var avOrientation = orientation == UIInterfaceOrientation.LandscapeLeft ? AVCaptureVideoOrientation.LandscapeLeft : AVCaptureVideoOrientation.LandscapeRight;
			return avOrientation;
		}
	}
}

