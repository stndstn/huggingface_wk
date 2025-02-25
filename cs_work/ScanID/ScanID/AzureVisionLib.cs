using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using SkiaSharp;
//using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
//using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
//using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text.Json;
using System.IO;
//using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using System.ComponentModel;
using System.Security.Claims;
using System.Security.Principal;
using System.Net;
using System.Net.NetworkInformation;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
//using SpellSuggestionLib;
//using static ScanIDLib.Code;
using static ScanID.Code;
using ZXing;
using System.Numerics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
//using Microsoft.VisualBasic.FileIO;

//namespace ScanIDLib
namespace ScanID
{
#if false
    public class ScanIDResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }

        public DateTime? Timestamp { get; set; }

        protected string _documentType;
        public string documentType { get { return _documentType; } }
        public bool isBackOfIDImage { get; set; } = false;

        public string lastNameOrFullName { get; set; }
        public Confidence? lastNameOrFullNameConfidence { get; set; }
        public string? firstName { get; set; }
        public Confidence? firstNameConfidence { get; set; }
        public string? middleName { get; set; }
        public Confidence? middleNameConfidence { get; set; }
        public string documentNumber { get; set; }
        public Confidence? documentNumberConfidence { get; set; }
        public string nationality { get; set; }
        public Confidence? nationalityConfidence { get; set; }
        public string? dateOfBirth { get; set; }
        public Confidence? dateOfBirthConfidence { get; set; }
        public string? placeOfBirth { get; set; }
        public Confidence? placeOfBirthConfidence { get; set; }
        public string? gender { get; set; }
        public Confidence? genderConfidence { get; set; }
        public string? maritalStatus { get; set; }
        public Confidence? maritalStatusConfidence { get; set; }
        public string? documentExpirationDate { get; set; }
        public Confidence? documentExpirationDateConfidence { get; set; }
        public string? documentIssueDate { get; set; }
        public Confidence? documentIssueDateConfidence { get; set; }
        public string? addressLine1 { get; set; }
        public Confidence? addressLine1Confidence { get; set; }
        public string? addressLine2 { get; set; }
        public Confidence? addressLine2Confidence { get; set; }
        public string? addressTown { get; set; }
        public Confidence? addressTownConfidence { get; set; }
        public string? postcode { get; set; }
        public Confidence? postcodeConfidence { get; set; }
        public string? personalNumber { get; set; }
        public Confidence? personalNumberConfidence { get; set; }

        protected string _country;
        public string country { get { return _country; } }

        public string? faceImageBase64 { get; set; }
        public string? faceImageMediaType { get; set; }

        public string? extraData { get; set; }

        public Confidence confidences
        {
            get
            {
                Confidence confidence = new Confidence();
                if (lastNameOrFullNameConfidence != null)
                    confidence += lastNameOrFullNameConfidence;
                if (firstNameConfidence != null)
                    confidence += firstNameConfidence;
                if (middleNameConfidence != null)
                    confidence += middleNameConfidence;
                if (documentNumberConfidence != null)
                    confidence += documentNumberConfidence;
                if (nationalityConfidence != null)
                    confidence += nationalityConfidence;
                if (dateOfBirthConfidence != null)
                    confidence += dateOfBirthConfidence;
                if (genderConfidence != null)
                    confidence += genderConfidence;
                if (documentExpirationDateConfidence != null)
                    confidence += documentExpirationDateConfidence;
                if (documentIssueDateConfidence != null)
                    confidence += documentIssueDateConfidence;
                if (addressLine1Confidence != null)
                    confidence += addressLine1Confidence;
                if (addressLine2Confidence != null)
                    confidence += addressLine2Confidence;
                if (postcodeConfidence != null)
                    confidence += postcodeConfidence;

                return confidence;
            }
        }

        public string? resultJsonStringOCR { get; set; }
        public string? resultJsonStringImageLabeling { get; set; }

        public double? documentLandmarksProbabilityAvg { get; set; }

        public string? eKycSessionId { get; set; }
        public string? faceImageDataUrl { 
            get {
                //data:[<mediatype>][;base64],<data>
                if(!string.IsNullOrEmpty(faceImageBase64) && !string.IsNullOrEmpty(faceImageMediaType))
                {
                    return "data:" + faceImageMediaType + ";base64," + faceImageBase64;
                }
                return null;
            } 
        }

    }
#endif

    public class Confidence
    {
        private double[] confidences = null;
        public Confidence() { }
        public Confidence(double value)
        {
            Min = value; Max = value; Avg = value;
            confidences = new double[] { value };
        }
        public Confidence(double[] values)
        {
            if (values.Length > 0)
            {
                Min = values.Min(); Max = values.Max(); Avg = values.Average();
                confidences = values;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Confidence values is empty");
            }
        }

        public static Confidence operator +(Confidence a, Confidence b)
        {
            List<double> confidences = new List<double>();
            confidences.AddRange(a.GetConfidences());
            confidences.AddRange(b.GetConfidences());
            return new Confidence(confidences.ToArray());
        }

        public double[] GetConfidences()
        {
            if (confidences != null)
            {
                return confidences;
            }
            else
            {
                if (Avg != null)
                    return new double[] { Avg.Value };
                else
                    return new double[] { };
            }
        }

        public double? Min { get; set; }
        public double? Max { get; set; }
        public double? Avg { get; set; }
        public double? Worst
        {
            get
            {
                if (confidences != null)
                {
                    var itemsWithValue = confidences.Where(v => v > 0);
                    return (itemsWithValue.Count() == 0) ? 0 : itemsWithValue.Min();
                }
                else
                {
                    return 0;
                }
            }
        }
        public double? Best { get { return Max; } }
    }

    /* Document Type
    DL - DRIVER'S LICENSE
    KP - KITAP
    KS - KITAS
    KT - KTP
    MY - MyKad
    PS - NEW NRC
    ON - OLD NRC
    OT - OTHERS
    PP - PASSPORT
    NI - PHILID
    PI - PROFESSIONAL ID
    SI - SSS ID
    TI - TAX ID
    UI - UMID
    VI - VOTER ID
    */

