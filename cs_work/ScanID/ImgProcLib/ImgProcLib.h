#pragma once

using namespace System;

namespace ImgProcLib {
	public ref class Class1
	{
	public:
		// TODO: Add your methods for this class here.
		static String^ Hello(String^ value);
	};

	public ref class MatchTemplateResultItem
	{
	protected:
		System::String^ Name;

	public:
		MatchTemplateResultItem(System::String^ name);
		System::String^ GetName() { return Name; };
		double MatchResult;
		int LocX;
		int LocY;
		int Width;
		int Height;
	};

	public ref class MatchTemplateResult
	{

	public:
		MatchTemplateResult();
		property System::Collections::Generic::Dictionary<System::String^, MatchTemplateResultItem^>^ MatchResult;
		//property double MatchVal_MyKad;
		//property double MatchVal_Flag;
	};

	public ref class MatchTemplateIDCard
	{
		bool isTemplateLoaded = false;
		bool loadTemplate(System::String^ templateFolderPath);

	protected:
		~MatchTemplateIDCard();
	public:
		// TODO: Add your methods for this class here.
		//!MatchTemplateMyKad();
		bool Init(System::String^ templateFolderPath);
		bool IsInitialized() { return isTemplateLoaded; }
		MatchTemplateResult^ DoMatchTemplate(array<System::Byte>^ docImage);
	};

	class CropLayer : public cv::dnn::Layer
	{
	public:
		CropLayer(const cv::dnn::LayerParams& params);
		static cv::Ptr<cv::dnn::Layer> create(cv::dnn::LayerParams& params);
		
		virtual bool getMemoryShapes(const std::vector<std::vector<int> >& inputs,
			const int requiredOutputs,
			std::vector<std::vector<int> >& outputs,
			std::vector<std::vector<int> >& internals) const CV_OVERRIDE;
		
		virtual void forward(std::vector<cv::Mat*>& input, std::vector<cv::Mat>& output, std::vector<cv::Mat>& internals) CV_OVERRIDE;
		virtual void forward(cv::InputArrayOfArrays inputs,
			cv::OutputArrayOfArrays outputs,
			cv::OutputArrayOfArrays internals) CV_OVERRIDE;

//	private:
//		int batchSize;
//		int numChannels;
//		int height;
//		int width;

	};


