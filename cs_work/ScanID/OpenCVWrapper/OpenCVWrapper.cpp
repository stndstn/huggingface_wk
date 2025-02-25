#include <stdio.h>
#include <map>
#include <string>

#ifdef _WINDOWS
#include <shlwapi.h>
#else
#include <dirent.h>
#endif

#include <iostream>
#include <vector>

#include <opencv2/opencv.hpp>
#include <opencv2/core.hpp>
#include <opencv2/imgcodecs.hpp>
#include <opencv2/highgui.hpp>
#include <opencv2/objdetect.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/dnn/layer.details.hpp>  // CV_DNN_REGISTER_LAYER_CLASS
#include <opencv2/dnn/all_layers.hpp>

#include "OpenCVWrapper.h"

OPENCVWRAPPER_API
int Hello(const char* pszName)
{
	printf("Hello %s\n", pszName);
	return true;
}


// Function to check if a file is an image
bool isImage(const std::string& filename) {
	// Check for common image file extensions
	std::vector<std::string> imageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
	for (const auto& extension : imageExtensions) {
		if (filename.size() >= extension.size() && filename.compare(filename.size() - extension.size(), extension.size(), extension) == 0) {
			return true;
		}
	}
	return false;
}

const std::string getExtIfImage(const std::string& filename) {
	// Check for common image file extensions
	std::vector<std::string> imageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
	for (const auto& extension : imageExtensions) {
		if (filename.size() >= extension.size() && filename.compare(filename.size() - extension.size(), extension.size(), extension) == 0) {
			return extension;
		}
	}
	return "";
}

std::map<std::string, cv::Mat> _mapTmpl;
bool _isTemplateLoaded = false;

bool loadTemplate(const char* pszTemplateFolderPath)
{
	// clean existing dictionary
	_mapTmpl.clear();

	// register all image files in the template folder
#ifdef _WINDOWS
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
		return false;
	}

	// Specify the folder path
	//std::string folderPath = "C:\\path\\to\\your\\folder";
	std::string folderPath(pszTemplateFolderPath);

	if (PathIsRelativeA(pszTemplateFolderPath)) {
		CHAR szDirFullPath[_MAX_FNAME];
		memset(szDirFullPath, 0, sizeof(szDirFullPath));
		errno_t err = _makepath_s(szDirFullPath, _MAX_FNAME, szDrive, szDir, pszTemplateFolderPath, NULL);
		if (err != 0) {
			return false;
		}
		folderPath = szDirFullPath;
	}

	// Open the directory
	WIN32_FIND_DATAA findData;
	HANDLE hFind = FindFirstFileA((folderPath + "\\*").c_str(), &findData);
	if (hFind == INVALID_HANDLE_VALUE) {
		std::cerr << "Error opening directory: " << folderPath << std::endl;
		return 1;
	}

	// Iterate through the directory
	do {
		// Check if the entry is a file
		if (!(findData.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)) {
			// Get the filename
			std::string filename = findData.cFileName;

			// Check if the file is an image
			std::string ext = getExtIfImage(filename);
			if (!ext.empty()) {
				// Print the image file name
				std::string fileNameWOExt = filename.substr(0, filename.size() - ext.size());
				//std::cout << filename << std::endl;
				if (!fileNameWOExt.empty()) {
					//cv::Mat srcImg = cv::imread(filename, 1);
					cv::Mat srcImg = cv::imread(folderPath + "\\" + filename, 1);
					if (!srcImg.empty()) {
						cv::Mat tmpl;
						cv::cvtColor(srcImg, tmpl, cv::COLOR_BGR2GRAY);
						srcImg.release();
						_mapTmpl.insert(std::map<std::string, cv::Mat>::value_type(fileNameWOExt, tmpl));
					}
					else {
						std::cerr << "Error loading image: " << filename << std::endl;
					}
				}
			}
		}
	} while (FindNextFileA(hFind, &findData));

	// Close the directory
	FindClose(hFind);
#else
	// Specify the folder path
	//std::string folderPath = "/path/to/your/folder";
	std::string folderPath = pszTemplateFolderPath;
	std::cout << "loadTemplate folderPath: " << folderPath << std::endl;

	// Open the directory
	DIR* dir = opendir(folderPath.c_str());
	if (dir == nullptr) {
		std::cerr << "Error opening directory: " << folderPath << std::endl;
		return 1;
	}

	// Iterate through the directory
	dirent* ent;
	while ((ent = readdir(dir)) != nullptr) {
		// Check if the entry is a file
		if (ent->d_type == DT_REG) {
			// Get the filename
			std::string filename = ent->d_name;
			std::string filenameFull = folderPath + "/" + ent->d_name;
			//std::cout << "loadTemplate filenameFull: " << filenameFull << std::endl;

			// Check if the file is an image
			std::string ext = getExtIfImage(filenameFull);
			//std::cout << "loadTemplate ext: " << ext << std::endl;
			if (!ext.empty()) {
				// Print the image file name
				std::string fileNameWOExt = filename.substr(0, filename.size() - ext.size());
				//std::cout << "loadTemplate fileNameWOExt: " << fileNameWOExt << std::endl;
				//std::cout << filename << std::endl;
				if (!fileNameWOExt.empty()) {
					cv::Mat srcImg = cv::imread(filenameFull, 1);
					cv::Mat tmpl;
					if (!srcImg.empty()) {
						cv::cvtColor(srcImg, tmpl, cv::COLOR_BGR2GRAY);
						srcImg.release();
						_mapTmpl.insert(std::map<std::string, cv::Mat>::value_type(fileNameWOExt, tmpl));
					}
					else {
						std::cerr << "Error loading image: " << filenameFull << std::endl;
					}
				}
			}
		}
	}

	// Close the directory
	closedir(dir);

