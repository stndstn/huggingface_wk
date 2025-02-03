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
                        List<Line> lines = PostOCRWithRegionRequest(b64Image);
                        //List<Line> linesTess = OCRWithTesseract(b64Image);
                        DateTime dtEnd = DateTime.Now;
                        Console.WriteLine($"({(dtEnd - dtStart).TotalSeconds} sec)\n");

                        // remove </s> from the start of 1st line
                        if(lines.Count > 0 && lines[0].Text.StartsWith("</s>"))
                        {
                            lines[0].Text = lines[0].Text.Replace("</s>", "");
                        }

                        foreach (Line line in lines)
                        {
                            Console.WriteLine(line.Text);
                        }

                        ScanMYDLResult scanMYDLResult = ExtractFieldsFromReadResultOfMYDL(lines, width);
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
                        List<Line> lines = PostOCRWithRegionRequest(b64Image);
                        //List<Line> linesTess = OCRWithTesseract(b64Image);
                        DateTime dtEnd = DateTime.Now;
                        Console.WriteLine($"({(dtEnd - dtStart).TotalSeconds} sec)\n");

                        // remove </s> from the start of 1st line
                        if (lines.Count > 0 && lines[0].Text.StartsWith("</s>"))
                        {
                            lines[0].Text = lines[0].Text.Replace("</s>", "");
                        }

                        foreach (Line line in lines)
                        {
                            Console.WriteLine(line.Text);
                        }

                        ScanMyKadResult scanMyKadResult = ExtractFieldsFromReadResultOfMyKad(lines);
                        Console.WriteLine($"scanMyKadResult.Success: {scanMyKadResult.Success}");
                        Console.WriteLine($"scanMyKadResult.Error: {scanMyKadResult.Error}");
                        Console.WriteLine($"scanMyKadResult.lastNameOrFullName: {scanMyKadResult.lastNameOrFullName}");
                        Console.WriteLine($"scanMyKadResult.documentNumber: {scanMyKadResult.documentNumber}");
                        Console.WriteLine($"scanMyKadResult.addressLine1: {scanMyKadResult.addressLine1}");
                        Console.WriteLine($"scanMyKadResult.addressLine2: {scanMyKadResult.addressLine2}");
                        Console.WriteLine($"scanMyKadResult.postcode: {scanMyKadResult.postcode}");
                    }
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

        static List<Line> PostOCRWithRegionRequest(string b64Image)
        {
            string ret = "";
            List<Line> lines = new List<Line>();
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
                //var response = client.PostAsync($"{BASEADDR_URL}ocrWithRegionB64", content).GetAwaiter().GetResult();
                DateTime dtStart = DateTime.Now;
                var response = client.PostAsync($"{BASEADDR_URL}ocrWithRegion", content).GetAwaiter().GetResult();
                DateTime dtEnd = DateTime.Now;

                Console.WriteLine($"PostOCRWithRegionRequest ({(dtEnd - dtStart).TotalSeconds} sec)\n");

                // Check the status code of the response
                if (response.IsSuccessStatusCode)
                {
                    // Get the response content
                    var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    // Deserialize the response content to a string
                    var jsonRes = JObject.Parse(responseBody);
                    if (jsonRes.ContainsKey("<OCR_WITH_REGION>"))
                    {
                        ret = jsonRes["<OCR_WITH_REGION>"].ToString();
                        //Console.WriteLine("<OCR_WITH_REGION>:" + ret);
                        JObject jsonRet = JObject.Parse(ret);
                        JArray labels = (JArray)jsonRet["labels"];
                        JArray boxes = (JArray)jsonRet["quad_boxes"];
                        for (int i = 0; i < labels.Count; i++)
                        {
                            Console.WriteLine(labels[i]);
                            Console.WriteLine(boxes[i]);
                            Line line = new Line();
                            line.Text = labels[i].ToString();
                            JArray jsonBoundingBox = (JArray)boxes[i];
                            List<double?> boundingBox = new List<double?>();
                            for (int j = 0; j < jsonBoundingBox.Count; j++)
                            {
                                boundingBox.Add((double)jsonBoundingBox[j]);
                            }
                            line.BoundingBox = boundingBox;
                            lines.Add(line);
                        }
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
            return lines;
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
                            line.BoundingBox = new List<double?> { (double)rcBoundingBox.X1, (double)rcBoundingBox.X1, (double)rcBoundingBox.Y1, (double)rcBoundingBox.X2, (double)rcBoundingBox.Y1,
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

        public static ScanMYDLResult ExtractFieldsFromReadResultOfMYDL(IList<Line> linesAll, int widthImageOriginal)
        {
            LabelInfo labelLESEN_MEMANDU = new LabelInfo("LESEN MEMANDU");
            LabelInfo labelMALAYSIA = new LabelInfo("MALAYSIA");
            LabelInfo labelDRIVING_LICENCE = new LabelInfo("DRIVING LICENCE");
            LabelInfo labelWarganegara_Nationality = new LabelInfo("Warganegara / Nationality");
            LabelInfo labelNo_Pengenalan_Identity_No = new LabelInfo("No. Pengenalan / Identity No.");
            LabelInfo labelKelas_Class = new LabelInfo("Kelas / Class");
            LabelInfo labelTempoh_Validity = new LabelInfo("Tempoh / Validity");
            LabelInfo labelAlamat_Address = new LabelInfo("Alamat / Address");

            ScanMYDLResult result = new ScanMYDLResult();

            string IDNUM = "";
            string NATIONALITY = "";
            string NAME = "";
            string CLASS = "";
            string VALID_FROM = "";
            string VALID_UNTIL = "";
            string ADDRESS1 = "";
            string ADDRESS2 = "";
            string ADDRESS3 = "";
            string POSTCODE = "";
            string CITY = "";
            string STATE = "";

            Regex regexValidFromValidUntil = new Regex(@"\d{1,2}/\d{1,2}/\d{4} - \d{1,2}/\d{1,2}/\d{4}");
            Regex regexNationality = new Regex(@"^[a-zA-Z]{3}$|^MALAYSIA$");
            Regex regexFiveDigitsNumber = new Regex(@"^\d{5}$"); 
            char[] separatorBlank = { ' ' };

            List<Line> linesField = new List<Line>();   // lines valid and not label
            //List<Line> linesFieldOrLabel = new List<Line>();   // lines valid and not label
            foreach (Line line in linesAll)
            {
                string text = line.Text.Trim();
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] linesAll {line.Text} Height:{line.ExtGetHeight()}");

                double? angle = line.ExtGetAngle();
                if (angle == null || Math.Abs((decimal)angle) > 10)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   angle:{angle} > 10 --> ignored");
                    continue;
                }

                if (!labelLESEN_MEMANDU.IsLabelFound)
                {
                    if (labelLESEN_MEMANDU.MatchTitleExactly(line))
                        continue;
                }
                if (!labelNo_Pengenalan_Identity_No.IsLabelFound)
                {
                    if (labelNo_Pengenalan_Identity_No.MatchTitleExactly(line))
                        continue;
                }
                if (!labelDRIVING_LICENCE.IsLabelFound)
                {
                    if (labelDRIVING_LICENCE.MatchTitleExactly(line))
                        continue;
                }
                if (!labelWarganegara_Nationality.IsLabelFound)
                {
                    if (labelWarganegara_Nationality.MatchTitleExactly(line))
                        continue;
                }
                if (!labelMALAYSIA.IsLabelFound)
                {
                    if (labelMALAYSIA.MatchTitleExactly(line))
                        continue;
                }
                if (!labelKelas_Class.IsLabelFound)
                {
                    if (labelKelas_Class.MatchTitleExactly(line))
                        continue;
                }
                if (!labelTempoh_Validity.IsLabelFound)
                {
                    if (labelTempoh_Validity.MatchTitleExactly(line))
                        continue;
                }
                if (!labelAlamat_Address.IsLabelFound)
                {
                    if (labelAlamat_Address.MatchTitleExactly(line))
                        continue;
                }

                //linesFieldOrLabel.Add(line);
                linesField.Add(line);
            }// foreach lines in other columns
            /*
            foreach (Line line in linesFieldOrLabel)
            {
                string text = line.Text.Trim();
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] linesFieldOrLabel {line.Text} Height:{line.ExtGetHeight()}");

                double? angle = line.ExtGetAngle();
                if (angle == null || Math.Abs((decimal)angle) > 10)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   angle:{angle} > 10 --> ignored");
                    continue;
                }

                if (!labelLESEN_MEMANDU.IsLabelFound)
                {
                    if (labelLESEN_MEMANDU.MatchTitle(line))
                        continue;
                }
                if (!labelNo_Pengenalan_Identity_No.IsLabelFound)
                {
                    if (labelNo_Pengenalan_Identity_No.MatchTitle(line))
                        continue;
                }
                if (!labelDRIVING_LICENCE.IsLabelFound)
                {
                    if (labelDRIVING_LICENCE.MatchTitle(line))
                        continue;
                }
                if (!labelWarganegara_Nationality.IsLabelFound)
                {
                    if (labelWarganegara_Nationality.MatchTitle(line))
                        continue;
                }
                if (!labelMALAYSIA.IsLabelFound)
                {
                    if (labelMALAYSIA.MatchTitle(line))
                        continue;
                }
                if (!labelKelas_Class.IsLabelFound)
                {
                    if (labelKelas_Class.MatchTitle(line))
                        continue;
                }
                if (!labelTempoh_Validity.IsLabelFound)
                {
                    if (labelTempoh_Validity.MatchTitle(line))
                        continue;
                }
                if (!labelAlamat_Address.IsLabelFound)
                {
                    if (labelAlamat_Address.MatchTitle(line))
                        continue;
                }

                linesField.Add(line);
            }// foreach lines in other columns
            */

            int countLinesField = linesField.Count;
            if (countLinesField > 0)
            {
                // classify fields into main column and other (right aligned) columns
                // main column contains: NAME, NATIONALITY, CLASS, VALID_FROM, VALID_UNTIL, ADDRESS1, ADDRESS2, ADDRESS3, POSTCODE, CITY, STATE
                // other columns contains: IDNUM

                int idxMedianLinesField = countLinesField / 2;
                var linesLeftOrder = linesField.OrderBy(l => l.BoundingBox[0]);
                double? leftMedian = linesLeftOrder.ElementAt(idxMedianLinesField).BoundingBox[0];
                double? leftMiddleOfFields = leftMedian;

                if(labelLESEN_MEMANDU.IsLabelFound && labelMALAYSIA.IsLabelFound)
                {
                    double? middleOfFields = (labelLESEN_MEMANDU.Right + labelMALAYSIA.Left) / 2;
                    if (middleOfFields.HasValue)
                        leftMiddleOfFields = middleOfFields;
                }
                else if (labelDRIVING_LICENCE.IsLabelFound && labelMALAYSIA.IsLabelFound)
                {
                    double? middleOfFields = (labelDRIVING_LICENCE.Right + labelMALAYSIA.Left) / 2;
                    if (middleOfFields.HasValue)
                        leftMiddleOfFields = middleOfFields;
                }

                double? leftEdgeOfBlock = linesField.Min(l => l.BoundingBox[0]);
                double? rightEdgeOfBlock = linesField.Max(l => l.BoundingBox[2]);
                double? topEdgeOfBlock = linesField.Min(l => l.BoundingBox[1]);
                double? bottomEdgeOfBlock = linesField.Max(l => l.BoundingBox[5]);
                double? sumLeft = linesLeftOrder.Take(5).Sum(l => l.BoundingBox[0]);
                double? avgLeft = sumLeft / 5;
                double? acceptableRangeOfLeftEdge = (rightEdgeOfBlock - leftEdgeOfBlock) / 20;
                double? h_center = leftEdgeOfBlock + (rightEdgeOfBlock - leftEdgeOfBlock) / 2;
                double? v_center = topEdgeOfBlock + (bottomEdgeOfBlock - topEdgeOfBlock) / 2;
                double? h_leftSideEdge = leftEdgeOfBlock + (rightEdgeOfBlock - leftEdgeOfBlock) / 3;

                var linesInMainColumn = linesField.Where(l => (decimal)l.BoundingBox[0] <= (decimal)leftMiddleOfFields);
                var linesOutOfMainColumn = linesField.Where(l => (decimal)l.BoundingBox[0] > (decimal)leftMiddleOfFields);

                double? heightName = null;
                double? bottomName = null;
                int numLinesInMainColumn = linesInMainColumn.Count();
                int idxMainColumn = 0;
                // sort from top to bottom
                linesInMainColumn.OrderBy(l => l.BoundingBox[1]);

                // find fields from main column
                foreach (Line line in linesInMainColumn)
                {
                    string text = line.Text.Trim();

                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} Height:{line.ExtGetHeight()}");

                    if (heightName.HasValue)
                    {
                        if (line.ExtGetHeight() < heightName * 0.65)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   Height:{line.ExtGetHeight()} < heightName:{heightName} * 0.65 = {heightName * 0.65} --> ignored");
                            numLinesInMainColumn--;
                            continue;
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] idxMainColumn:{idxMainColumn} numLinesInMainColumn:{numLinesInMainColumn}");
                    // the 2nd last line is postcode and city
                    if (idxMainColumn + 2 == numLinesInMainColumn)
                    {
                        // POSTCODE CITY
                        string postcode_city = text;

                        string[] token = postcode_city.Split(separatorBlank, 2);
                        if (token.Length > 1)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> POSTCODE: {token[0]} CITY: {token[1]}");
                            if (regexFiveDigitsNumber.Match(token[0]).Success)
                            {
                                POSTCODE = token[0];
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] !!! {token[0]} is not valid POSTCODE !!!");
                            }

                            CITY = token[1];
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> city_postcode: {postcode_city}");
                        }
                        idxMainColumn++;
                        continue;
                    }
                    // the last line is state
                    if (idxMainColumn + 1 == numLinesInMainColumn)
                    {
                        // STATE
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> STATE");
                        STATE = line.Text;
                        idxMainColumn++;
                        continue;
                    }

                    if (string.IsNullOrEmpty(NAME))
                    {
                        // NAME is under DRIVING_LICENCE
                        if (labelDRIVING_LICENCE.IsLabelFound &&
                            ((double)(line.BoundingBox[1].Value - labelDRIVING_LICENCE.Bottom) > 0
                            && (double)(line.BoundingBox[1].Value - labelDRIVING_LICENCE.Bottom) < labelDRIVING_LICENCE.Height * 4
                            && labelDRIVING_LICENCE.Height < line.ExtGetHeight())
                            )
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> NAME");
                            NAME = line.Text;
                            heightName = line.ExtGetHeight();
                            bottomName = line.ExtGetBottom();
                            idxMainColumn++;
                            continue;
                        }
                    }

                    if (string.IsNullOrEmpty(NATIONALITY))
                    {
                        // Warganegara_Nationality
                        if (/*labelWarganegara_Nationality.IsLabelFound
                            && labelWarganegara_Nationality.IsFieldJustUnderTheLabel(line)*/
                            regexNationality.Match(line.Text).Success
                            )
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> NATIONALITY");
                            // (CITIZENSHIP) nationality is "MALAYSIA" or 3 letter code
                            if (CheckCharInLine(line, "MALAYSIA"))
                            {
                                NATIONALITY = "MY";
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MY --> NATIONALITY");
                            }
                            else
                            {
                                NATIONALITY = line.Text;
                            }
                            idxMainColumn++;
                            continue;
                        }
                    }

                    if (string.IsNullOrEmpty(CLASS))
                    {
                        // check if the line is field of 'Kelas/Class'
                        // https://en.wikipedia.org/wiki/Driving_licence_in_Malaysia#Classes
                        // A, A1, B, B1, B2, C, D, DA, E, E1, E2, F, G, H, I, M
                        bool isNotValueOfClass = false;
                        string[] tokens = line.Text.Split(' ');
                        foreach (string token in tokens)
                        {
                            if (token.Length > 2)
                            {
                                isNotValueOfClass = true;
                                break;
                            }

                            char c = token[0];
                            if ((c < 'A' || 'M' < c)
                                && (c != '4' /* A */ && c != '8' /* B */ && c != 'c' /* C */ && c != '1' /* I */ && c != 'l' /* I */))
                            {
                                isNotValueOfClass = true;
                                break;
                            }
                        }

                        // Kelas_Class
                        if (/*labelKelas_Class.IsLabelFound
                            && labelKelas_Class.IsFieldJustUnderTheLabel(line)*/
                            !isNotValueOfClass
                            )
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> CLASS");
                            CLASS = line.Text;
                            idxMainColumn++;
                            continue;
                        }
                    }

                    if (string.IsNullOrEmpty(VALID_FROM))
                    {
                        // Tempoh_Validity    dd/MM/yyyy - dd/MM/yyyy
                        if (/*labelTempoh_Validity.IsLabelFound && labelTempoh_Validity.IsFieldJustUnderTheLabel(line)*/
                            regexValidFromValidUntil.Match(line.Text).Success
                            )
                        {
                            string line_validity = line.Text;
                            String[] dates = line_validity.Split('-');
                            if(dates.Length == 2)
                            {
                                VALID_FROM = dates[0].Trim();
                                VALID_UNTIL = dates[1].Trim();
                                idxMainColumn++;
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> VALID_FROM:${VALID_FROM} VALID_UNTIL:${VALID_UNTIL}");
                            }
                            continue;
                        }

                        // maybe it is line of VALID_FROM, but not scanned properly
                        VALID_FROM = line.Text;
                        continue;
                    }

                    if (string.IsNullOrEmpty(ADDRESS1))
                    {
                        //if (labelAlamat_Address.IsLabelFound
                        //   && (labelAlamat_Address.IsFieldJustUnderTheLabel(line) && labelAlamat_Address.IsFieldInSameLeftEdge(line)))
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS1");
                            ADDRESS1 = line.Text;
                            idxMainColumn++;
                            continue;
                        }
                    }
                    else if (string.IsNullOrEmpty(ADDRESS2))
                    {
                        //if (labelAlamat_Address.IsLabelFound
                        //   && (labelAlamat_Address.IsFieldUnderTheLabel(line) && labelAlamat_Address.IsFieldInSameLeftEdge(line)))
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS2");
                            ADDRESS2 = line.Text;
                            idxMainColumn++;
                            continue;
                        }
                    }
                    else if (string.IsNullOrEmpty(ADDRESS3))
                    {
                        //if (labelAlamat_Address.IsLabelFound
                        //   && (labelAlamat_Address.IsFieldUnderTheLabel(line) && labelAlamat_Address.IsFieldInSameLeftEdge(line)))
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS3");
                            ADDRESS3 = line.Text;
                            idxMainColumn++;
                            continue;
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> UNKNOWN");
                    idxMainColumn++;
                }// foreach lines in main column

                // find fields from other (right aligned) column
                int numLinesOutOfMainColumn = linesOutOfMainColumn.Count();
                int idxOutOfMainColumn = 0;
                foreach (Line line in linesOutOfMainColumn)
                {
                    string text = line.Text.Trim();
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] linesOutMainColumn {line.Text} Height:{line.ExtGetHeight()}");

                    if (heightName.HasValue)
                    {
                        if (line.ExtGetHeight() < heightName * 0.65)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   Height:{line.ExtGetHeight()} < heightName:{heightName} * 0.65 = {heightName * 0.65} --> ignored");
                            numLinesOutOfMainColumn--;
                            continue;
                        }
                    }

                    if (string.IsNullOrEmpty(IDNUM))
                    {
                        // IDNUM is under NAME
                        if (!string.IsNullOrEmpty(NAME) &&
                            ((double)(line.BoundingBox[1].Value - bottomName) > 0
                            && (double)(line.BoundingBox[1].Value - bottomName) < heightName * 4
                            && Math.Abs((decimal)heightName - (decimal)line.ExtGetHeight()) < (decimal)(heightName / 2))
                            )
                        {
                            // No_Pengenalan_Identity_No
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> IDNUM");
                            IDNUM = line.Text;
                            idxOutOfMainColumn++;
                            continue;
                        }
                    }
                }// foreach lines in other columns
            } // linesField.Count > 0

            // map to result and convert format 
            List<string> lsMissingFields = new List<string>();
            // NAME -> lastNameOrFullName 
            result.lastNameOrFullName = NAME;
            if (string.IsNullOrEmpty(NAME)) lsMissingFields.Add("NAME");

            // IDNUM -> documentNumber
            result.documentNumber = IDNUM;
            if (string.IsNullOrEmpty(IDNUM)) lsMissingFields.Add("IDNUM");

            // (CITIZENSHIP) nationality is "MALAYSIA" or 3 letter code
            result.nationality = NATIONALITY;
            if (string.IsNullOrEmpty(NATIONALITY)) lsMissingFields.Add("NATIONALITY");

            // VALID_FROM "dd/MM/yyyy" -> documentIssueDate "yyyy-MM-dd"
            try
            {
                result.documentIssueDate = "";

                if (VALID_FROM.Length == 10)
                {
                    int dd = int.Parse(VALID_FROM.Substring(0, 2));
                    int MM = int.Parse(VALID_FROM.Substring(3, 2));
                    int yyyy = int.Parse(VALID_FROM.Substring(6, 4));
                    result.documentIssueDate = $"{yyyy:0000}-{MM:00}-{dd:00}";
                }
                else
                {
                    lsMissingFields.Add("VALID_FROM");
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                lsMissingFields.Add("VALID_FROM");
            }

            // VALID_UNTIL "dd/MM/yyyy" -> documentExpirationDate "yyyy-MM-dd"
            try
            {
                result.documentExpirationDate = "";
                if (VALID_UNTIL.Length == 10)
                {
                    int dd = int.Parse(VALID_UNTIL.Substring(0, 2));
                    int MM = int.Parse(VALID_UNTIL.Substring(3, 2));
                    int yyyy = int.Parse(VALID_UNTIL.Substring(6, 4));
                    result.documentExpirationDate = $"{yyyy:0000}-{MM:00}-{dd:00}";
                }
                else
                {
                    lsMissingFields.Add("VALID_UNTIL");
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                lsMissingFields.Add("VALID_UNTIL");
            }

            // ADDRESS1, ADDRESS2, ADDRESS3, CITY, STATE -> addressLine1, addressLine2
            result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
            if (string.IsNullOrEmpty(ADDRESS1)) lsMissingFields.Add("ADDRESS1");
            if (string.IsNullOrEmpty(ADDRESS3))
            {
                result.addressLine2 = $"{CITY} {STATE}";
            }
            else
            {
                result.addressLine2 = $"{ADDRESS3} {CITY} {STATE}";
            }

            // POSTCODE
            if(!string.IsNullOrEmpty(POSTCODE))
            {
                result.postcode = POSTCODE;
            }
            else
            {
                lsMissingFields.Add("POSTCODE");
            }

            // determine success or not
            if (lsMissingFields.Count == 0)
            {
                result.Success = true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExtractFieldsFromReadResultOfMYDL result NOT success");
                if (lsMissingFields.Count > 0)
                {
                    string fields = "";
                    foreach (string field in lsMissingFields)
                    {
                        if (!string.IsNullOrEmpty(fields))
                            fields += ",";
                        fields += field;
                    }
                    result.Error = $"Failed to scan [{fields}]";
                }
            }
            return result;
        }

        public static ScanMyKadResult ExtractFieldsFromReadResultOfMyKad(IList<Line> linesAll)
        {
            const string KAD_PENGENALAN = "KAD PENGENALAN";
            const string MALAYSIA = "MALAYSIA";
            const string IDENTITY_CARD = "IDENTITY CARD";
            char[] separatorBlank = { ' ' };

            ScanMyKadResult result = new ScanMyKadResult();

            //const double FILTER_WEAK_TEXT_SMALLER_THAN_IDNUM = 0.75f;
            const double FILTER_TEXT_SMALLER_COMPARE_TO_IDNUM = 0.5f;
            string IDNUM = "";
            string NAME = "";
            string ADDRESS1 = "";
            string ADDRESS2 = "";
            string ADDRESS3 = "";
            string POSTCODE = "";
            string CITY = "";
            string STATE = "";
            string CITIZENSHIP = "";
            string GENDER = "";
            string EASTMSIAN = "";
            string BIRTHDATE = "";

            var linesLeftOrder = linesAll.OrderBy(l => l.BoundingBox[0]);
            double? leftEdgeOfBlock = linesAll.Min(l => l.BoundingBox[0]);
            double? rightEdgeOfBlock = linesAll.Max(l => l.BoundingBox[2]);
            double? topEdgeOfBlock = linesAll.Min(l => l.BoundingBox[1]);
            double? bottomEdgeOfBlock = linesAll.Max(l => l.BoundingBox[5]);
            double? sumLeft = linesLeftOrder.Take(5).Sum(l => l.BoundingBox[0]);
            double? avgLeft = sumLeft / 5;
            double? acceptableRangeOfLeftEdge = (rightEdgeOfBlock - leftEdgeOfBlock) / 20;
            double? h_center = leftEdgeOfBlock + (rightEdgeOfBlock - leftEdgeOfBlock) / 2;
            double? v_center = topEdgeOfBlock + (bottomEdgeOfBlock - topEdgeOfBlock) / 2;
            double? h_leftSideEdge = leftEdgeOfBlock + (rightEdgeOfBlock - leftEdgeOfBlock) / 3;

            // pick the lines aligned to left 
            var linesLeftSide = linesAll.Where(l => l.BoundingBox[0] < h_leftSideEdge);
            if (linesLeftSide.Any())
            {
                // sort from top to bottom
                linesLeftSide = linesLeftSide.OrderBy(l => l.BoundingBox[1]);
                System.Diagnostics.Debug.WriteLine("\nLines aligned to left:");
                Regex regexIDNum = new Regex(@"\d{6}-\d{2}-\d{4}");
                int idxIdNum = -1;
                decimal heightIdNum = 0;
                Line[] arrayLinesLeftSide = linesLeftSide.ToArray();
                List<Line> lsLinesLeftSideValid = new List<Line>();
                int numLines = arrayLinesLeftSide.Length;
                for (int idx = 0; idx < arrayLinesLeftSide.Length; idx++)
                {
                    Line line = arrayLinesLeftSide[idx];
                    string text = line.Text.Trim();
                    decimal heightLine = 0;
                    if (line.BoundingBox.Count == 8 && line.BoundingBox[7].HasValue && line.BoundingBox[1].HasValue)
                    {
                        heightLine = Math.Abs((decimal)line.BoundingBox[7] - (decimal)line.BoundingBox[1]);
                    }
                    //List<double> conconfidencesOfWords = new List<double>();
                    //foreach (var word in line.Words)
                    //{
                    //    conconfidencesOfWords.Add(word.Confidence);
                    //}
                    double? angle = line.ExtGetAngle();
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {text} -> Angle:{angle}");

                    try
                    {
                        if (regexIDNum.Match(text).Success)
                        {
                            idxIdNum = idx;
                            heightIdNum = heightLine;
                            lsLinesLeftSideValid.Add(line);
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   idxIdNum:{idxIdNum} heightIdNum:{heightIdNum}");
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }

                    if (angle == null || Math.Abs((decimal)angle) > 10)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   angle:{angle} > 10 --> ignored");
                        numLines--;
                        continue;
                    }

                    if (idxIdNum == -1)
                    {
                        string strRegex = ".";
                        int countCharNotInKAD_PENGENALAN = 0;
                        int countCharInKAD_PENGENALAN = 0;
                        int countCharNotInMALAYSIA = 0;
                        int countCharInMALAYSIA = 0;
                        int countCharNotInIDENTITY_CARD = 0;
                        int countCharInIDENTITY_CARD = 0;
                        foreach (char c in text)
                        {
                            strRegex += $"{c}?";
                            if (!KAD_PENGENALAN.Contains(c))
                                countCharNotInKAD_PENGENALAN++;
                            else
                                countCharInKAD_PENGENALAN++;
                            if (!MALAYSIA.Contains(c))
                                countCharNotInMALAYSIA++;
                            else
                                countCharInMALAYSIA++;
                            if (!IDENTITY_CARD.Contains(c))
                                countCharNotInIDENTITY_CARD++;
                            else
                                countCharInIDENTITY_CARD++;
                        }
                        strRegex += ".";
                        Regex regexLine = new Regex(strRegex);
                        if (countCharNotInKAD_PENGENALAN < 3 && countCharInKAD_PENGENALAN > KAD_PENGENALAN.Length - 3 && regexLine.Match(KAD_PENGENALAN).Success)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> KAD_PENGENALAN");
                        }
                        else if (countCharNotInMALAYSIA < 3 && countCharInMALAYSIA > MALAYSIA.Length - 3 && regexLine.Match(MALAYSIA).Success)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> MALAYSIA");
                        }
                        else if (countCharNotInIDENTITY_CARD < 3 && countCharInIDENTITY_CARD > IDENTITY_CARD.Length - 3 && regexLine.Match(IDENTITY_CARD).Success)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> IDENTITY_CARD");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> UNKNOWN");
                        }
                    }
                    else
                    {
                        // lines under IDNUM contains what we need...

                        if ((double)heightLine < (double)heightIdNum * FILTER_TEXT_SMALLER_COMPARE_TO_IDNUM)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] heightLine: {heightLine} < (heightIdNum: {heightIdNum})* {FILTER_TEXT_SMALLER_COMPARE_TO_IDNUM} --> Ignore this line.");
                            numLines--;
                            continue;
                        }

                        lsLinesLeftSideValid.Add(line);
                    }
                }// foreach

                numLines = lsLinesLeftSideValid.Count;
                for (int idx = 0; idx < lsLinesLeftSideValid.Count; idx++)
                {
                    Line line = lsLinesLeftSideValid[idx];
                    string text = line.Text.Trim();

                    if (numLines - 2 == idx)
                    {
                        // the 2nd last line is POSTCODE CITY
                        string postcode_city = text;
                        string[] token = postcode_city.Split(separatorBlank, 2);
                        if (token.Length > 1)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> POSTCODE: {token[0]} CITY: {token[1]}");
                            POSTCODE = token[0];
                            CITY = token[1];
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> postcode_city: {postcode_city}");
                        }
                    }
                    else if (numLines - 1 == idx)
                    {
                        // the last line is STATE
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> STATE: {text}");
                        STATE = line.Text.Trim();
                    }
                    else
                    {
                        switch (idx)
                        {
                            case 0:
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> IDNUM");
                                IDNUM = text;
                                // DOB is first 6 digit
                                BIRTHDATE = text.Substring(0, 6);
                                break;
                            case 1:
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> NAME");
                                NAME = text;
                                break;
                            case 2:
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> ADDRESS1");
                                ADDRESS1 = text;
                                break;
                            case 3:
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> ADDRESS2");
                                ADDRESS2 = text;
                                break;
                            case 4:
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> ADDRESS3");
                                ADDRESS3 = text;
                                break;
                            default:
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> ??UNKNOWN??");
                                break;
                        }
                    }
                }
            }

            // pick the lines aligned to right 
            var linesRightButtomSide = linesAll.Where(l => l.BoundingBox[0] >= h_center && l.BoundingBox[1] >= v_center);
            if (linesRightButtomSide.Any())
            {
                const string WARGANEGARA = "WARGANEGARA";
                const string LELAKI = "LELAKI";
                const string PEREMPUAN = "PEREMPUAN";
                // sort from top to bottom
                linesRightButtomSide = linesRightButtomSide.OrderBy(l => l.BoundingBox[1]);
                System.Diagnostics.Debug.WriteLine("\nLines aligned to right:");
                int numLines = linesRightButtomSide.Count();
                for (int i = 0; i < numLines; i++)
                {
                    Line line = linesRightButtomSide.ElementAt(i);
                    string text = line.Text.Trim();
                    string strRegex = ".";
                    int countCharNotInWARGANEGARA = 0;
                    int countCharInWARGANEGARA = 0;
                    int countCharNotInLELAKI = 0;
                    int countCharInLELAKI = 0;
                    int countCharNotInPEREMPUAN = 0;
                    int countCharInPEREMPUAN = 0;
                    foreach (char c in text)
                    {
                        strRegex += $"{c}?";
                        if (!WARGANEGARA.Contains(c))
                            countCharNotInWARGANEGARA++;
                        else
                            countCharInWARGANEGARA++;
                        if (!LELAKI.Contains(c))
                            countCharNotInLELAKI++;
                        else
                            countCharInLELAKI++;
                        if (!PEREMPUAN.Contains(c))
                            countCharNotInPEREMPUAN++;
                        else
                            countCharInPEREMPUAN++;
                    }
                    strRegex += ".";

                    try
                    {
                        Regex regexLine = new Regex(strRegex);
                        if (countCharNotInWARGANEGARA < 3 && countCharInWARGANEGARA > WARGANEGARA.Length - 3 && regexLine.Match(WARGANEGARA).Success)
                        {
                            System.Diagnostics.Debug.WriteLine("--> WARGANEGARA");
                            CITIZENSHIP = text;
                        }
                        else if (countCharNotInLELAKI < 3 && countCharInLELAKI > LELAKI.Length - 3 && regexLine.Match(LELAKI).Success)
                        {
                            System.Diagnostics.Debug.WriteLine("--> LELAKI");
                            GENDER = "LELAKI";
                        }
                        else if (countCharNotInPEREMPUAN < 3 && countCharInPEREMPUAN > PEREMPUAN.Length - 3 && regexLine.Match(PEREMPUAN).Success)
                        {
                            System.Diagnostics.Debug.WriteLine("--> PEREMPUAN");
                            GENDER = "PEREMPUAN";
                        }
                        else if (text == "H" || text == "K")
                        {
                            System.Diagnostics.Debug.WriteLine("--> EAST_M");
                            EASTMSIAN = text;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("--> UNKNOWN");
                        }

                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }
            }


            // map to result and convert format 
            List<string> lsMissingFields = new List<string>();

            // NAME -> lastNameOrFullName 
            result.lastNameOrFullName = NAME;
            if (string.IsNullOrEmpty(NAME)) lsMissingFields.Add("NAME");

            // IDNUM -> documentNumber
            result.documentNumber = IDNUM;
            if (string.IsNullOrEmpty(IDNUM)) lsMissingFields.Add("IDNUM");

            // (CITIZENSHIP) nationality is "MY" (by default)

            // BIRTHDATE "yyMMdd" -> dateOfBirth "yyyy-MM-dd"
            try
            {
                if (!string.IsNullOrEmpty(BIRTHDATE))
                {
                    int yy = int.Parse(BIRTHDATE.Substring(0, 2));
                    int MM = int.Parse(BIRTHDATE.Substring(2, 2));
                    int dd = int.Parse(BIRTHDATE.Substring(4, 2));
                    //https://www.ibm.com/docs/en/i/7.2?topic=mcdtdi-conversion-2-digit-years-4-digit-years-centuries
                    // If the 2-digit year is greater than or equal to 40, the century used is 1900. In other words, 19 becomes the first 2 digits of the 4-digit year.
                    // If the 2 - digit year is less than 40, the century used is 2000.In other words, 20 becomes the first 2 digits of the 4 - digit year.
                    if (yy >= 40)
                        result.dateOfBirth = $"{(1900 + yy):0000}-{MM:00}-{dd:00}";
                    else
                        result.dateOfBirth = $"{(2000 + yy):0000}-{MM:00}-{dd:00}";
                }
                else
                {
                    lsMissingFields.Add("BIRTHDATE");
                }
            }
            catch (Exception e)
            {
                result.dateOfBirth = "";
                lsMissingFields.Add("BIRTHDATE");
            }

            // GENDER -> gender
            switch (GENDER)
            {
                case "LELAKI":
                    result.gender = "M";
                    break;
                case "PEREMPUAN":
                    result.gender = "F";
                    break;
                default:
                    result.gender = "";
                    lsMissingFields.Add("GENDER");
                    break;
            }

            result.documentExpirationDate = null;

            result.documentIssueDate = null;

            // ADDRESS1, ADDRESS2, ADDRESS3, STATE -> addressLine1, addressLine2
            if (string.IsNullOrEmpty(ADDRESS1)) lsMissingFields.Add("ADDRESS1");
            if (string.IsNullOrEmpty(ADDRESS3))
            {
                result.addressLine1 = ADDRESS1;
                result.addressLine2 = $"{ADDRESS2} {CITY} {STATE}";
            }
            else
            {
                result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                result.addressLine2 = $"{ADDRESS3} {CITY} {STATE}";
            }

            // POSTCODE
            result.postcode = POSTCODE;
            if (string.IsNullOrEmpty(POSTCODE)) lsMissingFields.Add("POSTCODE");

            // determine success or not
            if (lsMissingFields.Count == 0)
            {
                result.Success = true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExtractFieldsFromReadResultOfMyKad result NOT success");
                if (lsMissingFields.Count > 0)
                {
                    string fields = "";
                    foreach (string field in lsMissingFields)
                    {
                        if (!string.IsNullOrEmpty(fields))
                            fields += ",";
                        fields += field;
                    }
                    result.Error = $"Failed to scan [{fields}]";
                }
            }

            return result;
        }

        public static bool CheckCharInLine(Line line, string textExpected)
        {
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CheckCharInLine line:{line} value:{textExpected}");
            try
            {
                bool bRet = false;
                int countCharIn = 0;
                int countCharNotIn = 0;
                string text = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();

                string strRegex = ".";
                foreach (char c in text)
                {
                    strRegex += $"{c}?";
                    if (!textExpected.Contains(c))
                        countCharNotIn++;
                    else
                        countCharIn++;
                }

                try
                {
                    strRegex += ".";
                    Regex regexLine = new Regex(strRegex);
                    //System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] confidence.avg: {confidence.Avg} countCharNotIn: {countCharNotIn}, countCharIn: {countCharIn}, strRegex: {strRegex} Title: {textExpected}");
                    if (countCharNotIn < 3 && countCharIn > textExpected.Length - 3 && regexLine.Match(textExpected).Success)
                    {
                        bRet = true;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {textExpected}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }

                return bRet;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CheckCharInLine [{line}] exception:{ex}");
                return false;
            }
        }

        public static bool CheckCharInLine(Line line, Regex regexLine)
        {
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CheckCharInLine line:{line} regexLine:{regexLine}");
            try
            {
                bool bRet = false;
                string text = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();

                try
                {
                    //System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] confidence.avg: {confidence.Avg} countCharNotIn: {countCharNotIn}, countCharIn: {countCharIn}, strRegex: {strRegex} Title: {textExpected}");
                    Match match = regexLine.Match(text);
                    if (match.Success)
                    {
                        bRet = true;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {match.Value}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }

                return bRet;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CheckCharInLine [{line}] exception:{ex}");
                return false;
            }
        }

    }

    public class ScanIDResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }

        protected string _documentType;
        public string documentType { get { return _documentType; } }
        public bool isBackOfIDImage { get; set; } = false;

        public string lastNameOrFullName { get; set; }
        public string firstName { get; set; }
        public string middleName { get; set; }
        public string documentNumber { get; set; }
        public string nationality { get; set; }
        public string dateOfBirth { get; set; }
        public string placeOfBirth { get; set; }
        public string gender { get; set; }
        public string maritalStatus { get; set; }
        public string documentExpirationDate { get; set; }
        public string documentIssueDate { get; set; }
        public string addressLine1 { get; set; }
        public string addressLine2 { get; set; }
        public string addressTown { get; set; }
        public string postcode { get; set; }
        public string personalNumber { get; set; }

        protected string _country;
        public string country { get { return _country; } }

        public string faceImageBase64 { get; set; }

        public string extraData { get; set; }

        public string resultJsonStringOCR { get; set; }
        public string resultJsonStringImageLabeling { get; set; }

        public double? documentLandmarksProbabilityAvg { get; set; }
    }
    public class ScanMYDLResult : ScanIDResult
    {
        public ScanMYDLResult()
        {
            _documentType = "DL";
            _country = "MY";
        }
    }

    public class ScanMyKadResult : ScanIDResult
    {
        public ScanMyKadResult()
        {
            _documentType = "MY";
            _country = "MY";
            nationality = "MY";
        }
    }


    public class Line
    {
        /// <summary>
        /// Initializes a new instance of the Line class.
        /// </summary>
        public Line()
        {
        }

        /// <summary>
        /// Initializes a new instance of the Line class.
        /// </summary>
        /// <param name="boundingBox">Bounding box of a recognized
        /// line.</param>
        /// <param name="text">The text content of the line.</param>
        /// <param name="words">List of words in the text line.</param>
        /// <param name="language">The BCP-47 language code of the recognized
        /// text line. Only provided where the language of the line differs
        /// from the page's.</param>
        /// <param name="appearance">Appearance of the text line.</param>
        public Line(IList<double?> boundingBox, string text)
        {
            BoundingBox = boundingBox;
            Text = text;
        }

        /// <summary>
        /// Gets or sets bounding box of a recognized line.
        /// </summary>
        [JsonProperty(PropertyName = "boundingBox")]
        public IList<double?> BoundingBox { get; set; }

        /// <summary>
        /// Gets or sets the text content of the line.
        /// </summary>
        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }

    }

    public class LabelInfo
    {
        const string REGEX_ESCAPE_CHARS = ".+*?^$()[]{}|\\";

        public LabelInfo(string title)
        {
            _title = title;
        }
        string _title;
        Line _line;
        IList<double?> _boundingBox = new List<double?>();
        public string Title { get { return _title; } }
        double? _left;
        public double? Left { get { return _left; } }
        double? _top;
        public double? Top { get { return _top; } }
        public double? Right { get { return _left + _width; } }
        double? _bottom;
        public double? Bottom { get { return _bottom; } }
        double? _height;
        public double? Height { get { return _height; } }
        double? _width;
        public double? Width { get { return _width; } }

        bool _isLabelFound = false;
        public bool IsLabelFound { get { return _isLabelFound; } }

        public bool MatchTitleExactly(Line line)
        {
            try
            {
                bool bRet = false;
                string text = line.Text.Trim();

                try
                {
                    //System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] confidence.avg: {confidence.Avg} Title: {Title}");
                    if (line.Text.Trim().Replace('.', ' ').Replace(',', ' ').Trim() == Title.Trim().Replace('.', ' ').Replace(',', ' '))
                    {
                        bRet = true;
                        _isLabelFound = true;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {Title}");
                        _line = line;
                        _boundingBox = line.BoundingBox;
                        _left = (line.BoundingBox[0] + line.BoundingBox[6]) / 2.0;
                        _top = (line.BoundingBox[1] + line.BoundingBox[3]) / 2.0;
                        _bottom = (line.BoundingBox[7] + line.BoundingBox[5]) / 2.0;
                        _height = line.ExtGetHeight();
                        _width = line.ExtGetWidth();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] Left:{Left} Top:{Top} Bottom:{Bottom}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }

                return bRet;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTitleExactly [{line}] exception:{ex}");
                return false;
            }
        }
        public bool MatchTitle(Line line)
        {
            try
            {
                bool bRet = false;
                int countCharIn = 0;
                int countCharNotIn = 0;
                string text = line.Text.Trim();

                string strRegex = ".";
                foreach (char c in text)
                {
                    // regex escape char
                    //.+*?^$()[]{}|\
                    if (REGEX_ESCAPE_CHARS.Contains(c))
                    {
                        strRegex += $"\\{c}?";
                    }
                    else
                    {
                        strRegex += $"{c}?";
                    }
                    if (!Title.Contains(c))
                        countCharNotIn++;
                    else
                        countCharIn++;
                }
                strRegex += ".";

                try
                {
                    Regex regexLine = new Regex(strRegex);
                    //System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] confidence.avg: {confidence.Avg} countCharNotIn: {countCharNotIn}, countCharIn: {countCharIn}, strRegex: {strRegex} Title: {Title}");
                    if (countCharNotIn < 3 && countCharIn > Title.Length - 3 && regexLine.Match(Title).Success)
                    {
                        bRet = true;
                        _isLabelFound = true;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {Title}");
                        _line = line;
                        _boundingBox = line.BoundingBox;
                        _left = (line.BoundingBox[0] + line.BoundingBox[6]) / 2.0;
                        _top = (line.BoundingBox[1] + line.BoundingBox[3]) / 2.0;
                        _bottom = (line.BoundingBox[7] + line.BoundingBox[5]) / 2.0;
                        _height = line.ExtGetHeight();
                        _width = line.ExtGetWidth();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]     Left:{Left} Top:{Top} Bottom:{Bottom}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }

                return bRet;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTitle [{line}] exception:{ex}");
                return false;
            }
        }
        /*
        public bool MatchTitle(Line line, SpellSuggestionLib.SpellSuggestion spellSuggestion)
        {
            try
            {
                bool bRet = false;
                int countCharIn = 0;
                int countCharNotIn = 0;
                string text = line.Text.Trim();
                double[] confidences = line.ExtGetConfidenceArray();
                Confidence confidence = new Confidence(confidences);

                foreach (char c in text)
                {
                    if (!Title.Contains(c))
                        countCharNotIn++;
                    else
                        countCharIn++;
                }

                try
                {
                    bool bMatch = MatchWithSpellSuggestion(text, Title, spellSuggestion);
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] confidence.avg: {confidence.Avg} countCharNotIn: {countCharNotIn}, countCharIn: {countCharIn}, Title: {Title}");
                    if (((confidence.Avg >= LABEL_CONFIDENCE_THRESHOLD && countCharNotIn == 0) || (confidence.Avg > LABEL_CONFIDENCE_LOWER_THRESHOLD && countCharNotIn < 3))
                     && ((confidence.Avg >= LABEL_CONFIDENCE_THRESHOLD && countCharIn == Title.Length) || (confidence.Avg > LABEL_CONFIDENCE_LOWER_THRESHOLD && countCharIn > Title.Length - 3))
                        && bMatch)
                    {
                        bRet = true;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {Title}");
                        if (!Confidence.HasValue || Confidence.Value < confidences.Average())
                        {
                            _confidence = confidences.Average();
                            _line = line;
                            _boundingBox = line.BoundingBox;
                            _left = (line.BoundingBox[0] + line.BoundingBox[6]) / 2.0;
                            _top = (line.BoundingBox[1] + line.BoundingBox[3]) / 2.0;
                            _bottom = (line.BoundingBox[7] + line.BoundingBox[5]) / 2.0;
                            _height = line.ExtGetHeight();
                            _width = line.ExtGetWidth();
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]     Confidence:{Confidence} Left:{Left} Top:{Top} Bottom:{Bottom}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }

                return bRet;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CheckCharInLine [{line}] exception:{ex}");
                return false;
            }
        }
        */
        /*
        public bool MatchTitleWithSeparator(Line line, string separator, out string valueFollowedBySeparator)
        {
            valueFollowedBySeparator = "";
            try
            {
                bool bRet = false;
                int countCharIn = 0;
                int countCharNotIn = 0;

                string text = line.Text.Trim();

                // remove text followed by separator
                string[] strings = text.Split(separator, 2);
                if (strings.Length != 2)
                    return false;

                if (string.IsNullOrEmpty(strings[0]))
                    return false;

                text = strings[0];

                double[] confidences = line.ExtGetConfidenceArray();
                Confidence confidence = new Confidence(confidences);

                string strRegex = ".";
                foreach (char c in text)
                {
                    // regex escape char
                    //.+*?^$()[]{}|\
                    if (REGEX_ESCAPE_CHARS.Contains(c))
                    {
                        strRegex += $"\\{c}?";
                    }
                    else
                    {
                        strRegex += $"{c}?";
                    }
                    if (!Title.Contains(c))
                        countCharNotIn++;
                    else
                        countCharIn++;
                }
                strRegex += ".";

                try
                {
                    Regex regexLine = new Regex(strRegex);
                    //System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] confidence.avg: {confidence.Avg} countCharNotIn: {countCharNotIn}, countCharIn: {countCharIn}, strRegex: {strRegex} Title: {Title}");
                    if (((confidence.Avg >= LABEL_CONFIDENCE_THRESHOLD && countCharNotIn == 0) || (confidence.Avg > LABEL_CONFIDENCE_LOWER_THRESHOLD && countCharNotIn < 3))
                     && ((confidence.Avg >= LABEL_CONFIDENCE_THRESHOLD && countCharIn == Title.Length) || (confidence.Avg > LABEL_CONFIDENCE_LOWER_THRESHOLD && countCharIn > Title.Length - 3))
                        && regexLine.Match(Title).Success)
                    {
                        bRet = true;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {Title}");
                        valueFollowedBySeparator = strings[1].Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {valueFollowedBySeparator}");
                        if (!Confidence.HasValue || Confidence.Value < confidences.Average())
                        {
                            _confidence = confidences.Average();
                            _line = line;
                            _boundingBox = line.BoundingBox;
                            _left = (line.BoundingBox[0] + line.BoundingBox[6]) / 2.0;
                            _top = (line.BoundingBox[1] + line.BoundingBox[3]) / 2.0;
                            _bottom = (line.BoundingBox[7] + line.BoundingBox[5]) / 2.0;
                            _height = line.ExtGetHeight();
                            _width = line.ExtGetWidth();
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]     Confidence:{Confidence} Left:{Left} Top:{Top} Bottom:{Bottom}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }

                return bRet;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CheckCharInLine [{line}] exception:{ex}");
                return false;
            }
        }
        public bool MatchTitleWithSeparator(Line line, string separator, out string valueFollowedBySeparator, SpellSuggestionLib.SpellSuggestion spellSuggestion)
        {
            valueFollowedBySeparator = "";
            try
            {
                bool bRet = false;
                int countCharIn = 0;
                int countCharNotIn = 0;

                string text = line.Text.Trim();

                // remove text followed by separator
                string[] strings = text.Split(separator, 2);
                if (strings.Length != 2)
                    return false;

                if (string.IsNullOrEmpty(strings[0]))
                    return false;

                text = strings[0];

                double[] confidences = line.ExtGetConfidenceArray();
                Confidence confidence = new Confidence(confidences);
                foreach (char c in text)
                {
                    if (!Title.Contains(c))
                        countCharNotIn++;
                    else
                        countCharIn++;
                }

                try
                {
                    bool bMatch = MatchWithSpellSuggestion(text, Title, spellSuggestion);
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] confidence.avg: {confidence.Avg} countCharNotIn: {countCharNotIn}, countCharIn: {countCharIn}, Title: {Title}");
                    if (((confidence.Avg >= LABEL_CONFIDENCE_THRESHOLD && countCharNotIn == 0) || (confidence.Avg > LABEL_CONFIDENCE_LOWER_THRESHOLD && countCharNotIn < 3))
                     && ((confidence.Avg >= LABEL_CONFIDENCE_THRESHOLD && countCharIn == Title.Length) || (confidence.Avg > LABEL_CONFIDENCE_LOWER_THRESHOLD && countCharIn > Title.Length - 3))
                        && bMatch)
                    {
                        bRet = true;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {Title}");
                        valueFollowedBySeparator = strings[1].Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {valueFollowedBySeparator}");
                        if (!Confidence.HasValue || Confidence.Value < confidences.Average())
                        {
                            _confidence = confidences.Average();
                            _line = line;
                            _boundingBox = line.BoundingBox;
                            _left = (line.BoundingBox[0] + line.BoundingBox[6]) / 2.0;
                            _top = (line.BoundingBox[1] + line.BoundingBox[3]) / 2.0;
                            _bottom = (line.BoundingBox[7] + line.BoundingBox[5]) / 2.0;
                            _height = line.ExtGetHeight();
                            _width = line.ExtGetWidth();
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]     Confidence:{Confidence} Left:{Left} Top:{Top} Bottom:{Bottom}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }

                return bRet;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CheckCharInLine [{line}] exception:{ex}");
                return false;
            }
        }
        */
        public bool MatchTitleFollowedByField(Line line, out string valueFollowedBySeparator)
        {
            valueFollowedBySeparator = "";
            try
            {
                bool bRet = false;
                int countCharIn = 0;
                int countCharNotIn = 0;

                string text = line.Text.Trim();
                if (text.Length <= Title.Length)
                    return false;

                // extract same length as expected title
                text = text.Substring(0, Title.Length).Trim();

                string strRegex = ".";
                foreach (char c in text)
                {
                    // regex escape char
                    //.+*?^$()[]{}|\
                    if (REGEX_ESCAPE_CHARS.Contains(c))
                    {
                        strRegex += $"\\{c}?";
                    }
                    else
                    {
                        strRegex += $"{c}?";
                    }
                    if (!Title.Contains(c))
                        countCharNotIn++;
                    else
                        countCharIn++;
                }
                strRegex += ".";

                try
                {
                    Regex regexLine = new Regex(strRegex);
                    Match match = regexLine.Match(Title);
                    //System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] confidence.avg: {confidence.Avg} countCharNotIn: {countCharNotIn}, countCharIn: {countCharIn}, strRegex: {strRegex} Title: {Title}");
                    if (countCharNotIn < 3 && countCharIn > Title.Length - 3 && match.Success)
                    {
                        bRet = true;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {Title}");
                        valueFollowedBySeparator = line.Text.Substring(match.Value.Length).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {valueFollowedBySeparator}");
                        _line = line;
                        _boundingBox = line.BoundingBox;
                        _left = (line.BoundingBox[0] + line.BoundingBox[6]) / 2.0;
                        _top = (line.BoundingBox[1] + line.BoundingBox[3]) / 2.0;
                        _bottom = (line.BoundingBox[7] + line.BoundingBox[5]) / 2.0;
                        _height = line.ExtGetHeight();
                        _width = line.ExtGetWidth();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]     Left:{Left} Top:{Top} Bottom:{Bottom}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }

                return bRet;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CheckCharInLine [{line}] exception:{ex}");
                return false;
            }
        }
        /*
        public bool MatchTitleFollowedByField(Line line, out string valueFollowedBySeparator, SpellSuggestionLib.SpellSuggestion spellSuggestion)
        {
            valueFollowedBySeparator = "";
            try
            {
                bool bRet = false;
                int countCharIn = 0;
                int countCharNotIn = 0;

                string text = line.Text.Trim();
                if (text.Length <= Title.Length)
                    return false;

                // extract same length as expected title
                text = text.Substring(0, Title.Length).Trim();

                double[] confidences = line.ExtGetConfidenceArray();
                Confidence confidence = new Confidence(confidences);
                foreach (char c in text)
                {
                    if (!Title.Contains(c))
                        countCharNotIn++;
                    else
                        countCharIn++;
                }

                try
                {
                    string wordSuggested = text;
                    bool bMatch = FindWithSpellSuggestion(text, Title, spellSuggestion, out wordSuggested);
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] confidence.avg: {confidence.Avg} countCharNotIn: {countCharNotIn}, countCharIn: {countCharIn}, Title: {Title}");
                    if (((confidence.Avg >= LABEL_CONFIDENCE_THRESHOLD && countCharNotIn == 0) || (confidence.Avg > LABEL_CONFIDENCE_LOWER_THRESHOLD && countCharNotIn < 3))
                     && ((confidence.Avg >= LABEL_CONFIDENCE_THRESHOLD && countCharIn == Title.Length) || (confidence.Avg > LABEL_CONFIDENCE_LOWER_THRESHOLD && countCharIn > Title.Length - 3))
                        && bMatch)
                    {
                        int pos = wordSuggested.IndexOf(Title);
                        if (pos >= 0)
                        {
                            if (pos + Title.Length < wordSuggested.Length)
                            {
                                valueFollowedBySeparator = wordSuggested.Substring(pos, Title.Length).Trim();
                            }
                            else
                            {
                                valueFollowedBySeparator = wordSuggested.Substring(pos).Trim();
                            }
                        }

                        bRet = true;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {Title}");
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {valueFollowedBySeparator}");
                        if (!Confidence.HasValue || Confidence.Value < confidences.Average())
                        {
                            _confidence = confidences.Average();
                            _line = line;
                            _boundingBox = line.BoundingBox;
                            _left = (line.BoundingBox[0] + line.BoundingBox[6]) / 2.0;
                            _top = (line.BoundingBox[1] + line.BoundingBox[3]) / 2.0;
                            _bottom = (line.BoundingBox[7] + line.BoundingBox[5]) / 2.0;
                            _height = line.ExtGetHeight();
                            _width = line.ExtGetWidth();
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]     Confidence:{Confidence} Left:{Left} Top:{Top} Bottom:{Bottom}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }

                return bRet;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CheckCharInLine [{line}] exception:{ex}");
                return false;
            }
        }
        */
        public bool MatchTitleRegex(Line line, String strPatternRegex)
        {
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CheckCharInLine Title: {Title} line.Text {line.Text} strPatternRegex: {strPatternRegex}");
            try
            {
                bool bRet = false;
                string text = line.Text.Trim();

                try
                {
                    Regex regexLine = new Regex(strPatternRegex);
                    if (regexLine.Match(line.Text).Success)
                    {
                        bRet = true;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {Title}");
                        _line = line;
                        _boundingBox = line.BoundingBox;
                        _left = (line.BoundingBox[0] + line.BoundingBox[6]) / 2.0;
                        _top = (line.BoundingBox[1] + line.BoundingBox[3]) / 2.0;
                        _bottom = (line.BoundingBox[7] + line.BoundingBox[5]) / 2.0;
                        _height = line.ExtGetHeight();
                        _width = line.ExtGetWidth();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]     Left:{Left} Top:{Top} Bottom:{Bottom}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }

                return bRet;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CheckCharInLine [{line}] exception:{ex}");
                return false;
            }
        }

        public bool IsFieldInSameLeftEdge(Line field)
        {
            if (Math.Abs((double)(field.BoundingBox[0].Value - Left)) < Height * 3)
            {
                return true;
            }
            return false;
        }
        public bool IsFieldJustUnderTheLabel(Line field)
        {
            if (Math.Abs((double)(field.BoundingBox[0].Value - Left)) < Height * 3
                && (double)(field.ExtGetTop() - Top) >= 0
                && (double)(field.ExtGetTop() - Bottom) < Height * 2)
            {
                return true;
            }
            return false;
        }
        public bool IsFieldUnderTheLabel(Line field)
        {
            if ((double)(field.ExtGetTop() - Bottom) >= 0)
            {
                return true;
            }
            return false;
        }
        public bool IsFieldRightNextToTheLabel(Line field)
        {
            if (_boundingBox != null && _boundingBox.Count >= 8)
            {
                double dx = (double)((_boundingBox[4] - _boundingBox[6]) + (_boundingBox[2] - _boundingBox[0])) / 2.0;
                double dy = (double)((_boundingBox[5] - _boundingBox[7]) + (_boundingBox[3] - _boundingBox[1])) / 2.0;
                double a = 0;
                if (dx != 0)
                {
                    a = dy / dx;
                }
                // y = ax + b
                // b = y - ax;
                double b = (double)(_boundingBox[7] - (a * _boundingBox[6]));
                if (field.BoundingBox != null && field.BoundingBox.Count >= 8)
                {
                    double x1 = (double)field.BoundingBox[6];
                    double y1 = a * x1 + b;
                    double x2 = (double)field.BoundingBox[4];
                    double y2 = a * x2 + b;

                    if (Math.Abs((double)(field.BoundingBox[7] - y1)) < (Height) && Math.Abs((double)(field.BoundingBox[5] - y2)) < (Height))
                    {
                        return true;
                    }
                }
            }


            if (Right < field.ExtGetLeft()
                && Math.Abs((double)(field.ExtGetBottom() - Bottom)) < (Height / 2))
            {
                return true;
            }
            return false;
        }

    }

    static class Extension
    {
        /*
        public static int ExtGetLeftOfBox(this OcrLine ocrLine)
        {
            int left = 0;
            string[] values = ocrLine.BoundingBox.Split(',');
            if (values.Length == 4)
            {
                int.TryParse(values[0], out left);
            }
            return left;
        }
        public static int ExtGetTopOfBox(this OcrLine ocrLine)
        {
            int top = 0;
            string[] values = ocrLine.BoundingBox.Split(',');
            if (values.Length == 4)
            {
                int.TryParse(values[1], out top);
            }
            return top;
        }
        public static int ExtGetRightOfBox(this OcrLine ocrLine)
        {
            int right = 0;
            string[] values = ocrLine.BoundingBox.Split(',');
            if (values.Length == 4)
            {
                int.TryParse(values[2], out right);
            }
            return right;
        }
        public static int ExtGetBottomOfBox(this OcrLine ocrLine)
        {
            int bottom = 0;
            string[] values = ocrLine.BoundingBox.Split(',');
            if (values.Length == 4)
            {
                int.TryParse(values[3], out bottom);
            }
            return bottom;
        }
        public static string ExtGetText(this OcrLine ocrLine)
        {
            string ret = "";
            foreach (var word in ocrLine.Words)
            {
                if (!string.IsNullOrEmpty(ret))
                    ret += " ";
                ret += word.Text;
            }
            return ret;
        }
        public static string ExtGetConfidenceArrayToString(this Line line)
        {
            string ret = "";
            foreach (var word in line.Words)
            {
                if (string.IsNullOrEmpty(ret))
                    ret += "[";
                else
                    ret += ",";
                ret += word.Confidence;
            }
            ret += "]";
            return ret;
        }
        public static double[] ExtGetConfidenceArray(this Line line)
        {
            List<double> confidences = new List<double>();
            foreach (var word in line.Words)
            {
                confidences.Add(word.Confidence);
            }
            return confidences.ToArray();
        }
        */
        public static double? ExtGetHeight(this Line line)
        {
            //double? xLT = line.BoundingBox[0];
            double? yLT = line.BoundingBox[1];
            //double? xRT = line.BoundingBox[2];
            double? yRT = line.BoundingBox[3];
            //double? xRB = line.BoundingBox[4];
            double? yRB = line.BoundingBox[5];
            //double? xLB = line.BoundingBox[6];
            double? yLB = line.BoundingBox[7];
            double? h = ((yRB - yRT) + (yLB - yLT)) / 2;
            return h;
        }
        public static double? ExtGetWidth(this Line line)
        {
            double? xLT = line.BoundingBox[0];
            //double? yLT = line.BoundingBox[1];
            double? xRT = line.BoundingBox[2];
            //double? yRT = line.BoundingBox[3];
            double? xRB = line.BoundingBox[4];
            //double? yRB = line.BoundingBox[5];
            double? xLB = line.BoundingBox[6];
            //double? yLB = line.BoundingBox[7];
            double? w = ((xRB - xLB) + (xRT - xLT)) / 2;
            return w;
        }
        public static double? ExtGetBottom(this Line line)
        {
            double? yRB = line.BoundingBox[5];
            double? yLB = line.BoundingBox[7];
            double? bottom = (yRB + yLB) / 2;
            return bottom;
        }
        public static double? ExtGetTop(this Line line)
        {
            double? yLT = line.BoundingBox[1];
            double? yRT = line.BoundingBox[3];
            double? bottom = (yLT + yRT) / 2;
            return bottom;
        }
        public static double? ExtGetLeft(this Line line)
        {
            double? xLT = line.BoundingBox[0];
            double? xLB = line.BoundingBox[6];
            double? left = (xLT + xLB) / 2;
            return left;
        }
        public static double? ExtGetRight(this Line line)
        {
            double? xRT = line.BoundingBox[2];
            double? xRB = line.BoundingBox[4];
            double? right = (xRT + xRB) / 2;
            return right;
        }

        public static double? ExtGetAngle(this Line line)
        {
            double? xLT = line.BoundingBox[0];
            double? yLT = line.BoundingBox[1];
            double? xRT = line.BoundingBox[2];
            double? yRT = line.BoundingBox[3];
            double? xRB = line.BoundingBox[4];
            double? yRB = line.BoundingBox[5];
            double? xLB = line.BoundingBox[6];
            double? yLB = line.BoundingBox[7];
            if (xRB - xLB == 0)
            {
                if (yRB - yLB > 0)
                    return 90;
                else
                    return -90;
            }
            else
            {
                double radian = Math.Atan((double)((yRB - yLB) / (xRB - xLB)));
                double angle = radian * (180 / Math.PI);
                return angle;
            }
        }
        /*
        public static Line MergedLine(this Line line1, Line line2)
        {
            List<double?> boundingBox = new() {
                line1.BoundingBox[0], line1.BoundingBox[1], //left top
                line2.BoundingBox[2], line2.BoundingBox[3], //right top
                line2.BoundingBox[4], line2.BoundingBox[5], //right bottom
                line1.BoundingBox[6], line1.BoundingBox[7]  //left bottom
            };

            string text = line1.Text + " " + line2.Text;
            List<Word> words = line1.Words.ToList();
            words.AddRange(line2.Words);

            Line newLine = new Line(boundingBox, text, words);
            return newLine;
        }
        */
    }
}
