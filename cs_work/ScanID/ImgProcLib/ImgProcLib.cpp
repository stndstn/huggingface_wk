#include "stdafx.h"
#include <iostream>
#include <vector>
#include <opencv2/core.hpp>
#include <opencv2/imgcodecs.hpp>
#include <opencv2/highgui.hpp>
#include <opencv2/objdetect.hpp>
#include <opencv2/imgproc.hpp>
#include "opencv2/opencv.hpp"
#include <opencv2/dnn/layer.details.hpp>  // CV_DNN_REGISTER_LAYER_CLASS
#include <opencv2/dnn/all_layers.hpp>
#include <Windows.h>
#include "ImgProcLib.h"
#pragma managed(push, off)
	#include <dlib/image_processing/frontal_face_detector.h>
	#include <dlib/image_io.h>

	//https://tuttlem.github.io/2014/08/18/getting-istream-to-work-off-a-byte-array.html
	/*
		* memstream class - istream from byte array
		* Usage

	uint8_t buf[] = { 0x00, 0x01, 0x02, 0x03 };
	memstream s(buf, 4);
	char b;
	do {
		s.read(&b, 1);
		std::cout << "read: " << (int)b << std::endl;
	} while (s.good());
	*/

	class membuf : public std::basic_streambuf<char> {
	public:
		membuf(const uint8_t* p, size_t l) {
			setg((char*)p, (char*)p, (char*)p + l);
		}
	};

	class memstream : public std::istream {
	public:
		memstream(const uint8_t* p, size_t l) :
			std::istream(&buffer),
			buffer(p, l) {
			rdbuf(&buffer);
		}

	public:
		membuf buffer;
	};

	const std::string read_type(std::istream& in_stream)
	{
		// Read the first 12 bytes of the stream
		char buffer[13];
		in_stream.read(buffer, 12);
		buffer[12] = 0;

		// Determine the true image type using link:
		// http://en.wikipedia.org/wiki/List_of_file_signatures
		static const char* pngHeader = "\x89\x50\x4E\x47\x0D\x0A\x1A\x0A";
		static const char* jxlHeader = "\x00\x00\x00\x0C\x4A\x58\x4C\x20\x0D\x0A\x87\x0A";

		if (buffer[0] == '\xff' && buffer[1] == '\xd8' && buffer[2] == '\xff')
			return "JPG";
		else if (memcmp(buffer, pngHeader, strlen(pngHeader)) == 0)
			return "PNG";
		else if (buffer[0] == 'B' && buffer[1] == 'M')
			return "BMP";
		else if (buffer[0] == 'D' && buffer[1] == 'N' && buffer[2] == 'G')
			return "DNG";
		else if (buffer[0] == 'G' && buffer[1] == 'I' && buffer[2] == 'F')
			return "GIF";
		else if ((buffer[0] == '\xff' && buffer[1] == '\x0a') ||
			memcmp(buffer, jxlHeader, 12) == 0)  // we can't use strlen because the header starts with \x00.
			return "JXL";
		else if (buffer[0] == 'R' && buffer[1] == 'I' && buffer[2] == 'F' && buffer[3] == 'F' &&
			buffer[8] == 'W' && buffer[9] == 'E' && buffer[10] == 'B' && buffer[11] == 'P')
			return "WEBP";

		return "UNKNOWN";
	}

	template <typename image_type>
	void dlib_load_image(
		image_type& image,
		uint8_t* pBuffer,
		int nSize
	)
	{
		memstream in_stream(pBuffer, nSize);
		const std::string im_type = read_type(in_stream);

		if (im_type == "BMP")
		{
			dlib::load_bmp(image, in_stream);
		}
		else if (im_type == "DNG")
		{
			dlib::load_dng(image, in_stream);
		}
		else if (im_type == "JPG")
		{
			std::vector<byte> imgData;
			dlib::load_jpeg(image, pBuffer, nSize);
		}
		else if (im_type == "PNG")
		{
			dlib::load_png(image, in_stream);
		}
		else
		{
			throw dlib::image_load_error("Unknown image file format:" + im_type + " Unable to load image.");
		}
	}

	bool DlibDetectFace(byte* pImageData, int nSize, int pts[8])
	//std::vector<dlib::rectangle> DlibDetectFace(byte* pImageData, int nSize)
	{
		dlib::frontal_face_detector detector = dlib::get_frontal_face_detector();
		dlib::array2d<unsigned char> img;
		dlib_load_image(img, pImageData, nSize);

		// Make the image bigger by a factor of two.  This is useful since
		// the face detector looks for faces that are about 80 by 80 pixels
		// or larger.  Therefore, if you want to find faces that are smaller
		// than that then you need to upsample the image as we do here by
		// calling pyramid_up().  So this will allow it to detect faces that
		// are at least 40 by 40 pixels in size.  We could call pyramid_up()
		// again to find even smaller faces, but note that every time we
		// upsample the image we make the detector run slower since it must
		// process a larger image.
		//pyramid_up(img);

		// Now tell the face detector to give us a list of bounding boxes
		// around all the faces it can find in the image.
		std::vector<dlib::rectangle> dets = detector(img);
		/*
		return dets;
		*/
		pts[0] = dets[0].left();
		pts[1] = dets[0].top();
		pts[2] = dets[0].right();
		pts[3] = dets[0].bottom();
		return true;
	}
#pragma managed(pop)

#define PI 3.1415926535                             // Greek alphabet, is used to represent the ratio of the circumference of a circle to its

namespace ImgProcLib {

	String^ Class1::Hello(String^ value) {
		String^ ret = "hello ";
		ret += value;
		return ret;
	}

	MatchTemplateResult::MatchTemplateResult() {
		//MatchResult = gcnew System::Collections::Generic::Dictionary<System::String^, double>();
		MatchResult = gcnew System::Collections::Generic::Dictionary<System::String^, MatchTemplateResultItem^>();
	}

	MatchTemplateResultItem::MatchTemplateResultItem(System::String^ name) {
		Name = name;
	}

	//typedef std::map<std::wstring, cv::Mat> Mymap;
	std::map<std::wstring, cv::Mat> _mapTmpl;
	//cv::Mat	tmplMyKad_Flag;
	//cv::Mat tmplMyKad_MyKad;

	//MatchTemplateMyKad::!MatchTemplateMyKad()
	MatchTemplateIDCard::~MatchTemplateIDCard()
	{
		for (std::pair<std::wstring, cv::Mat> i : _mapTmpl)
		{
			i.second.release();
		}
	}

	bool MatchTemplateIDCard::Init(System::String^ templateFolderPath)
	{
		if (_mapTmpl.size() > 0)
			return true;

		return loadTemplate(templateFolderPath);
	}

	bool MatchTemplateIDCard::loadTemplate(System::String^ templateFolderPath)
	{
		CHAR szModulePath[_MAX_PATH];
		memset(szModulePath, 0, sizeof(szModulePath));
		::GetModuleFileNameA(NULL, szModulePath, _MAX_PATH);
		CHAR szDrive[_MAX_DRIVE];
		CHAR szDir[_MAX_DIR];
		CHAR szFName[_MAX_FNAME];
		CHAR szExt[_MAX_EXT];
		memset(szDrive, 0, sizeof(szDrive));
		memset(szDir, 0, sizeof(szDir));
		memset(szFName, 0, sizeof(szFName));
		memset(szExt, 0, sizeof(szExt));
		errno_t err = _splitpath_s(szModulePath, szDrive, _MAX_DRIVE, szDir, _MAX_DIR, szFName, _MAX_FNAME, szExt, _MAX_EXT);
		if (err != 0) {
			throw gcnew Exception("Could not parse the module path.");
		}

		// clean existing dictionary
		_mapTmpl.clear();

		System::Collections::Generic::IEnumerable<System::String^>^ files = System::IO::Directory::EnumerateFiles(templateFolderPath);
		System::Collections::Generic::IEnumerator<System::String^>^ iFile = files->GetEnumerator();
		CHAR szPathA[_MAX_PATH];
		while (iFile->MoveNext())
		{
			System::IO::FileInfo^ fi = gcnew System::IO::FileInfo(iFile->Current);
			if (fi->Exists) {
				System::String^ ext = fi->Extension;
				pin_ptr<Char> pExt = &ext->ToCharArray()[0];
				wchar_t* wpExt = pExt;
				//System::Console::Write("{0} {1} ", fi->Name, ext);
				//wprintf(L"%s\n", wpExt);
				if (_wcsicmp(wpExt, L".PNG") == 0 || _wcsicmp(wpExt, L".JPG") == 0 || _wcsicmp(wpExt, L".JPEG") == 0) {
					pin_ptr<Char> pFilePath = &fi->FullName->ToCharArray()[0];
					wchar_t* wpFilePath = pFilePath;
					memset(szPathA, 0, sizeof(szPathA));
					sprintf_s(szPathA, sizeof(szPathA), "%S", wpFilePath);
					cv::Mat srcImg = cv::imread(szPathA, 1);
					cv::Mat tmpl;
					cv::cvtColor(srcImg, tmpl, cv::COLOR_BGR2GRAY);
					srcImg.release();
					//printf("%s %d %d\n", szPathA, tmpl.rows, tmpl.cols);
					System::Diagnostics::Debug::WriteLine(String::Format("loadTemplate {0} {1} {2}", fi->Name, tmpl.rows, tmpl.cols));
					int lenFileNameWOExt = fi->Name->Length - fi->Extension->Length;
					String^ fileNameWOExt = fi->Name->Substring(0, lenFileNameWOExt);
					pin_ptr<Char> pName = &fileNameWOExt->ToCharArray()[0];
					wchar_t* wpName = pName;
					std::wstring name = wpName;
					if (!tmpl.empty()) {
						//wprintf(L"%s\n", name.c_str());
						_mapTmpl.insert(std::map<std::wstring, cv::Mat>::value_type(name, tmpl));
					}
				}
			}
		}

/*
		tmplMyKad_Flag = cv::imread(szTmplImgPath, 1);
		if (tmplMyKad_Flag.empty()) {
			return false;
		}
		//tmplMyKad_MyKad = cv::imread("tmpl\MyKad_670_MyKad.png", 1);
		sprintf_s(szTmplImgPath, _MAX_PATH, "%s%s\\tmpl\\MyKad_670_MyKad.png", szDrive, szDir);
		tmplMyKad_MyKad = cv::imread(szTmplImgPath, 1);
		if (tmplMyKad_MyKad.empty()) {
			return false;
		}
*/
		isTemplateLoaded = true;
		return true;
	}
	bool saveImageAsByteArray(cv::Mat& img, const char* szExt, uchar* pBuf, long lenBuf, size_t* pCopied)
	{
		bool ret = false;
		if (pCopied == NULL)
			return false;
		*pCopied = 0;

		cv::String ext(".png");
		if (szExt != NULL && strlen(szExt) > 0)
			ext = szExt;

		std::vector<uchar> data = std::vector<uchar>();
		imencode(ext, img, data);
		if (lenBuf == 0) {
			ret = true;
			*pCopied = data.size();
		}
		else if (pBuf != NULL && lenBuf >= data.size()) {
			std::copy(data.begin(), data.end(), pBuf);
			*pCopied = data.size();
			ret = true;
		}
		return ret;
	}
	bool saveImageAsByteArray(cv::Mat& img, const char* szExt, System::Collections::Generic::List<System::Byte>^ dest)
	{
		bool ret = false;

		cv::String ext(".png");
		if (szExt != NULL && strlen(szExt) > 0)
			ext = szExt;

		std::vector<uchar> data = std::vector<uchar>();
		imencode(ext, img, data);

		array<System::Byte>^ temp = gcnew array<System::Byte>(data.size());
		pin_ptr<Byte> p = &temp[0];   // entire array is now pinned
		unsigned char * cp = p;
		std::copy(data.begin(), data.end(), cp);
		dest->AddRange(temp);
		return true;
	}
	cv::Mat loadImageFromByteArray(uchar* pData, size_t len)
	{
		std::vector<uchar> data = std::vector<uchar>(pData, pData + len);
		cv::Mat decoded = cv::imdecode(data, cv::IMREAD_COLOR);	//IMREAD_COLOR: always convert image to the 3 channel BGR color image.
		return decoded;
	}
#if false
	bool saveImage(const char* name, cv::Mat& img) {
		bool ret = false;
		size_t lenBuf = 0;

		if (saveImageAsByteArray(img, ".png", NULL, 0, &lenBuf))
		{
			uchar* pBuf = new uchar[lenBuf];
			size_t lenCopied = 0;
			if (saveImageAsByteArray(img, ".png", pBuf, lenBuf, &lenCopied)) {
				FILE* fp = NULL;
				errno_t err = fopen_s(&fp, name, "wb");
				if (err != 0) {
					printf("failed to open file: (%d) %s\n", err, name);
					return -1;
				}
				size_t written = 0;
				do {
					written += fwrite(pBuf + written, 1, lenCopied, fp);
				} while (written < lenCopied);
				fclose(fp);
				ret = true;
			}
		}
		return ret;
	}
#endif

	void Match(cv::Mat& img, cv::Mat& templ, cv::Point& matchLoc, double& matchVal)
	{
		/// Create the result matrix
		int result_cols = img.cols - templ.cols + 1;
		int result_rows = img.rows - templ.rows + 1;

		cv::Mat result;
		result.create(result_rows, result_cols, CV_32FC1);

		/// Do the Matching and Normalize
		int match_method = cv::TM_CCOEFF_NORMED;
		matchTemplate(img, templ, result, match_method);
		//normalize(result, result, 0, 1, NORM_MINMAX, -1, Mat());

		/// Localizing the best match with minMaxLoc
		double minVal; double maxVal; 
		cv::Point minLoc; cv::Point maxLoc;

		minMaxLoc(result, &minVal, &maxVal, &minLoc, &maxLoc, cv::Mat());

		/// For SQDIFF and SQDIFF_NORMED, the best matches are lower values. For all the other methods, the higher the better
		if (match_method == cv::TM_SQDIFF || match_method == cv::TM_SQDIFF_NORMED)
		{
			matchLoc = minLoc;
			matchVal = minVal;
		}
		else
		{
			matchLoc = maxLoc;
			matchVal = maxVal;
		}

		/// Show me what you got
		//printf("%d,%d (%f)\n", matchLoc.x, matchLoc.y, matchVal);
		//rectangle(img_display, matchLoc, Point(matchLoc.x + templ.cols, matchLoc.y + templ.rows), Scalar::all(0), 2, 8, 0);
		//rectangle(result, matchLoc, Point(matchLoc.x + templ.cols, matchLoc.y + templ.rows), Scalar::all(0), 2, 8, 0);

		//imshow(image_window, img_display);
		//imshow(result_window, result);

		return;
	}

	//MatchTemplateMyKadResult^ MatchTemplateMyKad::DoMatchTemplate(array<System::Byte>^ docImage, System::Text::StringBuilder^% errmsg) {
	MatchTemplateResult^ MatchTemplateIDCard::DoMatchTemplate(array<System::Byte>^ docImage) {

		if (!isTemplateLoaded)
			throw gcnew Exception("Not initialized yet. Please call Init() before call this method.");

		//errmsg->Clear();
		MatchTemplateResult^ result = gcnew MatchTemplateResult();

		cv::Mat image;
		pin_ptr<Byte> p = &docImage[0];   // entire array is now pinned
		unsigned char * cp = p;
		int docImageLen = docImage->Length;
		image = loadImageFromByteArray(cp, docImageLen);

		//image = imread(imageName, IMREAD_COLOR); // Read the file
		if (image.empty())                      // Check for invalid input
		{
			throw gcnew Exception("Could not open or find the image");
		}

		//
		// Resize to 670 px width
		//
		cv::Mat resizedImg;
		double ratio = (double)image.rows / (double)image.cols;
		cv::Size s = cv::Size(670, (int)((double)670 * ratio));
		resize(image, resizedImg, s, cv::INTER_CUBIC);
		//imwrite(resizedImageName, resizedImg);
		//saveImage(resizedImageName.c_str(), resizedImg);

		//
		//Gray
		//
		cv::Mat grayImg;
		cvtColor(resizedImg, grayImg, cv::COLOR_BGR2GRAY);
		cv::equalizeHist(grayImg, grayImg);
		//imwrite(grayImageName, grayImg);
		//saveImage(grayImageName.c_str(), grayImg);
/*
		//
		// BW
		//
		cv::Mat bwImg;
		cv::Mat bwTempImg;
		//int iThreshBlockSize = 2 * (image.rows / 200) + 1;
		int iThreshBlockSize = 15;
		int threshConstantSize = 11;
		adaptiveThreshold(grayImg, bwTempImg, 125, cv::ADAPTIVE_THRESH_MEAN_C, cv::THRESH_BINARY, iThreshBlockSize, threshConstantSize);
		double THRESH_OUTSO = threshold(grayImg, bwTempImg, 127, 255, CV_THRESH_OTSU);
		double lower_thresh_val = THRESH_OUTSO * 0.5;
		threshold(bwTempImg, bwImg, lower_thresh_val, 255, cv::THRESH_BINARY);
		//imwrite(bwImageName, bwImg);
		//saveImage(bwImageName.c_str(), bwImg);
*/
		//
		// Match
		//
		cv::Point matchLoc;
		double matchVal = 0;
		//wprintf(L"%d\n", _mapTmpl.size());
		for (std::pair<std::wstring, cv::Mat> i : _mapTmpl)
		{
			try {
				Match(grayImg, i.second, matchLoc, matchVal);
				//Match(resizedImg, i.second, matchLoc, matchVal);
				System::String^ name = gcnew System::String(i.first.c_str());
				//wprintf(L"%s %f\n", i.first.c_str(), matchVal);
				MatchTemplateResultItem^ item = gcnew MatchTemplateResultItem(name);
				item->LocX = matchLoc.x;
				item->LocY = matchLoc.y;
				item->Width = i.second.cols;
				item->Height = i.second.rows;
				item->MatchResult = matchVal;
				result->MatchResult->Add(name, item);
			}
			catch (std::exception e)
			{
				printf(e.what());
			}
		}

		//Match(resizedImg, tmplMyKad_MyKad, matchLoc, matchVal);
		//result->MatchVal_MyKad = matchVal;
		//Match(resizedImg, tmplMyKad_Flag, matchLoc, matchVal);
		//result->MatchVal_Flag = matchVal;
		return result;
	}

#ifdef USE_SYSTEM_DRAWING
	bool ImgProcUtil::WarpImageFromBitmap(System::Drawing::Bitmap^ srcBmp, System::Drawing::Point^ pt1, System::Drawing::Point^ pt2, System::Drawing::Point^ pt3, System::Drawing::Point^ pt4, System::Collections::Generic::List<System::Byte>^% imageOut)
	{
		System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream();
		srcBmp->Save(ms, System::Drawing::Imaging::ImageFormat::Png);
		array<System::Byte>^ imageSrc = ms->GetBuffer();
		return WarpImage(imageSrc, pt1, pt2, pt3, pt4, imageOut);
	}
#endif // USE_SYSTEM_DRAWING


#ifdef USE_SYSTEM_DRAWING
	bool ImgProcUtil::WarpImage(array<System::Byte>^ imageSrc, System::Drawing::Point^ pt1, System::Drawing::Point^ pt2, System::Drawing::Point^ pt3, System::Drawing::Point^ pt4, System::Collections::Generic::List<System::Byte>^% imageOut)
	{
		cv::Mat srcImg;
		pin_ptr<Byte> p = &imageSrc[0];   // entire array is now pinned
		unsigned char * cp = p;
		int docImageLen = imageSrc->Length;
		srcImg = loadImageFromByteArray(cp, docImageLen);
		return WarpImage(srcImg, pt1, pt2, pt3, pt4, imageOut);
	}
#endif
#ifdef USE_SYSTEM_DRAWING
	bool ImgProcUtil::WarpImage(cv::Mat& srcImg, System::Drawing::Point^ pt1, System::Drawing::Point^ pt2, System::Drawing::Point^ pt3, System::Drawing::Point^ pt4, System::Collections::Generic::List<System::Byte>^% imageOut)
	{
		int outWidth;
		int outHeight;

		cv::Point2i p0 = cv::Point2i(pt1->X, pt1->Y);
		cv::Point2i p1 = cv::Point2i(pt2->X, pt2->Y);
		cv::Point2i p2 = cv::Point2i(pt3->X, pt3->Y);
		cv::Point2i p3 = cv::Point2i(pt4->X, pt4->Y);
		
		//outHeight is average of distance between P[1] and P[2], and distance between P[3] and P[0]
		cv::Point2i difference12 = p1 - p2;
		double distance12 = sqrt(difference12.ddot(difference12));
		cv::Point2i difference30 = p3 - p0;
		double distance30 = sqrt(difference30.ddot(difference30));
		outHeight = (distance12 + distance30) / 2;
		//outWidth is average of distance between P[0] and P[1], and distance between P[2] and P[3]
		cv::Point2i difference01 = p0 - p1;
		double distance01 = sqrt(difference01.ddot(difference01));
		cv::Point2i difference23 = p2 - p3;
		double distance23 = sqrt(difference23.ddot(difference23));
		outWidth = (distance01 + distance23) / 2;

		/*
		if (srcImg.cols < srcImg.rows) //cols < rows
		{
			//original image is Portrait
			//outHeight is average of distance between P[1] and P[2], and distance between P[3] and P[0]
			cv::Point2i difference12 = p1 - p2;
			double distance12 = sqrt(difference12.ddot(difference12));
			cv::Point2i difference30 = p3 - p0;
			double distance30 = sqrt(difference30.ddot(difference30));
			outHeight = (distance12 + distance30) / 2;
			//outWidth is average of distance between P[0] and P[1], and distance between P[2] and P[3]
			cv::Point2i difference01 = p0 - p1;
			double distance01 = sqrt(difference01.ddot(difference01));
			cv::Point2i difference23 = p2 - p3;
			double distance23 = sqrt(difference23.ddot(difference23));
			outWidth = (distance01 + distance23) / 2;
		}
		else
		{
			//original image is Landscape
			//outWidth is average of distance between P[1] and P[2], and distance between P[3] and P[0]
			cv::Point2i difference12 = p1 - p2;
			double distance12 = sqrt(difference12.ddot(difference12));
			cv::Point2i difference30 = p3 - p0;
			double distance30 = sqrt(difference30.ddot(difference30));
			outWidth = (distance12 + distance30) / 2;
			//outHeight is average of distance between P[0] and P[1], and distance between P[2] and P[3]
			cv::Point2i difference01 = p0 - p1;
			double distance01 = sqrt(difference01.ddot(difference01));
			cv::Point2i difference23 = p2 - p3;
			double distance23 = sqrt(difference23.ddot(difference23));
			outHeight = (distance01 + distance23) / 2;
		}
		*/

		// Points of edge (array to vector)
		std::vector<cv::Point2i> newPointScr;
		newPointScr.push_back(p0);
		newPointScr.push_back(p1);
		newPointScr.push_back(p2);
		newPointScr.push_back(p3);

		// Four corners of  in source image
		std::vector<cv::Point2i> pointDst;
		pointDst.push_back(cv::Point2i(0, 0));
		pointDst.push_back(cv::Point2i(outWidth, 0));
		pointDst.push_back(cv::Point2i(outWidth, outHeight));
		pointDst.push_back(cv::Point2i(0, outHeight));

		// Calculate Homography
		cv::Mat H = cv::findHomography(newPointScr, pointDst);

		/*
		// Triming
		float trimPercentage = 2; // Consider the 2% triming of margines of orginal image
		float eps = 1 / (1 - trimPercentage / 100) - 1;
		cv::Mat trimingMat = (cv::Mat_<double>(3, 3) << 1 + eps, 0, -eps * outWidth / 2,
			0, 1 + eps, -eps * outHeight / 2,
			0, 0, 1);
		H = trimingMat * H;
		*/
		// Normalization to ensure that ||C1|| = 1
		// S = [xp yp 1] = H*[x y 1] = [ h11 h12 h13; h21 h22 h23; h31 h32 h33;]*[x y 1]
		double norm = sqrt(H.at<double>(0, 0) * H.at<double>(0, 0) +
			H.at<double>(1, 0) * H.at<double>(1, 0) +
			H.at<double>(2, 0) * H.at<double>(2, 0));
		H /= norm;
		cv::Mat c1 = H.col(0);
		cv::Mat c2 = H.col(1);
		cv::Mat c3 = c1.cross(c2);
		cv::Mat tvec = H.col(2);
		cv::Mat R(3, 3, CV_64F);
		for (int i = 0; i < 3; i++)
		{
			R.at<double>(i, 0) = c1.at<double>(i, 0);
			R.at<double>(i, 1) = c2.at<double>(i, 0);
			R.at<double>(i, 2) = c3.at<double>(i, 0);
		}

		// Warping
		cv::Mat outImg;
		warpPerspective(srcImg, outImg, H, cv::Size(outWidth, outHeight));

		//
		// Resize to 670 px width
		//
		bool bRet = false;
		//if (outImg.cols > 670) {
		//	cv::Mat resizedImg;
		//	double ratio = (double)outImg.rows / (double)outImg.cols;
		//	cv::Size s = cv::Size(670, (int)((double)670 * ratio));
		//	resize(outImg, resizedImg, s, CV_INTER_CUBIC);
		//	bRet = saveImageAsByteArray(resizedImg, ".png", imageOut);
		//	resizedImg.release();
		//}
		//else {
			bRet = saveImageAsByteArray(outImg, ".png", imageOut);
		//}
		outImg.release();
		return bRet;
	}
#endif
#ifdef USE_SYSTEM_DRAWING
	bool ImgProcUtil::WarpImageFromBitmap(System::Drawing::Bitmap^ srcBmp,
		System::Drawing::Point^ pt1, System::Drawing::Point^ pt2, System::Drawing::Point^ pt3, System::Drawing::Point^ pt4,
		double outWidth, double outHeight,
		System::Collections::Generic::List<System::Byte>^% imageOut)
	{
		System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream();
		srcBmp->Save(ms, System::Drawing::Imaging::ImageFormat::Png);
		array<System::Byte>^ imageSrc = ms->GetBuffer();
		return WarpImage(imageSrc, pt1, pt2, pt3, pt4, outWidth, outHeight, imageOut);
	}
#endif
#ifdef USE_SYSTEM_DRAWING
	bool ImgProcUtil::WarpImage(array<System::Byte>^ imageSrc,
		System::Drawing::Point^ pt1, System::Drawing::Point^ pt2, System::Drawing::Point^ pt3, System::Drawing::Point^ pt4,
		double outWidth, double outHeight,
		System::Collections::Generic::List<System::Byte>^% imageOut)
	{
		cv::Mat srcImg;
		pin_ptr<Byte> p = &imageSrc[0];   // entire array is now pinned
		unsigned char* cp = p;
		int docImageLen = imageSrc->Length;
		srcImg = loadImageFromByteArray(cp, docImageLen);
		return WarpImage(srcImg, pt1, pt2, pt3, pt4, outWidth, outHeight, imageOut);
	}
#endif
#ifdef USE_SYSTEM_DRAWING
	bool ImgProcUtil::WarpImage(cv::Mat& srcImg,
		System::Drawing::Point^ pt1, System::Drawing::Point^ pt2, System::Drawing::Point^ pt3, System::Drawing::Point^ pt4,
		double outWidth, double outHeight,
		System::Collections::Generic::List<System::Byte>^% imageOut)
	{
		cv::Point2i p0 = cv::Point2i(pt1->X, pt1->Y);
		cv::Point2i p1 = cv::Point2i(pt2->X, pt2->Y);
		cv::Point2i p2 = cv::Point2i(pt3->X, pt3->Y);
		cv::Point2i p3 = cv::Point2i(pt4->X, pt4->Y);

		// Points of edge (array to vector)
		std::vector<cv::Point2i> newPointScr;
		newPointScr.push_back(p0);
		newPointScr.push_back(p1);
		newPointScr.push_back(p2);
		newPointScr.push_back(p3);

		// Four corners of  in source image
		std::vector<cv::Point2i> pointDst;
		pointDst.push_back(cv::Point2i(0, 0));
		pointDst.push_back(cv::Point2i(outWidth, 0));
		pointDst.push_back(cv::Point2i(outWidth, outHeight));
		pointDst.push_back(cv::Point2i(0, outHeight));

		// Calculate Homography
		cv::Mat H = cv::findHomography(newPointScr, pointDst);

		// Normalization to ensure that ||C1|| = 1
		// S = [xp yp 1] = H*[x y 1] = [ h11 h12 h13; h21 h22 h23; h31 h32 h33;]*[x y 1]
		double norm = sqrt(H.at<double>(0, 0) * H.at<double>(0, 0) +
			H.at<double>(1, 0) * H.at<double>(1, 0) +
			H.at<double>(2, 0) * H.at<double>(2, 0));
		H /= norm;
		cv::Mat c1 = H.col(0);
		cv::Mat c2 = H.col(1);
		cv::Mat c3 = c1.cross(c2);
		cv::Mat tvec = H.col(2);
		cv::Mat R(3, 3, CV_64F);
		for (int i = 0; i < 3; i++)
		{
			R.at<double>(i, 0) = c1.at<double>(i, 0);
			R.at<double>(i, 1) = c2.at<double>(i, 0);
			R.at<double>(i, 2) = c3.at<double>(i, 0);
		}

		// Warping
		cv::Mat outImg;
		warpPerspective(srcImg, outImg, H, cv::Size(outWidth, outHeight));

		bool bRet = false;
		bRet = saveImageAsByteArray(outImg, ".png", imageOut);
		outImg.release();
		return bRet;
	}
#endif
#ifdef USE_SYSTEM_DRAWING
	System::Drawing::Bitmap^ ImgProcUtil::BlackFilter(System::Drawing::Bitmap^ bmpSrc)
	{
		System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream();
		bmpSrc->Save(ms, System::Drawing::Imaging::ImageFormat::Bmp);
		array<System::Byte>^ imageSrc = ms->ToArray();
		return BlackFilter(imageSrc);
	}

	System::Drawing::Bitmap^ ImgProcUtil::BlackFilter(array<System::Byte>^ imageSrc)
	{
		cv::Mat srcImg;
		pin_ptr<Byte> p = &imageSrc[0];   // entire array is now pinned
		unsigned char * cp = p;
		int docImageLen = imageSrc->Length;
		srcImg = loadImageFromByteArray(cp, docImageLen);
		//cv::imshow("Original", srcImg);

		//sharpen image

		cv::Mat sharpened;
		cv::GaussianBlur(srcImg, sharpened, cv::Size(0, 0), 3);
		//cv::imshow("GaussianBlur", sharpened);
		cv::addWeighted(srcImg, 1.5, sharpened, -0.5, 0, sharpened);
		//cv::imshow("addWeighted", sharpened);
		//cv::imwrite(imageNameBody + "_sharpened.png", sharpened);

		//Filter to extract image of black color only 
		cv::Mat maskBlack;
		cv::Mat imageHsv;
		cv::cvtColor(sharpened, imageHsv, cv::COLOR_BGR2HSV);
		cv::inRange(imageHsv, cv::Scalar(0, 0, 0), cv::Scalar(180, 255, 120), maskBlack);
		//invert 
		cv::bitwise_not(maskBlack, maskBlack);

		System::Collections::Generic::List<unsigned char>^ filteredImageData = gcnew System::Collections::Generic::List<unsigned char>();
		if (saveImageAsByteArray(maskBlack, ".bmp", filteredImageData))
		{
			array<unsigned char>^ buffer = gcnew array<unsigned char>(filteredImageData->Count);
			buffer = filteredImageData->ToArray();
			System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream(buffer);
			System::Drawing::Bitmap^ bmp = gcnew System::Drawing::Bitmap(ms);
			return bmp;
		}
		return nullptr;
	}

	System::Drawing::Bitmap^ ImgProcUtil::BlackFilterIvt(System::Drawing::Bitmap^ bmpSrc)
	{
		System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream();
		bmpSrc->Save(ms, System::Drawing::Imaging::ImageFormat::Bmp);
		array<System::Byte>^ imageSrc = ms->ToArray();
		return BlackFilterIvt(imageSrc);
	}

	System::Drawing::Bitmap^ ImgProcUtil::BlackFilterIvt(array<System::Byte>^ imageSrc)
	{
		cv::Mat srcImg;
		pin_ptr<Byte> p = &imageSrc[0];   // entire array is now pinned
		unsigned char* cp = p;
		int docImageLen = imageSrc->Length;
		srcImg = loadImageFromByteArray(cp, docImageLen);
		//cv::imshow("Original", srcImg);

		//sharpen image

		cv::Mat sharpened;
		cv::GaussianBlur(srcImg, sharpened, cv::Size(0, 0), 3);
		//cv::imshow("GaussianBlur", sharpened);
		cv::addWeighted(srcImg, 1.5, sharpened, -0.5, 0, sharpened);
		//cv::imshow("addWeighted", sharpened);
		//cv::imwrite(imageNameBody + "_sharpened.png", sharpened);

		//Filter to extract image of black color only 
		cv::Mat maskBlack;
		cv::Mat imageHsv;
		cv::cvtColor(sharpened, imageHsv, cv::COLOR_BGR2HSV);
		cv::inRange(imageHsv, cv::Scalar(0, 0, 0), cv::Scalar(180, 255, 120), maskBlack);

		System::Collections::Generic::List<unsigned char>^ filteredImageData = gcnew System::Collections::Generic::List<unsigned char>();
		if (saveImageAsByteArray(maskBlack, ".bmp", filteredImageData))
		{
			array<unsigned char>^ buffer = gcnew array<unsigned char>(filteredImageData->Count);
			buffer = filteredImageData->ToArray();
			System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream(buffer);
			System::Drawing::Bitmap^ bmp = gcnew System::Drawing::Bitmap(ms);
			return bmp;
		}
		return nullptr;
	}

	System::Drawing::Bitmap^ ImgProcUtil::SharpenFilter(System::Drawing::Bitmap^ bmpSrc)
	{
		return SharpenFilter(bmpSrc, 5);
	}
	System::Drawing::Bitmap^ ImgProcUtil::SharpenFilter(System::Drawing::Bitmap^ bmpSrc, int kernel_param)
	{
		System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream();
		bmpSrc->Save(ms, System::Drawing::Imaging::ImageFormat::Bmp);
		array<System::Byte>^ imageSrc = ms->ToArray();
		return SharpenFilter(imageSrc, kernel_param);
	}

	System::Drawing::Bitmap^ ImgProcUtil::SharpenFilter(array<System::Byte>^ imageSrc)
	{
		return SharpenFilter(imageSrc, 5);
	}
	System::Drawing::Bitmap^ ImgProcUtil::SharpenFilter(array<System::Byte>^ imageSrc, int kernel_param)
	{
		cv::Mat srcImg;
		pin_ptr<Byte> p = &imageSrc[0];   // entire array is now pinned
		unsigned char* cp = p;
		int docImageLen = imageSrc->Length;
		srcImg = loadImageFromByteArray(cp, docImageLen);
		//cv::imshow("Original", srcImg);

		//sharpen image
		/// Initialize arguments for the filter
		cv::Point anchor = cv::Point(-1, -1);
		double delta = 0;
		int ddepth = -1;
		//cv::Mat kernel = (cv::Mat_<double>(3, 3) << 0, -1, 0, -1, kernel_param, -1, 0, -1, 0);
		cv::Mat kernel = (cv::Mat_<double>(3, 3) << 0, -1, 0, -1, kernel_param, -1, 0, -1, 0);

		/// Apply filter
		cv::Mat sharpened;
		filter2D(srcImg, sharpened, ddepth, kernel, anchor, delta, cv::BORDER_DEFAULT);

		System::Collections::Generic::List<unsigned char>^ filteredImageData = gcnew System::Collections::Generic::List<unsigned char>();
		if (saveImageAsByteArray(sharpened, ".bmp", filteredImageData))
		{
			array<unsigned char>^ buffer = gcnew array<unsigned char>(filteredImageData->Count);
			buffer = filteredImageData->ToArray();
			System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream(buffer);
			System::Drawing::Bitmap^ bmp = gcnew System::Drawing::Bitmap(ms);
			return bmp;
		}
		return nullptr;
	}

	System::Drawing::Bitmap^ ImgProcUtil::EdgeDetectionFilter(System::Drawing::Bitmap^ bmpSrc, int kernel_param)
	{
		System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream();
		bmpSrc->Save(ms, System::Drawing::Imaging::ImageFormat::Bmp);
		array<System::Byte>^ imageSrc = ms->ToArray();
		return SharpenFilter(imageSrc, kernel_param);
	}

