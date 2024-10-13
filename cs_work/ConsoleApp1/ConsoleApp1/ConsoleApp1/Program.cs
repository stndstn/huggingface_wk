using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConsoleApp1
{
    public class OcrRequest
    {
        public string b64 { get; set; }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            if(args.Length == 0){
                Console.WriteLine("No image provided");
                return;
            }

            List<Task> lsTask = new List<Task>();
            for(int i = 0; i < 1; i++)
            {
                Task t = Test(args);
                lsTask.Add(t);
                System.Threading.Thread.Sleep(1000);
            }
            Task.WaitAll(lsTask.ToArray());

            Console.Write("Hit enter to exit:");
            Console.ReadLine();
        }

        static Task Test(string[] args)
        {
            // Task to post each of all images
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

                    using (var client = new HttpClient())
                    {
                        // Set the base address of the web service
                        client.BaseAddress = new Uri("http://127.0.0.1:8085/");

                        // Post the request to the web service
                        var response = client.GetAsync("http://127.0.0.1:8085/device").GetAwaiter().GetResult();

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
                var response = client.PostAsync("http://127.0.0.1:8085/ocrB64", content).GetAwaiter().GetResult();

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
                var response = client.PostAsync("http://127.0.0.1:8085/ocrWithRegionB64", content).GetAwaiter().GetResult();

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