#if false
    public class ScanMyKadResult : ScanIDResult
    {
        public ScanMyKadResult()
        {
            _documentType = "MY";
            _country = "MY";
            nationality = "MY";
            nationalityConfidence = new Confidence(1);
        }
    }

    public class ScanMYDLResult : ScanIDResult
    {
        public ScanMYDLResult()
        {
            _documentType = "DL";
            _country = "MY";
        }
    }

    public class ScanPHUMIDResult : ScanIDResult
    {
        public ScanPHUMIDResult()
        {
            _documentType = "UI";
            _country = "PH";
            nationality = "PH";
            nationalityConfidence = new Confidence(1);
        }
    }

    public class ScanPHNIResult : ScanIDResult
    {
        public ScanPHNIResult()
        {
            _documentType = "NI";
            _country = "PH";
            nationality = "PH";
            nationalityConfidence = new Confidence(1);
        }
    }

    public class ScanPHNIBKResult : ScanPHNIResult
    {
        public string QRCodeData { get; set; } = "";
        public bool IsQRCodeDataValid { get; set; } = false;

        public string QRCode_DateIssued { get; set; } = "";
        public string QRCode_Issuer { get; set; } = "";
        public string QRCode_alg { get; set; } = "";
        public string QRCode_signature { get; set; } = "";
        public string QRCode_subject_Suffix { get; set; } = "";
        public string QRCode_subject_lName { get; set; } = "";
        public string QRCode_subject_fName { get; set; } = "";
        public string QRCode_subject_mName { get; set; } = "";
        public string QRCode_subject_sex { get; set; } = "";
        public string QRCode_subject_BT { get; set; } = "";
        public string QRCode_subject_DOB { get; set; } = "";
        public string QRCode_subject_POB { get; set; } = "";
        public string QRCode_subject_PCN { get; set; } = "";

        public ScanPHNIBKResult()
        {
            isBackOfIDImage = true;
        }
        public ScanPHNIBKResult(ScanPHNIResult refData)
        {
            if(refData != null)
            {
                this.addressLine1 = refData.addressLine1;
                this.addressLine1Confidence = refData.addressLine1Confidence;
                this.addressLine2 = refData.addressLine2;
                this.addressLine2Confidence = refData.addressLine2Confidence;
                this.addressTown = refData.addressTown;
                this.addressTownConfidence = refData.addressTownConfidence;
                this.dateOfBirth = refData.dateOfBirth;
                this.dateOfBirthConfidence = refData.dateOfBirthConfidence;
                this.documentExpirationDate = refData.documentExpirationDate;
                this.documentExpirationDateConfidence = refData.documentExpirationDateConfidence;
                this.documentNumber = refData.documentNumber;
                this.documentNumberConfidence = refData.documentNumberConfidence;
                this.gender = refData.gender;
                this.genderConfidence = refData.genderConfidence;
                this.maritalStatus = refData.maritalStatus;
                this.maritalStatusConfidence = refData.maritalStatusConfidence;
                this.nationality = refData.nationality;
                this.nationalityConfidence = refData.nationalityConfidence;
                this.personalNumber = refData.personalNumber;
                this.personalNumberConfidence = refData.personalNumberConfidence;
                this.placeOfBirth = refData.placeOfBirth;
                this.placeOfBirthConfidence = refData.placeOfBirthConfidence;
                this.postcode = refData.postcode;
                this.postcodeConfidence = refData.postcodeConfidence;
                this.resultJsonStringImageLabeling = refData.resultJsonStringImageLabeling;
                this.resultJsonStringOCR = refData.resultJsonStringOCR;
                this.Success = refData.Success;
                if(refData is ScanPHNIBKResult)
                {
                    ScanPHNIBKResult back = (ScanPHNIBKResult)refData;
                    this.QRCodeData = back.QRCodeData;
                    this.IsQRCodeDataValid = back.IsQRCodeDataValid;
                    this.QRCode_alg = back.QRCode_alg;
                    this.QRCode_DateIssued = back.QRCode_DateIssued;
                    this.QRCode_Issuer = back.QRCode_Issuer;
                    this.QRCode_signature = back.QRCode_signature;
                    this.QRCode_subject_BT = back.QRCode_subject_BT;
                    this.QRCode_subject_DOB = back.QRCode_subject_DOB;
                    this.QRCode_subject_POB = back.QRCode_subject_POB;
                    this.QRCode_subject_PCN = back.QRCode_subject_PCN;
                    this.QRCode_subject_Suffix = back.QRCode_subject_Suffix;
                    this.QRCode_subject_lName = back.QRCode_subject_lName;
                    this.QRCode_subject_fName = back.QRCode_subject_fName;
                    this.QRCode_subject_mName = back.QRCode_subject_mName;
                    this.QRCode_subject_sex = back.QRCode_subject_sex;
                }
            }

            isBackOfIDImage = true;
        }
    }

    public class ScanPHDLResult : ScanIDResult
    {
        public ScanPHDLResult()
        {
            _documentType = "DL";
            _country = "PH";
        }
    }

    public class ScanIDeKTPResult : ScanIDResult
    {
        public string? provinsi { get; set; }
        public Confidence? provinsiConfidence { get; set; }
        public string? kabKota { get; set; }
        public Confidence? kabKotaConfidence { get; set; }
        public string? alamat { get; set; }
        public Confidence? alamatConfidence { get; set; }
        public string? rt { get; set; }
        public Confidence? rtConfidence { get; set; }
        public string? rw { get; set; }
        public Confidence? rwConfidence { get; set; }
        public string? kelDesa { get; set; }
        public Confidence? kelDesaConfidence { get; set; }
        public string? kecamatan { get; set; }
        public Confidence? kecamatanConfidence { get; set; }

        public ScanIDeKTPResult()
        {
            _documentType = "KT";
            _country = "ID";
        }
    }

    public class ScanIDDLResult : ScanIDResult
    {
        public ScanIDDLResult()
        {
            _documentType = "DL";
            _country = "ID";
        }
    }

    public class ScanPassportMRZResult : ScanIDResult
    {
        public ScanPassportMRZResult()
        {
            _documentType = "PP";
        }

        public void SetCountry(Code.Country country)
        {
            _country = country.ncode;
        }
    }
