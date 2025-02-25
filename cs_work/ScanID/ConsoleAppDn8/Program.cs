// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;
using LibImportDn8;
using static LibImportDn8.MatchTemplateIDCard;

public partial class Program
{
    //https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke
    //[DllImport("OpenCVWrapper.dll")]
    //[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    //[LibraryImport("OpenCVWrapper", EntryPoint = "Hello", StringMarshalling = StringMarshalling.Utf8)]
    //private static partial int Hello([MarshalAs(UnmanagedType.LPStr)] string sourceString);


    public static void Main(string[] args)
    {
        // print all args
        Console.WriteLine($"args ({args.Length})");
        foreach(string arg in args){
            Console.WriteLine($"{arg}");
        }

        // Invoke the function as a regular managed method.
        //int ret = Hello("World");
        int ret = LibImport.Hello("World");
        Console.WriteLine(ret);

        if (args.Length == 1)
        {
            string imageFile = Path.GetFullPath(args[0]);
            Console.WriteLine($"imageFile ({imageFile})");
            if (File.Exists(imageFile))
            {
                using(FileStream fs = new FileStream(imageFile, FileMode.Open))
                {
                    byte[] buffer = new byte[fs.Length];
                    fs.Read(buffer, 0, buffer.Length);
                    fs.Close();
                    int[] rectFace = new int[4];
                    int retFace = LibImport.DlibDetectFace(buffer, buffer.Length, rectFace);
                    Console.WriteLine($"retFace: {retFace} rectFace: {rectFace[0]} {rectFace[1]} {rectFace[2]} {rectFace[3]}");
                }
            }
        }

        if (args.Length == 2)
        {
            string templatefolder = Path.GetFullPath(args[0]);
            Console.WriteLine($"templatefolder ({templatefolder})");
            string imageFile = Path.GetFullPath(args[1]);
            Console.WriteLine($"imageFile ({imageFile})");
            if (File.Exists(imageFile) && Directory.Exists(templatefolder))
            {
                using (FileStream fs = new FileStream(imageFile, FileMode.Open))
                {
                    byte[] buffer = new byte[fs.Length];
                    fs.Read(buffer, 0, buffer.Length);
                    fs.Close();
                    //IntPtr pResult = IntPtr.Zero;
                    //IntPtr pRet = LibImport.DoMatchTemplate(buffer, buffer.Length, templatefolder, ref pResult);
                    IntPtr pResult = LibImport.DoMatchTemplate(buffer, buffer.Length, templatefolder);
                    if (pResult != IntPtr.Zero)
                    {
                        LibImportDn8.MatchTemplateIDCard.StructMatchTemplateResult sResult = Marshal.PtrToStructure<LibImportDn8.MatchTemplateIDCard.StructMatchTemplateResult>(pResult);
                        Console.WriteLine($"ret: {sResult}");
                        for(int i = 0; i < sResult.countItems; i++)
                        {
                            //IntPtr ppItem = sResult.ppItems + i * Marshal.SizeOf(typeof(LibImportDn8.MatchTemplateIDCard.StructMatchTemplateResultItem));
                            IntPtr ppItem = sResult.pItems + i * Marshal.SizeOf(typeof(StructMatchTemplateResultItem));
                            //LibImportDn8.MatchTemplateIDCard.StructMatchTemplateResultItem sItem = Marshal.PtrToStructure<LibImportDn8.MatchTemplateIDCard.StructMatchTemplateResultItem>(ppItem);
                            StructMatchTemplateResultItem sItem = (StructMatchTemplateResultItem)Marshal.PtrToStructure(ppItem, typeof(StructMatchTemplateResultItem));
                            string name = "";
                            byte[] namebuf = new byte[256];
                            for (int ii = 0; ii < 256; ii++)
                            {
                                unsafe
                                {
                                    //Console.Write((char)sItem.Name[ii]);
                                    namebuf[ii] = sItem.Name[ii];
                                    if (sItem.Name[ii] == 0)
                                        break;
                                }
                            }

                            name = System.Text.Encoding.Default.GetString(namebuf).TrimEnd('\0'); ;
                            MatchTemplateResultItem item = new MatchTemplateResultItem(name, sItem.MatchResult, sItem.LocX, sItem.LocY, sItem.Width, sItem.Height);
                            Console.WriteLine($"[{i}git {item.Name}, LocX:{item.LocX} LocY:{item.LocY} Width:{item.Width} Height:{item.Height} MatchResult:{item.MatchResult}");
                        }
                        int retFree = LibImport.FreeMatchTemplateResult(pResult);
                    }
                }

            }
        }
    }
}