	System::Drawing::Bitmap^ ImgProcUtil::EdgeDetectionFilter(array<System::Byte>^ imageSrc, int kernel_param)
	{
		cv::Mat srcImg;
		pin_ptr<Byte> p = &imageSrc[0];   // entire array is now pinned
		unsigned char* cp = p;
		int docImageLen = imageSrc->Length;
		srcImg = loadImageFromByteArray(cp, docImageLen);
		//cv::imshow("Original", srcImg);

		//sharpen image
		/// Initialize arguments for the filter
		cv::Point anchor = cv::Point(-1, -1);
		double delta = 0;
		int ddepth = -1;
		cv::Mat kernel = (cv::Mat_<double>(3, 3) << -1, -1, -1, -1, kernel_param, -1, -1, -1, -1);

		/// Apply filter
		cv::Mat sharpened;
		filter2D(srcImg, sharpened, ddepth, kernel, anchor, delta, cv::BORDER_DEFAULT);

		System::Collections::Generic::List<unsigned char>^ filteredImageData = gcnew System::Collections::Generic::List<unsigned char>();
		if (saveImageAsByteArray(sharpened, ".bmp", filteredImageData))
		{
			array<unsigned char>^ buffer = gcnew array<unsigned char>(filteredImageData->Count);
			buffer = filteredImageData->ToArray();
			System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream(buffer);
			System::Drawing::Bitmap^ bmp = gcnew System::Drawing::Bitmap(ms);
			return bmp;
		}
		return nullptr;
	}

	System::Drawing::Bitmap^ ImgProcUtil::PreprocessOCR(System::Drawing::Bitmap^ bmpSrc)
	{
		return PreprocessOCR(bmpSrc, 9, 2);
	}
	System::Drawing::Bitmap^ ImgProcUtil::PreprocessOCR(System::Drawing::Bitmap^ bmpSrc, int kernel_param, int morph_size)
	{
		System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream();
		bmpSrc->Save(ms, System::Drawing::Imaging::ImageFormat::Bmp);
		array<System::Byte>^ imageSrc = ms->ToArray();
		return PreprocessOCR(imageSrc, kernel_param, morph_size);
	}

	System::Drawing::Bitmap^ ImgProcUtil::PreprocessOCR(array<System::Byte>^ imageSrc)
	{
		return PreprocessOCR(imageSrc, 9, 2);
	}
	System::Drawing::Bitmap^ ImgProcUtil::PreprocessOCR(array<System::Byte>^ imageSrc, int kernel_param, int morph_size)
	{
		cv::Mat srcImg;
		pin_ptr<Byte> p = &imageSrc[0];   // entire array is now pinned
		unsigned char* cp = p;
		int docImageLen = imageSrc->Length;
		srcImg = loadImageFromByteArray(cp, docImageLen);
		//cv::imshow("Original", srcImg);

		// Edge detection
		/// Initialize arguments for the filter
		cv::Point anchor = cv::Point(-1, -1);
		double delta = 0;
		int ddepth = -1;
		cv::Mat kernel = (cv::Mat_<double>(3, 3) << -1, -1, -1, -1, kernel_param, -1, -1, -1, -1);

		cv::Mat edge;
		filter2D(srcImg, edge, ddepth, kernel, anchor, delta, cv::BORDER_DEFAULT);

		// Morph close to blur background
		cv::Mat morphImg = MorphClose(edge, morph_size);

		//Filter to extract image of black color only 
		cv::Mat maskBlack;
		cv::Mat imageHsv;
		cv::cvtColor(morphImg, imageHsv, cv::COLOR_BGR2HSV);
		cv::inRange(imageHsv, cv::Scalar(0, 0, 0), cv::Scalar(180, 255, 120), maskBlack);

		System::Collections::Generic::List<unsigned char>^ filteredImageData = gcnew System::Collections::Generic::List<unsigned char>();
		if (saveImageAsByteArray(maskBlack, ".bmp", filteredImageData))
		{
			array<unsigned char>^ buffer = gcnew array<unsigned char>(filteredImageData->Count);
			buffer = filteredImageData->ToArray();
			System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream(buffer);
			System::Drawing::Bitmap^ bmp = gcnew System::Drawing::Bitmap(ms);
			return bmp;
		}
		return nullptr;
	}

	System::Drawing::Bitmap^ ImgProcUtil::DeblurGray(System::Drawing::Bitmap^ bmpSrc)
	{
		int r = 53;
		int snr = 5200;
		System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream();
		bmpSrc->Save(ms, System::Drawing::Imaging::ImageFormat::Bmp);
		array<System::Byte>^ imageSrc = ms->ToArray();
		return DeblurGray(imageSrc, r, snr);
	}

	System::Drawing::Bitmap^ ImgProcUtil::DeblurGray(System::Drawing::Bitmap^ bmpSrc, int r, int snr)
	{
		System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream();
		bmpSrc->Save(ms, System::Drawing::Imaging::ImageFormat::Bmp);
		array<System::Byte>^ imageSrc = ms->ToArray();
		return DeblurGray(imageSrc, r, snr);
	}
#endif
	void calcPSF(cv::Mat& outputImg, cv::Size filterSize, int R)
	{
		cv::Mat h(filterSize, CV_32F, cv::Scalar(0));
		cv::Point point(filterSize.width / 2, filterSize.height / 2);
		circle(h, point, R, 255, -1, 8);
		cv::Scalar summa = sum(h);
		outputImg = h / summa[0];
	}
	void fftshift(const cv::Mat& inputImg, cv::Mat& outputImg)
	{
		outputImg = inputImg.clone();
		int cx = outputImg.cols / 2;
		int cy = outputImg.rows / 2;
		cv::Mat q0(outputImg, cv::Rect(0, 0, cx, cy));
		cv::Mat q1(outputImg, cv::Rect(cx, 0, cx, cy));
		cv::Mat q2(outputImg, cv::Rect(0, cy, cx, cy));
		cv::Mat q3(outputImg, cv::Rect(cx, cy, cx, cy));
		cv::Mat tmp;
		q0.copyTo(tmp);
		q3.copyTo(q0);
		tmp.copyTo(q3);
		q1.copyTo(tmp);
		q2.copyTo(q1);
		tmp.copyTo(q2);
	}
	void filter2DFreq(const cv::Mat& inputImg, cv::Mat& outputImg, const cv::Mat& H)
	{
		cv::Mat planes[2] = { cv::Mat_<float>(inputImg.clone()), cv::Mat::zeros(inputImg.size(), CV_32F) };
		cv::Mat complexI;
		merge(planes, 2, complexI);
		dft(complexI, complexI, cv::DFT_SCALE);
		cv::Mat planesH[2] = { cv::Mat_<float>(H.clone()), cv::Mat::zeros(H.size(), CV_32F) };
		cv::Mat complexH;
		merge(planesH, 2, complexH);
		cv::Mat complexIH;
		mulSpectrums(complexI, complexH, complexIH, 0);
		idft(complexIH, complexIH);
		split(complexIH, planes);
		outputImg = planes[0];
	}
	void calcWnrFilter(const cv::Mat& input_h_PSF, cv::Mat& output_G, double nsr)
	{
		cv::Mat h_PSF_shifted;
		fftshift(input_h_PSF, h_PSF_shifted);
		cv::Mat planes[2] = { cv::Mat_<float>(h_PSF_shifted.clone()), cv::Mat::zeros(h_PSF_shifted.size(), CV_32F) };
		cv::Mat complexI;
		merge(planes, 2, complexI);
		dft(complexI, complexI);
		split(complexI, planes);
		cv::Mat denom;
		pow(abs(planes[0]), 2, denom);
		denom += nsr;
		divide(planes[0], denom, output_G);
	}
#ifdef USE_SYSTEM_DRAWING
	System::Drawing::Bitmap^ ImgProcUtil::DeblurGray(array<System::Byte>^ imageSrc)
	{
		int r = 53;
		int snr = 5200;
		return DeblurGray(imageSrc, r, snr);
	}
	System::Drawing::Bitmap^ ImgProcUtil::DeblurGray(array<System::Byte>^ imageSrc, int r, int snr)
	{
		cv::Mat imgIn;
		pin_ptr<Byte> p = &imageSrc[0];   // entire array is now pinned
		unsigned char* cp = p;
		int docImageLen = imageSrc->Length;
		imgIn = loadImageFromByteArray(cp, docImageLen);
		cv::Mat imgGray;
		cv::cvtColor(imgIn, imgGray, cv::COLOR_BGR2GRAY);

		cv::Mat imgOut;
		// it needs to process even image only
		cv::Rect roi = cv::Rect(0, 0, imgGray.cols & -2, imgGray.rows & -2);
		//Hw calculation (start)
		cv::Mat Hw, h;
		calcPSF(h, roi.size(), r);
		calcWnrFilter(h, Hw, 1.0 / double(snr));
		//Hw calculation (stop)
		// filtering (start)
		filter2DFreq(imgGray(roi), imgOut, Hw);
		// filtering (stop)
		imgOut.convertTo(imgOut, CV_8U);
		normalize(imgOut, imgOut, 0, 255, cv::NORM_MINMAX);

		System::Collections::Generic::List<unsigned char>^ filteredImageData = gcnew System::Collections::Generic::List<unsigned char>();
		if (saveImageAsByteArray(imgOut, ".bmp", filteredImageData))
		{
			array<unsigned char>^ buffer = gcnew array<unsigned char>(filteredImageData->Count);
			buffer = filteredImageData->ToArray();
			System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream(buffer);
			System::Drawing::Bitmap^ bmp = gcnew System::Drawing::Bitmap(ms);
			return bmp;
		}
		return nullptr;
	}

	//////////
	System::Drawing::Bitmap^ ImgProcUtil::HSVFilter(System::Drawing::Bitmap^ bmpSrc, byte HL, byte SL, byte VL, byte HH, byte SH, byte VH)
	{
		System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream();
		bmpSrc->Save(ms, System::Drawing::Imaging::ImageFormat::Bmp);
		array<System::Byte>^ imageSrc = ms->ToArray();
		return HSVFilter(imageSrc, HL, SL, VL, HH, SH, VH);
	}

	System::Drawing::Bitmap^ ImgProcUtil::HSVFilter(array<System::Byte>^ imageSrc, byte HL, byte SL, byte VL, byte HH, byte SH, byte VH)
	{
		cv::Mat srcImg;
		pin_ptr<Byte> p = &imageSrc[0];   // entire array is now pinned
		unsigned char* cp = p;
		int docImageLen = imageSrc->Length;
		srcImg = loadImageFromByteArray(cp, docImageLen);

		//Filter to extract image of hologram color only 
		cv::Mat maskHologram = HSVFilter(srcImg, HL, SL, VL, HH, SH, VH);

		System::Collections::Generic::List<unsigned char>^ filteredImageData = gcnew System::Collections::Generic::List<unsigned char>();
		if (saveImageAsByteArray(maskHologram, ".bmp", filteredImageData))
		{
			array<unsigned char>^ buffer = gcnew array<unsigned char>(filteredImageData->Count);
			buffer = filteredImageData->ToArray();
			System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream(buffer);
			System::Drawing::Bitmap^ bmp = gcnew System::Drawing::Bitmap(ms);
			return bmp;
		}
		return nullptr;
	}
#endif
	cv::Mat ImgProcUtil::HSVFilter(cv::Mat srcImg, byte HL, byte SL, byte VL, byte HH, byte SH, byte VH)
	{
		//sharpen image
		cv::Mat sharpened;
		cv::GaussianBlur(srcImg, sharpened, cv::Size(0, 0), 3);
		cv::addWeighted(srcImg, 1.5, sharpened, -0.5, 0, sharpened);

		//Filter to extract image of black color only 
		cv::Mat maskHologram;
		cv::Mat imageHsv;
		cv::cvtColor(sharpened, imageHsv, cv::COLOR_BGR2HSV);
		cv::inRange(imageHsv, cv::Scalar(HL, SL, VL), cv::Scalar(HH, SH, VH), maskHologram);

		return maskHologram;
	}

#ifdef USE_SYSTEM_DRAWING
	int ImgProcUtil::DetectByHSVRange(System::Drawing::Bitmap^ bmpSrc, byte HL, byte SL, byte VL, byte HH, byte SH, byte VH)
	{
		System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream();
		bmpSrc->Save(ms, System::Drawing::Imaging::ImageFormat::Bmp);
		array<System::Byte>^ imageSrc = ms->ToArray();
		return DetectByHSVRange(imageSrc, HL, SL, VL, HH, SH, VH);
	}
#endif
	int ImgProcUtil::DetectByHSVRange(array<System::Byte>^ imageSrc, byte HL, byte SL, byte VL, byte HH, byte SH, byte VH)
	{
		cv::Mat srcImg;
		pin_ptr<Byte> p = &imageSrc[0];   // entire array is now pinned
		unsigned char* cp = p;
		int docImageLen = imageSrc->Length;
		srcImg = loadImageFromByteArray(cp, docImageLen);

		//sharpen image
		cv::Mat sharpened;
		cv::GaussianBlur(srcImg, sharpened, cv::Size(0, 0), 3);
		cv::addWeighted(srcImg, 1.5, sharpened, -0.5, 0, sharpened);

		//Filter to extract image of hologram color only 
		cv::Mat maskHologram = HSVFilter(srcImg, HL, SL, VL, HH, SH, VH);
		int sumVal = 0;
		cv::Scalar sumHologram = cv::sum(maskHologram);
		sumVal = sumHologram[0] / 255;
		return sumVal;
	}
	//////////

#ifdef USE_SYSTEM_DRAWING
	System::Drawing::Bitmap^ ImgProcUtil::HologramFilter(System::Drawing::Bitmap^ bmpSrc)
	{
		System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream();
		bmpSrc->Save(ms, System::Drawing::Imaging::ImageFormat::Bmp);
		array<System::Byte>^ imageSrc = ms->ToArray();
		return HologramFilter(imageSrc);
	}

	System::Drawing::Bitmap^ ImgProcUtil::HologramFilter(array<System::Byte>^ imageSrc)
	{
		cv::Mat srcImg;
		pin_ptr<Byte> p = &imageSrc[0];   // entire array is now pinned
		unsigned char* cp = p;
		int docImageLen = imageSrc->Length;
		srcImg = loadImageFromByteArray(cp, docImageLen);

		//Filter to extract image of hologram color only 
		cv::Mat maskHologram = HologramFilter(srcImg);

		System::Collections::Generic::List<unsigned char>^ filteredImageData = gcnew System::Collections::Generic::List<unsigned char>();
		if (saveImageAsByteArray(maskHologram, ".bmp", filteredImageData))
		{
			array<unsigned char>^ buffer = gcnew array<unsigned char>(filteredImageData->Count);
			buffer = filteredImageData->ToArray();
			System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream(buffer);
			System::Drawing::Bitmap^ bmp = gcnew System::Drawing::Bitmap(ms);
			return bmp;
		}
		return nullptr;
	}
#endif
	cv::Mat ImgProcUtil::HologramFilter(cv::Mat srcImg)
	{
		//sharpen image
		cv::Mat sharpened;
		cv::GaussianBlur(srcImg, sharpened, cv::Size(0, 0), 3);
		cv::addWeighted(srcImg, 1.5, sharpened, -0.5, 0, sharpened);

		//Filter to extract image of black color only 
		cv::Mat maskHologram;
		cv::Mat imageHsv;
		cv::cvtColor(sharpened, imageHsv, cv::COLOR_BGR2HSV);
		cv::inRange(imageHsv, cv::Scalar(0, 0, 255), cv::Scalar(180, 50, 255), maskHologram);

		return maskHologram;
	}

#ifdef USE_SYSTEM_DRAWING
	int ImgProcUtil::DetectHologram(System::Drawing::Bitmap^ bmpSrc)
	{
		System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream();
		bmpSrc->Save(ms, System::Drawing::Imaging::ImageFormat::Bmp);
		array<System::Byte>^ imageSrc = ms->ToArray();
		return DetectHologram(imageSrc);
	}
#endif
	int ImgProcUtil::DetectHologram(array<System::Byte>^ imageSrc)
	{
		cv::Mat srcImg;
		pin_ptr<Byte> p = &imageSrc[0];   // entire array is now pinned
		unsigned char* cp = p;
		int docImageLen = imageSrc->Length;
		srcImg = loadImageFromByteArray(cp, docImageLen);
		//cv::imshow("Original", srcImg);

		//sharpen image

		cv::Mat sharpened;
		cv::GaussianBlur(srcImg, sharpened, cv::Size(0, 0), 3);
		cv::addWeighted(srcImg, 1.5, sharpened, -0.5, 0, sharpened);

		//Filter to extract image of hologram color only 
		cv::Mat maskHologram = HologramFilter(srcImg);
		int sumVal = 0;
		//for (int i = 0; i < maskHologram.cols; i++) {
		//	for (int j = 0; j < maskHologram.rows; j++) {
		//		int val = maskHologram.data[j * maskHologram.cols + maskHologram.channels() + i * maskHologram.channels()];
		//		sumVal += (val > 0) ? 1 : 0;
		//	}
		//}
		cv::Scalar sumHologram = cv::sum(maskHologram);
		sumVal = sumHologram[0] / 255;
		return sumVal;
		//int val = sumVal / (srcImg.cols * srcImg.rows);
		//return val;
	}

	////////////////////////////////////////
#ifdef USE_SYSTEM_DRAWING
	System::Drawing::Bitmap^ ImgProcUtil::InpaintHologram(System::Drawing::Bitmap^ bmpSrc, int radius)
	{
		System::IO::MemoryStream^ msSrc = gcnew System::IO::MemoryStream();
		bmpSrc->Save(msSrc, System::Drawing::Imaging::ImageFormat::Bmp);
		array<System::Byte>^ imageSrc = msSrc->ToArray();

		cv::Mat srcImg;
		pin_ptr<Byte> pSrc = &imageSrc[0];   // entire array is now pinned
		unsigned char* cpSrc = pSrc;
		int srcImageLen = imageSrc->Length;
		srcImg = loadImageFromByteArray(cpSrc, srcImageLen);

		cv::Mat maskImg = HologramFilter(srcImg);

		cv::Mat outImg = Inpaint(srcImg, maskImg, radius);

		System::Collections::Generic::List<unsigned char>^ filteredImageData = gcnew System::Collections::Generic::List<unsigned char>();
		if (saveImageAsByteArray(outImg, ".bmp", filteredImageData))
		{
			array<unsigned char>^ buffer = gcnew array<unsigned char>(filteredImageData->Count);
			buffer = filteredImageData->ToArray();
			System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream(buffer);
			System::Drawing::Bitmap^ bmp = gcnew System::Drawing::Bitmap(ms);
			return bmp;
		}
		return nullptr;
	}
#endif
	cv::Mat ImgProcUtil::Inpaint(cv::Mat srcImg, cv::Mat maskImg, int radius)
	{
		cv::Mat outImg(srcImg.rows, srcImg.cols, srcImg.type());
		cv::inpaint(srcImg, maskImg, outImg, radius, cv::INPAINT_TELEA);
		return outImg;
	}
	////////////////////////////////////////

#ifdef USE_SYSTEM_DRAWING
	System::Drawing::Bitmap^ ImgProcUtil::CvtToGray(System::Drawing::Bitmap^ bmpSrc)
	{
		System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream();
		bmpSrc->Save(ms, System::Drawing::Imaging::ImageFormat::Bmp);
		array<System::Byte>^ imageSrc = ms->ToArray();
		return CvtToGray(imageSrc);
	}

	System::Drawing::Bitmap^ ImgProcUtil::CvtToGray(array<System::Byte>^ imageSrc)
	{
		cv::Mat srcImg;
		pin_ptr<Byte> p = &imageSrc[0];   // entire array is now pinned
		unsigned char* cp = p;
		int docImageLen = imageSrc->Length;
		srcImg = loadImageFromByteArray(cp, docImageLen);
		//cv::imshow("Original", srcImg);

		cv::Mat grayImg;
		cvtColor(srcImg, grayImg, CV_BGR2GRAY);

		System::Collections::Generic::List<unsigned char>^ convertedImageData = gcnew System::Collections::Generic::List<unsigned char>();
		if (saveImageAsByteArray(grayImg, ".bmp", convertedImageData))
		{
			array<unsigned char>^ buffer = gcnew array<unsigned char>(convertedImageData->Count);
			buffer = convertedImageData->ToArray();
			System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream(buffer);
			System::Drawing::Bitmap^ bmp = gcnew System::Drawing::Bitmap(ms);
			return bmp;
		}
		return nullptr;
	}

	System::Drawing::Bitmap^ ImgProcUtil::CvtToGrayAndOpenClose(System::Drawing::Bitmap^ bmpSrc)
	{
		System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream();
		bmpSrc->Save(ms, System::Drawing::Imaging::ImageFormat::Bmp);
		array<System::Byte>^ imageSrc = ms->ToArray();
		return CvtToGrayAndOpenClose(imageSrc);
	}

	System::Drawing::Bitmap^ ImgProcUtil::CvtToGrayAndOpenClose(array<System::Byte>^ imageSrc)
	{
		cv::Mat srcImg;
		pin_ptr<Byte> p = &imageSrc[0];   // entire array is now pinned
		unsigned char* cp = p;
		int docImageLen = imageSrc->Length;
		srcImg = loadImageFromByteArray(cp, docImageLen);
		//cv::imshow("Original", srcImg);

		cv::Mat grayImg;
		cvtColor(srcImg, grayImg, CV_BGR2GRAY);

		int morph_size_open = 1;
		int morph_size_close = 19;
		cv::Mat element_open = getStructuringElement(cv::MORPH_RECT, cv::Size(2 * morph_size_open + 1, 2 * morph_size_open + 1), cv::Point(morph_size_open, morph_size_open));
		cv::Mat element_close = getStructuringElement(cv::MORPH_RECT, cv::Size(2 * morph_size_close + 1, 2 * morph_size_close + 1), cv::Point(morph_size_close, morph_size_close));
		cv::Mat morphImg;
		morphologyEx(grayImg, morphImg, cv::MORPH_OPEN, element_open, cv::Point(-1, -1));
		//morphologyEx(morphImg, morphImg, cv::MORPH_CLOSE, element_close, cv::Point(-1, -1));

		System::Collections::Generic::List<unsigned char>^ convertedImageData = gcnew System::Collections::Generic::List<unsigned char>();
		if (saveImageAsByteArray(morphImg, ".bmp", convertedImageData))
		{
			array<unsigned char>^ buffer = gcnew array<unsigned char>(convertedImageData->Count);
			buffer = convertedImageData->ToArray();
			System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream(buffer);
			System::Drawing::Bitmap^ bmp = gcnew System::Drawing::Bitmap(ms);
			return bmp;
		}
		return nullptr;
	}

	System::Drawing::Bitmap^ ImgProcUtil::MorphOpen(System::Drawing::Bitmap^ bmpSrc, int morph_size)
	{
		System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream();
		bmpSrc->Save(ms, System::Drawing::Imaging::ImageFormat::Bmp);
		array<System::Byte>^ imageSrc = ms->ToArray();
		return MorphOpen(imageSrc, morph_size);
	}
#endif
	cv::Mat ImgProcUtil::MorphOpen(cv::Mat srcImg, int morph_size)
	{
		cv::Mat element_open = getStructuringElement(cv::MORPH_RECT, cv::Size(morph_size, morph_size));
		cv::Mat morphImg;
		morphologyEx(srcImg, morphImg, cv::MORPH_OPEN, element_open);
		return morphImg;
	}

#ifdef USE_SYSTEM_DRAWING
	System::Drawing::Bitmap^ ImgProcUtil::MorphOpen(array<System::Byte>^ imageSrc, int morph_size)
	{
		cv::Mat srcImg;
		pin_ptr<Byte> p = &imageSrc[0];   
		unsigned char* cp = p;
		int docImageLen = imageSrc->Length;
		srcImg = loadImageFromByteArray(cp, docImageLen);

		cv::Mat morphImg = MorphOpen(srcImg, morph_size);

		System::Collections::Generic::List<unsigned char>^ convertedImageData = gcnew System::Collections::Generic::List<unsigned char>();
		if (saveImageAsByteArray(morphImg, ".bmp", convertedImageData))
		{
			array<unsigned char>^ buffer = gcnew array<unsigned char>(convertedImageData->Count);
			buffer = convertedImageData->ToArray();
			System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream(buffer);
			System::Drawing::Bitmap^ bmp = gcnew System::Drawing::Bitmap(ms);
			return bmp;
		}
		return nullptr;
	}

	System::Drawing::Bitmap^ ImgProcUtil::MorphClose(System::Drawing::Bitmap^ bmpSrc, int morph_size)
	{
		System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream();
		bmpSrc->Save(ms, System::Drawing::Imaging::ImageFormat::Bmp);
		array<System::Byte>^ imageSrc = ms->ToArray();
		return MorphClose(imageSrc, morph_size);
	}
#endif
	cv::Mat ImgProcUtil::MorphClose(cv::Mat srcImg, int morph_size)
	{
		cv::Mat element_close = getStructuringElement(cv::MORPH_RECT, cv::Size(morph_size, morph_size));
		cv::Mat morphImg;
		morphologyEx(srcImg, morphImg, cv::MORPH_CLOSE, element_close);
		return morphImg;
	}

#ifdef USE_SYSTEM_DRAWING
	System::Drawing::Bitmap^ ImgProcUtil::MorphClose(array<System::Byte>^ imageSrc, int morph_size)
	{
		cv::Mat srcImg;
		pin_ptr<Byte> p = &imageSrc[0];
		unsigned char* cp = p;
		int docImageLen = imageSrc->Length;
		srcImg = loadImageFromByteArray(cp, docImageLen);

		cv::Mat morphImg = MorphClose(srcImg, morph_size);

		System::Collections::Generic::List<unsigned char>^ convertedImageData = gcnew System::Collections::Generic::List<unsigned char>();
		if (saveImageAsByteArray(morphImg, ".bmp", convertedImageData))
		{
			array<unsigned char>^ buffer = gcnew array<unsigned char>(convertedImageData->Count);
			buffer = convertedImageData->ToArray();
			System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream(buffer);
			System::Drawing::Bitmap^ bmp = gcnew System::Drawing::Bitmap(ms);
			return bmp;
		}
		return nullptr;
	}

	System::Drawing::Bitmap^ ImgProcUtil::AdjustContrastBrightness(System::Drawing::Bitmap^ bmpSrc, double contrast, int brightness)
	{
		System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream();
		bmpSrc->Save(ms, System::Drawing::Imaging::ImageFormat::Bmp);
		array<System::Byte>^ imageSrc = ms->ToArray();
		System::Collections::Generic::List<unsigned char>^ outImageData = gcnew System::Collections::Generic::List<unsigned char>();
		if(AdjustContrastBrightness(imageSrc, contrast, brightness, outImageData))
		{
			array<unsigned char>^ buffer = gcnew array<unsigned char>(outImageData->Count);
			buffer = outImageData->ToArray();
			System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream(buffer);
			System::Drawing::Bitmap^ bmp = gcnew System::Drawing::Bitmap(ms);
			return bmp;
		}
		return nullptr;
	}
#endif
	cv::Mat ImgProcUtil::AdjustContrastBrightness(cv::Mat srcImg, double contrast, int brightness)
	{
		cv::Mat new_image = cv::Mat::zeros(srcImg.size(), srcImg.type());
		srcImg.convertTo(new_image, -1, contrast, brightness);
		return new_image;
	}

	bool ImgProcUtil::AdjustContrastBrightness(array<System::Byte>^ imageSrc, double contrast, int brightness, System::Collections::Generic::List<System::Byte>^% imageOut)
	{
		cv::Mat srcImg;
		pin_ptr<Byte> p = &imageSrc[0];
		unsigned char* cp = p;
		int docImageLen = imageSrc->Length;
		srcImg = loadImageFromByteArray(cp, docImageLen);

		cv::Mat outImg = AdjustContrastBrightness(srcImg, contrast, brightness);

		bool bRet = saveImageAsByteArray(outImg, ".png", imageOut);
		outImg.release();
		return bRet;
	}

#ifdef USE_SYSTEM_DRAWING
	int ImgProcUtil::GetBrightness(System::Drawing::Bitmap^ bmpSrc)
	{
		System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream();
		bmpSrc->Save(ms, System::Drawing::Imaging::ImageFormat::Bmp);
		array<System::Byte>^ imageSrc = ms->ToArray();
		System::Collections::Generic::List<unsigned char>^ outImageData = gcnew System::Collections::Generic::List<unsigned char>();
		return GetBrightness(imageSrc);
	}
#endif
	int ImgProcUtil::GetBrightness(cv::Mat srcImg)
	{
		//https://stackoverflow.com/questions/14243472/estimate-brightness-of-an-image-opencv
		cv::Mat imageHsv;
		cv::cvtColor(srcImg, imageHsv, cv::COLOR_BGR2HSV);
		const auto result = cv::mean(imageHsv);
		// cv::mean() will return 3 numbers, one for each channel:
		//      0=hue
		//      1=saturation
		//      2=value (brightness)
		return result[2];
	}

	int ImgProcUtil::GetBrightness(array<System::Byte>^ imageSrc)
	{
		cv::Mat srcImg;
		pin_ptr<Byte> p = &imageSrc[0];
		unsigned char* cp = p;
		int docImageLen = imageSrc->Length;
		srcImg = loadImageFromByteArray(cp, docImageLen);
		return GetBrightness(srcImg);
	}

	/*
		cv::Mat element_open = getStructuringElement(cv::MORPH_RECT, cv::Size(2 * morph_size_open + 1, 2 * morph_size_open + 1), cv::Point(morph_size_open, morph_size_open));
		cv::Mat element_close = getStructuringElement(cv::MORPH_RECT, cv::Size(2 * morph_size_close + 1, 2 * morph_size_close + 1), cv::Point(morph_size_close, morph_size_close));

		cv::Mat gaussianImg;
		//GaussianBlur(grayImg, grayImg3, cv::Size(15, 15), sqrt(2));
		//morphologyEx(grayImg3, _morphImg, cv::MORPH_OPEN, element_open, cv::Point(-1, -1));
		GaussianBlur(grayImg, gaussianImg, cv::Size(15, 15), sqrt(2));
		cv::Mat morphImg;
		morphologyEx(gaussianImg, morphImg, cv::MORPH_OPEN, element_open, cv::Point(-1, -1));
*/
	//**** Finding Distance
	float calcuteDistance(const std::vector<int> mainSizeMatrix, std::vector<float> x, std::vector<float> y) {
		const float lowestVal = 0.001;
		float b2 = 0;
		float m2 = 0;

		int diffCol = (x[x.size() - 1] - x[0]);
		int diffRow = (y[y.size() - 1] - y[0]);

		if ((int)diffCol == 0)
		{
			diffCol = 1;
			m2 = lowestVal;
			b2 = x[0];
		}
		else if ((int)diffRow == 0)
		{
			diffRow = 1;
			m2 = lowestVal;
			b2 = y[0];
		}
		else
		{
			m2 = (float)diffRow / diffCol;
			b2 = y[1] - ((m2)* x[1]);
		}

		float centerY = mainSizeMatrix[0] / 2;
		float centerX = mainSizeMatrix[1] / 2;
		float b1 = centerY + ((1 / m2) * centerX);
		float centerX2 = ((b1 - b2) / (m2 + (1 / m2)));
		float centerY2 = ((-1 / m2) * centerX2 + b1);

		if (mainSizeMatrix[1] <= mainSizeMatrix[0])
		{
			if (floor(abs(diffCol)) <= 1)
			{
				centerX2 = centerY2;
				centerY2 = centerX;
			}
			if (floor(abs(diffRow)) <= 1)
			{
				centerX2 = centerY2;
				centerX2 = centerX;
			}
		}
		if (mainSizeMatrix[1] >= mainSizeMatrix[0])
		{
			if (floor(abs(diffRow)) <= 1)
			{
				//I have no idea what he had planned here....
			}
			if (floor(abs(diffCol)) <= 1)
			{
				centerX2 = centerY2;
				centerY2 = centerY;
			}
		}

		float distPV = sqrt(pow((centerX2 - centerX), 2) + pow((centerY2 - centerY), 2));

		if (((diffCol == mainSizeMatrix[0]) && (ceil(centerY2) < centerY)) || ((diffRow == mainSizeMatrix[1]) && (ceil(centerX2) < centerX)))
		{
			return (-1 * distPV);
		}
		else
		{
			return distPV;
		}
	}

	// **** Get min & max
	float getMaxInt(std::vector<float>& v) {
		return *max_element(v.begin(), v.end());
	};

	float getMinInt(std::vector<float>& v) {
		return *min_element(v.begin(), v.end());
	};

	std::vector<float> getCross(std::vector<float> a, std::vector<float> b) {

		std::vector <float> c;
		float zc = a[0] * b[1] - a[1] * b[0];
		c.push_back((a[1] * b[2] - a[2] * b[1]) / zc);
		c.push_back((a[2] * b[0] - a[0] * b[2]) / zc);
		return c;

	};

	//**** length of edge lines
	float vectorDistance(std::vector<float> a, std::vector<float> b)
	{
		float dist = 0.0;
		dist = sqrt(pow(a[0] - b[0], 2) + pow(a[1] - b[1], 2));
		return dist;
	}