	public ref class ImgProcUtil
	{
	public:
#ifdef USE_SYSTEM_DRAWING
		static bool WarpImageFromBitmap(System::Drawing::Bitmap^ srcBmp, System::Drawing::Point^ pt1, System::Drawing::Point^ pt2, System::Drawing::Point^ pt3, System::Drawing::Point^ pt4, System::Collections::Generic::List<System::Byte>^% outImage);
		static bool WarpImage(array<System::Byte>^ srcImage, System::Drawing::Point^ pt1, System::Drawing::Point^ pt2, System::Drawing::Point^ pt3, System::Drawing::Point^ pt4, System::Collections::Generic::List<System::Byte>^% outImage);
		static bool WarpImage(cv::Mat& srcImg, System::Drawing::Point^ pt1, System::Drawing::Point^ pt2, System::Drawing::Point^ pt3, System::Drawing::Point^ pt4, System::Collections::Generic::List<System::Byte>^% imageOut);
		static bool WarpImageFromBitmap(System::Drawing::Bitmap^ srcBmp, 
			System::Drawing::Point^ pt1, System::Drawing::Point^ pt2, System::Drawing::Point^ pt3, System::Drawing::Point^ pt4, 
			double outWidth, double outHeight,
			System::Collections::Generic::List<System::Byte>^% outImage);
		static bool WarpImage(array<System::Byte>^ srcImage, 
			System::Drawing::Point^ pt1, System::Drawing::Point^ pt2, System::Drawing::Point^ pt3, System::Drawing::Point^ pt4, 
			double outWidth, double outHeight,
			System::Collections::Generic::List<System::Byte>^% outImage);
		static bool WarpImage(cv::Mat& srcImg, 
			System::Drawing::Point^ pt1, System::Drawing::Point^ pt2, System::Drawing::Point^ pt3, System::Drawing::Point^ pt4, 
			double outWidth, double outHeight,
			System::Collections::Generic::List<System::Byte>^% imageOut);
		static System::Drawing::Bitmap^ BlackFilter(System::Drawing::Bitmap^ bmpSrc);
		static System::Drawing::Bitmap^ BlackFilter(array<System::Byte>^ docImage);
		static System::Drawing::Bitmap^ BlackFilterIvt(System::Drawing::Bitmap^ bmpSrc);
		static System::Drawing::Bitmap^ BlackFilterIvt(array<System::Byte>^ docImage);
		static System::Drawing::Bitmap^ SharpenFilter(System::Drawing::Bitmap^ bmpSrc);
		static System::Drawing::Bitmap^ SharpenFilter(array<System::Byte>^ docImage);
		static System::Drawing::Bitmap^ SharpenFilter(System::Drawing::Bitmap^ bmpSrc, int kernel_param);
		static System::Drawing::Bitmap^ SharpenFilter(array<System::Byte>^ docImage, int kernel_param);
		static System::Drawing::Bitmap^ EdgeDetectionFilter(System::Drawing::Bitmap^ bmpSrc, int kernel_param);
		static System::Drawing::Bitmap^ EdgeDetectionFilter(array<System::Byte>^ docImage, int kernel_param);
		static System::Drawing::Bitmap^ PreprocessOCR(System::Drawing::Bitmap^ bmpSrc);
		static System::Drawing::Bitmap^ PreprocessOCR(System::Drawing::Bitmap^ bmpSrc, int kernel_param, int morph_size);
		static System::Drawing::Bitmap^ PreprocessOCR(array<System::Byte>^ docImage);
		static System::Drawing::Bitmap^ PreprocessOCR(array<System::Byte>^ docImage, int kernel_param, int morph_size);
		static System::Drawing::Bitmap^ DeblurGray(System::Drawing::Bitmap^ bmpSrc);
		static System::Drawing::Bitmap^ DeblurGray(array<System::Byte>^ docImage);
		static System::Drawing::Bitmap^ DeblurGray(System::Drawing::Bitmap^ bmpSrc, int r, int snr);
		static System::Drawing::Bitmap^ DeblurGray(array<System::Byte>^ docImage, int r, int snr);
#endif
#ifdef USE_SYSTEM_DRAWING
		static System::Drawing::Bitmap^ HSVFilter(System::Drawing::Bitmap^ bmpSrc, byte HL, byte SL, byte VL, byte HH, byte SH, byte VH);
#endif
		static cv::Mat HSVFilter(cv::Mat srcImg, byte HL, byte SL, byte VL, byte HH, byte SH, byte VH);
#ifdef USE_SYSTEM_DRAWING
		static System::Drawing::Bitmap^ HSVFilter(array<System::Byte>^ docImage, byte HL, byte SL, byte VL, byte HH, byte SH, byte VH);
		static int DetectByHSVRange(System::Drawing::Bitmap^ bmpSrc, byte HL, byte SL, byte VL, byte HH, byte SH, byte VH);
#endif
		static int DetectByHSVRange(array<System::Byte>^ docImage, byte HL, byte SL, byte VL, byte HH, byte SH, byte VH);

#ifdef USE_SYSTEM_DRAWING
		static System::Drawing::Bitmap^ HologramFilter(System::Drawing::Bitmap^ bmpSrc);
#endif
		static cv::Mat HologramFilter(cv::Mat srcImg);
#ifdef USE_SYSTEM_DRAWING
		static System::Drawing::Bitmap^ HologramFilter(array<System::Byte>^ docImage);
		static int DetectHologram(System::Drawing::Bitmap^ bmpSrc);
#endif
		static int DetectHologram(array<System::Byte>^ docImage);
#ifdef USE_SYSTEM_DRAWING
		static System::Drawing::Bitmap^ InpaintHologram(System::Drawing::Bitmap^ bmpSrc, int radius);
#endif
		static cv::Mat Inpaint(cv::Mat srcImg, cv::Mat maskImg, int radius);
#ifdef USE_SYSTEM_DRAWING
		static System::Drawing::Bitmap^ CvtToGray(System::Drawing::Bitmap^ bmpSrc);
		static System::Drawing::Bitmap^ CvtToGray(array<System::Byte>^ docImage);
		static System::Drawing::Bitmap^ CvtToGrayAndOpenClose(System::Drawing::Bitmap^ bmpSrc);
		static System::Drawing::Bitmap^ CvtToGrayAndOpenClose(array<System::Byte>^ docImage);
		static System::Drawing::Bitmap^ MorphOpen(System::Drawing::Bitmap^ bmpSrc, int morph_size);
#endif
		static cv::Mat MorphOpen(cv::Mat srcImg, int morph_size);
#ifdef USE_SYSTEM_DRAWING
		static System::Drawing::Bitmap^ MorphOpen(array<System::Byte>^ docImage, int morph_size);
		static System::Drawing::Bitmap^ MorphClose(System::Drawing::Bitmap^ bmpSrc, int morph_size);
#endif
		static cv::Mat MorphClose(cv::Mat srcImg, int morph_size);
#ifdef USE_SYSTEM_DRAWING
		static System::Drawing::Bitmap^ MorphClose(array<System::Byte>^ docImage, int morph_size);
		static System::Drawing::Bitmap^ AdjustContrastBrightness(System::Drawing::Bitmap^ bmpSrc, double contrast, int brightness);
#endif
		static cv::Mat AdjustContrastBrightness(cv::Mat srcImg, double contrast, int brightness);
		static bool AdjustContrastBrightness(array<System::Byte>^ docImage, double contrast, int brightness, System::Collections::Generic::List<System::Byte>^% imageOut);
#ifdef USE_SYSTEM_DRAWING
		static int GetBrightness(System::Drawing::Bitmap^ bmpSrc);
#endif
		static int GetBrightness(cv::Mat srcImg);
		static int GetBrightness(array<System::Byte>^ docImage);
#ifdef USE_SYSTEM_DRAWING
		static bool DetectEdge(array<System::Byte>^ imageSrc, System::Collections::Generic::List<System::Drawing::Point>^% points);
		static bool DetectEdgeFromBitmap(System::Drawing::Bitmap^ srcBmp, System::Collections::Generic::List<System::Drawing::Point>^% pointss);
		//static bool DetectEdge2(array<System::Byte>^ imageSrc, System::Collections::Generic::List<System::Drawing::Point>^% points);
		//static bool DetectEdge3(array<System::Byte>^ imageSrc, System::Collections::Generic::List<System::Drawing::Point>^% points);
		static bool DetectEdge4(array<System::Byte>^ imageSrc, System::Collections::Generic::List<System::Drawing::Point>^% points);
		static bool DetectEdge4(array<System::Byte>^ imageSrc, System::Collections::Generic::List<System::Drawing::Point>^% points, double expectedHVRatio);
		static bool DetectEdge4FromBitmap(System::Drawing::Bitmap^ srcBmp, System::Collections::Generic::List<System::Drawing::Point>^% points, double expectedHVRatio);
		static bool DetectEdgeHED(array<System::Byte>^ imageSrc, System::Collections::Generic::List<System::Drawing::Point>^% points);
		static bool DetectEdgeHED(array<System::Byte>^ imageSrc, System::Collections::Generic::List<System::Drawing::Point>^% points, double expectedHVRatio);
#endif

		static String^ ReadQRCode(array<System::Byte>^ imageSrc);
		static bool DetectFace(array<System::Byte>^ imageSrc, System::Int32^% left, System::Int32^% top, System::Int32^% right, System::Int32^% bottom);
	};

}
