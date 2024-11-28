using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Drawing.Imaging;
using System.Drawing;
using System.Text.RegularExpressions;
using ScanID;
//using Tesseract;

namespace ConsoleApp1
{
    public class DocFieldDef
    {
        public string Name { get; set; }
        public RectangleF BoundsInInch { get; set; }
    }

    public class OcrRequest
    {
        public string b64 { get; set; }
    }

    internal class Program
    {
        const string BASEADDR_URL = "http://127.0.0.1:8085/";
        static void Main(string[] args)
        {
            try
            {
                // check connection
                using (var client = new HttpClient())
                {
                    // Set the base address of the web service
                    client.BaseAddress = new Uri(BASEADDR_URL);

                    // Post the request to the web service
                    var response = client.GetAsync($"{BASEADDR_URL}device").GetAwaiter().GetResult();

                    // Check the status code of the response
                    if (response.IsSuccessStatusCode)
                    {
                        // Get the response content
                        var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        Console.WriteLine(responseBody);
                    }
                    else
                    {
                        Console.WriteLine("Error: " + response.StatusCode);
                    }
                }

                // check args
                if (args.Length == 0)
                {
                    Console.WriteLine("No image provided");
                    return;
                }

                List<Task> lsTask = new List<Task>();
                for (int i = 0; i < 1; i++)
                {
                    //Task t = TestMYDL(args);
                    Task t = TestMyKad(args);
                    lsTask.Add(t);
                    System.Threading.Thread.Sleep(1000);
                }
                Task.WaitAll(lsTask.ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }

            Console.Write("Hit enter to exit:");
            Console.ReadLine();
        }

        static Task Test(string[] args)
        {
            // Task to post each of all images
            // ./images/CSDEMO_BANK_Logo1.jpg ./images/CSDEMO_BANK_IndividualApplicationFormTitle1.jpg ./images/CSDEMO_BANK_Name1.jpg ./images/CSDEMO_BANK_DoB1.jpg ./images/CSDEMO_BANK_PlaceOfBirth1.jpg ./images/CSDEMO_BANK_Nationarity1.jpg
            return Task.Run(() =>
            {
                foreach (string imageFileName in args)
                {
                    if (!System.IO.File.Exists(imageFileName))
                    {
                        Console.WriteLine("File not found: " + imageFileName);
                        continue;
                    }
                    string b64Image = "";
                    using (var stream = System.IO.File.OpenRead(imageFileName))
                    {
                        byte[] b = new byte[stream.Length];
                        stream.Read(b, 0, b.Length);
                        b64Image = Convert.ToBase64String(b);
                    }

                    if (b64Image.Length == 0)
                    {
                        Console.WriteLine("File is empty: " + imageFileName);
                        continue;
                    }

                    Console.WriteLine($"[{imageFileName}]");
                    {
                        DateTime dtStart = DateTime.Now;
                        string retOcr = PostOCRRequest(b64Image);
                        DateTime dtEnd = DateTime.Now;
                        Console.WriteLine(retOcr);
                        Console.WriteLine($"({(dtEnd - dtStart).TotalSeconds} sec)\n");
                    }
                    /*
                    {
                        DateTime dtStart = DateTime.Now;
                        string retOcrWithRegion = PostOCRWithRegionRequest(b64Image);
                        DateTime dtEnd = DateTime.Now;
                        Console.WriteLine(retOcrWithRegion);
                        Console.WriteLine($"({(dtEnd - dtStart).TotalSeconds} sec)\n");
                    }
                    */
                }
            });
        }

        static Task Test2(string[] args)
        {
            // .\images\CSDEMOBANK_ApplicationForm_P1.pdf

            List<DocFieldDef> lsDocFields = new List<DocFieldDef>();
            lsDocFields.Add(new DocFieldDef() { Name = "DocTitle", BoundsInInch = new RectangleF(0.5f, 0.4f, 1.6f, 0.4f) });
            lsDocFields.Add(new DocFieldDef() { Name = "Name", BoundsInInch = new RectangleF(0.65f, 1.6f, 4.5f, 0.4f) });
            lsDocFields.Add(new DocFieldDef() { Name = "ChkMr", BoundsInInch = new RectangleF(0.8f, 2.0f, 0.25f, 0.2f) });
            lsDocFields.Add(new DocFieldDef() { Name = "ChkMrs", BoundsInInch = new RectangleF(1.65f, 2.05f, 0.25f, 0.2f) });
            lsDocFields.Add(new DocFieldDef() { Name = "ChkMs", BoundsInInch = new RectangleF(2.55f, 2.05f, 0.25f, 0.2f) });
            lsDocFields.Add(new DocFieldDef() { Name = "ChkOthers", BoundsInInch = new RectangleF(3.45f, 2.05f, 0.25f, 0.2f) });
            lsDocFields.Add(new DocFieldDef() { Name = "OthersText", BoundsInInch = new RectangleF(4.1f, 1.9f, 2.4f, 0.4f) });
            lsDocFields.Add(new DocFieldDef() { Name = "DateOfBirth", BoundsInInch = new RectangleF(0.65f, 2.25f, 2.4f, 0.4f) });
            lsDocFields.Add(new DocFieldDef() { Name = "PlaceOfBirth", BoundsInInch = new RectangleF(3.5f, 2.25f, 2.2f, 0.4f) });
            lsDocFields.Add(new DocFieldDef() { Name = "Nationarity", BoundsInInch = new RectangleF(5.3f, 2.25f, 2.2f, 0.4f) });

            // Task to post each of all images
            return Task.Run(() =>
            {
                string imageFileName = args[0];
                //string fieldDefFileName = args[1];

                if (!System.IO.File.Exists(imageFileName))
                {
                    Console.WriteLine("File not found: " + imageFileName);
                    return;
                }

                Bitmap bmpDoc = new Bitmap(imageFileName);
                float dpi = bmpDoc.Width / 8.27f;
                float dpi2 = bmpDoc.Height / 11.69f;

                foreach (DocFieldDef fd in lsDocFields)
                {
                    int x = (int)(fd.BoundsInInch.X * dpi);
                    int y = (int)(fd.BoundsInInch.Y * dpi);
                    int w = (int)(fd.BoundsInInch.Width * dpi);
                    int h = (int)(fd.BoundsInInch.Height * dpi);

                    Bitmap bmpField = bmpDoc.Clone(new Rectangle(x, y, w, h), System.Drawing.Imaging.PixelFormat.DontCare);

                    string b64Image = "";
                    using (var stream = new System.IO.MemoryStream())
                    {
                        bmpField.Save(fd.Name + ".png", System.Drawing.Imaging.ImageFormat.Png);
                        bmpField.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        stream.Seek(0, System.IO.SeekOrigin.Begin);
                        byte[] b = new byte[stream.Length];
                        stream.Read(b, 0, b.Length);
                        b64Image = Convert.ToBase64String(b);
                    }

                    if (b64Image.Length == 0)
                    {
                        Console.WriteLine("File is empty: " + imageFileName);
                        return;
                    }

                    Console.WriteLine($"[{imageFileName}]");
                    {
                        DateTime dtStart = DateTime.Now;
                        string retOcr = PostOCRRequest(b64Image);
                        DateTime dtEnd = DateTime.Now;
                        Console.WriteLine(retOcr);
                        Console.WriteLine($"({(dtEnd - dtStart).TotalSeconds} sec)\n");
                    }

                }
            });
        }

        static Task TestMYDL(string[] args)
        {
            // Task to post each of all images
            // .\images\MYDL1_s.jpg .\images\MYDL1.jpg .\images\MYDL2.png .\images\MYDL2.jpg
            return Task.Run(() =>
            {
                foreach (string imageFileName in args)
                {
                    try
                    {
                        ScanMYDLResult scanMYDLResult = ScanIDOCR.ScanMYDL(BASEADDR_URL, imageFileName);
                        Console.WriteLine($"scanMYDLResult.Success: {scanMYDLResult.Success}");
                        Console.WriteLine($"scanMYDLResult.Error: {scanMYDLResult.Error}");
                        Console.WriteLine($"scanMYDLResult.lastNameOrFullName: {scanMYDLResult.lastNameOrFullName}");
                        Console.WriteLine($"scanMYDLResult.documentNumber: {scanMYDLResult.documentNumber}");
                        Console.WriteLine($"scanMYDLResult.nationality: {scanMYDLResult.nationality}");
                        Console.WriteLine($"scanMYDLResult.documentIssueDate: {scanMYDLResult.documentIssueDate}");
                        Console.WriteLine($"scanMYDLResult.documentExpirationDate: {scanMYDLResult.documentExpirationDate}");
                        Console.WriteLine($"scanMYDLResult.addressLine1: {scanMYDLResult.addressLine1}");
                        Console.WriteLine($"scanMYDLResult.addressLine2: {scanMYDLResult.addressLine2}");
                        Console.WriteLine($"scanMYDLResult.postcode: {scanMYDLResult.postcode}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ScanMYDL exception: {ex}");
                    }
#if false
                    // Try Tesseract
                    if (!System.IO.File.Exists(imageFileName))
                    {
                        Console.WriteLine("File not found: " + imageFileName);
                        continue;
                    }

                    Bitmap bmpImage = new Bitmap(imageFileName);
                    int width = bmpImage.Width;
                    int height = bmpImage.Height;

                    string b64Image = "";
                    using (var stream = System.IO.File.OpenRead(imageFileName))
                    {
                        byte[] b = new byte[stream.Length];
                        stream.Read(b, 0, b.Length);
                        b64Image = Convert.ToBase64String(b);
                    }

                    if (b64Image.Length == 0)
                    {
                        Console.WriteLine("File is empty: " + imageFileName);
                        continue;
                    }

                    Console.WriteLine($"[{imageFileName}]");
                    {
                        DateTime dtStart = DateTime.Now;
                        //string retOcr = PostOCRWithRegionRequest(b64Image);
                        //Console.WriteLine(retOcr);
                        //List<Line> lines = ScanID.PostOCRWithRegionRequest(BASEADDR_URL, b64Image);
                        List<Line> linesTess = ScanIDOCR.OCRLinesWithTesseractB64(b64Image);
                        DateTime dtEnd = DateTime.Now;
                        Console.WriteLine($"({(dtEnd - dtStart).TotalSeconds} sec)\n");

                        // remove </s> from the start of 1st line
                        if (linesTess.Count > 0 && linesTess[0].Text.StartsWith("</s>"))
                        {
                            linesTess[0].Text = linesTess[0].Text.Replace("</s>", "");
                        }

                        Console.WriteLine("Lines extrected by Tesseract:");
                        List<Line> linesTessRemoveDuplicated = new List<Line>();
                        Dictionary<string, Line> linesTessDict = new Dictionary<string, Line>();
                        foreach (Line line in linesTess)
                        {
                            if (linesTessDict.ContainsKey(line.Text))
                            {
                                Console.WriteLine($"  {line.Text} --> duplicated");
                            }
                            else
                            {
                                Console.WriteLine($"  {line.Text}");
                                linesTessDict[line.Text] = line;
                            }
                        }

                        linesTessRemoveDuplicated = linesTessDict.Values.ToList();

                        ScanMYDLResult scanMYDLResult = ScanIDOCR.ExtractFieldsFromReadResultOfMYDL(linesTessRemoveDuplicated, width);
                        Console.WriteLine($"scanMYDLResult.Success: {scanMYDLResult.Success}");
                        Console.WriteLine($"scanMYDLResult.Error: {scanMYDLResult.Error}");
                        Console.WriteLine($"scanMYDLResult.lastNameOrFullName: {scanMYDLResult.lastNameOrFullName}");
                        Console.WriteLine($"scanMYDLResult.documentNumber: {scanMYDLResult.documentNumber}");
                        Console.WriteLine($"scanMYDLResult.nationality: {scanMYDLResult.nationality}");
                        Console.WriteLine($"scanMYDLResult.documentIssueDate: {scanMYDLResult.documentIssueDate}");
                        Console.WriteLine($"scanMYDLResult.documentExpirationDate: {scanMYDLResult.documentExpirationDate}");
                        Console.WriteLine($"scanMYDLResult.addressLine1: {scanMYDLResult.addressLine1}");
                        Console.WriteLine($"scanMYDLResult.addressLine2: {scanMYDLResult.addressLine2}");
                        Console.WriteLine($"scanMYDLResult.postcode: {scanMYDLResult.postcode}");
                    }
#endif
                }
            });
        }


        static Task TestMyKad(string[] args)
        {
            // Task to post each of all images
            // .\images\MyKad1_F.jpg .\images\MyKad1_F.png .\images\MyKad2_F.jpg .\images\MyKad3_F.jpg   
            return Task.Run(() =>
            {
                foreach (string imageFileName in args)
                {
                    try
                    {
                        ScanMyKadResult scanMyKadResult = ScanIDOCR.ScanMyKad(BASEADDR_URL, imageFileName);
                        Console.WriteLine($"scanMyKadResult.Success: {scanMyKadResult.Success}");
                        Console.WriteLine($"scanMyKadResult.Error: {scanMyKadResult.Error}");
                        Console.WriteLine($"scanMyKadResult.lastNameOrFullName: {scanMyKadResult.lastNameOrFullName}");
                        Console.WriteLine($"scanMyKadResult.documentNumber: {scanMyKadResult.documentNumber}");
                        Console.WriteLine($"scanMyKadResult.addressLine1: {scanMyKadResult.addressLine1}");
                        Console.WriteLine($"scanMyKadResult.addressLine2: {scanMyKadResult.addressLine2}");
                        Console.WriteLine($"scanMyKadResult.postcode: {scanMyKadResult.postcode}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ScanMYDL exception: {ex}");
                    }
#if false
                    // Try Tesseract
                    if (!System.IO.File.Exists(imageFileName))
                    {
                        Console.WriteLine("File not found: " + imageFileName);
                        continue;
                    }

                    Bitmap bmpImage = new Bitmap(imageFileName);
                    int width = bmpImage.Width;
                    int height = bmpImage.Height;

                    string b64Image = "";
                    using (var stream = System.IO.File.OpenRead(imageFileName))
                    {
                        byte[] b = new byte[stream.Length];
                        stream.Read(b, 0, b.Length);
                        b64Image = Convert.ToBase64String(b);
                    }

                    if (b64Image.Length == 0)
                    {
                        Console.WriteLine("File is empty: " + imageFileName);
                        continue;
                    }

                    Console.WriteLine($"[{imageFileName}]");
                    {
                        DateTime dtStart = DateTime.Now;
                        //string retOcr = PostOCRWithRegionRequest(b64Image);
                        //Console.WriteLine(retOcr);
                        //List<Line> lines = ScanID.PostOCRWithRegionRequest(BASEADDR_URL, b64Image);
                        List<Line> linesTess = ScanIDOCR.OCRLinesWithTesseractB64(b64Image);
                        DateTime dtEnd = DateTime.Now;
                        Console.WriteLine($"({(dtEnd - dtStart).TotalSeconds} sec)\n");

                        // remove </s> from the start of 1st line
                        if (linesTess.Count > 0 && linesTess[0].Text.StartsWith("</s>"))
                        {
                            linesTess[0].Text = linesTess[0].Text.Replace("</s>", "");
                        }

                        Console.WriteLine("Lines extrected by Tesseract:");
                        List<Line> linesTessRemoveDuplicated = new List<Line>();
                        Dictionary<string, Line> linesTessDict = new Dictionary<string, Line>();
                        foreach (Line line in linesTess)
                        {
                            if (linesTessDict.ContainsKey(line.Text))
                            {
                                Console.WriteLine($"  {line.Text} --> duplicated");
                            }
                            else
                            {
                                Console.WriteLine($"  {line.Text}");
                                linesTessDict[line.Text] = line;
                            }
                        }

                        linesTessRemoveDuplicated = linesTessDict.Values.ToList();

                        ScanMyKadResult scanMyKadResult = ScanIDOCR.ExtractFieldsFromReadResultOfMyKad(linesTessRemoveDuplicated);
                        Console.WriteLine($"scanMyKadResult.Success: {scanMyKadResult.Success}");
                        Console.WriteLine($"scanMyKadResult.Error: {scanMyKadResult.Error}");
                        Console.WriteLine($"scanMyKadResult.lastNameOrFullName: {scanMyKadResult.lastNameOrFullName}");
                        Console.WriteLine($"scanMyKadResult.documentNumber: {scanMyKadResult.documentNumber}");
                        Console.WriteLine($"scanMyKadResult.addressLine1: {scanMyKadResult.addressLine1}");
                        Console.WriteLine($"scanMyKadResult.addressLine2: {scanMyKadResult.addressLine2}");
                        Console.WriteLine($"scanMyKadResult.postcode: {scanMyKadResult.postcode}");
                    }
#endif
                }
            });
        }

        static Task TestPHUMID(string[] args)
        {
            // Task to post each of all images
            // .\images\MyKad1_F.jpg .\images\MyKad1_F.png .\images\MyKad2_F.jpg .\images\MyKad3_F.jpg   
            return Task.Run(() =>
            {
                foreach (string imageFileName in args)
                {
                    try
                    {
                        ScanPHUMIDResult scanPHUMIDResult = ScanIDOCR.ScanPHUMID(BASEADDR_URL, imageFileName);
                        Console.WriteLine($"scanPHUMIDResult.Success: {scanPHUMIDResult.Success}");
                        Console.WriteLine($"scanPHUMIDResult.Error: {scanPHUMIDResult.Error}");
                        Console.WriteLine($"scanPHUMIDResult.lastNameOrFullName: {scanPHUMIDResult.lastNameOrFullName}");
                        Console.WriteLine($"scanPHUMIDResult.documentNumber: {scanPHUMIDResult.documentNumber}");
                        Console.WriteLine($"scanPHUMIDResult.addressLine1: {scanPHUMIDResult.addressLine1}");
                        Console.WriteLine($"scanPHUMIDResult.addressLine2: {scanPHUMIDResult.addressLine2}");
                        Console.WriteLine($"scanPHUMIDResult.postcode: {scanPHUMIDResult.postcode}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ScanPHUMID exception: {ex}");
                    }
#if false
                    // Try Tesseract
                    if (!System.IO.File.Exists(imageFileName))
                    {
                        Console.WriteLine("File not found: " + imageFileName);
                        continue;
                    }

                    Bitmap bmpImage = new Bitmap(imageFileName);
                    int width = bmpImage.Width;
                    int height = bmpImage.Height;

                    string b64Image = "";
                    using (var stream = System.IO.File.OpenRead(imageFileName))
                    {
                        byte[] b = new byte[stream.Length];
                        stream.Read(b, 0, b.Length);
                        b64Image = Convert.ToBase64String(b);
                    }

                    if (b64Image.Length == 0)
                    {
                        Console.WriteLine("File is empty: " + imageFileName);
                        continue;
                    }

                    Console.WriteLine($"[{imageFileName}]");
                    {
                        DateTime dtStart = DateTime.Now;
                        //string retOcr = PostOCRWithRegionRequest(b64Image);
                        //Console.WriteLine(retOcr);
                        //List<Line> lines = ScanID.PostOCRWithRegionRequest(BASEADDR_URL, b64Image);
                        List<Line> linesTess = ScanIDOCR.OCRLinesWithTesseractB64(b64Image);
                        DateTime dtEnd = DateTime.Now;
                        Console.WriteLine($"({(dtEnd - dtStart).TotalSeconds} sec)\n");

                        // remove </s> from the start of 1st line
                        if (linesTess.Count > 0 && linesTess[0].Text.StartsWith("</s>"))
                        {
                            linesTess[0].Text = linesTess[0].Text.Replace("</s>", "");
                        }

                        Console.WriteLine("Lines extrected by Tesseract:");
                        List<Line> linesTessRemoveDuplicated = new List<Line>();
                        Dictionary<string, Line> linesTessDict = new Dictionary<string, Line>();
                        foreach (Line line in linesTess)
                        {
                            if (linesTessDict.ContainsKey(line.Text))
                            {
                                Console.WriteLine($"  {line.Text} --> duplicated");
                            }
                            else
                            {
                                Console.WriteLine($"  {line.Text}");
                                linesTessDict[line.Text] = line;
                            }
                        }

                        linesTessRemoveDuplicated = linesTessDict.Values.ToList();

                        ScanPHUMIDResult scanPHUMIDResult = ScanIDOCR.ExtractFieldsFromReadResultOfPHUMID(linesTessRemoveDuplicated);
                        Console.WriteLine($"scanPHUMIDResult.Success: {scanPHUMIDResult.Success}");
                        Console.WriteLine($"scanPHUMIDResult.Error: {scanPHUMIDResult.Error}");
                        Console.WriteLine($"scanPHUMIDResult.lastNameOrFullName: {scanPHUMIDResult.lastNameOrFullName}");
                        Console.WriteLine($"scanPHUMIDResult.documentNumber: {scanPHUMIDResult.documentNumber}");
                        Console.WriteLine($"scanPHUMIDResult.addressLine1: {scanPHUMIDResult.addressLine1}");
                        Console.WriteLine($"scanPHUMIDResult.addressLine2: {scanPHUMIDResult.addressLine2}");
                        Console.WriteLine($"scanPHUMIDResult.postcode: {scanPHUMIDResult.postcode}");
                    }
#endif
                }
            });
        }

        static string PostOCRRequest(string b64Image)
        {
            string ret = "";
            // Create a new instance of the HttpClient class
            using (var client = new HttpClient())
            {
                // Create a new instance of the MyRequest class
                var jsonReq = new JObject();
                jsonReq["b64"] = b64Image;

                // serialize jsonReq
                var strJsonReq = jsonReq.ToString();

                // Create a new instance of the StringContent class
                var content = new StringContent(strJsonReq, Encoding.UTF8, "application/json");

                // Post the request to the web service
                //var response = client.PostAsync($"{BASEADDR_URL}ocrB64", content).GetAwaiter().GetResult();
                DateTime dtStart = DateTime.Now;
                var response = client.PostAsync($"{BASEADDR_URL}ocr", content).GetAwaiter().GetResult();
                DateTime dtEnd = DateTime.Now;

                Console.WriteLine($"PostOCRRequest ({(dtEnd - dtStart).TotalSeconds} sec)\n");

                // Check the status code of the response
                if (response.IsSuccessStatusCode)
                {
                    // Get the response content
                    var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    // Deserialize the response content to a string
                    var jsonRes = JObject.Parse(responseBody);
                    if (jsonRes.ContainsKey("<OCR>"))
                    {
                        ret = jsonRes["<OCR>"].ToString();
                        //Console.WriteLine("<OCR>:" + ret);
                    }
                    else
                    {
                        Console.WriteLine(responseBody);
                    }
                }
                else
                {
                    Console.WriteLine("Error: " + response.StatusCode);
                    throw new Exception("Error: " + response.StatusCode);
                }
            }
            return ret;
        }


#if false
        static List<Line> OCRWithTesseract(string b64Image)
        {
            List<Line> lines = new List<Line>();
            TesseractEngine tesseractEngine = new TesseractEngine("./tessdata", "eng", EngineMode.Default);

            using (var image = Pix.LoadFromMemory(Convert.FromBase64String(b64Image)))
            {
                using (var page = tesseractEngine.Process(image))
                {
                    //ret = page.GetText();
                    ResultIterator ri = page.GetIterator();
                    ri.Begin();

                    do
                    {
                        string text = ri.GetText(PageIteratorLevel.Block);
                        Console.WriteLine(text);
                        Rect rcBoundingBox;
                        if(ri.TryGetBoundingBox(PageIteratorLevel.Block, out rcBoundingBox))
                        {
                            Line line = new Line();
                            line.Text = text;
                            line.BoundingBox = new List<double?> { (double)rcBoundingBox.X1, (double)rcBoundingBox.Y1, (double)rcBoundingBox.X2, (double)rcBoundingBox.Y1,
                                    (double)rcBoundingBox.X2, (double)rcBoundingBox.Y2, (double)rcBoundingBox.X1, (double)rcBoundingBox.Y2 }; 
                            lines.Add(line);
                        }
                    } while (ri.Next(PageIteratorLevel.TextLine));
                }
            }

            return lines;
        }
#endif
        /*
cuda:0
[.\images\MYDL1_s.jpg]
{
  "labels": [
    "</s>LESEN MEMANDU",
    "DRIVING LICENCE",
    "MALAYSIA",
    "TAKUMI TATEISHI",
    "JPN",
    "TZ114505JPN",
    "82 D",
    "1909-2018 - 18 04 2021",
    "12-12 CITY TOWER",
    "JNL ALOR BYT BINTANG",
    "SOLDEN KUALA LUMPUR",
    "WALKATI PERSENUTUAN KUALAL LUMPURI"
  ],
  "quad_boxes": [
    [
      190.63200378417969,
      316.57601928710938,
      284.23199462890625,
      316.57601928710938,
      284.23199462890625,
      326.55999755859375,
      190.63200378417969,
      326.55999755859375
    ],
    [
      190.63200378417969,
      327.39199829101562,
      284.23199462890625,
      327.39199829101562,
      284.23199462890625,
      336.54400634765625,
      190.63200378417969,
      336.54400634765625
    ],
    [
      347.8800048828125,
      316.57601928710938,
      429.0,
      316.57601928710938,
      429.0,
      331.552001953125,
      347.8800048828125,
      331.552001953125
    ],
    [
      253.03199768066406,
      349.02401733398438,
      354.7440185546875,
      349.02401733398438,
      354.7440185546875,
      360.6719970703125,
      253.03199768066406,
      360.6719970703125
    ],
    [
      254.27999877929688,
      381.47201538085938,
      274.24801635742188,
      381.47201538085938,
      274.24801635742188,
      390.62399291992188,
      254.27999877929688,
      390.62399291992188
    ],
    [
      334.7760009765625,
      380.6400146484375,
      407.16000366210938,
      380.6400146484375,
      407.16000366210938,
      391.45599365234375,
      334.7760009765625,
      391.45599365234375
    ],
    [
      254.27999877929688,
      394.78399658203125,
      289.2239990234375,
      394.78399658203125,
      289.2239990234375,
      403.10400390625,
      254.27999877929688,
      403.10400390625
    ],
    [
      255.52799987792969,
      428.89599609375,
      363.48001098632812,
      428.89599609375,
      363.48001098632812,
      438.8800048828125,
      255.52799987792969,
      438.8800048828125
    ],
    [
      255.52799987792969,
      448.864013671875,
      354.7440185546875,
      448.864013671875,
      354.7440185546875,
      458.8480224609375,
      255.52799987792969,
      458.8480224609375
    ],
    [
      255.52799987792969,
      461.34402465820312,
      373.46401977539062,
      461.34402465820312,
      373.46401977539062,
      471.3280029296875,
      255.52799987792969,
      471.3280029296875
    ],
    [
      255.52799987792969,
      472.99200439453125,
      363.48001098632812,
      472.99200439453125,
      363.48001098632812,
      482.97601318359375,
      255.52799987792969,
      482.97601318359375
    ],
    [
      254.27999877929688,
      485.47201538085938,
      458.95199584960938,
      484.6400146484375,
      458.95199584960938,
      495.45602416992188,
      254.27999877929688,
      496.28802490234375
    ]
  ]
}
(30.8796109 sec)

[.\images\MYDL1.jpg]
{
  "labels": [
    "</s>LESEN MEMANDU",
    "DRIVING LICENCE",
    "MALAYSIA",
    "TAKUMI TATEISHI",
    "JPN",
    "TZ114505JPN",
    "82 D",
    "19/03/2016 - 18/04/2021",
    "12-12 CITY TOWER",
    "JNL ALOR BYT BINTANG",
    "SOLOR KUALA LUMPUR",
    "WILATI PENSENUTIAN KUALAL LUMPURI"
  ],
  "quad_boxes": [
    [
      953.15997314453125,
      1582.8798828125,
      1421.159912109375,
      1582.8798828125,
      1421.159912109375,
      1632.7999267578125,
      953.15997314453125,
      1632.7999267578125
    ],
    [
      953.15997314453125,
      1636.9599609375,
      1421.159912109375,
      1636.9599609375,
      1421.159912109375,
      1682.719970703125,
      953.15997314453125,
      1682.719970703125
    ],
    [
      1742.5198974609375,
      1587.0399169921875,
      2141.8798828125,
      1587.0399169921875,
      2141.8798828125,
      1657.7598876953125,
      1742.5198974609375,
      1657.7598876953125
    ],
    [
      1265.159912109375,
      1749.2799072265625,
      1770.5999755859375,
      1749.2799072265625,
      1770.5999755859375,
      1803.3599853515625,
      1265.159912109375,
      1803.3599853515625
    ],
    [
      1271.39990234375,
      1907.3599853515625,
      1368.1199951171875,
      1907.3599853515625,
      1368.1199951171875,
      1953.119873046875,
      1271.39990234375,
      1953.119873046875
    ],
    [
      1673.8798828125,
      1903.199951171875,
      2032.679931640625,
      1903.199951171875,
      2032.679931640625,
      1957.2799072265625,
      1673.8798828125,
      1957.2799072265625
    ],
    [
      1271.39990234375,
      2019.679931640625,
      1386.8399658203125,
      2019.679931640625,
      1386.8399658203125,
      2065.43994140625,
      1271.39990234375,
      2065.43994140625
    ],
    [
      1271.39990234375,
      2144.47998046875,
      1817.39990234375,
      2144.47998046875,
      1817.39990234375,
      2194.39990234375,
      1271.39990234375,
      2194.39990234375
    ],
    [
      1271.39990234375,
      2244.31982421875,
      1770.5999755859375,
      2244.31982421875,
      1770.5999755859375,
      2294.239990234375,
      1271.39990234375,
      2294.239990234375
    ],
    [
      1271.39990234375,
      2306.719970703125,
      1864.199951171875,
      2306.719970703125,
      1864.199951171875,
      2356.639892578125,
      1271.39990234375,
      2356.639892578125
    ],
    [
      1271.39990234375,
      2364.9599609375,
      1811.159912109375,
      2364.9599609375,
      1811.159912109375,
      2414.8798828125,
      1271.39990234375,
      2414.8798828125
    ],
    [
      1271.39990234375,
      2431.52001953125,
      2288.52001953125,
      2423.199951171875,
      2288.52001953125,
      2477.280029296875,
      1271.39990234375,
      2481.43994140625
    ]
  ]
}
(23.3986116 sec)

[.\images\MYDL2.png]
{
  "labels": [
    "</s>LESEN MEMANDU",
    "DRIVING LICENCE",
    "MALAYSIA",
    "TAKUMI TATEISHI",
    "Wargargara / Nationality No. Penangaln / Identity No.",
    "JPN",
    "TZ1145051 JPN",
    "Class",
    "B2 D",
    "Tempo/Moblity",
    "19/02/2016 - 18/04/2021",
    "Alamat / Address",
    "42-12F CITY TOWER",
    "JNL ALOR BKT BINTANG",
    "50200 KUALA LUMPUR",
    "WILAYAY PERSEKUTUAN KUALAL LUMPURA"
  ],
  "quad_boxes": [
    [
      913.24798583984375,
      778.67999267578125,
      1566.4320068359375,
      778.67999267578125,
      1566.4320068359375,
      845.2080078125,
      913.24798583984375,
      845.2080078125
    ],
    [
      917.280029296875,
      854.27996826171875,
      1566.4320068359375,
      854.27996826171875,
      1566.4320068359375,
      911.7359619140625,
      917.280029296875,
      911.7359619140625
    ],
    [
      1997.8560791015625,
      796.823974609375,
      2542.176025390625,
      796.823974609375,
      2542.176025390625,
      890.5679931640625,
      1997.8560791015625,
      890.5679931640625
    ],
    [
      1344.6719970703125,
      1005.47998046875,
      2042.2080078125,
      1005.47998046875,
      2042.2080078125,
      1084.10400390625,
      1344.6719970703125,
      1084.10400390625
    ],
    [
      1340.6400146484375,
      1168.7760009765625,
      2465.568115234375,
      1168.7760009765625,
      2465.568115234375,
      1214.135986328125,
      1340.6400146484375,
      1214.135986328125
    ],
    [
      1344.6719970703125,
      1223.2080078125,
      1485.7919921875,
      1223.2080078125,
      1485.7919921875,
      1283.68798828125,
      1344.6719970703125,
      1283.68798828125
    ],
    [
      1909.1519775390625,
      1223.2080078125,
      2401.05615234375,
      1223.2080078125,
      2401.05615234375,
      1286.7119140625,
      1909.1519775390625,
      1286.7119140625
    ],
    [
      1340.6400146484375,
      1323.0,
      1582.56005859375,
      1323.0,
      1582.56005859375,
      1365.3359375,
      1340.6400146484375,
      1365.3359375
    ],
    [
      1340.6400146484375,
      1380.4559326171875,
      1505.9520263671875,
      1380.4559326171875,
      1505.9520263671875,
      1440.9359130859375,
      1340.6400146484375,
      1440.9359130859375
    ],
    [
      1340.6400146484375,
      1480.2479248046875,
      1683.3599853515625,
      1480.2479248046875,
      1683.3599853515625,
      1522.583984375,
      1340.6400146484375,
      1522.583984375
    ],
    [
      1340.6400146484375,
      1552.823974609375,
      2098.656005859375,
      1552.823974609375,
      2098.656005859375,
      1616.3280029296875,
      1340.6400146484375,
      1616.3280029296875
    ],
    [
      1340.6400146484375,
      1634.471923828125,
      1687.3919677734375,
      1634.471923828125,
      1687.3919677734375,
      1676.8079833984375,
      1340.6400146484375,
      1676.8079833984375
    ],
    [
      1340.6400146484375,
      1694.951904296875,
      2038.176025390625,
      1694.951904296875,
      2038.176025390625,
      1755.4320068359375,
      1340.6400146484375,
      1755.4320068359375
    ],
    [
      1340.6400146484375,
      1779.6239013671875,
      2167.199951171875,
      1779.6239013671875,
      2167.199951171875,
      1843.1279296875,
      1340.6400146484375,
      1843.1279296875
    ],
    [
      1340.6400146484375,
      1864.2958984375,
      2098.656005859375,
      1861.27197265625,
      2098.656005859375,
      1924.7760009765625,
      1340.6400146484375,
      1927.7999267578125
    ],
    [
      1340.6400146484375,
      1948.9678955078125,
      2755.8720703125,
      1942.919921875,
      2755.8720703125,
      2012.471923828125,
      1340.6400146484375,
      2015.4959716796875
    ]
  ]
}
(32.2641117 sec)

[.\images\MYDL2.jpg]
{
  "labels": [
    "</s>LESEN MEMANDU",
    "DRIVING LICENCE",
    "MALAYSIA",
    "TAKUMI TATEISHI",
    "Wargargara / Nationality No. Penangalady / Identity No.",
    "JPN",
    "TZ1145051 JPN",
    "Class",
    "B2 D",
    "Tempo/Moblity",
    "19/02/2016 - 18/04/2021",
    "Alamat / Address",
    "42-12F CITY TOWER",
    "JNL ALOR BKT BINTANG",
    "50200 KUALA LUMPUR",
    "WILAYAY PERSEKUTUAN KUALAL LUMPURA"
  ],
  "quad_boxes": [
    [
      913.24798583984375,
      778.67999267578125,
      1566.4320068359375,
      778.67999267578125,
      1566.4320068359375,
      845.2080078125,
      913.24798583984375,
      845.2080078125
    ],
    [
      917.280029296875,
      854.27996826171875,
      1566.4320068359375,
      854.27996826171875,
      1566.4320068359375,
      911.7359619140625,
      917.280029296875,
      911.7359619140625
    ],
    [
      2001.8880615234375,
      796.823974609375,
      2542.176025390625,
      796.823974609375,
      2542.176025390625,
      890.5679931640625,
      2001.8880615234375,
      890.5679931640625
    ],
    [
      1344.6719970703125,
      1005.47998046875,
      2042.2080078125,
      1005.47998046875,
      2042.2080078125,
      1084.10400390625,
      1344.6719970703125,
      1084.10400390625
    ],
    [
      1340.6400146484375,
      1168.7760009765625,
      2465.568115234375,
      1168.7760009765625,
      2465.568115234375,
      1214.135986328125,
      1340.6400146484375,
      1214.135986328125
    ],
    [
      1344.6719970703125,
      1223.2080078125,
      1485.7919921875,
      1223.2080078125,
      1485.7919921875,
      1283.68798828125,
      1344.6719970703125,
      1283.68798828125
    ],
    [
      1909.1519775390625,
      1223.2080078125,
      2401.05615234375,
      1223.2080078125,
      2401.05615234375,
      1286.7119140625,
      1909.1519775390625,
      1286.7119140625
    ],
    [
      1340.6400146484375,
      1323.0,
      1582.56005859375,
      1323.0,
      1582.56005859375,
      1365.3359375,
      1340.6400146484375,
      1365.3359375
    ],
    [
      1340.6400146484375,
      1380.4559326171875,
      1505.9520263671875,
      1380.4559326171875,
      1505.9520263671875,
      1440.9359130859375,
      1340.6400146484375,
      1440.9359130859375
    ],
    [
      1340.6400146484375,
      1480.2479248046875,
      1683.3599853515625,
      1480.2479248046875,
      1683.3599853515625,
      1522.583984375,
      1340.6400146484375,
      1522.583984375
    ],
    [
      1340.6400146484375,
      1552.823974609375,
      2098.656005859375,
      1549.7999267578125,
      2098.656005859375,
      1613.303955078125,
      1340.6400146484375,
      1616.3280029296875
    ],
    [
      1340.6400146484375,
      1634.471923828125,
      1687.3919677734375,
      1634.471923828125,
      1687.3919677734375,
      1676.8079833984375,
      1340.6400146484375,
      1676.8079833984375
    ],
    [
      1340.6400146484375,
      1694.951904296875,
      2038.176025390625,
      1694.951904296875,
      2038.176025390625,
      1755.4320068359375,
      1340.6400146484375,
      1755.4320068359375
    ],
    [
      1340.6400146484375,
      1779.6239013671875,
      2167.199951171875,
      1779.6239013671875,
      2167.199951171875,
      1843.1279296875,
      1340.6400146484375,
      1843.1279296875
    ],
    [
      1340.6400146484375,
      1864.2958984375,
      2098.656005859375,
      1864.2958984375,
      2098.656005859375,
      1927.7999267578125,
      1340.6400146484375,
      1927.7999267578125
    ],
    [
      1340.6400146484375,
      1948.9678955078125,
      2755.8720703125,
      1942.919921875,
      2755.8720703125,
      2012.471923828125,
      1340.6400146484375,
      2015.4959716796875
    ]
  ]
}
(31.9238823 sec)
        */
    }
}