#endif

    public static class AzureVisionLib
    {
        // Add your Computer Vision subscription key and endpoint to your environment variables
        //string subscriptionKey = Environment.GetEnvironmentVariable("COMPUTER_VISION_SUBSCRIPTION_KEY");
        //string endpoint = Environment.GetEnvironmentVariable("COMPUTER_VISION_ENDPOINT");
        const string SUBSCRIPTION_KEY_VISION = "a99d0ee0b6f546b3af3eb968bdae2459";  // consolsys.com Vision API resource 'CSAZ'
        const string ENDPOINT_VISION = "https://csaz.cognitiveservices.azure.com/";
        const string SUBSCRIPTION_KEY_FACE = "b7a95cd8e08e4e658518bf94ef7b3d4d"; //consolsys.com Face API resource 'CSAZFACE'
        const string ENDPOINT_FACE = "https://csazface.cognitiveservices.azure.com/";
        const double PREDICTION_PROBABILITY_THRESHOLD_ID_ONLY = 0.85;
        const double PREDICTION_PROBABILITY_THRESHOLD_ID = 0.50;
        const double PREDICTION_PROBABILITY_THRESHOLD_LANDMARK_ONLY = 0.75;
        const double PREDICTION_PROBABILITY_THRESHOLD_LANDMARK = 0.60;
        const double LABEL_CONFIDENCE_THRESHOLD = 0.9;
        const double LABEL_CONFIDENCE_LOWER_THRESHOLD = 0.5;
        const string DEBUG_OUTPUT_FOLDER = "c:\\temp\\";  //for IIS
                                                          //const string DEBUG_OUTPUT_FOLDER = "";

        //static SpellSuggestionLib.SpellSuggestion mSpellSuggestion = new SpellSuggestionLib.SpellSuggestion();

        public class AzureScanOCRResult
        {
            public ReadResult ReadResult { get; set; }
            public string ResultJsonString { get; set; }
        }

        public static async Task<AzureScanOCRResult> AzureScanOCRAsync(byte[] dataImageSrc)
        {
            ReadResult retReadResult = null;
            // load image in proper size and format
            SKImage skImg = SKImage.FromEncodedData(dataImageSrc);
            SKBitmap skBmpSrc = SKBitmap.FromImage(skImg);
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] GetScanResultAsync CompressImageIfLarger --> dataImageSrc.Length: {dataImageSrc.Length} skBmpSrc W:{skBmpSrc.Width} H:{skBmpSrc.Height}");

            //
            // Read text from image by OCR (Azure Visoin) 
            //
            ReadOperationResult? operationResult = null;
            ReadInStreamHeaders? readInStreamHeaders = null;
            ComputerVisionClient computerVision = new ComputerVisionClient(
                new Microsoft.Azure.CognitiveServices.Vision.ComputerVision.ApiKeyServiceClientCredentials(SUBSCRIPTION_KEY_VISION))
            { Endpoint = ENDPOINT_VISION };
#if DEBUG
#if true
            /////
            using (FileStream fs = new FileStream($"{DEBUG_OUTPUT_FOLDER}src.jpeg", FileMode.Create))
            {
                SKData skData = skBmpSrc.Encode(SKEncodedImageFormat.Jpeg, 100);
                skData.SaveTo(fs);
            }
            /////
#endif
#endif
            DateTime dtSt = DateTime.Now;
            Console.Write(" Calling Azure VisionOCR... ");
            using (MemoryStream msImage = new MemoryStream(dataImageSrc))
            {
                readInStreamHeaders = await computerVision.ReadInStreamAsync(msImage);
            }

            Uri uri = new Uri(readInStreamHeaders.OperationLocation);
            string path = uri.AbsolutePath;
            string[] pathes = path.Split('/');
            Guid operationId = Guid.Parse(pathes.Last());
            for (int i = 0; i < 60; i++)
            {
                operationResult = await computerVision.GetReadResultAsync(operationId);
                if (operationResult != null)
                {
                    System.Diagnostics.Debug.WriteLine(operationResult.Status);
                    if (operationResult.Status != OperationStatusCodes.Running)
                        break;
                }
                Thread.Sleep(1000);
            }
            DateTime dtEn = DateTime.Now;
            var elapsed = dtEn - dtSt;
            Console.WriteLine(" Elapsed: " + elapsed);

            if (operationResult == null)
            {
                System.Diagnostics.Debug.WriteLine("operationResult is null");
                return null;
            }
            if (operationResult.AnalyzeResult == null)
            {
                System.Diagnostics.Debug.WriteLine("operationResult.AnalyzeResult is null");
                return null;
            }
            if (operationResult.AnalyzeResult.ReadResults == null)
            {
                System.Diagnostics.Debug.WriteLine("operationResult.AnalyzeResult.ReadResults is null");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] operationResult.AnalyzeResult.ReadResults.Count: {operationResult.AnalyzeResult.ReadResults.Count}");
            string resultJsonString = JsonSerializer.Serialize(operationResult);
            foreach (ReadResult readResult in operationResult.AnalyzeResult.ReadResults)
            {

                if (readResult.Lines.Count < 5)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] Number of lines found: {readResult.Lines.Count} < 5");
                    return null;
                }
                retReadResult = readResult;
            }

            return new AzureScanOCRResult { ReadResult = retReadResult, ResultJsonString = resultJsonString };
        }

        static IList<ScanID.Line> RotateBitmapAndLines(SKBitmap skBmpSrc, ReadResult readResult, out SKBitmap bitmapRotated)
        {
            bitmapRotated = null;

            // re-arrange BoundingBox considering rotated angle 
            //IList<Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.Line> linesAll = new List<Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.Line>();
            IList<ScanID.Line> linesAllScanID = new List<ScanID.Line>();
            if (80 < readResult.Angle && readResult.Angle < 100)
            {
                foreach (Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.Line line in readResult.Lines)
                {
                    IList<double?> boundingBoxRotated = new List<double?>
                    {
                        line.BoundingBox[1],
                        readResult.Width - line.BoundingBox[0],
                        line.BoundingBox[3],
                        readResult.Width - line.BoundingBox[2],
                        line.BoundingBox[5],
                        readResult.Width - line.BoundingBox[4],
                        line.BoundingBox[7],
                        readResult.Width - line.BoundingBox[6]
                    };
                    Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.Line lineRotated = new Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.Line(boundingBoxRotated, line.Text, line.Words);
                    //linesAll.Add(lineRotated);
                    ScanID.Line lineRotatedScanID = new ScanID.Line(boundingBoxRotated, line.Text);
                    linesAllScanID.Add(lineRotatedScanID);
                }
                if (skBmpSrc != null)
                {
                    bitmapRotated = Rotate(skBmpSrc, -90);
                }
            }
            else if (170 < readResult.Angle && readResult.Angle < 190)
            {
                foreach (Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.Line line in readResult.Lines)
                {
                    IList<double?> boundingBoxRotated = new List<double?>
                    {
                        readResult.Width - line.BoundingBox[0],
                        readResult.Height - line.BoundingBox[1],
                        readResult.Width - line.BoundingBox[2],
                        readResult.Height - line.BoundingBox[3],
                        readResult.Width - line.BoundingBox[4],
                        readResult.Height - line.BoundingBox[5],
                        readResult.Width - line.BoundingBox[6],
                        readResult.Height - line.BoundingBox[7]
                    };
                    Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.Line lineRotated = new Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.Line(boundingBoxRotated, line.Text, line.Words);
                    //linesAll.Add(lineRotated);
                    ScanID.Line lineRotatedScanID = new ScanID.Line(boundingBoxRotated, line.Text);
                    linesAllScanID.Add(lineRotatedScanID);
                }
                if (skBmpSrc != null)
                {
                    bitmapRotated = Rotate(skBmpSrc, 180);
                }
            }
            else if (-100 < readResult.Angle && readResult.Angle < -80 || 260 < readResult.Angle && readResult.Angle < 280)
            {
                foreach (Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.Line line in readResult.Lines)
                {
                    IList<double?> boundingBoxRotated = new List<double?>
                    {
                        readResult.Height - line.BoundingBox[1],
                        line.BoundingBox[0],
                        readResult.Height - line.BoundingBox[3],
                        line.BoundingBox[2],
                        readResult.Height - line.BoundingBox[5],
                        line.BoundingBox[4],
                        readResult.Height - line.BoundingBox[7],
                        line.BoundingBox[6]
                    };
                    Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.Line lineRotated = new Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.Line(boundingBoxRotated, line.Text, line.Words);
                    //linesAll.Add(lineRotated);
                    ScanID.Line lineRotatedScanID = new ScanID.Line(boundingBoxRotated, line.Text);
                    linesAllScanID.Add(lineRotatedScanID);
                }
                if (skBmpSrc != null)
                {
                    bitmapRotated = Rotate(skBmpSrc, 90);
                }
            }
            else if (-190 < readResult.Angle && readResult.Angle < -170)
            {
                foreach (Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.Line line in readResult.Lines)
                {
                    IList<double?> boundingBoxRotated = new List<double?>
                    {
                        readResult.Width - line.BoundingBox[0],
                        readResult.Height - line.BoundingBox[1],
                        readResult.Width - line.BoundingBox[2],
                        readResult.Height - line.BoundingBox[3],
                        readResult.Width - line.BoundingBox[4],
                        readResult.Height - line.BoundingBox[5],
                        readResult.Width - line.BoundingBox[6],
                        readResult.Height - line.BoundingBox[7]
                    };
                    Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.Line lineRotated = new Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.Line(boundingBoxRotated, line.Text, line.Words);
                    //linesAll.Add(lineRotated);
                    ScanID.Line lineRotatedScanID = new ScanID.Line(boundingBoxRotated, line.Text);
                    linesAllScanID.Add(lineRotatedScanID);
                }
                if (skBmpSrc != null)
                {
                    bitmapRotated = Rotate(skBmpSrc, 180);
                }
            }
            else
            {
                //linesAll = readResult.Lines;
                System.Diagnostics.Debug.WriteLine("----------");
                foreach (Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.Line line in readResult.Lines)
                {
                    double dx = (double)(Math.Abs((decimal)(line.BoundingBox[2] - line.BoundingBox[0])) + Math.Abs((decimal)(line.BoundingBox[6] - line.BoundingBox[4]))) / 2;
                    double dy = (double)(Math.Abs((decimal)(line.BoundingBox[3] - line.BoundingBox[1])) + Math.Abs((decimal)(line.BoundingBox[7] - line.BoundingBox[5]))) / 2;
                    double angle = Math.Round(Math.Atan2(dy, dx) * (180 / Math.PI), 2);
                    double dAngle = Math.Abs(angle - readResult.Angle);
                    if(dAngle > 10)
                    {
                        continue;
                    }

                    Line lineScanID = new Line(line.BoundingBox, line.Text);
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {lineScanID.Text} Height:{lineScanID.ExtGetHeight()}");
                    linesAllScanID.Add(lineScanID);
                }
                /*
                foreach (Line line in linesAll)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} Height:{line.ExtGetHeight()} Confidence:{line.ExtGetConfidenceArrayToString()}");
                }
                */
                System.Diagnostics.Debug.WriteLine("----------");

                if (skBmpSrc != null)
                {
                    bitmapRotated = skBmpSrc;
                }
            }
            return linesAllScanID;
        }

        public static async Task<ScanID.ScanIDResult?> GetScanResultAsync(byte[] dataImageSrc, byte[] dataImageSrcBack, ScanIDOCR scanIDOCR, string idType = "")
        {
            if (dataImageSrc == null || dataImageSrc.Length == 0)
            {
                System.Diagnostics.Debug.WriteLine("Source ID image is empry");
                return null;
            }
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] GetScanResultAsync dataImageSrc.Length: {dataImageSrc.Length}");

            // max 4MB for Free tier (50MB for paid), if exceeds, compress
            dataImageSrc = CompressImageIfLarger(1024 * 1024 * 4, dataImageSrc);
            // load image in proper size and format
            SKImage skImg = SKImage.FromEncodedData(dataImageSrc);
            SKBitmap skBmpSrc = SKBitmap.FromImage(skImg);
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] GetScanResultAsync CompressImageIfLarger --> dataImageSrc.Length: {dataImageSrc.Length} skBmpSrc W:{skBmpSrc.Width} H:{skBmpSrc.Height}");
            // Read text from image by OCR (Azure Visoin) 
            AzureScanOCRResult azureScanOCRResult = await AzureScanOCRAsync(dataImageSrc);
            if(azureScanOCRResult == null)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] GetScanResultAsync AzureScanOCRAsync failed. return null");
                return null;
            }
            ReadResult readResult = azureScanOCRResult.ReadResult;
            string resultJsonString = azureScanOCRResult.ResultJsonString;