	bool peaklineSortComp(const std::vector<float> & a, const std::vector<float> & b) {
		return (abs(a[1])) < (abs(b[1]));
	}
#if false
	void detectEdge(cv::Mat orgImg, System::Drawing::Point^% pt1, System::Drawing::Point^% pt2, System::Drawing::Point^% pt3, System::Drawing::Point^% pt4) {
		cv::Mat resizedImg; // = imageCV.clone();
		//--------------------
		//1 Resizing image
		//--------------------
		cv::Size s;;
		//double imgHeight = pickedImage.size.height;
		//double imgWidth = pickedImage.size.width;
		double imgHeight = orgImg.rows;
		double imgWidth = orgImg.cols;
		double imgHVRatio = imgHeight / imgWidth;
		if (imgHVRatio > 1) {
			imgHeight = 800;
			imgWidth = imgHeight / imgHVRatio;
			s = cv::Size(imgWidth, imgHeight);
		}
		else {
			imgWidth = 800;
			imgHeight = imgWidth * imgHVRatio;
			s = cv::Size(imgWidth, imgHeight);
		}
		double imgScalingRatio = imgHeight / orgImg.rows;
		resize(orgImg, resizedImg, s, 0, 0, CV_INTER_CUBIC);

		//UIImage *imageResized = MatToUIImage(resizedImg);
		//_imageView1.image = imageResized;

		//--------------------
		//2 Convert image to Grayscale
		//--------------------
		cv::Mat grayImg;
		cvtColor(resizedImg, grayImg, CV_RGBA2GRAY);

		//----------------------------------------
		//3 Using opening & closing Morphologic & Guasian filter ( Prepaer Image for edge detection)
		//----------------------------------------

		cv::Mat meane, covs;
		cv::calcCovarMatrix(grayImg, covs, meane, CV_COVAR_NORMAL | CV_COVAR_ROWS | CV_COVAR_SCALE);

		meane = meane / grayImg.rows;

		//cv::Scalar meanf = mean(meane);
		cv::Scalar meanIma = mean(grayImg);
		cv::Scalar meanedge = mean(covs);

		//float defualtmeancov = (float)meanf[0];
		int meanImag = (int)meanIma[0];
		int meanedgeint2 = (int)meanedge[0];
		int smeanT = sqrt(meanedgeint2);
		cv::Mat adapImg;

		int morph_size_open = 1; // Consider size of element size base on size of image
		int morph_size_close = 21; // Consider size of element size base on size of image
		//int repeatFindLine = 0;


		int adapConstant = 5 * smeanT;
		if (smeanT < 20 && smeanT > 10)
		{
			if (meanImag < 200)
				morph_size_close = 19;
			else if (meanImag < 180)
				morph_size_close = 19;
			adapConstant = 5 * smeanT;
		}
		else if (smeanT < 10)
		{
			morph_size_close = 19;
			adapConstant = 5 * smeanT;
		}
		else if (smeanT >= 20 && smeanT < 30)
		{

			if (meanImag < 200)
				morph_size_close = 19;
			else if (meanImag > 180)
				morph_size_close = 1;
			morph_size_close = 19;
			adapConstant = smeanT * 2.5;

		}
		else if (smeanT > 30)
		{
			adapConstant = 1 * smeanT;
			morph_size_close = 12;
		}

		//adaptiveThreshold(grayImg3, _meanAdaptiveThresholding, smeanT, CV_ADAPTIVE_THRESH_MEAN_C, cv::THRESH_BINARY, 5, adapConstant);
		cv::Mat meanAdaptiveThresholding;
		adaptiveThreshold(grayImg, meanAdaptiveThresholding, smeanT, CV_ADAPTIVE_THRESH_MEAN_C, cv::THRESH_BINARY, 5, adapConstant);

		cv::Mat element_open = getStructuringElement(cv::MORPH_RECT, cv::Size(2 * morph_size_open + 1, 2 * morph_size_open + 1), cv::Point(morph_size_open, morph_size_open));
		cv::Mat element_close = getStructuringElement(cv::MORPH_RECT, cv::Size(2 * morph_size_close + 1, 2 * morph_size_close + 1), cv::Point(morph_size_close, morph_size_close));

		cv::Mat gaussianImg;
		//GaussianBlur(grayImg, grayImg3, cv::Size(15, 15), sqrt(2));
		//morphologyEx(grayImg3, _morphImg, cv::MORPH_OPEN, element_open, cv::Point(-1, -1));
		GaussianBlur(grayImg, gaussianImg, cv::Size(15, 15), sqrt(2));
		cv::Mat morphImg;
		morphologyEx(gaussianImg, morphImg, cv::MORPH_OPEN, element_open, cv::Point(-1, -1));
		morphologyEx(morphImg, morphImg, cv::MORPH_CLOSE, element_close, cv::Point(-1, -1));

		GaussianBlur(morphImg, morphImg, cv::Size(15, 15), sqrt(2));
		cv::Mat edgePreparedImg = morphImg - meanAdaptiveThresholding;
		//UIImage *imageOut = MatToUIImage(edgePreparedImg);

		//----------------------------------------
		//4 Finding the lines by canny Filtering
		//----------------------------------------
		cv::Mat cannyImg;
		int lowcanny = 1;
		int highcanny = 39;
		Canny(edgePreparedImg, cannyImg, lowcanny, highcanny, 3);
		//UIImage *imageOut = MatToUIImage(cannyImg);


		//----------------------------------------
		//5 Extract points of Images
		//----------------------------------------
		int edge[8];
		std::vector<cv::Vec3f> linesOut;
		HoughLines(cannyImg, linesOut, 1, CV_PI / 180, 100, 0, 0);

		/////// ***** peaklines
		std::vector <std::vector<float> > peakLines;
		std::vector <std::vector<float> > peakLinesSort;

		// vector<vector<float>> peaklines = Peaklines.filterLines(vector<vec3f> lines, int lineRange)
		const int lines_toloranc = 64;
		for (size_t i = 0; i < linesOut.size(); i++)
		{
			std::vector <float> temLinesVector;
			if (i <= lines_toloranc)
			{ // TODONE : find inteligens to numbers of the lines
				temLinesVector.push_back((linesOut[i][0]));
				temLinesVector.push_back(linesOut[i][1]);
				peakLines.push_back(temLinesVector);
			}
		}
		//end

		int numLines = (int)peakLines.size();
		float theta;
		float rheo;
		std::vector<float> distanceDU;
		std::vector<float> distanceDV;

		// Initilization to NAN (not zaro) using for find the longest distnaces
		for (size_t i = 0; i < numLines; i++)
		{
			distanceDV.push_back(NAN);
			distanceDU.push_back(NAN);
		}
		// Check lines if vertical or horizantal are
		for (int i = 0; i < peakLines.size(); i++)
		{
			rheo = peakLines[i][0];
			theta = peakLines[i][1];

			if (rheo < 0)
			{
				theta = theta - PI;
				rheo = abs(rheo);
			}

			if (theta < PI / 2 - 0.5 && theta > PI / 2 - 0.5)
			{
				rheo = rheo * -1;
				theta = theta - PI / 2;
			}

			std::vector<float> X;
			std::vector<float> Y;

			int fx = ((abs(theta) > (PI / 4)) && (abs(theta) < (3 * PI / 4))) ? resizedImg.size[0] : resizedImg.size[1];;
			for (size_t i = 0; i <= fx; i++)
			{
				X.push_back(i);
			}

			for (size_t i = 0; i <= fx; i++)
			{
				Y.push_back(i);
			}
			if ((abs(theta) > (PI / 4)) && (abs(theta) < (3 * PI / 4)))
			{
				for (size_t i = 0; i < X.size(); i++)
				{ // another IDE be j < X
					Y[i] = 1 + ((rheo - X[i] * cos(theta)) / sin(theta));
				}
				// Call the distance function to find the deistance
				std::vector<int> vM(2);
				vM[0] = resizedImg.size[0];
				vM[1] = resizedImg.size[1];
				distanceDV[i] = (calcuteDistance(vM, X, Y));
			}
			else
			{
				for (size_t j = 0; j < Y.size(); j++)
				{ // another IDE be j < Y
					X[j] = 1 + ((rheo - Y[j] * sin(theta)) / cos(theta));
				}
				// Call the distance function to find the deistance
				std::vector<int> vM(2);
				vM[0] = resizedImg.size[0];
				vM[1] = resizedImg.size[1];
				distanceDU[i] = (calcuteDistance(vM, X, Y));
			}
		}

		if (numLines <= 0)
			return;

		// Finding the longest distance from center consider the desired line
		if (isnan(distanceDV[0]))
			distanceDV[0] = 0;
		if (isnan(distanceDU[0]))
			distanceDU[0] = 0;

		int iDUMax = 0;
		int iDUMin = 0;
		int iDVMax = 0;
		int iDVMin = 0;

		float maxDU = 0;
		float minDU = 0;
		float maxDV = 0;
		float minDV = 0;

		maxDU = getMaxInt(distanceDU);
		minDU = getMinInt(distanceDU);
		maxDV = getMaxInt(distanceDV);
		minDV = getMinInt(distanceDV);

		for (int i = 0; i < numLines; ++i) {
			if (maxDU == distanceDU[i]) {
				iDUMax = i;
			}
			if (minDU == distanceDU[i]) {
				iDUMin = i;
			}
			if (maxDV == distanceDV[i]) {
				iDVMax = i;
			}
			if (minDV == distanceDV[i]) {
				iDVMin = i;
			}
		}
		// Determine the desired Peaklines

		int assumptionLine = 1;
		std::vector<std::vector<float> > ReseveLine;
		// If any reason couldn't find total 4 lines, it asumpt the following asumption lines
		if (maxDU == 0 || minDU == 0 || maxDV == 0 || minDV == 0)
		{
			assumptionLine = 0;

			float temResRho;
			int temSize;
			float temResTheta;

			for (size_t i = 0; i < 4; i++)
			{
				std::vector<float> tempResLine;

				if ((i == 0) || (i == 1))
				{
					temResTheta = 0;
					temSize = resizedImg.size[1];
				}
				else
				{
					temResTheta = PI / 2;
					temSize = resizedImg.size[0];
				}

				if ((i == 0) || (i == 2))
				{
					temResRho = 0.98 * temSize;
				}
				else
				{
					temResRho = 0.02 * temSize;
				}
				tempResLine.push_back(temResRho);
				tempResLine.push_back(temResTheta);
				ReseveLine.push_back(tempResLine);
			}
		}

		std::vector <std::vector<float> > desiredPeakLines;

		if (assumptionLine == 1)
		{
			// Determine the desired Peaklines
			desiredPeakLines.push_back(peakLines[iDUMax]);
			desiredPeakLines.push_back(peakLines[iDUMin]);
			desiredPeakLines.push_back(peakLines[iDVMax]);
			desiredPeakLines.push_back(peakLines[iDVMin]);

			// Using peaklines for sorting
			peakLinesSort.push_back(peakLines[iDUMax]);
			peakLinesSort.push_back(peakLines[iDUMin]);
			peakLinesSort.push_back(peakLines[iDVMax]);
			peakLinesSort.push_back(peakLines[iDVMin]);
		}
		else
		{
			if (maxDU == 0)
			{
				desiredPeakLines.push_back(ReseveLine[0]);
				peakLinesSort.push_back(ReseveLine[0]);
			}
			else
			{
				desiredPeakLines.push_back(peakLines[iDUMax]);
				peakLinesSort.push_back(peakLines[iDUMax]);
			}
			if (minDU == 0)
			{
				desiredPeakLines.push_back(ReseveLine[1]);
				peakLinesSort.push_back(ReseveLine[1]);
			}
			else
			{
				desiredPeakLines.push_back(peakLines[iDUMin]);
				peakLinesSort.push_back(peakLines[iDUMin]);
			}
			if (maxDV == 0)
			{
				desiredPeakLines.push_back(ReseveLine[2]);
				peakLinesSort.push_back(ReseveLine[2]);
			}
			else
			{
				desiredPeakLines.push_back(peakLines[iDVMax]);
				peakLinesSort.push_back(peakLines[iDVMax]);
			}
			if (minDV == 0)
			{
				desiredPeakLines.push_back(ReseveLine[3]);
				peakLinesSort.push_back(ReseveLine[3]);
			}
			else
			{
				desiredPeakLines.push_back(peakLines[iDVMin]);
				peakLinesSort.push_back(peakLines[iDVMin]);
			}
		}

		// Sorting peaklines
		sort(peakLinesSort.begin(), peakLinesSort.end(), peaklineSortComp);

		// Coefiecnt of lines
		std::vector <std::vector<float> > coeffLines;

		for (size_t i = 0; i < 4; i++)
		{
			std::vector<float> tempCoeffLines;
			tempCoeffLines.push_back(cos(peakLinesSort[i][1]));
			tempCoeffLines.push_back(sin(peakLinesSort[i][1]));
			tempCoeffLines.push_back(-1 * peakLinesSort[i][0]);

			coeffLines.push_back(tempCoeffLines);
		}

		// Declaration
		std::vector<std::vector<float> > resultPoint;
		int toloranceSizeImg;

		if (resizedImg.size[0] > resizedImg.size[1])
			toloranceSizeImg = resizedImg.size[0] + (resizedImg.size[0]) - resizedImg.size[1];
		else
			toloranceSizeImg = resizedImg.size[1] + (resizedImg.size[1]) - resizedImg.size[0];

		resultPoint.push_back(getCross(coeffLines[1], coeffLines[3]));
		resultPoint.push_back(getCross(coeffLines[3], coeffLines[2]));
		resultPoint.push_back(getCross(coeffLines[2], coeffLines[0]));
		resultPoint.push_back(getCross(coeffLines[0], coeffLines[1]));

		for (size_t i = 0; i < resultPoint.size(); i++)
		{
			if (abs(resultPoint[i][0]) > (toloranceSizeImg) || abs(resultPoint[i][1]) > (toloranceSizeImg))
			{
				resultPoint[0] = (getCross(coeffLines[0], coeffLines[2]));
				resultPoint[1] = (getCross(coeffLines[2], coeffLines[1]));
				resultPoint[2] = (getCross(coeffLines[1], coeffLines[3]));
				resultPoint[3] = (getCross(coeffLines[3], coeffLines[0]));
			}
		}

		// Finding the area to find out sorting points
		float Zarea[4];

		Zarea[0] = (resultPoint[1][0] - resultPoint[0][0]);
		Zarea[1] = (resultPoint[1][1] - resultPoint[0][1]);

		Zarea[2] = (resultPoint[2][0] - resultPoint[0][0]);
		Zarea[3] = (resultPoint[2][1] - resultPoint[0][1]);

		float area = Zarea[0] * Zarea[3] - Zarea[1] * Zarea[2];
		if (area < 0)
		{
			reverse(resultPoint.begin(), resultPoint.end());
		}
		// Finding distance to find out long & short edges of object
		std::vector<std::vector<float> > lengthLine;

		for (size_t i = 0; i < 4; i++)
		{
			std::vector <float> lengthLineTemp;
			if (i == 3)
			{
				lengthLineTemp.push_back(vectorDistance(resultPoint[i], resultPoint[0]));
				lengthLineTemp.push_back(i + 1);
				lengthLine.push_back(lengthLineTemp);
			}
			else
			{
				lengthLineTemp.push_back(vectorDistance(resultPoint[i], resultPoint[i + 1]));
				lengthLineTemp.push_back(i + 1);
				lengthLine.push_back(lengthLineTemp);
			}
		}

		// Arrange Points based on the distance of edges of the object
		std::vector<std::vector<float> > mergedPoints;
		for (size_t i = 0; i < 4; i++)
		{
			lengthLine[i].push_back(resultPoint[i][0]);
			lengthLine[i].push_back(resultPoint[i][1]);
			mergedPoints.push_back(lengthLine[i]);
		}

		// Sorting the points (x,y)
		sort(mergedPoints.begin(), mergedPoints.end());
		std::vector<std::vector<float> > sortMergedPoints;
		int lowest = mergedPoints[0][1];
		for (int i = 0; i < 4; i++)
		{
			int a = (lowest - 1 + i);
			a = ((a % 4) + 1);

			for (int j = 0; j < 4; j++)
			{
				if (mergedPoints[j][1] == a)
				{
					sortMergedPoints.push_back(mergedPoints[j]);
					break;
				}
			}
		}

		// Return to the orginal dimention orginal image
		for (size_t i = 0; i < 4; i++)
		{
			sortMergedPoints[i][2] = sortMergedPoints[i][2] / imgScalingRatio;
			sortMergedPoints[i][3] = sortMergedPoints[i][3] / imgScalingRatio;
		}

		// Draw the peaklines
		cv::Mat discoveredLines = resizedImg.clone();

		for (size_t i = 0; i < peakLines.size(); i++)
		{
			float rho = peakLines[i][0], theta = peakLines[i][1];
			cv::Point pt1, pt2;
			double a = cos(theta), b = sin(theta);
			double x0 = a * rho, y0 = b * rho;
			pt1.x = cvRound(x0 + 1200 * (-b));
			pt1.y = cvRound(y0 + 1200 * (a));
			pt2.x = cvRound(x0 - 1200 * (-b));
			pt2.y = cvRound(y0 - 1200 * (a));
			line(discoveredLines, pt1, pt2, cv::Scalar(255, 0, 255), 1, CV_AA);
		}

		// Draw the desired lines
		cv::Mat desiredLines = resizedImg.clone();

		for (size_t i = 0; i < desiredPeakLines.size(); i++)
		{
			float rho = desiredPeakLines[i][0], theta = desiredPeakLines[i][1];

			cv::Point pt1, pt2;
			double a = cos(theta), b = sin(theta);
			double x0 = a * rho, y0 = b * rho;
			pt1.x = cvRound(x0 + 1200 * (-b));
			pt1.y = cvRound(y0 + 1200 * (a));
			pt2.x = cvRound(x0 - 1200 * (-b));
			pt2.y = cvRound(y0 - 1200 * (a));
			line(desiredLines, pt1, pt2, cv::Scalar(255, 0, 255), 5, CV_AA);
		}

		// Four corners of  in source image
		std::vector<cv::Point2i> pointSrc;
		pointSrc.push_back(cv::Point2i(sortMergedPoints[0][2], sortMergedPoints[0][3]));
		pointSrc.push_back(cv::Point2i(sortMergedPoints[1][2], sortMergedPoints[1][3]));
		pointSrc.push_back(cv::Point2i(sortMergedPoints[2][2], sortMergedPoints[2][3]));
		pointSrc.push_back(cv::Point2i(sortMergedPoints[3][2], sortMergedPoints[3][3]));

		// Put discovered points of detected edge to an array
		int counterTemp = 0; // I think bug to read memory ( trouble to readmemory in C#) jump from 0 & 1???!!!
		for (size_t i = 0; i < 4; i++)
		{
			for (size_t j = 0; j < 2; j++)
			{
				//cMobileDocScanning::iEdgePointsArray[counterTemp] = sortMergedPoints[i][j + 2];
				edge[counterTemp] = sortMergedPoints[i][j + 2];
				if (edge[counterTemp] < 0)
					edge[counterTemp] = 0;
				if (j == 0 && edge[counterTemp] > orgImg.cols)
					edge[counterTemp] = orgImg.cols;
				if (j == 1 && edge[counterTemp] > orgImg.rows)
					edge[counterTemp] = orgImg.rows;
				counterTemp++;
			}
		}

		//UIImage *imageOut = MatToUIImage(cannyImg);
		/*
		 UIAlertAction* defaultAction = [UIAlertAction actionWithTitle:@"OK" style:UIAlertActionStyleDefault handler:^(UIAlertAction * _Nonnull action) {}];
		 NSString* message = [NSString stringWithFormat:@"%d,%d %d,%d %d,%d %d,%d", edge[0],edge[1],edge[2],edge[3],edge[4],edge[5],edge[6],edge[7]];
		 UIAlertController* alert = [UIAlertController alertControllerWithTitle:@"Edge Detection" message:message preferredStyle:UIAlertControllerStyleAlert];
		 [alert addAction:defaultAction];
		 [self presentViewController:alert animated:YES completion:nil];
		 */
		pt1->X = edge[0];
		pt1->Y = edge[1];
		pt2->X = edge[2];
		pt2->Y = edge[3];
		pt3->X = edge[4];
		pt3->Y = edge[5];
		pt4->X = edge[6];
		pt4->Y = edge[7];
		/*
		CGSize orgImgSize = {(CGFloat)orgImg.cols, (CGFloat)orgImg.rows};
		UIImage *imageTemp = [self imageDrawingQuadrilateralOnClearBGBySize:orgImgSize Points:points MarkColor:[UIColor greenColor] MarkPointIndex:-1];
		_imageViewOverlay1.image = imageTemp;
		 */
	}
#else
#ifdef USE_SYSTEM_DRAWING
void detectEdge(cv::Mat orgImg, System::Drawing::Point^% pt1, System::Drawing::Point^% pt2, System::Drawing::Point^% pt3, System::Drawing::Point^% pt4) {
	cv::Mat resizedImg; // = imageCV.clone();
	//--------------------
	//1 Resizing image
	//--------------------
	cv::Size s;;
	double imgHeight = orgImg.rows;
	double imgWidth = orgImg.cols;
	double imgHVRatio = imgHeight / imgWidth;
	if (imgHVRatio > 1) {
		imgHeight = 335 * 4;
		imgWidth = imgHeight / imgHVRatio;
		s = cv::Size(imgWidth, imgHeight);
	}
	else {
		imgWidth = 335 * 4;
		imgHeight = imgWidth * imgHVRatio;
		s = cv::Size(imgWidth, imgHeight);
	}
	double imgScalingRatio = imgHeight / orgImg.rows;
	resize(orgImg, resizedImg, s, CV_INTER_CUBIC); //CV_INTER_CUBIC |  CV_INTER_LINEAR

	//UIImage *imageResized = MatToUIImage(resizedImg);
	//_imageView1.image = imageResized;


	//--------------------
	//2 Convert image to Grayscale
	//--------------------
	//cv::Mat grayImg;
	//cvtColor(resizedImg, grayImg, CV_RGBA2GRAY);


	//--------------------
	//2 Apply edge detection kernel 
	//--------------------		
	cv::Point anchor = cv::Point(-1, -1);
	double delta = 0;
	int ddepth = -1;
	cv::Mat kernel = (cv::Mat_<double>(3, 3) << -1, -1, -1, -1, 9, -1, -1, -1, -1);
	/// Apply filter
	cv::Mat filteredImg;
	filter2D(resizedImg, filteredImg, ddepth, kernel, anchor, delta, cv::BORDER_DEFAULT);

	//----------------------------------------
	//3 Using opening & closing Morphologic & Guasian filter ( Prepaer Image for edge detection)
	//----------------------------------------
	int morph_size_open = 1; // Consider size of element size base on size of image
	int morph_size_close = 19; // Consider size of element size base on size of image

	cv::Mat element_open = getStructuringElement(cv::MORPH_RECT, cv::Size(2 * morph_size_open + 1, 2 * morph_size_open + 1), cv::Point(morph_size_open, morph_size_open));
	cv::Mat element_close = getStructuringElement(cv::MORPH_RECT, cv::Size(2 * morph_size_close + 1, 2 * morph_size_close + 1), cv::Point(morph_size_close, morph_size_close));

	cv::Mat gaussianImg;
	//GaussianBlur(grayImg, grayImg3, cv::Size(15, 15), sqrt(2));
	//morphologyEx(grayImg3, _morphImg, cv::MORPH_OPEN, element_open, cv::Point(-1, -1));
	GaussianBlur(filteredImg, gaussianImg, cv::Size(15, 15), sqrt(2));
	cv::Mat morphImg;
	morphologyEx(gaussianImg, morphImg, cv::MORPH_OPEN, element_open, cv::Point(-1, -1));
	morphologyEx(morphImg, morphImg, cv::MORPH_CLOSE, element_close, cv::Point(-1, -1));

	GaussianBlur(morphImg, morphImg, cv::Size(15, 15), sqrt(2));
	//cv::Mat edgePreparedImg = morphImg - meanAdaptiveThresholding;
	//UIImage *imageOut = MatToUIImage(edgePreparedImg);

	//----------------------------------------
	//4 Finding the lines by canny Filtering
	//----------------------------------------
	cv::Mat cannyImg;
	int lowcanny = 10;
	int highcanny = lowcanny * 3;
	Canny(morphImg, cannyImg, lowcanny, highcanny);

	int thresholdHoughLines = 50;

	//----------------------------------------
	//5 Extract points of Images
	//----------------------------------------
	int edge[8];
	std::vector<cv::Vec3f> linesOut;
	HoughLines(cannyImg, linesOut, 1, CV_PI / 180, thresholdHoughLines, 0, 0);

	/////// ***** peaklines
	std::vector <std::vector<float> > peakLines;
	std::vector <std::vector<float> > peakLinesSort;

	// vector<vector<float>> peaklines = Peaklines.filterLines(vector<vec3f> lines, int lineRange)
	const int lines_toloranc = 64;
	for (size_t i = 0; i < linesOut.size(); i++)
	{
		std::vector <float> temLinesVector;
		if (i <= lines_toloranc)
		{ // TODONE : find inteligens to numbers of the lines
			temLinesVector.push_back((linesOut[i][0]));
			temLinesVector.push_back(linesOut[i][1]);
			peakLines.push_back(temLinesVector);
		}
	}
	//end

	int numLines = (int)peakLines.size();
	float theta;
	float rheo;
	std::vector<float> distanceDU;
	std::vector<float> distanceDV;

	// Initilization to NAN (not zaro) using for find the longest distnaces
	for (size_t i = 0; i < numLines; i++)
	{
		distanceDV.push_back(NAN);
		distanceDU.push_back(NAN);
	}
	// Check lines if vertical or horizantal are
	for (int i = 0; i < peakLines.size(); i++)
	{
		rheo = peakLines[i][0];
		theta = peakLines[i][1];

		if (rheo < 0)
		{
			theta = theta - PI;
			rheo = abs(rheo);
		}

		if (theta < PI / 2 - 0.5 && theta > PI / 2 - 0.5)
		{
			rheo = rheo * -1;
			theta = theta - PI / 2;
		}

		std::vector<float> X;
		std::vector<float> Y;

		int fx = ((abs(theta) > (PI / 4)) && (abs(theta) < (3 * PI / 4))) ? resizedImg.size[0] : resizedImg.size[1];;
		for (size_t i = 0; i <= fx; i++)
		{
			X.push_back(i);
		}

		for (size_t i = 0; i <= fx; i++)
		{
			Y.push_back(i);
		}
		if ((abs(theta) > (PI / 4)) && (abs(theta) < (3 * PI / 4)))
		{
			for (size_t i = 0; i < X.size(); i++)
			{ // another IDE be j < X
				Y[i] = 1 + ((rheo - X[i] * cos(theta)) / sin(theta));
			}
			// Call the distance function to find the deistance
			std::vector<int> vM(2);
			vM[0] = resizedImg.size[0];
			vM[1] = resizedImg.size[1];
			distanceDV[i] = (calcuteDistance(vM, X, Y));
		}
		else
		{
			for (size_t j = 0; j < Y.size(); j++)
			{ // another IDE be j < Y
				X[j] = 1 + ((rheo - Y[j] * sin(theta)) / cos(theta));
			}
			// Call the distance function to find the deistance
			std::vector<int> vM(2);
			vM[0] = resizedImg.size[0];
			vM[1] = resizedImg.size[1];
			distanceDU[i] = (calcuteDistance(vM, X, Y));
		}
	}

	if (numLines <= 0)
		return;

	// Finding the longest distance from center consider the desired line
	if (isnan(distanceDV[0]))
		distanceDV[0] = 0;
	if (isnan(distanceDU[0]))
		distanceDU[0] = 0;

	int iDUMax = 0;
	int iDUMin = 0;
	int iDVMax = 0;
	int iDVMin = 0;

	float maxDU = 0;
	float minDU = 0;
	float maxDV = 0;
	float minDV = 0;

	maxDU = getMaxInt(distanceDU);
	minDU = getMinInt(distanceDU);
	maxDV = getMaxInt(distanceDV);
	minDV = getMinInt(distanceDV);

	for (int i = 0; i < numLines; ++i) {
		if (maxDU == distanceDU[i]) {
			iDUMax = i;
		}
		if (minDU == distanceDU[i]) {
			iDUMin = i;
		}
		if (maxDV == distanceDV[i]) {
			iDVMax = i;
		}
		if (minDV == distanceDV[i]) {
			iDVMin = i;
		}
	}
	// Determine the desired Peaklines

	int assumptionLine = 1;
	std::vector<std::vector<float> > ReseveLine;
	// If any reason couldn't find total 4 lines, it asumpt the following asumption lines
	if (maxDU == 0 || minDU == 0 || maxDV == 0 || minDV == 0)
	{
		assumptionLine = 0;

		float temResRho;
		int temSize;
		float temResTheta;

		for (size_t i = 0; i < 4; i++)
		{
			std::vector<float> tempResLine;

			if ((i == 0) || (i == 1))
			{
				temResTheta = 0;
				temSize = resizedImg.size[1];
			}
			else
			{
				temResTheta = PI / 2;
				temSize = resizedImg.size[0];
			}

			if ((i == 0) || (i == 2))
			{
				temResRho = 0.98 * temSize;
			}
			else
			{
				temResRho = 0.02 * temSize;
			}
			tempResLine.push_back(temResRho);
			tempResLine.push_back(temResTheta);
			ReseveLine.push_back(tempResLine);
		}
	}

	std::vector <std::vector<float> > desiredPeakLines;

	if (assumptionLine == 1)
	{
		// Determine the desired Peaklines
		desiredPeakLines.push_back(peakLines[iDUMax]);
		desiredPeakLines.push_back(peakLines[iDUMin]);
		desiredPeakLines.push_back(peakLines[iDVMax]);
		desiredPeakLines.push_back(peakLines[iDVMin]);

		// Using peaklines for sorting
		peakLinesSort.push_back(peakLines[iDUMax]);
		peakLinesSort.push_back(peakLines[iDUMin]);
		peakLinesSort.push_back(peakLines[iDVMax]);
		peakLinesSort.push_back(peakLines[iDVMin]);
	}
	else
	{
		if (maxDU == 0)
		{
			desiredPeakLines.push_back(ReseveLine[0]);
			peakLinesSort.push_back(ReseveLine[0]);
		}
		else
		{
			desiredPeakLines.push_back(peakLines[iDUMax]);
			peakLinesSort.push_back(peakLines[iDUMax]);
		}
		if (minDU == 0)
		{
			desiredPeakLines.push_back(ReseveLine[1]);
			peakLinesSort.push_back(ReseveLine[1]);
		}
		else
		{
			desiredPeakLines.push_back(peakLines[iDUMin]);
			peakLinesSort.push_back(peakLines[iDUMin]);
		}
		if (maxDV == 0)
		{
			desiredPeakLines.push_back(ReseveLine[2]);
			peakLinesSort.push_back(ReseveLine[2]);
		}
		else
		{
			desiredPeakLines.push_back(peakLines[iDVMax]);
			peakLinesSort.push_back(peakLines[iDVMax]);
		}
		if (minDV == 0)
		{
			desiredPeakLines.push_back(ReseveLine[3]);
			peakLinesSort.push_back(ReseveLine[3]);
		}
		else
		{
			desiredPeakLines.push_back(peakLines[iDVMin]);
			peakLinesSort.push_back(peakLines[iDVMin]);
		}
	}

	// Sorting peaklines
	sort(peakLinesSort.begin(), peakLinesSort.end(), peaklineSortComp);

	// Coefiecnt of lines
	std::vector <std::vector<float> > coeffLines;

	for (size_t i = 0; i < 4; i++)
	{
		std::vector<float> tempCoeffLines;
		tempCoeffLines.push_back(cos(peakLinesSort[i][1]));
		tempCoeffLines.push_back(sin(peakLinesSort[i][1]));
		tempCoeffLines.push_back(-1 * peakLinesSort[i][0]);

		coeffLines.push_back(tempCoeffLines);
	}

	// Declaration
	std::vector<std::vector<float> > resultPoint;
	int toloranceSizeImg;

	if (resizedImg.size[0] > resizedImg.size[1])
		toloranceSizeImg = resizedImg.size[0] + (resizedImg.size[0]) - resizedImg.size[1];
	else
		toloranceSizeImg = resizedImg.size[1] + (resizedImg.size[1]) - resizedImg.size[0];

	resultPoint.push_back(getCross(coeffLines[1], coeffLines[3]));
	resultPoint.push_back(getCross(coeffLines[3], coeffLines[2]));
	resultPoint.push_back(getCross(coeffLines[2], coeffLines[0]));
	resultPoint.push_back(getCross(coeffLines[0], coeffLines[1]));

	for (size_t i = 0; i < resultPoint.size(); i++)
	{
		if (abs(resultPoint[i][0]) > (toloranceSizeImg) || abs(resultPoint[i][1]) > (toloranceSizeImg))
		{
			resultPoint[0] = (getCross(coeffLines[0], coeffLines[2]));
			resultPoint[1] = (getCross(coeffLines[2], coeffLines[1]));
			resultPoint[2] = (getCross(coeffLines[1], coeffLines[3]));
			resultPoint[3] = (getCross(coeffLines[3], coeffLines[0]));
		}
	}

	// Finding the area to find out sorting points
	float Zarea[4];

	Zarea[0] = (resultPoint[1][0] - resultPoint[0][0]);
	Zarea[1] = (resultPoint[1][1] - resultPoint[0][1]);

	Zarea[2] = (resultPoint[2][0] - resultPoint[0][0]);
	Zarea[3] = (resultPoint[2][1] - resultPoint[0][1]);

	float area = Zarea[0] * Zarea[3] - Zarea[1] * Zarea[2];
	if (area < 0)
	{
		reverse(resultPoint.begin(), resultPoint.end());
	}
	// Finding distance to find out long & short edges of object
	std::vector<std::vector<float> > lengthLine;

	for (size_t i = 0; i < 4; i++)
	{
		std::vector <float> lengthLineTemp;
		if (i == 3)
		{
			lengthLineTemp.push_back(vectorDistance(resultPoint[i], resultPoint[0]));
			lengthLineTemp.push_back(i + 1);
			lengthLine.push_back(lengthLineTemp);
		}
		else
		{
			lengthLineTemp.push_back(vectorDistance(resultPoint[i], resultPoint[i + 1]));
			lengthLineTemp.push_back(i + 1);
			lengthLine.push_back(lengthLineTemp);
		}
	}

	// Arrange Points based on the distance of edges of the object
	std::vector<std::vector<float> > mergedPoints;
	for (size_t i = 0; i < 4; i++)
	{
		lengthLine[i].push_back(resultPoint[i][0]);
		lengthLine[i].push_back(resultPoint[i][1]);
		mergedPoints.push_back(lengthLine[i]);
	}

	// Sorting the points (x,y)
	sort(mergedPoints.begin(), mergedPoints.end());
	std::vector<std::vector<float> > sortMergedPoints;
	int lowest = mergedPoints[0][1];
	for (int i = 0; i < 4; i++)
	{
		int a = (lowest - 1 + i);
		a = ((a % 4) + 1);

		for (int j = 0; j < 4; j++)
		{
			if (mergedPoints[j][1] == a)
			{
				sortMergedPoints.push_back(mergedPoints[j]);
				break;
			}
		}
	}

	// Return to the orginal dimention orginal image
	for (size_t i = 0; i < 4; i++)
	{
		sortMergedPoints[i][2] = sortMergedPoints[i][2] / imgScalingRatio;
		sortMergedPoints[i][3] = sortMergedPoints[i][3] / imgScalingRatio;
	}

	// Draw the peaklines
	cv::Mat discoveredLines = resizedImg.clone();

	for (size_t i = 0; i < peakLines.size(); i++)
	{
		float rho = peakLines[i][0], theta = peakLines[i][1];
		cv::Point pt1, pt2;
		double a = cos(theta), b = sin(theta);
		double x0 = a * rho, y0 = b * rho;
		pt1.x = cvRound(x0 + 1200 * (-b));
		pt1.y = cvRound(y0 + 1200 * (a));
		pt2.x = cvRound(x0 - 1200 * (-b));
		pt2.y = cvRound(y0 - 1200 * (a));
		line(discoveredLines, pt1, pt2, cv::Scalar(255, 0, 255), 1, CV_AA);
	}

	// Draw the desired lines
	cv::Mat desiredLines = resizedImg.clone();

	for (size_t i = 0; i < desiredPeakLines.size(); i++)
	{
		float rho = desiredPeakLines[i][0], theta = desiredPeakLines[i][1];

		cv::Point pt1, pt2;
		double a = cos(theta), b = sin(theta);
		double x0 = a * rho, y0 = b * rho;
		pt1.x = cvRound(x0 + 1200 * (-b));
		pt1.y = cvRound(y0 + 1200 * (a));
		pt2.x = cvRound(x0 - 1200 * (-b));
		pt2.y = cvRound(y0 - 1200 * (a));
		line(desiredLines, pt1, pt2, cv::Scalar(255, 0, 255), 5, CV_AA);
	}

	// Four corners of  in source image
	std::vector<cv::Point2i> pointSrc;
	pointSrc.push_back(cv::Point2i(sortMergedPoints[0][2], sortMergedPoints[0][3]));
	pointSrc.push_back(cv::Point2i(sortMergedPoints[1][2], sortMergedPoints[1][3]));
	pointSrc.push_back(cv::Point2i(sortMergedPoints[2][2], sortMergedPoints[2][3]));
	pointSrc.push_back(cv::Point2i(sortMergedPoints[3][2], sortMergedPoints[3][3]));

	// Put discovered points of detected edge to an array
	int counterTemp = 0; // I think bug to read memory ( trouble to readmemory in C#) jump from 0 & 1???!!!
	for (size_t i = 0; i < 4; i++)
	{
		for (size_t j = 0; j < 2; j++)
		{
			//cMobileDocScanning::iEdgePointsArray[counterTemp] = sortMergedPoints[i][j + 2];
			edge[counterTemp] = sortMergedPoints[i][j + 2];
			if (edge[counterTemp] < 0)
				edge[counterTemp] = 0;
			if (j == 0 && edge[counterTemp] > orgImg.cols)
				edge[counterTemp] = orgImg.cols;
			if (j == 1 && edge[counterTemp] > orgImg.rows)
				edge[counterTemp] = orgImg.rows;
			counterTemp++;
		}
	}

	//UIImage *imageOut = MatToUIImage(cannyImg);
	/*
	 UIAlertAction* defaultAction = [UIAlertAction actionWithTitle:@"OK" style:UIAlertActionStyleDefault handler:^(UIAlertAction * _Nonnull action) {}];
	 NSString* message = [NSString stringWithFormat:@"%d,%d %d,%d %d,%d %d,%d", edge[0],edge[1],edge[2],edge[3],edge[4],edge[5],edge[6],edge[7]];
	 UIAlertController* alert = [UIAlertController alertControllerWithTitle:@"Edge Detection" message:message preferredStyle:UIAlertControllerStyleAlert];
	 [alert addAction:defaultAction];
	 [self presentViewController:alert animated:YES completion:nil];
	 */
	pt1->X = edge[0];
	pt1->Y = edge[1];
	pt2->X = edge[2];
	pt2->Y = edge[3];
	pt3->X = edge[4];
	pt3->Y = edge[5];
	pt4->X = edge[6];
	pt4->Y = edge[7];
	/*
	CGSize orgImgSize = {(CGFloat)orgImg.cols, (CGFloat)orgImg.rows};
	UIImage *imageTemp = [self imageDrawingQuadrilateralOnClearBGBySize:orgImgSize Points:points MarkColor:[UIColor greenColor] MarkPointIndex:-1];
	_imageViewOverlay1.image = imageTemp;
	 */
}
#endif
#endif
#ifdef USE_SYSTEM_DRAWING
bool ImgProcUtil::DetectEdge(array<System::Byte>^ imageSrc, System::Collections::Generic::List<System::Drawing::Point>^% points)
	{
		int outWidth;
		int outHeight;

		cv::Mat srcImg;
		pin_ptr<Byte> p = &imageSrc[0];   // entire array is now pinned
		unsigned char * cp = p;
		int docImageLen = imageSrc->Length;
		srcImg = loadImageFromByteArray(cp, docImageLen);
		System::Drawing::Point^ pt1 = gcnew System::Drawing::Point();
		System::Drawing::Point^ pt2 = gcnew System::Drawing::Point();
		System::Drawing::Point^ pt3 = gcnew System::Drawing::Point();
		System::Drawing::Point^ pt4 = gcnew System::Drawing::Point();
		detectEdge(srcImg, pt1, pt2, pt3, pt4);
		points->Clear();
		points->Add(*pt1);
		points->Add(*pt2);
		points->Add(*pt3);
		points->Add(*pt4);

		return true;
	}

	bool ImgProcUtil::DetectEdgeFromBitmap(System::Drawing::Bitmap^ srcBmp, System::Collections::Generic::List<System::Drawing::Point>^% points)
	{
		System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream();
		srcBmp->Save(ms, System::Drawing::Imaging::ImageFormat::Png);
		array<System::Byte>^ imageSrc = ms->GetBuffer();
		return DetectEdge(imageSrc, points);
	}
#endif
	// **** Get min & max
	template <class numT>
	numT getMax(std::vector<numT>& v)
	{
		if (v.empty())
			return NAN;
		return *max_element(v.begin(), v.end());
	};

	template <class numT>
	numT getMin(std::vector<numT>& v)
	{
		if (v.empty())
			return NAN;
		return *min_element(v.begin(), v.end());
	};

