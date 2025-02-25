using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LibImportDn8
{
    public class MatchTemplateIDCard
    {
        public class MatchTemplateResultItem
        {
            public string Name { get; protected set; }
            public double MatchResult { get; protected set; }
            public int LocX { get; protected set; }
            public int LocY { get; protected set; }
            public int Width { get; protected set; }
            public int Height { get; protected set; }

            public MatchTemplateResultItem(string name, double matchResult, int locX, int locY, int width, int height)
            { 
                Name = name;
                LocX = locX; 
                LocY = locY; 
                Width = width; 
                Height = height;
                MatchResult = matchResult;
            }
        };

        public struct StructMatchTemplateResultItem
        {
            //public string Name;
            public unsafe fixed byte Name[256];
            public int LocX;
            public int LocY;
            public int Width;
            public int Height;
            public double MatchResult;
        };

        public class MatchTemplateResult
        {

            public MatchTemplateResult() { }

            ~MatchTemplateResult() { }

            public Dictionary<string, MatchTemplateResultItem> MatchResult { get; protected set; } = new Dictionary<string, MatchTemplateResultItem>();
            //property double MatchVal_MyKad;
            //property double MatchVal_Flag;
        };

        public struct StructMatchTemplateResult
        {
            public IntPtr pItems;
            public uint countItems;
        };

        public MatchTemplateIDCard(string templFolderPath) 
        {
            templateFolderPath = templFolderPath;
        }

        ~MatchTemplateIDCard() { }

        string templateFolderPath;
        /*
        public bool Init(string templateFolderPath)
        {
            return LoadTemplate(templateFolderPath);
        }

        bool LoadTemplate(string templateFolderPath)
        {
            return false;
        }
        */

        public MatchTemplateResult? DoMatchTemplate(byte[] docImage)
        {
            MatchTemplateResult result = new MatchTemplateResult();
            IntPtr pResult = IntPtr.Zero;
            pResult = LibImport.DoMatchTemplate(docImage, docImage.Length, templateFolderPath);
            //int ret = LibImport.DoMatchTemplate(docImage, docImage.Length, templateFolderPath, ref pResult);
            //int ret = LibImport.DoMatchTemplate(docImage, docImage.Length, templateFolderPath);
            if (pResult != IntPtr.Zero)
            {
                LibImportDn8.MatchTemplateIDCard.StructMatchTemplateResult sResult = Marshal.PtrToStructure<LibImportDn8.MatchTemplateIDCard.StructMatchTemplateResult>(pResult);
                Console.WriteLine($"ret: {sResult}");
                for (int i = 0; i < sResult.countItems; i++)
                {
                    IntPtr ppItem = sResult.pItems + i * Marshal.SizeOf(typeof(StructMatchTemplateResultItem));
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
                    Console.WriteLine($"[{i}] Name:{item.Name}, LocX:{item.LocX} LocY:{item.LocY} Width:{item.Width} Height:{item.Height} MatchResult:{item.MatchResult}");
                    result.MatchResult.Add(name, item);
                }

                int ret = LibImport.FreeMatchTemplateResult(pResult);
            }
            return result;
        }
    }
}