#if false
            //
            // Read text from image by OCR (Azure Visoin) 
            //
            ReadOperationResult? operationResult = null;
            ReadInStreamHeaders? readInStreamHeaders = null;
            ComputerVisionClient computerVision = new ComputerVisionClient(
                new Microsoft.Azure.CognitiveServices.Vision.ComputerVision.ApiKeyServiceClientCredentials(SUBSCRIPTION_KEY_VISION))
            { Endpoint = ENDPOINT_VISION };
#if DEBUG
#if true
            /////
            using (FileStream fs = new FileStream($"{DEBUG_OUTPUT_FOLDER}src.jpeg", FileMode.Create))
            {
                SKData skData = skBmpSrc.Encode(SKEncodedImageFormat.Jpeg, 100);
                skData.SaveTo(fs);
            }
            /////
#endif
#endif
            using (MemoryStream msImage = new MemoryStream(dataImageSrc))
            {
                readInStreamHeaders = await computerVision.ReadInStreamAsync(msImage);
            }

            Uri uri = new Uri(readInStreamHeaders.OperationLocation);
            string path = uri.AbsolutePath;
            string[] pathes = path.Split('/');
            Guid operationId = Guid.Parse(pathes.Last());
            for (int i = 0; i < 60; i++)
            {
                operationResult = await computerVision.GetReadResultAsync(operationId);
                if (operationResult != null)
                {
                    System.Diagnostics.Debug.WriteLine(operationResult.Status);
                    if (operationResult.Status != OperationStatusCodes.Running)
                        break;
                }
                Thread.Sleep(1000);
            }

            if (operationResult == null)
            {
                System.Diagnostics.Debug.WriteLine("operationResult is null");
                return null;
            }
            if (operationResult.AnalyzeResult == null)
            {
                System.Diagnostics.Debug.WriteLine("operationResult.AnalyzeResult is null");
                return null;
            }
            if (operationResult.AnalyzeResult.ReadResults == null)
            {
                System.Diagnostics.Debug.WriteLine("operationResult.AnalyzeResult.ReadResults is null");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] operationResult.AnalyzeResult.ReadResults.Count: {operationResult.AnalyzeResult.ReadResults.Count}");
            SKBitmap? bitmapRotated = null;
            ScanID.ScanIDResult? result = null;
            byte[] dataImageRotated = null;
            double resultAngle = 0;
            string resultJsonString = JsonSerializer.Serialize(operationResult);
            ReadResult readResult = null;
            foreach (ReadResult aResult in operationResult.AnalyzeResult.ReadResults)
            {
                if (aResult.Lines.Count < 5)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] Number of lines found: {aResult.Lines.Count} < 5");
                    continue;
                }
                readResult = aResult;
                break;
            }