#endif

	_isTemplateLoaded = true;
	return true;
}

cv::Mat loadImageFromByteArray(unsigned char* pData, size_t len)
{
	std::vector<unsigned char> data = std::vector<unsigned char>(pData, pData + len);
	cv::Mat decoded = cv::imdecode(data, cv::IMREAD_COLOR);	//IMREAD_COLOR: always convert image to the 3 channel BGR color image.
	return decoded;
}

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

OPENCVWRAPPER_API
//int DoMatchTemplate(unsigned char* pDocImageData, unsigned int docImageLen, const char* pszTemplateFolderPath, PCMATCH_TEMPLATE_RESULT* ppMatchTemplateResult) {
//OPENCVWRAPPER_API PCMATCH_TEMPLATE_RESULT DoMatchTemplate(unsigned char* pDocImageData, unsigned int docImageLen, const char* pszTemplateFolderPath) {
OPENCVWRAPPER_API void* DoMatchTemplate(unsigned char* pDocImageData, unsigned int docImageLen, const char* pszTemplateFolderPath) {

	if (!_isTemplateLoaded) {
		loadTemplate(pszTemplateFolderPath);
	}

	//errmsg->Clear();
	PMATCH_TEMPLATE_RESULT pResult = new MATCH_TEMPLATE_RESULT();
	memset(pResult, 0, sizeof(MATCH_TEMPLATE_RESULT));
	std::vector<PMATCH_TEMPLATE_RESULT_ITEM> resultItems;

	//
	//Load image
	//
	cv::Mat image = loadImageFromByteArray(pDocImageData, docImageLen);

	if (image.empty())                      // Check for invalid input
	{
		std::cerr << "Could not load image data to OpenCV.";
		//return -1;
		return nullptr;
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

	//
	// Match
	//
	cv::Point matchLoc;
	double matchVal = 0;
	//wprintf(L"%d\n", _mapTmpl.size());
	for (std::pair<std::string, cv::Mat> i : _mapTmpl)
	{
		try {
			Match(grayImg, i.second, matchLoc, matchVal);
			//Match(resizedImg, i.second, matchLoc, matchVal);
			std::string name = std::string(i.first.c_str());
			//wprintf(L"%s %f\n", i.first.c_str(), matchVal);
			PMATCH_TEMPLATE_RESULT_ITEM pItem = new MATCH_TEMPLATE_RESULT_ITEM();
			strcpy(pItem->name, name.c_str());
			pItem->locX = matchLoc.x;
			pItem->locY = matchLoc.y;
			pItem->width = i.second.cols;
			pItem->height = i.second.rows;
			pItem->MatchResult = matchVal;
			resultItems.push_back(pItem);
		}
		catch (std::exception e)
		{
			printf("DoMatchTemplate exception: %s", e.what());
		}
	}

	pResult->countItems = resultItems.size();
	//pResult->ppItems = resultItems.data();
	pResult->pItems = new MATCH_TEMPLATE_RESULT_ITEM[resultItems.size()];
	for(int i = 0; i < resultItems.size(); i++) {
		PMATCH_TEMPLATE_RESULT_ITEM pItem = resultItems[i];
		memset(&pResult->pItems[i], 0, sizeof(MATCH_TEMPLATE_RESULT_ITEM));
		memcpy(&pResult->pItems[i], pItem, sizeof(MATCH_TEMPLATE_RESULT_ITEM));
		delete(pItem);
	}

	return pResult;
	//*ppMatchTemplateResult = (unsigned long)pResult;
	//*ppMatchTemplateResult = pResult;
	//return 0;
}

OPENCVWRAPPER_API int FreeMatchTemplateResult(void* pResult)
{
	PMATCH_TEMPLATE_RESULT pMatchTemplateResult = (PMATCH_TEMPLATE_RESULT)pResult;
	int countItems = pMatchTemplateResult->countItems;
	if (countItems > 0 && pMatchTemplateResult->pItems != nullptr) {
		delete[] pMatchTemplateResult->pItems;
	}
	delete pMatchTemplateResult;
	return 0;
}
