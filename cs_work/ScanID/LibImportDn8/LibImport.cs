using System.Runtime.InteropServices;

namespace LibImportDn8
{
    public partial class LibImport
    {
        [LibraryImport("OpenCVWrapper", EntryPoint = "Hello", StringMarshalling = StringMarshalling.Utf8)]
        public static partial int Hello([MarshalAs(UnmanagedType.LPStr)] string sourceString);

        [LibraryImport("OpenCVWrapper", EntryPoint = "DoMatchTemplate", StringMarshalling = StringMarshalling.Utf8)]
        //PCMATCH_TEMPLATE_RESULT DoMatchTemplate(unsigned char* pDocImageData, unsigned int docImageLen, const unsigned char* pszTemplateFolderPath)
        //PCMATCH_TEMPLATE_RESULT DoMatchTemplate(unsigned char* pDocImageData, unsigned int docImageLen, const char* pszTemplateFolderPath) {
        public static partial IntPtr DoMatchTemplate(byte[] imageData, int imageDataLength, [MarshalAs(UnmanagedType.LPStr)] string templateFolderPath);
        //public static partial int DoMatchTemplate(byte[] imageData, int imageDataLength, [MarshalAs(UnmanagedType.LPStr)] string templateFolderPath, ref IntPtr ppResult);
        //public static partial int DoMatchTemplate(byte[] imageData, int imageDataLength, [MarshalAs(UnmanagedType.LPStr)] string templateFolderPath);

        [LibraryImport("OpenCVWrapper", EntryPoint = "FreeMatchTemplateResult")]
        public static partial int FreeMatchTemplateResult(IntPtr pResult);

        [LibraryImport("DlibWrapper", EntryPoint = "DlibDetectFace")]
        public static partial int DlibDetectFace(byte[] imageData, int imageDataLength, int[] faceRects);

    }
}