#endif
            if (readResult == null)
            {
                System.Diagnostics.Debug.WriteLine("No valid readResult found in operationResult.AnalyzeResult.ReadResults");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] readResult.Angle: {readResult.Angle} readResult.Width: {readResult.Width} readResult.Height: {readResult.Height}");

            SKBitmap bitmapRotated = null;
            IList<ScanID.Line> linesAllScanID = RotateBitmapAndLines(skBmpSrc, readResult, out bitmapRotated);

            // detect face image position
            System.ValueType valLeft = new System.Int32();
            System.ValueType valTop = new System.Int32();
            System.ValueType valRight = new System.Int32();
            System.ValueType valBottom = new System.Int32();
            bool bFaceFound = false;
            string b64ImageFace = null;
            try
            {
                bFaceFound = DlibDn47.DlibWrapper.DetectFace(dataImageSrc, ref valLeft, ref valTop, ref valRight, ref valBottom);
            }
            catch (Exception ex)
            {
                Console.WriteLine("DetectFace exception: " + ex.Message);
                return new ScanIDResult() { Error = "DetectFace exception: " + ex.Message };
            }

            SKImage imageRotated = SKImage.FromBitmap(bitmapRotated);
            SKRectI? rcFace = null;
            // extract face image
            if (imageRotated != null)
            {
                if (bFaceFound)
                {
                    // enlarge area to extract face image because detected area is only ROI for face recognition.
                    int width = (int)valRight - (int)valLeft;
                    int height = (int)valBottom - (int)valTop;
                    int leftFaceArea = (int)valLeft - width / 2;
                    if (leftFaceArea < 0) leftFaceArea = 0;
                    int topFaceArea = (int)valTop - height / 2;
                    if (topFaceArea < 0) topFaceArea = 0;
                    int rightFaceArea = (int)valRight + width / 2;
                    if (rightFaceArea > imageRotated.Width) rightFaceArea = imageRotated.Width;
                    int bottomFaceArea = (int)valBottom + height / 2;
                    if (bottomFaceArea > imageRotated.Height) bottomFaceArea = imageRotated.Height;

                    rcFace = new SKRectI(leftFaceArea, topFaceArea, rightFaceArea, bottomFaceArea);
                    SKImage skImgFace = imageRotated.Subset(rcFace.Value);
                    SKData skDataJpgFace = skImgFace.Encode(SKEncodedImageFormat.Jpeg, 90);
                    byte[] dataJpgFace = skDataJpgFace.ToArray();
                    b64ImageFace = Convert.ToBase64String(dataJpgFace);
                }
            }

            //
            // Detect document type and extract fields
            //
            //result = ScanIDLib.AzureVisionLib.ExtractFieldsFromReadResult(linesAll, bitmapRotated, idType);
            Console.WriteLine("==== Merged lines ====");
            ScanID.Line[] linesMergedAllScanID = scanIDOCR.MergeLinesInSameYPosIntoOneLine(linesAllScanID, 5f).ToArray();
            foreach (ScanID.Line line in linesMergedAllScanID)
            {
                Console.WriteLine(line.ExtToString());
            }
            Console.WriteLine("======================");

            List<ScanID.LabelInfo> labelsFound = new List<ScanID.LabelInfo>();
            List<ScanID.Line> linesNotLabel = new List<ScanID.Line>();

            string docType = idType;
            if (!string.IsNullOrEmpty(docType))
            {
                //LabelInfo[] labelsToFind = GetLabelsToFind(docType);
                //LabelInfo[] labelsAboveFields = GetLabelsAboveFields(docType);
                scanIDOCR.FindLabels(linesMergedAllScanID, docType, out labelsFound, out linesNotLabel);
            }
            else
            {
                docType = scanIDOCR.FindLabelsAndIdentifyDocType(linesMergedAllScanID, out labelsFound, out linesNotLabel);
            }

            IList<ScanID.Line> linesAllScanIDBack = null;
            List<ScanID.LabelInfo> labelsFoundBack = new List<ScanID.LabelInfo>();
            string imageSrcB64Back = "";
            List<ScanID.Line> linesNotLabelBack = new List<ScanID.Line>();
            if (docType == "PHNI")
            {
                // OCR back image
                if (dataImageSrcBack == null || dataImageSrcBack.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Source ID image is empry");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] GetScanResultAsync dataImageSrcBack.Length: {dataImageSrcBack.Length}");

                    // max 4MB for Free tier (50MB for paid), if exceeds, compress
                    dataImageSrcBack = CompressImageIfLarger(1024 * 1024 * 4, dataImageSrcBack);
                    // load image in proper size and format
                    SKImage skImgBack = SKImage.FromEncodedData(dataImageSrcBack);
                    SKBitmap skBmpSrcBack = SKBitmap.FromImage(skImgBack);
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] GetScanResultAsync CompressImageIfLarger --> dataImageSrcBack.Length: {dataImageSrcBack.Length} skBmpSrcBack W:{skBmpSrcBack.Width} H:{skBmpSrcBack.Height}");
                    // Read text from image by OCR (Azure Visoin) 
                    SKData skDataImgBack = skImgBack.Encode();
                    byte[] dataImgBack = skDataImgBack.ToArray();
                    imageSrcB64Back = Convert.ToBase64String(dataImgBack);
                    AzureScanOCRResult azureScanOCRResultBack = await AzureScanOCRAsync(dataImgBack);
                    if (azureScanOCRResultBack == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] GetScanResultAsync AzureScanOCRAsync failed. return null");
                    }
                    else
                    {
                        ReadResult readResultBack = azureScanOCRResultBack.ReadResult;
                        string resultJsonStringBack = azureScanOCRResultBack.ResultJsonString;

                        if (readResultBack == null)
                        {
                            System.Diagnostics.Debug.WriteLine("No valid readResultBack found in operationResult.AnalyzeResult.ReadResults");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] readResultBack.Angle: {readResultBack.Angle} readResultBack.Width: {readResultBack.Width} readResultBack.Height: {readResultBack.Height}");

                            SKBitmap bitmapRotatedBack = null;
                            linesAllScanIDBack = RotateBitmapAndLines(skBmpSrcBack, readResultBack, out bitmapRotatedBack);

                            //
                            // Detect document type and extract fields
                            //
                            Console.WriteLine("==== Merged lines ====");
                            ScanID.Line[] linesMergedAllScanIDBack = scanIDOCR.MergeLinesInSameYPosIntoOneLine(linesAllScanIDBack, 5f).ToArray();
                            foreach (ScanID.Line line in linesMergedAllScanIDBack)
                            {
                                Console.WriteLine(line.ExtToString());
                            }
                            Console.WriteLine("======================");

                            scanIDOCR.FindLabels(linesMergedAllScanIDBack, "PHNIBK", out labelsFoundBack, out linesNotLabelBack);
                        }
                    }
                }
            }

            ScanIDResult result = null;
            if (!string.IsNullOrEmpty(docType))
            {
                ImgProcLib.MatchTemplateIDCard matchTemplate = scanIDOCR.GetMatchTemplate(docType);
                if (matchTemplate != null)
                {
                    switch (docType)
                    {
                        case "MYKAD":
                            result = scanIDOCR.ScanMyKad(linesMergedAllScanID, imageRotated, matchTemplate, labelsFound.ToArray(), linesNotLabel.ToArray());
                            break;
                        case "MYDL":
                            result = scanIDOCR.ScanMYDL(linesMergedAllScanID, imageRotated, matchTemplate, labelsFound.ToArray(), linesNotLabel.ToArray());
                            break;
                        case "PHDL":
                            result = scanIDOCR.ScanPHDL(linesMergedAllScanID, imageRotated, matchTemplate, labelsFound.ToArray(), linesNotLabel.ToArray());
                            break;
                        case "PHUMID":
                            result = scanIDOCR.ScanPHUMID(linesMergedAllScanID, imageRotated, rcFace, matchTemplate, labelsFound.ToArray(), linesNotLabel.ToArray());
                            break;
                        case "PHUMID1":
                            result = scanIDOCR.ScanPHUMID1(linesMergedAllScanID, imageRotated, rcFace, matchTemplate, labelsFound.ToArray(), linesNotLabel.ToArray());
                            break;
                        case "PHUMID2":
                            result = scanIDOCR.ScanPHUMID2(linesMergedAllScanID, imageRotated, rcFace, matchTemplate, labelsFound.ToArray(), linesNotLabel.ToArray());
                            break;
                        case "PHNI":
                            result = scanIDOCR.ScanPHNI(linesMergedAllScanID, imageRotated, matchTemplate, labelsFound.ToArray(), linesNotLabel.ToArray(), labelsFoundBack.ToArray(), linesNotLabelBack.ToArray(), imageSrcB64Back);
                            break;
                        default:
                            return new ScanIDResult() { Error = $"ScanID error: doc type [{docType}] not supported." };
                    }
                    if (result != null)
                    {
                        result.faceImageBase64 = b64ImageFace;
                    }
                }
            }

            if (result != null)
            {
                //resultAngle = readResult.Angle;
                result.resultJsonStringOCR = resultJsonString;
            }

            if (result == null)
            {
                System.Diagnostics.Debug.WriteLine("result is null");
                result = new ScanIDResult();
                result.resultJsonStringOCR = resultJsonString; // maybe we can read reason of failure from this json
                return result;
            }

            if (!result.Success)
            {
                System.Diagnostics.Debug.WriteLine("result is not success");
                return result;
            }

            return result;
        }

        //https://stackoverflow.com/questions/45077047/rotate-photo-with-skiasharp
        public static SKBitmap Rotate(SKBitmap bitmap, double angle)
        {
            double radians = Math.PI * angle / 180;
            float sine = (float)Math.Abs(Math.Sin(radians));
            float cosine = (float)Math.Abs(Math.Cos(radians));
            int originalWidth = bitmap.Width;
            int originalHeight = bitmap.Height;
            int rotatedWidth = (int)(cosine * originalWidth + sine * originalHeight);
            int rotatedHeight = (int)(cosine * originalHeight + sine * originalWidth);

            var rotatedBitmap = new SKBitmap(rotatedWidth, rotatedHeight);

            using (var surface = new SKCanvas(rotatedBitmap))
            {
                surface.Clear();
                surface.Translate(rotatedWidth / 2, rotatedHeight / 2);
                surface.RotateDegrees((float)angle);
                surface.Translate(-originalWidth / 2, -originalHeight / 2);
                surface.DrawBitmap(bitmap, new SKPoint());
            }
            return rotatedBitmap;
        }

        static byte[] CompressImageIfLarger(int largerThanInBytes, byte[] dataImageSrc)
        {
            do
            {
                using (MemoryStream ms = new MemoryStream(dataImageSrc))
                {
                    SKData skData = SKData.Create(ms);
                    SKCodec skCodec = SKCodec.Create(skData);
                    SKImage skImg = SKImage.FromEncodedData(dataImageSrc);
                    SKBitmap skBmp = SKBitmap.FromImage(skImg);
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CompressImageIfLarger skBmp W:{skBmp.Width} H:{skBmp.Height} Length:{ms.Length}");
                    if (ms.Length >= largerThanInBytes || skCodec.EncodedFormat != SKEncodedImageFormat.Jpeg)
                    {
                        long lengthOriginal = dataImageSrc.Length;
                        if (ms.Length >= largerThanInBytes)
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] dataImageSrc.Length {dataImageSrc.Length} > 4MB --> compress to Jpeg...");

                        if (skCodec.EncodedFormat != SKEncodedImageFormat.Jpeg)
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] skCodec.EncodedFormat  {skCodec.EncodedFormat} --> compress to Jpeg...");

                        var skImageJpeg = skBmp.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 90);
                        dataImageSrc = skImageJpeg.ToArray();
                        if (dataImageSrc.Length >= lengthOriginal)
                        {
                            // compress more...
                            var skImageJpeg75 = skBmp.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 75);
                            dataImageSrc = skImageJpeg75.ToArray();
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] dataImageSrc.Length {dataImageSrc.Length}");
                }
            }
            while (dataImageSrc.Length >= largerThanInBytes);

            return dataImageSrc;
        }

        static byte[] CompressImageIfLarger(int lartherThanInBytes, int widerThan, SKBitmap bitmapSrc)
        {

            while (bitmapSrc.Width > widerThan || bitmapSrc.Height > widerThan)
            {
                int originalWidth = bitmapSrc.Width;
                int originalHeight = bitmapSrc.Height;
                //int originalLen = dataImageSrc.Length;
                int scaledWidth = bitmapSrc.Width;
                int scaledHeight = bitmapSrc.Height;
                if (bitmapSrc.Width > bitmapSrc.Height)
                {
                    scaledWidth = widerThan;
                    scaledHeight = (int)(bitmapSrc.Height * ((float)widerThan / (float)bitmapSrc.Width));
                }
                else
                {
                    scaledHeight = widerThan;
                    scaledWidth = (int)(bitmapSrc.Width * ((float)widerThan / (float)bitmapSrc.Height));
                }
                SKBitmap scaledBmp = new SKBitmap(scaledWidth, scaledHeight, bitmapSrc.ColorType, bitmapSrc.AlphaType, bitmapSrc.ColorSpace);
                bitmapSrc.ScalePixels(scaledBmp, SKFilterQuality.Medium);
                bitmapSrc = scaledBmp;
                //skDateImageJpeg = bitmapSrc.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 80);
                //dataImageSrc = skDateImageJpeg.ToArray();
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CompressImageIfLarger bitmapSrc W:{originalWidth} H:{originalHeight} --> W:{scaledWidth} H:{scaledHeight}");
            }

            var skDateImageJpeg = bitmapSrc.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 80);
            byte[] dataImageSrc = skDateImageJpeg.ToArray();
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] dataImageSrc.Length {dataImageSrc.Length}");

            while (dataImageSrc.Length > lartherThanInBytes)
            {
                using (MemoryStream ms = new MemoryStream(dataImageSrc))
                {
                    SKData skData = SKData.Create(ms);
                    SKImage skImg = SKImage.FromEncodedData(dataImageSrc);
                    SKBitmap skBmp = SKBitmap.FromImage(skImg);
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CompressImageIfLarger skBmp W:{skBmp.Width} H:{skBmp.Height} Length:{ms.Length}");
                    //SKBitmap skBmp2 = SkiaSharp.SKBitmap.Decode(skData);
                    //System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CompressImageIfLarger skBmp2 W:{skBmp2.Width} H:{skBmp2.Height} Length:{ms.Length}");
                    if (ms.Length >= lartherThanInBytes)
                    {
                        if (ms.Length >= lartherThanInBytes)
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] dataImageSrc.Length {dataImageSrc.Length} > 4MB --> compress to Jpeg...");
                        var skImageJpeg = skBmp.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 80);
                        dataImageSrc = skImageJpeg.ToArray();
                        bitmapSrc = skBmp;
                    }
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] dataImageSrc.Length {dataImageSrc.Length}");
                }
            }

            return dataImageSrc;
        }
    }
}