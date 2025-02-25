#include "DlibWrapper.h"

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
		std::vector<BYTE> imgData;
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

DLIBWRAPPER_API
bool DlibDetectFace(unsigned char* pImageData, int nSize, int pts[4])
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

	if (dets.size() > 0) {
		/*
		return dets;
		*/
		pts[0] = dets[0].left();
		pts[1] = dets[0].top();
		pts[2] = dets[0].right();
		pts[3] = dets[0].bottom();
		return true;
	}

	return false;
}
