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
        const string BASEURL = "http://127.0.0.1:5000/";
        static void Main(string[] args)
        {
            try
            {
                // check connection
                using (var client = new HttpClient())
                {
                    // Set the base address of the web service
                    //client.BaseAddress = new Uri(BASEURL);

                    // Post the request to the web service
                    var response = client.GetAsync($"{BASEURL}device").GetAwaiter().GetResult();

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

                List<Task> lsTask = new List<Task>();
                for (int i = 0; i < 1; i++)
                {
                    Task t = Test3(args);
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
            // .\images\CSDEMOBANK_ApplicationForm_P1.jpg
            if (args.Length == 0)
            {
                args = new string[] { ".\\images\\CSDEMOBANK_ApplicationForm_P1.jpg" };
            }

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
                        bmpField.Save(fd.Name + ".png", ImageFormat.Png);
                        bmpField.Save(stream, ImageFormat.Png);
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

        static Task Test3(string[] args)
        {
            // Task to post each of all images
            if (args.Length == 0)
            {
                args = new string[] { ".\\images\\MYDL1_s.jpg", ".\\images\\MYDL1.jpg", ".\\images\\MYDL2.png", ".\\images\\MYDL2.jpg" };
            }
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
                        string retOcr = PostOCRWithRegionRequest(b64Image);
                        DateTime dtEnd = DateTime.Now;
                        Console.WriteLine(retOcr);
                        Console.WriteLine($"({(dtEnd - dtStart).TotalSeconds} sec)\n");
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
                var response = client.PostAsync($"{BASEURL}ocrB64", content).GetAwaiter().GetResult();

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

        static string PostOCRWithRegionRequest(string b64Image)
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
                var response = client.PostAsync($"{BASEURL}ocrWithRegionB64", content).GetAwaiter().GetResult();

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
    }
}