	// **** Finding Cross
	cv::Point getCrossPoint(cv::Point p11, cv::Point p12, cv::Point p21, cv::Point p22) {
		double a1 = NAN;
		double a2 = NAN;
		double b1 = NAN;
		double b2 = NAN;

		if (p12.x != p11.x) {
			//line1     y1 = a1 * x1 + b1
			a1 = (double)(p12.y - p11.y) / (double)(p12.x - p11.x);
			b1 = (double)p11.y - a1 * (double)p11.x;
		}
		else {
			b1 = p11.y;
		}

		if (p22.x != p21.x) {
			//line2     y2 = a2 * x2 + b2
			a2 = (double)(p22.y - p21.y) / (double)(p22.x - p21.x);
			b2 = (double)p21.y - a2 * (double)p21.x;
		}
		else {
			b2 = p21.y;
		}

		//cross point
		//when y1 = y2 = y, and x1 = x2 = x
		//  a1 * x + b1 = a2 * x + b2
		if (a1 != a2) {
			if (isnan(a1)) {
				return cv::Point(p11.x, ((a2 * p11.x) + b2));
			}
			else if (isnan(a2)) {
				return cv::Point(p21.x, ((a1 * p21.x) + b1));
			}
			float x = (b2 - b1) / (a1 - a2);
			float y = (a1 * x) + b1;
			return cv::Point(x, y);
		}

		//no cross point
		return cv::Point();
	}

	struct LineAttr {
		cv::Point p1;
		cv::Point p2;
		double r;
		double t;
		double rh;
		bool merged;
		bool connected;
		int idx;
	};
	bool compareLineAttrByLengthDesc(LineAttr l1, LineAttr l2)
	{
		return (l2.r < l1.r);
	}
	bool compareLineAttrByMinXDesc(LineAttr* l1, LineAttr* l2)
	{
		int l1_minx = cv::min(l1->p1.x, l1->p2.x);
		int l2_minx = cv::min(l2->p1.x, l2->p2.x);
		return (l2_minx < l1_minx);
	}
	bool compareLineAttrByMinYDesc(LineAttr* l1, LineAttr* l2)
	{
		int l1_miny = cv::min(l1->p1.y, l1->p2.y);
		int l2_miny = cv::min(l2->p1.y, l2->p2.y);
		return (l2_miny < l1_miny);
	}

	int GetDistanceFromPointToLine(cv::Point pt, const LineAttr& line) {
		//int x1 = line[0];
		//int y1 = line[1];
		//int x2 = line[2];
		//int y2 = line[3];
		cv::Point p1 = line.p1;
		cv::Point p2 = line.p2;
		if (p1.x == p2.x) {
			//line is vertical
			return abs(pt.x - p1.x);
		}
		else if (p1.y == p2.y) {
			//line is horizontal
			return abs(pt.y - p1.y);
		}
		else {
			double a = (p1.y - p2.y) / (p1.x - p2.x);
			if (isinf(a)) {
				//line is vertical
				return abs(pt.x - p1.x);
			}
			double b = p1.y - a * p1.x;//y - ax
			double c = (p1.x - p2.x) / (p1.y - p2.y);
			double d = p1.x - c * p1.y;//x - cy
			//double r = cv::norm(p1 - p2);
			double r = line.r;
			//double s = (p1.y - p2.y) / r;
			//if (s < -1) s = -1;
			//if (s > 1) s = 1;
			//double t = asin(s);
			double t = line.t;
			//double vert_s = (x1 - x2) / r;
			//if (vert_s < -1) vert_s = -1;
			//if (vert_s > 1) vert_s = 1;
			//double vert_t = asin(vert_s);
			if (t > PI / 4 || t < -(PI / 4)) {
				double vert_a = -1 / a;
				if (isinf(vert_a)) {
					//line is horizontal
					return abs(pt.y - p1.y);
				}
				double vert_b = pt.y - vert_a * pt.x;

				double cx = (vert_b - b) / (a - vert_a);
				double cy = (a * cx) + b;
				cv::Point pc = cv::Point(cx, cy);
				return cv::norm(pc - pt);
			}
			else {
				double vert_c = -1 / c;
				//double c = (x1 - x2) / (y1 - y2);
				//double d = x1 - c * y1;//x - cy
				if (isinf(vert_c)) {
					//line is vertical
					return abs(pt.x - p1.x);
				}
				double vert_d = pt.x - vert_c * pt.y;
				//x = cy + d
				//x/c = y + d/c
				double cy = (d - vert_d) / (vert_c - c);
				double cx = (c * cy) + d;
				cv::Point pc = cv::Point(cx, cy);
				return cv::norm(pc - pt);
			}


		}

		return -1;
	}
#if 0
	void detectEdge2(cv::Mat orgImg, System::Drawing::Point^% pt1, System::Drawing::Point^% pt2, System::Drawing::Point^% pt3, System::Drawing::Point^% pt4) {
		cv::Mat resizedImg; // = imageCV.clone();
		//--------------------
		//1 Resizing image
		//--------------------
		cv::Size s;;
		//double imgHeight = pickedImage.size.height;
		//double imgWidth = pickedImage.size.width;
		double imgHeight = orgImg.rows;
		double imgWidth = orgImg.cols;
		double imgHVRatio = imgHeight / imgWidth;
		if (imgHVRatio > 1) {
			imgHeight = 800;
			imgWidth = imgHeight / imgHVRatio;
			s = cv::Size(imgWidth, imgHeight);
		}
		else {
			imgWidth = 800;
			imgHeight = imgWidth * imgHVRatio;
			s = cv::Size(imgWidth, imgHeight);
		}
		double imgScalingRatio = imgHeight / orgImg.rows;
		resize(orgImg, resizedImg, s, CV_INTER_CUBIC); //CV_INTER_CUBIC |  CV_INTER_LINEAR

		//UIImage *imageResized = MatToUIImage(resizedImg);
		//_imageView1.image = imageResized;

		//--------------------
		//2 Convert image to Grayscale
		//--------------------
		cv::Mat grayImg;
		cvtColor(resizedImg, grayImg, CV_RGBA2GRAY);

		//----------------------------------------
		//3 Using opening & closing Morphologic & Guasian filter ( Prepaer Image for edge detection)
		//----------------------------------------
		cv::Mat meane, covs;
		cv::calcCovarMatrix(grayImg, covs, meane, CV_COVAR_NORMAL | CV_COVAR_ROWS | CV_COVAR_SCALE);

		meane = meane / grayImg.rows;

		//cv::Scalar meanf = mean(meane);
		cv::Scalar meanIma = mean(grayImg);
		cv::Scalar meanedge = mean(covs);

		//float defualtmeancov = (float)meanf[0];
		int meanImag = (int)meanIma[0];
		int meanedgeint2 = (int)meanedge[0];
		int smeanT = sqrt(meanedgeint2);
		cv::Mat adapImg;

		int morph_size_open = 1; // Consider size of element size base on size of image
		int morph_size_close = 21; // Consider size of element size base on size of image
		//int repeatFindLine = 0;


		int adapConstant = 5 * smeanT;
		if (smeanT < 20 && smeanT > 10)
		{
			if (meanImag < 200)
				morph_size_close = 19;
			else if (meanImag < 180)
				morph_size_close = 19;
			adapConstant = 5 * smeanT;
		}
		else if (smeanT < 10)
		{
			morph_size_close = 19;
			adapConstant = 5 * smeanT;
		}
		else if (smeanT >= 20 && smeanT < 30)
		{

			if (meanImag < 200)
				morph_size_close = 19;
			else if (meanImag > 180)
				morph_size_close = 1;
			morph_size_close = 19;
			adapConstant = smeanT * 2.5;

		}
		else if (smeanT > 30)
		{
			adapConstant = 1 * smeanT;
			morph_size_close = 12;
		}

		//adaptiveThreshold(grayImg3, _meanAdaptiveThresholding, smeanT, CV_ADAPTIVE_THRESH_MEAN_C, cv::THRESH_BINARY, 5, adapConstant);
		cv::Mat meanAdaptiveThresholding;
		adaptiveThreshold(grayImg, meanAdaptiveThresholding, smeanT, CV_ADAPTIVE_THRESH_MEAN_C, cv::THRESH_BINARY, 5, adapConstant);

		cv::Mat element_open = getStructuringElement(cv::MORPH_RECT, cv::Size(2 * morph_size_open + 1, 2 * morph_size_open + 1), cv::Point(morph_size_open, morph_size_open));
		cv::Mat element_close = getStructuringElement(cv::MORPH_RECT, cv::Size(2 * morph_size_close + 1, 2 * morph_size_close + 1), cv::Point(morph_size_close, morph_size_close));

		cv::Mat gaussianImg;
		//GaussianBlur(grayImg, grayImg3, cv::Size(15, 15), sqrt(2));
		//morphologyEx(grayImg3, _morphImg, cv::MORPH_OPEN, element_open, cv::Point(-1, -1));
		GaussianBlur(grayImg, gaussianImg, cv::Size(15, 15), sqrt(2));
		cv::Mat morphImg;
		morphologyEx(gaussianImg, morphImg, cv::MORPH_OPEN, element_open, cv::Point(-1, -1));
		morphologyEx(morphImg, morphImg, cv::MORPH_CLOSE, element_close, cv::Point(-1, -1));

		GaussianBlur(morphImg, morphImg, cv::Size(15, 15), sqrt(2));
		cv::Mat edgePreparedImg = morphImg - meanAdaptiveThresholding;
		//UIImage *imageOut = MatToUIImage(edgePreparedImg);

		//----------------------------------------
		//4 Finding the lines by canny Filtering
		//----------------------------------------
		cv::Mat cannyImg;
		int lowcanny = 10;
		int highcanny = lowcanny * 3;
		Canny(edgePreparedImg, cannyImg, lowcanny, highcanny, 3);
		//UIImage *imageOut = MatToUIImage(cannyImg);


		//----------------------------------------
		//5 Extract points of Images
		//----------------------------------------
		std::vector<cv::Vec3f> linesOut;
		std::vector<cv::Vec4i> linesOutP;
		std::vector<cv::Vec4i> linesMerged;
		std::vector<cv::Vec4i> linesNotFarAway;
		std::vector<cv::Vec4i> linesConnected;
		//HoughLines(cannyOut, linesOut, 1, CV_PI / 180, 100, 0, 0);
		int sizeMin = min(cannyImg.cols, cannyImg.rows);
		int threshold = sizeMin / 10;
		int deltaThreshold = sizeMin / 20;
		int connectionThreshold = sizeMin / 10;
		int lineThreshold = sizeMin / 10;
		int lineSegThreshold = sizeMin / 200;
		int lineIsolationThreshold = sizeMin / 5;
		//double lineAngleThreshold = 0.005;
		double lineThetaThreshold = 0.1;
		double mergedLineDensityThreshold = 0.20;
		//_RPT3(_CRT_WARN, "sizeMin:%d threshold:%d deltaThreshold:%d\n", sizeMin, threshold, deltaThreshold);
		//_RPT3(_CRT_WARN, "connectionThreshold:%d lineThreshold:%d lineSegThreshold:%d\n", connectionThreshold, lineThreshold, lineSegThreshold);
		//_RPT1(_CRT_WARN, "lineIsolationThreshold:%d\n", lineIsolationThreshold);
		//_RPT2(_CRT_WARN, "lineThetaThreshold:%f mergedLineDensityThreshold:%f\n", lineThetaThreshold, mergedLineDensityThreshold);

		// Copy edges to the images that will display the results in BGR
		cv::Mat cdstP;	//just for debug
		cvtColor(cannyImg, cdstP, CV_GRAY2BGR);
		cv::Mat cdstP2 = cdstP.clone();	//just for debug
		cv::Mat cdstP3 = cdstP.clone();	//just for debug
		cv::Mat cdstP4 = cdstP.clone();	//just for debug

		// Probabilistic Line Transform
		HoughLinesP(cannyImg, linesOutP, 1, CV_PI / 180, threshold, 0, 0);
		// Draw the lines
		int sizeLinesP = linesOutP.size();
		double* x1 = new double[sizeLinesP];
		double* y1 = new double[sizeLinesP];
		double* x2 = new double[sizeLinesP];
		double* y2 = new double[sizeLinesP];
		double* a = new double[sizeLinesP];	//y = ax + b
		double* b = new double[sizeLinesP];
		double* c = new double[sizeLinesP];	//x = cy + d
		double* d = new double[sizeLinesP];
		double* t = new double[sizeLinesP];	//angle by radians
		double* r = new double[sizeLinesP];	//line length
		double* rh = new double[sizeLinesP];	//distance from center
		bool* merged = new bool[sizeLinesP];
		memset(x1, 0, sizeof(double) * sizeLinesP);
		memset(y1, 0, sizeof(double) * sizeLinesP);
		memset(x2, 0, sizeof(double) * sizeLinesP);
		memset(y2, 0, sizeof(double) * sizeLinesP);
		memset(a, 0, sizeof(double) * sizeLinesP);
		memset(b, 0, sizeof(double) * sizeLinesP);
		memset(c, 0, sizeof(double) * sizeLinesP);
		memset(merged, 0, sizeof(bool) * sizeLinesP);

		for (size_t i = 0; i < linesOutP.size(); i++)
		{
			cv::Vec4i l = linesOutP[i];
			cv::Point p1 = cv::Point(l[0], l[1]);
			cv::Point p2 = cv::Point(l[2], l[3]);
			line(cdstP, p1, p2, cv::Scalar(0, 0, 255), 3, cv::LINE_AA);	//just for debug

			x1[i] = l[0];
			y1[i] = l[1];
			x2[i] = l[2];
			y2[i] = l[3];
			//y = ax + b;
			//when x = 0; y = b
			//x = cy + d;
			//when y = 0; x = d

			a[i] = (y1[i] - y2[i]) / (x1[i] - x2[i]);
			b[i] = y1[i] - a[i] * x1[i];//y - ax
			c[i] = (x1[i] - x2[i]) / (y1[i] - y2[i]);
			d[i] = x1[i] - c[i] * y1[i];//x - cy
			r[i] = cv::norm(p1 - p2);
			double s = (y1[i] - y2[i]) / r[i];
			if (s < -1) s = -1;
			if (s > 1) s = 1;
			t[i] = asin(s);
			rh[i] = GetDistanceFromPointToLine(cv::Point(0, 0), l);
		}


		for (size_t i = 0; i < sizeLinesP; i++)
		{
			//double lenLine1 = cv::norm(cv::Point(x1[i], y1[i]) - cv::Point(x2[i], y2[i]));
			double lenLine1 = r[i];
			//double len2 = sqrt(pow(x1[i] - x2[i], 2) + pow(y1[i] - y2[i], 2));
			if (!merged[i] && lenLine1 > lineSegThreshold) {

				double mergedA = a[i];
				double mergedB = b[i];
				double mergedC = c[i];
				double mergedD = d[i];
				double mergedT = t[i];
				double mergedX1 = 0;
				double mergedY1 = 0;
				double mergedX2 = 0;
				double mergedY2 = 0;
				double mergedLenTotal = lenLine1;
				double mergedRho = rh[i];

				if (-1 < mergedA && mergedA < 1) {
					if (x1[i] <= x2[i]) {
						mergedX1 = x1[i];
						mergedY1 = y1[i];
						mergedX2 = x2[i];
						mergedY2 = y2[i];
					}
					else {
						mergedX1 = x2[i];
						mergedY1 = y2[i];
						mergedX2 = x1[i];
						mergedY2 = y1[i];
					}
				}
				else {
					if (y1[i] <= y2[i]) {
						mergedY1 = y1[i];
						mergedX1 = x1[i];
						mergedY2 = y2[i];
						mergedX2 = x2[i];
					}
					else {
						mergedY1 = y2[i];
						mergedX1 = x2[i];
						mergedY2 = y1[i];
						mergedX2 = x1[i];
					}
				}
				//_RPT5(_CRT_WARN, "Finding lines to merge with %d mergedT:%f mergedB:%f mergedD:%f mergedRho:%f\n", i, mergedT, mergedB, mergedD, mergedRho);

				for (size_t j = i + 1; j < sizeLinesP; j++) {
					//double lenLine2 = cv::norm(cv::Point(x1[j], y1[j]) - cv::Point(x2[j], y2[j]));
					double lenLine2 = r[j];
					if (!merged[j] && lenLine2 > lineSegThreshold) {
						double dA = abs(mergedA - a[j]);
						double dC = abs(mergedC - c[j]);
						double dT = NAN;
						if ((mergedT <= 0 && t[j] <= 0) || (mergedT >= 0 && t[j] >= 0)) {
							dT = abs(mergedT - t[j]);
						}
						else {
							if (PI / 4 >= abs(mergedT) && PI / 4 >= abs(t[j])) {
								dT = abs(mergedT) + abs(t[j]);
							}
							else if (PI / 4 >= abs(mergedT) && PI / 4 <= abs(t[j])) {
								dT = abs(mergedT) + abs(t[j]);
							}
							else if (PI / 4 <= abs(mergedT) && PI / 4 <= abs(t[j])) {
								dT = (PI / 2 - abs(mergedT)) + (PI / 2 - abs(t[j]));
							}
							else if (PI / 4 <= abs(mergedT) && PI / 4 >= abs(t[j])) {
								//dT = (PI / 2 - abs(mergedT)) + (PI / 2 - abs(t[j]));
								dT = abs(mergedT) + abs(t[j]);
							}
							else {
								printf("!?");
							}
						}
						if (-1 < mergedA && mergedA < 1) {
							//|x| > |y|
							// check if a and b of lines are same or not, if same, these lines are on the same line
							//if (dA < lineAngleThreshold) {
							double dB = abs(mergedB - b[j]);
							double dRho = abs(mergedRho - rh[j]);
							//_RPT5(_CRT_WARN, "[%d] t:%f dT:%f dB:%f dRho:%f\n", j, t[j], dT, dB, dRho);
							if (dT < lineThetaThreshold) {
								if (dRho < deltaThreshold) {
									//these lines are on the same line
									//_RPT3(_CRT_WARN, "merged t:%f dB:%f dRho:%f\n", t[j], dB, dRho);
									merged[j] = true;
									mergedLenTotal += lenLine2;
									//find min and max of X
									if (x1[j] <= x2[j]) {
										//find min of X
										if (x1[j] < mergedX1) {
											mergedX1 = x1[j];
											mergedY1 = y1[j];
										}
										//find max of X
										if (x2[j] > mergedX2) {
											mergedX2 = x2[j];
											mergedY2 = y2[j];
										}
									}
									else {
										//find min of X
										if (x2[j] < mergedX1) {
											mergedX1 = x2[j];
											mergedY1 = y2[j];
										}
										//find max of X
										if (x1[j] > mergedX2) {
											mergedX2 = x1[j];
											mergedY2 = y1[j];
										}
									}
								}
							}
						}
						else {
							//|x| < |y|
							// check if a and b of lines are same or not, if same, these lines are on the same line
							//if (dC < lineAngleThreshold) {
							double dD = abs(mergedD - d[j]);
							double dRho = abs(mergedRho - rh[j]);
							//_RPT5(_CRT_WARN, "[%d] t:%f dT:%f dD:%f dRho:%f\n", j, t[j], dT, dD, dRho);
							if (dT < lineThetaThreshold) {
								if (dRho < deltaThreshold) {
									//these lines are on the same line
									//_RPT3(_CRT_WARN, "merged t:%f dD:%f dRho:%f\n", t[j], dD, dRho);
									merged[j] = true;
									mergedLenTotal += lenLine2;
									//find min and max of Y
									if (y1[j] <= y2[j]) {
										//find min of Y
										if (y1[j] < mergedY1) {
											mergedY1 = y1[j];
											mergedX1 = x1[j];
										}
										//find max of Y
										if (y2[j] > mergedY2) {
											mergedY2 = y2[j];
											mergedX2 = x2[j];
										}
									}
									else {
										//find min of Y
										if (y2[j] < mergedY1) {
											mergedY1 = y2[j];
											mergedX1 = x2[j];
										}
										//find max of Y
										if (y1[j] > mergedY2) {
											mergedY2 = y1[j];
											mergedX2 = x1[j];
										}
									}
								}
							}
						}
					}
				}//for loop j
				cv::Vec4i line;
				line[0] = mergedX1;
				line[1] = mergedY1;
				line[2] = mergedX2;
				line[3] = mergedY2;
				double lenMerged = cv::norm(cv::Point(line[0], line[1]) - cv::Point(line[2], line[3]));
				//_RPT2(_CRT_WARN, "### mergedLenTotal:%f lenMerged:%f\n", mergedLenTotal, lenMerged);
				if (mergedLenTotal / lenMerged > mergedLineDensityThreshold && lenMerged > lineThreshold) {
					//_RPT4(_CRT_WARN, "Added to linesMerged (%f,%f)-(%f,%f) ", mergedX1, mergedY1, mergedX2, mergedY2);
					//_RPT2(_CRT_WARN, "t:%f rh:%f\n", mergedT, mergedRho);
					linesMerged.push_back(line);
				}
			}
		}//for loop i

		delete[] x1;
		delete[] y1;
		delete[] x2;
		delete[] y2;
		delete[] a;
		delete[] b;
		delete[] c;
		delete[] d;
		delete[] t;
		delete[] r;
		delete[] merged;
		x1 = NULL;
		y1 = NULL;
		x2 = NULL;
		y2 = NULL;
		a = NULL;
		b = NULL;
		c = NULL;
		d = NULL;
		t = NULL;
		r = NULL;
		merged = NULL;

		//just for debug
		for (size_t i = 0; i < linesMerged.size(); i++)
		{
			cv::Vec4i l = linesMerged[i];
			line(cdstP2, cv::Point(l[0], l[1]), cv::Point(l[2], l[3]), cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
		}

		//remove lines far away
		for (size_t i = 0; i < linesMerged.size(); i++) {
			cv::Vec4i l = linesMerged[i];
			cv::Point p1(l[0], l[1]);
			cv::Point p2(l[2], l[3]);
			double r = cv::norm(p1 - p2);
			//_RPT5(_CRT_WARN, "[%d] (%d,%d)-(%d,%d) r:%f\n", i, p1.x, p1.y, p2.x, p2.y, r);
			bool isolated = true;
			for (size_t j = 0; j < linesMerged.size(); j++) {
				if (i == j)
					continue;

				cv::Vec4i ln = linesMerged[j];
				cv::Point pn1(ln[0], ln[1]);
				cv::Point pn2(ln[2], ln[3]);
				double rn = cv::norm(p1 - pn1);
				//_RPT5(_CRT_WARN, "--[%d] (%d,%d)-(%d,%d) rn:%f\n", j, p1.x, p1.y, pn1.x, pn1.y, rn);
				if (rn < lineIsolationThreshold) {
					//_RPT5(_CRT_WARN, "line %d is close to %d\n", i, j);
					isolated = false;
					break;
				}
				rn = cv::norm(p1 - pn2);
				//_RPT5(_CRT_WARN, "--[%d] (%d,%d)-(%d,%d) rn:%f\n", j, p1.x, p1.y, pn2.x, pn2.y, rn);
				if (rn < lineIsolationThreshold) {
					//_RPT5(_CRT_WARN, "line %d is close to %d\n", i, j);
					isolated = false;
					break;
				}
				rn = cv::norm(p2 - pn1);
				//_RPT5(_CRT_WARN, "--[%d] (%d,%d)-(%d,%d) rn:%f\n", j, p2.x, p2.y, pn1.x, pn1.y, rn);
				if (rn < lineIsolationThreshold) {
					//_RPT5(_CRT_WARN, "line %d is close to %d\n", i, j);
					isolated = false;
					break;
				}
				rn = cv::norm(p2 - pn2);
				//_RPT5(_CRT_WARN, "--[%d] (%d,%d)-(%d,%d) rn:%f\n", j, p2.x, p2.y, pn2.x, pn2.y, rn);
				if (rn < lineIsolationThreshold) {
					//_RPT5(_CRT_WARN, "line %d is close to %d\n", i, j);
					isolated = false;
					break;
				}
			}
			if (!isolated) {
				linesNotFarAway.push_back(l);
			}
		}

		//just for debug
		for (size_t i = 0; i < linesNotFarAway.size(); i++)
		{
			cv::Vec4i l = linesNotFarAway[i];
			line(cdstP3, cv::Point(l[0], l[1]), cv::Point(l[2], l[3]), cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
		}

		//find neighbours
		//check if end of line connect to end of other lines 
		bool* hasConnection = new bool[linesNotFarAway.size()];
		memset(hasConnection, 0, sizeof(bool) * linesNotFarAway.size());
		if (linesNotFarAway.size() <= 4) {
			//add all lines
			linesConnected = linesNotFarAway;
		}
		else {
			for (size_t i = 0; i < linesNotFarAway.size(); i++) {

				cv::Vec4i l = linesNotFarAway[i];
				cv::Point p1(l[0], l[1]);
				cv::Point p2(l[2], l[3]);
				double r = cv::norm(p1 - p2);
				double s = (p1.y - p2.y) / r;
				if (s < -1) s = -1;
				if (s > 1) s = 1;
				double t = asin(s);
				//_RPT4(_CRT_WARN, "testing connection of %d r:%f s:%f t:%f\n", i, r, s, t);

				bool connectedToP1 = false;
				bool connectedToP2 = false;
				for (size_t j = i + 1; j < linesNotFarAway.size(); j++) {

					cv::Vec4i ln = linesNotFarAway[j];
					cv::Point pn1(ln[0], ln[1]);
					cv::Point pn2(ln[2], ln[3]);

					//check the angle between lines
					//if angle is sharp, it's not a corner.
					double rn = cv::norm(pn1 - pn2);
					double sn = (pn1.y - pn2.y) / rn;
					if (sn < -1) sn = -1;
					if (sn > 1) sn = 1;
					double tn = asin(sn);
					double dT = NAN;
					if ((t <= 0 && tn <= 0) || (t >= 0 && tn >= 0)) {
						dT = abs(t - tn);
					}
					else {
						if (PI / 4 >= abs(t) && PI / 4 >= abs(tn)) {
							dT = abs(t) + abs(tn);
						}
						else if (PI / 4 >= abs(t) && PI / 4 <= abs(tn)) {
							dT = abs(t) + abs(tn);
						}
						else if (PI / 4 <= abs(t) && PI / 4 <= abs(tn)) {
							dT = (PI / 2 - abs(t)) + (PI / 2 - abs(tn));
						}
						else if (PI / 4 <= abs(t) && PI / 4 >= abs(tn)) {
							dT = abs(t) + abs(tn);
						}
						else {
							printf("!?");
						}
					}
					//_RPT4(_CRT_WARN, "rn:%f sn:%f tn:%f dT:%f\n", rn, sn, tn, dT);
					if (dT < PI / 4) {
						//_RPT2(_CRT_WARN, "%d and %d are NOT connected bcoz less angle\n", i, j);
						continue;
					}

					bool connected = false;
					double d = cv::norm(p1 - pn1);
					//_RPT5(_CRT_WARN, "(%d,%d)-(%d,%d):%f\n", p1.x, p1.y, pn1.x, pn1.y, d);
					if (d < connectionThreshold) {
						connected = true;
						connectedToP1 = true;
					}
					else {
						d = cv::norm(p1 - pn2);
						//_RPT5(_CRT_WARN, "(%d,%d)-(%d,%d):%f\n", p1.x, p1.y, pn2.x, pn2.y, d);
						if (d < connectionThreshold) {
							connected = true;
							connectedToP1 = true;
						}
						else {
							d = cv::norm(p2 - pn1);
							//_RPT5(_CRT_WARN, "(%d,%d)-(%d,%d):%f\n", p2.x, p2.y, pn1.x, pn1.y, d);
							if (d < connectionThreshold) {
								connected = true;
								connectedToP2 = true;
							}
							else {
								d = cv::norm(p2 - pn2);
								//_RPT5(_CRT_WARN, "(%d,%d)-(%d,%d):%f\n", p2.x, p2.y, pn2.x, pn2.y, d);
								if (d < connectionThreshold) {
									connected = true;
									connectedToP2 = true;
								}
							}
						}
					}
					if (connected) {
						//_RPT2(_CRT_WARN, "%d and %d are connected\n", i, j);
						hasConnection[i] = true;
						hasConnection[j] = true;
					}
					else {
						//_RPT1(_CRT_WARN, "%d not connected\n", j);
					}
					if (connectedToP1 && connectedToP2)
						break;
				}
			}
			//add only lines connected to another line
			for (size_t i = 0; i < linesNotFarAway.size(); i++) {
				//if the line connected to others, add to list
				cv::Vec4i l = linesNotFarAway[i];
				if (hasConnection[i])
					linesConnected.push_back(l);
			}
		}

		for (size_t i = 0; i < linesConnected.size(); i++)
		{
			cv::Vec4i l = linesConnected[i];
			line(cdstP4, cv::Point(l[0], l[1]), cv::Point(l[2], l[3]), cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
		}


		/////// ***** peaklines

		////////////////////////////////////////
		std::vector <cv::Vec4i> peakLines = linesConnected;
		std::vector <cv::Vec4i> peakLinesSort;

		//int numLines = (int)peakLines.size();
		float theta;
		float rheo;
		std::vector<float> distanceDU;
		std::vector<float> distanceDV;

		// Initilization to NAN (not zaro) using for find the longest distnaces
		for (size_t i = 0; i < peakLines.size(); i++)
		{
			distanceDV.push_back(NAN);
			distanceDU.push_back(NAN);
		}
		// Check lines if vertical or horizantal are
		for (int i = 0; i < peakLines.size(); i++)
		{
			cv::Vec4i l = peakLines[i];
			cv::Point p1(l[0], l[1]);
			cv::Point p2(l[2], l[3]);
			cv::Point p0(0, 0);
			double d0 = GetDistanceFromPointToLine(p0, l);
			double r = cv::norm(p1 - p2);
			double s = (p1.y - p2.y) / r;
			if (s < -1) s = -1;
			if (s > 1) s = 1;
			double t = asin(s);

			rheo = d0;
			theta = t;

			if (rheo < 0)
			{
				theta = theta - PI;
				rheo = abs(rheo);
			}

			if (theta < PI / 2 - 0.5 && theta > PI / 2 - 0.5)
			{
				rheo = rheo * -1;
				theta = theta - PI / 2;
			}

			std::vector<float> X;
			std::vector<float> Y;

			if ((abs(theta) > (PI / 4)) && (abs(theta) < (3 * PI / 4)))
			{
				distanceDV[i] = d0;
			}
			else
			{
				distanceDU[i] = d0;
			}
		}

		// Finding the longest distance from center consider the desired line
		//if (isnan(distanceDV[0]))
		//	distanceDV[0] = 0;
		//if (isnan(distanceDU[0]))
		//	distanceDU[0] = 0;

		int iDUMax = -1;
		int iDUMin = -1;
		int iDVMax = -1;
		int iDVMin = -1;

		float maxDU = NAN;
		float minDU = NAN;
		float maxDV = NAN;
		float minDV = NAN;

		std::vector<float> dUTemp;
		for (int i = 0; i < peakLines.size(); i++) {
			if (!isnan(distanceDU[i]))
				dUTemp.push_back(distanceDU[i]);
		}
		std::vector<float> dVTemp;
		for (int i = 0; i < peakLines.size(); i++) {
			if (!isnan(distanceDV[i]))
				dVTemp.push_back(distanceDV[i]);
		}
		maxDU = getMax(dUTemp);
		minDU = getMin(dUTemp);
		maxDV = getMax(dVTemp);
		minDV = getMin(dVTemp);

		for (int i = 0; i < peakLines.size(); ++i) {
			if (maxDU == distanceDU[i]) {
				iDUMax = i;
			}
			if (minDU == distanceDU[i]) {
				iDUMin = i;
			}
			if (maxDV == distanceDV[i]) {
				iDVMax = i;
			}
			if (minDV == distanceDV[i]) {
				iDVMin = i;
			}
		}

		//check if pearlines are collapsed or not
		if (minDU == maxDU) {
			minDU = NAN;
			maxDU = NAN;
			iDUMin = -1;
			iDUMax = -1;
		}
		if (minDV == maxDV) {
			minDV = NAN;
			maxDV = NAN;
			iDVMin = -1;
			iDVMax = -1;
		}

		// Determine the desired Peaklines
		int assumptionLine = 1;
		std::vector<cv::Vec4i> ReseveLine;
		// If any reason couldn't find total 4 lines, it asumpt the following asumption lines
		if (iDUMax == -1 || iDUMin == -1 || iDVMax == -1 || iDVMin == -1)
		{
			assumptionLine = 0;

			cv::Point ptLT = cv::Point(0, 0);
			cv::Point ptRT = cv::Point((int)((double)resizedImg.size[0] * 0.98), 0);
			cv::Point ptRB = cv::Point((int)((double)resizedImg.size[0] * 0.98), (int)((double)resizedImg.size[0] * 0.98));
			cv::Point ptLB = cv::Point(0, (int)((double)resizedImg.size[0] * 0.98));
			cv::Vec4i ln0 = cv::Vec4i(ptLT.x, ptLT.y, ptRT.x, ptRT.y);
			ReseveLine.push_back(ln0);
			cv::Vec4i ln1 = cv::Vec4i(ptRT.x, ptRT.y, ptRB.x, ptRB.y);
			ReseveLine.push_back(ln1);
			cv::Vec4i ln2 = cv::Vec4i(ptRB.x, ptRB.y, ptLB.x, ptLB.y);
			ReseveLine.push_back(ln2);
			cv::Vec4i ln3 = cv::Vec4i(ptLB.x, ptLB.y, ptLT.x, ptLT.y);
			ReseveLine.push_back(ln3);
		}

		//vector <cv::Vec4i> desiredPeakLines;

		if (assumptionLine == 1)
		{
			// Determine the desired Peaklines
			//desiredPeakLines.push_back(peakLines[iDUMin]);	//top-left to top-right
			//desiredPeakLines.push_back(peakLines[iDVMax]);	//top-right to bottom-right
			//desiredPeakLines.push_back(peakLines[iDUMax]);	//bottom-right to bottom-left
			//desiredPeakLines.push_back(peakLines[iDVMin]);	//bottom-left to top-left

			// Using peaklines for sorting
			peakLinesSort.push_back(peakLines[iDUMin]);	//top-left to top-right
			peakLinesSort.push_back(peakLines[iDVMax]);	//top-right to bottom-right
			peakLinesSort.push_back(peakLines[iDUMax]);	//bottom-right to bottom-left
			peakLinesSort.push_back(peakLines[iDVMin]);	//bottom-left to top-left
		}
		else
		{
			if (isnan(minDU) || isfinite(minDU) || iDUMin == -1)
			{
				//desiredPeakLines.push_back(ReseveLine[0]);
				peakLinesSort.push_back(ReseveLine[0]);
			}
			else
			{
				//desiredPeakLines.push_back(peakLines[iDUMin]);
				peakLinesSort.push_back(peakLines[iDUMin]);
			}
			if (isnan(maxDV) || isfinite(maxDV) || iDVMax == -1)
			{
				//desiredPeakLines.push_back(ReseveLine[1]);
				peakLinesSort.push_back(ReseveLine[1]);
			}
			else
			{
				//desiredPeakLines.push_back(peakLines[iDVMax]);
				peakLinesSort.push_back(peakLines[iDVMax]);
			}
			if (isnan(maxDU) || isfinite(maxDU) || iDUMax == -1)
			{
				//desiredPeakLines.push_back(ReseveLine[2]);
				peakLinesSort.push_back(ReseveLine[2]);
			}
			else
			{
				//desiredPeakLines.push_back(peakLines[iDUMax]);
				peakLinesSort.push_back(peakLines[iDUMax]);
			}
			if (isnan(minDV) || isfinite(minDV) || iDVMin == -1)
			{
				//desiredPeakLines.push_back(ReseveLine[3]);
				peakLinesSort.push_back(ReseveLine[3]);
			}
			else
			{
				//desiredPeakLines.push_back(peakLines[iDVMin]);
				peakLinesSort.push_back(peakLines[iDVMin]);
			}
		}

		//// Sorting peaklines
		//sort(peakLinesSort.begin(), peakLinesSort.end(), [](const cv::Vec4i & a, const cv::Vec4i & b) { 
		//	cv::Point p0(0, 0);
		//	double da = GetDistanceFromPointToLine(p0, a);
		//	double db = GetDistanceFromPointToLine(p0, b);
		//	return (da < db); 
		//});


		// Declaration
		cv::Point result_pRT = getCrossPoint(cv::Point(peakLinesSort[0][0], peakLinesSort[0][1]), cv::Point(peakLinesSort[0][2], peakLinesSort[0][3]),
			cv::Point(peakLinesSort[1][0], peakLinesSort[1][1]), cv::Point(peakLinesSort[1][2], peakLinesSort[1][3]));
		cv::Point result_pRB = getCrossPoint(cv::Point(peakLinesSort[1][0], peakLinesSort[1][1]), cv::Point(peakLinesSort[1][2], peakLinesSort[1][3]),
			cv::Point(peakLinesSort[2][0], peakLinesSort[2][1]), cv::Point(peakLinesSort[2][2], peakLinesSort[2][3]));
		cv::Point result_pLB = getCrossPoint(cv::Point(peakLinesSort[2][0], peakLinesSort[2][1]), cv::Point(peakLinesSort[2][2], peakLinesSort[2][3]),
			cv::Point(peakLinesSort[3][0], peakLinesSort[3][1]), cv::Point(peakLinesSort[3][2], peakLinesSort[3][3]));
		cv::Point result_pLT = getCrossPoint(cv::Point(peakLinesSort[3][0], peakLinesSort[3][1]), cv::Point(peakLinesSort[3][2], peakLinesSort[3][3]),
			cv::Point(peakLinesSort[0][0], peakLinesSort[0][1]), cv::Point(peakLinesSort[0][2], peakLinesSort[0][3]));

		pt1->X = (int)((double)result_pLT.x / imgScalingRatio);
		pt1->Y = (int)((double)result_pLT.y / imgScalingRatio);
		pt2->X = (int)((double)result_pRT.x / imgScalingRatio);
		pt2->Y = (int)((double)result_pRT.y / imgScalingRatio);
		pt3->X = (int)((double)result_pRB.x / imgScalingRatio);
		pt3->Y = (int)((double)result_pRB.y / imgScalingRatio);
		pt4->X = (int)((double)result_pLB.x / imgScalingRatio);
		pt4->Y = (int)((double)result_pLB.y / imgScalingRatio);
		/*
		CGSize orgImgSize = {(CGFloat)orgImg.cols, (CGFloat)orgImg.rows};
		UIImage *imageTemp = [self imageDrawingQuadrilateralOnClearBGBySize:orgImgSize Points:points MarkColor:[UIColor greenColor] MarkPointIndex:-1];
		_imageViewOverlay1.image = imageTemp;
		 */
	}

	bool ImgProcUtil::DetectEdge2(array<System::Byte>^ imageSrc, System::Collections::Generic::List<System::Drawing::Point>^% points)
	{
		int outWidth;
		int outHeight;

		cv::Mat srcImg;
		pin_ptr<Byte> p = &imageSrc[0];   // entire array is now pinned
		unsigned char * cp = p;
		int docImageLen = imageSrc->Length;
		srcImg = loadImageFromByteArray(cp, docImageLen);
		System::Drawing::Point^ pt1 = gcnew System::Drawing::Point();
		System::Drawing::Point^ pt2 = gcnew System::Drawing::Point();
		System::Drawing::Point^ pt3 = gcnew System::Drawing::Point();
		System::Drawing::Point^ pt4 = gcnew System::Drawing::Point();
		detectEdge2(srcImg, pt1, pt2, pt3, pt4);
		points->Clear();
		points->Add(*pt1);
		points->Add(*pt2);
		points->Add(*pt3);
		points->Add(*pt4);

		return true;
	}
#endif

	struct DistItem {
		float val;
		int idx;
	};

	struct DistAttr {
		float dist;		//distance
		DistItem st;	//start
		DistItem en;	//end
	};

	struct RectAttr{
		int wh;
		double ratioDiff;
		cv::Point ptLT;
		cv::Point ptLB;
		cv::Point ptRB;
		cv::Point ptRT;
		DistAttr du;
		DistAttr dv;
	};

	bool compDistSort(DistItem i, DistItem j) { return (i.val < j.val); }
	bool compDistAttrt(DistAttr i, DistAttr j) { return (i.dist < j.dist); }
	bool compRectAttrt(RectAttr i, RectAttr j) { return (i.wh < j.wh); }
	bool pickRectByHVRatio(double expectedRatio, double peakLineConnectionThreshold,
		const std::vector<LineAttr>& peakLines,
		const std::vector<float>& distanceDU,
		const std::vector<float>& distanceDV,
		int imgWidth, int imgHeight,
		int& iDUMin, int& iDUMax, int& iDVMin, int& iDVMax, cv::Mat cdstP5
		) {
		//_RPT1(_CRT_WARN, "pickRectByHVRatio expectedRatio:%d\n", expectedRatio);

		std::vector<DistItem> sortedDistanceDU;
		std::vector<DistItem> sortedDistanceDV;
		//exclude NaN
		for (int i = 0; i < distanceDU.size(); i++) {
			if (!isnan(distanceDU[i]))
				sortedDistanceDU.push_back(DistItem{ distanceDU[i], i});
		}
		for (int j = 0; j < distanceDV.size(); j++) {
			if (!isnan(distanceDV[j]))
				sortedDistanceDV.push_back(DistItem{ distanceDV[j], j });
		}

		//numeric sort
		std::sort(sortedDistanceDU.begin(), sortedDistanceDU.end(), compDistSort);
		std::reverse(sortedDistanceDU.begin(), sortedDistanceDU.end());
		std::sort(sortedDistanceDV.begin(), sortedDistanceDV.end(), compDistSort);
		std::reverse(sortedDistanceDV.begin(), sortedDistanceDV.end());

		std::vector<DistAttr> combinationOfDU;
		for (int i = 0; i < sortedDistanceDU.size(); i++) {
			for (int ii = i + 1; ii < sortedDistanceDU.size(); ii++) {
				//0:dist, 1:st 2:en
				DistAttr distAttr = { sortedDistanceDU[i].val - sortedDistanceDU[ii].val, sortedDistanceDU[ii], sortedDistanceDU[i] };
				combinationOfDU.push_back(distAttr);
			}
		}
		std::vector<DistAttr> combinationOfDV;
		for (int j = 0; j < sortedDistanceDV.size(); j++) {
			for (int jj = j + 1; jj < sortedDistanceDV.size(); jj++) {
				//0:dist, 1:st, 2:en
				DistAttr distAttr = { sortedDistanceDV[j].val - sortedDistanceDV[jj].val, sortedDistanceDV[jj], sortedDistanceDV[j] };
				combinationOfDV.push_back(distAttr);
			}
		}

		std::vector<DistAttr> sortedCombinationOfDU(combinationOfDU);
		//descending sort by dist
		std::sort(sortedCombinationOfDU.begin(), sortedCombinationOfDU.end(), compDistAttrt);
		std::reverse(sortedCombinationOfDU.begin(), sortedCombinationOfDU.end());

		std::vector<DistAttr> sortedCombinationOfDV(combinationOfDV);
		//descending sort by dist
		std::sort(sortedCombinationOfDV.begin(), sortedCombinationOfDV.end(), compDistAttrt);
		std::reverse(sortedCombinationOfDV.begin(), sortedCombinationOfDV.end());

		std::vector<RectAttr> combinationOfRect;
		for (int i = 0; i < sortedCombinationOfDU.size(); i++) {
			int hst = sortedCombinationOfDU[i].st.val;	//st
			int ii = 0;
			LineAttr lineH1 = peakLines[sortedCombinationOfDU[i].st.idx];
			int hen = sortedCombinationOfDU[i].en.val;	//en
			LineAttr lineH2 = peakLines[sortedCombinationOfDU[i].en.idx];
			for (int j = 0; j < sortedCombinationOfDV.size(); j++) {
				int wst = sortedCombinationOfDV[j].st.val;	//st
				int jj = 0;
				LineAttr lineW1 = peakLines[sortedCombinationOfDV[j].st.idx];
				int wen = sortedCombinationOfDV[j].en.val;	//en;
				LineAttr lineW2 = peakLines[sortedCombinationOfDV[j].en.idx];

				//reject if line is too shorter than oposit
				cv::Point ptW1_1 = lineW1.p1;
				cv::Point ptW1_2 = lineW1.p2;
				cv::Point ptW2_1 = lineW2.p1;
				cv::Point ptW2_2 = lineW2.p2;
				double lenW1 = lineW1.r;
				double lenW2 = lineW2.r;
				//_RPT5(_CRT_WARN, "ptW1_1(%d,%d) ptW1_2(%d,%d) lenW1:%f\n", ptW1_1.x, ptW1_1.y, ptW1_2.x, ptW1_2.y, lenW1);
				//_RPT5(_CRT_WARN, "ptW2_1(%d,%d) ptW2_2(%d,%d) lenW2:%f\n", ptW2_1.x, ptW2_1.y, ptW2_2.x, ptW2_2.x, lenW2);
				double rateW = lenW1 / lenW2;
				if (rateW < 0.5 || 2.0 < rateW) {
					//_RPT2(_CRT_WARN, "rateW:%d\n", rateW);
					continue;
				}

				cv::Point ptH1_1 = lineH1.p1;
				cv::Point ptH1_2 = lineH1.p2;
				cv::Point ptH2_1 = lineH2.p1;
				cv::Point ptH2_2 = lineH2.p2;
				double lenH1 = lineH1.r;
				double lenH2 = lineH2.r;
				//_RPT5(_CRT_WARN, "ptH1_1(%d,%d) ptH1_2(%d,%d) lenH1:%f\n", ptH1_1.x, ptH1_1.y, ptH1_2.x, ptH1_2.y, lenH1);
				//_RPT5(_CRT_WARN, "ptH2_1(%d,%d) ptH2_2(%d,%d) lenH2:%f\n", ptH2_1.x, ptH2_1.y, ptH2_2.x, ptH2_2.x, lenH2);
				double rateH = lenH1 / lenH2;
				if (rateH < 0.5 || 2.0 < rateH) {
					//_RPT1(_CRT_WARN, "rateH:%d\n", rateH);
					continue;
				}

				//reject if each line is too far from its neighbour
				double distW1_1_H1_1 = cv::norm(ptW1_1 - ptH1_1);
				double distW1_1_H1_2 = cv::norm(ptW1_1 - ptH1_2);
				double distW1_2_H1_1 = cv::norm(ptW1_2 - ptH1_1);
				double distW1_2_H1_2 = cv::norm(ptW1_2 - ptH1_2);
				//_RPT4(_CRT_WARN, "distW1_1_H1_1:%f distW1_1_H1_2:%f distW1_2_H1_1:%f distW1_2_H1_2:%f\n",
				//	distW1_1_H1_1, distW1_1_H1_2, distW1_2_H1_1, distW1_2_H1_2);
				std::vector<double> lenW1H1{ distW1_1_H1_1, distW1_1_H1_2, distW1_2_H1_1, distW1_2_H1_2 };
				double distW1H1Min = *std::min_element(lenW1H1.begin(), lenW1H1.end());
				//_RPT1(_CRT_WARN, "distW1H1Min:%f\n", distW1H1Min);
				if (distW1H1Min > peakLineConnectionThreshold) {
					//_RPT2(_CRT_WARN, "distW1H1Min(=%f) > peakLineConnectionThreshold(=%f)\n", distW1H1Min, peakLineConnectionThreshold);
					continue;
				}

				double distW1_1_H2_1 = cv::norm(ptW1_1 - ptH2_1);
				double distW1_1_H2_2 = cv::norm(ptW1_1 - ptH2_2);
				double distW1_2_H2_1 = cv::norm(ptW1_2 - ptH2_1);
				double distW1_2_H2_2 = cv::norm(ptW1_2 - ptH2_2);
				//_RPT4(_CRT_WARN, "distW1_1_H1_1:%f distW1_1_H1_2:%f distW1_2_H1_1:%f distW1_2_H1_2:%f\n",
				//	distW1_1_H2_1, distW1_1_H2_2, distW1_2_H2_1, distW1_2_H2_2);
				std::vector<double> lenW1H2{ distW1_1_H2_1, distW1_1_H2_2, distW1_2_H2_1, distW1_2_H2_2 };
				double distW1H2Min = *std::min_element(lenW1H2.begin(), lenW1H2.end());
				//_RPT1(_CRT_WARN, "distW1H2Min:%f\n", distW1H2Min);
				if (distW1H2Min > peakLineConnectionThreshold) {
					//_RPT2(_CRT_WARN, "distW1H2Min(=%f) > peakLineConnectionThreshold(=%f)\n", distW1H2Min, peakLineConnectionThreshold);
					continue;
				}

				double distW2_1_H1_1 = cv::norm(ptW2_1 - ptH1_1);
				double distW2_1_H1_2 = cv::norm(ptW2_1 - ptH1_2);
				double distW2_2_H1_1 = cv::norm(ptW2_2 - ptH1_1);
				double distW2_2_H1_2 = cv::norm(ptW2_2 - ptH1_2);
				//_RPT4(_CRT_WARN, "distW2_1_H1_1:%f distW2_1_H1_2:%f distW2_2_H1_1:%f distW2_2_H1_2:%f\n",
				//	distW2_1_H1_1, distW2_1_H1_2, distW2_2_H1_1, distW2_2_H1_2);
				std::vector<double> lenW2H1{ distW2_1_H1_1, distW2_1_H1_2, distW2_2_H1_1, distW2_2_H1_2 };
				double distW2H1Min = *std::min_element(lenW2H1.begin(), lenW2H1.end());
				//_RPT1(_CRT_WARN, "distW2H1Min:%f\n", distW2H1Min);
				if (distW2H1Min > peakLineConnectionThreshold) {
					//_RPT2(_CRT_WARN, "distW2H1Min(=%f) > peakLineConnectionThreshold(=%f)\n", distW2H1Min, peakLineConnectionThreshold);
					continue;
				}

				double distW2_1_H2_1 = cv::norm(ptW2_1 - ptH2_1);
				double distW2_1_H2_2 = cv::norm(ptW2_1 - ptH2_2);
				double distW2_2_H2_1 = cv::norm(ptW2_2 - ptH2_1);
				double distW2_2_H2_2 = cv::norm(ptW2_2 - ptH2_2);
				//_RPT4(_CRT_WARN, "distW2_1_H2_1:%f distW2_1_H2_2:%f distW2_2_H2_1:%f distW2_2_H2_2:%f\n",
				//	distW2_1_H2_1, distW2_1_H2_2, distW2_2_H2_1, distW2_2_H2_2);
				std::vector<double> lenW2H2{ distW2_1_H2_1, distW2_1_H2_2, distW2_2_H2_1, distW2_2_H2_2 };
				double distW2H2Min = *std::min_element(lenW2H2.begin(), lenW2H2.end());
				//_RPT1(_CRT_WARN, "distW2H2Min:%f\n", distW2H2Min);
				if (distW2H2Min > peakLineConnectionThreshold) {
					//_RPT2(_CRT_WARN, "distW2H2Min(=%f) > peakLineConnectionThreshold(=%f)\n", distW2H2Min, peakLineConnectionThreshold);
					continue;
				}

				//reject if both of edge are also far away
				if (distW1H1Min + distW1H2Min > peakLineConnectionThreshold * 1.5) {
					//_RPT2(_CRT_WARN, "distW1H1Min + distW1H2Min (%f) > peakLineConnectionThreshold (%f)", distW1H1Min + distW1H2Min, peakLineConnectionThreshold * 1.5);
					continue;
				}
				if (distW1H1Min + distW2H1Min > peakLineConnectionThreshold * 1.5) {
					//_RPT2(_CRT_WARN, "distW1H1Min + distW2H1Min (%f) > peakLineConnectionThreshold (%f)", distW1H1Min + distW2H1Min, peakLineConnectionThreshold * 1.5);
					continue;
				}
				if (distW2H1Min + distW2H2Min > peakLineConnectionThreshold * 1.5) {
					//_RPT2(_CRT_WARN, "distW2H1Min + distW2H2Min (%f) > peakLineConnectionThreshold (%f)", distW2H1Min + distW2H2Min, peakLineConnectionThreshold * 1.5);
					continue;
				}
				if (distW1H2Min + distW2H2Min > peakLineConnectionThreshold * 1.5) {
					//_RPT2(_CRT_WARN, "distW1H2Min + distW2H2Min (%f) > peakLineConnectionThreshold (%f)", distW1H2Min + distW2H2Min, peakLineConnectionThreshold * 1.5);
					continue;
				}

				//cv::line(cdstP5, cv::Point(lineW1[0], lineW1[1]), cv::Point(lineW1[2], lineW1[3]), cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
				//cv::line(cdstP5, cv::Point(lineW2[0], lineW2[1]), cv::Point(lineW2[2], lineW2[3]), cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
				//cv::line(cdstP5, cv::Point(lineH1[0], lineH1[1]), cv::Point(lineH1[2], lineH1[3]), cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
				//cv::line(cdstP5, cv::Point(lineH2[0], lineH2[1]), cv::Point(lineH2[2], lineH2[3]), cv::Scalar(0, 255, 0), 3, cv::LINE_AA);

				//check if lines are far away of nor 
				cv::Point ptLT = getCrossPoint(
					lineW1.p1, lineW1.p2,lineH1.p1, lineH1.p2);
				cv::Point ptRT = getCrossPoint(
					lineW1.p1, lineW1.p2, lineH2.p1, lineH2.p2);
				cv::Point ptRB = getCrossPoint(
					lineW2.p1, lineW2.p2, lineH2.p1, lineH2.p2);
				cv::Point ptLB = getCrossPoint(
					lineW2.p1, lineW2.p2, lineH1.p1, lineH1.p2);

				//pick rectangles within visible rect
				if (ptLT.x > 0 && ptLT.y > 0
					&& ptRT.x < imgWidth && ptRT.y > 0
					&& ptRB.x < imgWidth && ptRB.y < imgHeight
					&& ptLB.x > 0 && ptLB.y < imgHeight) {
					int lenW1 = cv::norm(ptLT - ptRT);
					int lenH1 = cv::norm(ptLT - ptLB);
					int lenW2 = cv::norm(ptRB - ptLB);
					int lenH2 = cv::norm(ptRT - ptRB);
					int w = (lenW1 + lenW2) / 2;
					if (w == 0)
						break;
					int h = (lenH1 + lenH2) / 2;
					if (h == 0)
						break;
					double aspectRatio = (double)w / (double)h;
					double ratioDiff = abs(aspectRatio - expectedRatio);
					//_RPT5(_CRT_WARN, "pickRectByHVRatio [%d,%d] w:%d h:%d aspectRatio: %f\n", i, j, w, h, aspectRatio);
					RectAttr attr = {w * h, ratioDiff, ptLT, ptLB, ptRB, ptRT, sortedCombinationOfDU[i], sortedCombinationOfDV[j] };
					combinationOfRect.push_back(attr);

					cv::line(cdstP5, ptLT, ptRT, cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
					cv::line(cdstP5, ptRT, ptRB, cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
					cv::line(cdstP5, ptRB, ptLB, cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
					cv::line(cdstP5, ptLB, ptLT, cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
				}
			}
		}

		//find least diff of ratio
		//descending sort by w*h
		std::sort(combinationOfRect.begin(), combinationOfRect.end(), compRectAttrt);
		std::reverse(combinationOfRect.begin(), combinationOfRect.end());
		//has largest rect and lesser diff of ratio than allowed rate
		int iStX = -1;
		int iEnX = -1;
		int iStY = -1;
		int iEnY = -1;
		bool bRet = false;
		if (combinationOfRect.size() > 0) {
			int idx = 0;
			double allowedDiffRate = 0.2;
			for (idx = 0; idx < combinationOfRect.size(); idx++) {
				if (combinationOfRect[idx].ratioDiff < allowedDiffRate) {
					iStX = combinationOfRect[idx].du.st.idx;
					iEnX = combinationOfRect[idx].du.en.idx;
					iStY = combinationOfRect[idx].dv.st.idx;
					iEnY = combinationOfRect[idx].dv.en.idx;
					if (iStX > -1 && iEnX > -1 && iStY > -1 && iEnY > -1) {
						//_RPT3(_CRT_WARN, "iStX:%d distanceDU:%f\n", iStX, distanceDU[iStX]);
						//_RPT3(_CRT_WARN, "iStY:%d distanceDV:%f\n", iStY, distanceDV[iStY]);
						//_RPT3(_CRT_WARN, "iEnX:%d distanceDU:%f\n", iEnX, distanceDU[iEnX]);
						//_RPT3(_CRT_WARN, "iEnY:%d distanceDV:%f\n", iEnY, distanceDV[iEnY]);
						bRet = true;
					}
					break;
				}
			}
		}

		iDUMin = iStX;
		iDUMax = iEnX;
		iDVMin = iStY;
		iDVMax = iEnY;
		return bRet;
	}
#if false
	bool detectEdge3(cv::Mat orgImg, System::Drawing::Point^% pt1, System::Drawing::Point^% pt2, System::Drawing::Point^% pt3, System::Drawing::Point^% pt4) {
		cv::Mat resizedImg; // = imageCV.clone();
		//--------------------
		//1 Resizing image
		//--------------------
		cv::Size s;;
		double imgHeight = orgImg.rows;
		double imgWidth = orgImg.cols;
		double imgHVRatio = imgHeight / imgWidth;
		if (imgHVRatio > 1) {
			imgHeight = 335*4;
			imgWidth = imgHeight / imgHVRatio;
			s = cv::Size(imgWidth, imgHeight);
		}
		else {
			imgWidth = 335*4;
			imgHeight = imgWidth * imgHVRatio;
			s = cv::Size(imgWidth, imgHeight);
		}
		double imgScalingRatio = imgHeight / orgImg.rows;
		resize(orgImg, resizedImg, s, CV_INTER_CUBIC); //CV_INTER_CUBIC |  CV_INTER_LINEAR

		//UIImage *imageResized = MatToUIImage(resizedImg);
		//_imageView1.image = imageResized;

		
		//--------------------
		//2 Convert image to Grayscale
		//--------------------
		//cv::Mat grayImg;
		//cvtColor(resizedImg, grayImg, CV_RGBA2GRAY);
		
		
		//--------------------
		//2 Apply edge detection kernel 
		//--------------------		
		cv::Point anchor = cv::Point(-1, -1);
		double delta = 0;
		int ddepth = -1;
		cv::Mat kernel = (cv::Mat_<double>(3, 3) << -1, -1, -1, -1, 9, -1, -1, -1, -1);
		/// Apply filter
		cv::Mat filteredImg;
		filter2D(resizedImg, filteredImg, ddepth, kernel, anchor, delta, cv::BORDER_DEFAULT);

/*
		//----------------------------------------
		//3 Using opening & closing Morphologic & Guasian filter ( Prepaer Image for edge detection)
		//----------------------------------------
		cv::Mat meane, covs;
		cv::calcCovarMatrix(resizedImg, covs, meane, CV_COVAR_NORMAL | CV_COVAR_ROWS | CV_COVAR_SCALE);

		meane = meane / resizedImg.rows;

		//cv::Scalar meanf = mean(meane);
		cv::Scalar meanIma = mean(resizedImg);
		cv::Scalar meanedge = mean(covs);

		//float defualtmeancov = (float)meanf[0];
		int meanImag = (int)meanIma[0];
		int meanedgeint2 = (int)meanedge[0];
		int smeanT = sqrt(meanedgeint2);
		cv::Mat adapImg;
*/
		int morph_size_open = 1; // Consider size of element size base on size of image
		int morph_size_close = 19; // Consider size of element size base on size of image
		//int repeatFindLine = 0;
/*
		int adapConstant = 5 * smeanT;
		if (smeanT < 20 && smeanT > 10)
		{
			if (meanImag < 200)
				morph_size_close = 19;
			else if (meanImag < 180)
				morph_size_close = 19;
			adapConstant = 5 * smeanT;
		}
		else if (smeanT < 10)
		{
			morph_size_close = 19;
			adapConstant = 5 * smeanT;
		}
		else if (smeanT >= 20 && smeanT < 30)
		{

			if (meanImag < 200)
				morph_size_close = 19;
			else if (meanImag > 180)
				morph_size_close = 1;
			morph_size_close = 19;
			adapConstant = smeanT * 2.5;

		}
		else if (smeanT > 30)
		{
			adapConstant = 1 * smeanT;
			morph_size_close = 12;
		}

		//adaptiveThreshold(grayImg3, _meanAdaptiveThresholding, smeanT, CV_ADAPTIVE_THRESH_MEAN_C, cv::THRESH_BINARY, 5, adapConstant);
		cv::Mat meanAdaptiveThresholding;
		adaptiveThreshold(resizedImg, meanAdaptiveThresholding, smeanT, CV_ADAPTIVE_THRESH_MEAN_C, cv::THRESH_BINARY, 5, adapConstant);
*/

		cv::Mat element_open = getStructuringElement(cv::MORPH_RECT, cv::Size(2 * morph_size_open + 1, 2 * morph_size_open + 1), cv::Point(morph_size_open, morph_size_open));
		cv::Mat element_close = getStructuringElement(cv::MORPH_RECT, cv::Size(2 * morph_size_close + 1, 2 * morph_size_close + 1), cv::Point(morph_size_close, morph_size_close));

		cv::Mat gaussianImg;
		//GaussianBlur(grayImg, grayImg3, cv::Size(15, 15), sqrt(2));
		//morphologyEx(grayImg3, _morphImg, cv::MORPH_OPEN, element_open, cv::Point(-1, -1));
		GaussianBlur(filteredImg, gaussianImg, cv::Size(15, 15), sqrt(2));
		cv::Mat morphImg;
		morphologyEx(gaussianImg, morphImg, cv::MORPH_OPEN, element_open, cv::Point(-1, -1));
		morphologyEx(morphImg, morphImg, cv::MORPH_CLOSE, element_close, cv::Point(-1, -1));

		GaussianBlur(morphImg, morphImg, cv::Size(15, 15), sqrt(2));
		//cv::Mat edgePreparedImg = morphImg - meanAdaptiveThresholding;
		//UIImage *imageOut = MatToUIImage(edgePreparedImg);

		//----------------------------------------
		//4 Finding the lines by canny Filtering
		//----------------------------------------
		cv::Mat cannyImg;
		int lowcanny = 10;
		int highcanny = lowcanny * 3;
		Canny(morphImg, cannyImg, lowcanny, highcanny);
		//UIImage *imageOut = MatToUIImage(cannyImg);


		//----------------------------------------
		//5 Extract points of Images
		//----------------------------------------
		std::vector<cv::Vec3f> linesOut;
		std::vector<cv::Vec4i> linesOutP;
		std::vector<cv::Vec4i> linesMerged;
		std::vector<cv::Vec4i> linesNotFarAway;
		std::vector<cv::Vec4i> linesConnected;
		//HoughLines(cannyOut, linesOut, 1, CV_PI / 180, 100, 0, 0);
		int sizeMin = min(cannyImg.cols, cannyImg.rows);
		int threshold = sizeMin / 10;
		int deltaThreshold = sizeMin / 20;
		int connectionThreshold = sizeMin / 10;
		int lineThreshold = sizeMin / 10;
		int thresholdHoughLines = 50;
		int deltaRhoThreshold = sizeMin / 50;
		int lineSegThreshold = max(sizeMin / 200, 2);
		int lineIsolationThreshold = sizeMin / 10;
		double lineThetaThreshold = 0.05;
		double mergedLineDensityThreshold = 0.25;

		//_RPT3(_CRT_WARN, "sizeMin:%d threshold:%d deltaThreshold:%d\n", sizeMin, threshold, deltaThreshold);
		//_RPT3(_CRT_WARN, "connectionThreshold:%d lineThreshold:%d lineSegThreshold:%d\n", connectionThreshold, lineThreshold, lineSegThreshold);
		//_RPT1(_CRT_WARN, "lineIsolationThreshold:%d\n", lineIsolationThreshold);
		//_RPT2(_CRT_WARN, "lineThetaThreshold:%f mergedLineDensityThreshold:%f\n", lineThetaThreshold, mergedLineDensityThreshold);

		// Copy edges to the images that will display the results in BGR
		cv::Mat cdstP;	//just for debug
		cvtColor(cannyImg, cdstP, CV_GRAY2BGR);
		cv::Mat cdstP2 = cdstP.clone();	//just for debug
		cv::Mat cdstP3 = cdstP.clone();	//just for debug
		cv::Mat cdstP4 = cdstP.clone();	//just for debug
		cv::Mat imgDetectedEdge = cdstP.clone();	//just for debug

		// Probabilistic Line Transform
		HoughLinesP(cannyImg, linesOutP, 1, CV_PI / 180, thresholdHoughLines, sizeMin / 30, sizeMin / 50);
		// Draw the lines
		int sizeLinesP = linesOutP.size();
		double* x1 = new double[sizeLinesP];
		double* y1 = new double[sizeLinesP];
		double* x2 = new double[sizeLinesP];
		double* y2 = new double[sizeLinesP];
		double* t = new double[sizeLinesP];	//angle by radians
		double* r = new double[sizeLinesP];	//line length
		double* rh = new double[sizeLinesP];	//distance from center
		bool* merged = new bool[sizeLinesP];
		memset(x1, 0, sizeof(double) * sizeLinesP);
		memset(y1, 0, sizeof(double) * sizeLinesP);
		memset(x2, 0, sizeof(double) * sizeLinesP);
		memset(y2, 0, sizeof(double) * sizeLinesP);
		memset(t, 0, sizeof(double)* sizeLinesP);
		memset(r, 0, sizeof(double)* sizeLinesP);
		memset(rh, 0, sizeof(double)* sizeLinesP);
		memset(merged, 0, sizeof(bool) * sizeLinesP);

		for (size_t i = 0; i < linesOutP.size(); i++)
		{
			cv::Vec4i l = linesOutP[i];
			cv::Point p1 = cv::Point(l[0], l[1]);
			cv::Point p2 = cv::Point(l[2], l[3]);
			line(cdstP, p1, p2, cv::Scalar(0, 0, 255), 3, cv::LINE_AA);	//just for debug

			x1[i] = l[0];
			y1[i] = l[1];
			x2[i] = l[2];
			y2[i] = l[3];
			//y = ax + b;
			//when x = 0; y = b
			//x = cy + d;
			//when y = 0; x = d

			r[i] = cv::norm(p1 - p2);
			double s = (y1[i] - y2[i]) / r[i];
			if (s < -1) s = -1;
			if (s > 1) s = 1;
			t[i] = asin(s);
			rh[i] = GetDistanceFromPointToLine(cv::Point(0, 0), l);
		}


		for (size_t i = 0; i < sizeLinesP; i++)
		{
			//double lenLine1 = cv::norm(cv::Point(x1[i], y1[i]) - cv::Point(x2[i], y2[i]));
			double lenLine1 = r[i];
			//double len2 = sqrt(pow(x1[i] - x2[i], 2) + pow(y1[i] - y2[i], 2));
			if (!merged[i] && lenLine1 > lineSegThreshold) {

				double mergedT = t[i];
				double mergedX1 = 0;
				double mergedY1 = 0;
				double mergedX2 = 0;
				double mergedY2 = 0;
				double mergedLenTotal = lenLine1;
				double mergedRho = rh[i];

				//if (-1 < mergedA && mergedA < 1) {
				if (CV_PI * 0.25 > abs(mergedT)) {
					if (x1[i] <= x2[i]) {
						mergedX1 = x1[i];
						mergedY1 = y1[i];
						mergedX2 = x2[i];
						mergedY2 = y2[i];
					}
					else {
						mergedX1 = x2[i];
						mergedY1 = y2[i];
						mergedX2 = x1[i];
						mergedY2 = y1[i];
					}
				}
				else {
					if (y1[i] <= y2[i]) {
						mergedY1 = y1[i];
						mergedX1 = x1[i];
						mergedY2 = y2[i];
						mergedX2 = x2[i];
					}
					else {
						mergedY1 = y2[i];
						mergedX1 = x2[i];
						mergedY2 = y1[i];
						mergedX2 = x1[i];
					}
				}
				//_RPT5(_CRT_WARN, "Finding lines to merge with %d mergedT:%f mergedRho:%f\n", i, mergedT, mergedRho);

				for (size_t j = i + 1; j < sizeLinesP; j++) {
					//double lenLine2 = cv::norm(cv::Point(x1[j], y1[j]) - cv::Point(x2[j], y2[j]));
					double lenLine2 = r[j];
					if (!merged[j] && lenLine2 > lineSegThreshold) {
						double dT = NAN;
						if ((mergedT <= 0 && t[j] <= 0) || (mergedT >= 0 && t[j] >= 0)) {
							dT = abs(mergedT - t[j]);
						}
						else {
							dT = abs(mergedT) + abs(t[j]);
							if (dT > CV_PI * 0.5 && dT <= CV_PI) {
								dT = CV_PI - dT;
							}
							else if (dT > CV_PI) {
								dT = dT - CV_PI;
							}
						}
						double dRho = abs(mergedRho - rh[j]);
						double dTRate = dT / lineThetaThreshold;
						double dRhoRate = dRho / deltaRhoThreshold;

						//if (-1 < mergedA && mergedA < 1) {
						if (CV_PI * 0.25 > abs(mergedT)) {
							//|x| > |y|
							// check if a and b of lines are same or not, if same, these lines are on the same line
							//if (dA < lineAngleThreshold) {
							double dRho = abs(mergedRho - rh[j]);
							//_RPT4(_CRT_WARN, "[%d] t:%f dT:%f dRho:%f\n", j, t[j], dT, dRho);
							//if diffeence of theta is larger, larger difference of Rho is allowed
							if (dTRate <= 1 && dRhoRate <= 1 && dRhoRate <= max(dTRate, 0.5)) {
								//these lines are on the same line
								//_RPT2(_CRT_WARN, "merged t:%f dRho:%f\n", t[j], dRho);
								merged[j] = true;
								mergedLenTotal += lenLine2;
								//find min and max of X
								if (x1[j] <= x2[j]) {
									//find min of X
									if (x1[j] < mergedX1) {
										mergedX1 = x1[j];
										mergedY1 = y1[j];
									}
									//find max of X
									if (x2[j] > mergedX2) {
										mergedX2 = x2[j];
										mergedY2 = y2[j];
									}
								}
								else {
									//find min of X
									if (x2[j] < mergedX1) {
										mergedX1 = x2[j];
										mergedY1 = y2[j];
									}
									//find max of X
									if (x1[j] > mergedX2) {
										mergedX2 = x1[j];
										mergedY2 = y1[j];
									}
								}
							}
						}
						else {
							//|x| < |y|
							// check if a and b of lines are same or not, if same, these lines are on the same line
							//if (dC < lineAngleThreshold) {
							double dRho = abs(mergedRho - rh[j]);
							//_RPT4(_CRT_WARN, "[%d] t:%f dT:%f dRho:%f\n", j, t[j], dT, dRho);
							//if diffeence of theta is larger, larger difference of Rho is allowed
							if (dTRate <= 1 && dRhoRate <= 1 && dRhoRate <= max(dTRate, 0.5)) {
								//these lines are on the same line
								//_RPT2(_CRT_WARN, "merged t:%f dRho:%f\n", t[j], dRho);
								merged[j] = true;
								mergedLenTotal += lenLine2;
								//find min and max of Y
								if (y1[j] <= y2[j]) {
									//find min of Y
									if (y1[j] < mergedY1) {
										mergedY1 = y1[j];
										mergedX1 = x1[j];
									}
									//find max of Y
									if (y2[j] > mergedY2) {
										mergedY2 = y2[j];
										mergedX2 = x2[j];
									}
								}
								else {
									//find min of Y
									if (y2[j] < mergedY1) {
										mergedY1 = y2[j];
										mergedX1 = x2[j];
									}
									//find max of Y
									if (y1[j] > mergedY2) {
										mergedY2 = y1[j];
										mergedX2 = x1[j];
									}
								}
							}
						}
					}
				}//for loop j
				cv::Vec4i line;
				line[0] = mergedX1;
				line[1] = mergedY1;
				line[2] = mergedX2;
				line[3] = mergedY2;
				double lenMerged = cv::norm(cv::Point(line[0], line[1]) - cv::Point(line[2], line[3]));
				//_RPT2(_CRT_WARN, "### mergedLenTotal:%f lenMerged:%f\n", mergedLenTotal, lenMerged);
				if (mergedLenTotal / lenMerged > mergedLineDensityThreshold && lenMerged > lineThreshold) {
					//_RPT4(_CRT_WARN, "Added to linesMerged (%f,%f)-(%f,%f) ", mergedX1, mergedY1, mergedX2, mergedY2);
					//_RPT2(_CRT_WARN, "t:%f rh:%f\n", mergedT, mergedRho);
					linesMerged.push_back(line);
				}
			}
		}//for loop i

		delete[] x1;
		delete[] y1;
		delete[] x2;
		delete[] y2;
		delete[] t;
		delete[] r;
		delete[] merged;
		x1 = NULL;
		y1 = NULL;
		x2 = NULL;
		y2 = NULL;
		t = NULL;
		r = NULL;
		merged = NULL;

		//just for debug
		for (size_t i = 0; i < linesMerged.size(); i++)
		{
			cv::Vec4i l = linesMerged[i];
			line(cdstP2, cv::Point(l[0], l[1]), cv::Point(l[2], l[3]), cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
		}

		//remove lines far away
		for (size_t i = 0; i < linesMerged.size(); i++) {
			cv::Vec4i l = linesMerged[i];
			cv::Point p1(l[0], l[1]);
			cv::Point p2(l[2], l[3]);
			double r = cv::norm(p1 - p2);
			//_RPT5(_CRT_WARN, "[%d] (%d,%d)-(%d,%d) r:%f\n", i, p1.x, p1.y, p2.x, p2.y, r);
			bool isolated = true;
			for (size_t j = 0; j < linesMerged.size(); j++) {
				if (i == j)
					continue;

				cv::Vec4i ln = linesMerged[j];
				cv::Point pn1(ln[0], ln[1]);
				cv::Point pn2(ln[2], ln[3]);
				double rn = cv::norm(p1 - pn1);
				//_RPT5(_CRT_WARN, "--[%d] (%d,%d)-(%d,%d) rn:%f\n", j, p1.x, p1.y, pn1.x, pn1.y, rn);
				if (rn < lineIsolationThreshold) {
					//_RPT5(_CRT_WARN, "line %d is close to %d\n", i, j);
					isolated = false;
					break;
				}
				rn = cv::norm(p1 - pn2);
				//_RPT5(_CRT_WARN, "--[%d] (%d,%d)-(%d,%d) rn:%f\n", j, p1.x, p1.y, pn2.x, pn2.y, rn);
				if (rn < lineIsolationThreshold) {
					//_RPT5(_CRT_WARN, "line %d is close to %d\n", i, j);
					isolated = false;
					break;
				}
				rn = cv::norm(p2 - pn1);
				//_RPT5(_CRT_WARN, "--[%d] (%d,%d)-(%d,%d) rn:%f\n", j, p2.x, p2.y, pn1.x, pn1.y, rn);
				if (rn < lineIsolationThreshold) {
					//_RPT5(_CRT_WARN, "line %d is close to %d\n", i, j);
					isolated = false;
					break;
				}
				rn = cv::norm(p2 - pn2);
				//_RPT5(_CRT_WARN, "--[%d] (%d,%d)-(%d,%d) rn:%f\n", j, p2.x, p2.y, pn2.x, pn2.y, rn);
				if (rn < lineIsolationThreshold) {
					//_RPT5(_CRT_WARN, "line %d is close to %d\n", i, j);
					isolated = false;
					break;
				}
			}
			if (!isolated) {
				linesNotFarAway.push_back(l);
			}
		}

		//just for debug
		for (size_t i = 0; i < linesNotFarAway.size(); i++)
		{
			cv::Vec4i l = linesNotFarAway[i];
			line(cdstP3, cv::Point(l[0], l[1]), cv::Point(l[2], l[3]), cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
		}

		//find neighbours
		//check if end of line connect to end of other lines 
		bool* hasConnection = new bool[linesNotFarAway.size()];
		memset(hasConnection, 0, sizeof(bool) * linesNotFarAway.size());
		if (linesNotFarAway.size() < 4) {
			//failed to find edge
		}
		else if (linesNotFarAway.size() <= 4) {
			//add all lines
			linesConnected = linesNotFarAway;
		}
		else {
			for (size_t i = 0; i < linesNotFarAway.size(); i++) {

				cv::Vec4i l = linesNotFarAway[i];
				cv::Point p1(l[0], l[1]);
				cv::Point p2(l[2], l[3]);
				double r = cv::norm(p1 - p2);
				double s = (p1.y - p2.y) / r;
				if (s < -1) s = -1;
				if (s > 1) s = 1;
				double t = asin(s);
				//_RPT4(_CRT_WARN, "testing connection of %d r:%f s:%f t:%f\n", i, r, s, t);

				bool connectedToP1 = false;
				bool connectedToP2 = false;
				for (size_t j = i + 1; j < linesNotFarAway.size(); j++) {

					cv::Vec4i ln = linesNotFarAway[j];
					cv::Point pn1(ln[0], ln[1]);
					cv::Point pn2(ln[2], ln[3]);

					//check the angle between lines
					//if angle is sharp, it's not a corner.
					double rn = cv::norm(pn1 - pn2);
					double sn = (pn1.y - pn2.y) / rn;
					if (sn < -1) sn = -1;
					if (sn > 1) sn = 1;
					double tn = asin(sn);
					double dT = NAN;
					if ((t <= 0 && tn <= 0) || (t >= 0 && tn >= 0)) {
						dT = abs(t - tn);
					}
					else {
						dT = abs(t) + abs(tn);
						if (dT > CV_PI * 0.5 && dT <= CV_PI) {
							dT = CV_PI - dT;
						}
						else if (dT > CV_PI) {
							dT = dT - CV_PI;
						}
					}
					//_RPT4(_CRT_WARN, "rn:%f sn:%f tn:%f dT:%f\n", rn, sn, tn, dT);
					if (dT < PI / 4) {
						//_RPT2(_CRT_WARN, "%d and %d are NOT connected bcoz less angle\n", i, j);
						continue;
					}

					bool connected = false;
					double d = cv::norm(p1 - pn1);
					//_RPT5(_CRT_WARN, "(%d,%d)-(%d,%d):%f\n", p1.x, p1.y, pn1.x, pn1.y, d);
					if (d < connectionThreshold) {
						connected = true;
						connectedToP1 = true;
					}
					else {
						d = cv::norm(p1 - pn2);
						//_RPT5(_CRT_WARN, "(%d,%d)-(%d,%d):%f\n", p1.x, p1.y, pn2.x, pn2.y, d);
						if (d < connectionThreshold) {
							connected = true;
							connectedToP1 = true;
						}
						else {
							d = cv::norm(p2 - pn1);
							//_RPT5(_CRT_WARN, "(%d,%d)-(%d,%d):%f\n", p2.x, p2.y, pn1.x, pn1.y, d);
							if (d < connectionThreshold) {
								connected = true;
								connectedToP2 = true;
							}
							else {
								d = cv::norm(p2 - pn2);
								//_RPT5(_CRT_WARN, "(%d,%d)-(%d,%d):%f\n", p2.x, p2.y, pn2.x, pn2.y, d);
								if (d < connectionThreshold) {
									connected = true;
									connectedToP2 = true;
								}
							}
						}
					}
					if (connected) {
						//_RPT2(_CRT_WARN, "%d and %d are connected\n", i, j);
						hasConnection[i] = true;
						hasConnection[j] = true;
					}
					else {
						//_RPT1(_CRT_WARN, "%d not connected\n", j);
					}
					if (connectedToP1 && connectedToP2)
						break;
				}
			}
			//add only lines connected to another line
			for (size_t i = 0; i < linesNotFarAway.size(); i++) {
				//if the line connected to others, add to list
				cv::Vec4i l = linesNotFarAway[i];
				if (hasConnection[i])
					linesConnected.push_back(l);
			}
		}

		for (size_t i = 0; i < linesConnected.size(); i++)
		{
			cv::Vec4i l = linesConnected[i];
			line(cdstP4, cv::Point(l[0], l[1]), cv::Point(l[2], l[3]), cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
		}


		/////// ***** peaklines

		////////////////////////////////////////
		std::vector <cv::Vec4i> peakLines = linesConnected;
		std::vector <cv::Vec4i> peakLinesSort;

		//int numLines = (int)peakLines.size();
		float theta;
		float rheo;
		std::vector<float> distanceDU;
		std::vector<float> distanceDV;

		// Initilization to NAN (not zaro) using for find the longest distnaces
		for (size_t i = 0; i < peakLines.size(); i++)
		{
			distanceDV.push_back(NAN);
			distanceDU.push_back(NAN);
		}
		// Check lines if vertical or horizantal are
		for (int i = 0; i < peakLines.size(); i++)
		{
			cv::Vec4i l = peakLines[i];
			cv::Point p1(l[0], l[1]);
			cv::Point p2(l[2], l[3]);
			cv::Point p0(0, 0);
			double d0 = GetDistanceFromPointToLine(p0, l);
			double r = cv::norm(p1 - p2);
			double s = (p1.y - p2.y) / r;
			if (s < -1) s = -1;
			if (s > 1) s = 1;
			double t = asin(s);

			rheo = d0;
			theta = t;

			if (rheo < 0)
			{
				theta = theta - PI;
				rheo = abs(rheo);
			}

			if (theta < PI / 2 - 0.5 && theta > PI / 2 - 0.5)
			{
				rheo = rheo * -1;
				theta = theta - PI / 2;
			}

			if ((abs(theta) > (PI / 4)) && (abs(theta) < (3 * PI / 4)))
			{
				distanceDU[i] = d0;
			}
			else
			{
				distanceDV[i] = d0;
			}
		}

		// Finding the longest distance from center consider the desired line
		//if (isnan(distanceDV[0]))
		//	distanceDV[0] = 0;
		//if (isnan(distanceDU[0]))
		//	distanceDU[0] = 0;

		int iDUMax = -1;
		int iDUMin = -1;
		int iDVMax = -1;
		int iDVMin = -1;

		float maxDU = NAN;
		float minDU = NAN;
		float maxDV = NAN;
		float minDV = NAN;

		std::vector<float> dUTemp;
		for (int i = 0; i < peakLines.size(); i++) {
			if (!isnan(distanceDU[i]))
				dUTemp.push_back(distanceDU[i]);
		}
		std::vector<float> dVTemp;
		for (int i = 0; i < peakLines.size(); i++) {
			if (!isnan(distanceDV[i]))
				dVTemp.push_back(distanceDV[i]);
		}
		maxDU = getMax(dUTemp);
		minDU = getMin(dUTemp);
		maxDV = getMax(dVTemp);
		minDV = getMin(dVTemp);

		for (int i = 0; i < peakLines.size(); ++i) {
			if (maxDU == distanceDU[i]) {
				iDUMax = i;
			}
			if (minDU == distanceDU[i]) {
				iDUMin = i;
			}
			if (maxDV == distanceDV[i]) {
				iDVMax = i;
			}
			if (minDV == distanceDV[i]) {
				iDVMin = i;
			}
		}

		if (peakLines.size() > 50)
			return false;

		double expectedHVRatio = 3.35 / 2.15;
		if (expectedHVRatio > 0 ) {
			 pickRectByHVRatio(expectedHVRatio, peakLineConnectionThreshold, peakLines, distanceDU, distanceDV, cannyImg.cols, cannyImg.rows,
				 iDUMin, iDUMax, iDVMin, iDVMax);
			minDU = distanceDU[iDUMin];
			maxDU = distanceDU[iDUMax];
			minDV = distanceDV[iDVMin];
			maxDV = distanceDV[iDVMax];
		}


		//check if pearlines are collapsed or not
		if (minDU == maxDU) {
			minDU = NAN;
			maxDU = NAN;
			iDUMin = -1;
			iDUMax = -1;
		}
		if (minDV == maxDV) {
			minDV = NAN;
			maxDV = NAN;
			iDVMin = -1;
			iDVMax = -1;
		}

		// Determine the desired Peaklines
		bool bFoundEdges = false;
		int assumptionLine = 1;
		std::vector<cv::Vec4i> ReseveLine;
		if (iDUMin != -1 && iDUMax != -1 && iDVMin != -1 && iDVMax != -1)
		{
			bFoundEdges = true;
			peakLinesSort.push_back(peakLines[iDUMin]);	//left edge (bottom-left to top-left)
			peakLinesSort.push_back(peakLines[iDVMax]);	//bottom edge (bottom-right to bottom-left)
			peakLinesSort.push_back(peakLines[iDUMax]);	//right edge (top-right to bottom-right)
			peakLinesSort.push_back(peakLines[iDVMin]);	//top-left to top-right

			//just for debug
			//_RPT1(_CRT_WARN, "peakLinesSort.length:%d\n", peakLinesSort.size());
			for (int i = 0; i < peakLinesSort.size(); i++)
			{
				cv::Vec4i l = peakLinesSort[i];
				//_RPT5(_CRT_WARN, "peakLinesSort[%d](%d,%d)-(%d,%d)\n", i, l[0], l[1], l[2], l[3]);
			}

			//cross point between the left edge (bottom-left to top-left), and bottom edge (bottom-right to bottom-left)
			cv::Point result_pLB = getCrossPoint(cv::Point(peakLinesSort[0][0], peakLinesSort[0][1]), cv::Point(peakLinesSort[0][2], peakLinesSort[0][3]),
				cv::Point(peakLinesSort[1][0], peakLinesSort[1][1]), cv::Point(peakLinesSort[1][2], peakLinesSort[1][3]));
			//_RPT2(_CRT_WARN, "result_pLB:%d,%d\n", result_pLB.x, result_pLB.y);
			//cross point between the bottom edge (bottom-right to bottom-left), and right edge (top-right to bottom-right)
			cv::Point result_pRB = getCrossPoint(cv::Point(peakLinesSort[1][0], peakLinesSort[1][1]), cv::Point(peakLinesSort[1][2], peakLinesSort[1][3]),
				cv::Point(peakLinesSort[2][0], peakLinesSort[2][1]), cv::Point(peakLinesSort[2][2], peakLinesSort[2][3]));
			//_RPT2(_CRT_WARN, "result_pRB:%d,%d\n", result_pRB.x, result_pRB.y);
			//cross point between the right edge (top-right to bottom-right), and top edge (top-left to top-right)
			cv::Point result_pRT = getCrossPoint(cv::Point(peakLinesSort[2][0], peakLinesSort[2][1]), cv::Point(peakLinesSort[2][2], peakLinesSort[2][3]),
				cv::Point(peakLinesSort[3][0], peakLinesSort[3][1]), cv::Point(peakLinesSort[3][2], peakLinesSort[3][3]));
			//_RPT2(_CRT_WARN, "result_pRT:%d,%d\n", result_pRT.x, result_pRT.y);
			//cross point between the top edge (top-left to top-right), and left edge (bottom-left to top-left)
			cv::Point result_pLT = getCrossPoint(cv::Point(peakLinesSort[3][0], peakLinesSort[3][1]), cv::Point(peakLinesSort[3][2], peakLinesSort[3][3]),
				cv::Point(peakLinesSort[0][0], peakLinesSort[0][1]), cv::Point(peakLinesSort[0][2], peakLinesSort[0][3]));
			//_RPT2(_CRT_WARN, "result_pLT:%d,%d\n", result_pLT.x, result_pLT.y);

			pt1->X = (int)((double)result_pLT.x / imgScalingRatio);
			pt1->Y = (int)((double)result_pLT.y / imgScalingRatio);
			pt2->X = (int)((double)result_pRT.x / imgScalingRatio);
			pt2->Y = (int)((double)result_pRT.y / imgScalingRatio);
			pt3->X = (int)((double)result_pRB.x / imgScalingRatio);
			pt3->Y = (int)((double)result_pRB.y / imgScalingRatio);
			pt4->X = (int)((double)result_pLB.x / imgScalingRatio);
			pt4->Y = (int)((double)result_pLB.y / imgScalingRatio);

			//just for debug
			cv::line(imgDetectedEdge, result_pLT, result_pRT, cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
			cv::line(imgDetectedEdge, result_pRT, result_pRB, cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
			cv::line(imgDetectedEdge, result_pRB, result_pLB, cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
			cv::line(imgDetectedEdge, result_pLB, result_pLT, cv::Scalar(0, 255, 0), 3, cv::LINE_AA);

			//_RPT2(_CRT_WARN, "DetectEdge pt1:(%d,%d)\n", pt1->X, pt1->Y);
			//_RPT2(_CRT_WARN, "DetectEdge pt2:(%d,%d)\n", pt2->X, pt2->Y);
			//_RPT2(_CRT_WARN, "DetectEdge pt3:(%d,%d)\n", pt3->X, pt3->Y);
			//_RPT2(_CRT_WARN, "DetectEdge pt4:(%d,%d)\n", pt4->X, pt4->Y);

		}
		return bFoundEdges;
	}

	bool ImgProcUtil::DetectEdge3(array<System::Byte>^ imageSrc, System::Collections::Generic::List<System::Drawing::Point>^% points)
	{
		int outWidth;
		int outHeight;

		cv::Mat srcImg;
		pin_ptr<Byte> p = &imageSrc[0];   // entire array is now pinned
		unsigned char* cp = p;
		int docImageLen = imageSrc->Length;
		srcImg = loadImageFromByteArray(cp, docImageLen);
		System::Drawing::Point^ pt1 = gcnew System::Drawing::Point();
		System::Drawing::Point^ pt2 = gcnew System::Drawing::Point();
		System::Drawing::Point^ pt3 = gcnew System::Drawing::Point();
		System::Drawing::Point^ pt4 = gcnew System::Drawing::Point();
		if (detectEdge3(srcImg, pt1, pt2, pt3, pt4))
		{
			points->Clear();
			points->Add(*pt1);
			points->Add(*pt2);
			points->Add(*pt3);
			points->Add(*pt4);
			return true;
		}
		return false;
	}
#endif


	//pt [I]: point near the line
	//ps [I]: start of line
	//pe [I]: end of line
	//return: the nearest point on the line
	cv::Point GetNearestPointToLine(cv::Point pt, cv::Point p1, cv::Point p2) {
		int x1 = p1.x;
		int y1 = p1.y;
		int x2 = p2.x;
		int y2 = p2.y;
		//_RPT2(_CRT_WARN, "GetNearestPointToLine pt:(%d,%d)\n", pt.x, pt.y);
		//_RPT4(_CRT_WARN, "line:(%d,%d)-(%d,%d)\n", x1, y1, x2, y2);

		if (x1 == x2) {
			//line is vertical
			return cv::Point(x1, pt.y);
		}
		else if (y1 == y2) {
			//line is horizontal
			return cv::Point(pt.x, y1);
		}
		else {
			double a = (double)(y1 - y2) / (double)(x1 - x2);
			if (!isfinite(a)) {
				//line is vertical
				return cv::Point(x1, pt.y);
			}
			else {
				double b = (double)y1 - (a * (double)x1);//y - ax
				double c = (double)(x1 - x2) / (double)(y1 - y2);
				double d = (double)x1 - (double)(c * (double)y1);//x - cy
				double r = cv::norm(p1 - p2);
				double s = (double)(y1 - y2) / r;
				if (s < -1) s = -1;
				if (s > 1) s = 1;
				double t = asin(s);
				//console.log("GetNearestPointToLine x1:" + x1 + " y1:" + y1 + " x2:" + x2 + " y2:" + y2 + " a:" + a + " b:" + b + " c:" + c + " d:" + d + " r:" + r + " t:" + t);
				if (abs(t) < CV_PI * 0.25) {
					double vert_a = -1.0 / a;
					if (!isfinite(vert_a)) {
						//line is horizontal
						return cv::Point(pt.x, y1);
					}
					else {
						double vert_b = (double)pt.y - vert_a * (double)pt.x;
						double cx = (vert_b - b) / (a - vert_a);
						double cy = (a * cx) + b;
						return cv::Point(cx, cy);
					}
				}
				else {
					double vert_c = -1.0 / c;
					if (!isfinite(vert_c)) {
						//line is vertical
						return cv::Point(x1, pt.y);
					}
					else {
						double vert_d = (double)pt.x - (vert_c * (double)pt.y);
						//x = cy + d
						//x/c = y + d/c
						double cy = (d - vert_d) / (vert_c - c);
						double cx = (c * cy) + d;
						return cv::Point(cx, cy);
					}
				}
			}
		}
	}
#ifdef USE_SYSTEM_DRAWING
	bool detectEdge4(cv::Mat orgImg, System::Drawing::Point^% pt1, System::Drawing::Point^% pt2, System::Drawing::Point^% pt3, System::Drawing::Point^% pt4, double expectedHVRatio) {
		cv::Mat resizedImg; // = imageCV.clone();
		//--------------------
		//1 Resizing image
		//--------------------
		cv::Size s;;
		double imgHeight = orgImg.rows;
		double imgWidth = orgImg.cols;
		double imgHVRatio = imgHeight / imgWidth;
		if (imgHVRatio > 1) {
			imgHeight = 335 * 4;
			imgWidth = imgHeight / imgHVRatio;
			s = cv::Size(imgWidth, imgHeight);
		}
		else {
			imgWidth = 335 * 4;
			imgHeight = imgWidth * imgHVRatio;
			s = cv::Size(imgWidth, imgHeight);
		}
		double imgScalingRatio = imgHeight / orgImg.rows;
		resize(orgImg, resizedImg, s, CV_INTER_CUBIC); //CV_INTER_CUBIC |  CV_INTER_LINEAR

		//UIImage *imageResized = MatToUIImage(resizedImg);
		//_imageView1.image = imageResized;


		//--------------------
		//2 Convert image to Grayscale
		//--------------------
		//cv::Mat grayImg;
		//cvtColor(resizedImg, grayImg, CV_RGBA2GRAY);


		//--------------------
		//2 Apply edge detection kernel 
		//--------------------		
		cv::Point anchor = cv::Point(-1, -1);
		double delta = 0;
		int ddepth = -1;
		cv::Mat kernel = (cv::Mat_<double>(3, 3) << -1, -1, -1, -1, 9, -1, -1, -1, -1);
		/// Apply filter
		cv::Mat filteredImg;
		filter2D(resizedImg, filteredImg, ddepth, kernel, anchor, delta, cv::BORDER_DEFAULT);

		//----------------------------------------
		//3 Using opening & closing Morphologic & Guasian filter ( Prepaer Image for edge detection)
		//----------------------------------------
		int morph_size_open = 1; // Consider size of element size base on size of image
		int morph_size_close = 19; // Consider size of element size base on size of image

		cv::Mat element_open = getStructuringElement(cv::MORPH_RECT, cv::Size(2 * morph_size_open + 1, 2 * morph_size_open + 1), cv::Point(morph_size_open, morph_size_open));
		cv::Mat element_close = getStructuringElement(cv::MORPH_RECT, cv::Size(2 * morph_size_close + 1, 2 * morph_size_close + 1), cv::Point(morph_size_close, morph_size_close));

		cv::Mat gaussianImg;
		//GaussianBlur(grayImg, grayImg3, cv::Size(15, 15), sqrt(2));
		//morphologyEx(grayImg3, _morphImg, cv::MORPH_OPEN, element_open, cv::Point(-1, -1));
		GaussianBlur(filteredImg, gaussianImg, cv::Size(15, 15), sqrt(2));
		cv::Mat morphImg;
		morphologyEx(gaussianImg, morphImg, cv::MORPH_OPEN, element_open, cv::Point(-1, -1));
		morphologyEx(morphImg, morphImg, cv::MORPH_CLOSE, element_close, cv::Point(-1, -1));

		GaussianBlur(morphImg, morphImg, cv::Size(15, 15), sqrt(2));
		//cv::Mat edgePreparedImg = morphImg - meanAdaptiveThresholding;
		//UIImage *imageOut = MatToUIImage(edgePreparedImg);

		//----------------------------------------
		//4 Finding the lines by canny Filtering
		//----------------------------------------
		cv::Mat cannyImg;
		int lowcanny = 10;
		int highcanny = lowcanny * 3;
		Canny(morphImg, cannyImg, lowcanny, highcanny);
		//UIImage *imageOut = MatToUIImage(cannyImg);


		//----------------------------------------
		//5 Extract points of Images
		//----------------------------------------
		std::vector<cv::Vec3f> linesOut;
		std::vector<cv::Vec4i> linesOutP;
		std::vector<LineAttr> linesMerged;
		std::vector<LineAttr> linesNotFarAway;
		std::vector<LineAttr> linesConnected;
		int sizeMin = cv::min(cannyImg.cols, cannyImg.rows);
		int thresholdHoughLines = 50;
		double houghLinesLineLenMin = sizeMin / 50.0;
		double houghLinesLineGapMax = sizeMin / 30.0;
		int connectionThreshold = sizeMin / 8;
		//double peakLineConnectionThreshold = sizeMin / 4.0;
		double peakLineConnectionThreshold = sizeMin / 2.0;
		int lineThreshold = sizeMin / 10;
		int lineSegThreshold = cv::max(sizeMin / 200, 2);
		int lineIsolationThreshold = sizeMin / 4;
		//double deltaThetaThreshold = 0.025;
		double deltaThetaThreshold = 0.05;
		double deltaRhoThreshold = sizeMin / 50.0;
		double mergedLineDensityThreshold = 0.5;

		//_RPT3(_CRT_WARN, "sizeMin:%d thresholdHoughLines:%d houghLinesLineLenMin:%f houghLinesLineGapMax:%f\n", sizeMin, thresholdHoughLines, houghLinesLineLenMin, houghLinesLineGapMax);
		//_RPT3(_CRT_WARN, "connectionThreshold:%d lineThreshold:%d lineSegThreshold:%d\n", connectionThreshold, lineThreshold, lineSegThreshold);
		//_RPT1(_CRT_WARN, "lineIsolationThreshold:%d peakLineConnectionThreshold:%f\n", lineIsolationThreshold, peakLineConnectionThreshold);
		//_RPT2(_CRT_WARN, "deltaRhoThreshold:%f deltaThetaThreshold: %f mergedLineDensityThreshold:%f\n", deltaRhoThreshold, deltaThetaThreshold, mergedLineDensityThreshold);

		// Copy edges to the images that will display the results in BGR
		cv::Mat cdstP;	//just for debug
		cvtColor(cannyImg, cdstP, CV_GRAY2BGR);
		cv::Mat cdstP1 = cdstP.clone();	//just for debug
		//cv::Mat cdstP2 = cdstP.clone();	//just for debug
		cv::Mat cdstP3 = cdstP.clone();	//just for debug
		cv::Mat cdstP4 = cdstP.clone();	//just for debug
		cv::Mat imgDetectedEdge = cdstP.clone();	//just for debug

		//----------------------------------------
		//5-1. Get lines by HoughLinesP
		//----------------------------------------
		// Probabilistic Line Transform
		HoughLinesP(cannyImg, linesOutP, 1, CV_PI / 180.0, thresholdHoughLines, houghLinesLineLenMin, houghLinesLineGapMax);
		// Draw the lines
		//int sizeLinesP = linesOutP.size();
		std::vector<LineAttr> lineAttrs;
		
		for (size_t i = 0; i < linesOutP.size(); i++)
		{
			LineAttr lineAttr;
			cv::Vec4i l = linesOutP[i];
			lineAttr.p1 = cv::Point(l[0], l[1]);
			lineAttr.p2 = cv::Point(l[2], l[3]);
			cv::line(cdstP, lineAttr.p1, lineAttr.p2, cv::Scalar(0, 0, 255), 3, cv::LINE_AA);	//just for debug
			lineAttr.r = cv::norm(lineAttr.p1 - lineAttr.p2);
			if (lineAttr.r > lineSegThreshold) {
				cv::line(cdstP1, lineAttr.p1, lineAttr.p2, cv::Scalar(0, 0, 255), 3, cv::LINE_AA);	//just for debug
				double s = (double)(lineAttr.p1.y - lineAttr.p2.y) / lineAttr.r;
				if (s < -1) s = -1;
				if (s > 1) s = 1;
				lineAttr.t = asin(s);
				lineAttr.rh = GetDistanceFromPointToLine(cv::Point(0, 0), lineAttr);
				lineAttr.merged = false;
				lineAttrs.push_back(lineAttr);
			}
		}

		//sort by line length
		std::sort(lineAttrs.begin(), lineAttrs.end(), compareLineAttrByLengthDesc);
		for (size_t i = 0; i < lineAttrs.size(); i++) {
			lineAttrs[i].idx = i;
		}

		//----------------------------------------
		//5-2. Merge lines
		//----------------------------------------
		for (size_t i = 0; i < lineAttrs.size(); i++)
		{
			//double lenLine1 = cv::norm(cv::Point(x1[i], y1[i]) - cv::Point(x2[i], y2[i]));
			double lenLine1 = lineAttrs[i].r;
			//_RPT4(_CRT_WARN, "lineAttrs[%d] merged:%d lenLine1:%f t:%f ", i, lineAttrs[i].merged, lenLine1, lineAttrs[i].t);
			//_RPT4(_CRT_WARN, "(%d,%d)-(%d,%d) ", lineAttrs[i].p1.x, lineAttrs[i].p1.y, lineAttrs[i].p2.x, lineAttrs[i].p2.y);
			//_RPT1(_CRT_WARN, "Math.abs(t):%f\n", abs(lineAttrs[i].t));

			if (!lineAttrs[i].merged
				&& ((abs(lineAttrs[i].t) > CV_PI * 0.875f
					|| (abs(lineAttrs[i].t) < CV_PI * 0.625f && abs(lineAttrs[i].t) > CV_PI * 0.375f)
					|| abs(lineAttrs[i].t) < CV_PI * 0.125f))
				) {

				double mergedT = lineAttrs[i].t;
				cv::Point mergedP1(0, 0);
				cv::Point mergedP2(0, 0);
				double mergedLenTotal = lenLine1;
				double mergedRho = lineAttrs[i].rh;

				//if (-1 < mergedA && mergedA < 1) {
				if (CV_PI * 0.25f > abs(mergedT)) {
					if (lineAttrs[i].p1.x <= lineAttrs[i].p2.x) {
						mergedP1.x = lineAttrs[i].p1.x;
						mergedP1.y = lineAttrs[i].p1.y;
						mergedP2.x = lineAttrs[i].p2.x;
						mergedP2.y = lineAttrs[i].p2.y;
					}
					else {
						mergedP1.x = lineAttrs[i].p2.x;
						mergedP1.y = lineAttrs[i].p2.y;
						mergedP2.x = lineAttrs[i].p1.x;
						mergedP2.y = lineAttrs[i].p1.y;
					}
				}
				else {
					if (lineAttrs[i].p1.y <= lineAttrs[i].p2.y) {
						mergedP1.y = lineAttrs[i].p1.y;
						mergedP1.x = lineAttrs[i].p1.x;
						mergedP2.y = lineAttrs[i].p2.y;
						mergedP2.x = lineAttrs[i].p2.x;
					}
					else {
						mergedP1.y = lineAttrs[i].p2.y;
						mergedP1.x = lineAttrs[i].p2.x;
						mergedP2.y = lineAttrs[i].p1.y;
						mergedP2.x = lineAttrs[i].p1.x;
					}
				}

				//find lines seems to be on the same line
				//_RPT5(_CRT_WARN, "Finding lines to merge with %d mergedT:%f mergedRho:%f ", i, mergedT, mergedRho);
				//_RPT4(_CRT_WARN, "lenLine1:%f t:%f ", lenLine1, lineAttrs[i].t);
				//_RPT4(_CRT_WARN, "(%d,%d)-(%d,%d)\n", lineAttrs[i].p1.x, lineAttrs[i].p1.y, lineAttrs[i].p2.x, lineAttrs[i].p2.y);
				std::vector<LineAttr*> segLines;
				for (size_t j = i + 1; j < lineAttrs.size(); j++) {
					double lenLine2 = lineAttrs[j].r;
					if (!lineAttrs[j].merged && lenLine2 > lineSegThreshold) {
						double dT = NAN;
						if ((mergedT <= 0.0 && lineAttrs[j].t <= 0.0) || (mergedT >= 0.0 && lineAttrs[j].t >= 0.0)) {
							dT = abs(mergedT - lineAttrs[j].t);
						}
						else {
							dT = abs(mergedT) + abs(lineAttrs[j].t);
							if (dT > CV_PI * 0.5 && dT <= CV_PI) {
								dT = CV_PI - dT;
							}
							else if (dT > CV_PI) {
								dT = dT - CV_PI;
							}
						}
						double dRho = abs(mergedRho - lineAttrs[j].rh);
						double dTRate = dT / deltaThetaThreshold;
						double dRhoRate = dRho / deltaRhoThreshold;
						if (dTRate <= 1.0 && dRhoRate <= 1.0) {
							//_RPT4(_CRT_WARN, "lineAttrs[%d] lenLine1:%f t:%f ", j, lenLine2, lineAttrs[j].t);
							//_RPT4(_CRT_WARN, "(%d,%d)-(%d,%d) ", lineAttrs[j].p1.x, lineAttrs[j].p1.y, lineAttrs[j].p2.x, lineAttrs[j].p2.y);
							//_RPT3(_CRT_WARN, "dRho:%f dTRate:%f dRhoRate:%f\n", dRho, dTRate, dRhoRate);
							//_RPT0(_CRT_WARN, "-->Add to seglines\n");
							segLines.push_back(&lineAttrs[j]);
						}
					}
				}
				//_RPT1(_CRT_WARN, "segLines.size():%d\n", segLines.size());

				// sort 
				if (CV_PI * 0.25 > abs(mergedT)) {
					//|x| > |y|
					std::sort(segLines.begin(), segLines.end(), compareLineAttrByMinXDesc);
				}
				else {
					//|x| < |y|
					std::sort(segLines.begin(), segLines.end(), compareLineAttrByMinYDesc);
				}

				// merge
				bool bFoundMergalbeLine = false;
				do {
					bFoundMergalbeLine = false;
					for (size_t k = 0; k < segLines.size(); k++) {
						//_RPT5(_CRT_WARN, "[%d] mergetP1(%d,%d) mergedP2(%d,%d)\n", i, mergedP1.x, mergedP1.y, mergedP2.x, mergedP2.y);
						LineAttr* seg = segLines[k];
						//_RPT5(_CRT_WARN, "seg->idx:%d seg->p1(%d,%d) seg->p2(%d,%d)\n", seg->idx, seg->p1.x, seg->p1.y, seg->p2.x, seg->p2.y);
						if (!seg->merged) {
							if (CV_PI * 0.25f > abs(mergedT)) {
								//|x| > |y|
								double distOfLines = 0;
								if (seg->p1.x <= seg->p2.x) {
									if (seg->p2.x < mergedP1.x) {
										//x1...x2...mergedX1...mergedX2
										distOfLines = cv::norm(mergedP1 - seg->p2);
										//_RPT1(_CRT_WARN, "x1...x2...mergedX1...mergedX2 distOfLines=%f\n", distOfLines);
										if (distOfLines > lineIsolationThreshold) {
											//_RPT1(_CRT_WARN, "distOfLines=%f\n", distOfLines);
										}
										else {
											//calc point on original line
											cv::Point ptNewEnd = GetNearestPointToLine(seg->p1, mergedP1, mergedP2);
											mergedP1.x = ptNewEnd.x;
											mergedP1.y = ptNewEnd.y;
											mergedLenTotal += seg->r;
											seg->merged = true;
										}
									}
									else if (seg->p1.x < mergedP1.x && seg->p2.x < mergedP2.x) {
										//x1...mergedX1...x2...mergedX2
										//calc point on original line
										cv::Point ptNewEnd = GetNearestPointToLine(seg->p1, mergedP1, mergedP2);
										mergedP1.x = ptNewEnd.x;
										mergedP1.y = ptNewEnd.y;
										mergedLenTotal = cv::norm(mergedP2 - seg->p1);
										seg->merged = true;
									}
									else if (seg->p1.x < mergedP1.x && mergedP2.x < seg->p2.x) {
										//x1...mergedX1...mergedX2...x2
										//calc point on original line
										mergedP1 = seg->p1;
										mergedP2 = seg->p2;
										mergedLenTotal = cv::norm(seg->p2 - seg->p1);
										seg->merged = true;
									}
									else if (mergedP1.x < seg->p1.x && seg->p1.x < mergedP2.x && mergedP2.x < seg->p2.x) {
										//mergedX1...x1...mergedX2...x2
										//calc point on original line
										cv::Point ptNewEnd = GetNearestPointToLine(seg->p2, mergedP1, mergedP2);
										mergedP2 = ptNewEnd;
										mergedLenTotal = cv::norm(seg->p2 - mergedP1);
										seg->merged = true;
									}
									else if (mergedP1.x < seg->p1.x && seg->p2.x < mergedP2.x) {
										//mergedX1...x1...x2...mergedX2
									}
									else if (mergedP2.x < seg->p1.x) {
										//mergedX1...mergedX2...x1...x2
										distOfLines = cv::norm(seg->p1 - mergedP2);
										//_RPT1(_CRT_WARN, "mergedX1...mergedX2...x1...x2 distOfLines=%f\n", distOfLines);
										if (distOfLines > lineIsolationThreshold) {
											//_RPT1(_CRT_WARN, "distOfLines=%f\n", distOfLines);
										}
										else {
											//calc point on original line
											cv::Point ptNewEnd = GetNearestPointToLine(seg->p2, mergedP1, mergedP2);
											mergedP2 = ptNewEnd;
											mergedLenTotal += seg->r;
											seg->merged = true;
										}
									}
								}
								else {
									if (seg->p1.x < mergedP1.x) {
										//x2...x1...mergedX1...mergedX2
										distOfLines = cv::norm(mergedP1 - seg->p1);
										if (distOfLines > lineIsolationThreshold) {
											//_RPT1(_CRT_WARN, "distOfLines=%f\n", distOfLines);
										}
										else {
											//calc point on original line
											cv::Point ptNewEnd = GetNearestPointToLine(seg->p2, mergedP1, mergedP2);
											mergedP1 = ptNewEnd;
											mergedLenTotal += seg->r;
											seg->merged = true;
										}
									}
									else if (seg->p2.x < mergedP1.x && seg->p1.x < mergedP2.x) {
										//x2...mergedX1...x1...mergedX2
										//calc point on original line
										cv::Point ptNewEnd = GetNearestPointToLine(seg->p2, mergedP1, mergedP2);
										mergedP1 = ptNewEnd;
										mergedLenTotal = cv::norm(mergedP2 - seg->p2);
										seg->merged = true;
									}
									else if (seg->p2.x < mergedP1.x && mergedP2.x < seg->p1.x) {
										//x2...mergedX1...mergedX2...x1
										mergedP1 = seg->p2;
										mergedP2 = seg->p1;
										seg->merged = true;
										mergedLenTotal = seg->r;
									}
									else if (mergedP1.x < seg->p2.x && seg->p2.x < mergedP2.x && mergedP2.x < seg->p1.x) {
										//mergedX1...x2...mergedX2...x1
										cv::Point ptNewEnd = GetNearestPointToLine(seg->p1, mergedP1, mergedP2);
										mergedP2 = ptNewEnd;
										seg->merged = true;
										mergedLenTotal = cv::norm(mergedP1 - seg->p1);
									}
									else if (mergedP1.x < seg->p2.x && seg->p1.x < mergedP2.x) {
										//mergedX1...x2...x1...mergedX2
										seg->merged = true;
									}
									else if (mergedP2.x < seg->p2.x) {
										//mergedX1...mergedX2...x2...x1
										distOfLines = cv::norm(seg->p2 - mergedP2);
										if (distOfLines > lineIsolationThreshold) {
											//_RPT1(_CRT_WARN, "distOfLines=%f\n", distOfLines);
										}
										else {
											//calc point on original line
											cv::Point ptNewEnd = GetNearestPointToLine(seg->p1, mergedP1, mergedP2);
											mergedP2 = ptNewEnd;
											seg->merged = true;
											mergedLenTotal += seg->r;
										}
									}
								}
							}
							else {
								//|x| < |y|
								double distOfLines = 0;
								if (seg->p1.y <= seg->p2.y) {
									if (seg->p2.y < mergedP1.y) {
										//y1...y2...mergedY1...mergedY2
										distOfLines = cv::norm(mergedP1 - seg->p2);
										if (distOfLines > lineIsolationThreshold) {
											//_RPT1(_CRT_WARN, "distOfLines=%f\n", distOfLines);
										}
										else {
											//calc point on original line
											cv::Point ptNewEnd = GetNearestPointToLine(seg->p1, mergedP1, mergedP2);
											mergedP1 = ptNewEnd;
											mergedLenTotal += seg->r;
											seg->merged = true;
										}
									}
									else if (seg->p1.y < mergedP1.y && seg->p2.y < mergedP2.y) {
										//y1...mergedY1...y2...mergedY2
										cv::Point ptNewEnd = GetNearestPointToLine(seg->p1, mergedP1, mergedP2);
										mergedP1 = ptNewEnd;
										mergedLenTotal = cv::norm(mergedP2 - seg->p1);
										seg->merged = true;
									}
									else if (seg->p1.y < mergedP1.y && mergedP2.y < seg->p2.y) {
										//y1...mergedY1...mergedY2...y2
										//calc point on original line
										mergedP1 = seg->p1;
										mergedP2 = seg->p2;
										mergedLenTotal = cv::norm(seg->p2 - seg->p1);
										seg->merged = true;
									}
									else if (mergedP1.y < seg->p1.y && seg->p1.y < mergedP2.y && mergedP2.y < seg->p2.y) {
										//mergedY1...y1...mergedY2...y2
										//calc point on original line
										cv::Point ptNewEnd = GetNearestPointToLine(seg->p2, mergedP1, mergedP2);
										mergedP2 = ptNewEnd;
										mergedLenTotal = cv::norm(seg->p2 - mergedP1);
										seg->merged = true;
									}
									else if (mergedP1.y < seg->p1.y && seg->p2.y < mergedP2.y) {
										//mergedY1...y1...y2...mergedY2
									}
									else if (mergedP2.y < seg->p1.y) {
										//mergedY1...mergedY2...y1...y2
										distOfLines = cv::norm(seg->p1 - mergedP2);
										if (distOfLines > lineIsolationThreshold) {
											//_RPT1(_CRT_WARN, "distOfLines=%f\n", distOfLines);
										}
										else {
											//calc point on original line
											cv::Point ptNewEnd = GetNearestPointToLine(seg->p2, mergedP1, mergedP2);
											mergedP2 = ptNewEnd;
											mergedLenTotal += seg->r;
											seg->merged = true;
										}
									}
								}
								else {
									if (seg->p1.y < mergedP1.y) {
										//y2...y1...mergedY1...mergedY2
										distOfLines = cv::norm(mergedP1 - seg->p1);
										if (distOfLines > lineIsolationThreshold) {
											//_RPT1(_CRT_WARN, "distOfLines=%f\n", distOfLines);
										}
										else {
											//calc point on original line
											cv::Point ptNewEnd = GetNearestPointToLine(seg->p2, mergedP1, mergedP2);
											mergedP1 = ptNewEnd;
											mergedLenTotal += seg->r;
											seg->merged = true;
										}
									}
									else if (seg->p2.y < mergedP1.y && seg->p1.y < mergedP2.y) {
										//y2...mergedY1...y1...mergedY2
										//calc point on original line
										cv::Point ptNewEnd = GetNearestPointToLine(seg->p2, mergedP1, mergedP2);
										mergedP1 = ptNewEnd;
										mergedLenTotal = cv::norm(mergedP2 - seg->p2);
										seg->merged = true;
									}
									else if (seg->p2.y < mergedP1.y && mergedP2.y < seg->p1.y) {
										//y2...mergedY1...mergedY2...y1
										mergedP1 = seg->p2;
										mergedP2 = seg->p1;
										seg->merged = true;
										mergedLenTotal = seg->r;
									}
									else if (mergedP1.y < seg->p2.y && seg->p2.y < mergedP2.y && mergedP2.y < seg->p1.y) {
										//mergedY1...y2...mergedY2...y1
										cv::Point ptNewEnd = GetNearestPointToLine(seg->p1, mergedP1, mergedP2);
										mergedP2 = ptNewEnd;
										seg->merged = true;
										mergedLenTotal = cv::norm(mergedP1 - seg->p1);
									}
									else if (mergedP1.y < seg->p2.y && seg->p1.y < mergedP2.y) {
										//mergedY1...y2...y1...mergedY2
									}
									else if (mergedP2.y < seg->p2.y) {
										//mergedY1...mergedY2...y2...y1
										distOfLines = cv::norm(seg->p2 - mergedP2);
										if (distOfLines > lineIsolationThreshold) {
											//_RPT1(_CRT_WARN, "distOfLines=%f\n", distOfLines);
										}
										else {
											//calc point on original line
											cv::Point ptNewEnd = GetNearestPointToLine(seg->p1, mergedP1, mergedP2);
											mergedP2 = ptNewEnd;
											seg->merged = true;
											mergedLenTotal += seg->r;
										}
									}
								}
							}
							if (seg->merged) {
								bFoundMergalbeLine = true;
								double lenMergedTemp = cv::norm(mergedP1 - mergedP2);
								//_RPT5(_CRT_WARN, "seg->idx=%d Merged line (%d, %d)-(%d, %d) ", seg->idx, mergedP1.x, mergedP1.y, mergedP2.x, mergedP2.y);
								//_RPT1(_CRT_WARN, "lenMergedTemp:%f\n", lenMergedTemp);
							}
						}
					}//for loop k (segLines)
				}while (bFoundMergalbeLine);

				LineAttr line;
				line.p1 = mergedP1;
				line.p2 = mergedP2;
				double lenMerged = cv::norm(mergedP1 - mergedP2);
				line.r = lenMerged;
				line.rh = mergedRho;
				line.t = mergedT;
				line.merged = false;
				line.connected = false;
				line.idx = -1;
				//_RPT2(_CRT_WARN, "### mergedLenTotal:%f lenMerged:%f\n", mergedLenTotal, lenMerged);
				if (mergedLenTotal / lenMerged > mergedLineDensityThreshold && lenMerged > lineThreshold) {
					//_RPT4(_CRT_WARN, "Added to linesMerged (%f,%f)-(%f,%f) ", mergedP1.x, mergedP1.y, mergedP2.x, mergedP2.y);
					//_RPT2(_CRT_WARN, "t:%f rh:%f\n", mergedT, mergedRho);
					linesMerged.push_back(line);
				}
			}
		}//for loop i

		//just for debug
		//for (size_t i = 0; i < linesMerged.size(); i++)
		//{
		//	cv::Vec4i l = linesMerged[i];
		//	line(cdstP2, cv::Point(l[0], l[1]), cv::Point(l[2], l[3]), cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
		//}

		//remove lines far away
 		for (size_t i = 0; i < linesMerged.size(); i++) {
			LineAttr l = linesMerged[i];
			cv::Point p1 = l.p1;
			cv::Point p2 = l.p2;
			//double r = cv::norm(p1 - p2);
			double r = l.r;
			//_RPT5(_CRT_WARN, "[%d] (%d,%d)-(%d,%d) r:%f\n", i, p1.x, p1.y, p2.x, p2.y, r);
			bool isolated = true;
			for (size_t j = 0; j < linesMerged.size(); j++) {
				if (i == j)
					continue;

				LineAttr ln = linesMerged[j];
				cv::Point pn1 = ln.p1;
				cv::Point pn2 = ln.p2;
				double rn = cv::norm(p1 - pn1);
				//_RPT5(_CRT_WARN, "--[%d] (%d,%d)-(%d,%d) rn:%f\n", j, p1.x, p1.y, pn1.x, pn1.y, rn);
				if (rn < lineIsolationThreshold) {
					//_RPT5(_CRT_WARN, "line %d is close to %d\n", i, j);
					isolated = false;
					break;
				}
				rn = cv::norm(p1 - pn2);
				//_RPT5(_CRT_WARN, "--[%d] (%d,%d)-(%d,%d) rn:%f\n", j, p1.x, p1.y, pn2.x, pn2.y, rn);
				if (rn < lineIsolationThreshold) {
					//_RPT5(_CRT_WARN, "line %d is close to %d\n", i, j);
					isolated = false;
					break;
				}
				rn = cv::norm(p2 - pn1);
				//_RPT5(_CRT_WARN, "--[%d] (%d,%d)-(%d,%d) rn:%f\n", j, p2.x, p2.y, pn1.x, pn1.y, rn);
				if (rn < lineIsolationThreshold) {
					//_RPT5(_CRT_WARN, "line %d is close to %d\n", i, j);
					isolated = false;
					break;
				}
				rn = cv::norm(p2 - pn2);
				//_RPT5(_CRT_WARN, "--[%d] (%d,%d)-(%d,%d) rn:%f\n", j, p2.x, p2.y, pn2.x, pn2.y, rn);
				if (rn < lineIsolationThreshold) {
					//_RPT5(_CRT_WARN, "line %d is close to %d\n", i, j);
					isolated = false;
					break;
				}
			}
			if (!isolated) {
				linesNotFarAway.push_back(l);
			}
		}

		//just for debug
		for (size_t i = 0; i < linesNotFarAway.size(); i++)
		{
			LineAttr l = linesNotFarAway[i];
			line(cdstP3, l.p1, l.p2, cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
		}

		//find neighbours
		//check if end of line connect to end of other lines 
		//bool* hasConnection = new bool[linesNotFarAway.size()];
		//memset(hasConnection, 0, sizeof(bool) * linesNotFarAway.size());
		if (linesNotFarAway.size() < 4) {
			//failed to find edge
		}
		else if (linesNotFarAway.size() <= 4) {
			//add all lines
			linesConnected = linesNotFarAway;
		}
		else {
			for (size_t i = 0; i < linesNotFarAway.size(); i++) {

				LineAttr& l = linesNotFarAway[i];
				cv::Point p1 = l.p1;
				cv::Point p2 = l.p2;
				//double r = cv::norm(p1 - p2);
				double r = l.r;
				//double s = (p1.y - p2.y) / r;
				//if (s < -1) s = -1;
				//if (s > 1) s = 1;
				//double t = asin(s);
				double t = l.t;
				//_RPT4(_CRT_WARN, "testing connection of %d r:%f s:%f t:%f\n", i, r, s, t);

				bool connectedToP1 = false;
				bool connectedToP2 = false;
				for (size_t j = i + 1; j < linesNotFarAway.size(); j++) {

					LineAttr& ln = linesNotFarAway[j];
					cv::Point pn1 = ln.p1;
					cv::Point pn2 = ln.p2;

					//check the angle between lines
					//if angle is sharp, it's not a corner.
					double rn = cv::norm(pn1 - pn2);
					double sn = (pn1.y - pn2.y) / rn;
					if (sn < -1) sn = -1;
					if (sn > 1) sn = 1;
					double tn = asin(sn);
					double dT = NAN;
					if ((t <= 0 && tn <= 0) || (t >= 0 && tn >= 0)) {
						dT = abs(t - tn);
					}
					else {
						dT = abs(t) + abs(tn);
						if (dT > CV_PI * 0.5 && dT <= CV_PI) {
							dT = CV_PI - dT;
						}
						else if (dT > CV_PI) {
							dT = dT - CV_PI;
						}
					}
					//_RPT4(_CRT_WARN, "rn:%f sn:%f tn:%f dT:%f\n", rn, sn, tn, dT);
					if (dT < PI / 4) {
						//_RPT2(_CRT_WARN, "%d and %d are NOT connected bcoz less angle\n", i, j);
						continue;
					}

					bool connected = false;
					double d = cv::norm(p1 - pn1);
					//_RPT5(_CRT_WARN, "(%d,%d)-(%d,%d):%f\n", p1.x, p1.y, pn1.x, pn1.y, d);
					if (d < connectionThreshold) {
						connected = true;
						connectedToP1 = true;
					}
					else {
						d = cv::norm(p1 - pn2);
						//_RPT5(_CRT_WARN, "(%d,%d)-(%d,%d):%f\n", p1.x, p1.y, pn2.x, pn2.y, d);
						if (d < connectionThreshold) {
							connected = true;
							connectedToP1 = true;
						}
						else {
							d = cv::norm(p2 - pn1);
							//_RPT5(_CRT_WARN, "(%d,%d)-(%d,%d):%f\n", p2.x, p2.y, pn1.x, pn1.y, d);
							if (d < connectionThreshold) {
								connected = true;
								connectedToP2 = true;
							}
							else {
								d = cv::norm(p2 - pn2);
								//_RPT5(_CRT_WARN, "(%d,%d)-(%d,%d):%f\n", p2.x, p2.y, pn2.x, pn2.y, d);
								if (d < connectionThreshold) {
									connected = true;
									connectedToP2 = true;
								}
							}
						}
					}
					if (connected) {
						//_RPT2(_CRT_WARN, "%d and %d are connected\n", i, j);
						l.connected = true;
						ln.connected = true;
					}
					else {
						//_RPT1(_CRT_WARN, "%d not connected\n", j);
					}
					if (connectedToP1 && connectedToP2)
						break;
				}
			}
			//add only lines connected to another line
			for (size_t i = 0; i < linesNotFarAway.size(); i++) {
				//if the line connected to others, add to list
				LineAttr l = linesNotFarAway[i];
				if (l.connected)
					linesConnected.push_back(l);
			}
		}

		for (size_t i = 0; i < linesConnected.size(); i++)
		{
			LineAttr l = linesConnected[i];
			line(cdstP4, l.p1, l.p2, cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
		}

		std::sort(linesConnected.begin(), linesConnected.end(), compareLineAttrByLengthDesc);

		/////// ***** peaklines

		////////////////////////////////////////
		int peakLineSize = cv::min(linesConnected.size(), 50);
		std::vector <LineAttr> peakLines;
		for (size_t i = 0; i < peakLineSize; i++)
		{
			LineAttr l = linesConnected[i];
			l.idx = i;
			peakLines.push_back(l);
		}

		//std::vector <cv::Vec4i> peakLinesSort;

		//int numLines = (int)peakLines.size();
		float theta;
		float rheo;
		std::vector<float> distanceDU;
		std::vector<float> distanceDV;

		//take top 50 only

		// Initilization to NAN (not zaro) using for find the longest distnaces
		for (size_t i = 0; i < peakLineSize; i++)
		{
			distanceDV.push_back(NAN);
			distanceDU.push_back(NAN);
		}
		// Check lines if vertical or horizantal are
		for (int i = 0; i < peakLineSize; i++)
		{
			LineAttr l = peakLines[i];
			cv::Point p1 = l.p1;
			cv::Point p2 = l.p2;
			cv::Point p0(0, 0);
			//double d0 = GetDistanceFromPointToLine(p0, l);
			//double r = cv::norm(p1 - p2);
			double r = l.r;
			//double s = (p1.y - p2.y) / r;
			//if (s < -1) s = -1;
			//if (s > 1) s = 1;
			//double t = asin(s);
			double t = l.t;
			rheo = l.rh;
			theta = t;

			if (rheo < 0)
			{
				theta = theta - PI;
				rheo = abs(rheo);
			}

			if (theta < PI / 2 - 0.5 && theta > PI / 2 - 0.5)
			{
				rheo = rheo * -1;
				theta = theta - PI / 2;
			}

			if ((abs(theta) > (PI / 4)) && (abs(theta) < (3 * PI / 4)))
			{
				distanceDU[i] = rheo;
			}
			else
			{
				distanceDV[i] = rheo;
			}
		}

		// Finding the longest distance from center consider the desired line
		//if (isnan(distanceDV[0]))
		//	distanceDV[0] = 0;
		//if (isnan(distanceDU[0]))
		//	distanceDU[0] = 0;

		int iDUMax = -1;
		int iDUMin = -1;
		int iDVMax = -1;
		int iDVMin = -1;

		float maxDU = NAN;
		float minDU = NAN;
		float maxDV = NAN;
		float minDV = NAN;

		std::vector<float> dUTemp;
		std::vector<float> dVTemp;
		for (int i = 0; i < peakLineSize; i++) {
			if (!isnan(distanceDU[i]))
				dUTemp.push_back(distanceDU[i]);
			if (!isnan(distanceDV[i]))
				dVTemp.push_back(distanceDV[i]);
		}
		/*
		std::vector<float> dUTemp;
		for (int i = 0; i < peakLineSize; i++) {
			if (!isnan(distanceDU[i]))
				dUTemp.push_back(distanceDU[i]);
		}
		std::vector<float> dVTemp;
		for (int i = 0; i < peakLineSize; i++) {
			if (!isnan(distanceDV[i]))
				dVTemp.push_back(distanceDV[i]);
		}
		*/
		maxDU = getMax(dUTemp);
		minDU = getMin(dUTemp);
		maxDV = getMax(dVTemp);
		minDV = getMin(dVTemp);

		for (int i = 0; i < peakLineSize; ++i) {
			if (maxDU == distanceDU[i]) {
				iDUMax = i;
			}
			if (minDU == distanceDU[i]) {
				iDUMin = i;
			}
			if (maxDV == distanceDV[i]) {
				iDVMax = i;
			}
			if (minDV == distanceDV[i]) {
				iDVMin = i;
			}
		}

		//double expectedHVRatio = 3.35 / 2.15;
		if (expectedHVRatio > 0) {
			cv::Mat cdstP5 = cdstP.clone();	//just for debug
			if (pickRectByHVRatio(expectedHVRatio, peakLineConnectionThreshold, peakLines, distanceDU, distanceDV, cannyImg.cols, cannyImg.rows,
				iDUMin, iDUMax, iDVMin, iDVMax, cdstP5)) 
			{
				minDU = distanceDU[iDUMin];
				maxDU = distanceDU[iDUMax];
				minDV = distanceDV[iDVMin];
				maxDV = distanceDV[iDVMax];
			}
		}


		//check if pearlines are collapsed or not
		if (minDU == maxDU) {
			minDU = NAN;
			maxDU = NAN;
			iDUMin = -1;
			iDUMax = -1;
		}
		if (minDV == maxDV) {
			minDV = NAN;
			maxDV = NAN;
			iDVMin = -1;
			iDVMax = -1;
		}

		// Determine the desired Peaklines
		bool bFoundEdges = false;
		int assumptionLine = 1;
		std::vector<cv::Vec4i> ReseveLine;
		if (iDUMin != -1 && iDUMax != -1 && iDVMin != -1 && iDVMax != -1)
		{
			bFoundEdges = true;
			//peakLinesSort.push_back(peakLines[iDUMin]);	//left edge (bottom-left to top-left)
			//peakLinesSort.push_back(peakLines[iDVMax]);	//bottom edge (bottom-right to bottom-left)
			//peakLinesSort.push_back(peakLines[iDUMax]);	//right edge (top-right to bottom-right)
			//peakLinesSort.push_back(peakLines[iDVMin]);	//top-left to top-right

			//just for debug
			//_RPT1(_CRT_WARN, "peakLinesSort.length:%d\n", peakLinesSort.size());
			//for (int i = 0; i < peakLinesSort.size(); i++)
			//{
			//	cv::Vec4i l = peakLinesSort[i];
			//	//_RPT5(_CRT_WARN, "peakLinesSort[%d](%d,%d)-(%d,%d)\n", i, l[0], l[1], l[2], l[3]);
			//}

			//cross point between the left edge (bottom-left to top-left), and bottom edge (bottom-right to bottom-left)
			cv::Point result_pLB = getCrossPoint(peakLines[iDUMin].p1, peakLines[iDUMin].p2, peakLines[iDVMax].p1, peakLines[iDVMax].p2);
			//_RPT2(_CRT_WARN, "result_pLB:%d,%d\n", result_pLB.x, result_pLB.y);
			//cross point between the bottom edge (bottom-right to bottom-left), and right edge (top-right to bottom-right)
			cv::Point result_pRB = getCrossPoint(peakLines[iDVMax].p1, peakLines[iDVMax].p2, peakLines[iDUMax].p1, peakLines[iDUMax].p2);
			//_RPT2(_CRT_WARN, "result_pRB:%d,%d\n", result_pRB.x, result_pRB.y);
			//cross point between the right edge (top-right to bottom-right), and top edge (top-left to top-right)
			cv::Point result_pRT = getCrossPoint(peakLines[iDUMax].p1, peakLines[iDUMax].p2, peakLines[iDVMin].p1, peakLines[iDVMin].p2);
			//_RPT2(_CRT_WARN, "result_pRT:%d,%d\n", result_pRT.x, result_pRT.y);
			//cross point between the top edge (top-left to top-right), and left edge (bottom-left to top-left)
			cv::Point result_pLT = getCrossPoint(peakLines[iDVMin].p1, peakLines[iDVMin].p2, peakLines[iDUMin].p1, peakLines[iDUMin].p2);
			//_RPT2(_CRT_WARN, "result_pLT:%d,%d\n", result_pLT.x, result_pLT.y);

			pt1->X = (int)((double)result_pLT.x / imgScalingRatio);
			pt1->Y = (int)((double)result_pLT.y / imgScalingRatio);
			pt2->X = (int)((double)result_pRT.x / imgScalingRatio);
			pt2->Y = (int)((double)result_pRT.y / imgScalingRatio);
			pt3->X = (int)((double)result_pRB.x / imgScalingRatio);
			pt3->Y = (int)((double)result_pRB.y / imgScalingRatio);
			pt4->X = (int)((double)result_pLB.x / imgScalingRatio);
			pt4->Y = (int)((double)result_pLB.y / imgScalingRatio);

			//just for debug
			cv::line(imgDetectedEdge, result_pLT, result_pRT, cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
			cv::line(imgDetectedEdge, result_pRT, result_pRB, cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
			cv::line(imgDetectedEdge, result_pRB, result_pLB, cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
			cv::line(imgDetectedEdge, result_pLB, result_pLT, cv::Scalar(0, 255, 0), 3, cv::LINE_AA);

			//_RPT2(_CRT_WARN, "DetectEdge pt1:(%d,%d)\n", pt1->X, pt1->Y);
			//_RPT2(_CRT_WARN, "DetectEdge pt2:(%d,%d)\n", pt2->X, pt2->Y);
			//_RPT2(_CRT_WARN, "DetectEdge pt3:(%d,%d)\n", pt3->X, pt3->Y);
			//_RPT2(_CRT_WARN, "DetectEdge pt4:(%d,%d)\n", pt4->X, pt4->Y);

		}
		return bFoundEdges;
	}

	bool ImgProcUtil::DetectEdge4FromBitmap(System::Drawing::Bitmap^ srcBmp, System::Collections::Generic::List<System::Drawing::Point>^% points, double expectedHVRatio)
	{
		System::IO::MemoryStream^ ms = gcnew System::IO::MemoryStream();
		srcBmp->Save(ms, System::Drawing::Imaging::ImageFormat::Png);
		array<System::Byte>^ imageSrc = ms->GetBuffer();
		return DetectEdge4(imageSrc, points, expectedHVRatio);
	}
	bool ImgProcUtil::DetectEdge4(array<System::Byte>^ imageSrc, System::Collections::Generic::List<System::Drawing::Point>^% points)
	{
		double expectedHVRatio = 3.35 / 2.15;
		return DetectEdge4(imageSrc, points, expectedHVRatio);
	}
	bool ImgProcUtil::DetectEdge4(array<System::Byte>^ imageSrc, System::Collections::Generic::List<System::Drawing::Point>^% points, double expectedHVRatio)
	{
		int outWidth;
		int outHeight;

		cv::Mat srcImg;
		pin_ptr<Byte> p = &imageSrc[0];   // entire array is now pinned
		unsigned char* cp = p;
		int docImageLen = imageSrc->Length;
		srcImg = loadImageFromByteArray(cp, docImageLen);
		System::Drawing::Point^ pt1 = gcnew System::Drawing::Point();
		System::Drawing::Point^ pt2 = gcnew System::Drawing::Point();
		System::Drawing::Point^ pt3 = gcnew System::Drawing::Point();
		System::Drawing::Point^ pt4 = gcnew System::Drawing::Point();
		if (detectEdge4(srcImg, pt1, pt2, pt3, pt4, expectedHVRatio))
		{
			points->Clear();
			points->Add(*pt1);
			points->Add(*pt2);
			points->Add(*pt3);
			points->Add(*pt4);
			return true;
		}
		return false;
	}
#endif
/*
class CropLayer(object):
	def __init__(self, params, blobs):
		# initialize our starting and ending (x, y)-coordinates of
		# the crop
		self.startX = 0
		self.startY = 0
		self.endX = 0
		self.endY = 0

	def getMemoryShapes(self, inputs):
		# the crop layer will receive two inputs -- we need to crop
		# the first input blob to match the shape of the second one,
		# keeping the batch size and number of channels
		print("getMemoryShapes inputs:", inputs)
		(inputShape, targetShape) = (inputs[0], inputs[1])
		(batchSize, numChannels) = (inputShape[0], inputShape[1])
		(H, W) = (targetShape[2], targetShape[3])

		# compute the starting and ending crop coordinates
		self.startX = int((inputShape[3] - targetShape[3]) / 2)
		self.startY = int((inputShape[2] - targetShape[2]) / 2)
		self.endX = self.startX + W
		self.endY = self.startY + H

		# return the shape of the volume (we'll perform the actual
		# crop during the forward pass
		return [[batchSize, numChannels, H, W]]

	def forward(self, inputs):
		# use the derived (x, y)-coordinates to perform the crop
		return [inputs[0][:, :, self.startY:self.endY,
				self.startX:self.endX]]
*/
	//https://berak.github.io/smallfry/dnn_edge.html
	CropLayer::CropLayer(const cv::dnn::LayerParams& params) : Layer(params) {
	}
	cv::Ptr<cv::dnn::Layer> CropLayer::create(cv::dnn::LayerParams& params) {
		return new CropLayer(params);
	}
	
	bool CropLayer::getMemoryShapes(const std::vector<std::vector<int> >& inputs,
		const int requiredOutputs,
		std::vector<std::vector<int> >& outputs,
		std::vector<std::vector<int> >& internals) const {

		CV_UNUSED(requiredOutputs); 
		CV_UNUSED(internals);

		int batchSize = inputs[0][0];
		int numChannels = inputs[0][1];
		int H = inputs[1][2];
		int W = inputs[1][3];
		std::vector<int> outShape(4);
		outShape[0] = batchSize;  // batch size
		outShape[1] = numChannels;  // number of channels
		outShape[2] = H;
		outShape[3] = W;
		printf("batchSize: %d numChannels:%d H:%d W:%d\n", batchSize, numChannels, H, W);
		outputs.assign(1, outShape);
		return false;
	}
	
	void CropLayer::forward(std::vector<cv::Mat*>& input, std::vector<cv::Mat>& output, std::vector<cv::Mat>& internals) {
		CV_UNUSED(internals);

		cv::Mat& inp = *input[0];
		cv::Mat& out = output[0];

		int ystart = (inp.size[2] - out.size[2]) / 2;
		int xstart = (inp.size[3] - out.size[3]) / 2;
		int yend = ystart + out.size[2];
		int xend = xstart + out.size[3];

		const int batchSize = inp.size[0];
		const int numChannels = inp.size[1];
		const int height = out.size[2];
		const int width = out.size[3];

//		return [inputs[0][:, :, self.startY:self.endY, self.startX:self.endX]]
		printf("batchSize: %d numChannels:%d height:%d width:%d xstart:%d ystart:%d xend:%d yend:%d\n", batchSize, numChannels, height, width, xstart, ystart, xend, yend);

		int sz[] = { (int)batchSize, numChannels, height, width };
		
		out.create(4, sz, CV_32F);
		for (int i = 0; i < batchSize; i++)
		{
			for (int j = 0; j < numChannels; j++)
			{
				cv::Mat plane(inp.size[2], inp.size[3], CV_32F, inp.ptr<float>(i, j));
				cv::Mat crop = plane(cv::Range(ystart, yend), cv::Range(xstart, xend));
				cv::Mat targ(height, width, CV_32F, out.ptr<float>(i, j));
				crop.copyTo(targ);
			}
		}
	}

	void CropLayer::forward(cv::InputArrayOfArrays inputs_arr,
		cv::OutputArrayOfArrays outputs_arr,
		cv::OutputArrayOfArrays internals_arr) {
		CV_UNUSED(internals_arr);

		std::vector<cv::Mat> inputs, outputs, internals;
		inputs_arr.getMatVector(inputs);
		outputs_arr.getMatVector(outputs);
		//internals_arr.getMatVector(internals);

		forward(inputs, outputs, internals);
	}
	

	//cv::dnn::Net caffeNetHED = cv::dnn::readNetFromCaffe("C:\\work\\CS\\OCR\\HED\\holistically-nested-edge-detection\\hed_model\\deploy.prototxt",
	//	"C:\\work\\CS\\OCR\\HED\\holistically-nested-edge-detection\\hed_model\\hed_pretrained_bsds.caffemodel");
#ifdef USE_SYSTEM_DRAWING
	bool detectEdgeHED(cv::Mat orgImg, System::Drawing::Point^% pt1, System::Drawing::Point^% pt2, System::Drawing::Point^% pt3, System::Drawing::Point^% pt4, double expectedHVRatio) {
		cv::Mat resizedImg; // = imageCV.clone();
		//--------------------
		//1 Resizing image
		//--------------------
		cv::Size s;;
		double imgHeight = orgImg.rows;
		double imgWidth = orgImg.cols;
		double imgHVRatio = imgHeight / imgWidth;
		if (imgHVRatio > 1) {
			if (imgHeight > 335 * 2) {
				imgHeight = 335 * 2;
				imgWidth = imgHeight / imgHVRatio;
			}
			s = cv::Size(imgWidth, imgHeight);
		}
		else {
			if (imgWidth > 335 * 2) {
				imgWidth = 335 * 2;
				imgHeight = imgWidth * imgHVRatio;
			}
			s = cv::Size(imgWidth, imgHeight);
		}
		double imgScalingRatio = imgHeight / orgImg.rows;
		resize(orgImg, resizedImg, s, cv::INTER_CUBIC); //CV_INTER_CUBIC |  CV_INTER_LINEAR

		//CV_DNN_REGISTER_LAYER_CLASS(Crop, CropLayer);
		DWORD dwSt = GetTickCount(); 
		cv::dnn::Net caffeNetHED = cv::dnn::readNetFromCaffe("C:\\work\\CS\\OCR\\HED\\holistically-nested-edge-detection\\hed_model\\deploy.prototxt",
			"C:\\work\\CS\\OCR\\HED\\holistically-nested-edge-detection\\hed_model\\hed_pretrained_bsds.caffemodel");
		DWORD dwEn = GetTickCount();
		printf("readNetFromCaffe %d\n", dwEn - dwSt);
		cv::dnn::LayerFactory::registerLayer("Crop", cv::dnn::details::_layerDynamicRegisterer<CropLayer>);

		cv::Scalar mean = cv::Scalar(104.00698793, 116.66876762, 122.67891434);
		
		dwSt = GetTickCount();
		cv::Mat blob = cv::dnn::blobFromImage(resizedImg, 1.0, s, mean, false, false);
		dwEn = GetTickCount();
		printf("blobFromImage %d\n", dwEn - dwSt);

		dwSt = GetTickCount();
		caffeNetHED.setInput(blob);
		dwEn = GetTickCount();
		printf("setInput %d\n", dwEn - dwSt);

		dwSt = GetTickCount();
		cv::Mat hed = caffeNetHED.forward();
		dwEn = GetTickCount();
		printf("forward %d\n", dwEn - dwSt);
		cv::dnn::LayerFactory::unregisterLayer("Crop");

		cv::Mat hed_resized;
		cv::resize(hed.reshape(1,s.height), hed_resized, s);
		//cv::resize(hed.reshape(0, 0), hed_resized, s);
		hed_resized.convertTo(hed_resized, CV_8UC1, 255);
		cv::Mat hed_threshold;
		double minVal, maxVal;
		cv::minMaxLoc(hed_resized, &minVal, &maxVal);
		//cv::inRange(hed_resized, cv::Scalar(0.4), cv::Scalar(1), hed_threshold);
		cv::inRange(hed_resized, cv::Scalar(100), cv::Scalar(255), hed_threshold);

		//----------------------------------------
		//5 Extract points of Images
		//----------------------------------------
		std::vector<cv::Vec3f> linesOut;
		std::vector<cv::Vec4i> linesOutP;
		std::vector<LineAttr> linesMerged;
		std::vector<LineAttr> linesNotFarAway;
		std::vector<LineAttr> linesConnected;
		int sizeMin = cv::min(hed_resized.cols, hed_resized.rows);
		int thresholdHoughLines = 30;
		double houghLinesLineLenMin = sizeMin / 5;
		double houghLinesLineGapMax = 2;
		int connectionThreshold = sizeMin / 8;
		double peakLineConnectionThreshold = sizeMin / 4.0;
		int lineThreshold = sizeMin / 10;
		int lineSegThreshold = cv::max(sizeMin / 200, 2);
		int lineIsolationThreshold = sizeMin / 4;
		double deltaThetaThreshold = 0.025;
		double deltaRhoThreshold = sizeMin / 50.0;
		double mergedLineDensityThreshold = 0.5;

		//_RPT3(_CRT_WARN, "sizeMin:%d thresholdHoughLines:%d houghLinesLineLenMin:%f houghLinesLineGapMax:%f\n", sizeMin, thresholdHoughLines, houghLinesLineLenMin, houghLinesLineGapMax);
		//_RPT3(_CRT_WARN, "connectionThreshold:%d lineThreshold:%d lineSegThreshold:%d\n", connectionThreshold, lineThreshold, lineSegThreshold);
		//_RPT1(_CRT_WARN, "lineIsolationThreshold:%d peakLineConnectionThreshold:%f\n", lineIsolationThreshold, peakLineConnectionThreshold);
		//_RPT2(_CRT_WARN, "deltaRhoThreshold:%f deltaThetaThreshold: %f mergedLineDensityThreshold:%f\n", deltaRhoThreshold, deltaThetaThreshold, mergedLineDensityThreshold);

		// Copy edges to the images that will display the results in BGR
		cv::Mat cdstP;	//just for debug
		cvtColor(hed_threshold, cdstP, cv::COLOR_GRAY2BGR);
		cv::Mat cdstP1 = cdstP.clone();	//just for debug
		cv::Mat cdstP2 = cdstP.clone();	//just for debug
		cv::Mat cdstP3 = cdstP.clone();	//just for debug
		cv::Mat cdstP4 = cdstP.clone();	//just for debug
		cv::Mat imgDetectedEdge = cdstP.clone();	//just for debug

		//----------------------------------------
		//5-1. Get lines by HoughLinesP
		//----------------------------------------
		// Probabilistic Line Transform
		HoughLinesP(hed_threshold, linesOutP, 1, CV_PI / 180.0, thresholdHoughLines, houghLinesLineLenMin, houghLinesLineGapMax);
		// Draw the lines
		//int sizeLinesP = linesOutP.size();
		std::vector<LineAttr> lineAttrs;

		for (size_t i = 0; i < linesOutP.size(); i++)
		{
			LineAttr lineAttr;
			cv::Vec4i l = linesOutP[i];
			lineAttr.p1 = cv::Point(l[0], l[1]);
			lineAttr.p2 = cv::Point(l[2], l[3]);
			cv::line(cdstP, lineAttr.p1, lineAttr.p2, cv::Scalar(0, 0, 255), 3, cv::LINE_AA);	//just for debug
			lineAttr.r = cv::norm(lineAttr.p1 - lineAttr.p2);
			if (lineAttr.r > lineSegThreshold) {
				cv::line(cdstP1, lineAttr.p1, lineAttr.p2, cv::Scalar(0, 0, 255), 3, cv::LINE_AA);	//just for debug
				double s = (double)(lineAttr.p1.y - lineAttr.p2.y) / lineAttr.r;
				if (s < -1) s = -1;
				if (s > 1) s = 1;
				lineAttr.t = asin(s);
				lineAttr.rh = GetDistanceFromPointToLine(cv::Point(0, 0), lineAttr);
				lineAttr.merged = false;
				lineAttrs.push_back(lineAttr);
			}
		}

		//sort by line length
		std::sort(lineAttrs.begin(), lineAttrs.end(), compareLineAttrByLengthDesc);
		for (size_t i = 0; i < lineAttrs.size(); i++) {
			lineAttrs[i].idx = i;
		}

#if true
		//----------------------------------------
		//5-2. Merge lines
		//----------------------------------------
		for (size_t i = 0; i < lineAttrs.size(); i++)
		{
			//double lenLine1 = cv::norm(cv::Point(x1[i], y1[i]) - cv::Point(x2[i], y2[i]));
			double lenLine1 = lineAttrs[i].r;
			//_RPT4(_CRT_WARN, "lineAttrs[%d] merged:%d lenLine1:%f t:%f ", i, lineAttrs[i].merged, lenLine1, lineAttrs[i].t);
			//_RPT4(_CRT_WARN, "(%d,%d)-(%d,%d) ", lineAttrs[i].p1.x, lineAttrs[i].p1.y, lineAttrs[i].p2.x, lineAttrs[i].p2.y);
			//_RPT1(_CRT_WARN, "Math.abs(t):%f\n", abs(lineAttrs[i].t));

			if (!lineAttrs[i].merged
				&& ((abs(lineAttrs[i].t) > CV_PI * 0.875f
					|| (abs(lineAttrs[i].t) < CV_PI * 0.625f && abs(lineAttrs[i].t) > CV_PI * 0.375f)
					|| abs(lineAttrs[i].t) < CV_PI * 0.125f))
				) {

				double mergedT = lineAttrs[i].t;
				cv::Point mergedP1(0, 0);
				cv::Point mergedP2(0, 0);
				double mergedLenTotal = lenLine1;
				double mergedRho = lineAttrs[i].rh;

				//if (-1 < mergedA && mergedA < 1) {
				if (CV_PI * 0.25f > abs(mergedT)) {
					if (lineAttrs[i].p1.x <= lineAttrs[i].p2.x) {
						mergedP1.x = lineAttrs[i].p1.x;
						mergedP1.y = lineAttrs[i].p1.y;
						mergedP2.x = lineAttrs[i].p2.x;
						mergedP2.y = lineAttrs[i].p2.y;
					}
					else {
						mergedP1.x = lineAttrs[i].p2.x;
						mergedP1.y = lineAttrs[i].p2.y;
						mergedP2.x = lineAttrs[i].p1.x;
						mergedP2.y = lineAttrs[i].p1.y;
					}
				}
				else {
					if (lineAttrs[i].p1.y <= lineAttrs[i].p2.y) {
						mergedP1.y = lineAttrs[i].p1.y;
						mergedP1.x = lineAttrs[i].p1.x;
						mergedP2.y = lineAttrs[i].p2.y;
						mergedP2.x = lineAttrs[i].p2.x;
					}
					else {
						mergedP1.y = lineAttrs[i].p2.y;
						mergedP1.x = lineAttrs[i].p2.x;
						mergedP2.y = lineAttrs[i].p1.y;
						mergedP2.x = lineAttrs[i].p1.x;
					}
				}

				//find lines seems to be on the same line
				//_RPT5(_CRT_WARN, "Finding lines to merge with %d mergedT:%f mergedRho:%f ", i, mergedT, mergedRho);
				//_RPT4(_CRT_WARN, "lenLine1:%f t:%f ", lenLine1, lineAttrs[i].t);
				//_RPT4(_CRT_WARN, "(%d,%d)-(%d,%d)\n", lineAttrs[i].p1.x, lineAttrs[i].p1.y, lineAttrs[i].p2.x, lineAttrs[i].p2.y);
				std::vector<LineAttr*> segLines;
				for (size_t j = i + 1; j < lineAttrs.size(); j++) {
					double lenLine2 = lineAttrs[j].r;
					if (!lineAttrs[j].merged && lenLine2 > lineSegThreshold) {
						double dT = NAN;
						if ((mergedT <= 0.0 && lineAttrs[j].t <= 0.0) || (mergedT >= 0.0 && lineAttrs[j].t >= 0.0)) {
							dT = abs(mergedT - lineAttrs[j].t);
						}
						else {
							dT = abs(mergedT) + abs(lineAttrs[j].t);
							if (dT > CV_PI * 0.5 && dT <= CV_PI) {
								dT = CV_PI - dT;
							}
							else if (dT > CV_PI) {
								dT = dT - CV_PI;
							}
						}
						double dRho = abs(mergedRho - lineAttrs[j].rh);
						double dTRate = dT / deltaThetaThreshold;
						double dRhoRate = dRho / deltaRhoThreshold;
						if (dTRate <= 1.0 && dRhoRate <= 1.0) {
							//_RPT4(_CRT_WARN, "lineAttrs[%d] lenLine1:%f t:%f ", j, lenLine2, lineAttrs[j].t);
							//_RPT4(_CRT_WARN, "(%d,%d)-(%d,%d) ", lineAttrs[j].p1.x, lineAttrs[j].p1.y, lineAttrs[j].p2.x, lineAttrs[j].p2.y);
							//_RPT3(_CRT_WARN, "dRho:%f dTRate:%f dRhoRate:%f\n", dRho, dTRate, dRhoRate);
							//_RPT0(_CRT_WARN, "-->Add to seglines\n");
							segLines.push_back(&lineAttrs[j]);
						}
					}
				}
				//_RPT1(_CRT_WARN, "segLines.size():%d\n", segLines.size());

				// sort 
				if (CV_PI * 0.25 > abs(mergedT)) {
					//|x| > |y|
					std::sort(segLines.begin(), segLines.end(), compareLineAttrByMinXDesc);
				}
				else {
					//|x| < |y|
					std::sort(segLines.begin(), segLines.end(), compareLineAttrByMinYDesc);
				}

				// merge
				bool bFoundMergalbeLine = false;
				do {
					bFoundMergalbeLine = false;
					for (size_t k = 0; k < segLines.size(); k++) {
						//_RPT5(_CRT_WARN, "[%d] mergetP1(%d,%d) mergedP2(%d,%d)\n", i, mergedP1.x, mergedP1.y, mergedP2.x, mergedP2.y);
						LineAttr* seg = segLines[k];
						//_RPT5(_CRT_WARN, "seg->idx:%d seg->p1(%d,%d) seg->p2(%d,%d)\n", seg->idx, seg->p1.x, seg->p1.y, seg->p2.x, seg->p2.y);
						if (!seg->merged) {
							if (CV_PI * 0.25f > abs(mergedT)) {
								//|x| > |y|
								double distOfLines = 0;
								if (seg->p1.x <= seg->p2.x) {
									if (seg->p2.x < mergedP1.x) {
										//x1...x2...mergedX1...mergedX2
										distOfLines = cv::norm(mergedP1 - seg->p2);
										//_RPT1(_CRT_WARN, "x1...x2...mergedX1...mergedX2 distOfLines=%f\n", distOfLines);
										if (distOfLines > lineIsolationThreshold) {
											//_RPT1(_CRT_WARN, "distOfLines=%f\n", distOfLines);
										}
										else {
											//calc point on original line
											cv::Point ptNewEnd = GetNearestPointToLine(seg->p1, mergedP1, mergedP2);
											mergedP1.x = ptNewEnd.x;
											mergedP1.y = ptNewEnd.y;
											mergedLenTotal += seg->r;
											seg->merged = true;
										}
									}
									else if (seg->p1.x < mergedP1.x && seg->p2.x < mergedP2.x) {
										//x1...mergedX1...x2...mergedX2
										//calc point on original line
										cv::Point ptNewEnd = GetNearestPointToLine(seg->p1, mergedP1, mergedP2);
										mergedP1.x = ptNewEnd.x;
										mergedP1.y = ptNewEnd.y;
										mergedLenTotal = cv::norm(mergedP2 - seg->p1);
										seg->merged = true;
									}
									else if (seg->p1.x < mergedP1.x && mergedP2.x < seg->p2.x) {
										//x1...mergedX1...mergedX2...x2
										//calc point on original line
										mergedP1 = seg->p1;
										mergedP2 = seg->p2;
										mergedLenTotal = cv::norm(seg->p2 - seg->p1);
										seg->merged = true;
									}
									else if (mergedP1.x < seg->p1.x && seg->p1.x < mergedP2.x && mergedP2.x < seg->p2.x) {
										//mergedX1...x1...mergedX2...x2
										//calc point on original line
										cv::Point ptNewEnd = GetNearestPointToLine(seg->p2, mergedP1, mergedP2);
										mergedP2 = ptNewEnd;
										mergedLenTotal = cv::norm(seg->p2 - mergedP1);
										seg->merged = true;
									}
									else if (mergedP1.x < seg->p1.x && seg->p2.x < mergedP2.x) {
										//mergedX1...x1...x2...mergedX2
									}
									else if (mergedP2.x < seg->p1.x) {
										//mergedX1...mergedX2...x1...x2
										distOfLines = cv::norm(seg->p1 - mergedP2);
										//_RPT1(_CRT_WARN, "mergedX1...mergedX2...x1...x2 distOfLines=%f\n", distOfLines);
										if (distOfLines > lineIsolationThreshold) {
											//_RPT1(_CRT_WARN, "distOfLines=%f\n", distOfLines);
										}
										else {
											//calc point on original line
											cv::Point ptNewEnd = GetNearestPointToLine(seg->p2, mergedP1, mergedP2);
											mergedP2 = ptNewEnd;
											mergedLenTotal += seg->r;
											seg->merged = true;
										}
									}
								}
								else {
									if (seg->p1.x < mergedP1.x) {
										//x2...x1...mergedX1...mergedX2
										distOfLines = cv::norm(mergedP1 - seg->p1);
										if (distOfLines > lineIsolationThreshold) {
											//_RPT1(_CRT_WARN, "distOfLines=%f\n", distOfLines);
										}
										else {
											//calc point on original line
											cv::Point ptNewEnd = GetNearestPointToLine(seg->p2, mergedP1, mergedP2);
											mergedP1 = ptNewEnd;
											mergedLenTotal += seg->r;
											seg->merged = true;
										}
									}
									else if (seg->p2.x < mergedP1.x && seg->p1.x < mergedP2.x) {
										//x2...mergedX1...x1...mergedX2
										//calc point on original line
										cv::Point ptNewEnd = GetNearestPointToLine(seg->p2, mergedP1, mergedP2);
										mergedP1 = ptNewEnd;
										mergedLenTotal = cv::norm(mergedP2 - seg->p2);
										seg->merged = true;
									}
									else if (seg->p2.x < mergedP1.x && mergedP2.x < seg->p1.x) {
										//x2...mergedX1...mergedX2...x1
										mergedP1 = seg->p2;
										mergedP2 = seg->p1;
										seg->merged = true;
										mergedLenTotal = seg->r;
									}
									else if (mergedP1.x < seg->p2.x && seg->p2.x < mergedP2.x && mergedP2.x < seg->p1.x) {
										//mergedX1...x2...mergedX2...x1
										cv::Point ptNewEnd = GetNearestPointToLine(seg->p1, mergedP1, mergedP2);
										mergedP2 = ptNewEnd;
										seg->merged = true;
										mergedLenTotal = cv::norm(mergedP1 - seg->p1);
									}
									else if (mergedP1.x < seg->p2.x && seg->p1.x < mergedP2.x) {
										//mergedX1...x2...x1...mergedX2
										seg->merged = true;
									}
									else if (mergedP2.x < seg->p2.x) {
										//mergedX1...mergedX2...x2...x1
										distOfLines = cv::norm(seg->p2 - mergedP2);
										if (distOfLines > lineIsolationThreshold) {
											//_RPT1(_CRT_WARN, "distOfLines=%f\n", distOfLines);
										}
										else {
											//calc point on original line
											cv::Point ptNewEnd = GetNearestPointToLine(seg->p1, mergedP1, mergedP2);
											mergedP2 = ptNewEnd;
											seg->merged = true;
											mergedLenTotal += seg->r;
										}
									}
								}
							}
							else {
								//|x| < |y|
								double distOfLines = 0;
								if (seg->p1.y <= seg->p2.y) {
									if (seg->p2.y < mergedP1.y) {
										//y1...y2...mergedY1...mergedY2
										distOfLines = cv::norm(mergedP1 - seg->p2);
										if (distOfLines > lineIsolationThreshold) {
											//_RPT1(_CRT_WARN, "distOfLines=%f\n", distOfLines);
										}
										else {
											//calc point on original line
											cv::Point ptNewEnd = GetNearestPointToLine(seg->p1, mergedP1, mergedP2);
											mergedP1 = ptNewEnd;
											mergedLenTotal += seg->r;
											seg->merged = true;
										}
									}
									else if (seg->p1.y < mergedP1.y && seg->p2.y < mergedP2.y) {
										//y1...mergedY1...y2...mergedY2
										cv::Point ptNewEnd = GetNearestPointToLine(seg->p1, mergedP1, mergedP2);
										mergedP1 = ptNewEnd;
										mergedLenTotal = cv::norm(mergedP2 - seg->p1);
										seg->merged = true;
									}
									else if (seg->p1.y < mergedP1.y && mergedP2.y < seg->p2.y) {
										//y1...mergedY1...mergedY2...y2
										//calc point on original line
										mergedP1 = seg->p1;
										mergedP2 = seg->p2;
										mergedLenTotal = cv::norm(seg->p2 - seg->p1);
										seg->merged = true;
									}
									else if (mergedP1.y < seg->p1.y && seg->p1.y < mergedP2.y && mergedP2.y < seg->p2.y) {
										//mergedY1...y1...mergedY2...y2
										//calc point on original line
										cv::Point ptNewEnd = GetNearestPointToLine(seg->p2, mergedP1, mergedP2);
										mergedP2 = ptNewEnd;
										mergedLenTotal = cv::norm(seg->p2 - mergedP1);
										seg->merged = true;
									}
									else if (mergedP1.y < seg->p1.y && seg->p2.y < mergedP2.y) {
										//mergedY1...y1...y2...mergedY2
									}
									else if (mergedP2.y < seg->p1.y) {
										//mergedY1...mergedY2...y1...y2
										distOfLines = cv::norm(seg->p1 - mergedP2);
										if (distOfLines > lineIsolationThreshold) {
											//_RPT1(_CRT_WARN, "distOfLines=%f\n", distOfLines);
										}
										else {
											//calc point on original line
											cv::Point ptNewEnd = GetNearestPointToLine(seg->p2, mergedP1, mergedP2);
											mergedP2 = ptNewEnd;
											mergedLenTotal += seg->r;
											seg->merged = true;
										}
									}
								}
								else {
									if (seg->p1.y < mergedP1.y) {
										//y2...y1...mergedY1...mergedY2
										distOfLines = cv::norm(mergedP1 - seg->p1);
										if (distOfLines > lineIsolationThreshold) {
											//_RPT1(_CRT_WARN, "distOfLines=%f\n", distOfLines);
										}
										else {
											//calc point on original line
											cv::Point ptNewEnd = GetNearestPointToLine(seg->p2, mergedP1, mergedP2);
											mergedP1 = ptNewEnd;
											mergedLenTotal += seg->r;
											seg->merged = true;
										}
									}
									else if (seg->p2.y < mergedP1.y && seg->p1.y < mergedP2.y) {
										//y2...mergedY1...y1...mergedY2
										//calc point on original line
										cv::Point ptNewEnd = GetNearestPointToLine(seg->p2, mergedP1, mergedP2);
										mergedP1 = ptNewEnd;
										mergedLenTotal = cv::norm(mergedP2 - seg->p2);
										seg->merged = true;
									}
									else if (seg->p2.y < mergedP1.y && mergedP2.y < seg->p1.y) {
										//y2...mergedY1...mergedY2...y1
										mergedP1 = seg->p2;
										mergedP2 = seg->p1;
										seg->merged = true;
										mergedLenTotal = seg->r;
									}
									else if (mergedP1.y < seg->p2.y && seg->p2.y < mergedP2.y && mergedP2.y < seg->p1.y) {
										//mergedY1...y2...mergedY2...y1
										cv::Point ptNewEnd = GetNearestPointToLine(seg->p1, mergedP1, mergedP2);
										mergedP2 = ptNewEnd;
										seg->merged = true;
										mergedLenTotal = cv::norm(mergedP1 - seg->p1);
									}
									else if (mergedP1.y < seg->p2.y && seg->p1.y < mergedP2.y) {
										//mergedY1...y2...y1...mergedY2
									}
									else if (mergedP2.y < seg->p2.y) {
										//mergedY1...mergedY2...y2...y1
										distOfLines = cv::norm(seg->p2 - mergedP2);
										if (distOfLines > lineIsolationThreshold) {
											//_RPT1(_CRT_WARN, "distOfLines=%f\n", distOfLines);
										}
										else {
											//calc point on original line
											cv::Point ptNewEnd = GetNearestPointToLine(seg->p1, mergedP1, mergedP2);
											mergedP2 = ptNewEnd;
											seg->merged = true;
											mergedLenTotal += seg->r;
										}
									}
								}
							}
							if (seg->merged) {
								bFoundMergalbeLine = true;
								double lenMergedTemp = cv::norm(mergedP1 - mergedP2);
								//_RPT5(_CRT_WARN, "seg->idx=%d Merged line (%d, %d)-(%d, %d) ", seg->idx, mergedP1.x, mergedP1.y, mergedP2.x, mergedP2.y);
								//_RPT1(_CRT_WARN, "lenMergedTemp:%f\n", lenMergedTemp);
							}
						}
					}//for loop k (segLines)
				} while (bFoundMergalbeLine);

				LineAttr line;
				line.p1 = mergedP1;
				line.p2 = mergedP2;
				double lenMerged = cv::norm(mergedP1 - mergedP2);
				line.r = lenMerged;
				line.rh = mergedRho;
				line.t = mergedT;
				line.merged = false;
				line.connected = false;
				line.idx = -1;
				//_RPT2(_CRT_WARN, "### mergedLenTotal:%f lenMerged:%f\n", mergedLenTotal, lenMerged);
				if (mergedLenTotal / lenMerged > mergedLineDensityThreshold && lenMerged > lineThreshold) {
					//_RPT4(_CRT_WARN, "Added to linesMerged (%f,%f)-(%f,%f) ", mergedP1.x, mergedP1.y, mergedP2.x, mergedP2.y);
					//_RPT2(_CRT_WARN, "t:%f rh:%f\n", mergedT, mergedRho);
					linesMerged.push_back(line);
				}
			}
		}//for loop i

		//just for debug
#ifdef _DEBUG		
		for (size_t i = 0; i < linesMerged.size(); i++)
		{
			LineAttr l = linesMerged[i];
			line(cdstP2, l.p1, l.p2, cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
		}
#endif
#else
		linesMerged = lineAttrs;
#endif
		//remove lines far away
		for (size_t i = 0; i < linesMerged.size(); i++) {
			LineAttr l = linesMerged[i];
			cv::Point p1 = l.p1;
			cv::Point p2 = l.p2;
			//double r = cv::norm(p1 - p2);
			double r = l.r;
			//_RPT5(_CRT_WARN, "[%d] (%d,%d)-(%d,%d) r:%f\n", i, p1.x, p1.y, p2.x, p2.y, r);
			bool isolated = true;
			for (size_t j = 0; j < linesMerged.size(); j++) {
				if (i == j)
					continue;

				LineAttr ln = linesMerged[j];
				cv::Point pn1 = ln.p1;
				cv::Point pn2 = ln.p2;
				double rn = cv::norm(p1 - pn1);
				//_RPT5(_CRT_WARN, "--[%d] (%d,%d)-(%d,%d) rn:%f\n", j, p1.x, p1.y, pn1.x, pn1.y, rn);
				if (rn < lineIsolationThreshold) {
					//_RPT5(_CRT_WARN, "line %d is close to %d\n", i, j);
					isolated = false;
					break;
				}
				rn = cv::norm(p1 - pn2);
				//_RPT5(_CRT_WARN, "--[%d] (%d,%d)-(%d,%d) rn:%f\n", j, p1.x, p1.y, pn2.x, pn2.y, rn);
				if (rn < lineIsolationThreshold) {
					//_RPT5(_CRT_WARN, "line %d is close to %d\n", i, j);
					isolated = false;
					break;
				}
				rn = cv::norm(p2 - pn1);
				//_RPT5(_CRT_WARN, "--[%d] (%d,%d)-(%d,%d) rn:%f\n", j, p2.x, p2.y, pn1.x, pn1.y, rn);
				if (rn < lineIsolationThreshold) {
					//_RPT5(_CRT_WARN, "line %d is close to %d\n", i, j);
					isolated = false;
					break;
				}
				rn = cv::norm(p2 - pn2);
				//_RPT5(_CRT_WARN, "--[%d] (%d,%d)-(%d,%d) rn:%f\n", j, p2.x, p2.y, pn2.x, pn2.y, rn);
				if (rn < lineIsolationThreshold) {
					//_RPT5(_CRT_WARN, "line %d is close to %d\n", i, j);
					isolated = false;
					break;
				}
			}
			if (!isolated) {
				linesNotFarAway.push_back(l);
			}
		}

		//just for debug
		for (size_t i = 0; i < linesNotFarAway.size(); i++)
		{
			LineAttr l = linesNotFarAway[i];
			cv::line(cdstP3, l.p1, l.p2, cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
		}

		//find neighbours
		//check if end of line connect to end of other lines 
		//bool* hasConnection = new bool[linesNotFarAway.size()];
		//memset(hasConnection, 0, sizeof(bool) * linesNotFarAway.size());
		if (linesNotFarAway.size() < 4) {
			//failed to find edge
		}
		else if (linesNotFarAway.size() <= 4) {
			//add all lines
			linesConnected = linesNotFarAway;
		}
		else {
			for (size_t i = 0; i < linesNotFarAway.size(); i++) {

				LineAttr& l = linesNotFarAway[i];
				cv::Point p1 = l.p1;
				cv::Point p2 = l.p2;
				//double r = cv::norm(p1 - p2);
				double r = l.r;
				//double s = (p1.y - p2.y) / r;
				//if (s < -1) s = -1;
				//if (s > 1) s = 1;
				//double t = asin(s);
				double t = l.t;
				//_RPT4(_CRT_WARN, "testing connection of %d r:%f s:%f t:%f\n", i, r, s, t);

				bool connectedToP1 = false;
				bool connectedToP2 = false;
				for (size_t j = i + 1; j < linesNotFarAway.size(); j++) {

					LineAttr& ln = linesNotFarAway[j];
					cv::Point pn1 = ln.p1;
					cv::Point pn2 = ln.p2;

					//check the angle between lines
					//if angle is sharp, it's not a corner.
					double rn = cv::norm(pn1 - pn2);
					double sn = (pn1.y - pn2.y) / rn;
					if (sn < -1) sn = -1;
					if (sn > 1) sn = 1;
					double tn = asin(sn);
					double dT = NAN;
					if ((t <= 0 && tn <= 0) || (t >= 0 && tn >= 0)) {
						dT = abs(t - tn);
					}
					else {
						dT = abs(t) + abs(tn);
						if (dT > CV_PI * 0.5 && dT <= CV_PI) {
							dT = CV_PI - dT;
						}
						else if (dT > CV_PI) {
							dT = dT - CV_PI;
						}
					}
					//_RPT4(_CRT_WARN, "rn:%f sn:%f tn:%f dT:%f\n", rn, sn, tn, dT);
					if (dT < PI / 4) {
						//_RPT2(_CRT_WARN, "%d and %d are NOT connected bcoz less angle\n", i, j);
						continue;
					}

					bool connected = false;
					double d = cv::norm(p1 - pn1);
					//_RPT5(_CRT_WARN, "(%d,%d)-(%d,%d):%f\n", p1.x, p1.y, pn1.x, pn1.y, d);
					if (d < connectionThreshold) {
						connected = true;
						connectedToP1 = true;
					}
					else {
						d = cv::norm(p1 - pn2);
						//_RPT5(_CRT_WARN, "(%d,%d)-(%d,%d):%f\n", p1.x, p1.y, pn2.x, pn2.y, d);
						if (d < connectionThreshold) {
							connected = true;
							connectedToP1 = true;
						}
						else {
							d = cv::norm(p2 - pn1);
							//_RPT5(_CRT_WARN, "(%d,%d)-(%d,%d):%f\n", p2.x, p2.y, pn1.x, pn1.y, d);
							if (d < connectionThreshold) {
								connected = true;
								connectedToP2 = true;
							}
							else {
								d = cv::norm(p2 - pn2);
								//_RPT5(_CRT_WARN, "(%d,%d)-(%d,%d):%f\n", p2.x, p2.y, pn2.x, pn2.y, d);
								if (d < connectionThreshold) {
									connected = true;
									connectedToP2 = true;
								}
							}
						}
					}
					if (connected) {
						//_RPT2(_CRT_WARN, "%d and %d are connected\n", i, j);
						l.connected = true;
						ln.connected = true;
					}
					else {
						//_RPT1(_CRT_WARN, "%d not connected\n", j);
					}
					if (connectedToP1 && connectedToP2)
						break;
				}
			}
			//add only lines connected to another line
			for (size_t i = 0; i < linesNotFarAway.size(); i++) {
				//if the line connected to others, add to list
				LineAttr l = linesNotFarAway[i];
				if (l.connected)
					linesConnected.push_back(l);
			}
		}

		for (size_t i = 0; i < linesConnected.size(); i++)
		{
			LineAttr l = linesConnected[i];
			line(cdstP4, l.p1, l.p2, cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
		}

		std::sort(linesConnected.begin(), linesConnected.end(), compareLineAttrByLengthDesc);

		/////// ***** peaklines

		////////////////////////////////////////
		int peakLineSize = cv::min(linesConnected.size(), 50);
		std::vector <LineAttr> peakLines;
		for (size_t i = 0; i < peakLineSize; i++)
		{
			LineAttr l = linesConnected[i];
			l.idx = i;
			peakLines.push_back(l);
		}

		//std::vector <cv::Vec4i> peakLinesSort;

		//int numLines = (int)peakLines.size();
		float theta;
		float rheo;
		std::vector<float> distanceDU;
		std::vector<float> distanceDV;

		//take top 50 only

		// Initilization to NAN (not zaro) using for find the longest distnaces
		for (size_t i = 0; i < peakLineSize; i++)
		{
			distanceDV.push_back(NAN);
			distanceDU.push_back(NAN);
		}
		// Check lines if vertical or horizantal are
		for (int i = 0; i < peakLineSize; i++)
		{
			LineAttr l = peakLines[i];
			cv::Point p1 = l.p1;
			cv::Point p2 = l.p2;
			cv::Point p0(0, 0);
			//double d0 = GetDistanceFromPointToLine(p0, l);
			//double r = cv::norm(p1 - p2);
			double r = l.r;
			//double s = (p1.y - p2.y) / r;
			//if (s < -1) s = -1;
			//if (s > 1) s = 1;
			//double t = asin(s);
			double t = l.t;

			rheo = l.rh;
			theta = t;

			if (rheo < 0)
			{
				theta = theta - PI;
				rheo = abs(rheo);
			}

			if (theta < PI / 2 - 0.5 && theta > PI / 2 - 0.5)
			{
				rheo = rheo * -1;
				theta = theta - PI / 2;
			}

			if ((abs(theta) > (PI / 4)) && (abs(theta) < (3 * PI / 4)))
			{
				distanceDU[i] = rheo;
			}
			else
			{
				distanceDV[i] = rheo;
			}
		}

		// Finding the longest distance from center consider the desired line
		//if (isnan(distanceDV[0]))
		//	distanceDV[0] = 0;
		//if (isnan(distanceDU[0]))
		//	distanceDU[0] = 0;

		int iDUMax = -1;
		int iDUMin = -1;
		int iDVMax = -1;
		int iDVMin = -1;

		float maxDU = NAN;
		float minDU = NAN;
		float maxDV = NAN;
		float minDV = NAN;

		std::vector<float> dUTemp;
		std::vector<float> dVTemp;
		for (int i = 0; i < peakLineSize; i++) {
			if (!isnan(distanceDU[i]))
				dUTemp.push_back(distanceDU[i]);
			if (!isnan(distanceDV[i]))
				dVTemp.push_back(distanceDV[i]);
		}
		maxDU = getMax(dUTemp);
		minDU = getMin(dUTemp);
		maxDV = getMax(dVTemp);
		minDV = getMin(dVTemp);

		for (int i = 0; i < peakLineSize; ++i) {
			if (maxDU == distanceDU[i]) {
				iDUMax = i;
			}
			if (minDU == distanceDU[i]) {
				iDUMin = i;
			}
			if (maxDV == distanceDV[i]) {
				iDVMax = i;
			}
			if (minDV == distanceDV[i]) {
				iDVMin = i;
			}
		}

		//double expectedHVRatio = 3.35 / 2.15;
		if (expectedHVRatio > 0) {
			cv::Mat cdstP5 = cdstP.clone();	//just for debug
			if (pickRectByHVRatio(expectedHVRatio, peakLineConnectionThreshold, peakLines, distanceDU, distanceDV, hed_threshold.cols, hed_threshold.rows,
				iDUMin, iDUMax, iDVMin, iDVMax, cdstP5))
			{
				minDU = distanceDU[iDUMin];
				maxDU = distanceDU[iDUMax];
				minDV = distanceDV[iDVMin];
				maxDV = distanceDV[iDVMax];
			}
		}


		//check if pearlines are collapsed or not
		if (minDU == maxDU) {
			minDU = NAN;
			maxDU = NAN;
			iDUMin = -1;
			iDUMax = -1;
		}
		if (minDV == maxDV) {
			minDV = NAN;
			maxDV = NAN;
			iDVMin = -1;
			iDVMax = -1;
		}

		// Determine the desired Peaklines
		bool bFoundEdges = false;
		int assumptionLine = 1;
		std::vector<cv::Vec4i> ReseveLine;
		if (iDUMin != -1 && iDUMax != -1 && iDVMin != -1 && iDVMax != -1)
		{
			bFoundEdges = true;

			//just for debug
			//_RPT1(_CRT_WARN, "peakLinesSort.length:%d\n", peakLinesSort.size());
			//for (int i = 0; i < peakLinesSort.size(); i++)
			//{
			//	cv::Vec4i l = peakLinesSort[i];
			//	//_RPT5(_CRT_WARN, "peakLinesSort[%d](%d,%d)-(%d,%d)\n", i, l[0], l[1], l[2], l[3]);
			//}

			//cross point between the left edge (bottom-left to top-left), and bottom edge (bottom-right to bottom-left)
			cv::Point result_pLB = getCrossPoint(peakLines[iDUMin].p1, peakLines[iDUMin].p2, peakLines[iDVMax].p1, peakLines[iDVMax].p2);
			//_RPT2(_CRT_WARN, "result_pLB:%d,%d\n", result_pLB.x, result_pLB.y);
			//cross point between the bottom edge (bottom-right to bottom-left), and right edge (top-right to bottom-right)
			cv::Point result_pRB = getCrossPoint(peakLines[iDVMax].p1, peakLines[iDVMax].p2, peakLines[iDUMax].p1, peakLines[iDUMax].p2);
			//_RPT2(_CRT_WARN, "result_pRB:%d,%d\n", result_pRB.x, result_pRB.y);
			//cross point between the right edge (top-right to bottom-right), and top edge (top-left to top-right)
			cv::Point result_pRT = getCrossPoint(peakLines[iDUMax].p1, peakLines[iDUMax].p2, peakLines[iDVMin].p1, peakLines[iDVMin].p2);
			//_RPT2(_CRT_WARN, "result_pRT:%d,%d\n", result_pRT.x, result_pRT.y);
			//cross point between the top edge (top-left to top-right), and left edge (bottom-left to top-left)
			cv::Point result_pLT = getCrossPoint(peakLines[iDVMin].p1, peakLines[iDVMin].p2, peakLines[iDUMin].p1, peakLines[iDUMin].p2);
			//_RPT2(_CRT_WARN, "result_pLT:%d,%d\n", result_pLT.x, result_pLT.y);

			pt1->X = (int)((double)result_pLT.x / imgScalingRatio);
			pt1->Y = (int)((double)result_pLT.y / imgScalingRatio);
			pt2->X = (int)((double)result_pRT.x / imgScalingRatio);
			pt2->Y = (int)((double)result_pRT.y / imgScalingRatio);
			pt3->X = (int)((double)result_pRB.x / imgScalingRatio);
			pt3->Y = (int)((double)result_pRB.y / imgScalingRatio);
			pt4->X = (int)((double)result_pLB.x / imgScalingRatio);
			pt4->Y = (int)((double)result_pLB.y / imgScalingRatio);

			//just for debug
			cv::line(imgDetectedEdge, result_pLT, result_pRT, cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
			cv::line(imgDetectedEdge, result_pRT, result_pRB, cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
			cv::line(imgDetectedEdge, result_pRB, result_pLB, cv::Scalar(0, 255, 0), 3, cv::LINE_AA);
			cv::line(imgDetectedEdge, result_pLB, result_pLT, cv::Scalar(0, 255, 0), 3, cv::LINE_AA);

			//_RPT2(_CRT_WARN, "DetectEdge pt1:(%d,%d)\n", pt1->X, pt1->Y);
			//_RPT2(_CRT_WARN, "DetectEdge pt2:(%d,%d)\n", pt2->X, pt2->Y);
			//_RPT2(_CRT_WARN, "DetectEdge pt3:(%d,%d)\n", pt3->X, pt3->Y);
			//_RPT2(_CRT_WARN, "DetectEdge pt4:(%d,%d)\n", pt4->X, pt4->Y);

		}
		return bFoundEdges;
	}

	bool ImgProcUtil::DetectEdgeHED(array<System::Byte>^ imageSrc, System::Collections::Generic::List<System::Drawing::Point>^% points)
	{
		double expectedHVRatio = 3.35 / 2.15;
		return DetectEdgeHED(imageSrc, points, expectedHVRatio);
	}
	bool ImgProcUtil::DetectEdgeHED(array<System::Byte>^ imageSrc, System::Collections::Generic::List<System::Drawing::Point>^% points, double expectedHVRatio)
	{
		int outWidth;
		int outHeight;

		cv::Mat srcImg;
		pin_ptr<Byte> p = &imageSrc[0];   // entire array is now pinned
		unsigned char* cp = p;
		int docImageLen = imageSrc->Length;
		srcImg = loadImageFromByteArray(cp, docImageLen);
		System::Drawing::Point^ pt1 = gcnew System::Drawing::Point();
		System::Drawing::Point^ pt2 = gcnew System::Drawing::Point();
		System::Drawing::Point^ pt3 = gcnew System::Drawing::Point();
		System::Drawing::Point^ pt4 = gcnew System::Drawing::Point();
		if (detectEdgeHED(srcImg, pt1, pt2, pt3, pt4, expectedHVRatio))
		{
			points->Clear();
			points->Add(*pt1);
			points->Add(*pt2);
			points->Add(*pt3);
			points->Add(*pt4);
			return true;
		}
		return false;
	}
#endif	

	cv::Rect PointsToRect(std::vector<cv::Point> corners, double marginRate = 0.0)
	{
		int min_x = INT_MAX;
		int min_y = INT_MAX;
		int max_x = 0;
		int max_y = 0;
		for (int i = 0; i < corners.size(); i++)
		{
			if (corners[i].x < min_x)
			{
				min_x = corners[i].x;
			}
			if (corners[i].y < min_y)
			{
				min_y = corners[i].y;
			}
			if (corners[i].x > max_x)
			{
				max_x = corners[i].x;
			}
			if (corners[i].y > max_y)
			{
				max_y = corners[i].y;
			}
		}

		if (marginRate > 0.0)
		{
			int width = max_x - min_x;
			int height = max_y - min_y;
			min_x -= width * marginRate;
			min_y -= height * marginRate;
			max_x += width * marginRate;
			max_y += height * marginRate;
		}

		return cv::Rect(min_x, min_y, max_x - min_x, max_y - min_y);
	}
	
	String^ ImgProcUtil::ReadQRCode(array<System::Byte>^ imageSrc) {
		std::string decoded;
		std::vector<cv::Point> corners;
		cv::Mat srcImg;
		pin_ptr<Byte> p = &imageSrc[0];   // entire array is now pinned
		unsigned char* cp = p;
		int docImageLen = imageSrc->Length;
		srcImg = loadImageFromByteArray(cp, docImageLen);

		cv::QRCodeDetectorAruco detector;
		bool bRet = detector.detect(srcImg, corners);
		if (bRet) {
			cv::Rect rect = PointsToRect(corners, 0.1);
			cv::Mat ROI(srcImg, rect);
			cv::Mat croppedImage;
			// Copy the data into new matrix
			ROI.copyTo(croppedImage);
			decoded = detector.detectAndDecode(croppedImage);
		}
		String^ ret = gcnew String(decoded.c_str());
		return ret;
	}


	bool ImgProcUtil::DetectFace(array<System::Byte>^ imageSrc, System::Int32^% left, System::Int32^% top, System::Int32^% right, System::Int32^% bottom)
	{
		bool bRet = false;
		pin_ptr<Byte> p = &imageSrc[0];   // entire array is now pinned
		byte* pData = p;
		int pts[8];
		if (DlibDetectFace(pData, imageSrc->Length, pts)) {
			bRet = true;
			int l = pts[0];
			int t = pts[1];
			int r = pts[2];
			int b = pts[3];
			left = gcnew System::Int32(l);
			top = gcnew System::Int32(t);
			right = gcnew System::Int32(r);
			bottom = gcnew System::Int32(b);
		}
		return bRet;
	}
}