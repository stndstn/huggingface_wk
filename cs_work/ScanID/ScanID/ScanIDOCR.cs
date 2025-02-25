using DlibDn47;
using ImgProcLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Tesseract;
using ZXing;
using static System.Net.Mime.MediaTypeNames;
using static ZXing.QrCode.Internal.Mode;


namespace ScanID
{
    public class ScanIDOCR
    {
        //const string DEBUG_OUTPUT_FOLDER = "c:\\temp\\";  //for IIS
        const string DEBUG_OUTPUT_FOLDER = "";

        static readonly char[] SEPARATOR_COMMA_DOT_BLANK = { ',', '.', ' ' };
        static readonly char[] SEPARATOR_BLANK = { ' ' };

        //Regex regexValidFromValidUntil = new Regex(@"\d{1,2}/\d{1,2}/\d{4} - \d{1,2}/\d{1,2}/\d{4}");
        static readonly Regex regexValidFromValidUntil = new Regex(@"\d{1,2}[\s\/]+\d{1,2}[\s\/]+\d{4}[\s\-|]*\d{1,2}[\s\/]+\d{1,2}[\s\/]+\d{4}");
        static readonly Regex regexValidDate = new Regex(@"\d{1,2}[\s\/]+\d{1,2}[\s\/]+\d{4}");
        static readonly Regex regexNationality = new Regex(@"^[a-zA-Z]{3}$|^MALAYSIA$");
        static readonly Regex regexFiveDigitsNumber = new Regex(@"^\d{5}$");
        static readonly string[] docTypesMY = { "MYKAD", "MYDL" };
        static readonly string[] docTypesPH = { "PHDL", "PHUMID1", "PHUMID2", "PHNI" };

        LabelInfo m_labelMYDL_LESEN_MEMANDU = new LabelInfo("LESEN MEMANDU", 0.15f, 1.05f, 0.6f, true);
        LabelInfo m_labelMYDL_MALAYSIA = new LabelInfo("MALAYSIA", 0.2f, 2.475f, true);
        //LabelInfo labelDRIVING_LICENCE_MALAYSIA = new LabelInfo("DRIVING LICENCE MALAYSIA");
        LabelInfo m_labelMYDL_DRIVING_LICENCE = new LabelInfo("DRIVING LICENCE", 0.25f, 1.05f, 0.6f, true);
        //LabelInfo labelDRIVING = new LabelInfo("DRIVING");
        //LabelInfo labelLICENCE = new LabelInfo("LICENCE");
        LabelInfo m_labelMYDL_Warganegara_Nationality = new LabelInfo("Warganegara / Nationality", 0.71f, 1.5f, 0.6f);
        LabelInfo m_labelMYDL_No_Pengenalan_Identity_No = new LabelInfo("No. Pengenalan / Identity No.", 0.71f, 2.35f, 0.6f);
        LabelInfo m_labelMYDL_Warganegara_Nationality_No_Pengenalan_Identity_No = new LabelInfo("Warganegara / Nationality No. Pengenalan / Identity No.", 0.7f, 1.9f, 0.6f);
        LabelInfo m_labelMYDL_Kelas_Class = new LabelInfo("Kelas / Class", 0.93f, 1.32f, 0.6f);
        LabelInfo m_labelMYDL_Tempoh_Validity = new LabelInfo("Tempoh / Validity", 1.14f, 1.16, 0.6f);
        LabelInfo m_labelMYDL_Alamat_Address = new LabelInfo("Alamat / Address", 1.4f, 1.4f, 0.6f);

        static readonly Regex regexMyKadIDNum = new Regex(@"^\d{6}-\d{2}-\d{4}$");
        static readonly Regex regexNum10DigitOrMore = new Regex(@"^\d{10,}$");
        LabelInfo m_labelMyKadKAD_PENGENALAN = new LabelInfo("KAD PENGENALAN", 0.15f, 1.0f, 0.6f, true);
        LabelInfo m_labelMyKadMALAYSIA = new LabelInfo("MALAYSIA", 0.25f, 1.0f, 0.6f, true);
        LabelInfo m_labelMyKadIDENTITY_CARD = new LabelInfo("IDENTITY CARD", 0.35f, 1.0f, 0.6f, true);
        LabelInfo m_labelMyKadIDNUM = new LabelInfo(regexMyKadIDNum, 0.55f, 0.65f);
        LabelInfo m_labelMyKadIDNUM_UnderFaceImage = new LabelInfo(regexMyKadIDNum, 1.75f, 2.7f);
        LabelInfo m_labelMyKadWARGANEGARA = new LabelInfo("WARGANEGARA", 1.85f, 2.7f, 0.6f);
        LabelInfo m_labelMyKadLELAKI = new LabelInfo("LELAKI", 1.95f, 2.9f, 0.6f);
        LabelInfo m_labelMyKadPEREMPUAN = new LabelInfo("PEREMPUAN", 1.95f, 2.95f, 0.6f);

        LabelInfo m_labelPHUMID_REPUBLIC_OF_THE_PHILIPPINES = new LabelInfo("REPUBLIC OF THE PHILIPPINES", 0.21f, 1.625f, 0.6f, true);
        LabelInfo m_labelPHUMID_Unified_Multi_Purpose_ID = new LabelInfo("Unified Multi-Purpose ID", 0.32f, 1.625f, 0.6f, true);
        LabelInfo m_labelPHUMID_CRN = new LabelInfo("CRN", 0.63f, null, true, 1.0f, true);
        LabelInfo m_labelPHUMID_SURNAME_FollowedByField = new LabelInfo("SURNAME", 0.84f, null, true, 0.6f);
        LabelInfo m_labelPHUMID_SURNAME = new LabelInfo("SURNAME", 0.84f, null, false, 0.6f);
        LabelInfo m_labelPHUMID_GIVEN_NAME_FollowedByField = new LabelInfo("GIVEN NAME", 1.04f, null, true, 0.6f);
        LabelInfo m_labelPHUMID_GIVEN_NAME = new LabelInfo("GIVEN NAME", 1.04f, null, false, 0.6f);
        LabelInfo m_labelPHUMID_MIDDLE_NAME_FollowedByField = new LabelInfo("MIDDLE NAME", 1.35f, null, true, 0.6f);
        LabelInfo m_labelPHUMID_MIDDLE_NAME = new LabelInfo("MIDDLE NAME", 1.35f, null, false, 0.6f);
        LabelInfo m_labelPHUMID_ADDRESS_LeftAligned = new LabelInfo("ADDRESS", 1.52f, null);
        LabelInfo m_labelPHUMID_ADDRESS = new LabelInfo("ADDRESS", 1.61f, null);

        LabelInfo m_labelPHUMID1_REPUBLIC_OF_THE_PHILIPPINES = new LabelInfo("REPUBLIC OF THE PHILIPPINES", 0.21f, 1.625f, 0.6f, true);
        LabelInfo m_labelPHUMID1_Unified_Multi_Purpose_ID = new LabelInfo("Unified Multi-Purpose ID", 0.32f, 1.625f, 0.6f, true);
        LabelInfo m_labelPHUMID1_CRN = new LabelInfo("CRN", 0.63f, null, true, 1.0f, true);
        LabelInfo m_labelPHUMID1_SURNAME_FollowedByField = new LabelInfo("SURNAME", 0.84f, null, true, 0.6f, true);
        LabelInfo m_labelPHUMID1_GIVEN_NAME_FollowedByField = new LabelInfo("GIVEN NAME", 1.04f, null, true, 0.6f, true);
        LabelInfo m_labelPHUMID1_MIDDLE_NAME_FollowedByField = new LabelInfo("MIDDLE NAME", 1.35f, null, true, 0.6f, true);
        LabelInfo m_labelPHUMID1_ADDRESS_LeftAligned = new LabelInfo("ADDRESS", 1.52f, null);

        LabelInfo m_labelPHUMID2_REPUBLIC_OF_THE_PHILIPPINES = new LabelInfo("REPUBLIC OF THE PHILIPPINES", 0.21f, 1.625f, 0.6f, true);
        LabelInfo m_labelPHUMID2_Unified_Multi_Purpose_ID = new LabelInfo("Unified Multi-Purpose ID", 0.32f, 1.625f, 0.6f, true);
        LabelInfo m_labelPHUMID2_CRN = new LabelInfo("CRN", 0.63f, null, true, 1.0f, true);
        LabelInfo m_labelPHUMID2_SURNAME = new LabelInfo("SURNAME", 0.84f, null, false, 0.6f, true);
        LabelInfo m_labelPHUMID2_GIVEN_NAME = new LabelInfo("GIVEN NAME", 1.04f, null, false, 0.6f, true);
        LabelInfo m_labelPHUMID2_MIDDLE_NAME = new LabelInfo("MIDDLE NAME", 1.35f, null, false, 0.6f, true);
        LabelInfo m_labelPHUMID2_ADDRESS = new LabelInfo("ADDRESS", 1.61f, null);


        LabelInfo m_labelPHDL_REPUBLIC_OF_THE_PHILIPPINES = new LabelInfo("REPUBLIC OF THE PHILIPPINES", 0.13f, 1.625f, 0.6f, true);
        LabelInfo m_labelPHDL_DEPARTMENT_OF_TRANSPORTATION = new LabelInfo("DEPARTMENT OF TRANSPORTATION", 0.26, 1.625f, 0.6f, true);
        LabelInfo m_labelPHDL_LAND_TRANSPORTATION_OFFICE = new LabelInfo("LAND TRANSPORTATION OFFICE", 0.35f, 1.625f, 0.6f, true);
        LabelInfo m_labelPHDL_NON_PROFESSIONAL_DRIVERS_LICENSE = new LabelInfo("NON-PROFESSIONAL DRIVER'S LICENSE", 0.46f, 1.625f, 0.6f, true);
        LabelInfo m_labelPHDL_PROFESSIONAL_DRIVERS_LICENSE = new LabelInfo("PROFESSIONAL DRIVER'S LICENSE", 0.46f, 1.625f, 0.6f, true);
        LabelInfo m_labelPHDL_DRIVERS_LICENSE = new LabelInfo("DRIVER'S LICENSE", 0.46f, null, 0.6f, true);
        LabelInfo m_labelPHDL_Last_Name_First_Name_Middle_Name = new LabelInfo("Last Name, First Name, Middle Name", 0.68f, null);
        LabelInfo m_labelPHDL_Last_Name_First_Name = new LabelInfo("Last Name, First Name", 0.68f, null);
        LabelInfo m_labelPHDL_Middle_Name = new LabelInfo("Middle Name", 0.68f, null);
        LabelInfo m_labelPHDL_Last_Name = new LabelInfo("Last Name", 0.68f, null);
        LabelInfo m_labelPHDL_First_Name_Middle_Name = new LabelInfo("First Name, Middle Name", 0.68f, null);
        LabelInfo m_labelPHDL_First_Name = new LabelInfo("First Name", 0.68f, null);
        LabelInfo m_labelPHDL_Nationality = new LabelInfo("Nationality", 0.9f, null);
        LabelInfo m_labelPHDL_Sex = new LabelInfo("Sex");
        LabelInfo m_labelPHDL_DateOfBirth = new LabelInfo("Date Of Birth");
        LabelInfo m_labelPHDL_Weight_kg_Height_m = new LabelInfo("Weight (kg) Height(m)");
        LabelInfo m_labelPHDL_Weight_kg = new LabelInfo("Weight (kg)");
        LabelInfo m_labelPHDL_Height_m = new LabelInfo("Height(m)");
        LabelInfo m_labelPHDL_Address = new LabelInfo("Address", 1.11f, null);
        LabelInfo m_labelPHDL_License_No = new LabelInfo("License No.");
        LabelInfo m_labelPHDL_Expiration_Date = new LabelInfo("Expiration Date");
        LabelInfo m_labelPHDL_Agency_Code = new LabelInfo("Agency Code");
        LabelInfo m_labelPHDL_Blood_Type = new LabelInfo("Blood Type");
        LabelInfo m_labelPHDL_Eyes_Color = new LabelInfo("Eyes Color");
        LabelInfo m_labelPHDL_Restrictions = new LabelInfo("Restrictions");
        LabelInfo m_labelPHDL_Conditions = new LabelInfo("Conditions");


        static readonly Regex regexPCN = new Regex(@"^\d{4}-\d{4}-\d{4}-\d{4}$");
        LabelInfo m_labelPHNI_REPUBLIKA_NG_PILIPINAS = new LabelInfo("REPUBLIKA NG PILIPINAS", 0.147f, 1.625f, 0.6f, true);
        LabelInfo m_labelPHNI_Republic_of_the_Philippines = new LabelInfo("Republic of the Philippines", 0.236f, 1.625f, 0.6f, true);
        LabelInfo m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN = new LabelInfo("PAMBANSANG PAGKAKAKILANLAN", 0.354f, 1.625f, 0.6f, true);
        LabelInfo m_labelPHNI_Philippine_Identification_Card = new LabelInfo("Philippine Identification Card", 0.472f, 1.625f, 0.6f, true);
        LabelInfo m_labelPHNI_PCN = new LabelInfo(regexPCN, 0.649f, null);
        LabelInfo m_labelPHNI_Apelyido_Last_Name = new LabelInfo("Apelyido/Last Name", 0.767f, null, 0.6f);
        LabelInfo m_labelPHNI_Mga_Pangalan_Given_Names = new LabelInfo("Mga Pangalan/Given Names", 1.003f, null, 0.6f);
        LabelInfo m_labelPHNI_Gitnang_Apelyido_Middle_Name = new LabelInfo("Gitnang Apelyido/Middle Name", 1.327f, null, 0.6f);
        LabelInfo m_labelPHNI_Petsa_ng_Kapanganakan_Date_of_Birth = new LabelInfo("Petsa ng Kapanganakan/Date of Birth", 1.56f, null, 0.6f);
        LabelInfo m_labelPHNI_PHL = new LabelInfo("PHL", 1.726f, 3.18f, 1.0, true);
        LabelInfo m_labelPHNI_Tirahan_Address = new LabelInfo("Tirahan/Address", 1.811f, null, 0.6f);


        LabelInfo m_labelPHNIBK_Araw_ng_pagkaka_loob_Date_of_issue = new LabelInfo("Araw ng pagkakaloob/Date of issue", 0.5f, 0.5f, 0.6f);
        LabelInfo m_labelPHNIBK_Kasarian_Sex = new LabelInfo("Kasarian/Sex", 0.77f, 0.33f, 0.6f);
        LabelInfo m_labelPHNIBK_labelUri_ng_Dugo_Blood_Type = new LabelInfo("Uri ng Dugo/Blood Type", 0.95f, 0.46f, 0.6f);
        LabelInfo m_labelPHNIBK_Kalagayang_Sibil_Marital_Status = new LabelInfo("Kalagayang Sibil/Marital Status", 1.14f, 0.666f, 0.6f);
        LabelInfo m_labelPHNIBK_Lugar_ng_Kapanganakan_Place_of_Birth = new LabelInfo("Lugar ng Kapanganakan/Place of Birth".ToLower(), 1.33f, 0.75f, 0.6f);
        LabelInfo m_labelPHNIBK_If_found_please_return_to_the_nearest = new LabelInfo("If found, please return to the nearest".ToLower(), 1.91f, 0.75f, 0.6f);
        LabelInfo m_labelPHNIBK_PSA_Office = new LabelInfo("PSA Office".ToLower(), 2.02f, 0.312f, 0.6f);
        LabelInfo m_labelPHNIBK_WWW_Psa_gov_ph = new LabelInfo("www.psa.gov.ph".ToLower(), 2.02f, 1.71f, 0.6f);


        string m_country = "";
        string[] m_docTypes = { };
        Dictionary<string, ImgProcLib.MatchTemplateIDCard> m_matchTemplates = null;
        protected ScanIDOCR(string country, Dictionary<string, ImgProcLib.MatchTemplateIDCard> matchTemplates)
        {
            m_country = country.ToUpper();
            switch (m_country)
            {
                case "MY":
                    m_docTypes = docTypesMY;
                    break;
                case "PH":
                    m_docTypes = docTypesPH;
                    break;
                default:
                    break;
            }
            m_matchTemplates = matchTemplates;
        }

        static Dictionary<string, ImgProcLib.MatchTemplateIDCard?> s_dictMatchTemplate = new Dictionary<string, ImgProcLib.MatchTemplateIDCard?>();
        static ImgProcLib.MatchTemplateIDCard? GetMatchTemplate(string strPathTmplDir, string name)
        {
            if(s_dictMatchTemplate.ContainsKey(name) == false) 
                s_dictMatchTemplate.Add(name, ScanIDOCR.LoadMatchTemplate(strPathTmplDir, name)); 
            return s_dictMatchTemplate[name]; 
        }


        public static ScanIDOCR Create(string strPathTmplDir, string country)
        {
            switch (country.ToUpper())
            {
                case "MY":
                    {
                        ImgProcLib.MatchTemplateIDCard? matchTemplateMyKad = ScanIDOCR.GetMatchTemplate(strPathTmplDir, "mykad_fr");
                        ImgProcLib.MatchTemplateIDCard? matchTemplateMYDL = ScanIDOCR.GetMatchTemplate(strPathTmplDir, "mydl_fr");
                        Dictionary<string, ImgProcLib.MatchTemplateIDCard?> matchTemplatesMY = new Dictionary<string, ImgProcLib.MatchTemplateIDCard?>(){
                            { "MYKAD", matchTemplateMyKad },
                            { "MYDL", matchTemplateMYDL },
                        };
                        return new ScanIDOCR(country.ToUpper(), matchTemplatesMY);
                    }
                    break;
                case "PH":
                    {
                        ImgProcLib.MatchTemplateIDCard? matchTemplatePHDL = ScanIDOCR.GetMatchTemplate(strPathTmplDir, "phdl_fr");
                        ImgProcLib.MatchTemplateIDCard? matchTemplatePHUMID = ScanIDOCR.GetMatchTemplate(strPathTmplDir, "phumid_fr");
                        ImgProcLib.MatchTemplateIDCard? matchTemplatePHNI = ScanIDOCR.GetMatchTemplate(strPathTmplDir, "phni_fr");
                        Dictionary<string, ImgProcLib.MatchTemplateIDCard?> matchTemplatesPH = new Dictionary<string, ImgProcLib.MatchTemplateIDCard?>(){
                            { "PHDL", matchTemplatePHDL },
                            { "PHUMID", matchTemplatePHUMID },
                            { "PHUMID1", matchTemplatePHUMID },
                            { "PHUMID2", matchTemplatePHUMID },
                            { "PHNI", matchTemplatePHNI }
                        };
                        return new ScanIDOCR(country.ToUpper(), matchTemplatesPH);
                    }
                    break;
                default:
                    return new ScanIDOCR(country.ToUpper(), new Dictionary<string, ImgProcLib.MatchTemplateIDCard?>());
                    break;
            }
        }


        public ImgProcLib.MatchTemplateIDCard GetMatchTemplate(string docType)
        {
            if(m_matchTemplates != null && m_matchTemplates.ContainsKey(docType.ToUpper()))
            {
                return m_matchTemplates[docType.ToUpper()];
            }
            return null;
        }

        string EncodeImageFileToBase64(string imageFileName)
        {
            if (string.IsNullOrEmpty(imageFileName))
            {
                return "";
            }

            if (!System.IO.File.Exists(imageFileName))
            {
                Console.WriteLine("File not found: " + imageFileName);
                throw new Exception("File not found: " + imageFileName);
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
                throw new Exception("File is empty: " + imageFileName);
            }

            return b64Image;
        }

        byte[] LoadImageFile(string imageFileName)
        {
            if (!System.IO.File.Exists(imageFileName))
            {
                Console.WriteLine("File not found: " + imageFileName);
                throw new Exception("File not found: " + imageFileName);
            }

            using (var stream = System.IO.File.OpenRead(imageFileName))
            {
                byte[] b = new byte[stream.Length];
                stream.Read(b, 0, b.Length);
                return b;
            }
        }

        List<Line> ScanEachLineWithTesseract(List<Line> lines, SkiaSharp.SKImage bmpImage, out List<Line> linesTess)
        {
            linesTess = new List<Line>();
            //System.Drawing.Image imageBmp = System.Drawing.Image.FromStream(new MemoryStream(bmpImage.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100).ToArray()));
            //imageBmp.Save("imageBmp.png", System.Drawing.Imaging.ImageFormat.Png);
            const double IMAGE_MARGIN_RATE = 0.075f;
            const double CONFIDENCE_THRESHOLD = 0.875f;

            foreach (Line line in lines)
            {
                SkiaSharp.SKImage bmpLine = null;
                SkiaSharp.SKRectI rect = SkiaSharp.SKRectI.Empty;
                if (line.BoundingBox.Count == 4)
                {
                    int left = (int)(line.BoundingBox[0] - line.ExtGetWidth() * IMAGE_MARGIN_RATE);
                    if(left < 0) left = 0;
                    int top = (int)(line.BoundingBox[1] - line.ExtGetHeight() * IMAGE_MARGIN_RATE);
                    if (top < 0) top = 0;
                    int right = (int)(line.BoundingBox[2] + line.ExtGetWidth() * IMAGE_MARGIN_RATE);
                    if (right > bmpImage.Width) right = bmpImage.Width;
                    int bottom = (int)(line.BoundingBox[3] + line.ExtGetHeight() * IMAGE_MARGIN_RATE);
                    if (bottom > bmpImage.Height) bottom = bmpImage.Height;

                    rect = new SkiaSharp.SKRectI(left, top, right, bottom);
                    bmpLine = bmpImage.Subset(rect);

                    //System.Drawing.Image imageLine = System.Drawing.Image.FromStream(new MemoryStream(bmpLine.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100).ToArray()));
                    //imageLine.Save("imageLine.png", System.Drawing.Imaging.ImageFormat.Png);
                }
                else if (line.BoundingBox.Count == 8)
                {
#if true
                    //rect = new SkiaSharp.SKRectI((int)line.BoundingBox[0], (int)line.BoundingBox[1], (int)line.BoundingBox[4], (int)line.BoundingBox[5]);
                    rect = new SkiaSharp.SKRectI((int)line.ExtGetLeft(), (int)line.ExtGetTop(), (int)line.ExtGetRight(), (int)line.ExtGetBottom());
                    bmpLine = bmpImage.Subset(rect);
#else
                    continue;   // no need to scan with tesseract for Florence-base 
#endif
                }

                if (rect.IsEmpty)
                    continue;

                if (bmpLine == null)
                    continue;

                SKData skData = bmpLine.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                var tessBlocks = OCRLinesWithTesseractEncodedData(skData.ToArray());
                tessBlocks = MergeLinesInSameYPosIntoOneLine(tessBlocks).ToList();
                List<Line> validLines = new List<Line>();
                if (linesTess != null)
                {
                    foreach (Line lineTess in tessBlocks)
                    {
                        if (string.IsNullOrWhiteSpace(lineTess.Text))
                            continue;

                        validLines.Add(lineTess);
                    }
                }

                if (validLines.Count == 1)
                {
                    Line lineValid = validLines[0];
                    if (!string.IsNullOrWhiteSpace(lineValid.Text)
                        && lineValid.Confidence != null
                        && lineValid.Confidence.Value > 0.5)
                    {
                        if (linesTess != null)
                            linesTess.Add(lineValid);
                        System.Diagnostics.Debug.WriteLine("Before: " + line.ExtToString());
                        // take boundingBox and baseline from Tesseract to accurate line height.
                        // but take scanned text only if confidence > 0.875

                        if (lineValid.Confidence.Value > CONFIDENCE_THRESHOLD && lineValid.Text.Length > (int)((double)line.Text.Length * CONFIDENCE_THRESHOLD))
                        {
                            line.Text = lineValid.Text.Trim().ToUpper();
                            line.Confidence = lineValid.Confidence;
                        }
                        if (line.BoundingBox.Count == 4)
                        {
                            // update bounding box
                            line.BoundingBox[0] = line.BoundingBox[0] + lineValid.BoundingBox[0];
                            line.BoundingBox[1] = line.BoundingBox[1] + lineValid.BoundingBox[1];
                            line.BoundingBox[2] = line.BoundingBox[0] + lineValid.BoundingBox[2];
                            line.BoundingBox[3] = line.BoundingBox[1] + lineValid.BoundingBox[3];
                            line.Baseline = new List<double?> {
                                        line.BoundingBox[0] + lineValid.Baseline[0],
                                        line.BoundingBox[1] + lineValid.Baseline[1],
                                        line.BoundingBox[0] + lineValid.Baseline[2],
                                        line.BoundingBox[1] + lineValid.Baseline[3]
                                    };
                        }
                        System.Diagnostics.Debug.WriteLine("After: " + line.ExtToString());
                    }
                }
                else
                {
                    if (validLines.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine("tessBlocks.Count: " + tessBlocks.Count);
                    }
                }
            }
            return lines;
        }

        LabelInfo[] GetLabelsToFind(string docType)
        {
            switch (docType.ToUpper())
            {
                case "MYKAD":
                    return new LabelInfo[] {
                        m_labelMyKadKAD_PENGENALAN, m_labelMyKadMALAYSIA, m_labelMyKadIDENTITY_CARD, m_labelMyKadIDNUM,
                        m_labelMyKadIDNUM_UnderFaceImage, m_labelMyKadWARGANEGARA, m_labelMyKadLELAKI, m_labelMyKadPEREMPUAN
                    };
                case "MYDL":
                    return new LabelInfo[] {
                        m_labelMYDL_LESEN_MEMANDU, m_labelMYDL_MALAYSIA, m_labelMYDL_DRIVING_LICENCE, m_labelMYDL_Warganegara_Nationality,
                        m_labelMYDL_No_Pengenalan_Identity_No, m_labelMYDL_Warganegara_Nationality_No_Pengenalan_Identity_No,
                        m_labelMYDL_Kelas_Class, m_labelMYDL_Tempoh_Validity, m_labelMYDL_Alamat_Address
                    };
                case "PHUMID":
                    return new LabelInfo[] {
                        m_labelPHUMID_REPUBLIC_OF_THE_PHILIPPINES, m_labelPHUMID_Unified_Multi_Purpose_ID, m_labelPHUMID1_CRN,
                        m_labelPHUMID_SURNAME_FollowedByField, m_labelPHUMID_GIVEN_NAME_FollowedByField,
                        m_labelPHUMID_MIDDLE_NAME_FollowedByField, m_labelPHUMID_SURNAME, m_labelPHUMID_GIVEN_NAME, m_labelPHUMID_MIDDLE_NAME,
                        m_labelPHUMID_ADDRESS_LeftAligned, m_labelPHUMID_ADDRESS
                    };
                case "PHUMID1":
                    return new LabelInfo[] {
                        m_labelPHUMID1_REPUBLIC_OF_THE_PHILIPPINES, m_labelPHUMID1_Unified_Multi_Purpose_ID, m_labelPHUMID1_CRN,
                        m_labelPHUMID1_SURNAME_FollowedByField, m_labelPHUMID1_GIVEN_NAME_FollowedByField, 
                        m_labelPHUMID1_MIDDLE_NAME_FollowedByField, m_labelPHUMID1_ADDRESS_LeftAligned
                    };
                case "PHUMID2":
                    return new LabelInfo[] {
                        m_labelPHUMID2_REPUBLIC_OF_THE_PHILIPPINES, m_labelPHUMID2_Unified_Multi_Purpose_ID, m_labelPHUMID2_CRN,
                        m_labelPHUMID2_SURNAME, m_labelPHUMID2_GIVEN_NAME, m_labelPHUMID2_MIDDLE_NAME, m_labelPHUMID2_ADDRESS
                    };
                case "PHDL":
                    return new LabelInfo[] {
                        m_labelPHDL_REPUBLIC_OF_THE_PHILIPPINES,
                        m_labelPHDL_DEPARTMENT_OF_TRANSPORTATION,
                        m_labelPHDL_LAND_TRANSPORTATION_OFFICE,
                        m_labelPHDL_NON_PROFESSIONAL_DRIVERS_LICENSE,
                        m_labelPHDL_PROFESSIONAL_DRIVERS_LICENSE,
                        m_labelPHDL_DRIVERS_LICENSE,
                        m_labelPHDL_Last_Name_First_Name_Middle_Name,
                        m_labelPHDL_Last_Name_First_Name,
                        m_labelPHDL_Middle_Name, m_labelPHDL_Last_Name,
                        m_labelPHDL_First_Name_Middle_Name, m_labelPHDL_First_Name,
                        m_labelPHDL_Nationality, m_labelPHDL_Sex, m_labelPHDL_DateOfBirth,
                        m_labelPHDL_Weight_kg_Height_m, m_labelPHDL_Weight_kg, m_labelPHDL_Height_m,
                        m_labelPHDL_Address, m_labelPHDL_License_No,
                        m_labelPHDL_Expiration_Date, m_labelPHDL_Agency_Code,
                        m_labelPHDL_Blood_Type, m_labelPHDL_Eyes_Color,
                        m_labelPHDL_Restrictions, m_labelPHDL_Conditions
                    };
                case "PHNI":
                    return new LabelInfo[] {
                        m_labelPHNI_REPUBLIKA_NG_PILIPINAS,
                        m_labelPHNI_Republic_of_the_Philippines,
                        m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN,
                        m_labelPHNI_Philippine_Identification_Card,
                        m_labelPHNI_PCN,
                        m_labelPHNI_Apelyido_Last_Name,
                        m_labelPHNI_Mga_Pangalan_Given_Names,
                        m_labelPHNI_Gitnang_Apelyido_Middle_Name,
                        m_labelPHNI_Petsa_ng_Kapanganakan_Date_of_Birth,
                        m_labelPHNI_PHL,
                        m_labelPHNI_Tirahan_Address
                    };
                case "PHNIBK":
                    return new LabelInfo[] {
                        m_labelPHNIBK_Araw_ng_pagkaka_loob_Date_of_issue,
                        m_labelPHNIBK_Kasarian_Sex,
                        m_labelPHNIBK_labelUri_ng_Dugo_Blood_Type,
                        m_labelPHNIBK_Kalagayang_Sibil_Marital_Status,
                        m_labelPHNIBK_Lugar_ng_Kapanganakan_Place_of_Birth,
                        m_labelPHNIBK_If_found_please_return_to_the_nearest,
                        m_labelPHNIBK_PSA_Office,
                        m_labelPHNIBK_WWW_Psa_gov_ph
                    };
                default:
                    return new LabelInfo[] { };
            }
        }

        LabelInfo[] GetLabelsAboveFields(string docType)
        {
            switch (docType.ToUpper())
            {
                case "MYKAD":
                    return new LabelInfo[] {
                        m_labelMyKadKAD_PENGENALAN,
                        m_labelMyKadMALAYSIA,
                        m_labelMyKadIDENTITY_CARD
                    };
                case "MYDL":
                    return new LabelInfo[] {
                        m_labelMYDL_LESEN_MEMANDU,
                        m_labelMYDL_MALAYSIA,
                        m_labelMYDL_DRIVING_LICENCE
                    };
                case "PHUMID1":
                    return new LabelInfo[] {
                        m_labelPHUMID1_REPUBLIC_OF_THE_PHILIPPINES,
                        m_labelPHUMID1_Unified_Multi_Purpose_ID,
                    };
                case "PHUMID2":
                    return new LabelInfo[] {
                        m_labelPHUMID2_REPUBLIC_OF_THE_PHILIPPINES,
                        m_labelPHUMID2_Unified_Multi_Purpose_ID,
                    };
                case "PHDL":
                    return new LabelInfo[] {
                        m_labelPHDL_REPUBLIC_OF_THE_PHILIPPINES,
                        m_labelPHDL_DEPARTMENT_OF_TRANSPORTATION,
                        m_labelPHDL_LAND_TRANSPORTATION_OFFICE,
                        m_labelPHDL_NON_PROFESSIONAL_DRIVERS_LICENSE,
                        m_labelPHDL_PROFESSIONAL_DRIVERS_LICENSE,
                        m_labelPHDL_DRIVERS_LICENSE
                    };
                case "PHNI":
                    return new LabelInfo[] {
                        m_labelPHNI_REPUBLIKA_NG_PILIPINAS,
                        m_labelPHNI_Republic_of_the_Philippines,
                        m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN,
                        m_labelPHNI_Philippine_Identification_Card,
                        m_labelPHNI_PHL
                    };
                default:
                    return new LabelInfo[] { };
            }
        }

        LabelInfo[] GetLabelsFooterFields(string docType)
        {
            switch (docType.ToUpper())
            {
                case "PHNIBK":
                    return new LabelInfo[] {
                    m_labelPHNIBK_If_found_please_return_to_the_nearest,
                    m_labelPHNIBK_PSA_Office,
                    m_labelPHNIBK_WWW_Psa_gov_ph
                };
                default:
                    return new LabelInfo[] { };
            }
        }

        public ScanIDResult ScanID(string baseAddrUrl, string imageFileName, string imageFileNameBack, string docType = "")
        {
            Console.WriteLine($"ScanID imageFileName: {imageFileName}");
            string b64Image = EncodeImageFileToBase64(imageFileName);
            Console.WriteLine($"ScanID imageFileNameBack: {imageFileNameBack}");
            string b64ImageBack = EncodeImageFileToBase64(imageFileNameBack);
            return ScanIDB64(baseAddrUrl, b64Image, b64ImageBack, docType);
        }

        public ScanIDResult ScanIDB64(string baseAddrUrl, string imageSrcB64, string imageSrcB64Back = "", string docType = "")
        {
            int? timeElapsedOCR = null;
            int? timeElapsedFacedetection = null;
            int? timeElapsedLandmarkDetection = null;
            int? timeElapsedTotal = null;
            DateTime dtStartTotal = DateTime.Now;

            imageSrcB64 = ResizeImageIfTooLarge(imageSrcB64); // max 2000 x 2000, size 2MB
            byte[] dataImageSrc = Convert.FromBase64String(imageSrcB64);

            DateTime dtStartOcrAndParse = DateTime.Now;
            Console.WriteLine($"PostOCRWithRegionRequest start... URL:{baseAddrUrl}");
            OCRWithRegionResponse ocrWithRegionResponse = PostOCRWithRegionRequest(baseAddrUrl, imageSrcB64);
            List<Line> lines = ocrWithRegionResponse.Lines;

            DateTime dtEndOcrAndParse = DateTime.Now;
            timeElapsedOCR = ocrWithRegionResponse.timeElapsed;
            Console.WriteLine($"(OCR {timeElapsedOCR} ms)\n");
            Console.WriteLine($"(OCR + parse {(dtEndOcrAndParse - dtStartOcrAndParse).TotalMilliseconds} ms)\n");

            // remove </s> from the start of 1st line
            if (lines.Count > 0 && (lines[0].Text.StartsWith("</s>")))
                lines[0].Text = lines[0].Text.Replace("</s>", "");
            if (lines.Count > 0 && (lines[0].Text.StartsWith("</S>")))
                lines[0].Text = lines[0].Text.Replace("</S>", "");

            foreach (Line line in lines)
            {
                Console.WriteLine(line.ExtToString());
            }
            Console.WriteLine("======================");

            // resize image if image resized in OCR
            SKData skDataImageSrc = SKData.CreateCopy(dataImageSrc);
            SKCodec codec = SKCodec.Create(skDataImageSrc);
            using (SKImage bmpImage = SKImage.FromEncodedData(skDataImageSrc))
            {
                SKImage image;
                if (ocrWithRegionResponse.ImageWidth != 0 && ocrWithRegionResponse.ImageHeight != 0
                && (bmpImage.Width != ocrWithRegionResponse.ImageWidth || bmpImage.Height != ocrWithRegionResponse.ImageHeight))
                {
                    // Load the image
                    SKBitmap srcBitmap = SKBitmap.FromImage(bmpImage);

                    // Define the new size
                    SKImageInfo newSize = new SKImageInfo(ocrWithRegionResponse.ImageWidth, ocrWithRegionResponse.ImageHeight);

                    // Resize the image
                    image = SKImage.FromBitmap(srcBitmap.Resize(newSize, SKFilterQuality.High));
                    SKData data = image.Encode(codec.EncodedFormat, 90);
                    dataImageSrc = data.ToArray();
                }
                else
                {
                    image = bmpImage;
                }
                // detect face image position
                System.ValueType valLeft = new System.Int32();
                System.ValueType valTop = new System.Int32();
                System.ValueType valRight = new System.Int32();
                System.ValueType valBottom = new System.Int32();
                bool bFaceFound = false;
                string b64ImageFace = null;
                try
                {
                    DateTime dtStartFaceDetection = DateTime.Now;
                    Console.WriteLine($"PostOCRWithRegionRequest start... URL:{baseAddrUrl}");
                    bFaceFound = DlibDn47.DlibWrapper.DetectFace(dataImageSrc, ref valLeft, ref valTop, ref valRight, ref valBottom);

                    DateTime dtEndFaceDetection = DateTime.Now;
                    timeElapsedFacedetection = (int)(dtEndFaceDetection - dtStartFaceDetection).TotalMilliseconds;
                    Console.WriteLine($"(Face detection {timeElapsedFacedetection} ms)\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("DetectFace exception: " + ex.Message);
                    return new ScanIDResult() { Error = "DetectFace exception: " + ex.Message };
                }

                if (image != null)
                {
                    // extract face image
                    SKRectI? rcFace = null;
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
                        if (rightFaceArea > image.Width) rightFaceArea = image.Width;
                        int bottomFaceArea = (int)valBottom + height / 2;
                        if (bottomFaceArea > image.Height) bottomFaceArea = image.Height;

                        rcFace = new SKRectI(leftFaceArea, topFaceArea, rightFaceArea, bottomFaceArea);
                        SKImage skImgFace = image.Subset(rcFace.Value);
                        SKData skDataJpgFace = skImgFace.Encode(SKEncodedImageFormat.Jpeg, 90);
                        byte[] dataJpgFace = skDataJpgFace.ToArray();
                        b64ImageFace = Convert.ToBase64String(dataJpgFace);
                    }

                    List<Line> linesTess;
                    lines = ScanEachLineWithTesseract(lines, image, out linesTess);
                    Console.WriteLine("==== Lines read by Tesseract ====");
                    foreach (Line lineTess in linesTess)
                    {
                        Console.WriteLine(lineTess.ExtToString());
                    }
                    Console.WriteLine("======================");

                    Console.WriteLine("==== Merged lines ====");
                    Line[] linesMergedAll = MergeLinesInSameYPosIntoOneLine(lines, 5f).ToArray();
                    foreach (Line line in linesMergedAll)
                    {
                        Console.WriteLine(line.ExtToString());
                    }
                    Console.WriteLine("======================");

                    List<LabelInfo> labelsFound = new List<LabelInfo>();
                    List<Line> linesNotLabel = new List<Line>();

                    if (!string.IsNullOrEmpty(docType))
                    {
                        //LabelInfo[] labelsToFind = GetLabelsToFind(docType);
                        //LabelInfo[] labelsAboveFields = GetLabelsAboveFields(docType);
                        FindLabels(linesMergedAll, docType, out labelsFound, out linesNotLabel);
                    }
                    else
                    {
                        docType = FindLabelsAndIdentifyDocType(linesMergedAll, out labelsFound, out linesNotLabel);
                    }

                    if (!string.IsNullOrEmpty(docType))
                    {
                        if (m_matchTemplates.ContainsKey(docType))
                        {
                            ImgProcLib.MatchTemplateIDCard matchTemplate = m_matchTemplates[docType];
                            ScanIDResult ret = null;
                            switch (docType)
                            {
                                case "MYKAD":
                                    ret = ScanMyKad(linesMergedAll, image, matchTemplate, labelsFound.ToArray(), linesNotLabel.ToArray());
                                    break;
                                case "MYDL":
                                    ret = ScanMYDL(linesMergedAll, image, matchTemplate, labelsFound.ToArray(), linesNotLabel.ToArray());
                                    break;
                                case "PHDL":
                                    ret = ScanPHDL(linesMergedAll, image, matchTemplate, labelsFound.ToArray(), linesNotLabel.ToArray());
                                    break;
                                case "PHUMID":
                                    ret = ScanPHUMID(linesMergedAll, image, rcFace, matchTemplate, labelsFound.ToArray(), linesNotLabel.ToArray());
                                    break;
                                case "PHUMID1":
                                    ret = ScanPHUMID1(linesMergedAll, image, rcFace, matchTemplate, labelsFound.ToArray(), linesNotLabel.ToArray());
                                    break;
                                case "PHUMID2":
                                    ret = ScanPHUMID2(linesMergedAll, image, rcFace, matchTemplate, labelsFound.ToArray(), linesNotLabel.ToArray());
                                    break;
                                case "PHNI":
                                    ret = ScanPHNI(linesMergedAll, image, matchTemplate, labelsFound.ToArray(), linesNotLabel.ToArray(), baseAddrUrl, imageSrcB64Back);
                                    break;
                                default:
                                    return new ScanIDResult() { Error = $"ScanID error: doc type [{docType}] not supported." };
                            }
                            if (ret != null)
                            {
                                ret.faceImageBase64 = b64ImageFace;
                                DateTime dtEndTotal = DateTime.Now;
                                ret.timeElapsedTotal = (int)(dtEndTotal - dtStartTotal).TotalMilliseconds;
                                ret.timeElapsedFaceDetection = timeElapsedFacedetection;
                                ret.timeElapsedOCR = timeElapsedOCR;
                                Console.WriteLine($"(SanIDBase64 total {timeElapsedTotal} ms)\n");
                                return ret;
                            }
                        }
                    }
                }
            }

            return new ScanIDResult() { Error = "ScanID error unknown."};
        }

        public ScanMyKadResult ScanMyKad(Line[] linesMergedAll, SKImage imageSrc, ImgProcLib.MatchTemplateIDCard? matchTemplateMyKad, LabelInfo[] labelsFound, Line[] linesMergedNotLabel)
        {
            DateTime dtStart2 = DateTime.Now;
            Console.WriteLine($"ExtractFieldsFromReadResultOfMyKad start...");
            //ScanMyKadResult scanMyKadResult = ExtractFieldsFromReadResultOfMyKad(linesMerged);
            ScanMyKadResult scanMyKadResult = ExtractFieldsFromReadResultOfMyKad(labelsFound, linesMergedNotLabel, imageSrc, matchTemplateMyKad);
            DateTime dtEnd2 = DateTime.Now;
            Console.WriteLine($"ExtractFieldsFromReadResultOfMyKad ({(dtEnd2 - dtStart2).TotalSeconds} sec)\n");

            //scanMyKadResult.landmarkImageBase64 = GenerateMatchTemplateResultImage(scanMyKadResult.MatchTemplateResults, scanMyKadResult.CardImage200ppiPngB64);

            return scanMyKadResult;
        }

        //public static ScanMYDLResult ScanMYDLB64(string baseAddrUrl, string imageSrcB64, ImgProcLib.MatchTemplateIDCard? matchTemplateMYDL)
        public ScanMYDLResult ScanMYDL(Line[] linesMergedAll, SKImage imageSrc, 
            ImgProcLib.MatchTemplateIDCard? matchTemplateMYDL, LabelInfo[] labelsFound, Line[] linesMergedNotLabel)
        {
            DateTime dtStart2 = DateTime.Now;
            ScanMYDLResult scanMYDLResult = ExtractFieldsFromReadResultOfMYDL(labelsFound, linesMergedNotLabel, imageSrc, matchTemplateMYDL);
            DateTime dtEnd2 = DateTime.Now;
            Console.WriteLine($"ExtractFieldsFromReadResultOfMYDL ({(dtEnd2 - dtStart2).TotalSeconds} sec)\n");

            //scanMYDLResult.landmarkImageBase64 = GenerateMatchTemplateResultImage(scanMYDLResult.MatchTemplateResults, scanMYDLResult.CardImage200ppiPngB64);
            /*
            if (scanMYDLResult.MatchTemplateResults != null)
            {
                if (!string.IsNullOrEmpty(scanMYDLResult.CardImage200ppiPngB64))
                {
                    byte[] dataImage200ppiPng = Convert.FromBase64String(scanMYDLResult.CardImage200ppiPngB64);
                    if (dataImage200ppiPng != null && dataImage200ppiPng.Length > 0)
                    {
                        SKData skData = SKData.CreateCopy(dataImage200ppiPng);
                        SKImage imgID200ppi = SKImage.FromEncodedData(skData);
                        SKData dataID200ppiPng = imgID200ppi.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                        SKBitmap skBmpWithLandmark = SKBitmap.FromImage(imgID200ppi);
                        using SKCanvas canvas = new SKCanvas(skBmpWithLandmark);
                        SKPaint paintGreen = new SKPaint() { Color = SKColors.Green, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
                        SKPaint paintYellow = new SKPaint() { Color = SKColors.Yellow, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
                        SKPaint paintRed = new SKPaint() { Color = SKColors.Red, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
                        //SKPaint paintOrange = new SKPaint() { Color = SKColors.Orange, StrokeWidth = 2 };

                        foreach (string key in scanMYDLResult.MatchTemplateResults.Keys)
                        {
                            MatchTemplateResultInfo matchTemplateResultInfo = scanMYDLResult.MatchTemplateResults[key];
                            if (matchTemplateResultInfo.MatchTemplateInfo.MatchResult != null
                                && matchTemplateResultInfo.MatchTemplateInfo.ResultLocX != null
                                && matchTemplateResultInfo.MatchTemplateInfo.ResultLocY != null
                                && matchTemplateResultInfo.MatchTemplateInfo.ResultWidth != null
                                && matchTemplateResultInfo.MatchTemplateInfo.ResultHeight != null)
                            {
                                double? dist = matchTemplateResultInfo.GetDistanceFromExpectedCenterInInch();
                                Console.WriteLine($"MatchTemplate key: {key} MatchResult: {matchTemplateResultInfo.MatchTemplateInfo.MatchResult}" +
                                    $"/{matchTemplateResultInfo.MatchTemplateInfo.MatchThreshold}" +
                                    $" x: {matchTemplateResultInfo.MatchTemplateInfo.ResultLocX} y: {matchTemplateResultInfo.MatchTemplateInfo.ResultLocY}" +
                                    $" w: {matchTemplateResultInfo.MatchTemplateInfo.ResultWidth} h: {matchTemplateResultInfo.MatchTemplateInfo.ResultHeight}" +
                                    $" dist: {dist}");
                                if (matchTemplateResultInfo.MatchTemplateInfo.MatchThreshold <= matchTemplateResultInfo.MatchTemplateInfo.MatchResult && dist < 0.2f)
                                {
                                    canvas.DrawRect(new SKRectI(
                                        matchTemplateResultInfo.MatchTemplateInfo.ResultLocX.Value, matchTemplateResultInfo.MatchTemplateInfo.ResultLocY.Value,
                                        matchTemplateResultInfo.MatchTemplateInfo.ResultLocX.Value + matchTemplateResultInfo.MatchTemplateInfo.ResultWidth.Value,
                                        matchTemplateResultInfo.MatchTemplateInfo.ResultLocY.Value + matchTemplateResultInfo.MatchTemplateInfo.ResultHeight.Value
                                        ), paintGreen);
                                }
                                else if (matchTemplateResultInfo.MatchTemplateInfo.MatchThreshold * 0.6f <= matchTemplateResultInfo.MatchTemplateInfo.MatchResult && dist < 0.2f)
                                {
                                    Console.WriteLine($"MatchTemplate key: {key} MatchResult: {matchTemplateResultInfo.MatchTemplateInfo.MatchResult}" +
                                        $"/{matchTemplateResultInfo.MatchTemplateInfo.MatchThreshold} --> Possibly Negative");
                                    canvas.DrawRect(new SKRectI(
                                        matchTemplateResultInfo.MatchTemplateInfo.ResultLocX.Value, matchTemplateResultInfo.MatchTemplateInfo.ResultLocY.Value,
                                        matchTemplateResultInfo.MatchTemplateInfo.ResultLocX.Value + matchTemplateResultInfo.MatchTemplateInfo.ResultWidth.Value,
                                        matchTemplateResultInfo.MatchTemplateInfo.ResultLocY.Value + matchTemplateResultInfo.MatchTemplateInfo.ResultHeight.Value
                                        ), paintYellow);
                                }
                                else
                                {
                                    Console.WriteLine($"MatchTemplate key: {key} MatchResult: {matchTemplateResultInfo.MatchTemplateInfo.MatchResult}" +
                                        $"/{matchTemplateResultInfo.MatchTemplateInfo.MatchThreshold} --> Not match");
                                }
                            }
                        } // foreach

                        SKData dataBmpLandmark = skBmpWithLandmark.Encode(SKEncodedImageFormat.Png, 100);
                        if (dataBmpLandmark != null && dataBmpLandmark.Size > 0)
                        {
                            string b64BmpLandmark = Convert.ToBase64String(dataBmpLandmark.ToArray());
                            if (!string.IsNullOrEmpty(b64BmpLandmark))
                                scanMYDLResult.landmarkImageBase64 = b64BmpLandmark;
                        }
                    }
                }
            }
            */

            //if (!string.IsNullOrEmpty(b64ImageFace))
            //    scanMYDLResult.faceImageBase64 = b64ImageFace;

            return scanMYDLResult;
        }

        string GenerateMatchTemplateResultImage(Dictionary<string, MatchTemplateResultInfo> matchTemplateResults, string cardImage200ppiPngB64)
        {
            string landmarkImageBase64 = "";
            if (matchTemplateResults != null)
            {
                if (!string.IsNullOrEmpty(cardImage200ppiPngB64))
                {
                    byte[] dataImage200ppiPng = Convert.FromBase64String(cardImage200ppiPngB64);
                    if (dataImage200ppiPng != null && dataImage200ppiPng.Length > 0)
                    {
                        SKData skData = SKData.CreateCopy(dataImage200ppiPng);
                        SKImage imgID200ppi = SKImage.FromEncodedData(skData);
                        SKData dataID200ppiPng = imgID200ppi.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                        /*
                        // save cropped ID image to file
                        string fileNameWithoutExt = null;
                        if (!string.IsNullOrEmpty(imageFileName))
                        {
                            fileNameWithoutExt = Path.GetFileNameWithoutExtension(imageFileName);
                            string matchTemplateIDImage = fileNameWithoutExt + "_IDImage200ppi";
                            matchTemplateIDImage = Path.ChangeExtension(matchTemplateIDImage, ".png");
                            using (FileStream fs = new FileStream(matchTemplateIDImage, FileMode.Create))
                            {
                                dataID200ppiPng.SaveTo(fs);
                            }
                        }
                        */
                        SKBitmap skBmpWithLandmark = SKBitmap.FromImage(imgID200ppi);
                        using SKCanvas canvas = new SKCanvas(skBmpWithLandmark);
                        SKPaint paintGreen = new SKPaint() { Color = SKColors.Green, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
                        SKPaint paintYellow = new SKPaint() { Color = SKColors.Yellow, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
                        SKPaint paintRed = new SKPaint() { Color = SKColors.Red, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
                        //SKPaint paintOrange = new SKPaint() { Color = SKColors.Orange, StrokeWidth = 2 };

                        foreach (string key in matchTemplateResults.Keys)
                        {
                            MatchTemplateResultInfo matchTemplateResultInfo = matchTemplateResults[key];
                            if (matchTemplateResultInfo.MatchTemplateInfo.MatchResult != null
                                && matchTemplateResultInfo.MatchTemplateInfo.ResultLocX != null
                                && matchTemplateResultInfo.MatchTemplateInfo.ResultLocY != null
                                && matchTemplateResultInfo.MatchTemplateInfo.ResultWidth != null
                                && matchTemplateResultInfo.MatchTemplateInfo.ResultHeight != null)
                            {
                                double? dist = matchTemplateResultInfo.GetDistanceFromExpectedCenterInInch();
                                Console.Write($"MatchTemplate key: {key} MatchResult: {matchTemplateResultInfo.MatchTemplateInfo.MatchResult}" +
                                    $"/{matchTemplateResultInfo.MatchTemplateInfo.MatchThreshold}" +
                                    $" x: {matchTemplateResultInfo.MatchTemplateInfo.ResultLocX} y: {matchTemplateResultInfo.MatchTemplateInfo.ResultLocY}" +
                                    $" w: {matchTemplateResultInfo.MatchTemplateInfo.ResultWidth} h: {matchTemplateResultInfo.MatchTemplateInfo.ResultHeight}" +
                                    $" dist: {dist}");
                                if (matchTemplateResultInfo.MatchTemplateInfo.MatchThreshold <= matchTemplateResultInfo.MatchTemplateInfo.MatchResult && dist < 0.2f)
                                {
                                    Console.WriteLine($"--> Match");
                                    canvas.DrawRect(new SKRectI(
                                        matchTemplateResultInfo.MatchTemplateInfo.ResultLocX.Value, matchTemplateResultInfo.MatchTemplateInfo.ResultLocY.Value,
                                        matchTemplateResultInfo.MatchTemplateInfo.ResultLocX.Value + matchTemplateResultInfo.MatchTemplateInfo.ResultWidth.Value,
                                        matchTemplateResultInfo.MatchTemplateInfo.ResultLocY.Value + matchTemplateResultInfo.MatchTemplateInfo.ResultHeight.Value
                                        ), paintGreen);
                                }
                                else if (matchTemplateResultInfo.MatchTemplateInfo.MatchThreshold * 0.6f <= matchTemplateResultInfo.MatchTemplateInfo.MatchResult && dist < 0.2f)
                                {
                                    Console.WriteLine($"--> Possibly Negative");
                                    canvas.DrawRect(new SKRectI(
                                        matchTemplateResultInfo.MatchTemplateInfo.ResultLocX.Value, matchTemplateResultInfo.MatchTemplateInfo.ResultLocY.Value,
                                        matchTemplateResultInfo.MatchTemplateInfo.ResultLocX.Value + matchTemplateResultInfo.MatchTemplateInfo.ResultWidth.Value,
                                        matchTemplateResultInfo.MatchTemplateInfo.ResultLocY.Value + matchTemplateResultInfo.MatchTemplateInfo.ResultHeight.Value
                                        ), paintYellow);
                                }
                                else
                                {
                                    Console.WriteLine($"--> Not match");
                                }
                            }
                        } // foreach

                        SKData dataBmpLandmark = skBmpWithLandmark.Encode(SKEncodedImageFormat.Png, 100);
                        if (dataBmpLandmark != null && dataBmpLandmark.Size > 0)
                        {
                            string b64BmpLandmark = Convert.ToBase64String(dataBmpLandmark.ToArray());
                            if (!string.IsNullOrEmpty(b64BmpLandmark))
                            {
                                landmarkImageBase64 = b64BmpLandmark;
                            }
                        }
                    }
                }
            }
            return landmarkImageBase64;
        }

        /*
        public static ScanPHUMIDResult ScanPHUMID(string baseAddrUrl, string imageFileName, string baseAddrUrl_OD, MatchTemplateIDCard? matchTemplatePHUMID)
        {
            Console.WriteLine($"ScanPHUMID imageFileName: {imageFileName}");
            string b64Image = EncodeImageFileToBase64(imageFileName);
            return ScanPHUMIDB64(baseAddrUrl, b64Image, baseAddrUrl_OD, matchTemplatePHUMID);
        }
        */
        //public static ScanPHUMIDResult ScanPHUMIDB64(string baseAddrUrl, string imageSrcB64, string baseAddrUrl_OD, MatchTemplateIDCard? matchTemplatePHUMID)
        public ScanPHUMIDResult ScanPHUMID(Line[] linesMergedAll, SKImage imageSrc, SKRectI? rcFace, ImgProcLib.MatchTemplateIDCard? matchTemplatePHUMID, LabelInfo[] labelsFound, Line[] linesMergedNotLabel)
        {
            List<LabeledObject> labeledObjects = new List<LabeledObject>();

            DateTime dtStart2 = DateTime.Now;
            Console.WriteLine($"ExtractFieldsFromReadResultOfPHUMID start...");
            //ScanPHUMIDResult scanPHUMIDResult = ExtractFieldsFromReadResultOfPHUMID(linesMerged, labeledObjects, image, matchTemplatePHUMID);
            ScanPHUMIDResult scanPHUMIDResult = ExtractFieldsFromReadResultOfPHUMID(labelsFound, linesMergedNotLabel, rcFace, imageSrc, matchTemplatePHUMID);
            DateTime dtEnd2 = DateTime.Now;
            Console.WriteLine($"ExtractFieldsFromReadResultOfPHUMID ({(dtEnd2 - dtStart2).TotalSeconds} sec)\n");

            //scanPHUMIDResult.landmarkImageBase64 = GenerateMatchTemplateResultImage(scanPHUMIDResult.MatchTemplateResults, scanPHUMIDResult.CardImage200ppiPngB64);
            //scanPHUMIDResult.faceImageBase64 = b64ImageFace;
            return scanPHUMIDResult;
        }
        public ScanPHUMIDResult ScanPHUMID1(Line[] linesMergedAll, SKImage imageSrc, SKRectI? rcFace, ImgProcLib.MatchTemplateIDCard? matchTemplatePHUMID, LabelInfo[] labelsFound, Line[] linesMergedNotLabel)
        {
            List<LabeledObject> labeledObjects = new List<LabeledObject>();

            DateTime dtStart2 = DateTime.Now;
            Console.WriteLine($"ExtractFieldsFromReadResultOfPHUMID start...");
            //ScanPHUMIDResult scanPHUMIDResult = ExtractFieldsFromReadResultOfPHUMID(linesMerged, labeledObjects, image, matchTemplatePHUMID);
            ScanPHUMIDResult scanPHUMIDResult = ExtractFieldsFromReadResultOfPHUMID1(labelsFound, linesMergedNotLabel, rcFace, imageSrc, matchTemplatePHUMID);
            DateTime dtEnd2 = DateTime.Now;
            Console.WriteLine($"ExtractFieldsFromReadResultOfPHUMID ({(dtEnd2 - dtStart2).TotalSeconds} sec)\n");

            //scanPHUMIDResult.landmarkImageBase64 = GenerateMatchTemplateResultImage(scanPHUMIDResult.MatchTemplateResults, scanPHUMIDResult.CardImage200ppiPngB64);
            //scanPHUMIDResult.faceImageBase64 = b64ImageFace;
            return scanPHUMIDResult;
        }
        public ScanPHUMIDResult ScanPHUMID2(Line[] linesMergedAll, SKImage imageSrc, SKRectI? rcFace, ImgProcLib.MatchTemplateIDCard? matchTemplatePHUMID, LabelInfo[] labelsFound, Line[] linesMergedNotLabel)
        {
            List<LabeledObject> labeledObjects = new List<LabeledObject>();

            DateTime dtStart2 = DateTime.Now;
            Console.WriteLine($"ExtractFieldsFromReadResultOfPHUMID start...");
            //ScanPHUMIDResult scanPHUMIDResult = ExtractFieldsFromReadResultOfPHUMID(linesMerged, labeledObjects, image, matchTemplatePHUMID);
            ScanPHUMIDResult scanPHUMIDResult = ExtractFieldsFromReadResultOfPHUMID2(labelsFound, linesMergedNotLabel, rcFace, imageSrc, matchTemplatePHUMID);
            DateTime dtEnd2 = DateTime.Now;
            Console.WriteLine($"ExtractFieldsFromReadResultOfPHUMID ({(dtEnd2 - dtStart2).TotalSeconds} sec)\n");

            //scanPHUMIDResult.landmarkImageBase64 = GenerateMatchTemplateResultImage(scanPHUMIDResult.MatchTemplateResults, scanPHUMIDResult.CardImage200ppiPngB64);
            //scanPHUMIDResult.faceImageBase64 = b64ImageFace;
            return scanPHUMIDResult;
        }

        /*
        public static ScanPHDLResult ScanPHDL(string baseAddrUrl, string imageFileName, ImgProcLib.MatchTemplateIDCard? matchTemplatePHDL)
        {
            Console.WriteLine($"ScanPHDL imageFileName: {imageFileName}");
            string b64Image = EncodeImageFileToBase64(imageFileName);

            return ScanPHDLB64(baseAddrUrl, b64Image, matchTemplatePHDL);
        }
         */

        //public static ScanPHDLResult ScanPHDLB64(string baseAddrUrl, string imageSrcB64, ImgProcLib.MatchTemplateIDCard? matchTemplatePHDL)
        public ScanPHDLResult ScanPHDL(Line[] linesMergedAll, SKImage imageSrc,
            ImgProcLib.MatchTemplateIDCard? matchTemplateMYDL, LabelInfo[] labelsFound, Line[] linesMergedNotLabel)
        {
            DateTime dtStart2 = DateTime.Now;
            Console.WriteLine($"ExtractFieldsFromReadResultOfPHDL start...");
            ScanPHDLResult scanPHDLResult = ExtractFieldsFromReadResultOfPHDL(labelsFound, linesMergedNotLabel, imageSrc, matchTemplateMYDL);
            DateTime dtEnd2 = DateTime.Now;
            Console.WriteLine($"ExtractFieldsFromReadResultOfPHDL ({(dtEnd2 - dtStart2).TotalSeconds} sec)\n");
            //scanPHDLResult.landmarkImageBase64 = GenerateMatchTemplateResultImage(scanPHDLResult.MatchTemplateResults, scanPHDLResult.CardImage200ppiPngB64);
            return scanPHDLResult;
        }
        /*
        public ScanPHNIResult ScanPHNI(string baseAddrUrl, string imageFileName, string backImageFileName, ImgProcLib.MatchTemplateIDCard? matchTemplatePHNI)
        {
            Console.WriteLine($"ScanPHNI imageFileName: {imageFileName}");
            Console.WriteLine($"ScanPHNI backImageFileName: {backImageFileName}");
            string b64Image = EncodeImageFileToBase64(imageFileName);
            string b64ImageBack = EncodeImageFileToBase64(backImageFileName);

            return ScanPHNIB64(baseAddrUrl, b64Image, b64ImageBack, matchTemplatePHNI);
        }
        */

        //public ScanPHNIResult ScanPHNIB64(string baseAddrUrl, string imageSrcB64, string imageBackSrcB64, ImgProcLib.MatchTemplateIDCard? matchTemplatePHNI)
        public ScanPHNIResult ScanPHNI(Line[] linesMergedAll, SKImage imageSrc, 
            ImgProcLib.MatchTemplateIDCard? matchTemplatePHNI, LabelInfo[] labelsFound, Line[] linesMergedNotLabel,
            string baseAddrUrl, string imageBackSrcB64)
        {
            DateTime dtStart1 = DateTime.Now;
            Console.WriteLine($"ExtractFieldsFromReadResultOfPHNI start...");
            ScanPHNIResult scanPHNIResult = ExtractFieldsFromReadResultOfPHNI(labelsFound, linesMergedNotLabel, imageSrc, matchTemplatePHNI);
            DateTime dtEnd1 = DateTime.Now;
            Console.WriteLine($"ExtractFieldsFromReadResultOfPHDL ({(dtEnd1 - dtStart1).TotalSeconds} sec)\n");
            //scanPHNIResult.landmarkImageBase64 = GenerateMatchTemplateResultImage(scanPHNIResult.MatchTemplateResults, scanPHNIResult.CardImage200ppiPngB64);
            //return scanPHDLResult;

            if (string.IsNullOrEmpty(imageBackSrcB64))
            {
                return scanPHNIResult;
            }

            byte[] dataImageBack = Convert.FromBase64String(imageBackSrcB64);
            using (SKImage bmpImageBak = SKImage.FromEncodedData(dataImageBack))
            {
                List<Line> linesBack = null;
                List<LabelInfo> labelsFoundBack = new List<LabelInfo>();
                List<Line> linesNotLabelBack = new List<Line>();
                Console.WriteLine($"ScanPHNI back...");
                Console.WriteLine($"Scan QR code...");
                ZXing.Result[] resReadQRCode = ReadQRCode(bmpImageBak);
                //if(resReadQRCode == null || resReadQRCode.Length == 0)
                {
                    //Console.WriteLine($"Failed to read QR code. Try OCR");
                    Console.WriteLine($"Try OCR Back Image...");
                    DateTime dtStartBak = DateTime.Now;
                    Console.WriteLine($"PostOCRWithRegionRequest start... URL:{baseAddrUrl}");
                    OCRWithRegionResponse ocrWithRegionResponseBak = PostOCRWithRegionRequest(baseAddrUrl, imageBackSrcB64);
                    linesBack = ocrWithRegionResponseBak.Lines;
                    DateTime dtEndBak = DateTime.Now;
                    Console.WriteLine($"({(dtEndBak - dtStartBak).TotalSeconds} sec)\n");

                    // remove </s> from the start of 1st line
                    if (linesBack.Count > 0 && (linesBack[0].Text.StartsWith("</s>")))
                        linesBack[0].Text = linesBack[0].Text.Replace("</s>", "");
                    if (linesBack.Count > 0 && (linesBack[0].Text.StartsWith("</S>")))
                        linesBack[0].Text = linesBack[0].Text.Replace("</S>", "");

                    foreach (Line line in linesBack)
                    {
                        Console.WriteLine(line.ExtToString());
                    }

                    SKImage image;
                    if (ocrWithRegionResponseBak.ImageWidth != 0 && ocrWithRegionResponseBak.ImageHeight != 0
                        && (bmpImageBak.Width != ocrWithRegionResponseBak.ImageWidth || bmpImageBak.Height != ocrWithRegionResponseBak.ImageHeight))
                    {
                        // Load the image
                        SKBitmap srcBitmap = SKBitmap.FromImage(bmpImageBak);

                        // Define the new size
                        SKImageInfo newSize = new SKImageInfo(ocrWithRegionResponseBak.ImageWidth, ocrWithRegionResponseBak.ImageHeight);

                        // Resize the image
                        image = SKImage.FromBitmap(srcBitmap.Resize(newSize, SKFilterQuality.High));
                    }
                    else
                    {
                        image = bmpImageBak;
                    }

                    List<Line> linesTess;
                    linesBack = ScanEachLineWithTesseract(linesBack, image, out linesTess);
                    Console.WriteLine("==== Lines read by Tesseract ====");
                    foreach (Line lineTess in linesTess)
                    {
                        Console.WriteLine(lineTess.ExtToString());
                    }
                    Console.WriteLine("======================");

                    Console.WriteLine("==== Merged lines ====");
                    IList<Line> linesBackMerged = MergeLinesInSameYPosIntoOneLine(linesBack);
                    foreach (Line line in linesBackMerged)
                    {
                        Console.WriteLine(line.ExtToString());
                    }
                    Console.WriteLine("======================");

                    FindLabels(linesBackMerged, "PHNIBK", out labelsFoundBack, out linesNotLabelBack);
                }

                DateTime dtStart2 = DateTime.Now;
                Console.WriteLine($"ExtractFieldsFromReadResultOfPHNIBK start...");
                ScanPHNIBKResult scanPHNIBKResult = ExtractFieldsFromReadResultOfPHNIBK(labelsFoundBack.ToArray(), linesNotLabelBack.ToArray(), bmpImageBak, resReadQRCode);
                DateTime dtEnd2 = DateTime.Now;
                Console.WriteLine($"ExtractFieldsFromReadResultOfPHNI ({(dtEnd2 - dtStart2).TotalSeconds} sec)\n");
                if (scanPHNIBKResult.IsQRCodeDataValid)
                {
                    /*
                    scanPHNIResult.documentIssueDate = scanPHNIBKResult.QRCode_DateIssued;
                    scanPHNIResult.lastNameOrFullName = scanPHNIBKResult.QRCode_subject_lName;
                    scanPHNIResult.firstName = scanPHNIBKResult.QRCode_subject_fName;
                    scanPHNIResult.middleName = scanPHNIBKResult.QRCode_subject_mName;
                    scanPHNIResult.gender = EncodeGender(scanPHNIBKResult.QRCode_subject_sex);
                    scanPHNIResult.dateOfBirth = scanPHNIBKResult.QRCode_subject_DOB;
                    scanPHNIResult.placeOfBirth = scanPHNIBKResult.QRCode_subject_POB;
                    scanPHNIResult.documentNumber = scanPHNIBKResult.QRCode_subject_PCN;
                    */
                    scanPHNIResult.documentIssueDate = scanPHNIBKResult.documentIssueDate;
                    scanPHNIResult.lastNameOrFullName = scanPHNIBKResult.lastNameOrFullName;
                    scanPHNIResult.firstName = scanPHNIBKResult.firstName;
                    scanPHNIResult.middleName = scanPHNIBKResult.middleName;
                    scanPHNIResult.gender = scanPHNIBKResult.gender;
                    scanPHNIResult.dateOfBirth = scanPHNIBKResult.dateOfBirth;
                    scanPHNIResult.placeOfBirth = scanPHNIBKResult.placeOfBirth;
                    scanPHNIResult.maritalStatus = scanPHNIBKResult.maritalStatus;
                    scanPHNIResult.documentNumber = scanPHNIBKResult.documentNumber;
                    scanPHNIResult.IsQRCodeDataValid = true;
                }

                //scanPHNIResult.faceImageBase64 = b64ImageFace;
                return scanPHNIResult;
            }
        }

        public ScanPHNIResult ScanPHNI(Line[] linesMergedAll, SKImage imageSrc,
            ImgProcLib.MatchTemplateIDCard? matchTemplatePHNI, LabelInfo[] labelsFound, Line[] linesMergedNotLabel,
             LabelInfo[] labelsFoundBack, Line[] linesMergedNotLabelBack, string imageBackSrcB64)
        {
            DateTime dtStart1 = DateTime.Now;
            Console.WriteLine($"ExtractFieldsFromReadResultOfPHNI start...");
            ScanPHNIResult scanPHNIResult = ExtractFieldsFromReadResultOfPHNI(labelsFound, linesMergedNotLabel, imageSrc, matchTemplatePHNI);
            DateTime dtEnd1 = DateTime.Now;
            Console.WriteLine($"ExtractFieldsFromReadResultOfPHDL ({(dtEnd1 - dtStart1).TotalSeconds} sec)\n");
            //scanPHNIResult.landmarkImageBase64 = GenerateMatchTemplateResultImage(scanPHNIResult.MatchTemplateResults, scanPHNIResult.CardImage200ppiPngB64);
            //return scanPHDLResult;

            if (string.IsNullOrEmpty(imageBackSrcB64))
            {
                return scanPHNIResult;
            }

            byte[] dataImageBack = Convert.FromBase64String(imageBackSrcB64);
            using (SKImage bmpImageBak = SKImage.FromEncodedData(dataImageBack))
            {
                Console.WriteLine($"ScanPHNI back...");
                Console.WriteLine($"Scan QR code...");
                ZXing.Result[] resReadQRCode = ReadQRCode(bmpImageBak);

                DateTime dtStart2 = DateTime.Now;
                Console.WriteLine($"ExtractFieldsFromReadResultOfPHNIBK start...");
                ScanPHNIBKResult scanPHNIBKResult = ExtractFieldsFromReadResultOfPHNIBK(labelsFoundBack.ToArray(), linesMergedNotLabelBack.ToArray(), bmpImageBak, resReadQRCode);
                DateTime dtEnd2 = DateTime.Now;
                Console.WriteLine($"ExtractFieldsFromReadResultOfPHNI ({(dtEnd2 - dtStart2).TotalSeconds} sec)\n");
                if (scanPHNIBKResult.IsQRCodeDataValid)
                {
                    /*
                    scanPHNIResult.documentIssueDate = scanPHNIBKResult.QRCode_DateIssued;
                    scanPHNIResult.lastNameOrFullName = scanPHNIBKResult.QRCode_subject_lName;
                    scanPHNIResult.firstName = scanPHNIBKResult.QRCode_subject_fName;
                    scanPHNIResult.middleName = scanPHNIBKResult.QRCode_subject_mName;
                    scanPHNIResult.gender = EncodeGender(scanPHNIBKResult.QRCode_subject_sex);
                    scanPHNIResult.dateOfBirth = scanPHNIBKResult.QRCode_subject_DOB;
                    scanPHNIResult.placeOfBirth = scanPHNIBKResult.QRCode_subject_POB;
                    scanPHNIResult.documentNumber = scanPHNIBKResult.QRCode_subject_PCN;
                    */
                    scanPHNIResult.documentIssueDate = scanPHNIBKResult.documentIssueDate;
                    scanPHNIResult.lastNameOrFullName = scanPHNIBKResult.lastNameOrFullName;
                    scanPHNIResult.firstName = scanPHNIBKResult.firstName;
                    scanPHNIResult.middleName = scanPHNIBKResult.middleName;
                    scanPHNIResult.gender = scanPHNIBKResult.gender;
                    scanPHNIResult.dateOfBirth = scanPHNIBKResult.dateOfBirth;
                    scanPHNIResult.placeOfBirth = scanPHNIBKResult.placeOfBirth;
                    scanPHNIResult.maritalStatus = scanPHNIBKResult.maritalStatus;
                    scanPHNIResult.documentNumber = scanPHNIBKResult.documentNumber;
                    scanPHNIResult.IsQRCodeDataValid = true;
                }

                //scanPHNIResult.faceImageBase64 = b64ImageFace;
                return scanPHNIResult;
            }
        }

        static string EncodeGender(string gender)
        {
            if (string.IsNullOrEmpty(gender))
                return "U";

            switch (gender.ToUpper())
            {
                case "MALE":
                    return "M";
                case "FEMALE":
                    return "F";
                default:
                    return gender;
            }
        }

        public static OCRWithRegionResponse PostOCRWithRegionRequest(string baseAddrUrl, string b64Image)
        {
            System.Diagnostics.Debug.WriteLine($"PostOCRWithRegionRequest baseAddrUrl:{baseAddrUrl}");
            string ret = "";
            OCRWithRegionResponse ocrWithRegionResponse = new OCRWithRegionResponse();
            ocrWithRegionResponse.Lines = new List<Line>();
            // Create a new instance of the HttpClient class
            using (var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(200) })
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
                Console.WriteLine($"PostOCRWithRegionRequest PostAsync start...");
                var response = client.PostAsync($"{baseAddrUrl}ocrWithRegion", content).GetAwaiter().GetResult();
                DateTime dtEnd = DateTime.Now;
                ocrWithRegionResponse.timeElapsed = (int)(dtEnd - dtStart).TotalMilliseconds;
                Console.WriteLine($"PostOCRWithRegionRequest PostAsync ({ocrWithRegionResponse.timeElapsed/1000f} sec)\n");

                // Check the status code of the response
                if (response.IsSuccessStatusCode)
                {
                    // Get the response content
                    var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ==== responseBody ====\n {responseBody}\n==========");

                    // Deserialize the response content to a string
                    var jsonRes = JObject.Parse(responseBody);
                    if (jsonRes.ContainsKey("<OCR_WITH_REGION>"))
                    {
                        ocrWithRegionResponse.ImageWidth = 0;
                        ocrWithRegionResponse.ImageHeight = 0;
                        if (jsonRes.ContainsKey("image_width"))
                        {
                            string image_width = jsonRes["image_width"].ToString();
                            ocrWithRegionResponse.ImageWidth = int.Parse(image_width);
                        }
                        if (jsonRes.ContainsKey("image_height"))
                        {
                            string image_height = jsonRes["image_height"].ToString();
                            ocrWithRegionResponse.ImageHeight = int.Parse(image_height);
                        }

                        ret = jsonRes["<OCR_WITH_REGION>"].ToString();
                        //Console.WriteLine("<OCR_WITH_REGION>:" + ret);
                        JObject jsonRet = JObject.Parse(ret);
                        JArray labels = (JArray)jsonRet["labels"];
                        JArray boxes = (JArray)jsonRet["quad_boxes"];
                        for (int i = 0; i < labels.Count; i++)
                        {
                            //System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] labels[{i}]: {labels[i]}");
                            //System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] boxes[{i}]: {boxes[i]}");
                            Line line = new Line();
                            line.Text = Utils.RemoveAccents(labels[i].ToString()).Trim().ToUpper();

                            if (string.IsNullOrEmpty(line.Text))
                            {
                                continue;
                            }
                            JArray jsonBoundingBox = (JArray)boxes[i];
                            List<double?> boundingBox = new List<double?>();
                            for (int j = 0; j < jsonBoundingBox.Count; j++)
                            {
                                boundingBox.Add((double)jsonBoundingBox[j]);
                            }
                            line.BoundingBox = boundingBox;
                            if(boundingBox.Count == 8)
                            {
                                List<double?> baseline = new List<double?>();
                                baseline.Add(boundingBox[6]); // X1 Left Bottom X
                                baseline.Add(boundingBox[7]); // Y1 Left Bottom Y
                                baseline.Add(boundingBox[4]); // X2 Right Bottom X
                                baseline.Add(boundingBox[5]); // Y2 Right Bottom Y
                            }
                            ocrWithRegionResponse.Lines.Add(line);
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
            return ocrWithRegionResponse;
        }
        public static ObjectDetectionResponse PostObjectDetectionRequest(string baseAddrUrl, string b64Image)
        {
            System.Diagnostics.Debug.WriteLine($"PostObjectDetectionRequest baseAddrUrl:{baseAddrUrl}");
            //'human face', 'human head', 'man'

            string ret = "";
            ObjectDetectionResponse objectDetectionResponse = new ObjectDetectionResponse();
            objectDetectionResponse.LabeledObjects = new List<LabeledObject>();
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
                DateTime dtStart = DateTime.Now;
                Console.WriteLine($"PostObjectDetectionRequest PostAsync start... URL:{baseAddrUrl}");
                var response = client.PostAsync($"{baseAddrUrl}objectDetection", content).GetAwaiter().GetResult();
                DateTime dtEnd = DateTime.Now;

                objectDetectionResponse.timeElapsed = (int)(dtEnd - dtStart).TotalMilliseconds;
                Console.WriteLine($"PostObjectDetectionRequest PostAsync ({objectDetectionResponse.timeElapsed/1000f} sec)\n");

                // Check the status code of the response
                if (response.IsSuccessStatusCode)
                {
                    // Get the response content
                    var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ==== responseBody ====\n {responseBody}\n==========");

                    // Deserialize the response content to a string
                    var jsonRes = JObject.Parse(responseBody);
                    if (jsonRes.ContainsKey("<OD>"))
                    {
                        objectDetectionResponse.ImageWidth = 0;
                        objectDetectionResponse.ImageHeight = 0;
                        if (jsonRes.ContainsKey("image_width"))
                        {
                            string image_width = jsonRes["image_width"].ToString();
                            objectDetectionResponse.ImageWidth = int.Parse(image_width);
                        }
                        if (jsonRes.ContainsKey("image_height"))
                        {
                            string image_height = jsonRes["image_height"].ToString();
                            objectDetectionResponse.ImageHeight = int.Parse(image_height);
                        }

                        ret = jsonRes["<OD>"].ToString();
                        Console.WriteLine("<OD>:" + ret);
                        JObject jsonRet = JObject.Parse(ret);
                        JArray labels = (JArray)jsonRet["labels"];
                        JArray boxes = (JArray)jsonRet["bboxes"];
                        for (int i = 0; i < labels.Count; i++)
                        {
                            LabeledObject labeledObject = new LabeledObject();
                            labeledObject.Label = Utils.RemoveAccents(labels[i].ToString()).Trim().ToUpper();

                            if (string.IsNullOrEmpty(labeledObject.Label))
                            {
                                continue;
                            }
                            JArray jsonBoundingBox = (JArray)boxes[i];
                            List<double?> boundingBox = new List<double?>();
                            for (int j = 0; j < jsonBoundingBox.Count; j++)
                            {
                                boundingBox.Add((double)jsonBoundingBox[j]);
                            }
                            labeledObject.BoundingBox = boundingBox;
                            objectDetectionResponse.LabeledObjects.Add(labeledObject);
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
            return objectDetectionResponse;
        }

        public ScanMYDLResult ExtractFieldsFromReadResultOfMYDL(LabelInfo[] labelsFound, Line[] linesMergedNotLabel, SKImage imageSrc, ImgProcLib.MatchTemplateIDCard? matchTemplateMYDL)
        {
            const float FILTER_WEAK_TEXT_SMALLER_THAN_IDNUM = 0.7f;
            const double ACCEPTABLE_DIFF_IN_LINE = 0.1f;
            //List<Line> lsLineMerged = new List<Line>();
            //lsLineMerged.AddRange(mergedLinesAll);
            List<Line> lsLineMergedNotLabel = new List<Line>();
            lsLineMergedNotLabel.AddRange(linesMergedNotLabel);

            const double centerYInInchOfField_Name = 0.5f;
            const double centerYInInchOfField_Nationality = 0.81f;
            const double centerYInInchOfField_IDNum = 0.81f;
            const double centerYInInchOfField_Class = 1.05f;
            const double centerYInInchOfField_Validity = 1.30f;
            const double centerYInInchOfField_Address1 = 1.5f;
            const double centerYInInchOfField_Address2 = 1.615f;
            const double centerYInInchOfField_Address3 = 1.73f;
            const double centerYInInchOfField_Address4 = 1.85f;
            const double centerYInInchOfField_Address5 = 1.96f;

            Dictionary<string, MatchTemplateInfo> dicMatchTemplateInfo = new Dictionary<string, MatchTemplateInfo>();
            MatchTemplateInfo matchTmplMYDL_670_COA_Gray = new MatchTemplateInfo("MYDL_670_COA_Gray", "COA", 0.8f, 0.3f, 0.25f);
            dicMatchTemplateInfo.Add(matchTmplMYDL_670_COA_Gray.Name, matchTmplMYDL_670_COA_Gray);
            MatchTemplateInfo matchTmplMYDL_670_Flag_Gray = new MatchTemplateInfo("MYDL_670_Flag_Gray", "Flag", 0.8f, 1.75f, 0.25f);
            dicMatchTemplateInfo.Add(matchTmplMYDL_670_Flag_Gray.Name, matchTmplMYDL_670_Flag_Gray);
            MatchTemplateInfo matchTmplMYDL_670_Flower_Gray = new MatchTemplateInfo("MYDL_670_Flower_Gray", "Flower", 0.8f, 3.0f, 1.15f);
            dicMatchTemplateInfo.Add(matchTmplMYDL_670_Flower_Gray.Name, matchTmplMYDL_670_Flower_Gray);

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
            string ADDRESS4 = "";
            string ADDRESS5 = "";
            string POSTCODE = "";
            string CITY = "";
            string STATE = "";

            Line lineName = null;
            Line lineNationalityIDNum = null;
            Line lineNationality = null;
            Line lineIDNum = null;
            Line lineClass = null;
            Line lineValidity = null;
            Line lineAddress1 = null;
            Line lineAddress2 = null;
            Line lineAddress3 = null;
            Line lineAddress4 = null;
            Line lineAddress5 = null;

            try
            {
                double? bottomOfHeaderArea = null;

                List<Line> linesField = new List<Line>();   // lines valid and not label

                // calc pixel per inch
                double? ppi = LabelInfo.CalcPixelPerInch(labelsFound);
                if (ppi == null)
                {
                    Console.WriteLine("CalcPixelPerInch failed");
                }
                else
                {
                    //
                    // calc top and left edge
                    //
                    double? topEdgeYOfIDImageInPixel;
                    double? leftEdgeXOfIDImageInPixel;
                    CalcLeftTopEdgeInPixel(labelsFound, ppi, out leftEdgeXOfIDImageInPixel, out topEdgeYOfIDImageInPixel);
                    if (topEdgeYOfIDImageInPixel != null && leftEdgeXOfIDImageInPixel != null)
                    {
                        if (topEdgeYOfIDImageInPixel != null && leftEdgeXOfIDImageInPixel != null)
                        {
                            MatchTemplateResult matchTemplateResult = null;
                            SKData dataID200ppiPng = null;
                            DateTime dtStart = DateTime.Now;
                            bool bRetMatchTemplate = DoMatchTemplate(matchTemplateMYDL, imageSrc, topEdgeYOfIDImageInPixel.Value, leftEdgeXOfIDImageInPixel.Value, ppi.Value,
                                out matchTemplateResult, out dataID200ppiPng);
                            DateTime dtEnd = DateTime.Now;
                            result.timeElapsedLandmarkDetection = (int)(dtEnd - dtStart).TotalMilliseconds;

                            result.CardImage200ppiPngB64 = Convert.ToBase64String(dataID200ppiPng.ToArray());
                            if (bRetMatchTemplate)
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTemplateMyKadResult: {matchTemplateResult.MatchResult}");
                                SKImage imgID200ppi = SKImage.FromEncodedData(dataID200ppiPng);
                                GenerateMatchTemplateResults(matchTemplateResult, dicMatchTemplateInfo, result.MatchTemplateResults);
                                result.landmarkImageBase64 = GenerateMatchTemplateResultImage(result.MatchTemplateResults, result.CardImage200ppiPngB64);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTemplate failed");
                            }
                        }
                    }

                    //
                    // remove lines shorter than expected
                    //
                    const double labelHeightFilterInInch = 0.07f;   // text shorter than this height shuld be ignored
                    double labelHeightFilterInPixel = (double)labelHeightFilterInInch * ppi.Value;
                    //if (linesField.Count > 0)
                    //{
                    //    // remove lines predicted as label because of height
                    //    int removedFromLinesField = linesField.RemoveAll(l => l.ExtGetHeight() < labelHeightFilterInPixel);
                    //    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] removed {removedFromLinesField} lines predicted as label removed from linesField [height < labelHeightInPixel ({labelHeightFilterInPixel})].");
                    //}
                    if (lsLineMergedNotLabel.Count > 0)
                    {
                        // remove lines predicted as label because of height
                        int removedFromLinesMerged = lsLineMergedNotLabel.RemoveAll(l => l.ExtGetHeight() < labelHeightFilterInPixel);
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] removed {removedFromLinesMerged} lines predicted as label removed from lsLineMerged [height < labelHeightInPixel ({labelHeightFilterInPixel})].");
                    }

                    // predit y in inch and expected field for each file line  
                    //foreach (Line line in linesField)
                    foreach (Line line in lsLineMergedNotLabel)
                    {
                        double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                        System.Diagnostics.Debug.WriteLine($"line: {line.Text} EstimateCenterYInInch: {y}");
                    }

                    try
                    {
                        // filter lines near to line of name field
                        FindFromMergedLine(ref lsLineMergedNotLabel, centerYInInchOfField_Name,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the label
                                if (m_labelMYDL_LESEN_MEMANDU.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_LESEN_MEMANDU.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelMYDL_MALAYSIA.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_MALAYSIA.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelMYDL_DRIVING_LICENCE.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_DRIVING_LICENCE.LineMacthed.ExtGetVerticalCenter()))
                                    return false;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> NAME");
                                NAME = l.Text;
                                lineName = l;
                                return true;
                            }));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        // filter lines near to line of name field
                        FindFromMergedLine(ref lsLineMergedNotLabel, centerYInInchOfField_Nationality,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the label
                                if (m_labelMYDL_LESEN_MEMANDU.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_LESEN_MEMANDU.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelMYDL_MALAYSIA.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_MALAYSIA.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelMYDL_DRIVING_LICENCE.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_DRIVING_LICENCE.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelMYDL_Warganegara_Nationality.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_Warganegara_Nationality.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelMYDL_No_Pengenalan_Identity_No.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_No_Pengenalan_Identity_No.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelMYDL_Warganegara_Nationality_No_Pengenalan_Identity_No.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_Warganegara_Nationality_No_Pengenalan_Identity_No.LineMacthed.ExtGetVerticalCenter()))
                                    return false;

                                string strLineNationalityIDNum = l.Text;
                                string[] items = strLineNationalityIDNum.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                if (items != null)
                                {
                                    double? leftX = l.EstimateLeftXInInch(ppi, leftEdgeXOfIDImageInPixel);
                                    foreach (string item in items)
                                    {
                                        if (leftX != null && leftX > 1.7)
                                        {
                                            if (string.IsNullOrEmpty(IDNUM))
                                            {
                                                IDNUM = item;
                                                if (lineIDNum == null)
                                                    lineIDNum = l;
                                            }
                                            else
                                            {
                                                if(lineIDNum != null)
                                                {
                                                    if(lineIDNum.ExtGetHorizontalCenter() < l.ExtGetHorizontalCenter())
                                                    {
                                                        // maybe IDNUM is separated and left half was alredy set to IDNUM. so add right part.
                                                        IDNUM = item + IDNUM;
                                                    }
                                                    else
                                                    {
                                                        IDNUM += item;
                                                    }
                                                }
                                                else
                                                {
                                                    // maybe IDNUM is separated and left half was alredy set to IDNUM. so add right part.
                                                    IDNUM = item + IDNUM;
                                                }
                                            }
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {IDNUM} --> IDNUM");
                                        }
                                        else
                                        {
                                            if (string.IsNullOrEmpty(NATIONALITY))
                                            {
                                                NATIONALITY = item;
                                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {NATIONALITY} --> NATIONALITY");
                                            }
                                            else
                                            {
                                                IDNUM += item;
                                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {IDNUM} --> IDNUM");
                                            }
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(NATIONALITY) && !string.IsNullOrEmpty(IDNUM))
                                    {
                                        if (lineNationality == null && lineIDNum == null)
                                        {
                                            // one line contains both
                                            lineNationalityIDNum = l;
                                        }
                                        else if (lineNationality == null)
                                        {
                                            // one line contains both
                                            lineNationality = l;
                                        }
                                        else if (lineIDNum == null)
                                        {
                                            // one line contains both
                                            lineIDNum = l;
                                        }
                                    }
                                    else
                                    {
                                        if (!string.IsNullOrEmpty(NATIONALITY))
                                        {
                                            lineNationality = l;
                                        }
                                        else if (!string.IsNullOrEmpty(IDNUM))
                                        {
                                            lineIDNum = l;
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(NATIONALITY) && !string.IsNullOrEmpty(IDNUM))
                                        return true; // line will be removed in FindFromMergedLine
                                    else
                                        lsLineMergedNotLabel.Remove(l);
                                }
                                return false;
                            }));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    if (string.IsNullOrEmpty(IDNUM))
                    {
                        try
                        {
                            // filter lines near to lien of name field
                            FindFromMergedLine(ref lsLineMergedNotLabel, centerYInInchOfField_IDNum,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is under the label
                                    if (m_labelMYDL_LESEN_MEMANDU.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_LESEN_MEMANDU.LineMacthed.ExtGetVerticalCenter()))
                                        return false;
                                    if (m_labelMYDL_MALAYSIA.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_MALAYSIA.LineMacthed.ExtGetVerticalCenter()))
                                        return false;
                                    if (m_labelMYDL_DRIVING_LICENCE.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_DRIVING_LICENCE.LineMacthed.ExtGetVerticalCenter()))
                                        return false;
                                    if (m_labelMYDL_Warganegara_Nationality.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_Warganegara_Nationality.LineMacthed.ExtGetVerticalCenter()))
                                        return false;
                                    if (m_labelMYDL_No_Pengenalan_Identity_No.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_No_Pengenalan_Identity_No.LineMacthed.ExtGetVerticalCenter()))
                                        return false;
                                    if (m_labelMYDL_Warganegara_Nationality_No_Pengenalan_Identity_No.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_Warganegara_Nationality_No_Pengenalan_Identity_No.LineMacthed.ExtGetVerticalCenter()))
                                        return false;

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> IDNUM");
                                    IDNUM = l.Text;
                                    lineIDNum = l;
                                    return true;
                                }));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{ex}");
                        }
                    }

                    try
                    {
                        // filter lines near to line of Class field
                        FindFromMergedLine(ref lsLineMergedNotLabel, centerYInInchOfField_Class,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the label
                                if (m_labelMYDL_LESEN_MEMANDU.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_LESEN_MEMANDU.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelMYDL_MALAYSIA.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_MALAYSIA.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelMYDL_DRIVING_LICENCE.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_DRIVING_LICENCE.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelMYDL_Warganegara_Nationality.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_Warganegara_Nationality.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelMYDL_No_Pengenalan_Identity_No.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_No_Pengenalan_Identity_No.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelMYDL_Warganegara_Nationality_No_Pengenalan_Identity_No.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_Warganegara_Nationality_No_Pengenalan_Identity_No.LineMacthed.ExtGetVerticalCenter()))
                                    return false;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> CLASS");
                                CLASS = l.Text;
                                lineClass = l;
                                return true;
                            }));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        // filter lines near to line of Validity field
                        FindFromMergedLine(ref lsLineMergedNotLabel, centerYInInchOfField_Validity,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the label
                                if (m_labelMYDL_LESEN_MEMANDU.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_LESEN_MEMANDU.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelMYDL_MALAYSIA.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_MALAYSIA.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelMYDL_DRIVING_LICENCE.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_DRIVING_LICENCE.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelMYDL_Warganegara_Nationality.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_Warganegara_Nationality.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelMYDL_No_Pengenalan_Identity_No.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_No_Pengenalan_Identity_No.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelMYDL_Warganegara_Nationality_No_Pengenalan_Identity_No.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_Warganegara_Nationality_No_Pengenalan_Identity_No.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelMYDL_Kelas_Class.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_Kelas_Class.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelMYDL_Tempoh_Validity.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMYDL_Tempoh_Validity.LineMacthed.ExtGetVerticalCenter()))
                                    return false;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> validity_expiry");
                                string validity_expiry = l.Text;
                                string validity_expiry_num = new string(validity_expiry.Where(c => '0' <= c && c <= '9').ToArray());
                                if (validity_expiry_num.Length >= 8)
                                {
                                    VALID_FROM = $"{validity_expiry_num.Substring(0, 2)}/{validity_expiry_num.Substring(2, 2)}/{validity_expiry_num.Substring(4, 4)}";
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {VALID_FROM} --> VALID_FROM");

                                    if (validity_expiry_num.Length >= 16)
                                    {
                                        VALID_UNTIL = $"{validity_expiry_num.Substring(8, 2)}/{validity_expiry_num.Substring(10, 2)}/{validity_expiry_num.Substring(12, 4)}";
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {VALID_UNTIL} --> VALID_UNTIL");
                                    }
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> invalid format for VALID_FROM, VALID_UNTIL");
                                }
                                lineValidity = l;
                                return true;
                            }));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        // filter lines in address field
                        Line[] mergedLinesBelowValidity = null;
                        if (m_labelMYDL_Alamat_Address.IsLabelFound)
                        {
                            mergedLinesBelowValidity = lsLineMergedNotLabel.Where(l => l.ExtGetVerticalCenter() > m_labelMYDL_Alamat_Address.LineMacthed.ExtGetVerticalCenter()).OrderBy(l => l.ExtGetVerticalCenter()).ToArray();
                        }
                        else if (lineValidity != null)
                        {
                            mergedLinesBelowValidity = lsLineMergedNotLabel.Where(l => l.ExtGetVerticalCenter() > lineValidity.ExtGetVerticalCenter()).OrderBy(l => l.ExtGetVerticalCenter()).ToArray();
                        }

                        if (mergedLinesBelowValidity != null)
                        {
                            foreach (Line line in mergedLinesBelowValidity)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress1: {line.Text} EstimateCenterYInInch: {y}");
                                // check if the line is under the label
                                if (m_labelMYDL_LESEN_MEMANDU.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMYDL_LESEN_MEMANDU.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelMYDL_MALAYSIA.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMYDL_MALAYSIA.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelMYDL_DRIVING_LICENCE.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMYDL_DRIVING_LICENCE.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelMYDL_Warganegara_Nationality.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMYDL_Warganegara_Nationality.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelMYDL_No_Pengenalan_Identity_No.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMYDL_No_Pengenalan_Identity_No.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelMYDL_Warganegara_Nationality_No_Pengenalan_Identity_No.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMYDL_Warganegara_Nationality_No_Pengenalan_Identity_No.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelMYDL_Kelas_Class.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMYDL_Kelas_Class.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelMYDL_Tempoh_Validity.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMYDL_Tempoh_Validity.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelMYDL_Alamat_Address.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMYDL_Alamat_Address.LineMacthed.ExtGetVerticalCenter()))
                                    continue;

                                // address lines should be under address line 1
                                if (y < centerYInInchOfField_Address1 - ACCEPTABLE_DIFF_IN_LINE)
                                    continue;

                                if (string.IsNullOrEmpty(ADDRESS1))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS1");
                                    ADDRESS1 = line.Text;
                                    lineAddress1 = line;
                                    lsLineMergedNotLabel.Remove(line);
                                    continue;
                                }
                                else if (string.IsNullOrEmpty(ADDRESS2))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS2");
                                    ADDRESS2 = line.Text;
                                    lineAddress2 = line;
                                    lsLineMergedNotLabel.Remove(line);
                                    continue;
                                }
                                else if (string.IsNullOrEmpty(ADDRESS3))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS3");
                                    ADDRESS3 = line.Text;
                                    lineAddress3 = line;
                                    lsLineMergedNotLabel.Remove(line);
                                    continue;
                                }
                                else if (string.IsNullOrEmpty(ADDRESS4))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS4");
                                    ADDRESS4 = line.Text;
                                    lineAddress3 = line;
                                    lsLineMergedNotLabel.Remove(line);
                                    continue;
                                }
                                else if (string.IsNullOrEmpty(ADDRESS5))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS5");
                                    ADDRESS5 = line.Text;
                                    lineAddress3 = line;
                                    lsLineMergedNotLabel.Remove(line);
                                    continue;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    if (string.IsNullOrEmpty(ADDRESS1))
                    {
                        try
                        {
                            // filter lines near to line of address field
                            Line[] mergedLinesNearToAddress1 = lsLineMergedNotLabel.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address1 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToAddress1)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress1: {line.Text} EstimateCenterYInInch: {y}");
                                // check if the line is under the label
                                if (m_labelMYDL_LESEN_MEMANDU.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMYDL_LESEN_MEMANDU.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelMYDL_MALAYSIA.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMYDL_MALAYSIA.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelMYDL_DRIVING_LICENCE.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMYDL_DRIVING_LICENCE.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelMYDL_Warganegara_Nationality.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMYDL_Warganegara_Nationality.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelMYDL_No_Pengenalan_Identity_No.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMYDL_No_Pengenalan_Identity_No.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelMYDL_Warganegara_Nationality_No_Pengenalan_Identity_No.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMYDL_Warganegara_Nationality_No_Pengenalan_Identity_No.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelMYDL_Kelas_Class.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMYDL_Kelas_Class.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelMYDL_Tempoh_Validity.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMYDL_Tempoh_Validity.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelMYDL_Alamat_Address.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMYDL_Alamat_Address.LineMacthed.ExtGetVerticalCenter()))
                                    continue;

                                // address lines should be under address line 1
                                if (y < centerYInInchOfField_Address1 - ACCEPTABLE_DIFF_IN_LINE)
                                    continue;

                                if (string.IsNullOrEmpty(ADDRESS1))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS1");
                                    ADDRESS1 = line.Text;
                                    lineAddress1 = line;
                                    lsLineMergedNotLabel.Remove(line);
                                    continue;
                                }
                                else if (string.IsNullOrEmpty(ADDRESS2))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS2");
                                    ADDRESS2 = line.Text;
                                    lineAddress2 = line;
                                    lsLineMergedNotLabel.Remove(line);
                                    continue;
                                }
                                else if (string.IsNullOrEmpty(ADDRESS3))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS3");
                                    ADDRESS3 = line.Text;
                                    lineAddress3 = line;
                                    lsLineMergedNotLabel.Remove(line);
                                    continue;
                                }
                                else if (string.IsNullOrEmpty(ADDRESS4))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS4");
                                    ADDRESS4 = line.Text;
                                    lineAddress3 = line;
                                    lsLineMergedNotLabel.Remove(line);
                                    continue;
                                }
                                else if (string.IsNullOrEmpty(ADDRESS5))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS5");
                                    ADDRESS5 = line.Text;
                                    lineAddress3 = line;
                                    lsLineMergedNotLabel.Remove(line);
                                    continue;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{ex}");
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

                // (CITIZENSHIP) nationality is "MALAYSIA" or 3 letter code
                Code.Country country = FindCountry(NATIONALITY);
                if (country != null)
                    result.nationality = country.ncode;
                else
                    result.nationality = NATIONALITY;

                if (string.IsNullOrEmpty(NATIONALITY)) lsMissingFields.Add("NATIONALITY");

                try
                {
                    result.documentIssueDate = "";
#if false
                // VALID_FROM "dd/MM/yyyy" -> documentIssueDate "yyyy-MM-dd"
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
#else
                    if (string.IsNullOrEmpty(VALID_FROM))
                        lsMissingFields.Add("VALID_FROM");
                    else
                        result.documentIssueDate = VALID_FROM;
#endif
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                    lsMissingFields.Add("VALID_FROM");
                }

                try
                {
                    result.documentExpirationDate = "";
#if false
                // VALID_UNTIL "dd/MM/yyyy" -> documentExpirationDate "yyyy-MM-dd"
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
#else
                    if (string.IsNullOrEmpty(VALID_UNTIL))
                        lsMissingFields.Add("VALID_UNTIL");
                    else
                        result.documentExpirationDate = VALID_UNTIL;
#endif
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                    lsMissingFields.Add("VALID_UNTIL");
                }

#if false
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
                if (!string.IsNullOrEmpty(POSTCODE))
                {
                    result.postcode = POSTCODE;
                }
                else
                {
                    lsMissingFields.Add("POSTCODE");
                }
#else
                // ADDRESS1, ADDRESS2, ADDRESS3, STATE -> addressLine1, addressLine2
                // ADDRESS1, ADDRESS2, ADDRESS3, ADDRESS4, ADDRESS5 -> addressLine1, addressLine2, POSTCODE, CITY, STATE 
                if (string.IsNullOrEmpty(ADDRESS1))
                {
                    lsMissingFields.Add("ADDRESS1");
                }
                else
                {
                    if (string.IsNullOrEmpty(ADDRESS2))
                    {
                        result.addressLine1 = ADDRESS1;
                        lsMissingFields.Add("POSTCODE");
                        lsMissingFields.Add("CITY");
                        lsMissingFields.Add("STATE");
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(ADDRESS3))
                        {
                            result.addressLine1 = ADDRESS1;
                            result.addressLine2 = ADDRESS2;
                            lsMissingFields.Add("POSTCODE");
                            lsMissingFields.Add("CITY");
                            lsMissingFields.Add("STATE");
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(ADDRESS4))
                            {
                                // ADDRESS1 --> AddressLine1
                                // ADDRESS2 --> POSTCODE, CITY
                                // ADDRESS3 --> STATE

                                // Extract POSTCODE CITY
                                string postcode_city = ADDRESS2;
                                string[] token = postcode_city.Split(SEPARATOR_BLANK, 2);
                                if (token.Length > 1)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> POSTCODE: {token[0]} CITY: {token[1]}");
                                    POSTCODE = token[0];
                                    CITY = token[1];
                                }
                                else
                                {
                                    CITY = postcode_city;
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> CITY: {CITY}");
                                }

                                STATE = ADDRESS3;
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> STATE: {STATE}");

                                result.addressLine1 = ADDRESS1;
                                result.addressLine2 = $"{CITY} {STATE}";
                            }
                            else
                            {
                                if (string.IsNullOrEmpty(ADDRESS5))
                                {
                                    // ADDRESS1 --> AddressLine1
                                    // ADDRESS2 --> AddressLine2
                                    // ADDRESS3 --> POSTCODE, CITY
                                    // ADDRESS4 --> STATE

                                    // Extract POSTCODE CITY
                                    string postcode_city = ADDRESS3;
                                    string[] token = postcode_city.Split(SEPARATOR_BLANK, 2);
                                    if (token.Length > 1)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> POSTCODE: {token[0]} CITY: {token[1]}");
                                        POSTCODE = token[0];
                                        CITY = token[1];
                                    }
                                    else
                                    {
                                        CITY = postcode_city;
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> CITY: {CITY}");
                                    }

                                    STATE = ADDRESS4;
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> STATE: {STATE}");

                                    result.addressLine1 = $"{ADDRESS1}";
                                    result.addressLine2 = $"{ADDRESS2} {CITY} {STATE}";
                                }
                                else
                                {
                                    // ADDRESS1 --> AddressLine1
                                    // ADDRESS2 --> AddressLine1
                                    // ADDRESS3 --> AddressLine2
                                    // ADDRESS4 --> POSTCODE, CITY
                                    // ADDRESS5 --> STATE
                                    // Extract POSTCODE CITY
                                    string postcode_city = ADDRESS4;
                                    string[] token = postcode_city.Split(SEPARATOR_BLANK, 2);
                                    if (token.Length > 1)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> POSTCODE: {token[0]} CITY: {token[1]}");
                                        POSTCODE = token[0];
                                        CITY = token[1];
                                    }
                                    else
                                    {
                                        CITY = postcode_city;
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> CITY: {CITY}");
                                    }

                                    STATE = ADDRESS5;
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> STATE: {STATE}");

                                    result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                                    result.addressLine2 = $"{ADDRESS3} {CITY} {STATE}";
                                }
                            }
                        }
                    }
                }

                // POSTCODE
                if (string.IsNullOrEmpty(POSTCODE)) lsMissingFields.Add("POSTCODE");
                else result.postcode = POSTCODE;
#endif
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw ex;
            }
        }

        public void FindLabels(IList<Line> lines, string docType, out List<LabelInfo> labelsFound, out List<Line> linesNotLabel)
        {
            labelsFound = new List<LabelInfo>();
            linesNotLabel = new List<Line>();

            foreach (Line line in lines)
            {
                LabelInfo labelFound = FindLabelInMergedLine(line, docType);
                if (labelFound != null)
                {
                    labelsFound.Add(labelFound);
                }
                else
                {
                    linesNotLabel.Add(line);
                }
            }
        }

        public string FindLabelsAndIdentifyDocType(IList<Line> lines, out List<LabelInfo> labelsFound, out List<Line> linesNotLabel)
        {
            string retDocType = "";
            labelsFound = new List<LabelInfo>();
            linesNotLabel = new List<Line>();
            Dictionary<string, List<LabelInfo>> dicDocTypeLabelsFound = new Dictionary<string, List<LabelInfo>>();
            Dictionary<string, List<Line>> dicDocTypeLinesNotLabel = new Dictionary<string, List<Line>>();

            // find labels for each docType
            foreach (string docType in m_docTypes)
            {
                //LabelInfo[] labelsToFind = GetLabelsToFind(docType);
                //LabelInfo[] labelsAboveFields = GetLabelsAboveFields(docType);
                List<LabelInfo> labelsFoundForDocType = null;
                List<Line> linesNotLabelForDocType = null;
                FindLabels(lines, docType, out labelsFoundForDocType, out linesNotLabelForDocType);
                dicDocTypeLabelsFound.Add(docType, labelsFoundForDocType);
                dicDocTypeLinesNotLabel.Add(docType, linesNotLabelForDocType);
            }

            float maxLabelFoundRate = 0f;
            foreach(string docType in dicDocTypeLabelsFound.Keys)
            {
                // count number of labels useful to identify title
                LabelInfo[] labelsToFind = GetLabelsToFind(docType);
                int countLabelsForTitleExpected = labelsToFind.Where(l => l.IsTitleToIdentify).Count();
                /*
                int countLabelsForTitleExpected = 0;
                foreach (LabelInfo label in labelsToFind)
                {
                    if (label.IsTitleToIdentify)
                    {
                        countLabelsForTitleExpected++;
                    }
                }
                */

                // count number of labels useful to identify title found in lines
                List<LabelInfo> labelsFoundDocType = dicDocTypeLabelsFound[docType];
                int countLabelsForTitleFound = labelsFoundDocType.Where(l => 
                    l.IsTitleToIdentify && l.IsLabelFound && (!l.FollowedByField || l.FollowedByField && !string.IsNullOrEmpty(l.FieldFollowing))).Count();
                /*
                int countLabelsForTitleFound = 0;
                foreach(LabelInfo label in labelsFoundDocType)
                {
                    if (label.IsTitleToIdentify)
                    {
                        if (label.IsLabelFound)
                        {
                            countLabelsForTitleFound++;
                        }
                    }
                }
                */

                // calculate label found rate to find the docType with highest label found rate
                float labelFoundRate = 0f;
                if(countLabelsForTitleExpected > 0)
                {
                    labelFoundRate = (float)countLabelsForTitleFound / (float)countLabelsForTitleExpected;
                    if (maxLabelFoundRate < labelFoundRate)
                    {
                        maxLabelFoundRate = labelFoundRate;
                        retDocType = docType;
                    }
                }
            }

            if (!string.IsNullOrEmpty(retDocType))
            {
                labelsFound = dicDocTypeLabelsFound[retDocType];
                linesNotLabel = dicDocTypeLinesNotLabel[retDocType];
            }

            return retDocType;
        }
        bool DoMatchTemplate(ImgProcLib.MatchTemplateIDCard? matchTemplate, SKImage imageSrc, double topEdgeYOfIDImageInPixel, double leftEdgeXOfIDImageInPixel, double ppi,
            out MatchTemplateResult matchTemplateResult, out SKData dataID200ppiPng)
        {
            matchTemplateResult = null;
            dataID200ppiPng = null;

            int widthOfIDImageInPixel = (int)(3.35f * ppi);
            int heightOfIDImageInPixel = (int)(2.15f * ppi);
            SKRectI rect = new SKRectI(
                (int)leftEdgeXOfIDImageInPixel - (int)(ppi * 0.1),
                (int)topEdgeYOfIDImageInPixel - (int)(ppi * 0.1),
                (int)leftEdgeXOfIDImageInPixel + widthOfIDImageInPixel + (int)(ppi * 0.1),
                (int)topEdgeYOfIDImageInPixel + heightOfIDImageInPixel + (int)(ppi * 0.1));
            if (rect.Top < 0) rect.Top = 0;
            if (rect.Left < 0) rect.Left = 0;
            if (rect.Right > imageSrc.Width) rect.Right = imageSrc.Width;
            if (rect.Bottom > imageSrc.Height) rect.Bottom = imageSrc.Height;
            SKImage imageIDSrc = imageSrc.Subset(rect);
            //SKData dataIDPng = imageIDSrc.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
            double rate = 200.0f / ppi;
            SKBitmap bmpID = SKBitmap.FromImage(imageIDSrc);
            SKBitmap bmpID200ppi = bmpID.Resize(new SKSizeI((int)(imageIDSrc.Width * rate), (int)(imageIDSrc.Height * rate)), SKFilterQuality.High);
            //dataID200ppiPng = bmpID200ppi.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
            double rateAdjust = (double)bmpID200ppi.Width / 670.0f; // DoMatchTemplate need the image width 670
            int heightAdjusted = (int)((double)bmpID200ppi.Height / rateAdjust);
            SKBitmap bmpID670 = bmpID200ppi.Resize(new SKSizeI(670, heightAdjusted), SKFilterQuality.High);
            dataID200ppiPng = bmpID670.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
            //cardImage200ppiPngB64 = Convert.ToBase64String(dataID200ppiPng.ToArray());
            //using (FileStream fs = new FileStream("ID.png", FileMode.Create))
            //{
            //    dataID200ppiPng.SaveTo(fs);
            //}

            if (matchTemplate != null)
            {
                matchTemplateResult = matchTemplate.DoMatchTemplate(dataID200ppiPng.ToArray());
                return true;
            }
            return false;
        }

        static void GenerateMatchTemplateResults(MatchTemplateResult matchTemplateResult, Dictionary<string, MatchTemplateInfo> dicMatchTemplateInfo, Dictionary<string, MatchTemplateResultInfo> dicMatchTemplateResults)
        {
            foreach (string key in matchTemplateResult.MatchResult.Keys)
            {
                MatchTemplateResultItem matchTemplateResultItem = matchTemplateResult.MatchResult[key];
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] key: {key} MatchResult: {matchTemplateResultItem.MatchResult} x: {matchTemplateResultItem.LocX} y: {matchTemplateResultItem.LocY} w: {matchTemplateResultItem.Width} h: {matchTemplateResultItem.Height}");
                if (dicMatchTemplateInfo.ContainsKey(key))
                {
                    MatchTemplateResultInfo matchTemplateResultInfo = new MatchTemplateResultInfo();
                    matchTemplateResultInfo.Title = key;
                    matchTemplateResultInfo.MatchTemplateInfo = dicMatchTemplateInfo[key];
                    matchTemplateResultInfo.MatchTemplateInfo.MatchResult = matchTemplateResultItem.MatchResult;
                    matchTemplateResultInfo.MatchTemplateInfo.ResultLocX = matchTemplateResultItem.LocX;
                    matchTemplateResultInfo.MatchTemplateInfo.ResultLocY = matchTemplateResultItem.LocY;
                    matchTemplateResultInfo.MatchTemplateInfo.ResultWidth = matchTemplateResultItem.Width;
                    matchTemplateResultInfo.MatchTemplateInfo.ResultHeight = matchTemplateResultItem.Height;
                    double? dist = matchTemplateResultInfo.GetDistanceFromExpectedCenterInInch();
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] key: {key} dist: {dist} ");
                    dicMatchTemplateResults.Add(key, matchTemplateResultInfo);
                }
                /*
                using (FileStream fs = new FileStream(matchTemplateMyKadResultItem.GetName() + ".png", FileMode.Create))
                {
                    SKRectI rectLandmark = new SKRectI((int)matchTemplateMyKadResultItem.LocX, (int)matchTemplateMyKadResultItem.LocY, (int)matchTemplateMyKadResultItem.LocX + matchTemplateMyKadResultItem.Width, (int)matchTemplateMyKadResultItem.LocY + matchTemplateMyKadResultItem.Height);
                    SKImage imageLandmark = imgID200ppi.Subset(rectLandmark);
                    SKData dataLandmark = imageLandmark.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                    dataLandmark.SaveTo(fs);
                }
                */
            }
        }
#if true
        ScanMyKadResult ExtractFieldsFromReadResultOfMyKad(LabelInfo[] labelsFound, Line[] linesMergedNotLabel, SKImage imageSrc, ImgProcLib.MatchTemplateIDCard? matchTemplateMyKad)
        {
            const double ACCEPTABLE_DIFF_IN_LINE = 0.1f;
            List<Line> lsLineMergedNotLabel = new List<Line>();
            lsLineMergedNotLabel.AddRange(linesMergedNotLabel); 

            const double centerYInInchOfField_IDNUM = 0.55f;
            const double centerYInInchOfField_Name = 1.4f;
            const double centerYInInchOfField_IDNUM_UnderFaceImage = 1.75f;
            const double centerYInInchOfField_Citizenship = 1.85f;
            const double centerYInInchOfField_Gender = 1.95f;
            const double centerYInInchOfField_Address1 = 1.64f;
            const double centerYInInchOfField_Address2 = 1.73f;
            const double centerYInInchOfField_Address3 = 1.82f;
            const double centerYInInchOfField_Address4 = 1.91f;
            const double centerYInInchOfField_Address5 = 2.00f;

            /*
MatchTemplate key: MyKad_670_Flag MatchResult: 0.9028843641281128 x: 482 y: 16 w: 138 h: 75
MatchTemplate key: MyKad_670_Flag_Gray MatchResult: 0.9330259561538696 x: 481 y: 17 w: 138 h: 71
MatchTemplate key: MyKad_670_Flag_MoonStar MatchResult: 0.9079629182815552 x: 493 y: 24 w: 33 h: 25
MatchTemplate key: MyKad_670_Flag_MoonStar_Gray MatchResult: 0.9398182034492493 x: 494 y: 24 w: 30 h: 27
MatchTemplate key: MyKad_670_Flower_Front_Gray MatchResult: 0.8148484826087952 x: 275 y: 326 w: 122 h: 92
MatchTemplate key: MyKad_670_IC_Chip MatchResult: 0.5414834022521973 x: 32 y: 139 w: 118 h: 103
MatchTemplate key: MyKad_670_IC_Chip_Gray MatchResult: 0.6005121469497681 x: 40 y: 143 w: 102 h: 92
MatchTemplate key: MyKad_670_IC_Chip_old MatchResult: 0.33573946356773376 x: 36 y: 141 w: 108 h: 99
MatchTemplate key: MyKad_670_IC_Chip_old_Gray MatchResult: 0.2513715922832489 x: 221 y: 74 w: 102 h: 93
MatchTemplate key: MyKad_670_MSC_Gray MatchResult: 0.8906120657920837 x: 224 y: 109 w: 54 h: 54
MatchTemplate key: MyKad_670_MyKad MatchResult: 0.9330142736434937 x: 375 y: 8 w: 59 h: 39
MatchTemplate key: MyKad_670_MyKad_Gray MatchResult: 0.9301272630691528 x: 376 y: 9 w: 61 h: 39
             */
            Dictionary<string, MatchTemplateInfo> dicMatchTemplateInfo = new Dictionary<string, MatchTemplateInfo>();
            MatchTemplateInfo matchTmplMyKad_670_Flag = new MatchTemplateInfo("MyKad_670_Flag", "Flag", 0.8f, 2.9f, 0.25f);
            dicMatchTemplateInfo.Add(matchTmplMyKad_670_Flag.Name, matchTmplMyKad_670_Flag);
            //MatchTemplateInfo matchTmplMyKad_670_Flag_Gray = new MatchTemplateInfo("MyKad_670_Flag_Gray", 0.8f, 2.9f, 0.25f);
            //dicMatchTemplateInfo.Add(matchTmplMyKad_670_Flag_Gray.Title, matchTmplMyKad_670_Flag_Gray);
            MatchTemplateInfo matchTmplMyKad_670_Flag_MoonStar = new MatchTemplateInfo("MyKad_670_Flag_MoonStar", "Flag_MoonStar", 0.8f, 2.7f, 0.15f);
            dicMatchTemplateInfo.Add(matchTmplMyKad_670_Flag_MoonStar.Name, matchTmplMyKad_670_Flag_MoonStar);
            //MatchTemplateInfo matchTmplMyKad_670_Flag_MoonStar_Gray = new MatchTemplateInfo("MyKad_670_Flag_MoonStar_Gray", 0.8f, 2.7f, 0.15f);
            //dicMatchTemplateInfo.Add(matchTmplMyKad_670_Flag_MoonStar_Gray.Title, matchTmplMyKad_670_Flag_MoonStar_Gray);
            MatchTemplateInfo matchTmplMyKad_670_Flower_Front_Gray = new MatchTemplateInfo("MyKad_670_Flower_Front_Gray", "Flower_Watermark", 0.6f, 1.8f, 1.8f);
            dicMatchTemplateInfo.Add(matchTmplMyKad_670_Flower_Front_Gray.Name, matchTmplMyKad_670_Flower_Front_Gray);
            MatchTemplateInfo matchTmplMyKad_670_IC_Chip = new MatchTemplateInfo("MyKad_670_IC_Chip", "IC_Chip", 0.4f, 0.55f, 0.9f);
            dicMatchTemplateInfo.Add(matchTmplMyKad_670_IC_Chip.Name, matchTmplMyKad_670_IC_Chip);
            //MatchTemplateInfo matchTmplMyKad_670_IC_Chip_Gray = new MatchTemplateInfo("MyKad_670_IC_Chip_Gray", 0.4f, 0.55f, 0.9f);
            //dicMatchTemplateInfo.Add(matchTmplMyKad_670_IC_Chip_Gray.Title, matchTmplMyKad_670_IC_Chip_Gray);
            MatchTemplateInfo matchTmplMyKad_670_IC_Chip_old = new MatchTemplateInfo("MyKad_670_IC_Chip_old", "IC_Chip_Old", 0.4f, 0.55f, 0.9f);
            dicMatchTemplateInfo.Add(matchTmplMyKad_670_IC_Chip_old.Name, matchTmplMyKad_670_IC_Chip_old);
            //MatchTemplateInfo matchTmplMyKad_670_IC_Chip_old_Gray = new MatchTemplateInfo("MyKad_670_IC_Chip_old_Gray", 0.4f, 0.55f, 0.9f);
            //dicMatchTemplateInfo.Add(matchTmplMyKad_670_IC_Chip_old_Gray.Title, matchTmplMyKad_670_IC_Chip_old_Gray);
            MatchTemplateInfo matchTmplMyKad_670_MSC_Gray = new MatchTemplateInfo("MyKad_670_MSC_Gray", "MSC_Watermark", 0.8f, 1.35f, 0.7f);
            dicMatchTemplateInfo.Add(matchTmplMyKad_670_MSC_Gray.Name, matchTmplMyKad_670_MSC_Gray);
            MatchTemplateInfo matchTmplMyKad_670_MyKad = new MatchTemplateInfo("MyKad_670_MyKad", "MyKad_Logo_Top", 0.8f, 2.15f, 0.15f);
            dicMatchTemplateInfo.Add(matchTmplMyKad_670_MyKad.Name, matchTmplMyKad_670_MyKad);
            //MatchTemplateInfo matchTmplMyKad_670_MyKad_Gray = new MatchTemplateInfo("MyKad_670_MyKad_Gray", 0.8f, 2.15f, 0.15f);
            //dicMatchTemplateInfo.Add(matchTmplMyKad_670_MyKad_Gray.Title, matchTmplMyKad_670_MyKad_Gray);


            ScanMyKadResult result = new ScanMyKadResult();

            string IDNUM = "";
            string NAME = "";
            string ADDRESS1 = "";
            string ADDRESS2 = "";
            string ADDRESS3 = "";
            string ADDRESS4 = "";
            string ADDRESS5 = "";
            string POSTCODE = "";
            string CITY = "";
            string STATE = "";
            string CITIZENSHIP = "";
            string GENDER = "";
            string EASTMSIAN = "";
            string BIRTHDATE = "";

            Line lineName = null;
            Line lineCitizenship = null;
            Line lineGender = null;
            Line lineAddress1 = null;
            Line lineAddress2 = null;
            Line lineAddress3 = null;
            Line lineAddress4 = null;
            Line lineAddress5 = null;

            // calc pixel per inch
            double? ppi = LabelInfo.CalcPixelPerInch(labelsFound);
            if (ppi == null)
            {
                Console.WriteLine("CalcPixelPerInch failed");
            }
            else
            {
                //
                // calc top and left edge
                //
                double? topEdgeYOfIDImageInPixel;
                double? leftEdgeXOfIDImageInPixel;
                CalcLeftTopEdgeInPixel(labelsFound, ppi, out leftEdgeXOfIDImageInPixel, out topEdgeYOfIDImageInPixel);
                if (topEdgeYOfIDImageInPixel != null && leftEdgeXOfIDImageInPixel != null)
                {
                    MatchTemplateResult matchTemplateResult = null;
                    SKData dataID200ppiPng = null;
                    DateTime dtStart = DateTime.Now;
                    bool bRetMatchTemplate = DoMatchTemplate(matchTemplateMyKad, imageSrc, topEdgeYOfIDImageInPixel.Value, leftEdgeXOfIDImageInPixel.Value, ppi.Value,
                        out matchTemplateResult, out dataID200ppiPng);
                    DateTime dtEnd = DateTime.Now;
                    result.timeElapsedLandmarkDetection = (int)(dtEnd - dtStart).TotalMilliseconds;
                    result.CardImage200ppiPngB64 = Convert.ToBase64String(dataID200ppiPng.ToArray());
                    if (bRetMatchTemplate)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTemplateMyKadResult: {matchTemplateResult.MatchResult}");
                        SKImage imgID200ppi = SKImage.FromEncodedData(dataID200ppiPng);

                        // choose IC_Chip
                        if (matchTemplateResult.MatchResult.ContainsKey("MyKad_670_IC_Chip") && matchTemplateResult.MatchResult.ContainsKey("MyKad_670_IC_Chip_old"))
                        {
                            MatchTemplateResultItem matchTemplateResultItemMyKad_670_IC_Chip = matchTemplateResult.MatchResult["MyKad_670_IC_Chip"];
                            MatchTemplateResultItem matchTemplateResultItemMyKad_670_IC_Chip_old = matchTemplateResult.MatchResult["MyKad_670_IC_Chip_old"];
                            if (matchTemplateResultItemMyKad_670_IC_Chip.MatchResult < matchTemplateResultItemMyKad_670_IC_Chip_old.MatchResult)
                            {
                                matchTemplateResult.MatchResult.Remove("MyKad_670_IC_Chip");
                            }
                            else
                            {
                                matchTemplateResult.MatchResult.Remove("MyKad_670_IC_Chip_old");
                            }
                        }
                        GenerateMatchTemplateResults(matchTemplateResult, dicMatchTemplateInfo, result.MatchTemplateResults);
                        result.landmarkImageBase64 = GenerateMatchTemplateResultImage(result.MatchTemplateResults, result.CardImage200ppiPngB64);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTemplate failed");
                    }
                }

                //
                // remove lines shorter than expected
                //
                const double labelHeightFilterInInch = 0.07f;   // text shorter than this height shuld be ignored
                double labelHeightFilterInPixel = (double)labelHeightFilterInInch * ppi.Value;
                //if (linesField.Count > 0)
                //{
                //    // remove lines predicted as label because of height
                //    int removedFromLinesField = linesField.RemoveAll(l => l.ExtGetHeight() < labelHeightFilterInPixel);
                //    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] removed {removedFromLinesField} lines predicted as label removed from linesField [height < labelHeightInPixel ({labelHeightFilterInPixel})].");
                //}
                if (lsLineMergedNotLabel.Count > 0)
                {
                    // remove lines predicted as label because of height
                    int removedFromLinesMerged = lsLineMergedNotLabel.RemoveAll(l => l.ExtGetHeight() < labelHeightFilterInPixel);
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] removed {removedFromLinesMerged} lines predicted as label removed from lsLineMerged [height < labelHeightInPixel ({labelHeightFilterInPixel})].");
                }

                // predit y in inch and expected field for each file line  
                //foreach (Line line in linesField)
                foreach (Line line in lsLineMergedNotLabel)
                {
                    double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                    System.Diagnostics.Debug.WriteLine($"line: {line.Text} EstimateCenterYInInch: {y}");
                }

                try
                {
                    if (m_labelMyKadIDNUM.IsLabelFound)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {m_labelMyKadIDNUM.LineMacthed.Text} --> IDNUM");
                        IDNUM = m_labelMyKadIDNUM.LineMacthed.Text;
                    }
                    else if (m_labelMyKadIDNUM_UnderFaceImage.IsLabelFound)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {m_labelMyKadIDNUM_UnderFaceImage.LineMacthed.Text} --> IDNUM");
                        IDNUM = m_labelMyKadIDNUM_UnderFaceImage.LineMacthed.Text;
                    }
                    else
                    {
                        // filter lines near to lien of IDNUM field
                        Line linePossiblyIDNum = null;
                        Line[] mergedLinesNearToIDNUM = lsLineMergedNotLabel.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_IDNUM - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToIDNUM)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToIDNUM: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_IDNUM)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the label
                                if (m_labelMyKadKAD_PENGENALAN.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMyKadKAD_PENGENALAN.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelMyKadMALAYSIA.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMyKadMALAYSIA.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelMyKadIDENTITY_CARD.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMyKadIDENTITY_CARD.LineMacthed.ExtGetVerticalCenter()))
                                    continue;

                                if (!string.IsNullOrEmpty(line.Text))
                                {
                                    Match matchIDNum = regexMyKadIDNum.Match(line.Text);
                                    if (matchIDNum.Success)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {matchIDNum.Value} --> IDNUM");
                                        IDNUM = matchIDNum.Value;
                                        lsLineMergedNotLabel.Remove(line);
                                        break;
                                    }

                                    string strNumeric = string.Concat(line.Text.Where(c => Char.IsDigit(c)));
                                    if (!string.IsNullOrEmpty(strNumeric))
                                    {
                                        Match matchIDNum2 = regexNum10DigitOrMore.Match(strNumeric);
                                        if (matchIDNum2.Success)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> possibly IDNUM");
                                            linePossiblyIDNum = line;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(IDNUM))
                        {
                            // try to read ID number under face image 

                            double? rightEdgeOfLeftAlignLabels = null;
                            if (m_labelMyKadKAD_PENGENALAN.IsLabelFound)
                                rightEdgeOfLeftAlignLabels = m_labelMyKadKAD_PENGENALAN.LineMacthed.ExtGetRight();
                            else if (m_labelMyKadMALAYSIA.IsLabelFound)
                                rightEdgeOfLeftAlignLabels = m_labelMyKadMALAYSIA.LineMacthed.ExtGetRight();
                            else if (m_labelMyKadIDENTITY_CARD.IsLabelFound)
                                rightEdgeOfLeftAlignLabels = m_labelMyKadIDENTITY_CARD.LineMacthed.ExtGetRight();

                            // filter lines near to line of IDNUM field
                            Line[] linesRightAlign = lsLineMergedNotLabel.Where(l => l.ExtGetLeft() > rightEdgeOfLeftAlignLabels).OrderBy(l => l.ExtGetTop()).ToArray();
                            if (linesRightAlign != null && linesRightAlign.Count() > 0)
                            {
                                Line[] mergedLinesNearToIDNUMUnderFaceImage = linesRightAlign.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_IDNUM_UnderFaceImage - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                                foreach (Line line in mergedLinesNearToIDNUMUnderFaceImage)
                                {
                                    double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                    System.Diagnostics.Debug.WriteLine($"mergedLinesNearToIDNUMUnderFaceImage: {line.Text} EstimateCenterYInInch: {y}");
                                    if (Math.Abs((decimal)(y - centerYInInchOfField_IDNUM_UnderFaceImage)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                    {
                                        // check if the line is above the label
                                        if (m_labelMyKadWARGANEGARA.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMyKadWARGANEGARA.LineMacthed.ExtGetVerticalCenter()))
                                            continue;
                                        if (m_labelMyKadPEREMPUAN.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMyKadPEREMPUAN.LineMacthed.ExtGetVerticalCenter()))
                                            continue;
                                        if (m_labelMyKadLELAKI.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMyKadLELAKI.LineMacthed.ExtGetVerticalCenter()))
                                            continue;

                                        Match matchIDNum = regexMyKadIDNum.Match(line.Text);
                                        if (matchIDNum.Success)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {matchIDNum.Value} --> IDNUM");
                                            IDNUM = matchIDNum.Value;
                                            lsLineMergedNotLabel.Remove(line);
                                            if(linePossiblyIDNum != null)
                                            {
                                                lsLineMergedNotLabel.Remove(linePossiblyIDNum);
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(IDNUM) && linePossiblyIDNum != null)
                        {
                            // take the line suspected to be IDNUM
                            IDNUM = linePossiblyIDNum.Text;
                            lsLineMergedNotLabel.Remove(linePossiblyIDNum);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex}");
                }

                try
                {
                    if(m_labelMyKadWARGANEGARA.IsLabelFound)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {m_labelMyKadWARGANEGARA.LineMacthed.Text} --> CITIZENSHIP");
                        CITIZENSHIP = m_labelMyKadWARGANEGARA.LineMacthed.Text;
                    }
                    else
                    {
                        double? rightEdgeOfLeftAlignLabels = null;
                        if (m_labelMyKadKAD_PENGENALAN.IsLabelFound)
                            rightEdgeOfLeftAlignLabels = m_labelMyKadKAD_PENGENALAN.LineMacthed.ExtGetRight();
                        else if (m_labelMyKadMALAYSIA.IsLabelFound)
                            rightEdgeOfLeftAlignLabels = m_labelMyKadMALAYSIA.LineMacthed.ExtGetRight();
                        else if (m_labelMyKadIDENTITY_CARD.IsLabelFound)
                            rightEdgeOfLeftAlignLabels = m_labelMyKadIDENTITY_CARD.LineMacthed.ExtGetRight();

                        Line[] linesRightAlign = lsLineMergedNotLabel.Where(l => l.ExtGetLeft() > rightEdgeOfLeftAlignLabels).OrderBy(l => l.ExtGetTop()).ToArray();
                        if(linesRightAlign != null && linesRightAlign.Count() > 0)
                        {
                            // filter lines near to lien of name field
                            Line[] mergedLinesNearToCitizenship = linesRightAlign.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Citizenship - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToCitizenship)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToCitizenship: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_Citizenship)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is under the label
                                    if (m_labelMyKadKAD_PENGENALAN.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMyKadKAD_PENGENALAN.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (m_labelMyKadMALAYSIA.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMyKadMALAYSIA.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (m_labelMyKadIDENTITY_CARD.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMyKadIDENTITY_CARD.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (m_labelMyKadIDNUM.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMyKadIDNUM.LineMacthed.ExtGetVerticalCenter()))
                                        continue;

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> CITIZENSHIP");
                                    CITIZENSHIP = line.Text;
                                    lineCitizenship = line;
                                    break;
                                }
                            }
                            // remove lines right aligned from lsit of merged lines
                            // because other lines to find should not be right aligned
                            foreach (Line line in linesRightAlign)
                            {
                                lsLineMergedNotLabel.Remove(line);
                            }
                        }
                    }

                    if (m_labelMyKadLELAKI.IsLabelFound)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {m_labelMyKadLELAKI.LineMacthed.Text} --> GENDER ({m_labelMyKadLELAKI.Title})");
                        GENDER = m_labelMyKadLELAKI.Title;
                    }
                    else if (m_labelMyKadPEREMPUAN.IsLabelFound)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {m_labelMyKadPEREMPUAN.LineMacthed.Text} --> GENDER ({m_labelMyKadPEREMPUAN.Title})");
                        GENDER = m_labelMyKadPEREMPUAN.Title;
                    }
                    else
                    {
                        double? rightEdgeOfLeftAlignLabels = null;
                        if (m_labelMyKadKAD_PENGENALAN.IsLabelFound)
                            rightEdgeOfLeftAlignLabels = m_labelMyKadKAD_PENGENALAN.LineMacthed.ExtGetRight();
                        else if (m_labelMyKadMALAYSIA.IsLabelFound)
                            rightEdgeOfLeftAlignLabels = m_labelMyKadMALAYSIA.LineMacthed.ExtGetRight();
                        else if (m_labelMyKadIDENTITY_CARD.IsLabelFound)
                            rightEdgeOfLeftAlignLabels = m_labelMyKadIDENTITY_CARD.LineMacthed.ExtGetRight();

                        Line[] linesRightAlign = lsLineMergedNotLabel.Where(l => l.ExtGetLeft() > rightEdgeOfLeftAlignLabels).OrderBy(l => l.ExtGetTop()).ToArray();
                        if (linesRightAlign != null && linesRightAlign.Count() > 0)
                        {
                            Line[] mergedLinesNearToGender = linesRightAlign.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Gender - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToGender)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToGender: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_Gender)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is under the label
                                    if (m_labelMyKadKAD_PENGENALAN.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMyKadKAD_PENGENALAN.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (m_labelMyKadMALAYSIA.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMyKadMALAYSIA.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (m_labelMyKadIDENTITY_CARD.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMyKadIDENTITY_CARD.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (m_labelMyKadIDNUM.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMyKadIDNUM.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (lineCitizenship != null && (line.ExtGetVerticalCenter() <= lineCitizenship.ExtGetVerticalCenter()))
                                        continue;

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> GENDER");
                                    GENDER = line.Text;
                                    lineGender = line;
                                    break;
                                }
                            }

                            // remove lines right aligned from lsit of merged lines
                            // because other lines to find should not be right aligned
                            foreach (Line line in linesRightAlign)
                            {
                                lsLineMergedNotLabel.Remove(line);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex}");
                }

                try
                {
                    // filter lines near to line of name field
                    FindFromMergedLine(ref lsLineMergedNotLabel, centerYInInchOfField_Name,
                        ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                        new ValidateLine(l =>
                        {
                            // check if the line is under the label
                            // check if the line is under the label
                            if (m_labelMyKadKAD_PENGENALAN.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMyKadKAD_PENGENALAN.LineMacthed.ExtGetVerticalCenter()))
                                return false;
                            if (m_labelMyKadMALAYSIA.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMyKadMALAYSIA.LineMacthed.ExtGetVerticalCenter()))
                                return false;
                            if (m_labelMyKadIDENTITY_CARD.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMyKadIDENTITY_CARD.LineMacthed.ExtGetVerticalCenter()))
                                return false;
                            if (m_labelMyKadIDNUM.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelMyKadIDNUM.LineMacthed.ExtGetVerticalCenter()))
                                return false;

                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> NAME");
                            NAME = l.Text;
                            lineName = l;
                            return true;
                        }));
                    /*
                    Line[] mergedLinesNearToName = lsLineMergedNotLabel.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Name - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                    foreach (Line line in mergedLinesNearToName)
                    {
                        double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                        System.Diagnostics.Debug.WriteLine($"mergedLinesNearToName: {line.Text} EstimateCenterYInInch: {y}");
                        if (Math.Abs((decimal)(y - centerYInInchOfField_Name)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                        {
                            // check if the line is under the label
                            if (labelMyKadKAD_PENGENALAN.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadKAD_PENGENALAN.LineMacthed.ExtGetVerticalCenter()))
                                continue;
                            if (labelMyKadMALAYSIA.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadMALAYSIA.LineMacthed.ExtGetVerticalCenter()))
                                continue;
                            if (labelMyKadIDENTITY_CARD.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadIDENTITY_CARD.LineMacthed.ExtGetVerticalCenter()))
                                continue;
                            if (labelMyKadIDNUM.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadIDNUM.LineMacthed.ExtGetVerticalCenter()))
                                continue;

                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> NAME");
                            NAME = line.Text;
                            lineName = line;
                            lsLineMergedNotLabel.Remove(line);
                            break;
                        }
                    }
                    */
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex}");
                }

                if(lineName != null)
                {
                    try
                    {
                        // lines below name are lines of address
                        Line[] mergedLinesBelowName = lsLineMergedNotLabel.Where(l => l.ExtGetVerticalCenter() > lineName.ExtGetVerticalCenter()).OrderBy(l => l.ExtGetVerticalCenter()).ToArray();
                        foreach (Line line in mergedLinesBelowName)
                        {
                            if(lineAddress1 == null)
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS1");
                                lineAddress1 = line;
                                lsLineMergedNotLabel.Remove(line);
                                ADDRESS1 = lineAddress1.Text;
                                continue;
                            }
                            if (lineAddress2 == null)
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS2");
                                lineAddress2 = line;
                                lsLineMergedNotLabel.Remove(line);
                                ADDRESS2 = lineAddress2.Text;
                                continue;
                            }
                            if (lineAddress3 == null)
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS3");
                                lineAddress3 = line;
                                lsLineMergedNotLabel.Remove(line);
                                ADDRESS3 = lineAddress3.Text;
                                continue;
                            }
                            if (lineAddress4 == null)
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS4");
                                lineAddress4 = line;
                                lsLineMergedNotLabel.Remove(line);
                                ADDRESS4 = lineAddress4.Text;
                                continue;
                            }
                            if (lineAddress5 == null)
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS5");
                                lineAddress5 = line;
                                lsLineMergedNotLabel.Remove(line);
                                ADDRESS5 = lineAddress5.Text;
                                continue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }
                }

                try
                {
                    if (string.IsNullOrEmpty(ADDRESS1))
                    {
                        try
                        {
                            // filter lines near to line of address field
                            Line[] mergedLinesNearToAddress1 = lsLineMergedNotLabel.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address1 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToAddress1)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress1: {line.Text} EstimateCenterYInInch: {y}");
                                // check if the line is under the label
                                if (m_labelMyKadKAD_PENGENALAN.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMyKadKAD_PENGENALAN.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelMyKadMALAYSIA.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMyKadMALAYSIA.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelMyKadIDENTITY_CARD.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMyKadIDENTITY_CARD.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelMyKadIDNUM.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelMyKadIDNUM.LineMacthed.ExtGetVerticalCenter()))
                                    continue;


                                // address lines should be under address line 1
                                if (y < centerYInInchOfField_Address1 - ACCEPTABLE_DIFF_IN_LINE)
                                    continue;

                                if (string.IsNullOrEmpty(ADDRESS1))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS1");
                                    ADDRESS1 = line.Text;
                                    lineAddress1 = line;
                                    lsLineMergedNotLabel.Remove(line);
                                    continue;
                                }
                                else if (string.IsNullOrEmpty(ADDRESS2))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS2");
                                    ADDRESS2 = line.Text;
                                    lineAddress2 = line;
                                    lsLineMergedNotLabel.Remove(line);
                                    continue;
                                }
                                else if (string.IsNullOrEmpty(ADDRESS3))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS3");
                                    ADDRESS3 = line.Text;
                                    lineAddress3 = line;
                                    lsLineMergedNotLabel.Remove(line);
                                    continue;
                                }
                                else if (string.IsNullOrEmpty(ADDRESS4))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS4");
                                    ADDRESS4 = line.Text;
                                    lineAddress3 = line;
                                    lsLineMergedNotLabel.Remove(line);
                                    continue;
                                }
                                else if (string.IsNullOrEmpty(ADDRESS5))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS5");
                                    ADDRESS5 = line.Text;
                                    lineAddress3 = line;
                                    lsLineMergedNotLabel.Remove(line);
                                    continue;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{ex}");
                        }
                    }
#if false
                    if (lineAddress1 == null)
                    {
                        Line[] mergedLinesNearToAddress1 = lsLineMergedNotLabel.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address1 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress1)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress1: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address1)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the label
                                if (labelMyKadKAD_PENGENALAN.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadKAD_PENGENALAN.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (labelMyKadMALAYSIA.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadMALAYSIA.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (labelMyKadIDENTITY_CARD.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadIDENTITY_CARD.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (labelMyKadIDNUM.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadIDNUM.LineMacthed.ExtGetVerticalCenter()))
                                    continue;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS1");
                                lineAddress1 = line;
                                lsLineMergedNotLabel.Remove(line);
                                ADDRESS1 = lineAddress1.Text;
                                break;
                            }
                        }
                    }

                    if(lineAddress2 == null)
                    {
                        Line[] mergedLinesNearToAddress2 = lsLineMergedNotLabel.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address2 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress2)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress2: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address2)) <= (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the Addres1
                                if (lineAddress1 != null && (line.ExtGetVerticalCenter() <= lineAddress1.ExtGetVerticalCenter()))
                                    continue;

                                // the 2nd line of Address
                                if (IsFieldJustUnderTheLine(lineAddress1, line))
                                {
                                    // the 1st line of Address
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS2");
                                    lineAddress2 = line;
                                    lsLineMergedNotLabel.Remove(line);
                                    ADDRESS2 = lineAddress2.Text;
                                    break;
                                }
                            }
                        }
                    }

                    if(lineAddress3 == null)
                    {
                        Line[] mergedLinesNearToAddress3 = lsLineMergedNotLabel.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address3 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress3)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress3: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address3)) <= (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the Addres1
                                if (lineAddress1 != null && (line.ExtGetVerticalCenter() <= lineAddress1.ExtGetVerticalCenter()))
                                    continue;
                                if (lineAddress2 != null && (line.ExtGetVerticalCenter() <= lineAddress2.ExtGetVerticalCenter()))
                                    continue;

                                // the 2nd line of Address
                                if (IsFieldJustUnderTheLine(lineAddress2, line))
                                {
                                    // the 1st line of Address
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS3");
                                    lineAddress3 = line;
                                    lsLineMergedNotLabel.Remove(line);
                                    ADDRESS3 = lineAddress3.Text;
                                    break;
                                }
                            }
                        }
                    }

                    if(lineAddress4 == null)
                    {
                        Line[] mergedLinesNearToAddress4 = lsLineMergedNotLabel.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address4 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress4)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress4: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address4)) <= (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the Addres1
                                if (lineAddress1 != null && (line.ExtGetVerticalCenter() <= lineAddress1.ExtGetVerticalCenter()))
                                    continue;
                                if (lineAddress2 != null && (line.ExtGetVerticalCenter() <= lineAddress2.ExtGetVerticalCenter()))
                                    continue;
                                if (lineAddress3 != null && (line.ExtGetVerticalCenter() <= lineAddress3.ExtGetVerticalCenter()))
                                    continue;

                                if (IsFieldJustUnderTheLine(lineAddress3, line))
                                {
                                    // the 1st line of Address
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS4");
                                    lineAddress4 = line;
                                    lsLineMergedNotLabel.Remove(line);
                                    ADDRESS4 = lineAddress4.Text;
                                    break;
                                }
                            }
                        }
                    }

                    if(lineAddress5 == null)
                    {
                        Line[] mergedLinesNearToAddress5 = lsLineMergedNotLabel.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address5 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress5)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress5: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address5)) <= (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the Addres1
                                if (lineAddress1 != null && (line.ExtGetVerticalCenter() <= lineAddress1.ExtGetVerticalCenter()))
                                    continue;
                                if (lineAddress2 != null && (line.ExtGetVerticalCenter() <= lineAddress2.ExtGetVerticalCenter()))
                                    continue;
                                if (lineAddress3 != null && (line.ExtGetVerticalCenter() <= lineAddress3.ExtGetVerticalCenter()))
                                    continue;
                                if (lineAddress4 != null && (line.ExtGetVerticalCenter() <= lineAddress4.ExtGetVerticalCenter()))
                                    continue;

                                if (IsFieldJustUnderTheLine(lineAddress4, line))
                                {
                                    // the 1st line of Address
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS5");
                                    lineAddress5 = line;
                                    lsLineMergedNotLabel.Remove(line);
                                    ADDRESS5 = lineAddress5.Text;
                                    break;
                                }
                            }
                        }
                    }
#endif
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex}");
                }
            } // ppi is not null

            // map to result and convert format 
            List<string> lsMissingFields = new List<string>();

            // NAME -> lastNameOrFullName 
            if (string.IsNullOrEmpty(NAME))
            {
                lsMissingFields.Add("NAME");
            }
            else
            {
                result.lastNameOrFullName = NAME;
            }

            // IDNUM -> documentNumber
            if (string.IsNullOrEmpty(IDNUM))
            {
                lsMissingFields.Add("IDNUM");
            }
            else
            {
                result.documentNumber = IDNUM;
                // DOB is first 6 digit
                if(IDNUM.Length > 6)
                {
                    BIRTHDATE = IDNUM.Substring(0, 6);
                }
            }

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

            char[] separatorBlank = { ' ' };
            // ADDRESS1, ADDRESS2, ADDRESS3, STATE -> addressLine1, addressLine2
            // ADDRESS1, ADDRESS2, ADDRESS3, ADDRESS4, ADDRESS5 -> addressLine1, addressLine2, POSTCODE, CITY, STATE 
            if (string.IsNullOrEmpty(ADDRESS1))
            {
                lsMissingFields.Add("ADDRESS1");
            }
            else
            {
                if (string.IsNullOrEmpty(ADDRESS2))
                {
                    result.addressLine1 = ADDRESS1;
                    lsMissingFields.Add("POSTCODE");
                    lsMissingFields.Add("CITY");
                    lsMissingFields.Add("STATE");
                }
                else
                {
                    if (string.IsNullOrEmpty(ADDRESS3))
                    {
                        result.addressLine1 = ADDRESS1;
                        result.addressLine2 = ADDRESS2;
                        lsMissingFields.Add("POSTCODE");
                        lsMissingFields.Add("CITY");
                        lsMissingFields.Add("STATE");
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(ADDRESS4))
                        {
                            // ADDRESS1 --> AddressLine1
                            // ADDRESS2 --> POSTCODE, CITY
                            // ADDRESS3 --> STATE

                            // Extract POSTCODE CITY
                            string postcode_city = ADDRESS2;
                            string[] token = postcode_city.Split(separatorBlank, 2);
                            if (token.Length > 1)
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> POSTCODE: {token[0]} CITY: {token[1]}");
                                POSTCODE = token[0];
                                CITY = token[1];
                            }
                            else
                            {
                                CITY = postcode_city;
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> CITY: {CITY}");
                            }

                            STATE = ADDRESS3;
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> STATE: {STATE}");

                            result.addressLine1 = ADDRESS1;
                            result.addressLine2 = $"{CITY} {STATE}";
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(ADDRESS5))
                            {
                                // ADDRESS1 --> AddressLine1
                                // ADDRESS2 --> AddressLine2
                                // ADDRESS3 --> POSTCODE, CITY
                                // ADDRESS4 --> STATE

                                // Extract POSTCODE CITY
                                string postcode_city = ADDRESS3;
                                string[] token = postcode_city.Split(separatorBlank, 2);
                                if (token.Length > 1)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> POSTCODE: {token[0]} CITY: {token[1]}");
                                    POSTCODE = token[0];
                                    CITY = token[1];
                                }
                                else
                                {
                                    CITY = postcode_city;
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> CITY: {CITY}");
                                }

                                STATE = ADDRESS4;
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> STATE: {STATE}");

                                result.addressLine1 = $"{ADDRESS1}";
                                result.addressLine2 = $"{ADDRESS2} {CITY} {STATE}";
                            }
                            else
                            {
                                // ADDRESS1 --> AddressLine1
                                // ADDRESS2 --> AddressLine1
                                // ADDRESS3 --> AddressLine2
                                // ADDRESS4 --> POSTCODE, CITY
                                // ADDRESS5 --> STATE
                                // Extract POSTCODE CITY
                                string postcode_city = ADDRESS4;
                                string[] token = postcode_city.Split(separatorBlank, 2);
                                if (token.Length > 1)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> POSTCODE: {token[0]} CITY: {token[1]}");
                                    POSTCODE = token[0];
                                    CITY = token[1];
                                }
                                else
                                {
                                    CITY = postcode_city;
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> CITY: {CITY}");
                                }

                                STATE = ADDRESS5;
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> STATE: {STATE}");

                                result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                                result.addressLine2 = $"{ADDRESS3} {CITY} {STATE}";
                            }
                        }
                    }
                }
            }

            // POSTCODE
            if (string.IsNullOrEmpty(POSTCODE)) lsMissingFields.Add("POSTCODE");
            else result.postcode = POSTCODE;

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
#else
        public static ScanMyKadResult ExtractFieldsFromReadResultOfMyKad(IList<Line> mergedLines, SKImage imageSrc, ImgProcLib.MatchTemplateIDCard? matchTemplateMyKad)
        {
            const double ACCEPTABLE_DIFF_IN_LINE = 0.1f;

            LabelInfo[] labelsAboveFields = {
                labelMyKadKAD_PENGENALAN,
                labelMyKadMALAYSIA,
                labelMyKadIDENTITY_CARD
            };

            double centerYInInchOfField_IDNUM = 0.55f;
            double centerYInInchOfField_Name = 1.4f;
            double centerYInInchOfField_IDNUM_UnderFaceImage = 1.75f;
            double centerYInInchOfField_Citizenship = 1.85f;
            double centerYInInchOfField_Gender = 1.95f;
            double centerYInInchOfField_Address1 = 1.64f;
            double centerYInInchOfField_Address2 = 1.73f;
            double centerYInInchOfField_Address3 = 1.82f;
            double centerYInInchOfField_Address4 = 1.91f;
            double centerYInInchOfField_Address5 = 2.00f;

            /*
MatchTemplate key: MyKad_670_Flag MatchResult: 0.9028843641281128 x: 482 y: 16 w: 138 h: 75
MatchTemplate key: MyKad_670_Flag_Gray MatchResult: 0.9330259561538696 x: 481 y: 17 w: 138 h: 71
MatchTemplate key: MyKad_670_Flag_MoonStar MatchResult: 0.9079629182815552 x: 493 y: 24 w: 33 h: 25
MatchTemplate key: MyKad_670_Flag_MoonStar_Gray MatchResult: 0.9398182034492493 x: 494 y: 24 w: 30 h: 27
MatchTemplate key: MyKad_670_Flower_Front_Gray MatchResult: 0.8148484826087952 x: 275 y: 326 w: 122 h: 92
MatchTemplate key: MyKad_670_IC_Chip MatchResult: 0.5414834022521973 x: 32 y: 139 w: 118 h: 103
MatchTemplate key: MyKad_670_IC_Chip_Gray MatchResult: 0.6005121469497681 x: 40 y: 143 w: 102 h: 92
MatchTemplate key: MyKad_670_IC_Chip_old MatchResult: 0.33573946356773376 x: 36 y: 141 w: 108 h: 99
MatchTemplate key: MyKad_670_IC_Chip_old_Gray MatchResult: 0.2513715922832489 x: 221 y: 74 w: 102 h: 93
MatchTemplate key: MyKad_670_MSC_Gray MatchResult: 0.8906120657920837 x: 224 y: 109 w: 54 h: 54
MatchTemplate key: MyKad_670_MyKad MatchResult: 0.9330142736434937 x: 375 y: 8 w: 59 h: 39
MatchTemplate key: MyKad_670_MyKad_Gray MatchResult: 0.9301272630691528 x: 376 y: 9 w: 61 h: 39
             */
            Dictionary<string, MatchTemplateInfo> dicMatchTemplateInfo = new Dictionary<string, MatchTemplateInfo>();
            MatchTemplateInfo matchTmplMyKad_670_Flag = new MatchTemplateInfo("MyKad_670_Flag", "Flag", 0.8f, 2.9f, 0.25f);
            dicMatchTemplateInfo.Add(matchTmplMyKad_670_Flag.Name, matchTmplMyKad_670_Flag);
            //MatchTemplateInfo matchTmplMyKad_670_Flag_Gray = new MatchTemplateInfo("MyKad_670_Flag_Gray", 0.8f, 2.9f, 0.25f);
            //dicMatchTemplateInfo.Add(matchTmplMyKad_670_Flag_Gray.Title, matchTmplMyKad_670_Flag_Gray);
            MatchTemplateInfo matchTmplMyKad_670_Flag_MoonStar = new MatchTemplateInfo("MyKad_670_Flag_MoonStar", "Flag_MoonStar", 0.8f, 2.7f, 0.15f);
            dicMatchTemplateInfo.Add(matchTmplMyKad_670_Flag_MoonStar.Name, matchTmplMyKad_670_Flag_MoonStar);
            //MatchTemplateInfo matchTmplMyKad_670_Flag_MoonStar_Gray = new MatchTemplateInfo("MyKad_670_Flag_MoonStar_Gray", 0.8f, 2.7f, 0.15f);
            //dicMatchTemplateInfo.Add(matchTmplMyKad_670_Flag_MoonStar_Gray.Title, matchTmplMyKad_670_Flag_MoonStar_Gray);
            MatchTemplateInfo matchTmplMyKad_670_Flower_Front_Gray = new MatchTemplateInfo("MyKad_670_Flower_Front_Gray", "Flower_Watermark", 0.6f, 1.8f, 1.8f);
            dicMatchTemplateInfo.Add(matchTmplMyKad_670_Flower_Front_Gray.Name, matchTmplMyKad_670_Flower_Front_Gray);
            MatchTemplateInfo matchTmplMyKad_670_IC_Chip = new MatchTemplateInfo("MyKad_670_IC_Chip", "IC_Chip", 0.4f, 0.55f, 0.9f);
            dicMatchTemplateInfo.Add(matchTmplMyKad_670_IC_Chip.Name, matchTmplMyKad_670_IC_Chip);
            //MatchTemplateInfo matchTmplMyKad_670_IC_Chip_Gray = new MatchTemplateInfo("MyKad_670_IC_Chip_Gray", 0.4f, 0.55f, 0.9f);
            //dicMatchTemplateInfo.Add(matchTmplMyKad_670_IC_Chip_Gray.Title, matchTmplMyKad_670_IC_Chip_Gray);
            MatchTemplateInfo matchTmplMyKad_670_IC_Chip_old = new MatchTemplateInfo("MyKad_670_IC_Chip_old", "IC_Chip_Old", 0.4f, 0.55f, 0.9f);
            dicMatchTemplateInfo.Add(matchTmplMyKad_670_IC_Chip_old.Name, matchTmplMyKad_670_IC_Chip_old);
            //MatchTemplateInfo matchTmplMyKad_670_IC_Chip_old_Gray = new MatchTemplateInfo("MyKad_670_IC_Chip_old_Gray", 0.4f, 0.55f, 0.9f);
            //dicMatchTemplateInfo.Add(matchTmplMyKad_670_IC_Chip_old_Gray.Title, matchTmplMyKad_670_IC_Chip_old_Gray);
            MatchTemplateInfo matchTmplMyKad_670_MSC_Gray = new MatchTemplateInfo("MyKad_670_MSC_Gray", "MSC_Watermark", 0.8f, 1.35f, 0.7f);
            dicMatchTemplateInfo.Add(matchTmplMyKad_670_MSC_Gray.Name, matchTmplMyKad_670_MSC_Gray);
            MatchTemplateInfo matchTmplMyKad_670_MyKad = new MatchTemplateInfo("MyKad_670_MyKad", "MyKad_Logo_Top", 0.8f, 2.15f, 0.15f);
            dicMatchTemplateInfo.Add(matchTmplMyKad_670_MyKad.Name, matchTmplMyKad_670_MyKad);
            //MatchTemplateInfo matchTmplMyKad_670_MyKad_Gray = new MatchTemplateInfo("MyKad_670_MyKad_Gray", 0.8f, 2.15f, 0.15f);
            //dicMatchTemplateInfo.Add(matchTmplMyKad_670_MyKad_Gray.Title, matchTmplMyKad_670_MyKad_Gray);


            ScanMyKadResult result = new ScanMyKadResult();

            const double FILTER_TEXT_SMALLER_COMPARE_TO_IDNUM = 0.5f;
            int idxOf_KAD_PENGENALAN = -1;
            int idxOf_MALAYSIA = -1;
            int idxOf_IDENTITY_CARD = -1;
            string IDNUM = "";
            string NAME = "";
            string ADDRESS1 = "";
            string ADDRESS2 = "";
            string ADDRESS3 = "";
            string ADDRESS4 = "";
            string ADDRESS5 = "";
            string POSTCODE = "";
            string CITY = "";
            string STATE = "";
            string CITIZENSHIP = "";
            string GENDER = "";
            string EASTMSIAN = "";
            string BIRTHDATE = "";

            Line lineName = null;
            Line lineCitizenship = null;
            Line lineGender = null;
            Line lineAddress1 = null;
            Line lineAddress2 = null;
            Line lineAddress3 = null;
            Line lineAddress4 = null;
            Line lineAddress5 = null;

            //List<Line> linesField = new List<Line>();   // lines valid and not label
            List<LabelInfo> labelsFound = new List<LabelInfo>();

            List<Line> lsLineMerged = new List<Line>();

            foreach (Line line in mergedLines)
            {
                LabelInfo labelFound = FindLabelInMergedLine(line, labelsToFindMyKad.ToArray(), labelsAboveFields);
                if (labelFound != null)
                {
                    labelsFound.Add(labelFound);
                }
                else
                {
                    //linesField.Add(line);
                    lsLineMerged.Add(line);
                }
            }

            // calc pixel per inch
            double? ppi = LabelInfo.CalcPixelPerInch(labelsFound);
            if (ppi == null)
            {
                Console.WriteLine("CalcPixelPerInch failed");
            }
            else
            {
                //
                // calc top and left edge
                //
                double? topEdgeYOfIDImageInPixel;
                double? leftEdgeXOfIDImageInPixel;
                CalcLeftTopEdgeInPixel(labelsFound, ppi, out leftEdgeXOfIDImageInPixel, out topEdgeYOfIDImageInPixel);
                if (topEdgeYOfIDImageInPixel != null && leftEdgeXOfIDImageInPixel != null)
                {
                    int widthOfIDImageInPixel = (int)(3.35f * ppi.Value);
                    int heightOfIDImageInPixel = (int)(2.15f * ppi.Value);
                    SKRectI rect = new SKRectI(
                        (int)leftEdgeXOfIDImageInPixel - (int)(ppi * 0.1),
                        (int)topEdgeYOfIDImageInPixel - (int)(ppi * 0.1),
                        (int)leftEdgeXOfIDImageInPixel + widthOfIDImageInPixel + (int)(ppi * 0.1),
                        (int)topEdgeYOfIDImageInPixel + heightOfIDImageInPixel + (int)(ppi * 0.1));
                    if (rect.Top < 0) rect.Top = 0;
                    if (rect.Left < 0) rect.Left = 0;
                    if (rect.Right > imageSrc.Width) rect.Right = imageSrc.Width;
                    if (rect.Bottom > imageSrc.Height) rect.Bottom = imageSrc.Height;
                    SKImage imageIDSrc = imageSrc.Subset(rect);
                    //SKData dataIDPng = imageIDSrc.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                    double rate = 200.0f / ppi.Value;
                    SKBitmap bmpID = SKBitmap.FromImage(imageIDSrc);
                    SKBitmap bmpID200ppi = bmpID.Resize(new SKSizeI((int)(imageIDSrc.Width * rate), (int)(imageIDSrc.Height * rate)), SKFilterQuality.High);
                    SKData dataID200ppiPng = bmpID200ppi.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                    result.CardImage200ppiPngB64 = Convert.ToBase64String(dataID200ppiPng.ToArray());
                    //using (FileStream fs = new FileStream("ID.png", FileMode.Create))
                    //{
                    //    dataID200ppiPng.SaveTo(fs);
                    //}

                    if (matchTemplateMyKad != null)
                    {
                        MatchTemplateResult matchTemplateResult = matchTemplateMyKad.DoMatchTemplate(dataID200ppiPng.ToArray());
                        if (matchTemplateResult != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTemplateMyKadResult: {matchTemplateResult.MatchResult}");
                            SKImage imgID200ppi = SKImage.FromBitmap(bmpID200ppi);

                            // choose IC_Chip
                            if (matchTemplateResult.MatchResult.ContainsKey("MyKad_670_IC_Chip") && matchTemplateResult.MatchResult.ContainsKey("MyKad_670_IC_Chip_old"))
                            {
                                MatchTemplateResultItem matchTemplateResultItemMyKad_670_IC_Chip = matchTemplateResult.MatchResult["MyKad_670_IC_Chip"];
                                MatchTemplateResultItem matchTemplateResultItemMyKad_670_IC_Chip_old = matchTemplateResult.MatchResult["MyKad_670_IC_Chip_old"];
                                if (matchTemplateResultItemMyKad_670_IC_Chip.MatchResult < matchTemplateResultItemMyKad_670_IC_Chip_old.MatchResult)
                                {
                                    matchTemplateResult.MatchResult.Remove("MyKad_670_IC_Chip");
                                }
                                else
                                {
                                    matchTemplateResult.MatchResult.Remove("MyKad_670_IC_Chip_old");
                                }
                            }

                            foreach (string key in matchTemplateResult.MatchResult.Keys)
                            {
                                MatchTemplateResultItem matchTemplateResultItem = matchTemplateResult.MatchResult[key];
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] key: {key} MatchResult: {matchTemplateResultItem.MatchResult} x: {matchTemplateResultItem.LocX} y: {matchTemplateResultItem.LocY} w: {matchTemplateResultItem.Width} h: {matchTemplateResultItem.Height}");
                                if (dicMatchTemplateInfo.ContainsKey(key))
                                {
                                    MatchTemplateResultInfo matchTemplateResultInfo = new MatchTemplateResultInfo();
                                    matchTemplateResultInfo.Title = key;
                                    matchTemplateResultInfo.MatchTemplateInfo = dicMatchTemplateInfo[key];
                                    matchTemplateResultInfo.MatchTemplateInfo.MatchResult = matchTemplateResultItem.MatchResult;
                                    matchTemplateResultInfo.MatchTemplateInfo.ResultLocX = matchTemplateResultItem.LocX;
                                    matchTemplateResultInfo.MatchTemplateInfo.ResultLocY = matchTemplateResultItem.LocY;
                                    matchTemplateResultInfo.MatchTemplateInfo.ResultWidth = matchTemplateResultItem.Width;
                                    matchTemplateResultInfo.MatchTemplateInfo.ResultHeight = matchTemplateResultItem.Height;
                                    double? dist = matchTemplateResultInfo.GetDistanceFromExpectedCenterInInch();
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] key: {key} dist: {dist} ");
                                    result.MatchTemplateResults.Add(key, matchTemplateResultInfo);
                                }
                                /*
                                using (FileStream fs = new FileStream(matchTemplateMyKadResultItem.GetName() + ".png", FileMode.Create))
                                {
                                    SKRectI rectLandmark = new SKRectI((int)matchTemplateMyKadResultItem.LocX, (int)matchTemplateMyKadResultItem.LocY, (int)matchTemplateMyKadResultItem.LocX + matchTemplateMyKadResultItem.Width, (int)matchTemplateMyKadResultItem.LocY + matchTemplateMyKadResultItem.Height);
                                    SKImage imageLandmark = imgID200ppi.Subset(rectLandmark);
                                    SKData dataLandmark = imageLandmark.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                                    dataLandmark.SaveTo(fs);
                                }
                                */
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTemplateMyKad is null");
                    }
                }

                //
                // remove lines shorter than expected
                //
                const double labelHeightFilterInInch = 0.07f;   // text shorter than this height shuld be ignored
                double labelHeightFilterInPixel = (double)labelHeightFilterInInch * ppi.Value;
                //if (linesField.Count > 0)
                //{
                //    // remove lines predicted as label because of height
                //    int removedFromLinesField = linesField.RemoveAll(l => l.ExtGetHeight() < labelHeightFilterInPixel);
                //    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] removed {removedFromLinesField} lines predicted as label removed from linesField [height < labelHeightInPixel ({labelHeightFilterInPixel})].");
                //}
                if (lsLineMerged.Count > 0)
                {
                    // remove lines predicted as label because of height
                    int removedFromLinesMerged = lsLineMerged.RemoveAll(l => l.ExtGetHeight() < labelHeightFilterInPixel);
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] removed {removedFromLinesMerged} lines predicted as label removed from lsLineMerged [height < labelHeightInPixel ({labelHeightFilterInPixel})].");
                }

                // predit y in inch and expected field for each file line  
                //foreach (Line line in linesField)
                foreach (Line line in lsLineMerged)
                {
                    double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                    System.Diagnostics.Debug.WriteLine($"line: {line.Text} EstimateCenterYInInch: {y}");
                }

                try
                {
                    if (labelMyKadIDNUM.IsLabelFound)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {labelMyKadIDNUM.LineMacthed.Text} --> IDNUM");
                        IDNUM = labelMyKadIDNUM.LineMacthed.Text;
                    }
                    else if (labelMyKadIDNUM_UnderFaceImage.IsLabelFound)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {labelMyKadIDNUM_UnderFaceImage.LineMacthed.Text} --> IDNUM");
                        IDNUM = labelMyKadIDNUM_UnderFaceImage.LineMacthed.Text;
                    }
                    else
                    {
                        // filter lines near to lien of IDNUM field
                        Line linePossiblyIDNum = null;
                        Line[] mergedLinesNearToIDNUM = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_IDNUM - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToIDNUM)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToIDNUM: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_IDNUM)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the label
                                if (labelMyKadKAD_PENGENALAN.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadKAD_PENGENALAN.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (labelMyKadMALAYSIA.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadMALAYSIA.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (labelMyKadIDENTITY_CARD.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadIDENTITY_CARD.LineMacthed.ExtGetVerticalCenter()))
                                    continue;

                                if (!string.IsNullOrEmpty(line.Text))
                                {
                                    Match matchIDNum = regexMyKadIDNum.Match(line.Text);
                                    if (matchIDNum.Success)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {matchIDNum.Value} --> IDNUM");
                                        IDNUM = matchIDNum.Value;
                                        lsLineMerged.Remove(line);
                                        break;
                                    }

                                    string strNumeric = string.Concat(line.Text.Where(c => Char.IsDigit(c)));
                                    if (!string.IsNullOrEmpty(strNumeric))
                                    {
                                        Match matchIDNum2 = regexNum10DigitOrMore.Match(strNumeric);
                                        if (matchIDNum2.Success)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> possibly IDNUM");
                                            linePossiblyIDNum = line;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(IDNUM))
                        {
                            // try to read ID number under face image 

                            double? rightEdgeOfLeftAlignLabels = null;
                            if (labelMyKadKAD_PENGENALAN.IsLabelFound)
                                rightEdgeOfLeftAlignLabels = labelMyKadKAD_PENGENALAN.LineMacthed.ExtGetRight();
                            else if (labelMyKadMALAYSIA.IsLabelFound)
                                rightEdgeOfLeftAlignLabels = labelMyKadMALAYSIA.LineMacthed.ExtGetRight();
                            else if (labelMyKadIDENTITY_CARD.IsLabelFound)
                                rightEdgeOfLeftAlignLabels = labelMyKadIDENTITY_CARD.LineMacthed.ExtGetRight();

                            // filter lines near to line of IDNUM field
                            Line[] linesRightAlign = lsLineMerged.Where(l => l.ExtGetLeft() > rightEdgeOfLeftAlignLabels).OrderBy(l => l.ExtGetTop()).ToArray();
                            if (linesRightAlign != null && linesRightAlign.Count() > 0)
                            {
                                Line[] mergedLinesNearToIDNUMUnderFaceImage = linesRightAlign.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_IDNUM_UnderFaceImage - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                                foreach (Line line in mergedLinesNearToIDNUMUnderFaceImage)
                                {
                                    double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                    System.Diagnostics.Debug.WriteLine($"mergedLinesNearToIDNUMUnderFaceImage: {line.Text} EstimateCenterYInInch: {y}");
                                    if (Math.Abs((decimal)(y - centerYInInchOfField_IDNUM_UnderFaceImage)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                    {
                                        // check if the line is above the label
                                        if (labelMyKadWARGANEGARA.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadWARGANEGARA.LineMacthed.ExtGetVerticalCenter()))
                                            continue;
                                        if (labelMyKadPEREMPUAN.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadPEREMPUAN.LineMacthed.ExtGetVerticalCenter()))
                                            continue;
                                        if (labelMyKadLELAKI.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadLELAKI.LineMacthed.ExtGetVerticalCenter()))
                                            continue;

                                        Match matchIDNum = regexMyKadIDNum.Match(line.Text);
                                        if (matchIDNum.Success)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {matchIDNum.Value} --> IDNUM");
                                            IDNUM = matchIDNum.Value;
                                            lsLineMerged.Remove(line);
                                            if (linePossiblyIDNum != null)
                                            {
                                                lsLineMerged.Remove(linePossiblyIDNum);
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(IDNUM) && linePossiblyIDNum != null)
                        {
                            // take the line suspected to be IDNUM
                            IDNUM = linePossiblyIDNum.Text;
                            lsLineMerged.Remove(linePossiblyIDNum);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex}");
                }

                try
                {
                    if (labelMyKadWARGANEGARA.IsLabelFound)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {labelMyKadWARGANEGARA.LineMacthed.Text} --> CITIZENSHIP");
                        CITIZENSHIP = labelMyKadWARGANEGARA.LineMacthed.Text;
                    }
                    else
                    {
                        double? rightEdgeOfLeftAlignLabels = null;
                        if (labelMyKadKAD_PENGENALAN.IsLabelFound)
                            rightEdgeOfLeftAlignLabels = labelMyKadKAD_PENGENALAN.LineMacthed.ExtGetRight();
                        else if (labelMyKadMALAYSIA.IsLabelFound)
                            rightEdgeOfLeftAlignLabels = labelMyKadMALAYSIA.LineMacthed.ExtGetRight();
                        else if (labelMyKadIDENTITY_CARD.IsLabelFound)
                            rightEdgeOfLeftAlignLabels = labelMyKadIDENTITY_CARD.LineMacthed.ExtGetRight();

                        Line[] linesRightAlign = lsLineMerged.Where(l => l.ExtGetLeft() > rightEdgeOfLeftAlignLabels).OrderBy(l => l.ExtGetTop()).ToArray();
                        if (linesRightAlign != null && linesRightAlign.Count() > 0)
                        {
                            Line[] mergedLinesNearToCitizenship = linesRightAlign.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Citizenship - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToCitizenship)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToCitizenship: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_Citizenship)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is under the label
                                    if (labelMyKadKAD_PENGENALAN.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadKAD_PENGENALAN.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (labelMyKadMALAYSIA.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadMALAYSIA.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (labelMyKadIDENTITY_CARD.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadIDENTITY_CARD.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (labelMyKadIDNUM.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadIDNUM.LineMacthed.ExtGetVerticalCenter()))
                                        continue;

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> CITIZENSHIP");
                                    CITIZENSHIP = line.Text;
                                    lineCitizenship = line;
                                    break;
                                }
                            }

                            // remove lines right aligned from lsit of merged lines
                            // because other lines to find should not be right aligned
                            foreach (Line line in linesRightAlign)
                            {
                                lsLineMerged.Remove(line);
                            }
                        }
                    }

                    if (labelMyKadLELAKI.IsLabelFound)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {labelMyKadLELAKI.LineMacthed.Text} --> GENDER ({labelMyKadLELAKI.Title})");
                        GENDER = labelMyKadLELAKI.Title;
                    }
                    else if (labelMyKadPEREMPUAN.IsLabelFound)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {labelMyKadPEREMPUAN.LineMacthed.Text} --> GENDER ({labelMyKadPEREMPUAN.Title})");
                        GENDER = labelMyKadPEREMPUAN.Title;
                    }
                    else
                    {
                        double? rightEdgeOfLeftAlignLabels = null;
                        if (labelMyKadKAD_PENGENALAN.IsLabelFound)
                            rightEdgeOfLeftAlignLabels = labelMyKadKAD_PENGENALAN.LineMacthed.ExtGetRight();
                        else if (labelMyKadMALAYSIA.IsLabelFound)
                            rightEdgeOfLeftAlignLabels = labelMyKadMALAYSIA.LineMacthed.ExtGetRight();
                        else if (labelMyKadIDENTITY_CARD.IsLabelFound)
                            rightEdgeOfLeftAlignLabels = labelMyKadIDENTITY_CARD.LineMacthed.ExtGetRight();

                        Line[] linesRightAlign = lsLineMerged.Where(l => l.ExtGetLeft() > rightEdgeOfLeftAlignLabels).OrderBy(l => l.ExtGetTop()).ToArray();
                        if (linesRightAlign != null && linesRightAlign.Count() > 0)
                        {
                            Line[] mergedLinesNearToGender = linesRightAlign.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Gender - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToGender)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToGender: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_Gender)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is under the label
                                    if (labelMyKadKAD_PENGENALAN.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadKAD_PENGENALAN.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (labelMyKadMALAYSIA.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadMALAYSIA.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (labelMyKadIDENTITY_CARD.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadIDENTITY_CARD.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (labelMyKadIDNUM.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadIDNUM.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (lineCitizenship != null && (line.ExtGetVerticalCenter() <= lineCitizenship.ExtGetVerticalCenter()))
                                        continue;

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> GENDER");
                                    GENDER = line.Text;
                                    lineGender = line;
                                    break;
                                }
                            }

                            // remove lines right aligned from lsit of merged lines
                            // because other lines to find should not be right aligned
                            foreach (Line line in linesRightAlign)
                            {
                                lsLineMerged.Remove(line);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex}");
                }

                try
                {
                    // filter lines near to lien of name field
                    Line[] mergedLinesNearToName = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Name - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                    foreach (Line line in mergedLinesNearToName)
                    {
                        double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                        System.Diagnostics.Debug.WriteLine($"mergedLinesNearToName: {line.Text} EstimateCenterYInInch: {y}");
                        if (Math.Abs((decimal)(y - centerYInInchOfField_Name)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                        {
                            // check if the line is under the label
                            if (labelMyKadKAD_PENGENALAN.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadKAD_PENGENALAN.LineMacthed.ExtGetVerticalCenter()))
                                continue;
                            if (labelMyKadMALAYSIA.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadMALAYSIA.LineMacthed.ExtGetVerticalCenter()))
                                continue;
                            if (labelMyKadIDENTITY_CARD.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadIDENTITY_CARD.LineMacthed.ExtGetVerticalCenter()))
                                continue;
                            if (labelMyKadIDNUM.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadIDNUM.LineMacthed.ExtGetVerticalCenter()))
                                continue;

                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> NAME");
                            NAME = line.Text;
                            lineName = line;
                            lsLineMerged.Remove(line);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex}");
                }

                if (lineName != null)
                {
                    try
                    {
                        // lines below name are lines of address
                        Line[] mergedLinesBelowName = lsLineMerged.Where(l => l.ExtGetVerticalCenter() > lineName.ExtGetVerticalCenter()).OrderBy(l => l.ExtGetVerticalCenter()).ToArray();
                        foreach (Line line in mergedLinesBelowName)
                        {
                            if (lineAddress1 == null)
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS1");
                                lineAddress1 = line;
                                lsLineMerged.Remove(line);
                                ADDRESS1 = lineAddress1.Text;
                                continue;
                            }
                            if (lineAddress2 == null)
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS2");
                                lineAddress2 = line;
                                lsLineMerged.Remove(line);
                                ADDRESS2 = lineAddress2.Text;
                                continue;
                            }
                            if (lineAddress3 == null)
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS3");
                                lineAddress3 = line;
                                lsLineMerged.Remove(line);
                                ADDRESS3 = lineAddress3.Text;
                                continue;
                            }
                            if (lineAddress4 == null)
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS4");
                                lineAddress4 = line;
                                lsLineMerged.Remove(line);
                                ADDRESS4 = lineAddress4.Text;
                                continue;
                            }
                            if (lineAddress5 == null)
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS5");
                                lineAddress5 = line;
                                lsLineMerged.Remove(line);
                                ADDRESS5 = lineAddress5.Text;
                                continue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }
                }

                try
                {
                    if (lineAddress1 == null)
                    {
                        Line[] mergedLinesNearToAddress1 = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address1 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress1)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress1: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address1)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the label
                                if (labelMyKadKAD_PENGENALAN.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadKAD_PENGENALAN.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (labelMyKadMALAYSIA.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadMALAYSIA.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (labelMyKadIDENTITY_CARD.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadIDENTITY_CARD.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (labelMyKadIDNUM.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMyKadIDNUM.LineMacthed.ExtGetVerticalCenter()))
                                    continue;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS1");
                                lineAddress1 = line;
                                lsLineMerged.Remove(line);
                                ADDRESS1 = lineAddress1.Text;
                                break;
                            }
                        }
                    }

                    if (lineAddress2 == null)
                    {
                        Line[] mergedLinesNearToAddress2 = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address2 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress2)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress2: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address2)) <= (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the Addres1
                                if (lineAddress1 != null && (line.ExtGetVerticalCenter() <= lineAddress1.ExtGetVerticalCenter()))
                                    continue;

                                // the 2nd line of Address
                                if (IsFieldJustUnderTheLine(lineAddress1, line))
                                {
                                    // the 1st line of Address
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS2");
                                    lineAddress2 = line;
                                    lsLineMerged.Remove(line);
                                    ADDRESS2 = lineAddress2.Text;
                                    break;
                                }
                            }
                        }
                    }

                    if (lineAddress3 == null)
                    {
                        Line[] mergedLinesNearToAddress3 = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address3 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress3)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress3: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address3)) <= (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the Addres1
                                if (lineAddress1 != null && (line.ExtGetVerticalCenter() <= lineAddress1.ExtGetVerticalCenter()))
                                    continue;
                                if (lineAddress2 != null && (line.ExtGetVerticalCenter() <= lineAddress2.ExtGetVerticalCenter()))
                                    continue;

                                // the 2nd line of Address
                                if (IsFieldJustUnderTheLine(lineAddress2, line))
                                {
                                    // the 1st line of Address
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS3");
                                    lineAddress3 = line;
                                    lsLineMerged.Remove(line);
                                    ADDRESS3 = lineAddress3.Text;
                                    break;
                                }
                            }
                        }
                    }

                    if (lineAddress4 == null)
                    {
                        Line[] mergedLinesNearToAddress4 = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address4 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress4)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress4: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address4)) <= (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the Addres1
                                if (lineAddress1 != null && (line.ExtGetVerticalCenter() <= lineAddress1.ExtGetVerticalCenter()))
                                    continue;
                                if (lineAddress2 != null && (line.ExtGetVerticalCenter() <= lineAddress2.ExtGetVerticalCenter()))
                                    continue;
                                if (lineAddress3 != null && (line.ExtGetVerticalCenter() <= lineAddress3.ExtGetVerticalCenter()))
                                    continue;

                                if (IsFieldJustUnderTheLine(lineAddress3, line))
                                {
                                    // the 1st line of Address
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS4");
                                    lineAddress4 = line;
                                    lsLineMerged.Remove(line);
                                    ADDRESS4 = lineAddress4.Text;
                                    break;
                                }
                            }
                        }
                    }

                    if (lineAddress5 == null)
                    {
                        Line[] mergedLinesNearToAddress5 = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address5 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress5)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress5: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address5)) <= (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the Addres1
                                if (lineAddress1 != null && (line.ExtGetVerticalCenter() <= lineAddress1.ExtGetVerticalCenter()))
                                    continue;
                                if (lineAddress2 != null && (line.ExtGetVerticalCenter() <= lineAddress2.ExtGetVerticalCenter()))
                                    continue;
                                if (lineAddress3 != null && (line.ExtGetVerticalCenter() <= lineAddress3.ExtGetVerticalCenter()))
                                    continue;
                                if (lineAddress4 != null && (line.ExtGetVerticalCenter() <= lineAddress4.ExtGetVerticalCenter()))
                                    continue;

                                if (IsFieldJustUnderTheLine(lineAddress4, line))
                                {
                                    // the 1st line of Address
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS5");
                                    lineAddress5 = line;
                                    lsLineMerged.Remove(line);
                                    ADDRESS5 = lineAddress5.Text;
                                    break;
                                }
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex}");
                }
            } // ppi is not null

            // map to result and convert format 
            List<string> lsMissingFields = new List<string>();

            // NAME -> lastNameOrFullName 
            if (string.IsNullOrEmpty(NAME))
            {
                lsMissingFields.Add("NAME");
            }
            else
            {
                result.lastNameOrFullName = NAME;
            }

            // IDNUM -> documentNumber
            if (string.IsNullOrEmpty(IDNUM))
            {
                lsMissingFields.Add("IDNUM");
            }
            else
            {
                result.documentNumber = IDNUM;
                // DOB is first 6 digit
                if (IDNUM.Length > 6)
                {
                    BIRTHDATE = IDNUM.Substring(0, 6);
                }
            }

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

            char[] separatorBlank = { ' ' };
            // ADDRESS1, ADDRESS2, ADDRESS3, STATE -> addressLine1, addressLine2
            // ADDRESS1, ADDRESS2, ADDRESS3, ADDRESS4, ADDRESS5 -> addressLine1, addressLine2, POSTCODE, CITY, STATE 
            if (string.IsNullOrEmpty(ADDRESS1))
            {
                lsMissingFields.Add("ADDRESS1");
            }
            else
            {
                if (string.IsNullOrEmpty(ADDRESS2))
                {
                    result.addressLine1 = ADDRESS1;
                    lsMissingFields.Add("POSTCODE");
                    lsMissingFields.Add("CITY");
                    lsMissingFields.Add("STATE");
                }
                else
                {
                    if (string.IsNullOrEmpty(ADDRESS3))
                    {
                        result.addressLine1 = ADDRESS1;
                        result.addressLine2 = ADDRESS2;
                        lsMissingFields.Add("POSTCODE");
                        lsMissingFields.Add("CITY");
                        lsMissingFields.Add("STATE");
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(ADDRESS4))
                        {
                            // ADDRESS1 --> AddressLine1
                            // ADDRESS2 --> POSTCODE, CITY
                            // ADDRESS3 --> STATE

                            // Extract POSTCODE CITY
                            string postcode_city = ADDRESS2;
                            string[] token = postcode_city.Split(separatorBlank, 2);
                            if (token.Length > 1)
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> POSTCODE: {token[0]} CITY: {token[1]}");
                                POSTCODE = token[0];
                                CITY = token[1];
                            }
                            else
                            {
                                CITY = postcode_city;
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> CITY: {CITY}");
                            }

                            STATE = ADDRESS3;
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> STATE: {STATE}");

                            result.addressLine1 = ADDRESS1;
                            result.addressLine2 = $"{CITY} {STATE}";
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(ADDRESS5))
                            {
                                // ADDRESS1 --> AddressLine1
                                // ADDRESS2 --> AddressLine2
                                // ADDRESS3 --> POSTCODE, CITY
                                // ADDRESS4 --> STATE

                                // Extract POSTCODE CITY
                                string postcode_city = ADDRESS3;
                                string[] token = postcode_city.Split(separatorBlank, 2);
                                if (token.Length > 1)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> POSTCODE: {token[0]} CITY: {token[1]}");
                                    POSTCODE = token[0];
                                    CITY = token[1];
                                }
                                else
                                {
                                    CITY = postcode_city;
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> CITY: {CITY}");
                                }

                                STATE = ADDRESS4;
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> STATE: {STATE}");

                                result.addressLine1 = $"{ADDRESS1}";
                                result.addressLine2 = $"{ADDRESS2} {CITY} {STATE}";
                            }
                            else
                            {
                                // ADDRESS1 --> AddressLine1
                                // ADDRESS2 --> AddressLine1
                                // ADDRESS3 --> AddressLine2
                                // ADDRESS4 --> POSTCODE, CITY
                                // ADDRESS5 --> STATE
                                // Extract POSTCODE CITY
                                string postcode_city = ADDRESS4;
                                string[] token = postcode_city.Split(separatorBlank, 2);
                                if (token.Length > 1)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> POSTCODE: {token[0]} CITY: {token[1]}");
                                    POSTCODE = token[0];
                                    CITY = token[1];
                                }
                                else
                                {
                                    CITY = postcode_city;
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> CITY: {CITY}");
                                }

                                STATE = ADDRESS5;
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> STATE: {STATE}");

                                result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                                result.addressLine2 = $"{ADDRESS3} {CITY} {STATE}";
                            }
                        }
                    }
                }
            }

            // POSTCODE
            if (string.IsNullOrEmpty(POSTCODE)) lsMissingFields.Add("POSTCODE");
            else result.postcode = POSTCODE;

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
#endif
        class GroupOfLine
        {
            public GroupOfLine() { }
            public GroupOfLine(List<Line> lines) { Lines = lines; }

            public List<Line> Lines { get; } = new List<Line>();
            public double? Left
            {
                get
                {
                    return Lines.Select(l => l.ExtGetLeft()).Min();
                }
            }
            public double? Right
            {
                get
                {
                    return Lines.Select(l => l.ExtGetRight()).Max();
                }
            }
            public double? AvgLeft
            {
                get
                {
                    return Lines.Select(l => l.ExtGetLeft()).Average();
                }
            }
            public double? AvgTop
            {
                get
                {
                    return Lines.Select(l => l.ExtGetTop()).Average();
                }
            }
            public double? AvgRight
            {
                get
                {
                    return Lines.Select(l => l.ExtGetRight()).Average();
                }
            }
            public double? AvgBottom
            {
                get
                {
                    return Lines.Select(l => l.ExtGetBottom()).Average();
                }
            }
            public double? AvgHeight
            {
                get
                {
                    return Lines.Select(l => l.ExtGetHeight()).Average();
                }
            }
            public double? AvgWidth
            {
                get
                {
                    return Lines.Select(l => l.ExtGetWidth()).Average();
                }
            }
            public double? AvgBaselineSlope
            {
                get
                {
                    return Lines.Select(l => l.ExtGetBaselineSlope()).Average();
                }
            }
            public double? AvgInterceptWithYAxis
            {
                get
                {
                    return Lines.Select(l => l.ExtGetBaselineInterceptWithYAxis()).Average();
                }
            }
        }

        public IList<Line> MergeLinesInSameYPosIntoOneLine(IList<Line> linesAll, double? holizonalGapAllowedPerLineHeight = null)
        {
            List<GroupOfLine> lsLineGroupOnTheSameLine = new List<GroupOfLine>();
            List<Line> linesInTheSameLine = new List<Line>();
            //List<Line> linesSorted = linesAll.OrderBy(l => l.ExtGetBottom()).ToList();
            List<Line> linesSorted = linesAll.ToList();
            //double? prevBottom = null;
            //double? prevTop = null;
            GroupOfLine curGroup = null;
            foreach (Line line in linesSorted)
            {
                if (curGroup == null)
                {
                    //prevBottom = line.ExtGetBottom();
                    //prevTop = line.ExtGetTop();
                    curGroup = new GroupOfLine(new List<Line>() { line });
                    lsLineGroupOnTheSameLine.Add(curGroup);
                }
                else if (line.ExtGetBottom() != null && line.ExtGetTop() != null && line.ExtGetHeight() != null)
                {
                    double gapAllowedVertically = (double)(line.ExtGetHeight() / 3);
                    // double gapAllowedHorizontally = (double)(line.ExtGetHeight() * 3);
                    double gapAllowedHorizontally = (double)((holizonalGapAllowedPerLineHeight.HasValue)
                        ? (holizonalGapAllowedPerLineHeight.Value * line.ExtGetHeight()) : double.MaxValue);
                    double? curGroupAvgSlope = curGroup.AvgBaselineSlope;
                    double? curGroupAvgInterceptWithYAxis = curGroup.AvgInterceptWithYAxis;
                    bool isOnSameLine = false;
                    if(curGroupAvgSlope != null && curGroupAvgInterceptWithYAxis != null && line.ExtGetBaselineSlope() != null && line.ExtGetBaselineInterceptWithYAxis() != null)
                    {
                        // to-do: handle the case the lines are overlap

                        double? slopeLine = line.ExtGetBaselineSlope();
                        double? interceptWithYAxisLine = line.ExtGetBaselineInterceptWithYAxis();
                        if (Math.Abs((decimal)(curGroupAvgSlope - slopeLine)) < (decimal)0.5 
                        && Math.Abs((decimal)(curGroupAvgInterceptWithYAxis - interceptWithYAxisLine)) < (decimal)gapAllowedVertically
                        && curGroup.AvgBottom > line.ExtGetTop() && curGroup.AvgTop < line.ExtGetBottom() // check if the line is overlap vertically
                        && ((curGroup.Left > line.ExtGetLeft() && curGroup.Left - line.ExtGetRight() < gapAllowedHorizontally) // at left side 
                            || (line.ExtGetRight() > curGroup.Right && line.ExtGetLeft() - curGroup.Right < gapAllowedHorizontally)  // at right side
                            || curGroup.Left < line.ExtGetLeft() && curGroup.Right > line.ExtGetRight()) // inside 
                        )
                        {
                            isOnSameLine = true;
                        }
                    }

                    if(!isOnSameLine)
                    {
                        if (Math.Abs((decimal)(line.ExtGetBottom() - curGroup.AvgBottom)) < (decimal)gapAllowedVertically
                            && Math.Abs((decimal)(line.ExtGetTop() - curGroup.AvgTop)) < (decimal)gapAllowedVertically
                            && ((curGroup.Left > line.ExtGetLeft() && curGroup.Left - line.ExtGetRight() < gapAllowedHorizontally) // at left side 
                                || (line.ExtGetRight() > curGroup.Right && line.ExtGetLeft() - curGroup.Right < gapAllowedHorizontally)  // at right side
                                || curGroup.Left < line.ExtGetLeft() && curGroup.Right > line.ExtGetRight()) // inside 
                                )
                        {
                            isOnSameLine = true;
                        }
                    }

                    if (isOnSameLine)
                    {
                        curGroup.Lines.Add(line);
                        /*
                        foreach (GroupOfLine groupOfLine in lsLineGroupOnTheSameLine)
                        {
                            if(groupOfLine.AvgTop != null && groupOfLine.AvgBottom != null)
                            {
                                if ((Math.Abs((decimal)(groupOfLine.AvgTop - line.ExtGetTop())) < (decimal)gapAllowed)
                                 && (Math.Abs((decimal)(groupOfLine.AvgBottom - line.ExtGetBottom())) < (decimal)gapAllowed)
                                    )
                                {
                                    groupOfLine.Lines.Add(line);
                                    break;
                                }
                            }
                        }
                        */
                    }
                    else
                    {
                        curGroup = new GroupOfLine(new List<Line>() { line });
                        lsLineGroupOnTheSameLine.Add(curGroup);
                    }
                }
            }

            foreach (GroupOfLine groupOfLine in lsLineGroupOnTheSameLine)
            {
                //List<Line> lsInTheSameLine = dictLinesInTheSameLine[bottom];
                List<Line> linesSortedFromLeftToRight = groupOfLine.Lines.OrderBy(l => l.ExtGetLeft()).ToList();
                Line lineConcat = null;
                foreach (Line l in linesSortedFromLeftToRight)
                {
                    if (lineConcat == null)
                    {
                        lineConcat = l;
                    }
                    else
                    {
                        try
                        {
                            lineConcat = lineConcat.MergedLine(l);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] Exception {ex}");
                        }
                    }
                }
                if (lineConcat != null)
                {
                    linesInTheSameLine.Add(lineConcat);
                }
            }

            return linesInTheSameLine;
        }

        bool IsLineInTheSameLine(Line line1, Line line2, double? holizonalGapAllowedPerLineHeight = null)
        {
            double? slopeLine1 = line1.ExtGetBaselineSlope();
            double? interceptWithYAxisLine1 = line1.ExtGetBaselineInterceptWithYAxis();
            double? slopeLine2 = line2.ExtGetBaselineSlope();
            double? interceptWithYAxisLine2 = line2.ExtGetBaselineInterceptWithYAxis();
            double? avgHeight = (line1.ExtGetHeight() + line2.ExtGetHeight()) / 2;
            double gapAllowedVertically = (double)(avgHeight / 3);
            double gapAllowedHorizontally = (double)((holizonalGapAllowedPerLineHeight.HasValue)
                ? (holizonalGapAllowedPerLineHeight.Value * avgHeight) : double.MaxValue);

            if(slopeLine1 != null && slopeLine2 != null && interceptWithYAxisLine1 != null && interceptWithYAxisLine2 != null)
            {
                if (Math.Abs((decimal)(slopeLine1 - slopeLine2)) < (decimal)0.5
                && Math.Abs((decimal)(interceptWithYAxisLine1 - interceptWithYAxisLine2)) < (decimal)gapAllowedVertically
                && ((line1.ExtGetRight() > line2.ExtGetRight() && line1.ExtGetLeft() - line1.ExtGetRight() < gapAllowedHorizontally) // at left side 
                    || (line2.ExtGetLeft() > line1.ExtGetRight() && line2.ExtGetLeft() - line1.ExtGetRight() < gapAllowedHorizontally)  // at right side
                    || line1.ExtGetLeft() < line2.ExtGetLeft() && line1.ExtGetRight() > line2.ExtGetRight()) // inside
                )
                {
                    return true;
                }
            }

            if (Math.Abs((decimal)(line2.ExtGetBottom() - line1.ExtGetBottom())) < (decimal)gapAllowedVertically
                && Math.Abs((decimal)(line2.ExtGetTop() - line2.ExtGetTop())) < (decimal)gapAllowedVertically
            && ((line1.ExtGetLeft() < line2.ExtGetRight() && line1.ExtGetLeft() - line2.ExtGetRight() < gapAllowedHorizontally) // at left side 
                || (line2.ExtGetLeft() < line1.ExtGetRight() && line2.ExtGetLeft() - line1.ExtGetRight() < gapAllowedHorizontally)  // at right side
                || line1.ExtGetLeft() < line2.ExtGetLeft() && line1.ExtGetRight() < line2.ExtGetLeft()  // overlapped
                || line2.ExtGetLeft() < line1.ExtGetLeft() && line2.ExtGetRight() < line1.ExtGetLeft()  // overlapped
                ) 
            )
            {
                return true;
            }

            return false;
        }

        Line[] FindLabelInLine(ref List<LabelInfo> labelsFound, Line[] arLinesInSameLine, Line lineMergedToCheck, string docType)
        {
            bool isLabelFound = false;
            // check if the merged line is a label or not
            LabelInfo[] labelsToFind = GetLabelsToFind(docType);
            LabelInfo[] labelsAboveFields = GetLabelsAboveFields(docType);
            foreach (LabelInfo labelToFind in labelsToFind)
            {
                if (!labelToFind.IsLabelFound)
                {
                    if (labelToFind.MatchTitle(lineMergedToCheck))
                    {
                        if (labelToFind.CenterYFromTopEdgeInInch != null)
                        {
                            labelsFound.Add(labelToFind);
                            isLabelFound = true;
                            break;
                        }
                    }
                }
            }
            if (isLabelFound)
                return null;

            List<Line> linesToAddFields = new List<Line>();
            // if merged line is not match any label, then check each line
            if(arLinesInSameLine != null)
            {
                foreach (Line aLine in arLinesInSameLine)
                {
                    // check if the merged line is a label or not
                    foreach (LabelInfo labelToFind in labelsToFind)
                    {
                        // check if label and its children are not found yet
                        bool bFound = labelToFind.IsLabelFound;
                        if (!bFound)
                        {
                            if (labelToFind.Childs.Count > 0)
                            {
                                foreach (LabelInfo child in labelToFind.Childs)
                                {
                                    if (child.IsLabelFound)
                                    {
                                        bFound = true;
                                        break;
                                    }
                                }
                            }
                        }

                        if (!bFound)
                        {
                            if (labelToFind.MatchTitle(lineMergedToCheck))
                            {
                                if (labelToFind.CenterYFromTopEdgeInInch != null)
                                {
                                    labelsFound.Add(labelToFind);
                                }
                                isLabelFound = true;
                                break;
                            }
                            if (labelToFind.Childs.Count > 0)
                            {
                                foreach (LabelInfo child in labelToFind.Childs)
                                {
                                    if (labelToFind.MatchTitle(lineMergedToCheck))
                                    {
                                        if (labelToFind.CenterYFromTopEdgeInInch != null)
                                        {
                                            labelsFound.Add(labelToFind);
                                        }
                                        isLabelFound = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (isLabelFound)
                            break;
                    } // foreach labelsToFind

                    // filter lines above header titles
                    bool isLineAboveHeaderTitles = false;
                    foreach (LabelInfo labelAboveFields in labelsAboveFields)
                    {
                        if (IsLineAboveOrSmallerThanLabel(aLine, labelAboveFields))
                        {
                            isLineAboveHeaderTitles = true;
                            break;
                        }
                    }
                    if ((isLineAboveHeaderTitles))
                        continue;

                    linesToAddFields.Add(aLine);
                } // foreach linesInSameLine
            }
            return linesToAddFields.ToArray();
        }

        Line[] FindLabelInLineAbove(ref List<LabelInfo> labelsFound, Line[] arLinesInSameLine, Line lineMergedToCheck, string docType)
        {
            bool isLabelFound = false;
            LabelInfo[] labelsToFind = GetLabelsToFind(docType);
            LabelInfo[] labelsBelowFields = GetLabelsFooterFields(docType);
            // check if the merged line is a label or not
            foreach (LabelInfo labelToFind in labelsToFind)
            {
                if (!labelToFind.IsLabelFound)
                {
                    if (labelToFind.MatchTitle(lineMergedToCheck))
                    {
                        if (labelToFind.CenterYFromTopEdgeInInch != null)
                        {
                            labelsFound.Add(labelToFind);
                            isLabelFound = true;
                            break;
                        }
                    }
                }
            }
            if (isLabelFound)
                return null;

            List<Line> linesToAddFields = new List<Line>();
            // if merged line is not match any label, then check each line
            if (arLinesInSameLine != null)
            {
                foreach (Line aLine in arLinesInSameLine)
                {
                    // check if the merged line is a label or not
                    foreach (LabelInfo labelToFind in labelsToFind)
                    {
                        // check if label and its children are not found yet
                        bool bFound = labelToFind.IsLabelFound;
                        if (!bFound)
                        {
                            if (labelToFind.Childs.Count > 0)
                            {
                                foreach (LabelInfo child in labelToFind.Childs)
                                {
                                    if (child.IsLabelFound)
                                    {
                                        bFound = true;
                                        break;
                                    }
                                }
                            }
                        }

                        if (!bFound)
                        {
                            if (labelToFind.MatchTitle(lineMergedToCheck))
                            {
                                if (labelToFind.CenterYFromTopEdgeInInch != null)
                                {
                                    labelsFound.Add(labelToFind);
                                }
                                isLabelFound = true;
                                break;
                            }
                            if (labelToFind.Childs.Count > 0)
                            {
                                foreach (LabelInfo child in labelToFind.Childs)
                                {
                                    if (labelToFind.MatchTitle(lineMergedToCheck))
                                    {
                                        if (labelToFind.CenterYFromTopEdgeInInch != null)
                                        {
                                            labelsFound.Add(labelToFind);
                                        }
                                        isLabelFound = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (isLabelFound)
                            break;
                    } // foreach labelsToFind

                    // filter lines below footer
                    bool isLineBelowFooter = false;
                    foreach (LabelInfo labelBelowFields in labelsBelowFields)
                    {
                        if (IsLineBelowOrSmallerThanLabel(aLine, labelBelowFields))
                        {
                            isLineBelowFooter = true;
                            break;
                        }
                    }
                    if ((isLineBelowFooter))
                        continue;

                    linesToAddFields.Add(aLine);
                } // foreach linesInSameLine
            }
            return linesToAddFields.ToArray();
        }

        LabelInfo FindLabelInMergedLine(Line lineMergedToCheck, string docType)
        {
            LabelInfo[] labelsToFind = GetLabelsToFind(docType);
            LabelInfo[] labelsAboveFields = GetLabelsAboveFields(docType);

            // check if the merged line is a label or not
            foreach (LabelInfo labelToFind in labelsToFind)
            {
                if (!labelToFind.IsLabelFound)
                {
                    if (labelToFind.MatchTitle(lineMergedToCheck))
                    {
                        if (labelToFind.CenterYFromTopEdgeInInch != null)
                        {
                            return labelToFind;
                        }
                    }
                }
            }
            return null;
        }

        delegate bool ValidateLine(Line line);

        /// <summary>
        /// Find label from list of marged line. 
        /// If a line expected to be label is found, ValidateLine delegate is called.
        /// If ValidateLine delegate return false, continue search in list.
        /// If ValidateLine delegate return true, the label marked as found, and the line removed from lsLineMerged.
        /// </summary>
        /// <param name="lsLineMerged">list of marged line</param>
        /// <param name="centerYInInchOfField">Vertical position of label in inch</param>
        /// <param name="ACCEPTABLE_DIFF_IN_LINE">Acceptable difference between the label and a found line</param>
        /// <param name="ppi">pixel per inch of image</param>
        /// <param name="topEdgeYOfIDImageInPixel"></param>
        /// <param name="validateLine">Delegate to validate the line meets condition of the label</param>
        /// <returns></returns>
        bool FindFromMergedLine(
            ref List<Line> lsLineMerged,  double centerYInInchOfField, double ACCEPTABLE_DIFF_IN_LINE, 
            double? ppi, double? topEdgeYOfIDImageInPixel, ValidateLine validateLine)
        {
            Line[] mergedLinesNearTarget = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
            bool isValidLineFound = false;
            foreach (Line line in mergedLinesNearTarget)
            {
                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                //System.Diagnostics.Debug.WriteLine($"mergedLinesNearTarget: {line.Text} EstimateCenterYInInch: {y}");
                if (Math.Abs((decimal)(y - centerYInInchOfField)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                {
                    if (validateLine(line))
                    {
                        lsLineMerged.Remove(line);
                        isValidLineFound = true;
                        break;
                    }
                }
            }
            return isValidLineFound;
        }

#if true
        //public static ScanPHUMIDResult ExtractFieldsFromReadResultOfPHUMID(IList<Line> linesAll, List<LabeledObject> labeledObjects, SKImage imageSrc, ImgProcLib.MatchTemplateIDCard? matchTemplatePHUMID)
        ScanPHUMIDResult ExtractFieldsFromReadResultOfPHUMID(LabelInfo[] labelsFound, Line[] linesMergedNotLabel, SKRectI? rcFace, SKImage imageSrc, ImgProcLib.MatchTemplateIDCard? matchTemplatePHUMID)
        {
            // For UMID with face picture located in the right side
            const double CENTER_Y_INCH_CRN_FACE_R = 0.62f;
            const double CENTER_Y_INCH_SURNAME_FACE_R = 0.74f;
            const double CENTER_Y_INCH_GIVENNAME_FACE_R = 0.96f;
            const double CENTER_Y_INCH_MIDDLENAME_FACE_R = 1.18f;
            const double CENTER_Y_INCH_SEX_FACE_R = 1.40f;
            const double CENTER_Y_INCH_DOB_FACE_R = 1.52f;
            const double CENTER_Y_INCH_ADDRESS1_FACE_R = 1.74f;
            const double CENTER_Y_INCH_ADDRESS2_FACE_R = 1.84f;
            const double CENTER_Y_INCH_ADDRESS3_FACE_R = 1.93f;
            const double CENTER_Y_INCH_ADDRESS4_FACE_R = 2.03f;

            const double CENTER_Y_INCH_CRN_FACE_L = 0.63f;
            const double CENTER_Y_INCH_SURNAME_FACE_L = 0.92f;
            const double CENTER_Y_INCH_GIVENNAME_FACE_L = 1.12f;
            const double CENTER_Y_INCH_MIDDLENAME_FACE_L = 1.42f;
            const double CENTER_Y_INCH_SEX_DOB_FACE_L = 1.54f;
            const double CENTER_Y_INCH_ADDRESS1_FACE_L = 1.68f;
            const double CENTER_Y_INCH_ADDRESS2_FACE_L = 1.76f;
            const double CENTER_Y_INCH_ADDRESS3_FACE_L = 1.83f;
            const double CENTER_Y_INCH_ADDRESS4_FACE_L = 1.92f;

            Dictionary<string, MatchTemplateInfo> dicMatchTemplateInfo = new Dictionary<string, MatchTemplateInfo>();
            MatchTemplateInfo matchTmplPHUMID_670_LogoL_Gray = new MatchTemplateInfo("PHUMID_670_LogoL_Gray", "COA", 0.8f, 0.3f, 0.25f);
            dicMatchTemplateInfo.Add(matchTmplPHUMID_670_LogoL_Gray.Name, matchTmplPHUMID_670_LogoL_Gray);
            MatchTemplateInfo matchTmplPHUMID_670_LogoR_Gray = new MatchTemplateInfo("PHUMID_670_LogoR_Gray", "Flag", 0.8f, 1.75f, 0.25f);
            dicMatchTemplateInfo.Add(matchTmplPHUMID_670_LogoR_Gray.Name, matchTmplPHUMID_670_LogoR_Gray);

            ScanPHUMIDResult result = new ScanPHUMIDResult();

            string CRN = "";
            string SURNAME = "";
            string GIVEN_NAME = "";
            string GIVEN_NAME2 = "";
            string MIDDLE_NAME = "";
            string SEX = "";
            string DOB = "";
            Line lineSexDoB = null;
            string ADDRESS1 = "";
            Line lineAddress1 = null;
            string ADDRESS2 = "";
            Line lineAddress2 = null;
            string ADDRESS3 = "";
            Line lineAddress3 = null;
            string ADDRESS4 = "";
            Line lineAddress4 = null;
            string POSTCODE = "";
            const double ACCEPTABLE_DIFF_IN_LINE = 0.1f;

            List<Line> lsLineMergedNotLabel = new List<Line>();
            lsLineMergedNotLabel.AddRange(linesMergedNotLabel);

            // calc pixel per inch
            double? ppi = LabelInfo.CalcPixelPerInch(labelsFound);
            if (ppi == null)
            {
                Console.WriteLine("CalcPixelPerInch failed");
            }
            else
            {
                const double labelHeightFilterInInch = 0.08f;
                double labelHeightFilterInPixel = (double)labelHeightFilterInInch * ppi.Value;

                //
                // calc top and left edge
                //
                double? topEdgeYOfIDImageInPixel;
                double? leftEdgeXOfIDImageInPixel;
                CalcLeftTopEdgeInPixel(labelsFound, ppi, out leftEdgeXOfIDImageInPixel, out topEdgeYOfIDImageInPixel);
                if (topEdgeYOfIDImageInPixel != null && leftEdgeXOfIDImageInPixel != null)
                {
                    if (topEdgeYOfIDImageInPixel != null && leftEdgeXOfIDImageInPixel != null)
                    {
                        MatchTemplateResult matchTemplateResult = null;
                        SKData dataID200ppiPng = null;
                        DateTime dtStart = DateTime.Now;
                        bool bRetMatchTemplate = DoMatchTemplate(matchTemplatePHUMID, imageSrc, topEdgeYOfIDImageInPixel.Value, leftEdgeXOfIDImageInPixel.Value, ppi.Value,
                            out matchTemplateResult, out dataID200ppiPng);
                        DateTime dtEnd = DateTime.Now;
                        result.timeElapsedLandmarkDetection = (int)(dtEnd - dtStart).TotalMilliseconds;
                        result.CardImage200ppiPngB64 = Convert.ToBase64String(dataID200ppiPng.ToArray());
                        if (bRetMatchTemplate)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTemplateMyKadResult: {matchTemplateResult.MatchResult}");
                            SKImage imgID200ppi = SKImage.FromEncodedData(dataID200ppiPng);
                            GenerateMatchTemplateResults(matchTemplateResult, dicMatchTemplateInfo, result.MatchTemplateResults);
                            result.landmarkImageBase64 = GenerateMatchTemplateResultImage(result.MatchTemplateResults, result.CardImage200ppiPngB64);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTemplate failed");
                        }
                    }
                }

                /*
                List<double> lsTopEdgeYOfIDImageInPixelCalculated = new List<double>();
                foreach (LabelInfo label in labelsFound)
                {
                    double? topEdgeYOfIDImageInPixelCalculated = label.CalcTopEdgeYOfIDImageInPixel(ppi.Value);
                    if (topEdgeYOfIDImageInPixelCalculated != null)
                    {
                        lsTopEdgeYOfIDImageInPixelCalculated.Add(topEdgeYOfIDImageInPixelCalculated.Value);
                        double? y = label.PredictCenterYInPixel(ppi, topEdgeYOfIDImageInPixelCalculated);
                        System.Diagnostics.Debug.WriteLine($"{label.Title} PredictCenterYInPixel: {y}");
                    }
                }
                double? topEdgeYOfIDImageInPixel;
                if (lsTopEdgeYOfIDImageInPixelCalculated.Count > 0)
                {
                    topEdgeYOfIDImageInPixel = lsTopEdgeYOfIDImageInPixelCalculated.Average();
                }
                else
                {
                    topEdgeYOfIDImageInPixel = null;
                }
                */

                if (linesMergedNotLabel.Length > 0)
                {
                    // remove lines predicted as label because of height
                    int removedFromLinesField = lsLineMergedNotLabel.RemoveAll(l => l.ExtGetHeight() < labelHeightFilterInPixel);
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] removed {removedFromLinesField} lines predicted as label removed from linesField [height < labelHeightInPixel ({labelHeightFilterInPixel})].");
                }

                if (lsLineMergedNotLabel.Count > 0)
                {
                    //var linesLeftOrder = linesField.OrderBy(l => l.BoundingBox[0]);
                    var linesLeftOrder = lsLineMergedNotLabel.OrderBy(l => l.ExtGetLeft());
                    int countLinesField = lsLineMergedNotLabel.Count;
                    int idxMedianLinesField = countLinesField / 2;
                    double? leftMedian = null;
                    if (linesLeftOrder != null && linesLeftOrder.Count() > 0)
                    {
                        leftMedian = linesLeftOrder.ElementAt(idxMedianLinesField).BoundingBox[0];
                    }

                    int numLinesField = lsLineMergedNotLabel.Count;
                    int idxMainFields = 0;

                    // predit y in inch and expected field for each file line  
                    foreach (Line line in lsLineMergedNotLabel)
                    {
                        double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                        System.Diagnostics.Debug.WriteLine($"line: {line.Text} EstimateCenterYInInch: {y}");
                    }

                    // check if face image is aligned right or left.
                    bool? isFaceAlignedRight = null;
                    if (rcFace != null && imageSrc != null)
                    {
                        int xHalf = imageSrc.Width / 2;
                        if (xHalf < rcFace.Value.Left)
                            isFaceAlignedRight = true;
                        else
                            isFaceAlignedRight = false;
                    }

                    try
                    {
                        if (m_labelPHUMID_CRN.IsLabelFound && !string.IsNullOrEmpty(m_labelPHUMID_CRN.FieldFollowing))
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {m_labelPHUMID_CRN.FieldFollowing} --> CRN");
                            CRN = m_labelPHUMID_CRN.FieldFollowing.Trim('-');    // remove '-' between 'CRN' and numbers
                        }
                        else
                        {
                            // filter lines near to line of CRN field
                            FindFromMergedLine(ref lsLineMergedNotLabel,
                                (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_CRN_FACE_R : CENTER_Y_INCH_CRN_FACE_L,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> CRN");
                                    CRN = l.Text;
                                    return true;
                                }));
                            /*
                            Line[] mergedLinesNearToCRN = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_CRN - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToCRN)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToCRN: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_CRN)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> CRN");
                                    CRN = line.Text;
                                    lsLineMerged.Remove(line);
                                    break;
                                }
                            }
                            */
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    double? heightFieldLine = 0;
                    try
                    {
                        if (m_labelPHUMID_SURNAME_FollowedByField.IsLabelFound && !string.IsNullOrEmpty(m_labelPHUMID_SURNAME_FollowedByField.FieldFollowing))
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {m_labelPHUMID_SURNAME_FollowedByField.FieldFollowing} --> SURNAME");
                            SURNAME = m_labelPHUMID_SURNAME_FollowedByField.FieldFollowing;
                            heightFieldLine = m_labelPHUMID_SURNAME_FollowedByField.LineMacthed.ExtGetHeight();
                        }
                        else
                        {
                            // filter lines near to line of sur name field
                            FindFromMergedLine(ref lsLineMergedNotLabel,
                                (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_SURNAME_FACE_R : CENTER_Y_INCH_SURNAME_FACE_L,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is under the label
                                    if (m_labelPHUMID_SURNAME.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() <= m_labelPHUMID_SURNAME.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                        if (!m_labelPHUMID_SURNAME.IsFieldInLineJustUnderTheLabel(l))
                                            return false;
                                    }

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> SURNAME");
                                    SURNAME = l.Text;
                                    heightFieldLine = l.ExtGetHeight();
                                    return true;
                                }));
                            /*
                            double centerYInInchOfField_SurName = CENTER_Y_INCH_SURNAME_FACE_L;
                            if (isFaceAlignedRight != null && isFaceAlignedRight.Value == true)
                            {
                                centerYInInchOfField_SurName = CENTER_Y_INCH_SURNAME_FACE_R;
                            }
                            Line[] mergedLinesNearToSurName = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_SurName - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToSurName)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToSurName: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_SurName)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is under the label
                                    if (labelSURNAME.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() <= labelSURNAME.LineMacthed.ExtGetVerticalCenter())
                                            continue;
                                        if (!labelSURNAME.IsFieldInLineJustUnderTheLabel(line))
                                            continue;
                                    }

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> SURNAME");
                                    SURNAME = line.Text;
                                    lsLineMerged.Remove(line);
                                    heightFieldLine = line.ExtGetHeight();
                                    break;
                                }
                            }
                            */
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        if (m_labelPHUMID_GIVEN_NAME_FollowedByField.IsLabelFound && !string.IsNullOrEmpty(m_labelPHUMID_GIVEN_NAME_FollowedByField.FieldFollowing))
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {m_labelPHUMID_GIVEN_NAME_FollowedByField.FieldFollowing} --> GIVEN_NAME");
                            GIVEN_NAME = m_labelPHUMID_GIVEN_NAME_FollowedByField.FieldFollowing;
                            if (heightFieldLine != null)
                            {
                                heightFieldLine = (heightFieldLine + m_labelPHUMID_GIVEN_NAME_FollowedByField.LineMacthed.ExtGetHeight()) / 2;
                            }
                            else
                            {
                                heightFieldLine = m_labelPHUMID_GIVEN_NAME_FollowedByField.LineMacthed.ExtGetHeight();
                            }
                        }
                        else
                        {
                            // filter lines near to lien of given name field
                            FindFromMergedLine(ref lsLineMergedNotLabel,
                                (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_GIVENNAME_FACE_R : CENTER_Y_INCH_GIVENNAME_FACE_L,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is under the label
                                    if (m_labelPHUMID_GIVEN_NAME.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() <= m_labelPHUMID_GIVEN_NAME.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                        if (!m_labelPHUMID_GIVEN_NAME.IsFieldInLineJustUnderTheLabel(l))
                                            return false;
                                    }

                                    if (heightFieldLine != null)
                                    {
                                        if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                            return false;
                                    }

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> GIVEN_NAME");
                                    GIVEN_NAME = l.Text;
                                    if (heightFieldLine != null)
                                    {
                                        heightFieldLine = (heightFieldLine + l.ExtGetHeight()) / 2;
                                    }
                                    else
                                    {
                                        heightFieldLine = l.ExtGetHeight();
                                    }
                                    return true;
                                }));
                            /*
                            double centerYInInchOfField_GivenName = CENTER_Y_INCH_GIVENNAME_FACE_L;
                            if (isFaceAlignedRight != null && isFaceAlignedRight.Value == true)
                            {
                                centerYInInchOfField_GivenName = CENTER_Y_INCH_GIVENNAME_FACE_R;
                            }
                            Line[] mergedLinesNearToGivedName = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_GivenName - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToGivedName)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToGivedName: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_GivenName)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is under the label
                                    if (labelGIVEN_NAME.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() <= labelGIVEN_NAME.LineMacthed.ExtGetVerticalCenter())
                                            continue;
                                        if (!labelGIVEN_NAME.IsFieldInLineJustUnderTheLabel(line))
                                            continue;
                                    }

                                    if (heightFieldLine != null)
                                    {
                                        if (line.ExtGetHeight() < heightFieldLine * 0.75)
                                            continue;
                                    }

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> GIVEN_NAME");
                                    GIVEN_NAME = line.Text;
                                    lsLineMerged.Remove(line);
                                    if (heightFieldLine != null)
                                    {
                                        heightFieldLine = (heightFieldLine + line.ExtGetHeight()) / 2;
                                    }
                                    else
                                    {
                                        heightFieldLine = line.ExtGetHeight();
                                    }
                                    break;
                                }
                            }
                            */
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        if (m_labelPHUMID_MIDDLE_NAME_FollowedByField.IsLabelFound && !string.IsNullOrEmpty(m_labelPHUMID_MIDDLE_NAME_FollowedByField.FieldFollowing))
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {m_labelPHUMID_MIDDLE_NAME_FollowedByField.FieldFollowing} --> MIDDLE_NAME");
                            MIDDLE_NAME = m_labelPHUMID_MIDDLE_NAME_FollowedByField.FieldFollowing;
                            if (heightFieldLine != null)
                            {
                                heightFieldLine = (heightFieldLine + m_labelPHUMID_MIDDLE_NAME_FollowedByField.LineMacthed.ExtGetHeight()) / 2;
                            }
                            else
                            {
                                heightFieldLine = m_labelPHUMID_MIDDLE_NAME_FollowedByField.LineMacthed.ExtGetHeight();
                            }
                        }
                        else
                        {
                            // filter lines near to lien of middle name field
                            FindFromMergedLine(ref lsLineMergedNotLabel,
                                (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_MIDDLENAME_FACE_R : CENTER_Y_INCH_MIDDLENAME_FACE_L,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is under the label
                                    if (m_labelPHUMID_GIVEN_NAME.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() <= m_labelPHUMID_GIVEN_NAME.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                        if (!m_labelPHUMID_GIVEN_NAME.IsFieldInLineJustUnderTheLabel(l))
                                            return false;
                                    }

                                    if (heightFieldLine != null)
                                    {
                                        if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                            return false;
                                    }

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> GIVEN_NAME");
                                    GIVEN_NAME = l.Text;
                                    if (heightFieldLine != null)
                                    {
                                        heightFieldLine = (heightFieldLine + l.ExtGetHeight()) / 2;
                                    }
                                    else
                                    {
                                        heightFieldLine = l.ExtGetHeight();
                                    }
                                    return true;
                                }));

                            FindFromMergedLine(ref lsLineMergedNotLabel,
                                (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_MIDDLENAME_FACE_R : CENTER_Y_INCH_MIDDLENAME_FACE_L,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is under the label
                                    if (m_labelPHUMID_MIDDLE_NAME.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() <= m_labelPHUMID_MIDDLE_NAME.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                        if (!m_labelPHUMID_MIDDLE_NAME.IsFieldInLineJustUnderTheLabel(l))
                                            return false;
                                    }

                                    if (heightFieldLine != null)
                                    {
                                        if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                            return false;
                                    }

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> MIDDLE_NAME");
                                    MIDDLE_NAME = l.Text;
                                    if (heightFieldLine != null)
                                    {
                                        heightFieldLine = (heightFieldLine + l.ExtGetHeight()) / 2;
                                    }
                                    else
                                    {
                                        heightFieldLine = l.ExtGetHeight();
                                    }
                                    return true;
                                }));
                            /*
                            double centerYInInchOfField_MiddleName = CENTER_Y_INCH_MIDDLENAME_FACE_L;
                            if (isFaceAlignedRight != null && isFaceAlignedRight.Value == true)
                            {
                                centerYInInchOfField_MiddleName = CENTER_Y_INCH_MIDDLENAME_FACE_R;
                            }
                            Line[] mergedLinesNearToMiddleName = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_MiddleName - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToMiddleName)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToMiddleName: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_MiddleName)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is under the label
                                    if (labelMIDDLE_NAME.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() <= labelMIDDLE_NAME.LineMacthed.ExtGetVerticalCenter())
                                            continue;
                                        if (!labelMIDDLE_NAME.IsFieldInLineJustUnderTheLabel(line))
                                            continue;
                                    }

                                    if (heightFieldLine != null)
                                    {
                                        if (line.ExtGetHeight() < heightFieldLine * 0.75)
                                            continue;
                                    }

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> MIDDLE_NAME");
                                    MIDDLE_NAME = line.Text;
                                    lsLineMerged.Remove(line);
                                    if (heightFieldLine != null)
                                    {
                                        heightFieldLine = (heightFieldLine + line.ExtGetHeight()) / 2;
                                    }
                                    else
                                    {
                                        heightFieldLine = line.ExtGetHeight();
                                    }
                                    break;
                                }
                            }
                            */
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {

                        // filter lines near to lien of sex and date of birth field
                        if (isFaceAlignedRight != null && isFaceAlignedRight.Value == true)
                        {
                            // SEX and DOB are in separated line
                            FindFromMergedLine(ref lsLineMergedNotLabel,
                                CENTER_Y_INCH_SEX_FACE_R,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is above the label 'Address'
                                    if (m_labelPHUMID_ADDRESS.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() >= m_labelPHUMID_ADDRESS.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                    }
                                    if (m_labelPHUMID_ADDRESS_LeftAligned.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() >= m_labelPHUMID_ADDRESS_LeftAligned.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                    }

                                    // split line to label and field
                                    string[] tokens = l.Text.Trim().ToUpper().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                    if (tokens != null && tokens.Length > 0)
                                    {
                                        string lastToken = tokens[tokens.Length - 1];
                                        if (lastToken == "FEMALE")
                                        {
                                            SEX = "F";
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {SEX} --> SEX");
                                            return true;
                                        }
                                        if (lastToken == "MALE")
                                        {
                                            SEX = "M";
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {SEX} --> SEX");
                                            return true;
                                        }
                                    }
                                    return false;
                                }));

                            /*
                            Line[] mergedLinesNearToSex = lsLineMerged.OrderBy(l => Math.Abs((decimal)(CENTER_Y_INCH_SEX_FACE_R - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToSex)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToSex: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - CENTER_Y_INCH_SEX_FACE_R)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is above the label 'Address'
                                    if (labelADDRESS.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() >= labelADDRESS.LineMacthed.ExtGetVerticalCenter())
                                            continue;
                                    }
                                    if (labelADDRESS_LeftAligned.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() >= labelADDRESS_LeftAligned.LineMacthed.ExtGetVerticalCenter())
                                            continue;
                                    }

                                    // split line to label and field
                                    string[] tokens = line.Text.Trim().ToUpper().Split(' ');
                                    if(tokens != null && tokens.Length > 0)
                                    {
                                        string lastToken = tokens[tokens.Length - 1];
                                        if (lastToken == "FEMALE")
                                        {
                                            SEX = "F";
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {SEX} --> SEX");
                                            lsLineMerged.Remove(line);
                                            break;
                                        }
                                        if (lastToken == "MALE")
                                        {
                                            SEX = "M";
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {SEX} --> SEX");
                                            lsLineMerged.Remove(line);
                                            break;
                                        }
                                    }
                                }
                            }
                            */

                            FindFromMergedLine(ref lsLineMergedNotLabel,
                                CENTER_Y_INCH_DOB_FACE_R,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is above the label 'Address'
                                    if (m_labelPHUMID_ADDRESS.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() >= m_labelPHUMID_ADDRESS.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                    }
                                    if (m_labelPHUMID_ADDRESS_LeftAligned.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() >= m_labelPHUMID_ADDRESS_LeftAligned.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                    }

                                    // split line to label and field
                                    string[] tokens = l.Text.Trim().ToUpper().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                    if (tokens != null && tokens.Length > 0)
                                    {
                                        DOB = tokens[tokens.Length - 1];
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {DOB} --> DOB");
                                        return true;
                                    }
                                    return false;
                                }));
                            /*    
                            Line[] mergedLinesNearToDoB = lsLineMerged.OrderBy(l => Math.Abs((decimal)(CENTER_Y_INCH_DOB_FACE_R - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToDoB)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToDoB: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - CENTER_Y_INCH_DOB_FACE_R)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is above the label 'Address'
                                    if (labelADDRESS.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() >= labelADDRESS.LineMacthed.ExtGetVerticalCenter())
                                            continue;
                                    }
                                    if (labelADDRESS_LeftAligned.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() >= labelADDRESS_LeftAligned.LineMacthed.ExtGetVerticalCenter())
                                            continue;
                                    }

                                    // split line to label and field
                                    string[] tokens = line.Text.Trim().ToUpper().Split(' ');
                                    if(tokens != null && tokens.Length > 0)
                                    {
                                        DOB = tokens[tokens.Length - 1];
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {DOB} --> DOB");
                                        break;
                                    }
                                }
                            }
                            */
                        }
                        else
                        {
                            // SEX and DOB are in the same line
                            FindFromMergedLine(ref lsLineMergedNotLabel,
                                CENTER_Y_INCH_SEX_DOB_FACE_L,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is above the label 'Address'
                                    if (m_labelPHUMID_ADDRESS.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() >= m_labelPHUMID_ADDRESS.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                    }
                                    if (m_labelPHUMID_ADDRESS_LeftAligned.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() >= m_labelPHUMID_ADDRESS_LeftAligned.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                    }

                                    // split line to sex and date of birth
                                    string sex_dob = l.Text.Trim().ToUpper();
                                    string[] tokens = sex_dob.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                    for (int i = 0; i < tokens.Length; i++)
                                    {
                                        string token = tokens[i];
                                        if (token == "SEX" && tokens.Length > i + 1)
                                        {
                                            string sex_field = tokens[i + 1];
                                            if (sex_field == "F" || sex_field == "M")
                                            {
                                                i++;
                                                SEX = sex_field;
                                                continue;
                                            }
                                        }

                                        if (token == "F" || token == "M")
                                        {
                                            SEX = token;
                                            continue;
                                        }

                                        if (i == tokens.Length - 1)
                                        {
                                            DOB = token;
                                        }
                                    }

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {SEX} --> SEX");
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {DOB} --> DOB");
                                    return (!string.IsNullOrEmpty(SEX) || !string.IsNullOrEmpty(DOB));
                                }));

                            /*
                            double centerYInInchOfField_SexDateOfBirth = CENTER_Y_INCH_SEX_DOB_FACE_L;
                            Line[] mergedLinesNearToSexDoB = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_SexDateOfBirth - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToSexDoB)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToSexDoB: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_SexDateOfBirth)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is above the label 'Address'
                                    if (labelADDRESS.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() >= labelADDRESS.LineMacthed.ExtGetVerticalCenter())
                                            continue;
                                    }
                                    if (labelADDRESS_LeftAligned.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() >= labelADDRESS_LeftAligned.LineMacthed.ExtGetVerticalCenter())
                                            continue;
                                    }

                                    // split line to sex and date of birth
                                    string sex_dob = line.Text.Trim().ToUpper();
                                    string[] tokens = sex_dob.Split(' ');
                                    for (int i = 0; i < tokens.Length; i++)
                                    {
                                        string token = tokens[i];
                                        if (token == "SEX" && tokens.Length > i + 1)
                                        {
                                            string sex_field = tokens[i + 1];
                                            if (sex_field == "F" || sex_field == "M")
                                            {
                                                i++;
                                                SEX = sex_field;
                                                continue;
                                            }
                                        }

                                        if (token == "F" || token == "M")
                                        {
                                            SEX = token;
                                            continue;
                                        }

                                        if (i == tokens.Length - 1)
                                        {
                                            DOB = token;
                                        }
                                    }

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {SEX} --> SEX");
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {DOB} --> DOB");
                                    if (!string.IsNullOrEmpty(SEX) || !string.IsNullOrEmpty(DOB))
                                    {
                                        lsLineMerged.Remove(line);
                                    }
                                    break;
                                }
                            }
                            */
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        // filter lines near address field
                        FindFromMergedLine(ref lsLineMergedNotLabel,
                            (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_ADDRESS1_FACE_R : CENTER_Y_INCH_ADDRESS1_FACE_L,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the label
                                if (m_labelPHUMID_ADDRESS.IsLabelFound && (l.ExtGetVerticalCenter() < m_labelPHUMID_ADDRESS.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHUMID_ADDRESS_LeftAligned.IsLabelFound && (l.ExtGetVerticalCenter() < m_labelPHUMID_ADDRESS_LeftAligned.LineMacthed.ExtGetVerticalCenter()))
                                    return false;

                                if (heightFieldLine != null)
                                {
                                    if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                        return false;
                                }

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> ADDRESS1");
                                lineAddress1 = l;
                                if (heightFieldLine != null)
                                {
                                    heightFieldLine = (heightFieldLine + l.ExtGetHeight()) / 2;
                                }
                                else
                                {
                                    heightFieldLine = l.ExtGetHeight();
                                }
                                ADDRESS1 = lineAddress1.Text;
                                return true;
                            }));
                        /*
                        // filter lines near to lien of address field
                        double centerYInInchOfField_Address1 = CENTER_Y_INCH_ADDRESS1_FACE_L;
                        if (isFaceAlignedRight != null && isFaceAlignedRight.Value == true)
                        {
                            centerYInInchOfField_Address1 = CENTER_Y_INCH_ADDRESS1_FACE_R;
                        }
                        Line[] mergedLinesNearToAddress1 = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address1 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress1)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress1: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address1)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the label
                                if (labelADDRESS.IsLabelFound && (line.ExtGetVerticalCenter() < labelADDRESS.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (labelADDRESS_LeftAligned.IsLabelFound && (line.ExtGetVerticalCenter() < labelADDRESS_LeftAligned.LineMacthed.ExtGetVerticalCenter()))
                                    continue;

                                if (heightFieldLine != null)
                                {
                                    if (line.ExtGetHeight() < heightFieldLine * 0.75)
                                        continue;
                                }

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS1");
                                lineAddress1 = line;
                                lsLineMerged.Remove(line);
                                if (heightFieldLine != null)
                                {
                                    heightFieldLine = (heightFieldLine + line.ExtGetHeight()) / 2;
                                }
                                else
                                {
                                    heightFieldLine = line.ExtGetHeight();
                                }
                                ADDRESS1 = lineAddress1.Text;
                                break;
                            }
                        }
                        */
                        FindFromMergedLine(ref lsLineMergedNotLabel,
                            (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_ADDRESS2_FACE_R : CENTER_Y_INCH_ADDRESS2_FACE_L,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the Addres1
                                if (lineAddress1 != null && (l.ExtGetVerticalCenter() <= lineAddress1.ExtGetVerticalCenter()))
                                    return false;

                                if (heightFieldLine != null)
                                {
                                    if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                        return false;
                                }

                                // the 2nd line of Address is under 1st line of address 
                                if (!IsFieldJustUnderTheLine(lineAddress1, l))
                                    return false;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> ADDRESS2");
                                lineAddress2 = l;
                                ADDRESS2 = lineAddress2.Text;
                                if (heightFieldLine != null)
                                {
                                    heightFieldLine = (heightFieldLine + l.ExtGetHeight()) / 2;
                                }
                                else
                                {
                                    heightFieldLine = l.ExtGetHeight();
                                }
                                return true;
                            }));

                        /*
                        double centerYInInchOfField_Address2 = CENTER_Y_INCH_ADDRESS2_FACE_L;
                        if (isFaceAlignedRight != null && isFaceAlignedRight.Value == true)
                        {
                            centerYInInchOfField_Address2 = CENTER_Y_INCH_ADDRESS2_FACE_R;
                        }
                        Line[] mergedLinesNearToAddress2 = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address2 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress2)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress2: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address2)) <= (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the Addres1
                                if (lineAddress1 != null && (line.ExtGetVerticalCenter() <= lineAddress1.ExtGetVerticalCenter()))
                                    continue;

                                if (heightFieldLine != null)
                                {
                                    if (line.ExtGetHeight() < heightFieldLine * 0.75)
                                        continue;
                                }

                                // the 2nd line of Address is under 1st line of address 
                                if (IsFieldJustUnderTheLine(lineAddress1, line))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS2");
                                    lineAddress2 = line;
                                    lsLineMerged.Remove(line);
                                    ADDRESS2 = lineAddress2.Text;
                                    if (heightFieldLine != null)
                                    {
                                        heightFieldLine = (heightFieldLine + line.ExtGetHeight()) / 2;
                                    }
                                    else
                                    {
                                        heightFieldLine = line.ExtGetHeight();
                                    }
                                    break;
                                }
                            }
                        }
                        */

                        FindFromMergedLine(ref lsLineMergedNotLabel,
                            (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_ADDRESS3_FACE_R : CENTER_Y_INCH_ADDRESS3_FACE_L,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the Addres2
                                if (lineAddress2 != null && (l.ExtGetVerticalCenter() <= lineAddress2.ExtGetVerticalCenter()))
                                    return false;

                                if (heightFieldLine != null)
                                {
                                    if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                        return false;
                                }

                                // the 3rd line of Address is under 2nd line of address 
                                if (!IsFieldJustUnderTheLine(lineAddress2, l))
                                    return false;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> ADDRESS3");
                                lineAddress3 = l;
                                ADDRESS3 = lineAddress3.Text;
                                if (heightFieldLine != null)
                                {
                                    heightFieldLine = (heightFieldLine + l.ExtGetHeight()) / 2;
                                }
                                else
                                {
                                    heightFieldLine = l.ExtGetHeight();
                                }
                                return true;
                            }));

                        /*
                        double centerYInInchOfField_Address3 = CENTER_Y_INCH_ADDRESS3_FACE_L;
                        if (isFaceAlignedRight != null && isFaceAlignedRight.Value == true)
                        {
                            centerYInInchOfField_Address3 = CENTER_Y_INCH_ADDRESS3_FACE_R;
                        }
                        Line[] mergedLinesNearToAddress3 = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address3 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress3)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress3: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address3)) <= (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the Addres2
                                if (lineAddress2 != null && (line.ExtGetVerticalCenter() <= lineAddress2.ExtGetVerticalCenter()))
                                    continue;

                                if (heightFieldLine != null)
                                {
                                    if (line.ExtGetHeight() < heightFieldLine * 0.75)
                                        continue;
                                }

                                // the 3rd line of Address is under 2nd line of address 
                                if (IsFieldJustUnderTheLine(lineAddress2, line))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS3");
                                    lineAddress3 = line;
                                    lsLineMerged.Remove(line);
                                    ADDRESS3 = lineAddress3.Text;
                                    if (heightFieldLine != null)
                                    {
                                        heightFieldLine = (heightFieldLine + line.ExtGetHeight()) / 2;
                                    }
                                    else
                                    {
                                        heightFieldLine = line.ExtGetHeight();
                                    }
                                    break;
                                }
                            }
                        }
                        */

                        FindFromMergedLine(ref lsLineMergedNotLabel,
                            (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_ADDRESS4_FACE_R : CENTER_Y_INCH_ADDRESS4_FACE_L,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the Addres3
                                if (lineAddress3 != null && (l.ExtGetVerticalCenter() <= lineAddress3.ExtGetVerticalCenter()))
                                    return false;

                                if (heightFieldLine != null)
                                {
                                    if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                        return false;
                                }

                                // the 4th line of Address is under 3rd line of address 
                                if (!IsFieldJustUnderTheLine(lineAddress3, l))
                                    return false;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> ADDRESS4");
                                lineAddress4 = l;
                                ADDRESS4 = lineAddress4.Text;
                                return true;
                            }));
                        /*
                        double centerYInInchOfField_Address4 = CENTER_Y_INCH_ADDRESS4_FACE_L;
                        if (isFaceAlignedRight != null && isFaceAlignedRight.Value == true)
                        {
                            centerYInInchOfField_Address4 = CENTER_Y_INCH_ADDRESS4_FACE_R;
                        }
                        Line[] mergedLinesNearToAddress4 = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address4 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress4)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress4: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address4)) <= (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the Addres3
                                if (lineAddress3 != null && (line.ExtGetVerticalCenter() <= lineAddress3.ExtGetVerticalCenter()))
                                    continue;

                                if (heightFieldLine != null)
                                {
                                    if (line.ExtGetHeight() < heightFieldLine * 0.75)
                                        continue;
                                }

                                // the 4th line of Address is under 3rd line of address 
                                if (IsFieldJustUnderTheLine(lineAddress3, line))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS4");
                                    lineAddress4 = line;
                                    lsLineMerged.Remove(line);
                                    ADDRESS4 = lineAddress4.Text;
                                    break;
                                }
                            }
                        }
                        */
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    // sort from top to bottom
                    //linesInMainColumn.OrderBy(l => l.BoundingBox[1]);
                }
            }

            // map to result and convert format 
            List<string> lsMissingFields = new List<string>();

            // SURNAME -> lastNameOrFullName 
            result.lastNameOrFullName = SURNAME;
            if (string.IsNullOrEmpty(SURNAME)) lsMissingFields.Add("SURNAME");

            // GIVEN_NAME -> firstName 
            result.firstName = GIVEN_NAME;
            if (string.IsNullOrEmpty(GIVEN_NAME)) lsMissingFields.Add("GIVEN_NAME");

            if (!string.IsNullOrEmpty(GIVEN_NAME2))
            {
                result.firstName = GIVEN_NAME + " " + GIVEN_NAME2;
            }

            // MIDDLE_NAME -> middleName 
            result.middleName = MIDDLE_NAME;

            // IDNUM -> documentNumber
            string tempCRN = CorrectFalseParsedNumericLine(CRN);
            if (!string.IsNullOrEmpty(tempCRN))
            {
                tempCRN = tempCRN.Replace(" ", "").ToUpper();
                tempCRN = tempCRN.Replace("CRN-", "");
                tempCRN = tempCRN.Replace("CRN", "");
            }
            result.documentNumber = tempCRN;
            if (string.IsNullOrEmpty(CRN)) lsMissingFields.Add("CRN");

            // (CITIZENSHIP) nationality is "PH" (by default)

            // SEX
            result.gender = SEX;

            // DOB "yyyy/MM/dd" -> dateOfBirth "yyyy-MM-dd"
            try
            {
                result.dateOfBirth = "";

                if (DOB.Length == 10)
                {
                    int yyyy = int.Parse(DOB.Substring(0, 4));
                    int MM = int.Parse(DOB.Substring(5, 2));
                    int dd = int.Parse(DOB.Substring(8, 2));
                    result.dateOfBirth = $"{yyyy:0000}-{MM:00}-{dd:00}";
                }
                else
                {
                    if (string.IsNullOrEmpty(DOB)) lsMissingFields.Add("DOB");
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                if (string.IsNullOrEmpty(DOB)) lsMissingFields.Add("DOB");
            }

            // ADDRESS1, ADDRESS2 -> addressLine1, addressLine2
            if (string.IsNullOrEmpty(ADDRESS1)) lsMissingFields.Add("ADDRESS1");
            // extract post code 
            if (string.IsNullOrEmpty(ADDRESS4))
            {
                if (string.IsNullOrEmpty(ADDRESS3))
                {
                    if (string.IsNullOrEmpty(ADDRESS2))
                    {
                        // 1 address line only
                        int lenAddrLast = ADDRESS1.Length;
                        string addrLast = "";
                        string last4 = "";
                        if (ADDRESS1.Length > 4)
                        {
                            addrLast = ADDRESS1.Substring(0, ADDRESS1.Length - 4);
                            last4 = ADDRESS1.Substring(ADDRESS1.Length - 4);
                        }
                        int nPostcode = 0;
                        if (int.TryParse(last4, out nPostcode))
                        {
                            result.addressLine1 = $"{addrLast}";
                            result.postcode = $"{last4}";
                        }
                        else
                        {
                            result.addressLine1 = $"{ADDRESS1}";
                        }
                    }
                    else
                    {
                        // 2 addresslines
                        int lenAddrLast = ADDRESS2.Length;
                        string addrLast = "";
                        string last4 = "";
                        if (ADDRESS2.Length > 4)
                        {
                            addrLast = ADDRESS2.Substring(0, ADDRESS2.Length - 4);
                            last4 = ADDRESS2.Substring(ADDRESS2.Length - 4);
                        }
                        int nPostcode = 0;
                        if (int.TryParse(last4, out nPostcode))
                        {
                            result.addressLine1 = $"{ADDRESS1}";
                            result.addressLine2 = $"{addrLast}";
                            result.postcode = $"{last4}";
                        }
                        else
                        {
                            result.addressLine1 = $"{ADDRESS1}";
                            result.addressLine2 = $"{ADDRESS2}";
                        }
                    }
                }
                else
                {
                    // 3 address lines
                    int lenAddrLast = ADDRESS3.Length;
                    string addrLast = "";
                    string last4 = "";
                    int nPostcode = 0;
                    if (ADDRESS3.Length <= 4)
                    {
                        // the last line may be postcode
                        last4 = ADDRESS3;
                        if (int.TryParse(last4, out nPostcode))
                        {
                            result.addressLine1 = $"{ADDRESS1}";
                            result.addressLine2 = $"{ADDRESS2}";
                            result.postcode = $"{last4}";
                        }
                        else
                        {
                            result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                            result.addressLine2 = $"{ADDRESS3}";
                        }
                    }
                    else
                    {
                        addrLast = ADDRESS3.Substring(0, ADDRESS3.Length - 4);
                        last4 = ADDRESS3.Substring(ADDRESS3.Length - 4);
                        if (int.TryParse(last4, out nPostcode))
                        {
                            result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                            result.addressLine2 = $"{addrLast}";
                            result.postcode = $"{last4}";
                        }
                        else
                        {
                            result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                            result.addressLine2 = $"{ADDRESS3}";
                        }
                    }
                }
            }
            else
            {
                // 4 address lines
                int lenAddrLast = ADDRESS4.Length;
                string addrLast = "";
                string last4 = "";
                if (ADDRESS4.Length > 4)
                {
                    addrLast = ADDRESS4.Substring(0, ADDRESS4.Length - 4);
                    last4 = ADDRESS4.Substring(ADDRESS4.Length - 4);
                }
                int nPostcode = 0;
                if (int.TryParse(last4, out nPostcode))
                {
                    result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                    result.addressLine2 = $"{ADDRESS3} {addrLast}";
                    result.postcode = $"{last4}";
                }
                else
                {
                    result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                    result.addressLine2 = $"{ADDRESS3} {ADDRESS4}";
                }
            }

            // determine success or not
            if (lsMissingFields.Count == 0)
            {
                result.Success = true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ExtractFieldsFromReadResultOfPHUMID result NOT success");
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
        ScanPHUMIDResult ExtractFieldsFromReadResultOfPHUMID1(LabelInfo[] labelsFound, Line[] linesMergedNotLabel, SKRectI? rcFace, SKImage imageSrc, ImgProcLib.MatchTemplateIDCard? matchTemplatePHUMID)
        {
            // For UMID with face picture located in the right side
            const double CENTER_Y_INCH_CRN_FACE_R = 0.62f;
            const double CENTER_Y_INCH_SURNAME_FACE_R = 0.74f;
            const double CENTER_Y_INCH_GIVENNAME_FACE_R = 0.96f;
            const double CENTER_Y_INCH_MIDDLENAME_FACE_R = 1.18f;
            const double CENTER_Y_INCH_SEX_FACE_R = 1.40f;
            const double CENTER_Y_INCH_DOB_FACE_R = 1.52f;
            const double CENTER_Y_INCH_ADDRESS1_FACE_R = 1.74f;
            const double CENTER_Y_INCH_ADDRESS2_FACE_R = 1.84f;
            const double CENTER_Y_INCH_ADDRESS3_FACE_R = 1.93f;
            const double CENTER_Y_INCH_ADDRESS4_FACE_R = 2.03f;

            const double CENTER_Y_INCH_CRN_FACE_L = 0.63f;
            const double CENTER_Y_INCH_SURNAME_FACE_L = 0.92f;
            const double CENTER_Y_INCH_GIVENNAME_FACE_L = 1.12f;
            const double CENTER_Y_INCH_MIDDLENAME_FACE_L = 1.42f;
            const double CENTER_Y_INCH_SEX_DOB_FACE_L = 1.54f;
            const double CENTER_Y_INCH_ADDRESS1_FACE_L = 1.68f;
            const double CENTER_Y_INCH_ADDRESS2_FACE_L = 1.76f;
            const double CENTER_Y_INCH_ADDRESS3_FACE_L = 1.83f;
            const double CENTER_Y_INCH_ADDRESS4_FACE_L = 1.92f;

            Dictionary<string, MatchTemplateInfo> dicMatchTemplateInfo = new Dictionary<string, MatchTemplateInfo>();
            MatchTemplateInfo matchTmplPHUMID_670_LogoL_Gray = new MatchTemplateInfo("PHUMID_670_LogoL_Gray", "COA", 0.8f, 0.3f, 0.25f);
            dicMatchTemplateInfo.Add(matchTmplPHUMID_670_LogoL_Gray.Name, matchTmplPHUMID_670_LogoL_Gray);
            MatchTemplateInfo matchTmplPHUMID_670_LogoR_Gray = new MatchTemplateInfo("PHUMID_670_LogoR_Gray", "Flag", 0.8f, 1.75f, 0.25f);
            dicMatchTemplateInfo.Add(matchTmplPHUMID_670_LogoR_Gray.Name, matchTmplPHUMID_670_LogoR_Gray);

            ScanPHUMIDResult result = new ScanPHUMIDResult();

            string CRN = "";
            string SURNAME = "";
            string GIVEN_NAME = "";
            string GIVEN_NAME2 = "";
            string MIDDLE_NAME = "";
            string SEX = "";
            string DOB = "";
            Line lineSexDoB = null;
            string ADDRESS1 = "";
            Line lineAddress1 = null;
            string ADDRESS2 = "";
            Line lineAddress2 = null;
            string ADDRESS3 = "";
            Line lineAddress3 = null;
            string ADDRESS4 = "";
            Line lineAddress4 = null;
            string POSTCODE = "";
            const double ACCEPTABLE_DIFF_IN_LINE = 0.1f;

            List<Line> lsLineMergedNotLabel = new List<Line>();
            lsLineMergedNotLabel.AddRange(linesMergedNotLabel);

            // calc pixel per inch
            double? ppi = LabelInfo.CalcPixelPerInch(labelsFound);
            if (ppi == null)
            {
                Console.WriteLine("CalcPixelPerInch failed");
            }
            else
            {
                const double labelHeightFilterInInch = 0.08f;
                double labelHeightFilterInPixel = (double)labelHeightFilterInInch * ppi.Value;

                //
                // calc top and left edge
                //
                double? topEdgeYOfIDImageInPixel;
                double? leftEdgeXOfIDImageInPixel;
                CalcLeftTopEdgeInPixel(labelsFound, ppi, out leftEdgeXOfIDImageInPixel, out topEdgeYOfIDImageInPixel);
                if (topEdgeYOfIDImageInPixel != null && leftEdgeXOfIDImageInPixel != null)
                {
                    if (topEdgeYOfIDImageInPixel != null && leftEdgeXOfIDImageInPixel != null)
                    {
                        MatchTemplateResult matchTemplateResult = null;
                        SKData dataID200ppiPng = null;
                        DateTime dtStart = DateTime.Now;
                        bool bRetMatchTemplate = DoMatchTemplate(matchTemplatePHUMID, imageSrc, topEdgeYOfIDImageInPixel.Value, leftEdgeXOfIDImageInPixel.Value, ppi.Value,
                            out matchTemplateResult, out dataID200ppiPng);
                        DateTime dtEnd = DateTime.Now;
                        result.timeElapsedLandmarkDetection = (int)(dtEnd - dtStart).TotalMilliseconds;
                        result.CardImage200ppiPngB64 = Convert.ToBase64String(dataID200ppiPng.ToArray());
                        if (bRetMatchTemplate)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTemplateMyKadResult: {matchTemplateResult.MatchResult}");
                            SKImage imgID200ppi = SKImage.FromEncodedData(dataID200ppiPng);
                            GenerateMatchTemplateResults(matchTemplateResult, dicMatchTemplateInfo, result.MatchTemplateResults);
                            result.landmarkImageBase64 = GenerateMatchTemplateResultImage(result.MatchTemplateResults, result.CardImage200ppiPngB64);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTemplate failed");
                        }
                    }
                }

                /*
                List<double> lsTopEdgeYOfIDImageInPixelCalculated = new List<double>();
                foreach (LabelInfo label in labelsFound)
                {
                    double? topEdgeYOfIDImageInPixelCalculated = label.CalcTopEdgeYOfIDImageInPixel(ppi.Value);
                    if (topEdgeYOfIDImageInPixelCalculated != null)
                    {
                        lsTopEdgeYOfIDImageInPixelCalculated.Add(topEdgeYOfIDImageInPixelCalculated.Value);
                        double? y = label.PredictCenterYInPixel(ppi, topEdgeYOfIDImageInPixelCalculated);
                        System.Diagnostics.Debug.WriteLine($"{label.Title} PredictCenterYInPixel: {y}");
                    }
                }
                double? topEdgeYOfIDImageInPixel;
                if (lsTopEdgeYOfIDImageInPixelCalculated.Count > 0)
                {
                    topEdgeYOfIDImageInPixel = lsTopEdgeYOfIDImageInPixelCalculated.Average();
                }
                else
                {
                    topEdgeYOfIDImageInPixel = null;
                }
                */

                if (linesMergedNotLabel.Length > 0)
                {
                    // remove lines predicted as label because of height
                    int removedFromLinesField = lsLineMergedNotLabel.RemoveAll(l => l.ExtGetHeight() < labelHeightFilterInPixel);
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] removed {removedFromLinesField} lines predicted as label removed from linesField [height < labelHeightInPixel ({labelHeightFilterInPixel})].");
                }

                if (lsLineMergedNotLabel.Count > 0)
                {
                    //var linesLeftOrder = linesField.OrderBy(l => l.BoundingBox[0]);
                    var linesLeftOrder = lsLineMergedNotLabel.OrderBy(l => l.ExtGetLeft());
                    int countLinesField = lsLineMergedNotLabel.Count;
                    int idxMedianLinesField = countLinesField / 2;
                    double? leftMedian = null;
                    if (linesLeftOrder != null && linesLeftOrder.Count() > 0)
                    {
                        leftMedian = linesLeftOrder.ElementAt(idxMedianLinesField).BoundingBox[0];
                    }

                    int numLinesField = lsLineMergedNotLabel.Count;
                    int idxMainFields = 0;

                    // predit y in inch and expected field for each file line  
                    foreach (Line line in lsLineMergedNotLabel)
                    {
                        double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                        System.Diagnostics.Debug.WriteLine($"line: {line.Text} EstimateCenterYInInch: {y}");
                    }

                    // check if face image is aligned right or left.
                    bool? isFaceAlignedRight = null;
                    if (rcFace != null && imageSrc != null)
                    {
                        int xHalf = imageSrc.Width / 2;
                        if (xHalf < rcFace.Value.Left)
                            isFaceAlignedRight = true;
                        else
                            isFaceAlignedRight = false;
                    }

                    try
                    {
                        if (m_labelPHUMID1_CRN.IsLabelFound && !string.IsNullOrEmpty(m_labelPHUMID1_CRN.FieldFollowing))
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {m_labelPHUMID1_CRN.FieldFollowing} --> CRN");
                            CRN = m_labelPHUMID1_CRN.FieldFollowing.Trim('-');    // remove '-' between 'CRN' and numbers
                        }
                        else
                        {
                            // filter lines near to line of CRN field
                            FindFromMergedLine(ref lsLineMergedNotLabel,
                                (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_CRN_FACE_R : CENTER_Y_INCH_CRN_FACE_L,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> CRN");
                                    CRN = l.Text;
                                    return true;
                                }));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    double? heightFieldLine = 0;
                    try
                    {
                        if (m_labelPHUMID1_SURNAME_FollowedByField.IsLabelFound && !string.IsNullOrEmpty(m_labelPHUMID1_SURNAME_FollowedByField.FieldFollowing))
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {m_labelPHUMID1_SURNAME_FollowedByField.FieldFollowing} --> SURNAME");
                            SURNAME = m_labelPHUMID1_SURNAME_FollowedByField.FieldFollowing;
                            heightFieldLine = m_labelPHUMID1_SURNAME_FollowedByField.LineMacthed.ExtGetHeight();
                        }
                        else
                        {
                            // filter lines near to line of sur name field
                            FindFromMergedLine(ref lsLineMergedNotLabel,
                                (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_SURNAME_FACE_R : CENTER_Y_INCH_SURNAME_FACE_L,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> SURNAME");
                                    SURNAME = l.Text;
                                    heightFieldLine = l.ExtGetHeight();
                                    return true;
                                }));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        if (m_labelPHUMID1_GIVEN_NAME_FollowedByField.IsLabelFound && !string.IsNullOrEmpty(m_labelPHUMID1_GIVEN_NAME_FollowedByField.FieldFollowing))
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {m_labelPHUMID1_GIVEN_NAME_FollowedByField.FieldFollowing} --> GIVEN_NAME");
                            GIVEN_NAME = m_labelPHUMID1_GIVEN_NAME_FollowedByField.FieldFollowing;
                            if (heightFieldLine != null)
                            {
                                heightFieldLine = (heightFieldLine + m_labelPHUMID1_GIVEN_NAME_FollowedByField.LineMacthed.ExtGetHeight()) / 2;
                            }
                            else
                            {
                                heightFieldLine = m_labelPHUMID1_GIVEN_NAME_FollowedByField.LineMacthed.ExtGetHeight();
                            }
                        }
                        else
                        {
                            // filter lines near to lien of given name field
                            FindFromMergedLine(ref lsLineMergedNotLabel,
                                (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_GIVENNAME_FACE_R : CENTER_Y_INCH_GIVENNAME_FACE_L,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    if (heightFieldLine != null)
                                    {
                                        if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                            return false;
                                    }

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> GIVEN_NAME");
                                    GIVEN_NAME = l.Text;
                                    if (heightFieldLine != null)
                                    {
                                        heightFieldLine = (heightFieldLine + l.ExtGetHeight()) / 2;
                                    }
                                    else
                                    {
                                        heightFieldLine = l.ExtGetHeight();
                                    }
                                    return true;
                                }));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        if (m_labelPHUMID1_MIDDLE_NAME_FollowedByField.IsLabelFound && !string.IsNullOrEmpty(m_labelPHUMID1_MIDDLE_NAME_FollowedByField.FieldFollowing))
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {m_labelPHUMID1_MIDDLE_NAME_FollowedByField.FieldFollowing} --> MIDDLE_NAME");
                            MIDDLE_NAME = m_labelPHUMID1_MIDDLE_NAME_FollowedByField.FieldFollowing;
                            if (heightFieldLine != null)
                            {
                                heightFieldLine = (heightFieldLine + m_labelPHUMID1_MIDDLE_NAME_FollowedByField.LineMacthed.ExtGetHeight()) / 2;
                            }
                            else
                            {
                                heightFieldLine = m_labelPHUMID1_MIDDLE_NAME_FollowedByField.LineMacthed.ExtGetHeight();
                            }
                        }
                        else
                        {
                            // filter lines near to lien of middle name field
                            FindFromMergedLine(ref lsLineMergedNotLabel,
                                (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_MIDDLENAME_FACE_R : CENTER_Y_INCH_MIDDLENAME_FACE_L,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    if (heightFieldLine != null)
                                    {
                                        if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                            return false;
                                    }

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> MIDDLE_NAME");
                                    MIDDLE_NAME = l.Text;
                                    if (heightFieldLine != null)
                                    {
                                        heightFieldLine = (heightFieldLine + l.ExtGetHeight()) / 2;
                                    }
                                    else
                                    {
                                        heightFieldLine = l.ExtGetHeight();
                                    }
                                    return true;
                                }));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {

                        // filter lines near to lien of sex and date of birth field
                        if (isFaceAlignedRight != null && isFaceAlignedRight.Value == true)
                        {
                            // SEX and DOB are in separated line
                            FindFromMergedLine(ref lsLineMergedNotLabel,
                                CENTER_Y_INCH_SEX_FACE_R,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is above the label 'Address'
                                    if (m_labelPHUMID1_ADDRESS_LeftAligned.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() >= m_labelPHUMID1_ADDRESS_LeftAligned.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                    }

                                    // split line to label and field
                                    string[] tokens = l.Text.Trim().ToUpper().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                    if (tokens != null && tokens.Length > 0)
                                    {
                                        string lastToken = tokens[tokens.Length - 1];
                                        if (lastToken == "FEMALE")
                                        {
                                            SEX = "F";
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {SEX} --> SEX");
                                            return true;
                                        }
                                        if (lastToken == "MALE")
                                        {
                                            SEX = "M";
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {SEX} --> SEX");
                                            return true;
                                        }
                                    }
                                    return false;
                                }));

                            FindFromMergedLine(ref lsLineMergedNotLabel,
                                CENTER_Y_INCH_DOB_FACE_R,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is above the label 'Address'
                                    if (m_labelPHUMID1_ADDRESS_LeftAligned.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() >= m_labelPHUMID1_ADDRESS_LeftAligned.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                    }

                                    // split line to label and field
                                    string[] tokens = l.Text.Trim().ToUpper().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                    if (tokens != null && tokens.Length > 0)
                                    {
                                        DOB = tokens[tokens.Length - 1];
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {DOB} --> DOB");
                                        return true;
                                    }
                                    return false;
                                }));
                        }
                        else
                        {
                            // SEX and DOB are in the same line
                            FindFromMergedLine(ref lsLineMergedNotLabel,
                                CENTER_Y_INCH_SEX_DOB_FACE_L,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    if (m_labelPHUMID1_ADDRESS_LeftAligned.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() >= m_labelPHUMID1_ADDRESS_LeftAligned.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                    }

                                    // split line to sex and date of birth
                                    string sex_dob = l.Text.Trim().ToUpper();
                                    string[] tokens = sex_dob.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                    for (int i = 0; i < tokens.Length; i++)
                                    {
                                        string token = tokens[i];
                                        if (token == "SEX" && tokens.Length > i + 1)
                                        {
                                            string sex_field = tokens[i + 1];
                                            if (sex_field == "F" || sex_field == "M")
                                            {
                                                i++;
                                                SEX = sex_field;
                                                continue;
                                            }
                                        }

                                        if (token == "F" || token == "M")
                                        {
                                            SEX = token;
                                            continue;
                                        }

                                        if (i == tokens.Length - 1)
                                        {
                                            DOB = token;
                                        }
                                    }

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {SEX} --> SEX");
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {DOB} --> DOB");
                                    return (!string.IsNullOrEmpty(SEX) || !string.IsNullOrEmpty(DOB));
                                }));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        // filter lines near address field
                        FindFromMergedLine(ref lsLineMergedNotLabel,
                            (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_ADDRESS1_FACE_R : CENTER_Y_INCH_ADDRESS1_FACE_L,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the label
                                if (m_labelPHUMID1_ADDRESS_LeftAligned.IsLabelFound && (l.ExtGetVerticalCenter() < m_labelPHUMID1_ADDRESS_LeftAligned.LineMacthed.ExtGetVerticalCenter()))
                                    return false;

                                if (heightFieldLine != null)
                                {
                                    if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                        return false;
                                }

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> ADDRESS1");
                                lineAddress1 = l;
                                if (heightFieldLine != null)
                                {
                                    heightFieldLine = (heightFieldLine + l.ExtGetHeight()) / 2;
                                }
                                else
                                {
                                    heightFieldLine = l.ExtGetHeight();
                                }
                                ADDRESS1 = lineAddress1.Text;
                                return true;
                            }));
                        FindFromMergedLine(ref lsLineMergedNotLabel,
                            (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_ADDRESS2_FACE_R : CENTER_Y_INCH_ADDRESS2_FACE_L,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the Addres1
                                if (lineAddress1 != null && (l.ExtGetVerticalCenter() <= lineAddress1.ExtGetVerticalCenter()))
                                    return false;

                                if (heightFieldLine != null)
                                {
                                    if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                        return false;
                                }

                                // the 2nd line of Address is under 1st line of address 
                                if (!IsFieldJustUnderTheLine(lineAddress1, l))
                                    return false;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> ADDRESS2");
                                lineAddress2 = l;
                                ADDRESS2 = lineAddress2.Text;
                                if (heightFieldLine != null)
                                {
                                    heightFieldLine = (heightFieldLine + l.ExtGetHeight()) / 2;
                                }
                                else
                                {
                                    heightFieldLine = l.ExtGetHeight();
                                }
                                return true;
                            }));

                        FindFromMergedLine(ref lsLineMergedNotLabel,
                            (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_ADDRESS3_FACE_R : CENTER_Y_INCH_ADDRESS3_FACE_L,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the Addres2
                                if (lineAddress2 != null && (l.ExtGetVerticalCenter() <= lineAddress2.ExtGetVerticalCenter()))
                                    return false;

                                if (heightFieldLine != null)
                                {
                                    if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                        return false;
                                }

                                // the 3rd line of Address is under 2nd line of address 
                                if (!IsFieldJustUnderTheLine(lineAddress2, l))
                                    return false;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> ADDRESS3");
                                lineAddress3 = l;
                                ADDRESS3 = lineAddress3.Text;
                                if (heightFieldLine != null)
                                {
                                    heightFieldLine = (heightFieldLine + l.ExtGetHeight()) / 2;
                                }
                                else
                                {
                                    heightFieldLine = l.ExtGetHeight();
                                }
                                return true;
                            }));

                        FindFromMergedLine(ref lsLineMergedNotLabel,
                            (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_ADDRESS4_FACE_R : CENTER_Y_INCH_ADDRESS4_FACE_L,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the Addres3
                                if (lineAddress3 != null && (l.ExtGetVerticalCenter() <= lineAddress3.ExtGetVerticalCenter()))
                                    return false;

                                if (heightFieldLine != null)
                                {
                                    if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                        return false;
                                }

                                // the 4th line of Address is under 3rd line of address 
                                if (!IsFieldJustUnderTheLine(lineAddress3, l))
                                    return false;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> ADDRESS4");
                                lineAddress4 = l;
                                ADDRESS4 = lineAddress4.Text;
                                return true;
                            }));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    // sort from top to bottom
                    //linesInMainColumn.OrderBy(l => l.BoundingBox[1]);
                }
            }

            // map to result and convert format 
            List<string> lsMissingFields = new List<string>();

            // SURNAME -> lastNameOrFullName 
            result.lastNameOrFullName = SURNAME;
            if (string.IsNullOrEmpty(SURNAME)) lsMissingFields.Add("SURNAME");

            // GIVEN_NAME -> firstName 
            result.firstName = GIVEN_NAME;
            if (string.IsNullOrEmpty(GIVEN_NAME)) lsMissingFields.Add("GIVEN_NAME");

            if (!string.IsNullOrEmpty(GIVEN_NAME2))
            {
                result.firstName = GIVEN_NAME + " " + GIVEN_NAME2;
            }

            // MIDDLE_NAME -> middleName 
            result.middleName = MIDDLE_NAME;

            // IDNUM -> documentNumber
            string tempCRN = CorrectFalseParsedNumericLine(CRN);
            if (!string.IsNullOrEmpty(tempCRN))
            {
                tempCRN = tempCRN.Replace(" ", "").ToUpper();
                tempCRN = tempCRN.Replace("CRN-", "");
                tempCRN = tempCRN.Replace("CRN", "");
            }
            result.documentNumber = tempCRN;
            if (string.IsNullOrEmpty(CRN)) lsMissingFields.Add("CRN");

            // (CITIZENSHIP) nationality is "PH" (by default)

            // SEX
            result.gender = SEX;

            // DOB "yyyy/MM/dd" -> dateOfBirth "yyyy-MM-dd"
            try
            {
                result.dateOfBirth = "";

                if (DOB.Length == 10)
                {
                    int yyyy = int.Parse(DOB.Substring(0, 4));
                    int MM = int.Parse(DOB.Substring(5, 2));
                    int dd = int.Parse(DOB.Substring(8, 2));
                    result.dateOfBirth = $"{yyyy:0000}-{MM:00}-{dd:00}";
                }
                else
                {
                    if (string.IsNullOrEmpty(DOB)) lsMissingFields.Add("DOB");
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                if (string.IsNullOrEmpty(DOB)) lsMissingFields.Add("DOB");
            }

            // ADDRESS1, ADDRESS2 -> addressLine1, addressLine2
            if (string.IsNullOrEmpty(ADDRESS1)) lsMissingFields.Add("ADDRESS1");
            // extract post code 
            if (string.IsNullOrEmpty(ADDRESS4))
            {
                if (string.IsNullOrEmpty(ADDRESS3))
                {
                    if (string.IsNullOrEmpty(ADDRESS2))
                    {
                        // 1 address line only
                        int lenAddrLast = ADDRESS1.Length;
                        string addrLast = "";
                        string last4 = "";
                        if (ADDRESS1.Length > 4)
                        {
                            addrLast = ADDRESS1.Substring(0, ADDRESS1.Length - 4);
                            last4 = ADDRESS1.Substring(ADDRESS1.Length - 4);
                        }
                        int nPostcode = 0;
                        if (int.TryParse(last4, out nPostcode))
                        {
                            result.addressLine1 = $"{addrLast}";
                            result.postcode = $"{last4}";
                        }
                        else
                        {
                            result.addressLine1 = $"{ADDRESS1}";
                        }
                    }
                    else
                    {
                        // 2 addresslines
                        int lenAddrLast = ADDRESS2.Length;
                        string addrLast = "";
                        string last4 = "";
                        if (ADDRESS2.Length > 4)
                        {
                            addrLast = ADDRESS2.Substring(0, ADDRESS2.Length - 4);
                            last4 = ADDRESS2.Substring(ADDRESS2.Length - 4);
                        }
                        int nPostcode = 0;
                        if (int.TryParse(last4, out nPostcode))
                        {
                            result.addressLine1 = $"{ADDRESS1}";
                            result.addressLine2 = $"{addrLast}";
                            result.postcode = $"{last4}";
                        }
                        else
                        {
                            result.addressLine1 = $"{ADDRESS1}";
                            result.addressLine2 = $"{ADDRESS2}";
                        }
                    }
                }
                else
                {
                    // 3 address lines
                    int lenAddrLast = ADDRESS3.Length;
                    string addrLast = "";
                    string last4 = "";
                    int nPostcode = 0;
                    if (ADDRESS3.Length <= 4)
                    {
                        // the last line may be postcode
                        last4 = ADDRESS3;
                        if (int.TryParse(last4, out nPostcode))
                        {
                            result.addressLine1 = $"{ADDRESS1}";
                            result.addressLine2 = $"{ADDRESS2}";
                            result.postcode = $"{last4}";
                        }
                        else
                        {
                            result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                            result.addressLine2 = $"{ADDRESS3}";
                        }
                    }
                    else
                    {
                        addrLast = ADDRESS3.Substring(0, ADDRESS3.Length - 4);
                        last4 = ADDRESS3.Substring(ADDRESS3.Length - 4);
                        if (int.TryParse(last4, out nPostcode))
                        {
                            result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                            result.addressLine2 = $"{addrLast}";
                            result.postcode = $"{last4}";
                        }
                        else
                        {
                            result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                            result.addressLine2 = $"{ADDRESS3}";
                        }
                    }
                }
            }
            else
            {
                // 4 address lines
                int lenAddrLast = ADDRESS4.Length;
                string addrLast = "";
                string last4 = "";
                if (ADDRESS4.Length > 4)
                {
                    addrLast = ADDRESS4.Substring(0, ADDRESS4.Length - 4);
                    last4 = ADDRESS4.Substring(ADDRESS4.Length - 4);
                }
                int nPostcode = 0;
                if (int.TryParse(last4, out nPostcode))
                {
                    result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                    result.addressLine2 = $"{ADDRESS3} {addrLast}";
                    result.postcode = $"{last4}";
                }
                else
                {
                    result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                    result.addressLine2 = $"{ADDRESS3} {ADDRESS4}";
                }
            }

            // determine success or not
            if (lsMissingFields.Count == 0)
            {
                result.Success = true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ExtractFieldsFromReadResultOfPHUMID result NOT success");
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
        ScanPHUMIDResult ExtractFieldsFromReadResultOfPHUMID2(LabelInfo[] labelsFound, Line[] linesMergedNotLabel, SKRectI? rcFace, SKImage imageSrc, ImgProcLib.MatchTemplateIDCard? matchTemplatePHUMID)
        {
            // For UMID with face picture located in the right side
            const double CENTER_Y_INCH_CRN_FACE_R = 0.62f;
            const double CENTER_Y_INCH_SURNAME_FACE_R = 0.74f;
            const double CENTER_Y_INCH_GIVENNAME_FACE_R = 0.96f;
            const double CENTER_Y_INCH_MIDDLENAME_FACE_R = 1.18f;
            const double CENTER_Y_INCH_SEX_FACE_R = 1.40f;
            const double CENTER_Y_INCH_DOB_FACE_R = 1.52f;
            const double CENTER_Y_INCH_ADDRESS1_FACE_R = 1.74f;
            const double CENTER_Y_INCH_ADDRESS2_FACE_R = 1.84f;
            const double CENTER_Y_INCH_ADDRESS3_FACE_R = 1.93f;
            const double CENTER_Y_INCH_ADDRESS4_FACE_R = 2.03f;

            const double CENTER_Y_INCH_CRN_FACE_L = 0.63f;
            const double CENTER_Y_INCH_SURNAME_FACE_L = 0.92f;
            const double CENTER_Y_INCH_GIVENNAME_FACE_L = 1.12f;
            const double CENTER_Y_INCH_MIDDLENAME_FACE_L = 1.42f;
            const double CENTER_Y_INCH_SEX_DOB_FACE_L = 1.54f;
            const double CENTER_Y_INCH_ADDRESS1_FACE_L = 1.68f;
            const double CENTER_Y_INCH_ADDRESS2_FACE_L = 1.76f;
            const double CENTER_Y_INCH_ADDRESS3_FACE_L = 1.83f;
            const double CENTER_Y_INCH_ADDRESS4_FACE_L = 1.92f;

            Dictionary<string, MatchTemplateInfo> dicMatchTemplateInfo = new Dictionary<string, MatchTemplateInfo>();
            MatchTemplateInfo matchTmplPHUMID_670_LogoL_Gray = new MatchTemplateInfo("PHUMID_670_LogoL_Gray", "COA", 0.8f, 0.3f, 0.25f);
            dicMatchTemplateInfo.Add(matchTmplPHUMID_670_LogoL_Gray.Name, matchTmplPHUMID_670_LogoL_Gray);
            MatchTemplateInfo matchTmplPHUMID_670_LogoR_Gray = new MatchTemplateInfo("PHUMID_670_LogoR_Gray", "Flag", 0.8f, 1.75f, 0.25f);
            dicMatchTemplateInfo.Add(matchTmplPHUMID_670_LogoR_Gray.Name, matchTmplPHUMID_670_LogoR_Gray);

            ScanPHUMIDResult result = new ScanPHUMIDResult();

            string CRN = "";
            string SURNAME = "";
            string GIVEN_NAME = "";
            string GIVEN_NAME2 = "";
            string MIDDLE_NAME = "";
            string SEX = "";
            string DOB = "";
            Line lineSexDoB = null;
            string ADDRESS1 = "";
            Line lineAddress1 = null;
            string ADDRESS2 = "";
            Line lineAddress2 = null;
            string ADDRESS3 = "";
            Line lineAddress3 = null;
            string ADDRESS4 = "";
            Line lineAddress4 = null;
            string POSTCODE = "";
            const double ACCEPTABLE_DIFF_IN_LINE = 0.1f;

            List<Line> lsLineMergedNotLabel = new List<Line>();
            lsLineMergedNotLabel.AddRange(linesMergedNotLabel);

            // calc pixel per inch
            double? ppi = LabelInfo.CalcPixelPerInch(labelsFound);
            if (ppi == null)
            {
                Console.WriteLine("CalcPixelPerInch failed");
            }
            else
            {
                //const double labelHeightFilterInInch = 0.08f;
                const double labelHeightFilterInInch = 0.07f;
                double labelHeightFilterInPixel = (double)labelHeightFilterInInch * ppi.Value;

                //
                // calc top and left edge
                //
                double? topEdgeYOfIDImageInPixel;
                double? leftEdgeXOfIDImageInPixel;
                CalcLeftTopEdgeInPixel(labelsFound, ppi, out leftEdgeXOfIDImageInPixel, out topEdgeYOfIDImageInPixel);
                if (topEdgeYOfIDImageInPixel != null && leftEdgeXOfIDImageInPixel != null)
                {
                    if (topEdgeYOfIDImageInPixel != null && leftEdgeXOfIDImageInPixel != null)
                    {
                        MatchTemplateResult matchTemplateResult = null;
                        SKData dataID200ppiPng = null;
                        DateTime dtStart = DateTime.Now;
                        bool bRetMatchTemplate = DoMatchTemplate(matchTemplatePHUMID, imageSrc, topEdgeYOfIDImageInPixel.Value, leftEdgeXOfIDImageInPixel.Value, ppi.Value,
                            out matchTemplateResult, out dataID200ppiPng);
                        DateTime dtEnd = DateTime.Now;
                        result.timeElapsedLandmarkDetection = (int)(dtEnd - dtStart).TotalMilliseconds;
                        result.CardImage200ppiPngB64 = Convert.ToBase64String(dataID200ppiPng.ToArray());
                        if (bRetMatchTemplate)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTemplateMyKadResult: {matchTemplateResult.MatchResult}");
                            SKImage imgID200ppi = SKImage.FromEncodedData(dataID200ppiPng);
                            GenerateMatchTemplateResults(matchTemplateResult, dicMatchTemplateInfo, result.MatchTemplateResults);
                            result.landmarkImageBase64 = GenerateMatchTemplateResultImage(result.MatchTemplateResults, result.CardImage200ppiPngB64);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTemplate failed");
                        }
                    }
                }

                if (linesMergedNotLabel.Length > 0)
                {
                    // remove lines predicted as label because of height
                    int removedFromLinesField = lsLineMergedNotLabel.RemoveAll(l => l.ExtGetHeight() < labelHeightFilterInPixel);
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] removed {removedFromLinesField} lines predicted as label removed from linesField [height < labelHeightInPixel ({labelHeightFilterInPixel})].");
                }

                if (lsLineMergedNotLabel.Count > 0)
                {
                    //var linesLeftOrder = linesField.OrderBy(l => l.BoundingBox[0]);
                    var linesLeftOrder = lsLineMergedNotLabel.OrderBy(l => l.ExtGetLeft());
                    int countLinesField = lsLineMergedNotLabel.Count;
                    int idxMedianLinesField = countLinesField / 2;
                    double? leftMedian = null;
                    if (linesLeftOrder != null && linesLeftOrder.Count() > 0)
                    {
                        leftMedian = linesLeftOrder.ElementAt(idxMedianLinesField).BoundingBox[0];
                    }

                    int numLinesField = lsLineMergedNotLabel.Count;
                    int idxMainFields = 0;

                    // predit y in inch and expected field for each file line  
                    foreach (Line line in lsLineMergedNotLabel)
                    {
                        double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                        System.Diagnostics.Debug.WriteLine($"line: {line.Text} EstimateCenterYInInch: {y}");
                    }

                    // check if face image is aligned right or left.
                    bool? isFaceAlignedRight = null;
                    if (rcFace != null && imageSrc != null)
                    {
                        int xHalf = imageSrc.Width / 2;
                        if (xHalf < rcFace.Value.Left)
                            isFaceAlignedRight = true;
                        else
                            isFaceAlignedRight = false;
                    }

                    try
                    {
                        if (m_labelPHUMID2_CRN.IsLabelFound && !string.IsNullOrEmpty(m_labelPHUMID2_CRN.FieldFollowing))
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {m_labelPHUMID2_CRN.FieldFollowing} --> CRN");
                            CRN = m_labelPHUMID2_CRN.FieldFollowing.Trim('-');    // remove '-' between 'CRN' and numbers
                        }
                        else
                        {
                            // filter lines near to line of CRN field
                            FindFromMergedLine(ref lsLineMergedNotLabel,
                                (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_CRN_FACE_R : CENTER_Y_INCH_CRN_FACE_L,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> CRN");
                                    CRN = l.Text;
                                    return true;
                                }));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    double? heightFieldLine = 0;
                    try
                    {
                        // filter lines near to line of sur name field
                        FindFromMergedLine(ref lsLineMergedNotLabel,
                            (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_SURNAME_FACE_R : CENTER_Y_INCH_SURNAME_FACE_L,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the label
                                if (m_labelPHUMID2_SURNAME.IsLabelFound)
                                {
                                    if (l.ExtGetVerticalCenter() <= m_labelPHUMID2_SURNAME.LineMacthed.ExtGetVerticalCenter())
                                        return false;
                                    if (!m_labelPHUMID2_SURNAME.IsFieldInLineJustUnderTheLabel(l))
                                        return false;
                                }

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> SURNAME");
                                SURNAME = l.Text;
                                heightFieldLine = l.ExtGetHeight();
                                return true;
                            }));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        // filter lines near to lien of given name field
                        FindFromMergedLine(ref lsLineMergedNotLabel,
                            (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_GIVENNAME_FACE_R : CENTER_Y_INCH_GIVENNAME_FACE_L,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the label
                                if (m_labelPHUMID2_GIVEN_NAME.IsLabelFound)
                                {
                                    if (l.ExtGetVerticalCenter() <= m_labelPHUMID2_GIVEN_NAME.LineMacthed.ExtGetVerticalCenter())
                                        return false;
                                    if (!m_labelPHUMID2_GIVEN_NAME.IsFieldInLineJustUnderTheLabel(l))
                                        return false;
                                }

                                if (heightFieldLine != null)
                                {
                                    if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                        return false;
                                }

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> GIVEN_NAME");
                                GIVEN_NAME = l.Text;
                                if (heightFieldLine != null)
                                {
                                    heightFieldLine = (heightFieldLine + l.ExtGetHeight()) / 2;
                                }
                                else
                                {
                                    heightFieldLine = l.ExtGetHeight();
                                }
                                return true;
                            }));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        // filter lines near to lien of middle name field
                        FindFromMergedLine(ref lsLineMergedNotLabel,
                            (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_MIDDLENAME_FACE_R : CENTER_Y_INCH_MIDDLENAME_FACE_L,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the label
                                if (m_labelPHUMID2_GIVEN_NAME.IsLabelFound)
                                {
                                    if (l.ExtGetVerticalCenter() <= m_labelPHUMID2_GIVEN_NAME.LineMacthed.ExtGetVerticalCenter())
                                        return false;
                                    if (!m_labelPHUMID2_GIVEN_NAME.IsFieldInLineJustUnderTheLabel(l))
                                        return false;
                                }

                                if (heightFieldLine != null)
                                {
                                    if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                        return false;
                                }

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> GIVEN_NAME");
                                GIVEN_NAME = l.Text;
                                if (heightFieldLine != null)
                                {
                                    heightFieldLine = (heightFieldLine + l.ExtGetHeight()) / 2;
                                }
                                else
                                {
                                    heightFieldLine = l.ExtGetHeight();
                                }
                                return true;
                            }));

                        FindFromMergedLine(ref lsLineMergedNotLabel,
                            (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_MIDDLENAME_FACE_R : CENTER_Y_INCH_MIDDLENAME_FACE_L,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the label
                                if (m_labelPHUMID2_MIDDLE_NAME.IsLabelFound)
                                {
                                    if (l.ExtGetVerticalCenter() <= m_labelPHUMID2_MIDDLE_NAME.LineMacthed.ExtGetVerticalCenter())
                                        return false;
                                    if (!m_labelPHUMID2_MIDDLE_NAME.IsFieldInLineJustUnderTheLabel(l))
                                        return false;
                                }

                                if (heightFieldLine != null)
                                {
                                    if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                        return false;
                                }

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> MIDDLE_NAME");
                                MIDDLE_NAME = l.Text;
                                if (heightFieldLine != null)
                                {
                                    heightFieldLine = (heightFieldLine + l.ExtGetHeight()) / 2;
                                }
                                else
                                {
                                    heightFieldLine = l.ExtGetHeight();
                                }
                                return true;
                            }));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {

                        // filter lines near to lien of sex and date of birth field
                        if (isFaceAlignedRight != null && isFaceAlignedRight.Value == true)
                        {
                            // SEX and DOB are in separated line
                            FindFromMergedLine(ref lsLineMergedNotLabel,
                                CENTER_Y_INCH_SEX_FACE_R,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is above the label 'Address'
                                    if (m_labelPHUMID2_ADDRESS.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() >= m_labelPHUMID2_ADDRESS.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                    }

                                    // split line to label and field
                                    string[] tokens = l.Text.Trim().ToUpper().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                    if (tokens != null && tokens.Length > 0)
                                    {
                                        string lastToken = tokens[tokens.Length - 1];
                                        if (lastToken == "FEMALE")
                                        {
                                            SEX = "F";
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {SEX} --> SEX");
                                            return true;
                                        }
                                        if (lastToken == "MALE")
                                        {
                                            SEX = "M";
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {SEX} --> SEX");
                                            return true;
                                        }
                                    }
                                    return false;
                                }));

                            FindFromMergedLine(ref lsLineMergedNotLabel,
                                CENTER_Y_INCH_DOB_FACE_R,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is above the label 'Address'
                                    if (m_labelPHUMID2_ADDRESS.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() >= m_labelPHUMID2_ADDRESS.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                    }

                                    // split line to label and field
                                    string[] tokens = l.Text.Trim().ToUpper().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                    if (tokens != null && tokens.Length > 0)
                                    {
                                        DOB = tokens[tokens.Length - 1];
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {DOB} --> DOB");
                                        return true;
                                    }
                                    return false;
                                }));
                        }
                        else
                        {
                            // SEX and DOB are in the same line
                            FindFromMergedLine(ref lsLineMergedNotLabel,
                                CENTER_Y_INCH_SEX_DOB_FACE_L,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is above the label 'Address'
                                    if (m_labelPHUMID2_ADDRESS.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() >= m_labelPHUMID2_ADDRESS.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                    }
                                    // split line to sex and date of birth
                                    string sex_dob = l.Text.Trim().ToUpper();
                                    string[] tokens = sex_dob.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                    for (int i = 0; i < tokens.Length; i++)
                                    {
                                        string token = tokens[i];
                                        if (token == "SEX" && tokens.Length > i + 1)
                                        {
                                            string sex_field = tokens[i + 1];
                                            if (sex_field == "F" || sex_field == "M")
                                            {
                                                i++;
                                                SEX = sex_field;
                                                continue;
                                            }
                                        }

                                        if (token == "F" || token == "M")
                                        {
                                            SEX = token;
                                            continue;
                                        }

                                        if (i == tokens.Length - 1)
                                        {
                                            DOB = token;
                                        }
                                    }

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {SEX} --> SEX");
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {DOB} --> DOB");
                                    return (!string.IsNullOrEmpty(SEX) || !string.IsNullOrEmpty(DOB));
                                }));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        // filter lines near address field
                        FindFromMergedLine(ref lsLineMergedNotLabel,
                            (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_ADDRESS1_FACE_R : CENTER_Y_INCH_ADDRESS1_FACE_L,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the label
                                if (m_labelPHUMID2_ADDRESS.IsLabelFound && (l.ExtGetVerticalCenter() < m_labelPHUMID2_ADDRESS.LineMacthed.ExtGetVerticalCenter()))
                                    return false;

                                if (heightFieldLine != null)
                                {
                                    if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                        return false;
                                }

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> ADDRESS1");
                                lineAddress1 = l;
                                if (heightFieldLine != null)
                                {
                                    heightFieldLine = (heightFieldLine + l.ExtGetHeight()) / 2;
                                }
                                else
                                {
                                    heightFieldLine = l.ExtGetHeight();
                                }
                                ADDRESS1 = lineAddress1.Text;
                                return true;
                            }));
                        FindFromMergedLine(ref lsLineMergedNotLabel,
                            (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_ADDRESS2_FACE_R : CENTER_Y_INCH_ADDRESS2_FACE_L,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the Addres1
                                if (lineAddress1 != null && (l.ExtGetVerticalCenter() <= lineAddress1.ExtGetVerticalCenter()))
                                    return false;

                                if (heightFieldLine != null)
                                {
                                    if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                        return false;
                                }

                                // the 2nd line of Address is under 1st line of address 
                                if (!IsFieldJustUnderTheLine(lineAddress1, l))
                                    return false;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> ADDRESS2");
                                lineAddress2 = l;
                                ADDRESS2 = lineAddress2.Text;
                                if (heightFieldLine != null)
                                {
                                    heightFieldLine = (heightFieldLine + l.ExtGetHeight()) / 2;
                                }
                                else
                                {
                                    heightFieldLine = l.ExtGetHeight();
                                }
                                return true;
                            }));

                        FindFromMergedLine(ref lsLineMergedNotLabel,
                            (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_ADDRESS3_FACE_R : CENTER_Y_INCH_ADDRESS3_FACE_L,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the Addres2
                                if (lineAddress2 != null && (l.ExtGetVerticalCenter() <= lineAddress2.ExtGetVerticalCenter()))
                                    return false;

                                if (heightFieldLine != null)
                                {
                                    if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                        return false;
                                }

                                // the 3rd line of Address is under 2nd line of address 
                                if (!IsFieldJustUnderTheLine(lineAddress2, l))
                                    return false;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> ADDRESS3");
                                lineAddress3 = l;
                                ADDRESS3 = lineAddress3.Text;
                                if (heightFieldLine != null)
                                {
                                    heightFieldLine = (heightFieldLine + l.ExtGetHeight()) / 2;
                                }
                                else
                                {
                                    heightFieldLine = l.ExtGetHeight();
                                }
                                return true;
                            }));

                        FindFromMergedLine(ref lsLineMergedNotLabel,
                            (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_ADDRESS4_FACE_R : CENTER_Y_INCH_ADDRESS4_FACE_L,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the Addres3
                                if (lineAddress3 != null && (l.ExtGetVerticalCenter() <= lineAddress3.ExtGetVerticalCenter()))
                                    return false;

                                if (heightFieldLine != null)
                                {
                                    if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                        return false;
                                }

                                // the 4th line of Address is under 3rd line of address 
                                if (!IsFieldJustUnderTheLine(lineAddress3, l))
                                    return false;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> ADDRESS4");
                                lineAddress4 = l;
                                ADDRESS4 = lineAddress4.Text;
                                return true;
                            }));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    // sort from top to bottom
                    //linesInMainColumn.OrderBy(l => l.BoundingBox[1]);
                }
            }

            // map to result and convert format 
            List<string> lsMissingFields = new List<string>();

            // SURNAME -> lastNameOrFullName 
            result.lastNameOrFullName = SURNAME;
            if (string.IsNullOrEmpty(SURNAME)) lsMissingFields.Add("SURNAME");

            // GIVEN_NAME -> firstName 
            result.firstName = GIVEN_NAME;
            if (string.IsNullOrEmpty(GIVEN_NAME)) lsMissingFields.Add("GIVEN_NAME");

            if (!string.IsNullOrEmpty(GIVEN_NAME2))
            {
                result.firstName = GIVEN_NAME + " " + GIVEN_NAME2;
            }

            // MIDDLE_NAME -> middleName 
            result.middleName = MIDDLE_NAME;

            // IDNUM -> documentNumber
            string tempCRN = CorrectFalseParsedNumericLine(CRN);
            if (!string.IsNullOrEmpty(tempCRN))
            {
                tempCRN = tempCRN.Replace(" ", "").ToUpper();
                tempCRN = tempCRN.Replace("CRN-", "");
                tempCRN = tempCRN.Replace("CRN", "");
            }
            result.documentNumber = tempCRN;
            if (string.IsNullOrEmpty(CRN)) lsMissingFields.Add("CRN");

            // (CITIZENSHIP) nationality is "PH" (by default)

            // SEX
            result.gender = SEX;

            // DOB "yyyy/MM/dd" -> dateOfBirth "yyyy-MM-dd"
            try
            {
                result.dateOfBirth = "";

                if (DOB.Length == 10)
                {
                    int yyyy = int.Parse(DOB.Substring(0, 4));
                    int MM = int.Parse(DOB.Substring(5, 2));
                    int dd = int.Parse(DOB.Substring(8, 2));
                    result.dateOfBirth = $"{yyyy:0000}-{MM:00}-{dd:00}";
                }
                else
                {
                    if (string.IsNullOrEmpty(DOB)) lsMissingFields.Add("DOB");
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                if (string.IsNullOrEmpty(DOB)) lsMissingFields.Add("DOB");
            }

            // ADDRESS1, ADDRESS2 -> addressLine1, addressLine2
            if (string.IsNullOrEmpty(ADDRESS1)) lsMissingFields.Add("ADDRESS1");
            // extract post code 
            if (string.IsNullOrEmpty(ADDRESS4))
            {
                if (string.IsNullOrEmpty(ADDRESS3))
                {
                    if (string.IsNullOrEmpty(ADDRESS2))
                    {
                        // 1 address line only
                        int lenAddrLast = ADDRESS1.Length;
                        string addrLast = "";
                        string last4 = "";
                        if (ADDRESS1.Length > 4)
                        {
                            addrLast = ADDRESS1.Substring(0, ADDRESS1.Length - 4);
                            last4 = ADDRESS1.Substring(ADDRESS1.Length - 4);
                        }
                        int nPostcode = 0;
                        if (int.TryParse(last4, out nPostcode))
                        {
                            result.addressLine1 = $"{addrLast}";
                            result.postcode = $"{last4}";
                        }
                        else
                        {
                            result.addressLine1 = $"{ADDRESS1}";
                        }
                    }
                    else
                    {
                        // 2 addresslines
                        int lenAddrLast = ADDRESS2.Length;
                        string addrLast = "";
                        string last4 = "";
                        if (ADDRESS2.Length > 4)
                        {
                            addrLast = ADDRESS2.Substring(0, ADDRESS2.Length - 4);
                            last4 = ADDRESS2.Substring(ADDRESS2.Length - 4);
                        }
                        int nPostcode = 0;
                        if (int.TryParse(last4, out nPostcode))
                        {
                            result.addressLine1 = $"{ADDRESS1}";
                            result.addressLine2 = $"{addrLast}";
                            result.postcode = $"{last4}";
                        }
                        else
                        {
                            result.addressLine1 = $"{ADDRESS1}";
                            result.addressLine2 = $"{ADDRESS2}";
                        }
                    }
                }
                else
                {
                    // 3 address lines
                    int lenAddrLast = ADDRESS3.Length;
                    string addrLast = "";
                    string last4 = "";
                    int nPostcode = 0;
                    if (ADDRESS3.Length <= 4)
                    {
                        // the last line may be postcode
                        last4 = ADDRESS3;
                        if (int.TryParse(last4, out nPostcode))
                        {
                            result.addressLine1 = $"{ADDRESS1}";
                            result.addressLine2 = $"{ADDRESS2}";
                            result.postcode = $"{last4}";
                        }
                        else
                        {
                            result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                            result.addressLine2 = $"{ADDRESS3}";
                        }
                    }
                    else
                    {
                        addrLast = ADDRESS3.Substring(0, ADDRESS3.Length - 4);
                        last4 = ADDRESS3.Substring(ADDRESS3.Length - 4);
                        if (int.TryParse(last4, out nPostcode))
                        {
                            result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                            result.addressLine2 = $"{addrLast}";
                            result.postcode = $"{last4}";
                        }
                        else
                        {
                            result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                            result.addressLine2 = $"{ADDRESS3}";
                        }
                    }
                }
            }
            else
            {
                // 4 address lines
                int lenAddrLast = ADDRESS4.Length;
                string addrLast = "";
                string last4 = "";
                if (ADDRESS4.Length > 4)
                {
                    addrLast = ADDRESS4.Substring(0, ADDRESS4.Length - 4);
                    last4 = ADDRESS4.Substring(ADDRESS4.Length - 4);
                }
                int nPostcode = 0;
                if (int.TryParse(last4, out nPostcode))
                {
                    result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                    result.addressLine2 = $"{ADDRESS3} {addrLast}";
                    result.postcode = $"{last4}";
                }
                else
                {
                    result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                    result.addressLine2 = $"{ADDRESS3} {ADDRESS4}";
                }
            }

            // determine success or not
            if (lsMissingFields.Count == 0)
            {
                result.Success = true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ExtractFieldsFromReadResultOfPHUMID result NOT success");
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
#else
        public static ScanPHUMIDResult ExtractFieldsFromReadResultOfPHUMID(IList<Line> linesAll, List<LabeledObject> labeledObjects, SKImage imageSrc, ImgProcLib.MatchTemplateIDCard? matchTemplatePHUMID)
        {
            // For UMID with face picture located in the right side

            //char[] SEPARATOR_NAME = new char[] { ',', '.', ' ' };

            const double CENTER_Y_INCH_CRN_FACE_R = 0.62f;
            const double CENTER_Y_INCH_SURNAME_FACE_R = 0.74f;
            const double CENTER_Y_INCH_GIVENNAME_FACE_R = 0.96f;
            const double CENTER_Y_INCH_MIDDLENAME_FACE_R = 1.18f;
            const double CENTER_Y_INCH_SEX_FACE_R = 1.40f;
            const double CENTER_Y_INCH_DOB_FACE_R = 1.52f;
            const double CENTER_Y_INCH_ADDRESS1_FACE_R = 1.74f;
            const double CENTER_Y_INCH_ADDRESS2_FACE_R = 1.84f;
            const double CENTER_Y_INCH_ADDRESS3_FACE_R = 1.93f;
            const double CENTER_Y_INCH_ADDRESS4_FACE_R = 2.03f;

            const double CENTER_Y_INCH_CRN_FACE_L = 0.63f;
            const double CENTER_Y_INCH_SURNAME_FACE_L = 0.92f;
            const double CENTER_Y_INCH_GIVENNAME_FACE_L = 1.12f;
            const double CENTER_Y_INCH_MIDDLENAME_FACE_L = 1.42f;
            const double CENTER_Y_INCH_SEX_DOB_FACE_L = 1.54f;
            const double CENTER_Y_INCH_ADDRESS1_FACE_L = 1.68f;
            const double CENTER_Y_INCH_ADDRESS2_FACE_L = 1.76f;
            const double CENTER_Y_INCH_ADDRESS3_FACE_L = 1.83f;
            const double CENTER_Y_INCH_ADDRESS4_FACE_L = 1.92f;

            Dictionary<string, MatchTemplateInfo> dicMatchTemplateInfo = new Dictionary<string, MatchTemplateInfo>();
            MatchTemplateInfo matchTmplPHUMID_670_LogoL_Gray = new MatchTemplateInfo("PHUMID_670_LogoL_Gray", "COA", 0.8f, 0.3f, 0.25f);
            dicMatchTemplateInfo.Add(matchTmplPHUMID_670_LogoL_Gray.Name, matchTmplPHUMID_670_LogoL_Gray);
            MatchTemplateInfo matchTmplPHUMID_670_LogoR_Gray = new MatchTemplateInfo("PHUMID_670_LogoR_Gray", "Flag", 0.8f, 1.75f, 0.25f);
            dicMatchTemplateInfo.Add(matchTmplPHUMID_670_LogoR_Gray.Name, matchTmplPHUMID_670_LogoR_Gray);

            ScanPHUMIDResult result = new ScanPHUMIDResult();

            string CRN = "";
            string SURNAME = "";
            string GIVEN_NAME = "";
            string GIVEN_NAME2 = "";
            string MIDDLE_NAME = "";
            string SEX = "";
            string DOB = "";
            Line lineSexDoB = null;
            string ADDRESS1 = "";
            Line lineAddress1 = null;
            string ADDRESS2 = "";
            Line lineAddress2 = null;
            string ADDRESS3 = "";
            Line lineAddress3 = null;
            string ADDRESS4 = "";
            Line lineAddress4 = null;
            string POSTCODE = "";
            const double ACCEPTABLE_DIFF_IN_LINE = 0.1f;

            List<Line> linesField = new List<Line>();   // lines valid and not label
            List<LabelInfo> labelsFound = new List<LabelInfo>();

            List<Line> lsLinesInSameLine = new List<Line>();
            Line lineMerged = null;
            List<Line> lsLineMerged = new List<Line>();
            // find labels exactly match
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

                if (lsLinesInSameLine.Count == 0)
                {
                    lsLinesInSameLine.Add(line);
                    lineMerged = line;
                    continue;
                }

                if (IsLineInTheSameLine(lineMerged, line))
                {
                    lsLinesInSameLine.Add(line);
                    lineMerged = lineMerged.MergedLine(line);
                    continue;
                }

                Line lineMergedToCheck = lineMerged;
                lineMerged = line;  // for next turn

                // find labels in line
                Line[] arLinesInSameLine = lsLinesInSameLine.ToArray();
                Line[] linesToAddFields = FindLabelInLine(ref labelsFound, arLinesInSameLine, lineMergedToCheck, labelsToFind.ToArray(), labelsAboveFields);
                lsLinesInSameLine.Clear();
                lsLinesInSameLine.Add(line);

                if (linesToAddFields != null && linesToAddFields.Length > 0)
                {
                    linesField.AddRange(linesToAddFields);
                    lsLineMerged.Add(lineMergedToCheck);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] lineMergedToCheck: {lineMergedToCheck.Text} is not field.");
                }

            }// foreach lines in other columns

            // find labels in the last line
            if (lineMerged != null)
            {
                Line[] arLinesInSameLine = lsLinesInSameLine.ToArray();
                Line[] linesToAddFields = FindLabelInLine(ref labelsFound, arLinesInSameLine, lineMerged, labelsToFind.ToArray(), labelsAboveFields);
                lsLinesInSameLine.Clear();
                if (linesToAddFields != null && linesToAddFields.Length > 0)
                {
                    linesField.AddRange(linesToAddFields);
                    lsLineMerged.Add(lineMerged);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] lineMergedToCheck: {lineMerged.Text} is not field.");
                }
            }

            // find face image and determine its alignment left or right
            bool? isFaceAlignedRight = null;
            if (labeledObjects != null && labeledObjects.Count > 0)
            {
                //'human face', 'human head', 'man'
                double? leftOfFaceImageInPixel = null;
                double? heightOfFaceImageInPixel = null;
                foreach (LabeledObject obj in labeledObjects)
                {
                    if (obj.Label == "HUMAN FACE" || obj.Label == "HUMAN HEAD" || obj.Label == "PERSON")
                    {
                        if(obj.BoundingBox != null && obj.BoundingBox.Count == 4)
                        {
                            // pick the largest one
                            double? height = obj.BoundingBox[3] - obj.BoundingBox[1];
                            if (heightOfFaceImageInPixel == null || height > heightOfFaceImageInPixel.Value)
                            {
                                leftOfFaceImageInPixel = obj.BoundingBox[0];
                                heightOfFaceImageInPixel = height;
                            }
                        }
                    }
                }

                if(leftOfFaceImageInPixel != null)
                {
                    if (labelPHUMID_REPUBLIC_OF_THE_PHILIPPINES.IsLabelFound && labelPHUMID_REPUBLIC_OF_THE_PHILIPPINES.LineMacthed.ExtGetLeft() < leftOfFaceImageInPixel)
                        isFaceAlignedRight = true;
                    else if (labelPHUMID_Unified_Multi_Purpose_ID.IsLabelFound && labelPHUMID_Unified_Multi_Purpose_ID.LineMacthed.ExtGetLeft() < leftOfFaceImageInPixel)
                        isFaceAlignedRight = true;
                    else if (labelPHUMID_SURNAME_FollowedByField.IsLabelFound && labelPHUMID_SURNAME_FollowedByField.LineMacthed.ExtGetLeft() < leftOfFaceImageInPixel)
                        isFaceAlignedRight = true;
                    else if (labelPHUMID_GIVEN_NAME_FollowedByField.IsLabelFound && labelPHUMID_GIVEN_NAME_FollowedByField.LineMacthed.ExtGetLeft() < leftOfFaceImageInPixel)
                        isFaceAlignedRight = true;
                    else if (labelPHUMID_MIDDLE_NAME_FollowedByField.IsLabelFound && labelPHUMID_MIDDLE_NAME_FollowedByField.LineMacthed.ExtGetLeft() < leftOfFaceImageInPixel)
                        isFaceAlignedRight = true;
                    else if (labelPHUMID_ADDRESS_LeftAligned.IsLabelFound && labelPHUMID_ADDRESS_LeftAligned.LineMacthed.ExtGetLeft() < leftOfFaceImageInPixel)
                        isFaceAlignedRight = true;
                    else
                        isFaceAlignedRight = false;
                }
            }

            // calc pixel per inch
            double? ppi = LabelInfo.CalcPixelPerInch(labelsFound);
            if (ppi == null)
            {
                Console.WriteLine("CalcPixelPerInch failed");
            }
            else
            {
                const double labelHeightFilterInInch = 0.08f;
                double labelHeightFilterInPixel = (double)labelHeightFilterInInch * ppi.Value;

                //
                // calc top and left edge
                //
                double? topEdgeYOfIDImageInPixel;
                double? leftEdgeXOfIDImageInPixel;
                CalcLeftTopEdgeInPixel(labelsFound, ppi, out leftEdgeXOfIDImageInPixel, out topEdgeYOfIDImageInPixel);
                if (topEdgeYOfIDImageInPixel != null && leftEdgeXOfIDImageInPixel != null)
                {
                    int widthOfIDImageInPixel = (int)(3.35f * ppi.Value);
                    int heightOfIDImageInPixel = (int)(2.15f * ppi.Value);
                    SKRectI rect = new SKRectI(
                        (int)leftEdgeXOfIDImageInPixel - (int)(ppi * 0.1),
                        (int)topEdgeYOfIDImageInPixel - (int)(ppi * 0.1),
                        (int)leftEdgeXOfIDImageInPixel + widthOfIDImageInPixel + (int)(ppi * 0.1),
                        (int)topEdgeYOfIDImageInPixel + heightOfIDImageInPixel + (int)(ppi * 0.1));
                    if (rect.Top < 0) rect.Top = 0;
                    if (rect.Left < 0) rect.Left = 0;
                    if (rect.Right > imageSrc.Width) rect.Right = imageSrc.Width;
                    if (rect.Bottom > imageSrc.Height) rect.Bottom = imageSrc.Height;
                    SKImage imageIDSrc = imageSrc.Subset(rect);
                    //SKData dataIDPng = imageIDSrc.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                    double rate = 200.0f / ppi.Value;
                    SKBitmap bmpID = SKBitmap.FromImage(imageIDSrc);
                    SKBitmap bmpID200ppi = bmpID.Resize(new SKSizeI((int)(imageIDSrc.Width * rate), (int)(imageIDSrc.Height * rate)), SKFilterQuality.High);
                    SKData dataID200ppiPng = bmpID200ppi.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                    result.CardImage200ppiPngB64 = Convert.ToBase64String(dataID200ppiPng.ToArray());
                    //using (FileStream fs = new FileStream("ID.png", FileMode.Create))
                    //{
                    //    dataID200ppiPng.SaveTo(fs);
                    //}

                    if (matchTemplatePHUMID != null)
                    {
                        MatchTemplateResult matchTemplateResult = matchTemplatePHUMID.DoMatchTemplate(dataID200ppiPng.ToArray());
                        if (matchTemplateResult != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTemplateResult.MatchResult: {matchTemplateResult.MatchResult}");
                            SKImage imgID200ppi = SKImage.FromBitmap(bmpID200ppi);

                            foreach (string key in matchTemplateResult.MatchResult.Keys)
                            {
                                MatchTemplateResultItem matchTemplateResultItem = matchTemplateResult.MatchResult[key];
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] key: {key} MatchResult: {matchTemplateResultItem.MatchResult} x: {matchTemplateResultItem.LocX} y: {matchTemplateResultItem.LocY} w: {matchTemplateResultItem.Width} h: {matchTemplateResultItem.Height}");
                                if (dicMatchTemplateInfo.ContainsKey(key))
                                {
                                    MatchTemplateResultInfo matchTemplateResultInfo = new MatchTemplateResultInfo();
                                    matchTemplateResultInfo.Title = key;
                                    matchTemplateResultInfo.MatchTemplateInfo = dicMatchTemplateInfo[key];
                                    matchTemplateResultInfo.MatchTemplateInfo.MatchResult = matchTemplateResultItem.MatchResult;
                                    matchTemplateResultInfo.MatchTemplateInfo.ResultLocX = matchTemplateResultItem.LocX;
                                    matchTemplateResultInfo.MatchTemplateInfo.ResultLocY = matchTemplateResultItem.LocY;
                                    matchTemplateResultInfo.MatchTemplateInfo.ResultWidth = matchTemplateResultItem.Width;
                                    matchTemplateResultInfo.MatchTemplateInfo.ResultHeight = matchTemplateResultItem.Height;
                                    double? dist = matchTemplateResultInfo.GetDistanceFromExpectedCenterInInch();
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] key: {key} dist: {dist} ");
                                    result.MatchTemplateResults.Add(key, matchTemplateResultInfo);
                                }
                                /*
                                using (FileStream fs = new FileStream(matchTemplateMyKadResultItem.GetName() + ".png", FileMode.Create))
                                {
                                    SKRectI rectLandmark = new SKRectI((int)matchTemplateMyKadResultItem.LocX, (int)matchTemplateMyKadResultItem.LocY, (int)matchTemplateMyKadResultItem.LocX + matchTemplateMyKadResultItem.Width, (int)matchTemplateMyKadResultItem.LocY + matchTemplateMyKadResultItem.Height);
                                    SKImage imageLandmark = imgID200ppi.Subset(rectLandmark);
                                    SKData dataLandmark = imageLandmark.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                                    dataLandmark.SaveTo(fs);
                                }
                                */
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTemplateResult is null");
                    }
                }

                /*
                List<double> lsTopEdgeYOfIDImageInPixelCalculated = new List<double>();
                foreach (LabelInfo label in labelsFound)
                {
                    double? topEdgeYOfIDImageInPixelCalculated = label.CalcTopEdgeYOfIDImageInPixel(ppi.Value);
                    if (topEdgeYOfIDImageInPixelCalculated != null)
                    {
                        lsTopEdgeYOfIDImageInPixelCalculated.Add(topEdgeYOfIDImageInPixelCalculated.Value);
                        double? y = label.PredictCenterYInPixel(ppi, topEdgeYOfIDImageInPixelCalculated);
                        System.Diagnostics.Debug.WriteLine($"{label.Title} PredictCenterYInPixel: {y}");
                    }
                }
                double? topEdgeYOfIDImageInPixel;
                if (lsTopEdgeYOfIDImageInPixelCalculated.Count > 0)
                {
                    topEdgeYOfIDImageInPixel = lsTopEdgeYOfIDImageInPixelCalculated.Average();
                }
                else
                {
                    topEdgeYOfIDImageInPixel = null;
                }
                */

                if (linesField.Count > 0)
                {
                    // remove lines predicted as label because of height
                    int removedFromLinesField = linesField.RemoveAll(l => l.ExtGetHeight() < labelHeightFilterInPixel);
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] removed {removedFromLinesField} lines predicted as label removed from linesField [height < labelHeightInPixel ({labelHeightFilterInPixel})].");
                    int removedFromLinesMerged = lsLineMerged.RemoveAll(l => l.ExtGetHeight() < labelHeightFilterInPixel);
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] removed {removedFromLinesMerged} lines predicted as label removed from lsLineMerged [height < labelHeightInPixel ({labelHeightFilterInPixel})].");
                }

                if (linesField.Count > 0)
                {
                    //var linesLeftOrder = linesField.OrderBy(l => l.BoundingBox[0]);
                    var linesLeftOrder = linesField.OrderBy(l => l.ExtGetLeft());
                    int countLinesField = linesField.Count;
                    int idxMedianLinesField = countLinesField / 2;
                    double? leftMedian = null;
                    if (linesLeftOrder != null && linesLeftOrder.Count() > 0)
                    {
                        leftMedian = linesLeftOrder.ElementAt(idxMedianLinesField).BoundingBox[0];
                    }

                    int numLinesField = linesField.Count;
                    int idxMainFields = 0;

                    // predit y in inch and expected field for each file line  
                    foreach (Line line in linesField)
                    {
                        double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                        System.Diagnostics.Debug.WriteLine($"line: {line.Text} EstimateCenterYInInch: {y}");
                    }

                    try
                    {
                        if(labelPHUMID_CRN.IsLabelFound && !string.IsNullOrEmpty(labelPHUMID_CRN.FieldFollowing))
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {labelPHUMID_CRN.FieldFollowing} --> CRN");
                            CRN = labelPHUMID_CRN.FieldFollowing.Trim('-');    // remove '-' between 'CRN' and numbers
                        }
                        else
                        {
                            // filter lines near to line of CRN field
                            FindFromMergedLine(ref lsLineMerged,
                                (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_CRN_FACE_R : CENTER_Y_INCH_CRN_FACE_L, 
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> CRN");
                                    CRN = l.Text;
                                    return true;
                                }));
                            /*
                            Line[] mergedLinesNearToCRN = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_CRN - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToCRN)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToCRN: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_CRN)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> CRN");
                                    CRN = line.Text;
                                    lsLineMerged.Remove(line);
                                    break;
                                }
                            }
                            */
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    double? heightFieldLine = 0;
                    try
                    {
                        if(labelPHUMID_SURNAME_FollowedByField.IsLabelFound && !string.IsNullOrEmpty(labelPHUMID_SURNAME_FollowedByField.FieldFollowing))
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {labelPHUMID_SURNAME_FollowedByField.FieldFollowing} --> SURNAME");
                            SURNAME = labelPHUMID_SURNAME_FollowedByField.FieldFollowing;
                            heightFieldLine = labelPHUMID_SURNAME_FollowedByField.LineMacthed.ExtGetHeight();
                        }
                        else
                        {
                            // filter lines near to line of sur name field
                            FindFromMergedLine(ref lsLineMerged,
                                (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_SURNAME_FACE_R : CENTER_Y_INCH_SURNAME_FACE_L, 
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is under the label
                                    if (labelPHUMID_SURNAME.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() <= labelPHUMID_SURNAME.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                        if (!labelPHUMID_SURNAME.IsFieldInLineJustUnderTheLabel(l))
                                            return false;
                                    }

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> SURNAME");
                                    SURNAME = l.Text;
                                    heightFieldLine = l.ExtGetHeight();
                                    return true;
                                }));
                            /*
                            double centerYInInchOfField_SurName = CENTER_Y_INCH_SURNAME_FACE_L;
                            if (isFaceAlignedRight != null && isFaceAlignedRight.Value == true)
                            {
                                centerYInInchOfField_SurName = CENTER_Y_INCH_SURNAME_FACE_R;
                            }
                            Line[] mergedLinesNearToSurName = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_SurName - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToSurName)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToSurName: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_SurName)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is under the label
                                    if (labelSURNAME.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() <= labelSURNAME.LineMacthed.ExtGetVerticalCenter())
                                            continue;
                                        if (!labelSURNAME.IsFieldInLineJustUnderTheLabel(line))
                                            continue;
                                    }

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> SURNAME");
                                    SURNAME = line.Text;
                                    lsLineMerged.Remove(line);
                                    heightFieldLine = line.ExtGetHeight();
                                    break;
                                }
                            }
                            */
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        if(labelPHUMID_GIVEN_NAME_FollowedByField.IsLabelFound && !string.IsNullOrEmpty(labelPHUMID_GIVEN_NAME_FollowedByField.FieldFollowing))
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {labelPHUMID_GIVEN_NAME_FollowedByField.FieldFollowing} --> GIVEN_NAME");
                            GIVEN_NAME = labelPHUMID_GIVEN_NAME_FollowedByField.FieldFollowing;
                            if (heightFieldLine != null)
                            {
                                heightFieldLine = (heightFieldLine + labelPHUMID_GIVEN_NAME_FollowedByField.LineMacthed.ExtGetHeight()) / 2;
                            }
                            else
                            {
                                heightFieldLine = labelPHUMID_GIVEN_NAME_FollowedByField.LineMacthed.ExtGetHeight();
                            }
                        }
                        else
                        {
                            // filter lines near to lien of given name field
                            FindFromMergedLine(ref lsLineMerged,
                                (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_GIVENNAME_FACE_R : CENTER_Y_INCH_GIVENNAME_FACE_L, 
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is under the label
                                    if (labelPHUMID_GIVEN_NAME.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() <= labelPHUMID_GIVEN_NAME.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                        if (!labelPHUMID_GIVEN_NAME.IsFieldInLineJustUnderTheLabel(l))
                                            return false;
                                    }

                                    if (heightFieldLine != null)
                                    {
                                        if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                            return false;
                                    }

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> GIVEN_NAME");
                                    GIVEN_NAME = l.Text;
                                    if (heightFieldLine != null)
                                    {
                                        heightFieldLine = (heightFieldLine + l.ExtGetHeight()) / 2;
                                    }
                                    else
                                    {
                                        heightFieldLine = l.ExtGetHeight();
                                    }
                                    return true;
                                }));
                            /*
                            double centerYInInchOfField_GivenName = CENTER_Y_INCH_GIVENNAME_FACE_L;
                            if (isFaceAlignedRight != null && isFaceAlignedRight.Value == true)
                            {
                                centerYInInchOfField_GivenName = CENTER_Y_INCH_GIVENNAME_FACE_R;
                            }
                            Line[] mergedLinesNearToGivedName = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_GivenName - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToGivedName)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToGivedName: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_GivenName)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is under the label
                                    if (labelGIVEN_NAME.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() <= labelGIVEN_NAME.LineMacthed.ExtGetVerticalCenter())
                                            continue;
                                        if (!labelGIVEN_NAME.IsFieldInLineJustUnderTheLabel(line))
                                            continue;
                                    }

                                    if (heightFieldLine != null)
                                    {
                                        if (line.ExtGetHeight() < heightFieldLine * 0.75)
                                            continue;
                                    }

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> GIVEN_NAME");
                                    GIVEN_NAME = line.Text;
                                    lsLineMerged.Remove(line);
                                    if (heightFieldLine != null)
                                    {
                                        heightFieldLine = (heightFieldLine + line.ExtGetHeight()) / 2;
                                    }
                                    else
                                    {
                                        heightFieldLine = line.ExtGetHeight();
                                    }
                                    break;
                                }
                            }
                            */
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        if(labelPHUMID_MIDDLE_NAME_FollowedByField.IsLabelFound && !string.IsNullOrEmpty(labelPHUMID_MIDDLE_NAME_FollowedByField.FieldFollowing))
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {labelPHUMID_MIDDLE_NAME_FollowedByField.FieldFollowing} --> MIDDLE_NAME");
                            MIDDLE_NAME = labelPHUMID_MIDDLE_NAME_FollowedByField.FieldFollowing;
                            if (heightFieldLine != null)
                            {
                                heightFieldLine = (heightFieldLine + labelPHUMID_MIDDLE_NAME_FollowedByField.LineMacthed.ExtGetHeight()) / 2;
                            }
                            else
                            {
                                heightFieldLine = labelPHUMID_MIDDLE_NAME_FollowedByField.LineMacthed.ExtGetHeight();
                            }
                        }
                        else
                        {
                            // filter lines near to lien of middle name field
                            FindFromMergedLine(ref lsLineMerged,
                                (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_MIDDLENAME_FACE_R : CENTER_Y_INCH_MIDDLENAME_FACE_L, 
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is under the label
                                    if (labelPHUMID_GIVEN_NAME.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() <= labelPHUMID_GIVEN_NAME.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                        if (!labelPHUMID_GIVEN_NAME.IsFieldInLineJustUnderTheLabel(l))
                                            return false;
                                    }

                                    if (heightFieldLine != null)
                                    {
                                        if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                            return false;
                                    }

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> GIVEN_NAME");
                                    GIVEN_NAME = l.Text;
                                    if (heightFieldLine != null)
                                    {
                                        heightFieldLine = (heightFieldLine + l.ExtGetHeight()) / 2;
                                    }
                                    else
                                    {
                                        heightFieldLine = l.ExtGetHeight();
                                    }
                                    return true;
                                }));

                            FindFromMergedLine(ref lsLineMerged,
                                (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_MIDDLENAME_FACE_R : CENTER_Y_INCH_MIDDLENAME_FACE_L,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is under the label
                                    if (labelPHUMID_MIDDLE_NAME.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() <= labelPHUMID_MIDDLE_NAME.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                        if (!labelPHUMID_MIDDLE_NAME.IsFieldInLineJustUnderTheLabel(l))
                                            return false;
                                    }

                                    if (heightFieldLine != null)
                                    {
                                        if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                            return false;
                                    }

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> MIDDLE_NAME");
                                    MIDDLE_NAME = l.Text;
                                    if (heightFieldLine != null)
                                    {
                                        heightFieldLine = (heightFieldLine + l.ExtGetHeight()) / 2;
                                    }
                                    else
                                    {
                                        heightFieldLine = l.ExtGetHeight();
                                    }
                                    return true;
                                }));
                            /*
                            double centerYInInchOfField_MiddleName = CENTER_Y_INCH_MIDDLENAME_FACE_L;
                            if (isFaceAlignedRight != null && isFaceAlignedRight.Value == true)
                            {
                                centerYInInchOfField_MiddleName = CENTER_Y_INCH_MIDDLENAME_FACE_R;
                            }
                            Line[] mergedLinesNearToMiddleName = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_MiddleName - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToMiddleName)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToMiddleName: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_MiddleName)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is under the label
                                    if (labelMIDDLE_NAME.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() <= labelMIDDLE_NAME.LineMacthed.ExtGetVerticalCenter())
                                            continue;
                                        if (!labelMIDDLE_NAME.IsFieldInLineJustUnderTheLabel(line))
                                            continue;
                                    }

                                    if (heightFieldLine != null)
                                    {
                                        if (line.ExtGetHeight() < heightFieldLine * 0.75)
                                            continue;
                                    }

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> MIDDLE_NAME");
                                    MIDDLE_NAME = line.Text;
                                    lsLineMerged.Remove(line);
                                    if (heightFieldLine != null)
                                    {
                                        heightFieldLine = (heightFieldLine + line.ExtGetHeight()) / 2;
                                    }
                                    else
                                    {
                                        heightFieldLine = line.ExtGetHeight();
                                    }
                                    break;
                                }
                            }
                            */
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {

                        // filter lines near to lien of sex and date of birth field
                        if (isFaceAlignedRight != null && isFaceAlignedRight.Value == true)
                        {
                            // SEX and DOB are in separated line
                            FindFromMergedLine(ref lsLineMerged,
                                CENTER_Y_INCH_SEX_FACE_R,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is above the label 'Address'
                                    if (labelPHUMID_ADDRESS.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() >= labelPHUMID_ADDRESS.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                    }
                                    if (labelPHUMID_ADDRESS_LeftAligned.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() >= labelPHUMID_ADDRESS_LeftAligned.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                    }

                                    // split line to label and field
                                    string[] tokens = l.Text.Trim().ToUpper().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                    if (tokens != null && tokens.Length > 0)
                                    {
                                        string lastToken = tokens[tokens.Length - 1];
                                        if (lastToken == "FEMALE")
                                        {
                                            SEX = "F";
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {SEX} --> SEX");
                                            return true;
                                        }
                                        if (lastToken == "MALE")
                                        {
                                            SEX = "M";
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {SEX} --> SEX");
                                            return true;
                                        }
                                    }
                                    return false;
                                }));

                            /*
                            Line[] mergedLinesNearToSex = lsLineMerged.OrderBy(l => Math.Abs((decimal)(CENTER_Y_INCH_SEX_FACE_R - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToSex)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToSex: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - CENTER_Y_INCH_SEX_FACE_R)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is above the label 'Address'
                                    if (labelADDRESS.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() >= labelADDRESS.LineMacthed.ExtGetVerticalCenter())
                                            continue;
                                    }
                                    if (labelADDRESS_LeftAligned.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() >= labelADDRESS_LeftAligned.LineMacthed.ExtGetVerticalCenter())
                                            continue;
                                    }

                                    // split line to label and field
                                    string[] tokens = line.Text.Trim().ToUpper().Split(' ');
                                    if(tokens != null && tokens.Length > 0)
                                    {
                                        string lastToken = tokens[tokens.Length - 1];
                                        if (lastToken == "FEMALE")
                                        {
                                            SEX = "F";
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {SEX} --> SEX");
                                            lsLineMerged.Remove(line);
                                            break;
                                        }
                                        if (lastToken == "MALE")
                                        {
                                            SEX = "M";
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {SEX} --> SEX");
                                            lsLineMerged.Remove(line);
                                            break;
                                        }
                                    }
                                }
                            }
                            */

                            FindFromMergedLine(ref lsLineMerged,
                                CENTER_Y_INCH_DOB_FACE_R,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is above the label 'Address'
                                    if (labelPHUMID_ADDRESS.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() >= labelPHUMID_ADDRESS.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                    }
                                    if (labelPHUMID_ADDRESS_LeftAligned.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() >= labelPHUMID_ADDRESS_LeftAligned.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                    }

                                    // split line to label and field
                                    string[] tokens = l.Text.Trim().ToUpper().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                    if (tokens != null && tokens.Length > 0)
                                    {
                                        DOB = tokens[tokens.Length - 1];
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {DOB} --> DOB");
                                        return true;
                                    }
                                    return false;
                                }));
                            /*    
                            Line[] mergedLinesNearToDoB = lsLineMerged.OrderBy(l => Math.Abs((decimal)(CENTER_Y_INCH_DOB_FACE_R - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToDoB)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToDoB: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - CENTER_Y_INCH_DOB_FACE_R)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is above the label 'Address'
                                    if (labelADDRESS.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() >= labelADDRESS.LineMacthed.ExtGetVerticalCenter())
                                            continue;
                                    }
                                    if (labelADDRESS_LeftAligned.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() >= labelADDRESS_LeftAligned.LineMacthed.ExtGetVerticalCenter())
                                            continue;
                                    }

                                    // split line to label and field
                                    string[] tokens = line.Text.Trim().ToUpper().Split(' ');
                                    if(tokens != null && tokens.Length > 0)
                                    {
                                        DOB = tokens[tokens.Length - 1];
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {DOB} --> DOB");
                                        break;
                                    }
                                }
                            }
                            */
                        }
                        else
                        {
                            // SEX and DOB are in the same line
                            FindFromMergedLine(ref lsLineMerged,
                                CENTER_Y_INCH_SEX_DOB_FACE_L,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is above the label 'Address'
                                    if (labelPHUMID_ADDRESS.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() >= labelPHUMID_ADDRESS.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                    }
                                    if (labelPHUMID_ADDRESS_LeftAligned.IsLabelFound)
                                    {
                                        if (l.ExtGetVerticalCenter() >= labelPHUMID_ADDRESS_LeftAligned.LineMacthed.ExtGetVerticalCenter())
                                            return false;
                                    }

                                    // split line to sex and date of birth
                                    string sex_dob = l.Text.Trim().ToUpper();
                                    string[] tokens = sex_dob.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                    for (int i = 0; i < tokens.Length; i++)
                                    {
                                        string token = tokens[i];
                                        if (token == "SEX" && tokens.Length > i + 1)
                                        {
                                            string sex_field = tokens[i + 1];
                                            if (sex_field == "F" || sex_field == "M")
                                            {
                                                i++;
                                                SEX = sex_field;
                                                continue;
                                            }
                                        }

                                        if (token == "F" || token == "M")
                                        {
                                            SEX = token;
                                            continue;
                                        }

                                        if (i == tokens.Length - 1)
                                        {
                                            DOB = token;
                                        }
                                    }

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {SEX} --> SEX");
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {DOB} --> DOB");
                                    return (!string.IsNullOrEmpty(SEX) || !string.IsNullOrEmpty(DOB));
                                }));

                            /*
                            double centerYInInchOfField_SexDateOfBirth = CENTER_Y_INCH_SEX_DOB_FACE_L;
                            Line[] mergedLinesNearToSexDoB = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_SexDateOfBirth - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToSexDoB)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToSexDoB: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_SexDateOfBirth)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is above the label 'Address'
                                    if (labelADDRESS.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() >= labelADDRESS.LineMacthed.ExtGetVerticalCenter())
                                            continue;
                                    }
                                    if (labelADDRESS_LeftAligned.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() >= labelADDRESS_LeftAligned.LineMacthed.ExtGetVerticalCenter())
                                            continue;
                                    }

                                    // split line to sex and date of birth
                                    string sex_dob = line.Text.Trim().ToUpper();
                                    string[] tokens = sex_dob.Split(' ');
                                    for (int i = 0; i < tokens.Length; i++)
                                    {
                                        string token = tokens[i];
                                        if (token == "SEX" && tokens.Length > i + 1)
                                        {
                                            string sex_field = tokens[i + 1];
                                            if (sex_field == "F" || sex_field == "M")
                                            {
                                                i++;
                                                SEX = sex_field;
                                                continue;
                                            }
                                        }

                                        if (token == "F" || token == "M")
                                        {
                                            SEX = token;
                                            continue;
                                        }

                                        if (i == tokens.Length - 1)
                                        {
                                            DOB = token;
                                        }
                                    }

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {SEX} --> SEX");
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {DOB} --> DOB");
                                    if (!string.IsNullOrEmpty(SEX) || !string.IsNullOrEmpty(DOB))
                                    {
                                        lsLineMerged.Remove(line);
                                    }
                                    break;
                                }
                            }
                            */
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        // filter lines near address field
                        FindFromMergedLine(ref lsLineMerged,
                            (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_ADDRESS1_FACE_R : CENTER_Y_INCH_ADDRESS1_FACE_L,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the label
                                if (labelPHUMID_ADDRESS.IsLabelFound && (l.ExtGetVerticalCenter() < labelPHUMID_ADDRESS.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (labelPHUMID_ADDRESS_LeftAligned.IsLabelFound && (l.ExtGetVerticalCenter() < labelPHUMID_ADDRESS_LeftAligned.LineMacthed.ExtGetVerticalCenter()))
                                    return false;

                                if (heightFieldLine != null)
                                {
                                    if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                        return false;
                                }

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> ADDRESS1");
                                lineAddress1 = l;
                                if (heightFieldLine != null)
                                {
                                    heightFieldLine = (heightFieldLine + l.ExtGetHeight()) / 2;
                                }
                                else
                                {
                                    heightFieldLine = l.ExtGetHeight();
                                }
                                ADDRESS1 = lineAddress1.Text;
                                return true;
                            }));
                        /*
                        // filter lines near to lien of address field
                        double centerYInInchOfField_Address1 = CENTER_Y_INCH_ADDRESS1_FACE_L;
                        if (isFaceAlignedRight != null && isFaceAlignedRight.Value == true)
                        {
                            centerYInInchOfField_Address1 = CENTER_Y_INCH_ADDRESS1_FACE_R;
                        }
                        Line[] mergedLinesNearToAddress1 = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address1 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress1)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress1: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address1)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the label
                                if (labelADDRESS.IsLabelFound && (line.ExtGetVerticalCenter() < labelADDRESS.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (labelADDRESS_LeftAligned.IsLabelFound && (line.ExtGetVerticalCenter() < labelADDRESS_LeftAligned.LineMacthed.ExtGetVerticalCenter()))
                                    continue;

                                if (heightFieldLine != null)
                                {
                                    if (line.ExtGetHeight() < heightFieldLine * 0.75)
                                        continue;
                                }

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS1");
                                lineAddress1 = line;
                                lsLineMerged.Remove(line);
                                if (heightFieldLine != null)
                                {
                                    heightFieldLine = (heightFieldLine + line.ExtGetHeight()) / 2;
                                }
                                else
                                {
                                    heightFieldLine = line.ExtGetHeight();
                                }
                                ADDRESS1 = lineAddress1.Text;
                                break;
                            }
                        }
                        */
                        FindFromMergedLine(ref lsLineMerged,
                            (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_ADDRESS2_FACE_R : CENTER_Y_INCH_ADDRESS2_FACE_L,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the Addres1
                                if (lineAddress1 != null && (l.ExtGetVerticalCenter() <= lineAddress1.ExtGetVerticalCenter()))
                                    return false;

                                if (heightFieldLine != null)
                                {
                                    if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                        return false;
                                }

                                // the 2nd line of Address is under 1st line of address 
                                if (!IsFieldJustUnderTheLine(lineAddress1, l))
                                    return false;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> ADDRESS2");
                                lineAddress2 = l;
                                ADDRESS2 = lineAddress2.Text;
                                if (heightFieldLine != null)
                                {
                                    heightFieldLine = (heightFieldLine + l.ExtGetHeight()) / 2;
                                }
                                else
                                {
                                    heightFieldLine = l.ExtGetHeight();
                                }
                                return true;
                            }));

                        /*
                        double centerYInInchOfField_Address2 = CENTER_Y_INCH_ADDRESS2_FACE_L;
                        if (isFaceAlignedRight != null && isFaceAlignedRight.Value == true)
                        {
                            centerYInInchOfField_Address2 = CENTER_Y_INCH_ADDRESS2_FACE_R;
                        }
                        Line[] mergedLinesNearToAddress2 = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address2 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress2)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress2: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address2)) <= (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the Addres1
                                if (lineAddress1 != null && (line.ExtGetVerticalCenter() <= lineAddress1.ExtGetVerticalCenter()))
                                    continue;

                                if (heightFieldLine != null)
                                {
                                    if (line.ExtGetHeight() < heightFieldLine * 0.75)
                                        continue;
                                }

                                // the 2nd line of Address is under 1st line of address 
                                if (IsFieldJustUnderTheLine(lineAddress1, line))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS2");
                                    lineAddress2 = line;
                                    lsLineMerged.Remove(line);
                                    ADDRESS2 = lineAddress2.Text;
                                    if (heightFieldLine != null)
                                    {
                                        heightFieldLine = (heightFieldLine + line.ExtGetHeight()) / 2;
                                    }
                                    else
                                    {
                                        heightFieldLine = line.ExtGetHeight();
                                    }
                                    break;
                                }
                            }
                        }
                        */

                        FindFromMergedLine(ref lsLineMerged,
                            (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_ADDRESS3_FACE_R : CENTER_Y_INCH_ADDRESS3_FACE_L,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the Addres2
                                if (lineAddress2 != null && (l.ExtGetVerticalCenter() <= lineAddress2.ExtGetVerticalCenter()))
                                    return false;

                                if (heightFieldLine != null)
                                {
                                    if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                        return false;
                                }

                                // the 3rd line of Address is under 2nd line of address 
                                if (!IsFieldJustUnderTheLine(lineAddress2, l))
                                    return false;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> ADDRESS3");
                                lineAddress3 = l;
                                ADDRESS3 = lineAddress3.Text;
                                if (heightFieldLine != null)
                                {
                                    heightFieldLine = (heightFieldLine + l.ExtGetHeight()) / 2;
                                }
                                else
                                {
                                    heightFieldLine = l.ExtGetHeight();
                                }
                                return true;
                            }));

                        /*
                        double centerYInInchOfField_Address3 = CENTER_Y_INCH_ADDRESS3_FACE_L;
                        if (isFaceAlignedRight != null && isFaceAlignedRight.Value == true)
                        {
                            centerYInInchOfField_Address3 = CENTER_Y_INCH_ADDRESS3_FACE_R;
                        }
                        Line[] mergedLinesNearToAddress3 = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address3 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress3)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress3: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address3)) <= (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the Addres2
                                if (lineAddress2 != null && (line.ExtGetVerticalCenter() <= lineAddress2.ExtGetVerticalCenter()))
                                    continue;

                                if (heightFieldLine != null)
                                {
                                    if (line.ExtGetHeight() < heightFieldLine * 0.75)
                                        continue;
                                }

                                // the 3rd line of Address is under 2nd line of address 
                                if (IsFieldJustUnderTheLine(lineAddress2, line))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS3");
                                    lineAddress3 = line;
                                    lsLineMerged.Remove(line);
                                    ADDRESS3 = lineAddress3.Text;
                                    if (heightFieldLine != null)
                                    {
                                        heightFieldLine = (heightFieldLine + line.ExtGetHeight()) / 2;
                                    }
                                    else
                                    {
                                        heightFieldLine = line.ExtGetHeight();
                                    }
                                    break;
                                }
                            }
                        }
                        */

                        FindFromMergedLine(ref lsLineMerged,
                            (isFaceAlignedRight != null && isFaceAlignedRight.Value == true) ? CENTER_Y_INCH_ADDRESS4_FACE_R : CENTER_Y_INCH_ADDRESS4_FACE_L,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the Addres3
                                if (lineAddress3 != null && (l.ExtGetVerticalCenter() <= lineAddress3.ExtGetVerticalCenter()))
                                    return false;

                                if (heightFieldLine != null)
                                {
                                    if (l.ExtGetHeight() < heightFieldLine * 0.75)
                                        return false;
                                }

                                // the 4th line of Address is under 3rd line of address 
                                if (!IsFieldJustUnderTheLine(lineAddress3, l))
                                    return false;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> ADDRESS4");
                                lineAddress4 = l;
                                ADDRESS4 = lineAddress4.Text;
                                return true;
                            }));
                        /*
                        double centerYInInchOfField_Address4 = CENTER_Y_INCH_ADDRESS4_FACE_L;
                        if (isFaceAlignedRight != null && isFaceAlignedRight.Value == true)
                        {
                            centerYInInchOfField_Address4 = CENTER_Y_INCH_ADDRESS4_FACE_R;
                        }
                        Line[] mergedLinesNearToAddress4 = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address4 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress4)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress4: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address4)) <= (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the Addres3
                                if (lineAddress3 != null && (line.ExtGetVerticalCenter() <= lineAddress3.ExtGetVerticalCenter()))
                                    continue;

                                if (heightFieldLine != null)
                                {
                                    if (line.ExtGetHeight() < heightFieldLine * 0.75)
                                        continue;
                                }

                                // the 4th line of Address is under 3rd line of address 
                                if (IsFieldJustUnderTheLine(lineAddress3, line))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS4");
                                    lineAddress4 = line;
                                    lsLineMerged.Remove(line);
                                    ADDRESS4 = lineAddress4.Text;
                                    break;
                                }
                            }
                        }
                        */
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    // sort from top to bottom
                    //linesInMainColumn.OrderBy(l => l.BoundingBox[1]);
                }
            }

            // map to result and convert format 
            List<string> lsMissingFields = new List<string>();

            // SURNAME -> lastNameOrFullName 
            result.lastNameOrFullName = SURNAME;
            if (string.IsNullOrEmpty(SURNAME)) lsMissingFields.Add("SURNAME");

            // GIVEN_NAME -> firstName 
            result.firstName = GIVEN_NAME;
            if (string.IsNullOrEmpty(GIVEN_NAME)) lsMissingFields.Add("GIVEN_NAME");

            if (!string.IsNullOrEmpty(GIVEN_NAME2))
            {
                result.firstName = GIVEN_NAME + " " + GIVEN_NAME2;
            }

            // MIDDLE_NAME -> middleName 
            result.middleName = MIDDLE_NAME;

            // IDNUM -> documentNumber
            string tempCRN = CorrectFalseParsedNumericLine(CRN);
            if (!string.IsNullOrEmpty(tempCRN))
            {
                tempCRN = tempCRN.Replace(" ", "").ToUpper();
                tempCRN = tempCRN.Replace("CRN-", "");
                tempCRN = tempCRN.Replace("CRN", "");
            }
            result.documentNumber = tempCRN;
            if (string.IsNullOrEmpty(CRN)) lsMissingFields.Add("CRN");

            // (CITIZENSHIP) nationality is "PH" (by default)

            // SEX
            result.gender = SEX;

            // DOB "yyyy/MM/dd" -> dateOfBirth "yyyy-MM-dd"
            try
            {
                result.dateOfBirth = "";

                if (DOB.Length == 10)
                {
                    int yyyy = int.Parse(DOB.Substring(0, 4));
                    int MM = int.Parse(DOB.Substring(5, 2));
                    int dd = int.Parse(DOB.Substring(8, 2));
                    result.dateOfBirth = $"{yyyy:0000}-{MM:00}-{dd:00}";
                }
                else
                {
                    if (string.IsNullOrEmpty(DOB)) lsMissingFields.Add("DOB");
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                if (string.IsNullOrEmpty(DOB)) lsMissingFields.Add("DOB");
            }

            // ADDRESS1, ADDRESS2 -> addressLine1, addressLine2
            if (string.IsNullOrEmpty(ADDRESS1)) lsMissingFields.Add("ADDRESS1");
            // extract post code 
            if (string.IsNullOrEmpty(ADDRESS4))
            {
                if (string.IsNullOrEmpty(ADDRESS3))
                {
                    if (string.IsNullOrEmpty(ADDRESS2))
                    {
                        // 1 address line only
                        int lenAddrLast = ADDRESS1.Length;
                        string addrLast = "";
                        string last4 = "";
                        if (ADDRESS1.Length > 4)
                        {
                            addrLast = ADDRESS1.Substring(0, ADDRESS1.Length - 4);
                            last4 = ADDRESS1.Substring(ADDRESS1.Length - 4);
                        }
                        int nPostcode = 0;
                        if (int.TryParse(last4, out nPostcode))
                        {
                            result.addressLine1 = $"{addrLast}";
                            result.postcode = $"{last4}";
                        }
                        else
                        {
                            result.addressLine1 = $"{ADDRESS1}";
                        }
                    }
                    else
                    {
                        // 2 addresslines
                        int lenAddrLast = ADDRESS2.Length;
                        string addrLast = "";
                        string last4 = "";
                        if (ADDRESS2.Length > 4)
                        {
                            addrLast = ADDRESS2.Substring(0, ADDRESS2.Length - 4);
                            last4 = ADDRESS2.Substring(ADDRESS2.Length - 4);
                        }
                        int nPostcode = 0;
                        if (int.TryParse(last4, out nPostcode))
                        {
                            result.addressLine1 = $"{ADDRESS1}";
                            result.addressLine2 = $"{addrLast}";
                            result.postcode = $"{last4}";
                        }
                        else
                        {
                            result.addressLine1 = $"{ADDRESS1}";
                            result.addressLine2 = $"{ADDRESS2}";
                        }
                    }
                }
                else
                {
                    // 3 address lines
                    int lenAddrLast = ADDRESS3.Length;
                    string addrLast = "";
                    string last4 = "";
                    int nPostcode = 0;
                    if (ADDRESS3.Length <= 4)
                    {
                        // the last line may be postcode
                        last4 = ADDRESS3;
                        if (int.TryParse(last4, out nPostcode))
                        {
                            result.addressLine1 = $"{ADDRESS1}";
                            result.addressLine2 = $"{ADDRESS2}";
                            result.postcode = $"{last4}";
                        }
                        else
                        {
                            result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                            result.addressLine2 = $"{ADDRESS3}";
                        }
                    }
                    else
                    {
                        addrLast = ADDRESS3.Substring(0, ADDRESS3.Length - 4);
                        last4 = ADDRESS3.Substring(ADDRESS3.Length - 4);
                        if (int.TryParse(last4, out nPostcode))
                        {
                            result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                            result.addressLine2 = $"{addrLast}";
                            result.postcode = $"{last4}";
                        }
                        else
                        {
                            result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                            result.addressLine2 = $"{ADDRESS3}";
                        }
                    }
                }
            }
            else
            {
                // 4 address lines
                int lenAddrLast = ADDRESS4.Length;
                string addrLast = "";
                string last4 = "";
                if (ADDRESS4.Length > 4)
                {
                    addrLast = ADDRESS4.Substring(0, ADDRESS4.Length - 4);
                    last4 = ADDRESS4.Substring(ADDRESS4.Length - 4);
                }
                int nPostcode = 0;
                if (int.TryParse(last4, out nPostcode))
                {
                    result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                    result.addressLine2 = $"{ADDRESS3} {addrLast}";
                    result.postcode = $"{last4}";
                }
                else
                {
                    result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                    result.addressLine2 = $"{ADDRESS3} {ADDRESS4}";
                }
            }

            // determine success or not
            if (lsMissingFields.Count == 0)
            {
                result.Success = true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ExtractFieldsFromReadResultOfPHUMID result NOT success");
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
#endif
#if true
        ScanPHNIResult ExtractFieldsFromReadResultOfPHNI(LabelInfo[] labelsFound, Line[] linesMergedNotLabel, SKImage imageSrc, ImgProcLib.MatchTemplateIDCard? matchTemplatePHNI)
        {
            const double ACCEPTABLE_DIFF_IN_LINE = 0.1f;
            ScanPHNIResult result = new ScanPHNIResult();

            List<Line> lsLineMergedNotLabel = new List<Line>();
            lsLineMergedNotLabel.AddRange(linesMergedNotLabel);

            double centerYInInchOfField_PCN = 0.649f;
            double centerYInInchOfField_LastName = 0.885f;
            double centerYInInchOfField_GivenNames = 1.121f;
            double centerYInInchOfField_MiddleName = 1.446f;
            double centerYInInchOfField_DoB = 1.68f;
            double centerYInInchOfField_Address1 = 1.92f;
            double centerYInInchOfField_Address2 = 2.03f;

            Dictionary<string, MatchTemplateInfo> dicMatchTemplateInfo = new Dictionary<string, MatchTemplateInfo>();
            MatchTemplateInfo matchTmplPHNI_670_COA = new MatchTemplateInfo("PHNI_670_COA", "COA", 0.8f, 0.5f, 0.3f);
            dicMatchTemplateInfo.Add("PHNI_670_COA", matchTmplPHNI_670_COA);
            MatchTemplateInfo matchTmplPHNI_670_Logo_Fingerprint = new MatchTemplateInfo("PHNI_670_Logo_Fingerprint", "Logo_Fingerprint", 0.8f, 3.0f, 0.30f);
            dicMatchTemplateInfo.Add("PHNI_670_Logo_Fingerprint", matchTmplPHNI_670_Logo_Fingerprint);
            MatchTemplateInfo matchTmplPHNI_670_PHL = new MatchTemplateInfo("PHNI_670_PHL", "PHL", 0.8f, 3.18f, 1.726f);
            dicMatchTemplateInfo.Add("PHNI_670_PHL", matchTmplPHNI_670_PHL);
            MatchTemplateInfo matchTmplPHNI_670_Logo_Right_Bottom = new MatchTemplateInfo("PHNI_670_Logo_Right_Bottom", "Logo_Right_Bottom", 0.8f, 3.18f, 1.97f);
            dicMatchTemplateInfo.Add("PHNI_670_Logo_Right_Bottom", matchTmplPHNI_670_Logo_Right_Bottom);
            MatchTemplateInfo matchTmplPHNI_670_PSA_Watermark = new MatchTemplateInfo("PHNI_670_PSA_Watermark", "PSA Watermark", 0.8f, 2.714f, 1.79f);
            dicMatchTemplateInfo.Add("PHNI_670_PSA_Watermark", matchTmplPHNI_670_PSA_Watermark);

            string PCN = "";
            string LAST_NAME = "";
            string GIVEN_NAMES = "";
            string MIDDLE_NAME = "";
            string DOB = "";
            string ADDRESS1 = "";
            string ADDRESS2 = "";

            // calc pixel per inch
            double? ppi = LabelInfo.CalcPixelPerInch(labelsFound);
            if (ppi == null)
            {
                Console.WriteLine("CalcPixelPerInch failed");
            }
            else
            {
                const double labelHeightFilterInInch = 0.08f;
                double labelHeightFilterInPixel = (double)labelHeightFilterInInch * ppi.Value;

                //
                // calc top and left edge
                //
                double? topEdgeYOfIDImageInPixel;
                double? leftEdgeXOfIDImageInPixel;
                CalcLeftTopEdgeInPixel(labelsFound, ppi, out leftEdgeXOfIDImageInPixel, out topEdgeYOfIDImageInPixel);
                if (topEdgeYOfIDImageInPixel != null && leftEdgeXOfIDImageInPixel != null)
                {
                    MatchTemplateResult matchTemplateResult = null;
                    SKData dataID200ppiPng = null;
                    DateTime dtStart = DateTime.Now;
                    bool bRetMatchTemplate = DoMatchTemplate(matchTemplatePHNI, imageSrc, topEdgeYOfIDImageInPixel.Value, leftEdgeXOfIDImageInPixel.Value, ppi.Value,
                        out matchTemplateResult, out dataID200ppiPng);
                    DateTime dtEnd = DateTime.Now;
                    result.timeElapsedLandmarkDetection = (int)(dtEnd - dtStart).TotalMilliseconds;
                    result.CardImage200ppiPngB64 = Convert.ToBase64String(dataID200ppiPng.ToArray());
                    if (bRetMatchTemplate)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTemplatePHNIResult: {matchTemplateResult.MatchResult}");
                        SKImage imgID200ppi = SKImage.FromEncodedData(dataID200ppiPng);
                        GenerateMatchTemplateResults(matchTemplateResult, dicMatchTemplateInfo, result.MatchTemplateResults);
                        result.landmarkImageBase64 = GenerateMatchTemplateResultImage(result.MatchTemplateResults, result.CardImage200ppiPngB64);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTemplate failed");
                    }
                }

                if (lsLineMergedNotLabel.Count > 0)
                {
                    // remove lines predicted as label because of height
                    int removedFromLinesField = lsLineMergedNotLabel.RemoveAll(l => l.ExtGetHeight() < labelHeightFilterInPixel);
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] removed {removedFromLinesField} lines predicted as label removed from linesField [height < labelHeightInPixel ({labelHeightFilterInPixel})].");
                }

                if (lsLineMergedNotLabel.Count > 0)
                {
                    //var linesLeftOrder = linesField.OrderBy(l => l.BoundingBox[0]);
                    var linesLeftOrder = lsLineMergedNotLabel.OrderBy(l => l.ExtGetLeft());
                    int countLinesField = lsLineMergedNotLabel.Count;
                    int idxMedianLinesField = countLinesField / 2;
                    double? leftMedian = null;
                    if (linesLeftOrder != null && linesLeftOrder.Count() > 0)
                    {
                        leftMedian = linesLeftOrder.ElementAt(idxMedianLinesField).BoundingBox[0];
                    }
                    int numLinesField = lsLineMergedNotLabel.Count;
                    //int idxMainFields = 0;

                    // predit y in inch and expected field for each file line  
                    foreach (Line line in lsLineMergedNotLabel)
                    {
                        double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                        System.Diagnostics.Debug.WriteLine($"line: {line.Text} EstimateCenterYInInch: {y}");
                    }

                    try
                    {
                        if (m_labelPHNI_PCN.IsLabelFound)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {m_labelPHNI_PCN.LineMacthed.Text} --> IDNUM");
                            PCN = m_labelPHNI_PCN.LineMacthed.Text;
                        }
                        else
                        {
                            // filter lines near to line of IDNUM field
                            FindFromMergedLine(ref lsLineMergedNotLabel, centerYInInchOfField_PCN,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is under the label
                                    if (m_labelPHNI_REPUBLIKA_NG_PILIPINAS.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_REPUBLIKA_NG_PILIPINAS.LineMacthed.ExtGetVerticalCenter()))
                                        return false;
                                    if (m_labelPHNI_Republic_of_the_Philippines.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Republic_of_the_Philippines.LineMacthed.ExtGetVerticalCenter()))
                                        return false;
                                    if (m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.LineMacthed.ExtGetVerticalCenter()))
                                        return false;
                                    if (m_labelPHNI_Philippine_Identification_Card.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Philippine_Identification_Card.LineMacthed.ExtGetVerticalCenter()))
                                        return false;

                                    if (!string.IsNullOrEmpty(l.Text))
                                    {
                                        // exactly match format of PCN
                                        Match matchIDNum = regexPCN.Match(l.Text);
                                        if (matchIDNum.Success)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {matchIDNum.Value} --> IDNUM");
                                            //linePCN = line;
                                            PCN = matchIDNum.Value;
                                            return true;
                                        }

                                        // not exactly match format of PCN, but possibly PCN...
                                        string strNumeric = string.Concat(l.Text.Where(c => Char.IsDigit(c)));
                                        if (!string.IsNullOrEmpty(strNumeric))
                                        {
                                            Match matchIDNum2 = regexNum10DigitOrMore.Match(strNumeric);
                                            if (matchIDNum2.Success)
                                            {
                                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> possibly IDNUM");
                                                //linePCN = line;
                                                PCN = l.Text;
                                                return true;
                                            }
                                        }
                                    }
                                    return false;
                                }));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        // filter lines near to line of name field
                        FindFromMergedLine(ref lsLineMergedNotLabel, centerYInInchOfField_LastName,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the label
                                if (m_labelPHNI_REPUBLIKA_NG_PILIPINAS.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_REPUBLIKA_NG_PILIPINAS.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_Republic_of_the_Philippines.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Republic_of_the_Philippines.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_Philippine_Identification_Card.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Philippine_Identification_Card.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_PCN.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_PCN.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_Apelyido_Last_Name.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Apelyido_Last_Name.LineMacthed.ExtGetVerticalCenter()))
                                    return false;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> LAST_NAME");
                                LAST_NAME = l.Text;
                                return true;
                            }));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        // filter lines near to line of name field
                        FindFromMergedLine(ref lsLineMergedNotLabel, centerYInInchOfField_GivenNames,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the label
                                if (m_labelPHNI_REPUBLIKA_NG_PILIPINAS.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_REPUBLIKA_NG_PILIPINAS.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_Republic_of_the_Philippines.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Republic_of_the_Philippines.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_Philippine_Identification_Card.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Philippine_Identification_Card.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_PCN.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_PCN.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_Apelyido_Last_Name.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Apelyido_Last_Name.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_Mga_Pangalan_Given_Names.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Mga_Pangalan_Given_Names.LineMacthed.ExtGetVerticalCenter()))
                                    return false;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> GIVEN_NAMES");
                                GIVEN_NAMES = l.Text;
                                return true;
                            }));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        // filter lines near to line of name field
                        FindFromMergedLine(ref lsLineMergedNotLabel, centerYInInchOfField_MiddleName,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the label
                                if (m_labelPHNI_REPUBLIKA_NG_PILIPINAS.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_REPUBLIKA_NG_PILIPINAS.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_Republic_of_the_Philippines.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Republic_of_the_Philippines.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_Philippine_Identification_Card.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Philippine_Identification_Card.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_PCN.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_PCN.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_Apelyido_Last_Name.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Apelyido_Last_Name.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_Mga_Pangalan_Given_Names.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Mga_Pangalan_Given_Names.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_Gitnang_Apelyido_Middle_Name.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Gitnang_Apelyido_Middle_Name.LineMacthed.ExtGetVerticalCenter()))
                                    return false;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> MIDDLE_NAME");
                                MIDDLE_NAME = l.Text;
                                return true;
                            }));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        // filter lines near to line of DoB field
                        FindFromMergedLine(ref lsLineMergedNotLabel, centerYInInchOfField_DoB,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the label
                                if (m_labelPHNI_REPUBLIKA_NG_PILIPINAS.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_REPUBLIKA_NG_PILIPINAS.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_Republic_of_the_Philippines.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Republic_of_the_Philippines.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_Philippine_Identification_Card.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Philippine_Identification_Card.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_PCN.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_PCN.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_Apelyido_Last_Name.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Apelyido_Last_Name.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_Mga_Pangalan_Given_Names.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Mga_Pangalan_Given_Names.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_Gitnang_Apelyido_Middle_Name.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Gitnang_Apelyido_Middle_Name.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_Petsa_ng_Kapanganakan_Date_of_Birth.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Petsa_ng_Kapanganakan_Date_of_Birth.LineMacthed.ExtGetVerticalCenter()))
                                    return false;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> DOB");
                                DOB = l.Text;
                                return true;
                            }));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    Line lineAddress1 = null;
                    try
                    {
                        // filter lines near to line of address1 field
                        FindFromMergedLine(ref lsLineMergedNotLabel, centerYInInchOfField_Address1,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the label
                                if (m_labelPHNI_REPUBLIKA_NG_PILIPINAS.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_REPUBLIKA_NG_PILIPINAS.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_Republic_of_the_Philippines.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Republic_of_the_Philippines.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_Philippine_Identification_Card.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Philippine_Identification_Card.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_PCN.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_PCN.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_Apelyido_Last_Name.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Apelyido_Last_Name.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_Mga_Pangalan_Given_Names.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Mga_Pangalan_Given_Names.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_Gitnang_Apelyido_Middle_Name.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Gitnang_Apelyido_Middle_Name.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_Petsa_ng_Kapanganakan_Date_of_Birth.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Petsa_ng_Kapanganakan_Date_of_Birth.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHNI_Tirahan_Address.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNI_Tirahan_Address.LineMacthed.ExtGetVerticalCenter()))
                                    return false;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> ADDRESS1");
                                ADDRESS1 = l.Text;
                                lineAddress1 = l;
                                return true;
                            }));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        if(lineAddress1 != null)
                        {
                            // filter lines near to line of address1 field
                            FindFromMergedLine(ref lsLineMergedNotLabel, centerYInInchOfField_Address2,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is under the label
                                    if (lineAddress1 != null && (l.ExtGetVerticalCenter() < lineAddress1.ExtGetVerticalCenter()))
                                        return false;

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> ADDRESS2");
                                    ADDRESS2 = l.Text;
                                    return true;
                                }));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }
                }
            }

            // map to result and convert format 
            List<string> lsMissingFields = new List<string>();

            // LAST_NAME -> lastNameOrFullName 
            if (string.IsNullOrEmpty(LAST_NAME))
            {
                lsMissingFields.Add("LAST_NAME");
            }
            else
            {
                result.lastNameOrFullName = LAST_NAME;
            }

            // GIVEN_NAMES -> firstName 
            if (string.IsNullOrEmpty(GIVEN_NAMES))
            {
                lsMissingFields.Add("GIVEN_NAMES");
            }
            else
            {
                result.firstName = GIVEN_NAMES;
            }

            // MIDDLE_NAME -> middleName 
            if (string.IsNullOrEmpty(MIDDLE_NAME))
            {
                lsMissingFields.Add("MIDDLE_NAME");
            }
            else
            {
                result.middleName = MIDDLE_NAME;
            }

            // PCN -> documentNumber
            if (string.IsNullOrEmpty(PCN))
            {
                lsMissingFields.Add("PCN");
            }
            else
            {
                result.documentNumber = PCN;
            }

            // (CITIZENSHIP) nationality is "PH" (by default)

            // DOB "yyyy/MM/dd" -> dateOfBirth "yyyy-MM-dd"
            try
            {
                result.dateOfBirth = "";

                if (!string.IsNullOrEmpty(DOB))
                {
                    DateTime dtDoB;
                    if (DateTime.TryParse(DOB, out dtDoB))
                    {
                        int yyyy = dtDoB.Year;
                        int MM = dtDoB.Month;
                        int dd = dtDoB.Day;
                        result.dateOfBirth = $"{yyyy:0000}-{MM:00}-{dd:00}";
                    }
                    else
                    {
                        String[] token = DOB.Split(SEPARATOR_COMMA_DOT_BLANK);
                        if (token.Length >= 3)
                        {
                            int MM = 0;
                            int dd = 0;
                            int yyyy = 0;
                            foreach (string v in token)
                            {
                                if (string.IsNullOrEmpty(v))
                                    continue;

                                if (MM == 0)
                                {
                                    MM = MonthNameToNum(v);
                                    continue;
                                }
                                if (dd == 0)
                                {
                                    dd = int.Parse(v);
                                    continue;
                                }
                                if (yyyy == 0)
                                {
                                    yyyy = int.Parse(v);
                                    continue;
                                }
                            }
                            result.dateOfBirth = $"{yyyy:0000}-{MM:00}-{dd:00}";
                        }
                    }
                }
                else
                {
                    lsMissingFields.Add("Date Of Birth");
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                lsMissingFields.Add("Date Of Birth");
            }

            // ADDRESS1, ADDRESS2 -> addressLine1, addressLine2
            if (string.IsNullOrEmpty(ADDRESS1))
            {
                lsMissingFields.Add("ADDRESS1");
            }
            else
            {
                result.addressLine1 = $"{ADDRESS1}";
            }

            result.addressLine2 = $"{ADDRESS2}";

            // determine success or not
            if (lsMissingFields.Count == 0)
            {
                result.Success = true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExtractFieldsFromReadResultOfPHNI result NOT success");
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
#else
        ScanPHNIResult ExtractFieldsFromReadResultOfPHNI(IList<Line> linesAll, SKImage imageSrc, ImgProcLib.MatchTemplateIDCard? matchTemplatePHNI)
        {
            const double ACCEPTABLE_DIFF_IN_LINE = 0.1f;
            ScanPHNIResult result = new ScanPHNIResult();

            double centerYInInchOfField_PCN = 0.649f;
            double centerYInInchOfField_LastName = 0.885f;
            double centerYInInchOfField_GivenNames = 1.121f;
            double centerYInInchOfField_MiddleName = 1.446f;
            double centerYInInchOfField_DoB = 1.68f;
            double centerYInInchOfField_Address1 = 1.92f;
            double centerYInInchOfField_Address2 = 2.03f;

            Dictionary<string, MatchTemplateInfo> dicMatchTemplateInfo = new Dictionary<string, MatchTemplateInfo>();
            MatchTemplateInfo matchTmplPHNI_670_COA = new MatchTemplateInfo("PHNI_670_COA", "COA", 0.8f, 0.5f, 0.3f);
            dicMatchTemplateInfo.Add("PHNI_670_COA", matchTmplPHNI_670_COA);
            MatchTemplateInfo matchTmplPHNI_670_Logo_Fingerprint = new MatchTemplateInfo("PHNI_670_Logo_Fingerprint", "Logo_Fingerprint", 0.8f, 3.0f, 0.30f);
            dicMatchTemplateInfo.Add("PHNI_670_Logo_Fingerprint", matchTmplPHNI_670_Logo_Fingerprint);
            MatchTemplateInfo matchTmplPHNI_670_PHL = new MatchTemplateInfo("PHNI_670_PHL", "PHL", 0.8f, 3.18f, 1.726f);
            dicMatchTemplateInfo.Add("PHNI_670_PHL", matchTmplPHNI_670_PHL);
            MatchTemplateInfo matchTmplPHNI_670_Logo_Right_Bottom = new MatchTemplateInfo("PHNI_670_Logo_Right_Bottom", "Logo_Right_Bottom", 0.8f, 3.18f, 1.97f);
            dicMatchTemplateInfo.Add("PHNI_670_Logo_Right_Bottom", matchTmplPHNI_670_Logo_Right_Bottom);
            MatchTemplateInfo matchTmplPHNI_670_PSA_Watermark = new MatchTemplateInfo("PHNI_670_PSA_Watermark", "Flag", 0.8f, 2.714f, 1.79f);
            dicMatchTemplateInfo.Add("PHNI_670_PSA_Watermark", matchTmplPHNI_670_PSA_Watermark);

            string PCN = "";
            string LAST_NAME = "";
            string GIVEN_NAMES = "";
            string MIDDLE_NAME = "";
            string DOB = "";
            string ADDRESS1 = "";
            string ADDRESS2 = "";

            List<Line> linesField = new List<Line>();   // lines valid and not label
            List<LabelInfo> labelsFound = new List<LabelInfo>();

            List<Line> lsLinesInSameLine = new List<Line>();
            Line lineMerged = null;
            List<Line> lsLineMerged = new List<Line>();
            // find labels exactly match
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

                if (lsLinesInSameLine.Count == 0)
                {
                    lsLinesInSameLine.Add(line);
                    lineMerged = line;
                    continue;
                }

                if (IsLineInTheSameLine(lineMerged, line))
                {
                    lsLinesInSameLine.Add(line);
                    lineMerged = lineMerged.MergedLine(line);
                    continue;
                }

                Line lineMergedToCheck = lineMerged;
                lineMerged = line;  // for next turn
                Line[] arLinesInSameLine = lsLinesInSameLine.ToArray();
                Line[] linesToAddFields = FindLabelInLine(ref labelsFound, arLinesInSameLine, lineMergedToCheck,"PHNI");
                lsLinesInSameLine.Clear();
                lsLinesInSameLine.Add(line);

                if (linesToAddFields != null && linesToAddFields.Length > 0)
                {
                    linesField.AddRange(linesToAddFields);
                    lsLineMerged.Add(lineMergedToCheck);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] lineMergedToCheck: {lineMergedToCheck.Text} is not field.");
                }

            }// foreach lines in other columns

            // find labels in the last line
            if (lineMerged != null)
            {
                Line[] arLinesInSameLine = lsLinesInSameLine.ToArray();
                Line[] linesToAddFields = FindLabelInLine(ref labelsFound, arLinesInSameLine, lineMerged, "PHNI");
                lsLinesInSameLine.Clear();
                if (linesToAddFields != null && linesToAddFields.Length > 0)
                {
                    linesField.AddRange(linesToAddFields);
                    lsLineMerged.Add(lineMerged);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] lineMergedToCheck: {lineMerged.Text} is not field.");
                }
            }

            // calc pixel per inch
            double? ppi = LabelInfo.CalcPixelPerInch(labelsFound);
            if (ppi == null)
            {
                Console.WriteLine("CalcPixelPerInch failed");
            }
            else
            {
                const double labelHeightFilterInInch = 0.08f;
                double labelHeightFilterInPixel = (double)labelHeightFilterInInch * ppi.Value;

                //
                // calc top and left edge
                //
                double? topEdgeYOfIDImageInPixel;
                double? leftEdgeXOfIDImageInPixel;
                CalcLeftTopEdgeInPixel(labelsFound, ppi, out leftEdgeXOfIDImageInPixel, out topEdgeYOfIDImageInPixel);
                if (topEdgeYOfIDImageInPixel != null && leftEdgeXOfIDImageInPixel != null)
                {
                    int widthOfIDImageInPixel = (int)(3.35f * ppi.Value);
                    int heightOfIDImageInPixel = (int)(2.15f * ppi.Value);
                    SKRectI rect = new SKRectI(
                        (int)leftEdgeXOfIDImageInPixel - (int)(ppi * 0.1),
                        (int)topEdgeYOfIDImageInPixel - (int)(ppi * 0.1),
                        (int)leftEdgeXOfIDImageInPixel + widthOfIDImageInPixel + (int)(ppi * 0.1),
                        (int)topEdgeYOfIDImageInPixel + heightOfIDImageInPixel + (int)(ppi * 0.1));
                    if (rect.Top < 0) rect.Top = 0;
                    if (rect.Left < 0) rect.Left = 0;
                    if (rect.Right > imageSrc.Width) rect.Right = imageSrc.Width;
                    if (rect.Bottom > imageSrc.Height) rect.Bottom = imageSrc.Height;
                    SKImage imageIDSrc = imageSrc.Subset(rect);
                    //SKData dataIDPng = imageIDSrc.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                    double rate = 200.0f / ppi.Value;
                    SKBitmap bmpID = SKBitmap.FromImage(imageIDSrc);
                    SKBitmap bmpID200ppi = bmpID.Resize(new SKSizeI((int)(imageIDSrc.Width * rate), (int)(imageIDSrc.Height * rate)), SKFilterQuality.High);
                    SKData dataID200ppiPng = bmpID200ppi.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                    result.CardImage200ppiPngB64 = Convert.ToBase64String(dataID200ppiPng.ToArray());
                    //using (FileStream fs = new FileStream("ID.png", FileMode.Create))
                    //{
                    //    dataID200ppiPng.SaveTo(fs);
                    //}

                    if (matchTemplatePHNI != null)
                    {
                        MatchTemplateResult matchTemplateResult = matchTemplatePHNI.DoMatchTemplate(dataID200ppiPng.ToArray());
                        if (matchTemplateResult != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTemplateResult.MatchResult: {matchTemplateResult.MatchResult}");
                            SKImage imgID200ppi = SKImage.FromBitmap(bmpID200ppi);

                            foreach (string key in matchTemplateResult.MatchResult.Keys)
                            {
                                MatchTemplateResultItem matchTemplateResultItem = matchTemplateResult.MatchResult[key];
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] key: {key} MatchResult: {matchTemplateResultItem.MatchResult} x: {matchTemplateResultItem.LocX} y: {matchTemplateResultItem.LocY} w: {matchTemplateResultItem.Width} h: {matchTemplateResultItem.Height}");
                                if (dicMatchTemplateInfo.ContainsKey(key))
                                {
                                    MatchTemplateResultInfo matchTemplateResultInfo = new MatchTemplateResultInfo();
                                    matchTemplateResultInfo.Title = key;
                                    matchTemplateResultInfo.MatchTemplateInfo = dicMatchTemplateInfo[key];
                                    matchTemplateResultInfo.MatchTemplateInfo.MatchResult = matchTemplateResultItem.MatchResult;
                                    matchTemplateResultInfo.MatchTemplateInfo.ResultLocX = matchTemplateResultItem.LocX;
                                    matchTemplateResultInfo.MatchTemplateInfo.ResultLocY = matchTemplateResultItem.LocY;
                                    matchTemplateResultInfo.MatchTemplateInfo.ResultWidth = matchTemplateResultItem.Width;
                                    matchTemplateResultInfo.MatchTemplateInfo.ResultHeight = matchTemplateResultItem.Height;
                                    double? dist = matchTemplateResultInfo.GetDistanceFromExpectedCenterInInch();
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] key: {key} dist: {dist} ");
                                    result.MatchTemplateResults.Add(key, matchTemplateResultInfo);
                                }
                                /*
                                using (FileStream fs = new FileStream(matchTemplateMyKadResultItem.GetName() + ".png", FileMode.Create))
                                {
                                    SKRectI rectLandmark = new SKRectI((int)matchTemplateMyKadResultItem.LocX, (int)matchTemplateMyKadResultItem.LocY, (int)matchTemplateMyKadResultItem.LocX + matchTemplateMyKadResultItem.Width, (int)matchTemplateMyKadResultItem.LocY + matchTemplateMyKadResultItem.Height);
                                    SKImage imageLandmark = imgID200ppi.Subset(rectLandmark);
                                    SKData dataLandmark = imageLandmark.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                                    dataLandmark.SaveTo(fs);
                                }
                                */
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTemplateResult is null");
                    }
                }
                /*
                List<double> lsTopEdgeYOfIDImageInPixelCalculated = new List<double>();
                foreach (LabelInfo label in labelsFound)
                {
                    double? topEdgeYOfIDImageInPixelCalculated = label.CalcTopEdgeYOfIDImageInPixel(ppi.Value);
                    if (topEdgeYOfIDImageInPixelCalculated != null)
                    {
                        lsTopEdgeYOfIDImageInPixelCalculated.Add(topEdgeYOfIDImageInPixelCalculated.Value);
                        double? y = label.PredictCenterYInPixel(ppi, topEdgeYOfIDImageInPixelCalculated);
                        System.Diagnostics.Debug.WriteLine($"{label.Title} PredictCenterYInPixel: {y}");
                    }
                }
                double? topEdgeYOfIDImageInPixel;
                if (lsTopEdgeYOfIDImageInPixelCalculated.Count > 0)
                {
                    topEdgeYOfIDImageInPixel = lsTopEdgeYOfIDImageInPixelCalculated.Average();
                }
                else
                {
                    topEdgeYOfIDImageInPixel = null;
                }
                */

                if (linesField.Count > 0)
                {
                    // remove lines predicted as label because of height
                    int removedFromLinesField = linesField.RemoveAll(l => l.ExtGetHeight() < labelHeightFilterInPixel);
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] removed {removedFromLinesField} lines predicted as label removed from linesField [height < labelHeightInPixel ({labelHeightFilterInPixel})].");
                    int removedFromLinesMerged = lsLineMerged.RemoveAll(l => l.ExtGetHeight() < labelHeightFilterInPixel);
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] removed {removedFromLinesMerged} lines predicted as label removed from lsLineMerged [height < labelHeightInPixel ({labelHeightFilterInPixel})].");
                }

                if (linesField.Count > 0)
                {
                    //var linesLeftOrder = linesField.OrderBy(l => l.BoundingBox[0]);
                    var linesLeftOrder = linesField.OrderBy(l => l.ExtGetLeft());
                    int countLinesField = linesField.Count;
                    int idxMedianLinesField = countLinesField / 2;
                    double? leftMedian = null;
                    if (linesLeftOrder != null && linesLeftOrder.Count() > 0)
                    {
                        leftMedian = linesLeftOrder.ElementAt(idxMedianLinesField).BoundingBox[0];
                    }
                    int numLinesField = linesField.Count;
                    //int idxMainFields = 0;

                    // predit y in inch and expected field for each file line  
                    foreach (Line line in linesField)
                    {
                        double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                        System.Diagnostics.Debug.WriteLine($"line: {line.Text} EstimateCenterYInInch: {y}");
                    }

                    try
                    {
                        if (m_labelPHNI_PCN.IsLabelFound)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {m_labelPHNI_PCN.LineMacthed.Text} --> IDNUM");
                            PCN = m_labelPHNI_PCN.LineMacthed.Text;
                        }
                        else
                        {
                            // filter lines near to lien of IDNUM field
                            Line[] mergedLinesNearToPCN = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_PCN - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToPCN)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToPCN: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_PCN)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is under the label
                                    if (m_labelPHNI_REPUBLIKA_NG_PILIPINAS.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_REPUBLIKA_NG_PILIPINAS.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (m_labelPHNI_Republic_of_the_Philippines.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Republic_of_the_Philippines.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (m_labelPHNI_Philippine_Identification_Card.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Philippine_Identification_Card.LineMacthed.ExtGetVerticalCenter()))
                                        continue;

                                    if (!string.IsNullOrEmpty(line.Text))
                                    {
                                        Match matchIDNum = regexPCN.Match(line.Text);
                                        if (matchIDNum.Success)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {matchIDNum.Value} --> IDNUM");
                                            //linePCN = line;
                                            PCN = matchIDNum.Value;
                                            lsLineMerged.Remove(line);
                                            break;
                                        }

                                        string strNumeric = string.Concat(line.Text.Where(c => Char.IsDigit(c)));
                                        if (!string.IsNullOrEmpty(strNumeric))
                                        {
                                            Match matchIDNum2 = regexNum10DigitOrMore.Match(strNumeric);
                                            if (matchIDNum2.Success)
                                            {
                                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> possibly IDNUM");
                                                //linePCN = line;
                                                PCN = line.Text;
                                                lsLineMerged.Remove(line);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        // filter lines near to lien of name field
                        Line[] mergedLinesNearToLastName = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_LastName - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToLastName)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToLastName: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_LastName)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the label
                                if (m_labelPHNI_REPUBLIKA_NG_PILIPINAS.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_REPUBLIKA_NG_PILIPINAS.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_Republic_of_the_Philippines.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Republic_of_the_Philippines.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_Philippine_Identification_Card.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Philippine_Identification_Card.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_PCN.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_PCN.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_Apelyido_Last_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Apelyido_Last_Name.LineMacthed.ExtGetVerticalCenter()))
                                    continue;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> LAST_NAME");
                                LAST_NAME = line.Text;
                                lsLineMerged.Remove(line);
                                //heightName = line.ExtGetHeight();
                                //bottomName = line.ExtGetBottom();
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        // filter lines near to lien of name field
                        Line[] mergedLinesNearToGivenName = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_GivenNames - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToGivenName)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToGivenName: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_GivenNames)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the label
                                if (m_labelPHNI_REPUBLIKA_NG_PILIPINAS.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_REPUBLIKA_NG_PILIPINAS.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_Republic_of_the_Philippines.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Republic_of_the_Philippines.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_Philippine_Identification_Card.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Philippine_Identification_Card.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_PCN.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_PCN.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_Apelyido_Last_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Apelyido_Last_Name.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_Mga_Pangalan_Given_Names.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Mga_Pangalan_Given_Names.LineMacthed.ExtGetVerticalCenter()))
                                    continue;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> GIVEN_NAMES");
                                GIVEN_NAMES = line.Text;
                                lsLineMerged.Remove(line);
                                //heightName = line.ExtGetHeight();
                                //bottomName = line.ExtGetBottom();
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        // filter lines near to lien of name field
                        Line[] mergedLinesNearToMiddleName = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_MiddleName - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToMiddleName)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToMiddleName: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_MiddleName)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the label
                                if (m_labelPHNI_REPUBLIKA_NG_PILIPINAS.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_REPUBLIKA_NG_PILIPINAS.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_Republic_of_the_Philippines.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Republic_of_the_Philippines.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_Philippine_Identification_Card.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Philippine_Identification_Card.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_PCN.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_PCN.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_Apelyido_Last_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Apelyido_Last_Name.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_Mga_Pangalan_Given_Names.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Mga_Pangalan_Given_Names.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_Gitnang_Apelyido_Middle_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Gitnang_Apelyido_Middle_Name.LineMacthed.ExtGetVerticalCenter()))
                                    continue;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> MIDDLE_NAME");
                                MIDDLE_NAME = line.Text;
                                lsLineMerged.Remove(line);
                                //heightName = line.ExtGetHeight();
                                //bottomName = line.ExtGetBottom();
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        // filter lines near to line of DoB field
                        Line[] mergedLinesNearToDOB = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_DoB - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToDOB)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToDOB: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_DoB)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the label
                                if (m_labelPHNI_REPUBLIKA_NG_PILIPINAS.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_REPUBLIKA_NG_PILIPINAS.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_Republic_of_the_Philippines.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Republic_of_the_Philippines.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_Philippine_Identification_Card.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Philippine_Identification_Card.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_PCN.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_PCN.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_Apelyido_Last_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Apelyido_Last_Name.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_Mga_Pangalan_Given_Names.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Mga_Pangalan_Given_Names.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_Gitnang_Apelyido_Middle_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Gitnang_Apelyido_Middle_Name.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_Petsa_ng_Kapanganakan_Date_of_Birth.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Petsa_ng_Kapanganakan_Date_of_Birth.LineMacthed.ExtGetVerticalCenter()))
                                    continue;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> DOB");
                                DOB = line.Text;
                                lsLineMerged.Remove(line);
                                //heightName = line.ExtGetHeight();
                                //bottomName = line.ExtGetBottom();
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    Line lineAddress1 = null;
                    try
                    {
                        // filter lines near to line of address1 field
                        Line[] mergedLinesNearToAddress1 = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address1 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress1)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress1: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address1)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the label
                                if (m_labelPHNI_REPUBLIKA_NG_PILIPINAS.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_REPUBLIKA_NG_PILIPINAS.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_Republic_of_the_Philippines.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Republic_of_the_Philippines.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_Philippine_Identification_Card.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Philippine_Identification_Card.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_PCN.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_PCN.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_Apelyido_Last_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Apelyido_Last_Name.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_Mga_Pangalan_Given_Names.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Mga_Pangalan_Given_Names.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_Gitnang_Apelyido_Middle_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Gitnang_Apelyido_Middle_Name.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_Petsa_ng_Kapanganakan_Date_of_Birth.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Petsa_ng_Kapanganakan_Date_of_Birth.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_PHL.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_PHL.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (m_labelPHNI_Tirahan_Address.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNI_Tirahan_Address.LineMacthed.ExtGetVerticalCenter()))
                                    continue;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS1");
                                ADDRESS1 = line.Text;
                                lsLineMerged.Remove(line);
                                lineAddress1 = line;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        Line[] mergedLinesNearToAddress2 = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address2 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress2)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress2: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address2)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the label
                                if (lineAddress1 != null && (line.ExtGetVerticalCenter() < lineAddress1.ExtGetVerticalCenter()))
                                    continue;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS2");
                                lsLineMerged.Remove(line);
                                ADDRESS2 = line.Text;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }
                }
            }

#if false
            // pick the lines aligned to left 
            List<Line> linesFieldOrLabel = new List<Line>();   // lines valid and not label

            // find labels exactly match
            if (linesAll.Any())
            {
                // sort from top to bottom
                linesAll = linesAll.OrderBy(l => l.BoundingBox[1]).ToList();
                //linesLeftSide = linesLeftSide.OrderBy(l => l.BoundingBox[1]);
                System.Diagnostics.Debug.WriteLine("\nLines aligned to left:");
                Regex regexIDNum = new Regex(@"\d{4}-\d{4}-\d{4}-\d{4}");
                int idxIdNum = -1;
                decimal heightIdNum = 0;
                //int numLines = linesLeftSide.Count();
                int numLines = linesAll.Count();
                //int idx = 0;
                //foreach (Line line in linesLeftSide)
                foreach (Line line in linesAll)
                {
                    string text = line.Text.Trim();
                    decimal heightLine = 0;
#if false
                    if (line.BoundingBox.Count == 8 && line.BoundingBox[7].HasValue && line.BoundingBox[1].HasValue)
                    {
                        heightLine = Math.Abs((decimal)line.BoundingBox[7] - (decimal)line.BoundingBox[1]);
                    }
                    else if (line.BoundingBox.Count == 4 && line.BoundingBox[1].HasValue && line.BoundingBox[3].HasValue)
                    {
                        heightLine = Math.Abs((decimal)line.BoundingBox[3] - (decimal)line.BoundingBox[1]);
                    }
#else
                    heightLine = Math.Abs((decimal)line.ExtGetHeight());
#endif

                    double? angle = line.ExtGetAngle();
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {text}");
                    if (angle == null || Math.Abs((decimal)angle) > 10)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   angle:{angle} > 10 --> ignored");
                        continue;
                    }

                    //"REPUBLIKA NG PILIPINAS"
                    if (!m_labelPHNI_REPUBLIKA_NG_PILIPINAS.IsLabelFound)
                    {
                        if (m_labelPHNI_REPUBLIKA_NG_PILIPINAS.MatchTitle(line))
                            continue;
                    }
                    //"Republic of the Philippines"
                    if (!m_labelPHNI_Republic_of_the_Philippines.IsLabelFound)
                    {
                        if (m_labelPHNI_Republic_of_the_Philippines.MatchTitle(line))
                            continue;
                    }
                    //"PAMBANSANG PAGKAKAKILANLAN"
                    if (!m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.IsLabelFound)
                    {
                        if (m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.MatchTitle(line))
                            continue;
                    }
                    //"Philippine Identification Card"
                    if (!m_labelPHNI_Philippine_Identification_Card.IsLabelFound)
                    {
                        if (m_labelPHNI_Philippine_Identification_Card.MatchTitle(line))
                            continue;
                    }

                    //"LASTNAME"
                    if (!m_labelPHNI_Apelyido_Last_Name.IsLabelFound)
                    {
                        if (m_labelPHNI_Apelyido_Last_Name.MatchTitle(line))
                            continue;
                    }

                    //"GIVEN NAMES"
                    if (!m_labelPHNI_Mga_Pangalan_Given_Names.IsLabelFound)
                    {
                        if (m_labelPHNI_Mga_Pangalan_Given_Names.MatchTitle(line))
                            continue;
                    }

                    //"MIDDLE NAME"
                    if (!m_labelPHNI_Gitnang_Apelyido_Middle_Name.IsLabelFound)
                    {
                        if (m_labelPHNI_Gitnang_Apelyido_Middle_Name.MatchTitle(line))
                            continue;
                    }

                    //"DATE_OF_BIRTH"
                    if (!m_labelPHNI_Petsa_ng_Kapanganakan_Date_of_Birth.IsLabelFound)
                    {
                        if (m_labelPHNI_Petsa_ng_Kapanganakan_Date_of_Birth.MatchTitle(line))
                            continue;
                    }

                    //"Tirahan/Address"
                    if (!m_labelPHNI_Tirahan_Address.IsLabelFound)
                    {
                        if (m_labelPHNI_Tirahan_Address.MatchTitle(line))
                            continue;
                    }

                    linesFieldOrLabel.Add(line);
                }
            }

            // find labels not found yet, and fields
            if (linesFieldOrLabel.Any())
            {
                // sort from top to bottom
                linesFieldOrLabel = linesFieldOrLabel.OrderBy(l => l.BoundingBox[1]).ToList();
                //linesLeftSide = linesLeftSide.OrderBy(l => l.BoundingBox[1]);
                System.Diagnostics.Debug.WriteLine("\nLines aligned to left:");
                Regex regexIDNum = new Regex(@"\d{4}-\d{4}-\d{4}-\d{4}");
                //int idxIdNum = -1;
                decimal heightIdNum = 0;
                //int numLines = linesLeftSide.Count();
                int numLines = linesFieldOrLabel.Count();
                //foreach (Line line in linesLeftSide)
                foreach (Line line in linesFieldOrLabel)
                {
                    string text = line.Text.Trim();
                    decimal heightLine = 0;
#if false
                    if (line.BoundingBox.Count == 8 && line.BoundingBox[7].HasValue && line.BoundingBox[1].HasValue)
                    {
                        heightLine = Math.Abs((decimal)line.BoundingBox[7] - (decimal)line.BoundingBox[1]);
                    }
                    else if (line.BoundingBox.Count == 4 && line.BoundingBox[1].HasValue && line.BoundingBox[3].HasValue)
                    {
                        heightLine = Math.Abs((decimal)line.BoundingBox[3] - (decimal)line.BoundingBox[1]);
                    }
#else
                    heightLine = Math.Abs((decimal)line.ExtGetHeight());
#endif

                    double? angle = line.ExtGetAngle();
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {text}");
                    if (angle == null || Math.Abs((decimal)angle) > 10)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   angle:{angle} > 10 --> ignored");
                        continue;
                    }

                    if (string.IsNullOrEmpty(PCN))
                    {
                        //"REPUBLIKA NG PILIPINAS"
                        if (!m_labelPHNI_REPUBLIKA_NG_PILIPINAS.IsLabelFound)
                        {
                            if (m_labelPHNI_REPUBLIKA_NG_PILIPINAS.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }
                        //"Republic of the Philippines"
                        if (!m_labelPHNI_Republic_of_the_Philippines.IsLabelFound)
                        {
                            if (m_labelPHNI_Republic_of_the_Philippines.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }
                        //"PAMBANSANG PAGKAKAKILANLAN"
                        if (!m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.IsLabelFound)
                        {
                            if (m_labelPHNI_PAMBANSANG_PAGKAKAKILANLAN.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }
                        //"Philippine Identification Card"
                        if (!m_labelPHNI_Philippine_Identification_Card.IsLabelFound)
                        {
                            if (m_labelPHNI_Philippine_Identification_Card.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }

                        try
                        {
                            //PCN
                            if (regexIDNum.Match(text).Success)
                            {
                                heightIdNum = heightLine;
                                PCN = text.Trim();
                                linePCN = line;
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(ex.Message);
                        }
                    }
                    else
                    {
                        //"LASTNAME"
                        if (!m_labelPHNI_Apelyido_Last_Name.IsLabelFound)
                        {
                            if (m_labelPHNI_Apelyido_Last_Name.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }
                        if (string.IsNullOrEmpty(LAST_NAME))
                        {
                            if (!m_labelPHNI_Apelyido_Last_Name.IsLabelFound
                              || m_labelPHNI_Apelyido_Last_Name.IsFieldJustUnderTheLabel(line))
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> SURNAME");
                                LAST_NAME = line.Text;
                                continue;
                            }
                        }

                        //"GIVEN NAMES"
                        if (!m_labelPHNI_Mga_Pangalan_Given_Names.IsLabelFound)
                        {
                            if (m_labelPHNI_Mga_Pangalan_Given_Names.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }
                        if (string.IsNullOrEmpty(GIVEN_NAMES))
                        {
                            if (!m_labelPHNI_Mga_Pangalan_Given_Names.IsLabelFound
                                || m_labelPHNI_Mga_Pangalan_Given_Names.IsFieldJustUnderTheLabel(line)
                                )
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> GIVEN_NAME");
                                GIVEN_NAMES = line.Text;
                                continue;
                            }
                        }

                        //"MIDDLE NAME"
                        if (!m_labelPHNI_Gitnang_Apelyido_Middle_Name.IsLabelFound)
                        {
                            if (m_labelPHNI_Gitnang_Apelyido_Middle_Name.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }
                        if (string.IsNullOrEmpty(MIDDLE_NAME))
                        {
                            // MIDDLE_NAME
                            if (!m_labelPHNI_Gitnang_Apelyido_Middle_Name.IsLabelFound
                                || m_labelPHNI_Gitnang_Apelyido_Middle_Name.IsFieldJustUnderTheLabel(line)
                                )
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> MIDDLE_NAME");
                                MIDDLE_NAME = line.Text;
                                continue;
                            }
                        }

                        //"DATE_OF_BIRTH"
                        if (!m_labelPHNI_Petsa_ng_Kapanganakan_Date_of_Birth.IsLabelFound)
                        {
                            if (m_labelPHNI_Petsa_ng_Kapanganakan_Date_of_Birth.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }
                        if (string.IsNullOrEmpty(DOB))
                        {
                            if (!m_labelPHNI_Petsa_ng_Kapanganakan_Date_of_Birth.IsLabelFound
                                || m_labelPHNI_Petsa_ng_Kapanganakan_Date_of_Birth.IsFieldJustUnderTheLabel(line)
                                )
                            {
                                try
                                {
                                    //(JANUARY|FEBRUARY|MARCH|APRIL|MAY|JUNE|JULY|AUGUST|SEPTEMBER|OCTOBER|NOVEMBER|DECEMBER) \d{2}[.,] \d{4}
                                    Regex regexLine = new Regex("(JANUARY|FEBRUARY|MARCH|APRIL|MAY|JUNE|JULY|AUGUST|SEPTEMBER|OCTOBER|NOVEMBER|DECEMBER) \\d{2}[.,] \\d{4}");
                                    Match match = regexLine.Match(text);
                                    if (match.Success)
                                    {
                                        DOB = match.Value;
                                        continue;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine(ex.Message);
                                }
                            }
                        }

                        //"Tirahan/Address"
                        if (!m_labelPHNI_Tirahan_Address.IsLabelFound)
                        {
                            if (m_labelPHNI_Tirahan_Address.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }

                        if (string.IsNullOrEmpty(ADDRESS1))
                        {
                            if ((!m_labelPHNI_Tirahan_Address.IsLabelFound
                                || m_labelPHNI_Tirahan_Address.IsFieldJustUnderTheLabel(line))
                              && (linePCN != null && IsFieldInSameLeftEdgeOfLine(linePCN, line) && IsFieldsUnderTheLine(linePCN, line))
                            )
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS1");
                                ADDRESS1 = line.Text;
                                //idxMainColumn++;
                                continue;
                            }
                        }
                        if (string.IsNullOrEmpty(ADDRESS2))
                        {
                            if ((!m_labelPHNI_Tirahan_Address.IsLabelFound
                                || m_labelPHNI_Tirahan_Address.IsFieldUnderTheLabel(line))
                              && (linePCN != null && IsFieldInSameLeftEdgeOfLine(linePCN, line) && IsFieldsUnderTheLine(linePCN, line))
                            )
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS2");
                                ADDRESS2 = line.Text;
                                //idxMainColumn++;
                                continue;
                            }
                        }
                    }
                    linesField.Add(line);
                }
            }
#endif
            // map to result and convert format 
            List<string> lsMissingFields = new List<string>();

            // LAST_NAME -> lastNameOrFullName 
            if (string.IsNullOrEmpty(LAST_NAME))
            {
                lsMissingFields.Add("LAST_NAME");
            }
            else
            {
                result.lastNameOrFullName = LAST_NAME;
            }

            // GIVEN_NAMES -> firstName 
            if (string.IsNullOrEmpty(GIVEN_NAMES))
            {
                lsMissingFields.Add("GIVEN_NAMES");
            }
            else
            {
                result.firstName = GIVEN_NAMES;
            }

            // MIDDLE_NAME -> middleName 
            if (string.IsNullOrEmpty(MIDDLE_NAME))
            {
                lsMissingFields.Add("MIDDLE_NAME");
            }
            else
            {
                result.middleName = MIDDLE_NAME;
            }

            // PCN -> documentNumber
            if (string.IsNullOrEmpty(PCN))
            {
                lsMissingFields.Add("PCN");
            }
            else
            {
                result.documentNumber = PCN;
            }

            // (CITIZENSHIP) nationality is "PH" (by default)

            // DOB "yyyy/MM/dd" -> dateOfBirth "yyyy-MM-dd"
            try
            {
                result.dateOfBirth = "";

                if (!string.IsNullOrEmpty(DOB))
                {
                    DateTime dtDoB;
                    if (DateTime.TryParse(DOB, out dtDoB))
                    {
                        int yyyy = dtDoB.Year;
                        int MM = dtDoB.Month;
                        int dd = dtDoB.Day;
                        result.dateOfBirth = $"{yyyy:0000}-{MM:00}-{dd:00}";
                    }
                    else
                    {
                        String[] token = DOB.Split(SEPARATOR_COMMA_DOT_BLANK);
                        if (token.Length >= 3)
                        {
                            int MM = 0;
                            int dd = 0;
                            int yyyy = 0;
                            foreach (string v in token)
                            {
                                if (string.IsNullOrEmpty(v))
                                    continue;

                                if (MM == 0)
                                {
                                    MM = MonthNameToNum(v);
                                    continue;
                                }
                                if (dd == 0)
                                {
                                    dd = int.Parse(v);
                                    continue;
                                }
                                if (yyyy == 0)
                                {
                                    yyyy = int.Parse(v);
                                    continue;
                                }
                            }
                            result.dateOfBirth = $"{yyyy:0000}-{MM:00}-{dd:00}";
                        }
                    }
                }
                else
                {
                    lsMissingFields.Add("Date Of Birth");
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                lsMissingFields.Add("Date Of Birth");
            }

            // ADDRESS1, ADDRESS2 -> addressLine1, addressLine2
            if (string.IsNullOrEmpty(ADDRESS1))
            {
                lsMissingFields.Add("ADDRESS1");
            }
            else
            {
                result.addressLine1 = $"{ADDRESS1}";
            }

            result.addressLine2 = $"{ADDRESS2}";

            // determine success or not
            if (lsMissingFields.Count == 0)
            {
                result.Success = true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExtractFieldsFromReadResultOfPHNI result NOT success");
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
#endif

#if true
        //ScanPHNIBKResult ExtractFieldsFromReadResultOfPHNIBK(IList<Line> linesAll, SKImage imageSrc, ZXing.Result[] resReadQRCode)
        ScanPHNIBKResult ExtractFieldsFromReadResultOfPHNIBK(LabelInfo[] labelsFound, Line[] linesMergedNotLabel, SKImage imageSrc, ZXing.Result[] resReadQRCode)
        {
            Regex regexDateOfIssue = new Regex(@"^[0-9]{1,2}[A-Z]*[0-9]{4}$");

            // Fields
            string DATE_OF_ISSUE = "";
            string PCN = "";
            string LAST_NAME = "";
            string GIVEN_NAMES = "";
            string MIDDLE_NAME = "";
            string DOB = "";
            string POB = "";
            string SEX = "";
            string BLOOD_TYPE = "";
            string MARITAL_STATUS = "";

            Line valueKasarian_Sex = null;

            const double ACCEPTABLE_DIFF_IN_LINE = 0.1f;

            double centerYInInchOfField_DateOfIssue = 0.604f;
            double centerYInInchOfField_Sex = 0.854f;
            double centerYInInchOfField_BloodType = 1.04f;
            double centerYInInchOfField_MaritalStatus = 1.23f;
            double centerYInInchOfField_PlaceOfBirthLine1 = 1.437f;
            double centerYInInchOfField_PlaceOfBirthLine2 = 1.56f;

            ScanPHNIBKResult result = new ScanPHNIBKResult();
            List<Line> lsLineMergedNotLabel = new List<Line>();
            lsLineMergedNotLabel.AddRange(linesMergedNotLabel);

            //ZXing.Result[] resReadQRCode = ReadQRCode(imageSrc);
            //if (resReadQRCode == null || resReadQRCode.Length == 0 && labelsFound != null && linesMergedNotLabel != null)
            if (labelsFound != null && linesMergedNotLabel != null)
            {
                SKBitmap bmpID200ppi_1 = null;
                SKBitmap bmpID200ppi_2 = null;

                // calc pixel per inch
                double? ppi = LabelInfo.CalcPixelPerInch(labelsFound);
                if (ppi == null)
                {
                    Console.WriteLine("CalcPixelPerInch failed");
                }
                else
                {
                    const double labelHeightFilterInInch = 0.08f;
                    double labelHeightFilterInPixel = (double)labelHeightFilterInInch * ppi.Value;

                    //
                    // calc top and left edge
                    //
                    double? topEdgeYOfIDImageInPixel;
                    double? leftEdgeXOfIDImageInPixel;
                    CalcLeftTopEdgeInPixel(labelsFound, ppi, out leftEdgeXOfIDImageInPixel, out topEdgeYOfIDImageInPixel);
                    double rate = 200.0f / ppi.Value;
                    int widthOfIDImageInPixel = (int)(3.35f * ppi.Value);
                    int heightOfIDImageInPixel = (int)(2.15f * ppi.Value);
                    if (topEdgeYOfIDImageInPixel != null && leftEdgeXOfIDImageInPixel != null)
                    {
                        if (resReadQRCode == null || resReadQRCode.Length == 0)
                        {
                            // if QR code is not found in whole image, prepare cropped image of QR code to scan again later 
                            if (bmpID200ppi_1 == null)
                            {
                                SKRectI rect = new SKRectI(
                                    (int)leftEdgeXOfIDImageInPixel - (int)(ppi * 0.1),
                                    (int)topEdgeYOfIDImageInPixel - (int)(ppi * 0.1),
                                    (int)leftEdgeXOfIDImageInPixel + widthOfIDImageInPixel + (int)(ppi * 0.1),
                                    (int)topEdgeYOfIDImageInPixel + heightOfIDImageInPixel + (int)(ppi * 0.1));
                                if (rect.Top < 0) rect.Top = 0;
                                if (rect.Left < 0) rect.Left = 0;
                                if (rect.Right > imageSrc.Width) rect.Right = imageSrc.Width;
                                if (rect.Bottom > imageSrc.Height) rect.Bottom = imageSrc.Height;
                                SKImage imageIDSrc = imageSrc.Subset(rect);
                                //SKData dataIDPng = imageIDSrc.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                                SKBitmap bmpID = SKBitmap.FromImage(imageIDSrc);
                                bmpID200ppi_1 = bmpID.Resize(new SKSizeI((int)(imageIDSrc.Width * rate), (int)(imageIDSrc.Height * rate)), SKFilterQuality.High);
                            }
                            //SKData dataID200ppiPng = bmpID200ppi.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                            //using (FileStream fs = new FileStream("ID.png", FileMode.Create))
                            //{
                            //    dataID200ppiPng.SaveTo(fs);
                            //}
                            if (bmpID200ppi_2 == null)
                            {
                                SKRectI rect = new SKRectI(
                                    (int)leftEdgeXOfIDImageInPixel - (int)(ppi * 0.2),
                                    (int)topEdgeYOfIDImageInPixel - (int)(ppi * 0.2),
                                    (int)leftEdgeXOfIDImageInPixel + widthOfIDImageInPixel + (int)(ppi * 0.2),
                                    (int)topEdgeYOfIDImageInPixel + heightOfIDImageInPixel + (int)(ppi * 0.2));
                                if (rect.Top < 0) rect.Top = 0;
                                if (rect.Left < 0) rect.Left = 0;
                                if (rect.Right > imageSrc.Width) rect.Right = imageSrc.Width;
                                if (rect.Bottom > imageSrc.Height) rect.Bottom = imageSrc.Height;
                                SKImage imageIDSrc = imageSrc.Subset(rect);
                                //SKData dataIDPng = imageIDSrc.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                                SKBitmap bmpID = SKBitmap.FromImage(imageIDSrc);
                                bmpID200ppi_2 = bmpID.Resize(new SKSizeI((int)(imageIDSrc.Width * rate), (int)(imageIDSrc.Height * rate)), SKFilterQuality.High);
                            }
                        }
                    }

                    if (lsLineMergedNotLabel.Count > 0)
                    {
                        // remove lines predicted as label because of height
                        int removedFromLinesField = lsLineMergedNotLabel.RemoveAll(l => l.ExtGetHeight() < labelHeightFilterInPixel);
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] removed {removedFromLinesField} lines predicted as label removed from linesField [height < labelHeightInPixel ({labelHeightFilterInPixel})].");
                    }
                    if (lsLineMergedNotLabel.Count > 0)
                    {
                        // remove lines predicted as label because of height
                        int removedFromLinesField = lsLineMergedNotLabel.RemoveAll(l => l.ExtGetHeight() < labelHeightFilterInPixel);
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] removed {removedFromLinesField} lines predicted as label removed from linesField [height < labelHeightInPixel ({labelHeightFilterInPixel})].");
                    }

                    if (lsLineMergedNotLabel.Count > 0)
                    {
                        //var linesLeftOrder = linesField.OrderBy(l => l.BoundingBox[0]);
                        var linesLeftOrder = lsLineMergedNotLabel.OrderBy(l => l.ExtGetLeft());
                        int countLinesField = lsLineMergedNotLabel.Count;
                        int idxMedianLinesField = countLinesField / 2;
                        double? leftMedian = null;
                        if (linesLeftOrder != null && linesLeftOrder.Count() > 0)
                        {
                            leftMedian = linesLeftOrder.ElementAt(idxMedianLinesField).BoundingBox[0];
                        }
                        int numLinesField = lsLineMergedNotLabel.Count;
                        //int idxMainFields = 0;

                        // predit y in inch and expected field for each file line  
                        foreach (Line line in lsLineMergedNotLabel)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"line: {line.Text} EstimateCenterYInInch: {y}");
                        }

                        try
                        {
                            // filter lines near to line of Date of issue field
                            FindFromMergedLine(ref lsLineMergedNotLabel, centerYInInchOfField_DateOfIssue,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    if (!string.IsNullOrEmpty(l.Text))
                                    {
                                        string strNoBlank = l.Text.Replace(" ", String.Empty).Trim();
                                        Match matchDateOfIssue = regexDateOfIssue.Match(strNoBlank);
                                        if (matchDateOfIssue.Success)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {matchDateOfIssue.Value} --> DATE_OF_ISSUE");
                                            DATE_OF_ISSUE = matchDateOfIssue.Value;
                                            return true;
                                        }
                                    }
                                    return false;
                                }));

                            // filter lines near to line of Sex field
                            FindFromMergedLine(ref lsLineMergedNotLabel, centerYInInchOfField_Sex,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is under the label
                                    if (m_labelPHNIBK_Araw_ng_pagkaka_loob_Date_of_issue.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNIBK_Araw_ng_pagkaka_loob_Date_of_issue.LineMacthed.ExtGetVerticalCenter()))
                                        return false;

                                    if (!string.IsNullOrEmpty(l.Text))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> SEX");
                                        SEX = l.Text;
                                        return true;
                                    }
                                    return false;
                                }));

                            // filter lines near to line of Blood Type field
                            FindFromMergedLine(ref lsLineMergedNotLabel, centerYInInchOfField_BloodType,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is under the label
                                    if (m_labelPHNIBK_Araw_ng_pagkaka_loob_Date_of_issue.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNIBK_Araw_ng_pagkaka_loob_Date_of_issue.LineMacthed.ExtGetVerticalCenter()))
                                        return false;
                                    if (m_labelPHNIBK_Kasarian_Sex.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNIBK_Kasarian_Sex.LineMacthed.ExtGetVerticalCenter()))
                                        return false;

                                    if (!string.IsNullOrEmpty(l.Text))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> BLOOD_TYPE");
                                        BLOOD_TYPE = l.Text;
                                        return true;
                                    }
                                    return false;
                                }));

                            // filter lines near to line of Marital Status field
                            FindFromMergedLine(ref lsLineMergedNotLabel, centerYInInchOfField_MaritalStatus,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is under the label
                                    if (m_labelPHNIBK_Araw_ng_pagkaka_loob_Date_of_issue.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNIBK_Araw_ng_pagkaka_loob_Date_of_issue.LineMacthed.ExtGetVerticalCenter()))
                                        return false;
                                    if (m_labelPHNIBK_Kasarian_Sex.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNIBK_Kasarian_Sex.LineMacthed.ExtGetVerticalCenter()))
                                        return false;
                                    if (m_labelPHNIBK_Kalagayang_Sibil_Marital_Status.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNIBK_Kalagayang_Sibil_Marital_Status.LineMacthed.ExtGetVerticalCenter()))
                                        return false;

                                    if (!string.IsNullOrEmpty(l.Text))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> MARITAL_STATUS");
                                        MARITAL_STATUS = l.Text;
                                        return true;
                                    }
                                    return false;
                                }));

                            // filter lines near to line of Place Of Birth line1 field
                            FindFromMergedLine(ref lsLineMergedNotLabel, centerYInInchOfField_PlaceOfBirthLine1,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is under the label
                                    if (m_labelPHNIBK_Araw_ng_pagkaka_loob_Date_of_issue.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNIBK_Araw_ng_pagkaka_loob_Date_of_issue.LineMacthed.ExtGetVerticalCenter()))
                                        return false;
                                    if (m_labelPHNIBK_Kasarian_Sex.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNIBK_Kasarian_Sex.LineMacthed.ExtGetVerticalCenter()))
                                        return false;
                                    if (m_labelPHNIBK_Kalagayang_Sibil_Marital_Status.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNIBK_Kalagayang_Sibil_Marital_Status.LineMacthed.ExtGetVerticalCenter()))
                                        return false;
                                    if (m_labelPHNIBK_Lugar_ng_Kapanganakan_Place_of_Birth.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNIBK_Lugar_ng_Kapanganakan_Place_of_Birth.LineMacthed.ExtGetVerticalCenter()))
                                        return false;

                                    if (!string.IsNullOrEmpty(l.Text))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> POB");
                                        POB = l.Text;
                                        return true;
                                    }
                                    return false;
                                }));

                            // filter lines near to line of Place Of Birth line1 field
                            FindFromMergedLine(ref lsLineMergedNotLabel, centerYInInchOfField_PlaceOfBirthLine2,
                                ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                                new ValidateLine(l =>
                                {
                                    // check if the line is under the label
                                    if (m_labelPHNIBK_Araw_ng_pagkaka_loob_Date_of_issue.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNIBK_Araw_ng_pagkaka_loob_Date_of_issue.LineMacthed.ExtGetVerticalCenter()))
                                        return false;
                                    if (m_labelPHNIBK_Kasarian_Sex.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNIBK_Kasarian_Sex.LineMacthed.ExtGetVerticalCenter()))
                                        return false;
                                    if (m_labelPHNIBK_Kalagayang_Sibil_Marital_Status.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNIBK_Kalagayang_Sibil_Marital_Status.LineMacthed.ExtGetVerticalCenter()))
                                        return false;
                                    if (m_labelPHNIBK_Lugar_ng_Kapanganakan_Place_of_Birth.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHNIBK_Lugar_ng_Kapanganakan_Place_of_Birth.LineMacthed.ExtGetVerticalCenter()))
                                        return false;

                                    if (!string.IsNullOrEmpty(l.Text))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> POB");
                                        POB = POB.Trim() + " " + l.Text;
                                        return true;
                                    }
                                    return false;
                                }));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{ex}");
                        }
                    }
                }

                // read QR code
                if (bmpID200ppi_1 != null)
                {
                    SKImage img = SKImage.FromBitmap(bmpID200ppi_1);
                    int xQRCode = (int)(1.5f * 200f);
                    SKImage imgQR = img.Subset(new SKRectI(xQRCode, 0, img.Width, img.Height));
                    // read QR code from original image
                    resReadQRCode = ReadQRCode(imgQR);
                }

                // retry with wider image
                if (resReadQRCode == null && bmpID200ppi_2 != null)
                {
                    SKImage img = SKImage.FromBitmap(bmpID200ppi_2);
                    int xQRCode = (int)(1.5f * 200f);
                    SKImage imgQR = img.Subset(new SKRectI(xQRCode, 0, img.Width, img.Height));
                    // read QR code from original image
                    resReadQRCode = ReadQRCode(imgQR);
                }
            }

            if (resReadQRCode != null)
            {
                foreach (ZXing.Result aResult in resReadQRCode)
                {
                    Console.WriteLine(aResult.Text);
                    try
                    {
                        Newtonsoft.Json.Linq.JObject jsonObject = Newtonsoft.Json.Linq.JObject.Parse(aResult.Text);
                        if (jsonObject != null)
                        {
                            result.QRCode_DateIssued = (string)jsonObject.GetValue("DateIssued");
                            result.QRCode_Issuer = (string)jsonObject.GetValue("Issuer");
                            result.QRCode_alg = (string)jsonObject.GetValue("alg");
                            result.QRCode_signature = (string)jsonObject.GetValue("signature");
                            JObject qrcode_subject = (JObject)jsonObject.GetValue("subject");
                            if (qrcode_subject != null)
                            {
                                result.QRCode_subject_Suffix = (string)qrcode_subject.GetValue("Suffix");
                                result.QRCode_subject_lName = (string)qrcode_subject.GetValue("lName");
                                result.QRCode_subject_fName = (string)qrcode_subject.GetValue("fName");
                                result.QRCode_subject_mName = (string)qrcode_subject.GetValue("mName");
                                result.QRCode_subject_sex = (string)qrcode_subject.GetValue("sex");
                                result.QRCode_subject_BT = (string)qrcode_subject.GetValue("BF");
                                result.QRCode_subject_DOB = (string)qrcode_subject.GetValue("DOB");
                                result.QRCode_subject_POB = (string)qrcode_subject.GetValue("POB");
                                result.QRCode_subject_PCN = (string)qrcode_subject.GetValue("PCN");

                                result.IsQRCodeDataValid = true;
                                result.QRCodeData = aResult.Text;
                            }
                            Console.WriteLine($"DateIssued: {result.QRCode_DateIssued}");
                            Console.WriteLine($"Issuer: {result.QRCode_Issuer}");
                            Console.WriteLine($"alg: {result.QRCode_alg}");
                            Console.WriteLine($"signature: {result.QRCode_signature}");
                            Console.WriteLine($"subject:");
                            Console.WriteLine($"  Suffix: {result.QRCode_subject_Suffix}");
                            Console.WriteLine($"  lName: {result.QRCode_subject_lName}");
                            Console.WriteLine($"  fName: {result.QRCode_subject_fName}");
                            Console.WriteLine($"  mName: {result.QRCode_subject_mName}");
                            Console.WriteLine($"  sex: {result.QRCode_subject_sex}");
                            Console.WriteLine($"  BT: {result.QRCode_subject_BT}");
                            Console.WriteLine($"  DOB: {result.QRCode_subject_DOB}");
                            Console.WriteLine($"  POB: {result.QRCode_subject_POB}");
                            Console.WriteLine($"  PCN: {result.QRCode_subject_PCN}");

                            if (result.IsQRCodeDataValid)
                            {
                                DATE_OF_ISSUE = result.QRCode_DateIssued;
                                //confidence_DATE_OF_ISSUE = new Confidence(1);
                                LAST_NAME = result.QRCode_subject_lName;
                                //confidence_LAST_NAME = new Confidence(1);
                                GIVEN_NAMES = result.QRCode_subject_fName;
                                //confidence_GIVEN_NAMES = new Confidence(1);
                                MIDDLE_NAME = result.QRCode_subject_mName;
                                //confidence_MIDDLE_NAME = new Confidence(1);
                                SEX = result.QRCode_subject_sex;
                                //confidence_SEX = new Confidence(1);
                                DOB = result.QRCode_subject_DOB;
                                //confidence_DOB = new Confidence(1);
                                POB = result.QRCode_subject_POB;
                                //confidence_POB = new Confidence(1);
                                PCN = result.QRCode_subject_PCN;
                                //confidence_PCN = new Confidence(1);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {ex}");
                    }
                }
                /*
{
"DateIssued": "12 September 2022",
"Issuer": "PSA",
"subject": {
"Suffix": "",
"lName": "DELOS REYES",
"fName": "CHRISTIAN MARX",
"mName": "LOZADA",
"sex": "Male",
"BF": "[1,9]",
"DOB": "June 06, 1988",
"POB": "City of Caloocan,NCR, THIRD DISTRICT",
"PCN": "5931-9426-7546-1037"
},
"alg": "EDDSA",
"signature": "H6WF1LJOcXPiMlE6VTBgamixsA8GqxJ3tJpxpSDmR9qoCMj4/jBKJo3PyP3PdtmMBwXa/ZlypuIOkkcZxqzrAw=="
}
                */
            }

            // map to result and convert format 

            // LAST_NAME -> lastNameOrFullName 
            result.lastNameOrFullName = LAST_NAME;

            // GIVEN_NAMES -> firstName 
            result.firstName = GIVEN_NAMES;

            // MIDDLE_NAME -> middleName 
            result.middleName = MIDDLE_NAME;

            // IDNUM -> documentNumber
            result.documentNumber = PCN;

            // POB -> placeOfBirth
            result.placeOfBirth = POB;

            // DATE_OF_ISSUE "MMM dd, yyyy" -> dateOfBirth "yyyy-MM-dd"
            try
            {
                result.documentIssueDate = ConvertDateString(DATE_OF_ISSUE);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            // DOB "MMM dd, yyyy" -> dateOfBirth "yyyy-MM-dd"
            try
            {
                result.dateOfBirth = ConvertDateString(DOB);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            // Gender
            if (valueKasarian_Sex != null)
            {
                if (CheckCharInLine(valueKasarian_Sex, "MALE"))
                {
                    result.gender = "M";
                }
                else if (CheckCharInLine(valueKasarian_Sex, "FEMALE"))
                {
                    result.gender = "F";
                }
                else
                {
                    // unknown...
                    result.gender = valueKasarian_Sex.Text.Trim();
                }
            }
            else
            {
                if (SEX.ToUpper() == "MALE")
                {
                    result.gender = "M";
                }
                else if (SEX.ToLower() == "FEMALE")
                {
                    result.gender = "F";
                }
                else
                {
                    // unknown...
                    result.gender = SEX.Trim();
                }
            }

            // Marital Status
            /*
            Civil Status:		
            S	Single	
            M	Married	
            X	Separated/Divorced
            W	Widow/er
            */
            switch (MARITAL_STATUS.ToUpper())
            {
                case "SINGLE":
                    result.maritalStatus = "S";
                    break;
                case "MARRIED":
                    result.maritalStatus = "M";
                    break;
                case "SEPARATED":
                    result.maritalStatus = "X";
                    break;
                case "DIVORCED":
                    result.maritalStatus = "X";
                    break;
                case "WIDOW":
                    result.maritalStatus = "W";
                    break;
                case "WIDOWER":
                    result.maritalStatus = "W";
                    break;
                default:
                    result.maritalStatus = MARITAL_STATUS.Trim();   // Unknown
                    break;
            }

            result.Success = true;
            return result;
        }
#else
        public static ScanPHNIBKResult ExtractFieldsFromReadResultOfPHNIBK(IList<Line> linesAll, SKImage imageSrc, ZXing.Result[] resReadQRCode)
        {
            ScanPHNIBKResult result = new ScanPHNIBKResult();

            Regex regexDateOfIssue = new Regex(@"^[0-9]{1,2}[A-Z]*[0-9]{4}$");

            // Fields
            string DATE_OF_ISSUE = "";
            string PCN = "";
            string LAST_NAME = "";
            string GIVEN_NAMES = "";
            string MIDDLE_NAME = "";
            string DOB = "";
            string POB = "";
            string SEX = "";
            string BLOOD_TYPE = "";
            string MARITAL_STATUS = "";

            Line valueKasarian_Sex = null;

            const double ACCEPTABLE_DIFF_IN_LINE = 0.1f;

            double centerYInInchOfField_DateOfIssue = 0.604f;
            double centerYInInchOfField_Sex = 0.854f;
            double centerYInInchOfField_BloodType = 1.04f;
            double centerYInInchOfField_MaritalStatus = 1.23f;
            double centerYInInchOfField_PlaceOfBirthLine1 = 1.437f;
            double centerYInInchOfField_PlaceOfBirthLine2 = 1.56f;

            //ZXing.Result[] resReadQRCode = ReadQRCode(imageSrc);
            if(resReadQRCode == null || resReadQRCode.Length == 0 && linesAll != null)
            {
                SKBitmap bmpID200ppi_1 = null;
                SKBitmap bmpID200ppi_2 = null;

                List<Line> linesField = new List<Line>();   // lines valid and not label
                List<LabelInfo> labelsFound = new List<LabelInfo>();

                List<Line> lsLinesInSameLine = new List<Line>();
                Line lineMerged = null;
                List<Line> lsLineMerged = new List<Line>();
                // find labels exactly match
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

                    if (lsLinesInSameLine.Count == 0)
                    {
                        lsLinesInSameLine.Add(line);
                        lineMerged = line;
                        continue;
                    }

                    if (IsLineInTheSameLine(lineMerged, line))
                    {
                        lsLinesInSameLine.Add(line);
                        lineMerged = lineMerged.MergedLine(line);
                        continue;
                    }

                    Line lineMergedToCheck = lineMerged;
                    lineMerged = line;  // for next turn
                    Line[] arLinesInSameLine = lsLinesInSameLine.ToArray();
                    Line[] linesToAddFields = FindLabelInLineAbove(ref labelsFound, arLinesInSameLine, lineMergedToCheck, labelsToFind.ToArray(), labelsFooter);
                    lsLinesInSameLine.Clear();
                    lsLinesInSameLine.Add(line);

                    if (linesToAddFields != null && linesToAddFields.Length > 0)
                    {
                        linesField.AddRange(linesToAddFields);
                        lsLineMerged.Add(lineMergedToCheck);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] lineMergedToCheck: {lineMergedToCheck.Text} is not field.");
                    }

                }// foreach lines in other columns

                // find labels in the last line
                if (lineMerged != null)
                {
                    Line[] arLinesInSameLine = lsLinesInSameLine.ToArray();
                    Line[] linesToAddFields = FindLabelInLineAbove(ref labelsFound, arLinesInSameLine, lineMerged, labelsToFind.ToArray(), labelsFooter);
                    lsLinesInSameLine.Clear();
                    if (linesToAddFields != null && linesToAddFields.Length > 0)
                    {
                        linesField.AddRange(linesToAddFields);
                        lsLineMerged.Add(lineMerged);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] lineMergedToCheck: {lineMerged.Text} is not field.");
                    }
                }

                // calc pixel per inch
                double? ppi = LabelInfo.CalcPixelPerInch(labelsFound);
                if (ppi == null)
                {
                    Console.WriteLine("CalcPixelPerInch failed");
                }
                else
                {
                    const double labelHeightFilterInInch = 0.08f;
                    double labelHeightFilterInPixel = (double)labelHeightFilterInInch * ppi.Value;

                    //
                    // calc top and left edge
                    //
#if true
                    double? topEdgeYOfIDImageInPixel;
                    double? leftEdgeXOfIDImageInPixel;
                    CalcLeftTopEdgeInPixel(labelsFound, ppi, out leftEdgeXOfIDImageInPixel, out topEdgeYOfIDImageInPixel);
#else
                    List<double> lsTopEdgeYOfIDImageInPixelCalculated = new List<double>();
                    List<double> lsLeftEdgeXOfIDImageInPixelCalculated = new List<double>();
                    foreach (LabelInfo label in labelsFound)
                    {
                        double? topEdgeYOfIDImageInPixelCalculated = label.CalcTopEdgeYOfIDImageInPixel(ppi.Value);
                        if (topEdgeYOfIDImageInPixelCalculated != null)
                        {
                            lsTopEdgeYOfIDImageInPixelCalculated.Add(topEdgeYOfIDImageInPixelCalculated.Value);
                            double? y = label.PredictCenterYInPixel(ppi, topEdgeYOfIDImageInPixelCalculated);
                            System.Diagnostics.Debug.WriteLine($"{label.Title} PredictCenterYInPixel: {y}");
                        }
                        double? leftEdgeXOfIDImageInPixelCalculated = label.CalcLeftEdgeXOfIDImageInPixel(ppi.Value);
                        if (leftEdgeXOfIDImageInPixelCalculated != null)
                        {
                            lsLeftEdgeXOfIDImageInPixelCalculated.Add(leftEdgeXOfIDImageInPixelCalculated.Value);
                            double? x = label.PredictCenterXInPixel(ppi, leftEdgeXOfIDImageInPixelCalculated);
                            System.Diagnostics.Debug.WriteLine($"{label.Title} PredictCenterXInPixel: {x}");
                        }
                    }

                    double? topEdgeYOfIDImageInPixel;
                    if (lsTopEdgeYOfIDImageInPixelCalculated.Count > 0)
                    {
                        topEdgeYOfIDImageInPixel = lsTopEdgeYOfIDImageInPixelCalculated.Average();
                    }
                    else
                    {
                        topEdgeYOfIDImageInPixel = null;
                    }

                    double? leftEdgeXOfIDImageInPixel;
                    if (lsLeftEdgeXOfIDImageInPixelCalculated.Count > 0)
                    {
                        leftEdgeXOfIDImageInPixel = lsLeftEdgeXOfIDImageInPixelCalculated.Average();
                    }
                    else
                    {
                        leftEdgeXOfIDImageInPixel = null;
                    }
#endif
                    double rate = 200.0f / ppi.Value;
                    int widthOfIDImageInPixel = (int)(3.35f * ppi.Value);
                    int heightOfIDImageInPixel = (int)(2.15f * ppi.Value);
                    if (topEdgeYOfIDImageInPixel != null && leftEdgeXOfIDImageInPixel != null)
                    {
                        if(bmpID200ppi_1 == null)
                        {
                            SKRectI rect = new SKRectI(
                                (int)leftEdgeXOfIDImageInPixel - (int)(ppi * 0.1),
                                (int)topEdgeYOfIDImageInPixel - (int)(ppi * 0.1),
                                (int)leftEdgeXOfIDImageInPixel + widthOfIDImageInPixel + (int)(ppi * 0.1),
                                (int)topEdgeYOfIDImageInPixel + heightOfIDImageInPixel + (int)(ppi * 0.1));
                            if (rect.Top < 0) rect.Top = 0;
                            if (rect.Left < 0) rect.Left = 0;
                            if (rect.Right > imageSrc.Width) rect.Right = imageSrc.Width;
                            if (rect.Bottom > imageSrc.Height) rect.Bottom = imageSrc.Height;
                            SKImage imageIDSrc = imageSrc.Subset(rect);
                            //SKData dataIDPng = imageIDSrc.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                            SKBitmap bmpID = SKBitmap.FromImage(imageIDSrc);
                            bmpID200ppi_1 = bmpID.Resize(new SKSizeI((int)(imageIDSrc.Width * rate), (int)(imageIDSrc.Height * rate)), SKFilterQuality.High);
                        }
                        //SKData dataID200ppiPng = bmpID200ppi.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                        //using (FileStream fs = new FileStream("ID.png", FileMode.Create))
                        //{
                        //    dataID200ppiPng.SaveTo(fs);
                        //}
                        if (bmpID200ppi_2 == null)
                        {
                            SKRectI rect = new SKRectI(
                                (int)leftEdgeXOfIDImageInPixel - (int)(ppi * 0.2),
                                (int)topEdgeYOfIDImageInPixel - (int)(ppi * 0.2),
                                (int)leftEdgeXOfIDImageInPixel + widthOfIDImageInPixel + (int)(ppi * 0.2),
                                (int)topEdgeYOfIDImageInPixel + heightOfIDImageInPixel + (int)(ppi * 0.2));
                            if (rect.Top < 0) rect.Top = 0;
                            if (rect.Left < 0) rect.Left = 0;
                            if (rect.Right > imageSrc.Width) rect.Right = imageSrc.Width;
                            if (rect.Bottom > imageSrc.Height) rect.Bottom = imageSrc.Height;
                            SKImage imageIDSrc = imageSrc.Subset(rect);
                            //SKData dataIDPng = imageIDSrc.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                            SKBitmap bmpID = SKBitmap.FromImage(imageIDSrc);
                            bmpID200ppi_2 = bmpID.Resize(new SKSizeI((int)(imageIDSrc.Width * rate), (int)(imageIDSrc.Height * rate)), SKFilterQuality.High);
                        }
                    }

                    if (linesField.Count > 0)
                    {
                        // remove lines predicted as label because of height
                        int removedFromLinesField = linesField.RemoveAll(l => l.ExtGetHeight() < labelHeightFilterInPixel);
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] removed {removedFromLinesField} lines predicted as label removed from linesField [height < labelHeightInPixel ({labelHeightFilterInPixel})].");
                        int removedFromLinesMerged = lsLineMerged.RemoveAll(l => l.ExtGetHeight() < labelHeightFilterInPixel);
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] removed {removedFromLinesMerged} lines predicted as label removed from lsLineMerged [height < labelHeightInPixel ({labelHeightFilterInPixel})].");
                    }

                    if (linesField.Count > 0)
                    {
                        //var linesLeftOrder = linesField.OrderBy(l => l.BoundingBox[0]);
                        var linesLeftOrder = linesField.OrderBy(l => l.ExtGetLeft());
                        int countLinesField = linesField.Count;
                        int idxMedianLinesField = countLinesField / 2;
                        double? leftMedian = null;
                        if (linesLeftOrder != null && linesLeftOrder.Count() > 0)
                        {
                            leftMedian = linesLeftOrder.ElementAt(idxMedianLinesField).BoundingBox[0];
                        }
                        int numLinesField = linesField.Count;
                        //int idxMainFields = 0;

                        // predit y in inch and expected field for each file line  
                        foreach (Line line in linesField)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"line: {line.Text} EstimateCenterYInInch: {y}");
                        }

                        try
                        {
                            // filter lines near to line of Date of issue field
                            Line[] mergedLinesNearToDateOfIssue = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_DateOfIssue - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToDateOfIssue)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToDateOfIssue: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_DateOfIssue)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    if (!string.IsNullOrEmpty(line.Text))
                                    {
                                        string strNoBlank = line.Text.Replace(" ", String.Empty).Trim();
                                        Match matchDateOfIssue = regexDateOfIssue.Match(strNoBlank);
                                        if (matchDateOfIssue.Success)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {matchDateOfIssue.Value} --> DATE_OF_ISSUE");
                                            DATE_OF_ISSUE = matchDateOfIssue.Value;
                                            lsLineMerged.Remove(line);
                                            break;
                                        }
                                    }
                                }
                            }

                            // filter lines near to line of Sex field
                            Line[] mergedLinesNearToSex = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Sex - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToDateOfIssue)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToSex: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_Sex)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is under the label
                                    if (m_labelPHNIBK_Araw_ng_pagkaka_loob_Date_of_issue.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNIBK_Araw_ng_pagkaka_loob_Date_of_issue.LineMacthed.ExtGetVerticalCenter()))
                                        continue;

                                    if (!string.IsNullOrEmpty(line.Text))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> SEX");
                                        SEX = line.Text;
                                        lsLineMerged.Remove(line);
                                        break;
                                    }
                                }
                            }

                            // filter lines near to line of Blood Type field
                            Line[] mergedLinesNearToBloodType = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_BloodType - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToBloodType)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToBloodType: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_BloodType)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is under the label
                                    if (m_labelPHNIBK_Araw_ng_pagkaka_loob_Date_of_issue.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNIBK_Araw_ng_pagkaka_loob_Date_of_issue.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (m_labelPHNIBK_Kasarian_Sex.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNIBK_Kasarian_Sex.LineMacthed.ExtGetVerticalCenter()))
                                        continue;

                                    if (!string.IsNullOrEmpty(line.Text))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> BLOOD_TYPE");
                                        BLOOD_TYPE = line.Text;
                                        lsLineMerged.Remove(line);
                                        break;
                                    }
                                }
                            }

                            // filter lines near to line of Marital Status field
                            Line[] mergedLinesNearToMaritalStatus = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_MaritalStatus - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToMaritalStatus)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToMaritalStatus: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_MaritalStatus)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is under the label
                                    if (m_labelPHNIBK_Araw_ng_pagkaka_loob_Date_of_issue.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNIBK_Araw_ng_pagkaka_loob_Date_of_issue.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (m_labelPHNIBK_Kasarian_Sex.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNIBK_Kasarian_Sex.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (m_labelPHNIBK_Kalagayang_Sibil_Marital_Status.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNIBK_Kalagayang_Sibil_Marital_Status.LineMacthed.ExtGetVerticalCenter()))
                                        continue;

                                    if (!string.IsNullOrEmpty(line.Text))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> MARITAL_STATUS");
                                        MARITAL_STATUS = line.Text;
                                        lsLineMerged.Remove(line);
                                        break;
                                    }
                                }
                            }

                            // filter lines near to line of Place Of Birth line1 field
                            Line[] mergedLinesNearToPlaceOfBirthLine1 = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_PlaceOfBirthLine1 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToPlaceOfBirthLine1)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToPlaceOfBirthLine1: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_PlaceOfBirthLine1)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is under the label
                                    if (m_labelPHNIBK_Araw_ng_pagkaka_loob_Date_of_issue.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNIBK_Araw_ng_pagkaka_loob_Date_of_issue.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (m_labelPHNIBK_Kasarian_Sex.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNIBK_Kasarian_Sex.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (m_labelPHNIBK_Kalagayang_Sibil_Marital_Status.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNIBK_Kalagayang_Sibil_Marital_Status.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (m_labelPHNIBK_Lugar_ng_Kapanganakan_Place_of_Birth.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNIBK_Lugar_ng_Kapanganakan_Place_of_Birth.LineMacthed.ExtGetVerticalCenter()))
                                        continue;

                                    if (!string.IsNullOrEmpty(line.Text))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> POB");
                                        POB = line.Text;
                                        lsLineMerged.Remove(line);
                                        break;
                                    }
                                }
                            }

                            // filter lines near to line of Place Of Birth line1 field
                            Line[] mergedLinesNearToPlaceOfBirthLine2 = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_PlaceOfBirthLine2 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in mergedLinesNearToPlaceOfBirthLine2)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"mergedLinesNearToPlaceOfBirthLine2: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_PlaceOfBirthLine2)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is under the label
                                    if (m_labelPHNIBK_Araw_ng_pagkaka_loob_Date_of_issue.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNIBK_Araw_ng_pagkaka_loob_Date_of_issue.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (m_labelPHNIBK_Kasarian_Sex.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNIBK_Kasarian_Sex.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (m_labelPHNIBK_Kalagayang_Sibil_Marital_Status.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNIBK_Kalagayang_Sibil_Marital_Status.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (m_labelPHNIBK_Lugar_ng_Kapanganakan_Place_of_Birth.IsLabelFound && (line.ExtGetVerticalCenter() <= m_labelPHNIBK_Lugar_ng_Kapanganakan_Place_of_Birth.LineMacthed.ExtGetVerticalCenter()))
                                        continue;

                                    if (!string.IsNullOrEmpty(line.Text))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> POB");
                                        POB = POB.Trim() + " " + line.Text;
                                        lsLineMerged.Remove(line);
                                        break;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{ex}");
                        }
                    }
                }

                // read QR code
                if (bmpID200ppi_1 != null)
                {
                    SKImage img = SKImage.FromBitmap(bmpID200ppi_1);
                    int xQRCode = (int)(1.5f * 200f);
                    SKImage imgQR = img.Subset(new SKRectI(xQRCode, 0, img.Width, img.Height));
                    // read QR code from original image
                    resReadQRCode = ReadQRCode(imgQR);
                }

                // retry with wider image
                if (resReadQRCode == null && bmpID200ppi_2 != null)
                {
                    SKImage img = SKImage.FromBitmap(bmpID200ppi_2);
                    int xQRCode = (int)(1.5f * 200f);
                    SKImage imgQR = img.Subset(new SKRectI(xQRCode, 0, img.Width, img.Height));
                    // read QR code from original image
                    resReadQRCode = ReadQRCode(imgQR);
                }
            }

            if (resReadQRCode != null)
            {
                foreach (ZXing.Result aResult in resReadQRCode)
                {
                    Console.WriteLine(aResult.Text);
                    try
                    {
                        Newtonsoft.Json.Linq.JObject jsonObject = Newtonsoft.Json.Linq.JObject.Parse(aResult.Text);
                        if (jsonObject != null)
                        {
                            result.QRCode_DateIssued = (string)jsonObject.GetValue("DateIssued");
                            result.QRCode_Issuer = (string)jsonObject.GetValue("Issuer");
                            result.QRCode_alg = (string)jsonObject.GetValue("alg");
                            result.QRCode_signature = (string)jsonObject.GetValue("signature");
                            JObject qrcode_subject = (JObject)jsonObject.GetValue("subject");
                            if (qrcode_subject != null)
                            {
                                result.QRCode_subject_Suffix = (string)qrcode_subject.GetValue("Suffix");
                                result.QRCode_subject_lName = (string)qrcode_subject.GetValue("lName");
                                result.QRCode_subject_fName = (string)qrcode_subject.GetValue("fName");
                                result.QRCode_subject_mName = (string)qrcode_subject.GetValue("mName");
                                result.QRCode_subject_sex = (string)qrcode_subject.GetValue("sex");
                                result.QRCode_subject_BT = (string)qrcode_subject.GetValue("BF");
                                result.QRCode_subject_DOB = (string)qrcode_subject.GetValue("DOB");
                                result.QRCode_subject_POB = (string)qrcode_subject.GetValue("POB");
                                result.QRCode_subject_PCN = (string)qrcode_subject.GetValue("PCN");

                                result.IsQRCodeDataValid = true;
                                result.QRCodeData = aResult.Text;
                            }
                            Console.WriteLine($"DateIssued: {result.QRCode_DateIssued}");
                            Console.WriteLine($"Issuer: {result.QRCode_Issuer}");
                            Console.WriteLine($"alg: {result.QRCode_alg}");
                            Console.WriteLine($"signature: {result.QRCode_signature}");
                            Console.WriteLine($"subject:");
                            Console.WriteLine($"  Suffix: {result.QRCode_subject_Suffix}");
                            Console.WriteLine($"  lName: {result.QRCode_subject_lName}");
                            Console.WriteLine($"  fName: {result.QRCode_subject_fName}");
                            Console.WriteLine($"  mName: {result.QRCode_subject_mName}");
                            Console.WriteLine($"  sex: {result.QRCode_subject_sex}");
                            Console.WriteLine($"  BT: {result.QRCode_subject_BT}");
                            Console.WriteLine($"  DOB: {result.QRCode_subject_DOB}");
                            Console.WriteLine($"  POB: {result.QRCode_subject_POB}");
                            Console.WriteLine($"  PCN: {result.QRCode_subject_PCN}");

                            if (result.IsQRCodeDataValid)
                            {
                                DATE_OF_ISSUE = result.QRCode_DateIssued;
                                //confidence_DATE_OF_ISSUE = new Confidence(1);
                                LAST_NAME = result.QRCode_subject_lName;
                                //confidence_LAST_NAME = new Confidence(1);
                                GIVEN_NAMES = result.QRCode_subject_fName;
                                //confidence_GIVEN_NAMES = new Confidence(1);
                                MIDDLE_NAME = result.QRCode_subject_mName;
                                //confidence_MIDDLE_NAME = new Confidence(1);
                                SEX = result.QRCode_subject_sex;
                                //confidence_SEX = new Confidence(1);
                                DOB = result.QRCode_subject_DOB;
                                //confidence_DOB = new Confidence(1);
                                POB = result.QRCode_subject_POB;
                                //confidence_POB = new Confidence(1);
                                PCN = result.QRCode_subject_PCN;
                                //confidence_PCN = new Confidence(1);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {ex}");
                    }
                }
                /*
{
"DateIssued": "12 September 2022",
"Issuer": "PSA",
"subject": {
"Suffix": "",
"lName": "DELOS REYES",
"fName": "CHRISTIAN MARX",
"mName": "LOZADA",
"sex": "Male",
"BF": "[1,9]",
"DOB": "June 06, 1988",
"POB": "City of Caloocan,NCR, THIRD DISTRICT",
"PCN": "5931-9426-7546-1037"
},
"alg": "EDDSA",
"signature": "H6WF1LJOcXPiMlE6VTBgamixsA8GqxJ3tJpxpSDmR9qoCMj4/jBKJo3PyP3PdtmMBwXa/ZlypuIOkkcZxqzrAw=="
}
                */
            }

            // map to result and convert format 

            // LAST_NAME -> lastNameOrFullName 
            result.lastNameOrFullName = LAST_NAME;

            // GIVEN_NAMES -> firstName 
            result.firstName = GIVEN_NAMES;

            // MIDDLE_NAME -> middleName 
            result.middleName = MIDDLE_NAME;

            // IDNUM -> documentNumber
            result.documentNumber = PCN;

            // POB -> placeOfBirth
            result.placeOfBirth = POB;

            // DATE_OF_ISSUE "MMM dd, yyyy" -> dateOfBirth "yyyy-MM-dd"
            try
            {
                result.documentIssueDate = ConvertDateString(DATE_OF_ISSUE);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            // DOB "MMM dd, yyyy" -> dateOfBirth "yyyy-MM-dd"
            try
            {
                result.dateOfBirth = ConvertDateString(DOB);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            // Gender
            if (valueKasarian_Sex != null)
            {
                if (CheckCharInLine(valueKasarian_Sex, "MALE"))
                {
                    result.gender = "M";
                }
                else if (CheckCharInLine(valueKasarian_Sex, "FEMALE"))
                {
                    result.gender = "F";
                }
                else
                {
                    // unknown...
                    result.gender = valueKasarian_Sex.Text.Trim();
                }
            }
            else
            {
                if (SEX.ToUpper() == "MALE")
                {
                    result.gender = "M";
                }
                else if (SEX.ToLower() == "FEMALE")
                {
                    result.gender = "F";
                }
                else
                {
                    // unknown...
                    result.gender = SEX.Trim();
                }
            }

            // Marital Status
            /*
            Civil Status:		
            S	Single	
            M	Married	
            X	Separated/Divorced
            W	Widow/er
            */
            switch (MARITAL_STATUS.ToUpper())
            {
                case "SINGLE":
                    result.maritalStatus = "S";
                    break;
                case "MARRIED":
                    result.maritalStatus = "M";
                    break;
                case "SEPARATED":
                    result.maritalStatus = "X";
                    break;
                case "DIVORCED":
                    result.maritalStatus = "X";
                    break;
                case "WIDOW":
                    result.maritalStatus = "W";
                    break;
                case "WIDOWER":
                    result.maritalStatus = "W";
                    break;
                default:
                    result.maritalStatus = MARITAL_STATUS.Trim();   // Unknown
                    break;
            }

            result.Success = true;
            return result;
        }
#endif

        void CalcLeftTopEdgeInPixel(IList<LabelInfo> labelsFound, double? ppi, out double? leftEdgeXOfIDImageInPixel, out double? topEdgeYOfIDImageInPixel)
        {
            leftEdgeXOfIDImageInPixel = null;
            topEdgeYOfIDImageInPixel = null;
            //
            // calc top and left edge
            //
            List<double> lsTopEdgeYOfIDImageInPixelCalculated = new List<double>();
            List<double> lsLeftEdgeXOfIDImageInPixelCalculated = new List<double>();
            foreach (LabelInfo label in labelsFound)
            {
                double? topEdgeYOfIDImageInPixelCalculated = label.CalcTopEdgeYOfIDImageInPixel(ppi.Value);
                if (topEdgeYOfIDImageInPixelCalculated != null)
                {
                    lsTopEdgeYOfIDImageInPixelCalculated.Add(topEdgeYOfIDImageInPixelCalculated.Value);
                    double? y = label.PredictCenterYInPixel(ppi, topEdgeYOfIDImageInPixelCalculated);
                    System.Diagnostics.Debug.WriteLine($"{label.Title} PredictCenterYInPixel: {y}");
                }
                double? leftEdgeXOfIDImageInPixelCalculated = label.CalcLeftEdgeXOfIDImageInPixel(ppi.Value);
                if (leftEdgeXOfIDImageInPixelCalculated != null)
                {
                    lsLeftEdgeXOfIDImageInPixelCalculated.Add(leftEdgeXOfIDImageInPixelCalculated.Value);
                    double? x = label.PredictCenterXInPixel(ppi, leftEdgeXOfIDImageInPixelCalculated);
                    System.Diagnostics.Debug.WriteLine($"{label.Title} PredictCenterXInPixel: {x}");
                }
            }

            if (lsTopEdgeYOfIDImageInPixelCalculated.Count > 0)
            {
                topEdgeYOfIDImageInPixel = lsTopEdgeYOfIDImageInPixelCalculated.Average();
            }
            else
            {
                topEdgeYOfIDImageInPixel = null;
            }

            if (lsLeftEdgeXOfIDImageInPixelCalculated.Count > 0)
            {
                leftEdgeXOfIDImageInPixel = lsLeftEdgeXOfIDImageInPixelCalculated.Average();
            }
            else
            {
                leftEdgeXOfIDImageInPixel = null;
            }
        }


        #region QR Code
        ZXing.Result[] ReadQRCode(SkiaSharp.SKImage img)
        {
            SkiaSharp.SKBitmap bmp = SkiaSharp.SKBitmap.FromImage(img);
            SkiaSharp.SKSize skSize = bmp.Info.Size;
            if (skSize.Width > 500 || skSize.Height > 500)
            {
                int w = bmp.Info.Size.Width;
                int h = bmp.Info.Size.Height;
                
                int rate = 99;
                for (; rate > 0 && (w > 500 || h > 500); rate--)
                {
                    w = (int)Math.Round((bmp.Info.Size.Width * (rate / 100.0f)));
                    h = (int)Math.Round((bmp.Info.Size.Height * (rate / 100.0f)));
                }

                bmp = bmp.Resize(new SkiaSharp.SKSizeI(w, h), SkiaSharp.SKFilterQuality.High);
            }

#if DEBUG
            using (FileStream fs = new FileStream($"{DEBUG_OUTPUT_FOLDER}QR.png", FileMode.Create))
            {
                SkiaSharp.SKData dataImageQR = bmp.Encode(SkiaSharp.SKEncodedImageFormat.Png, 0);
                dataImageQR.SaveTo(fs);
            }
#endif

            ZXing.SkiaSharp.SKBitmapLuminanceSource skBmpLS = new ZXing.SkiaSharp.SKBitmapLuminanceSource(bmp);
            ZXing.Common.HybridBinarizer hybridBinarizer = new ZXing.Common.HybridBinarizer(skBmpLS);
            ZXing.BinaryBitmap bb = new ZXing.BinaryBitmap(hybridBinarizer);

            Newtonsoft.Json.Linq.JObject? jsonDataInQRCode = null;
            ZXing.SkiaSharp.BarcodeReader rdr = new ZXing.SkiaSharp.BarcodeReader();
            ZXing.Result[] resMulti = rdr.DecodeMultiple(bmp);
            return resMulti;
        }
        #endregion // QR Code

        /// format date string to "yyyy-MM-dd"
        /// <exception>
        /// int.Parse may throw exceptions.
        /// </exception>
        string ConvertDateString(string strDate)
        {
            string ret = "";
            if (!string.IsNullOrEmpty(strDate))
            {
                DateTime dtDoB;
                if (DateTime.TryParse(strDate, out dtDoB))
                {
                    int yyyy = dtDoB.Year;
                    int MM = dtDoB.Month;
                    int dd = dtDoB.Day;
                    ret = $"{yyyy:0000}-{MM:00}-{dd:00}";
                }
                else
                {
                    String[] token = strDate.Split(SEPARATOR_COMMA_DOT_BLANK);
                    if (token.Length >= 3)
                    {
                        int MM = 0;
                        int dd = 0;
                        int yyyy = 0;
                        foreach (string v in token)
                        {
                            if (string.IsNullOrEmpty(v))
                                continue;

                            if (MM == 0)
                            {
                                MM = MonthNameToNum(v);
                                continue;
                            }
                            if (dd == 0)
                            {
                                dd = int.Parse(v);
                                continue;
                            }
                            if (yyyy == 0)
                            {
                                yyyy = int.Parse(v);
                                continue;
                            }
                        }
                        ret = $"{yyyy:0000}-{MM:00}-{dd:00}";
                    }
                }
            }
            return ret;
        }

        int MonthNameToNum(string val)
        {
            if (string.IsNullOrEmpty(val))
                return 0;
            //JANUARY|FEBRUARY|MARCH|APRIL|MAY|JUNE|JULY|AUGUST|SEPTEMBER|OCTOBER|NOVEMBER|DECEMBER
            switch (val.ToUpper())
            {
                case "JAN":
                case "JANUARY":
                    return 1;
                case "FEB":
                case "FEBRUARY":
                    return 2;
                case "MAR":
                case "MARCH":
                    return 3;
                case "APR":
                case "APRIL":
                    return 4;
                case "MAY":
                    return 5;
                case "JUN":
                case "JUNE":
                    return 6;
                case "JUL":
                case "JULY":
                    return 7;
                case "AUG":
                case "AUGUST":
                    return 8;
                case "SEP":
                case "SEPTEMBER":
                    return 9;
                case "OCT":
                case "OCTOBER":
                    return 10;
                case "NOV":
                case "NOVEMBER":
                    return 11;
                case "DEC":
                case "DECEMBER":
                    return 12;
                default:
                    return 0;
            }
        }


#if true
        public ScanPHDLResult ExtractFieldsFromReadResultOfPHDL(LabelInfo[] labelsFound, Line[] linesMergedNotLabel, SKImage imageSrc, ImgProcLib.MatchTemplateIDCard? matchTemplatePHDL)
        {
            const double ACCEPTABLE_DIFF_IN_LINE = 0.1f;
            const double labelHeightFilterInInch = 0.07f;

            List<Line> lsLineMergedNotLabel = new List<Line>();
            lsLineMergedNotLabel.AddRange(linesMergedNotLabel);

            double centerYInInchOfField_Last_Name_First_Name_Middle_Name = 0.75f;
            double centerYInInchOfField_Nationality = 1.0f;
            double centerYInInchOfField_Address1 = 1.21f;
            double centerYInInchOfField_Address2 = 1.30f;
            double centerYInInchOfField_License_No = 1.52f;
            double centerYInInchOfField_Blood_Type = 1.69f;
            double centerYInInchOfField_Restrictions = 1.69f;

            Dictionary<string, MatchTemplateInfo> dicMatchTemplateInfo = new Dictionary<string, MatchTemplateInfo>();
            MatchTemplateInfo matchTmplPHDL_Flag = new MatchTemplateInfo("PHDL_Flag", "Flag", 0.8f, 0.39f, 0.26f);
            dicMatchTemplateInfo.Add("PHDL_Flag", matchTmplPHDL_Flag);
            MatchTemplateInfo matchTmplPHDL_Logo = new MatchTemplateInfo("PHDL_Logo", "Flag", 0.8f, 3.0f, 0.30f);
            dicMatchTemplateInfo.Add("PHDL_Logo", matchTmplPHDL_Logo);

            ScanPHDLResult result = new ScanPHDLResult();

            string LAST_NAME_FISRT_MIDDLE_NAME = "";
            //string LAST_NAME = "";
            //string FISRT_MIDDLE_NAME = "";
            //string NATIONALITY_SEX_DOB = "";
            string NATIONALITY = "";
            string SEX = "";
            string DOB = "";
            string ADDRESS_1 = "";
            Line lineAddress1 = null;
            string ADDRESS_2 = "";
            Line lineAddress2 = null;
            string ADDRESS = "";
            string LICENSE_NO_EXPIRY = "";
            string LICENSE_NO = "";
            string EXPIRY = "";

            //List<Line> linesField = new List<Line>();   // lines valid and not label

            //List<Line> lsLinesInSameLine = new List<Line>();
            //Line lineMerged = null;
           // List<Line> lsLineMerged = new List<Line>();

            // calc pixel per inch
            double? ppi = LabelInfo.CalcPixelPerInch(labelsFound);
            if (ppi == null)
            {
                Console.WriteLine("CalcPixelPerInch failed");
            }
            else
            {
                double labelHeightFilterInPixel = (double)labelHeightFilterInInch * ppi.Value;

                //
                // calc top and left edge
                //
                double? topEdgeYOfIDImageInPixel;
                double? leftEdgeXOfIDImageInPixel;
                CalcLeftTopEdgeInPixel(labelsFound, ppi, out leftEdgeXOfIDImageInPixel, out topEdgeYOfIDImageInPixel);
                if (topEdgeYOfIDImageInPixel != null && leftEdgeXOfIDImageInPixel != null)
                {
                    MatchTemplateResult matchTemplateResult = null;
                    SKData dataID200ppiPng = null;
                    DateTime dtStart = DateTime.Now;
                    bool bRetMatchTemplate = DoMatchTemplate(matchTemplatePHDL, imageSrc, topEdgeYOfIDImageInPixel.Value, leftEdgeXOfIDImageInPixel.Value, ppi.Value,
                        out matchTemplateResult, out dataID200ppiPng);
                    DateTime dtEnd = DateTime.Now;
                    result.timeElapsedLandmarkDetection = (int)(dtEnd - dtStart).TotalMilliseconds;
                    result.CardImage200ppiPngB64 = Convert.ToBase64String(dataID200ppiPng.ToArray());
                    if (bRetMatchTemplate)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTemplatePHDLResult: {matchTemplateResult.MatchResult}");
                        SKImage imgID200ppi = SKImage.FromEncodedData(dataID200ppiPng);
                        GenerateMatchTemplateResults(matchTemplateResult, dicMatchTemplateInfo, result.MatchTemplateResults);
                        result.landmarkImageBase64 = GenerateMatchTemplateResultImage(result.MatchTemplateResults, result.CardImage200ppiPngB64);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTemplate failed");
                    }
                }


                if (lsLineMergedNotLabel.Count > 0)
                {
                    // remove lines predicted as label because of height
                    int removedFromLinesField = lsLineMergedNotLabel.RemoveAll(l => l.ExtGetHeight() < labelHeightFilterInPixel);
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] removed {removedFromLinesField} lines predicted as label removed from linesField [height < labelHeightInPixel ({labelHeightFilterInPixel})].");
                }

                if (lsLineMergedNotLabel.Count > 0)
                {
                    //var linesLeftOrder = linesField.OrderBy(l => l.BoundingBox[0]);
                    var linesLeftOrder = lsLineMergedNotLabel.OrderBy(l => l.ExtGetLeft());
                    int countLinesField = lsLineMergedNotLabel.Count;
                    int idxMedianLinesField = countLinesField / 2;
                    double? leftMedian = null;
                    if (linesLeftOrder != null && linesLeftOrder.Count() > 0)
                    {
                        leftMedian = linesLeftOrder.ElementAt(idxMedianLinesField).BoundingBox[0];
                    }
                    int numLinesField = lsLineMergedNotLabel.Count;

                    // predit y in inch and expected field for each file line  
                    foreach (Line line in lsLineMergedNotLabel)
                    {
                        double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                        System.Diagnostics.Debug.WriteLine($"line: {line.Text} EstimateCenterYInInch: {y}");
                    }

                    try
                    {
                        // filter lines near to line of name field
                        FindFromMergedLine(ref lsLineMergedNotLabel, centerYInInchOfField_Last_Name_First_Name_Middle_Name,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the label
                                if (m_labelPHDL_Last_Name_First_Name_Middle_Name.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHDL_Last_Name_First_Name_Middle_Name.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHDL_Last_Name_First_Name.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHDL_Last_Name_First_Name.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHDL_Middle_Name.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHDL_Middle_Name.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHDL_Last_Name.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHDL_Middle_Name.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHDL_First_Name_Middle_Name.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHDL_First_Name_Middle_Name.LineMacthed.ExtGetVerticalCenter()))
                                    return false;
                                if (m_labelPHDL_First_Name.IsLabelFound && (l.ExtGetVerticalCenter() <= m_labelPHDL_First_Name.LineMacthed.ExtGetVerticalCenter()))
                                    return false;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> LAST_NAME_FISRT_MIDDLE_NAME");
                                LAST_NAME_FISRT_MIDDLE_NAME = l.Text;
                                return true;
                            }));
                        /*
                        // filter lines near to lien of name field
                        Line[] mergedLinesNearToLast_Name_First_Name_Middle_Name = lsLineMergedNotLabel.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Last_Name_First_Name_Middle_Name - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToLast_Name_First_Name_Middle_Name)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToLast_Name_First_Name_Middle_Name: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Last_Name_First_Name_Middle_Name)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the label
                                if (labelPHDL_Last_Name_First_Name_Middle_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= labelPHDL_Last_Name_First_Name_Middle_Name.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (labelPHDL_Last_Name_First_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= labelPHDL_Last_Name_First_Name.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (labelPHDL_Middle_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= labelPHDL_Middle_Name.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (labelPHDL_Last_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= labelPHDL_Middle_Name.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (labelPHDL_First_Name_Middle_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= labelPHDL_First_Name_Middle_Name.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (labelPHDL_First_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= labelPHDL_First_Name.LineMacthed.ExtGetVerticalCenter()))
                                    continue;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> LAST_NAME_FISRT_MIDDLE_NAME");
                                LAST_NAME_FISRT_MIDDLE_NAME = line.Text;
                                lsLineMergedNotLabel.Remove(line);
                                //heightName = line.ExtGetHeight();
                                //bottomName = line.ExtGetBottom();
                                break;
                            }
                        }
                        */
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }


                    try
                    {
                        // filter lines near to lien of nationality field
                        FindFromMergedLine(ref lsLineMergedNotLabel, centerYInInchOfField_Nationality,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the label
                                if (m_labelPHDL_Nationality.IsLabelFound)
                                {
                                    if (l.ExtGetVerticalCenter() <= m_labelPHDL_Nationality.LineMacthed.ExtGetVerticalCenter())
                                        return false;
                                    if (!m_labelPHDL_Nationality.IsFieldInLineJustUnderTheLabel(l))
                                        return false;
                                }

                                if (m_labelPHDL_Sex.IsLabelFound)
                                {
                                    if (l.ExtGetVerticalCenter() < m_labelPHDL_Sex.LineMacthed.ExtGetVerticalCenter())
                                        return false;
                                    if (!m_labelPHDL_Sex.IsFieldInLineJustUnderTheLabel(l))
                                        return false;
                                }

                                if (m_labelPHDL_DateOfBirth.IsLabelFound)
                                {
                                    if (l.ExtGetVerticalCenter() < m_labelPHDL_DateOfBirth.LineMacthed.ExtGetVerticalCenter())
                                        return false;
                                    if (!m_labelPHDL_DateOfBirth.IsFieldInLineJustUnderTheLabel(l))
                                        return false;
                                }

                                string[] fields = l.Text.Split(SEPARATOR_BLANK, StringSplitOptions.RemoveEmptyEntries);
                                bool isFieldFound = false;
                                foreach (string aField in fields)
                                {
                                    if (string.IsNullOrEmpty(NATIONALITY))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {aField} --> NATIONALITY");
                                        NATIONALITY = aField;
                                        isFieldFound = true;
                                        continue;
                                    }
                                    if (string.IsNullOrEmpty(SEX))
                                    {
                                        if (aField == "M" || aField == "F")
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {aField} --> SEX");
                                            SEX = aField;
                                            isFieldFound = true;
                                            continue;
                                        }
                                    }
                                    if (string.IsNullOrEmpty(DOB))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {aField} --> DOB");
                                        DOB = aField;
                                        isFieldFound = true;
                                        break;
                                    }
                                }
                                return isFieldFound;
                            }));
                        /*
                        // filter lines near to lien of nationality field
                        Line[] mergedLinesNearToNationality = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Nationality - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToNationality)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToNationality: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Nationality)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the label
                                if (labelNationality.IsLabelFound)
                                {
                                    if (line.ExtGetVerticalCenter() <= labelNationality.LineMacthed.ExtGetVerticalCenter())
                                        continue;
                                    if (!labelNationality.IsFieldInLineJustUnderTheLabel(line))
                                        continue;
                                }

                                if (labelSex.IsLabelFound)
                                {
                                    if (line.ExtGetVerticalCenter() < labelSex.LineMacthed.ExtGetVerticalCenter())
                                        continue;
                                    if (!labelSex.IsFieldInLineJustUnderTheLabel(line))
                                        continue;
                                }

                                if (labelDateOfBirth.IsLabelFound)
                                {
                                    if (line.ExtGetVerticalCenter() < labelDateOfBirth.LineMacthed.ExtGetVerticalCenter())
                                        continue;
                                    if (!labelDateOfBirth.IsFieldInLineJustUnderTheLabel(line))
                                        continue;
                                }

                                string[] fields = line.Text.Split(SEPARATOR_BLANK, StringSplitOptions.RemoveEmptyEntries);
                                bool isFieldFound = false;
                                foreach (string aField in fields)
                                {
                                    if (string.IsNullOrEmpty(NATIONALITY))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {aField} --> NATIONALITY");
                                        NATIONALITY = aField;
                                        isFieldFound = true;
                                        continue;
                                    }
                                    if (string.IsNullOrEmpty(SEX))
                                    {
                                        if (aField == "M" || aField == "F")
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {aField} --> SEX");
                                            SEX = aField;
                                            isFieldFound = true;
                                            continue;
                                        }
                                    }
                                    if (string.IsNullOrEmpty(DOB))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {aField} --> DOB");
                                        DOB = aField;
                                        isFieldFound = true;
                                        break;
                                    }
                                }
                                if (isFieldFound)
                                {
                                    lsLineMerged.Remove(line);
                                }
                            }
                        }
                        */

                        if (string.IsNullOrEmpty(NATIONALITY))
                        {
                            Line lineNationarity = null;
                            List<Line> lsLinesNearNationality = new List<Line>();
                            Line[] linesNearToNationality = lsLineMergedNotLabel.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Nationality - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in linesNearToNationality)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"linesNearToNationality: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_Nationality)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is under the label
                                    if (m_labelPHDL_Nationality.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() <= m_labelPHDL_Nationality.LineMacthed.ExtGetVerticalCenter())
                                            continue;

                                        if (string.IsNullOrEmpty(NATIONALITY))
                                        {
                                            if (line.Text == "PHL")
                                            {
                                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> NATIONALITY");
                                                NATIONALITY = line.Text;
                                                lineNationarity = line;
                                                lsLineMergedNotLabel.Remove(line);
                                                continue;
                                            }

                                            if (m_labelPHDL_Nationality.IsFieldJustUnderTheLabel(line))
                                            {
                                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> NATIONALITY");
                                                NATIONALITY = line.Text;
                                                lineNationarity = line;
                                                lsLineMergedNotLabel.Remove(line);
                                                continue;
                                            }
                                        }
                                    }

                                    if (lineNationarity != null)
                                    {
                                        // filter only lines on the same line of nationality
                                        if (IsLineInTheSameLine(line, lineNationarity))
                                        {
                                            lsLinesNearNationality.Add(line);
                                        }
                                        continue;
                                    }

                                    lsLinesNearNationality.Add(line);
                                }
                            }

                            if (lsLinesNearNationality.Count > 0)
                            {
                                Line[] sortedLinesNearNationality = lsLinesNearNationality.OrderBy(l => l.ExtGetLeft()).ToArray();
                                foreach (Line line in sortedLinesNearNationality)
                                {
                                    if (string.IsNullOrEmpty(NATIONALITY))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> NATIONALITY");
                                        NATIONALITY = line.Text;
                                        lineNationarity = line;
                                        lsLineMergedNotLabel.Remove(line);
                                        continue;
                                    }
                                    if (string.IsNullOrEmpty(SEX))
                                    {
                                        if (line.Text == "M" || line.Text == "F")
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> SEX");
                                            SEX = line.Text;
                                            lsLineMergedNotLabel.Remove(line);
                                            continue;
                                        }
                                    }
                                    if (string.IsNullOrEmpty(DOB))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> DOB");
                                        DOB = line.Text;
                                        lsLineMergedNotLabel.Remove(line);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        FindFromMergedLine(ref lsLineMergedNotLabel, centerYInInchOfField_Address1,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the label
                                if (m_labelPHDL_Address.IsLabelFound && (l.ExtGetVerticalCenter() < m_labelPHDL_Address.LineMacthed.ExtGetVerticalCenter()))
                                    return false;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> ADDRESS_1");
                                lineAddress1 = l;
                                ADDRESS_1 = lineAddress1.Text;
                                return true; ;
                            }));
                        /*
                        Line[] mergedLinesNearToAddress1 = lsLineMergedNotLabel.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address1 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress1)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress1: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address1)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the label
                                if (labelPHDL_Address.IsLabelFound && (line.ExtGetVerticalCenter() < labelPHDL_Address.LineMacthed.ExtGetVerticalCenter()))
                                    continue;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS_1");
                                lineAddress1 = line;
                                lsLineMergedNotLabel.Remove(line);
                                ADDRESS_1 = lineAddress1.Text;
                                break;
                            }
                        }
                        */
                        FindFromMergedLine(ref lsLineMergedNotLabel, centerYInInchOfField_Address2,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the Addres1
                                if (lineAddress1 != null && (l.ExtGetVerticalCenter() <= lineAddress1.ExtGetVerticalCenter()))
                                    return false;

                                // the 2nd line of Address
                                if (IsFieldJustUnderTheLine(lineAddress1, l))
                                {
                                    // the 1st line of Address
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {l.Text} --> ADDRESS_2");
                                    lineAddress2 = l;
                                    ADDRESS_2 = lineAddress2.Text;
                                    return true;
                                }
                                return false;
                            }));
                        /*
                        Line[] mergedLinesNearToAddress2 = lsLineMergedNotLabel.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address2 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress2)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress2: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address2)) <= (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the Addres1
                                if (lineAddress1 != null && (line.ExtGetVerticalCenter() <= lineAddress1.ExtGetVerticalCenter()))
                                    continue;

                                // the 2nd line of Address
                                if (IsFieldJustUnderTheLine(lineAddress1, line))
                                {
                                    // the 1st line of Address
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS_2");
                                    lineAddress2 = line;
                                    lsLineMerged.Remove(line);
                                    ADDRESS_2 = lineAddress2.Text;
                                    break;
                                }
                            }
                        }
                        */
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        FindFromMergedLine(ref lsLineMergedNotLabel, centerYInInchOfField_License_No,
                            ACCEPTABLE_DIFF_IN_LINE, ppi, topEdgeYOfIDImageInPixel,
                            new ValidateLine(l =>
                            {
                                // check if the line is under the Addres1
                                if (lineAddress1 != null && (l.ExtGetVerticalCenter() <= lineAddress1.ExtGetVerticalCenter()))
                                    return false;

                                // 'License No' should be just under the 2nd line of Address
                                if (lineAddress2 != null && !IsFieldJustUnderTheLine(lineAddress2, l, 2.0f))
                                    return false;
                                else if (lineAddress1 != null && !IsFieldJustUnderTheLine(lineAddress1, l, 3.0f))
                                    return false;

                                if (string.IsNullOrEmpty(LICENSE_NO_EXPIRY))
                                {
                                    //System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> LICENSE_NO_EXPIRY");
                                    //LICENSE_NO_EXPIRY = line.Text;
                                    string[] splited = l.Text.Split(SEPARATOR_BLANK, StringSplitOptions.RemoveEmptyEntries);
                                    if (splited != null && splited.Length > 0)
                                    {
                                        string strLicenseNo = "";
                                        string strExpiryDate = "";
                                        if (splited.Length > 1)
                                        {
                                            strLicenseNo = splited[0].Trim();
                                            if(splited.Length == 2)
                                            {
                                                strExpiryDate = splited[1].Trim().Replace(" ", "").Trim();
                                            }
                                            else
                                            {
                                                // The last word might be 'Agency Code'. Expiry date should be between 'License No' and 'Agency Code'
                                                for (int i = 1; i < splited.Length - 1; i++)
                                                {
                                                    strExpiryDate += splited[i].Trim().Replace(" ", "").Trim();
                                                }
                                            }
                                        }
                                        else
                                        {
                                            strLicenseNo = splited[0].Trim();
                                        }

                                        // check validity of license number
                                        StringBuilder sbNumInLicenseNo = new StringBuilder();
                                        foreach (char c in strLicenseNo)
                                        {
                                            if (c >= '0' && c <= '9')
                                            {
                                                sbNumInLicenseNo.Append(c);
                                            }
                                        }
                                        if (sbNumInLicenseNo.Length > 6)
                                        {
                                            // license number seems valid, take it.
                                            LICENSE_NO_EXPIRY = l.Text;
                                            LICENSE_NO = strLicenseNo;
                                            EXPIRY = strExpiryDate;
                                            return true; ;
                                        }
                                    }
                                }

                                return false;
                            }));
                        /*
                        Line[] mergedLinesNearLicenseNo = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_License_No - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearLicenseNo)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearLicenseNo: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_License_No)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the label
                                if (labelLicense_No.IsLabelFound)
                                {
                                    if (line.ExtGetVerticalCenter() <= labelLicense_No.LineMacthed.ExtGetVerticalCenter())
                                        continue;
                                    if (!labelLicense_No.IsFieldInLineJustUnderTheLabel(line))
                                        continue;
                                }

                                if (labelExpiration_Date.IsLabelFound)
                                {
                                    if (line.ExtGetVerticalCenter() <= labelExpiration_Date.LineMacthed.ExtGetVerticalCenter())
                                        continue;
                                    if (!labelExpiration_Date.IsFieldInLineJustUnderTheLabel(line))
                                        continue;
                                }

                                if (labelAgency_Code.IsLabelFound)
                                {
                                    if (line.ExtGetVerticalCenter() <= labelAgency_Code.LineMacthed.ExtGetVerticalCenter())
                                        continue;
                                    if (!labelAgency_Code.IsFieldInLineJustUnderTheLabel(line))
                                        continue;
                                }

                                if (string.IsNullOrEmpty(LICENSE_NO_EXPIRY))
                                {
                                    //System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> LICENSE_NO_EXPIRY");
                                    //LICENSE_NO_EXPIRY = line.Text;
                                    string[] splited = line.Text.Split(SEPARATOR_BLANK, StringSplitOptions.RemoveEmptyEntries);
                                    if (splited != null && splited.Length > 0)
                                    {
                                        string strLicenseNo = "";
                                        string strExpiryDate = "";
                                        if (splited.Length > 1)
                                        {
                                            strLicenseNo = splited[0].Trim();
                                            strExpiryDate = splited[1].Trim().Replace(" ", "").Trim();
                                        }
                                        else
                                        {
                                            strLicenseNo = splited[0].Trim();
                                        }

                                        // check validity of license number
                                        StringBuilder sbNumInLicenseNo = new StringBuilder();
                                        foreach (char c in strLicenseNo)
                                        {
                                            if (c >= '0' && c <= '9')
                                            {
                                                sbNumInLicenseNo.Append(c);
                                            }
                                        }
                                        if (sbNumInLicenseNo.Length > 6)
                                        {
                                            // license number seems valid, take it.
                                            LICENSE_NO_EXPIRY = line.Text;
                                            lsLineMerged.Remove(line);
                                            LICENSE_NO = strLicenseNo;
                                            EXPIRY = strExpiryDate;
                                            lsLineMerged.Remove(line);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        */
                        if (string.IsNullOrEmpty(LICENSE_NO))
                        {
                            Line[] linesNearLicenseNo = lsLineMergedNotLabel.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_License_No - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            // filter lines near to license no
                            Line lineLicenseNo = null;
                            List<Line> lsLinesNearLicenseNo = new List<Line>();
                            foreach (Line line in linesNearLicenseNo)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"linesNearLicenseNo: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_License_No)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is under the label
                                    if (m_labelPHDL_License_No.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() <= m_labelPHDL_License_No.LineMacthed.ExtGetVerticalCenter())
                                            continue;
                                    }

                                    if (m_labelPHDL_Expiration_Date.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() <= m_labelPHDL_Expiration_Date.LineMacthed.ExtGetVerticalCenter())
                                            continue;
                                    }

                                    if (m_labelPHDL_Agency_Code.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() <= m_labelPHDL_Agency_Code.LineMacthed.ExtGetVerticalCenter())
                                            continue;
                                    }

                                    if (string.IsNullOrEmpty(LICENSE_NO_EXPIRY))
                                    {
                                        if (m_labelPHDL_License_No.IsLabelFound && m_labelPHDL_License_No.IsFieldJustUnderTheLabel(line)
                                            || m_labelPHDL_Expiration_Date.IsLabelFound && m_labelPHDL_Expiration_Date.IsFieldUnderTheLabel(line)
                                            || m_labelPHDL_Agency_Code.IsLabelFound && m_labelPHDL_Agency_Code.IsFieldUnderTheLabel(line)
                                            || lineAddress1 != null && lineAddress1.ExtGetBottom() < line.ExtGetTop()
                                            )
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> LICENSE_NO_EXPIRY");
                                            LICENSE_NO_EXPIRY = line.Text;
                                            lineLicenseNo = line;
                                            lsLineMergedNotLabel.Remove(line);
                                            // LICENSE_NO_EXPIRY -> documentNumber, documentExpirationDate
                                            string[] splited = LICENSE_NO_EXPIRY.Split(SEPARATOR_BLANK, 2, StringSplitOptions.RemoveEmptyEntries);
                                            if (splited != null && splited.Length > 0)
                                            {
                                                if (splited.Length > 1)
                                                {
                                                    LICENSE_NO = splited[0].Trim();
                                                    EXPIRY = splited[1].Trim().Replace(" ", "").Trim();
                                                }
                                                else
                                                {
                                                    LICENSE_NO = splited[0].Trim(); ;
                                                }
                                            }
                                            continue;
                                        }
                                    }

                                    if (lineLicenseNo != null)
                                    {
                                        // filter only lines on the same line of nationality
                                        if (IsLineInTheSameLine(line, lineLicenseNo))
                                        {
                                            lsLinesNearLicenseNo.Add(line);
                                        }
                                        continue;
                                    }

                                    lsLinesNearLicenseNo.Add(line);
                                }
                            }

                            // 
                            Line[] sortedLinesNearLicenseNo = lsLinesNearLicenseNo.OrderBy(l => l.ExtGetLeft()).ToArray();
                            foreach (Line line in sortedLinesNearLicenseNo)
                            {
                                if (string.IsNullOrEmpty(LICENSE_NO_EXPIRY))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> LICENSE_NO_EXPIRY");
                                    LICENSE_NO_EXPIRY = line.Text;
                                    lineLicenseNo = line;
                                    // LICENSE_NO_EXPIRY -> documentNumber, documentExpirationDate
                                    string[] splited = LICENSE_NO_EXPIRY.Split(SEPARATOR_BLANK, 2, StringSplitOptions.RemoveEmptyEntries);
                                    if (splited != null && splited.Length > 0)
                                    {
                                        if (splited.Length > 1)
                                        {
                                            LICENSE_NO = splited[0].Trim();
                                            EXPIRY = splited[1].Trim().Replace(" ", "").Trim();
                                        }
                                        else
                                        {
                                            LICENSE_NO = splited[0].Trim(); ;
                                        }
                                    }
                                    continue;
                                }
                                if (string.IsNullOrEmpty(LICENSE_NO))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> LICENSE_NO");
                                    LICENSE_NO = line.Text;
                                    continue;
                                }
                                if (string.IsNullOrEmpty(EXPIRY))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> EXPIRY");
                                    EXPIRY = line.Text;
                                    break;
                                }
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }
                }
            } // ppi != null

            // map to result and convert format 
            List<string> lsMissingFields = new List<string>();

            // NAME -> lastNameOrFullName 
            if (string.IsNullOrEmpty(LAST_NAME_FISRT_MIDDLE_NAME))
            {
                lsMissingFields.Add("LAST_NAME_FISRT_MIDDLE_NAME");
            }
            else
            {
                result.lastNameOrFullName = LAST_NAME_FISRT_MIDDLE_NAME;
            }


            string[] namesSplit = LAST_NAME_FISRT_MIDDLE_NAME.Split(SEPARATOR_COMMA_DOT_BLANK, 2);
            if (namesSplit != null && namesSplit.Length > 0)
            {
                result.lastNameOrFullName = namesSplit[0];

                if (namesSplit.Length > 1)
                {
                    if (namesSplit.Length > 2)
                    {
                        result.firstName = namesSplit[1];
                        result.middleName = namesSplit[2];
                    }
                    else
                    {
                        result.firstName = namesSplit[1];
                    }
                }
            }

            // LICENSE_NO -> documentNumber
            if (string.IsNullOrEmpty(LICENSE_NO))
            {
                lsMissingFields.Add("LICENSE_NO");
            }
            else
            {
                result.documentNumber = LICENSE_NO;
            }

            // EXPIRY "yyyy/MM/dd" -> documentExpirationDate "yyyy-MM-dd"
            if (EXPIRY.Length == 10)
            {
                try
                {
                    int yyyy = int.Parse(EXPIRY.Substring(0, 4));
                    int MM = int.Parse(EXPIRY.Substring(5, 2));
                    int dd = int.Parse(EXPIRY.Substring(8, 2));
                    result.documentExpirationDate = $"{yyyy:0000}-{MM:00}-{dd:00}";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    lsMissingFields.Add("EXPIRY");
                }
            }
            else
            {
                lsMissingFields.Add("EXPIRY");
            }

            // nationality 3 letter code
            if (string.IsNullOrEmpty(NATIONALITY))
            {
                lsMissingFields.Add("NATIONALITY");
            }
            else
            {
                Code.Country country = FindCountry(NATIONALITY);
                if (country != null)
                    result.nationality = country.ncode;
                else
                    result.nationality = NATIONALITY;
            }

            if (string.IsNullOrEmpty(SEX))
            {
                lsMissingFields.Add("SEX");
            }
            else
            {
                result.gender = SEX;
            }

            // DOB "yyyy/MM/dd" -> documentExpirationDate "yyyy-MM-dd"
            try
            {
                result.dateOfBirth = "";
                if (DOB.Length == 10)
                {
                    int yyyy = int.Parse(DOB.Substring(0, 4));
                    int MM = int.Parse(DOB.Substring(5, 2));
                    int dd = int.Parse(DOB.Substring(8, 2));
                    result.dateOfBirth = $"{yyyy:0000}-{MM:00}-{dd:00}";
                }
                else
                {
                    lsMissingFields.Add("DOB");
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                lsMissingFields.Add("DOB");
            }

            // ADDRESS_1, ADDRESS_2 -> addressLine1, addressLine2
            if (string.IsNullOrEmpty(ADDRESS_1))
            {
                lsMissingFields.Add("ADDRESS_1");
            }
            else
            {
                result.addressLine1 = ADDRESS_1;
            }

            result.addressLine2 = ADDRESS_2;

            // determine success or not
            if (lsMissingFields.Count == 0)
            {
                result.Success = true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExtractFieldsFromReadResultOfPHDL result NOT success");
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
#else
        public static ScanPHDLResult ExtractFieldsFromReadResultOfPHDL(IList<Line> linesAll, SKImage imageSrc, ImgProcLib.MatchTemplateIDCard? matchTemplatePHDL)
        {
            //char[] SEPARATOR_NAME = new char[] { ',', '.', ' ' };
            List<LabelInfo> labelsToFind = new List<LabelInfo>();
            LabelInfo m_labelPHNI_Republic_of_the_Philippines = new LabelInfo("REPUBLIC OF THE PHILIPPINES", 0.13f, 1.625f);
            labelsToFind.Add(m_labelPHNI_Republic_of_the_Philippines);
            LabelInfo labelDEPARTMENT_OF_TRANSPORTATION = new LabelInfo("DEPARTMENT OF TRANSPORTATION", 0.26, 1.625f);
            labelsToFind.Add(labelDEPARTMENT_OF_TRANSPORTATION);
            LabelInfo labelLAND_TRANSPORTATION_OFFICE = new LabelInfo("LAND TRANSPORTATION OFFICE", 0.35f, 1.625f);
            labelsToFind.Add(labelLAND_TRANSPORTATION_OFFICE);
            LabelInfo labelNON_PROFESSIONAL_DRIVERS_LICENSE = new LabelInfo("NON-PROFESSIONAL DRIVER'S LICENSE", 0.46f, 1.625f);
            labelsToFind.Add(labelNON_PROFESSIONAL_DRIVERS_LICENSE);
            LabelInfo labelPROFESSIONAL_DRIVERS_LICENSE = new LabelInfo("PROFESSIONAL DRIVER'S LICENSE", 0.46f, 1.625f);
            labelsToFind.Add(labelPROFESSIONAL_DRIVERS_LICENSE);
            LabelInfo labelDRIVERS_LICENSE = new LabelInfo("DRIVER'S LICENSE", 0.46f, null);
            labelsToFind.Add(labelDRIVERS_LICENSE);
            LabelInfo[] labelsAboveFields = {
                m_labelPHNI_Republic_of_the_Philippines,
                labelDEPARTMENT_OF_TRANSPORTATION,
                labelLAND_TRANSPORTATION_OFFICE,
                labelNON_PROFESSIONAL_DRIVERS_LICENSE,
                labelPROFESSIONAL_DRIVERS_LICENSE,
                labelDRIVERS_LICENSE
            };
            LabelInfo labelLast_Name_First_Name_Middle_Name = new LabelInfo("Last Name, First Name, Middle Name", 0.68f, null);
            labelsToFind.Add(labelLast_Name_First_Name_Middle_Name);
            LabelInfo labelLast_Name_First_Name = new LabelInfo("Last Name, First Name", 0.68f, null);
            labelLast_Name_First_Name_Middle_Name.Childs.Add(labelLast_Name_First_Name);
            LabelInfo labelMiddle_Name = new LabelInfo("Middle Name", 0.68f, null);
            labelLast_Name_First_Name_Middle_Name.Childs.Add(labelMiddle_Name);
            LabelInfo labelLast_Name = new LabelInfo("Last Name", 0.68f, null);
            labelLast_Name_First_Name_Middle_Name.Childs.Add(labelLast_Name);
            LabelInfo labelFirst_Name_Middle_Name = new LabelInfo("First Name, Middle Name", 0.68f, null);
            labelLast_Name_First_Name_Middle_Name.Childs.Add(labelFirst_Name_Middle_Name);
            LabelInfo labelFirst_Name = new LabelInfo("First Name", 0.68f, null);
            labelLast_Name_First_Name_Middle_Name.Childs.Add(labelLast_Name);
            LabelInfo labelNationality = new LabelInfo("Nationality", 0.9f, null);
            labelsToFind.Add(labelNationality);
            LabelInfo labelSex = new LabelInfo("Sex");
            labelsToFind.Add(labelSex);
            LabelInfo labelDateOfBirth = new LabelInfo("Date Of Birth");
            labelsToFind.Add(labelDateOfBirth);
            LabelInfo labelWeight_kg_Height_m = new LabelInfo("Weight (kg) Height(m)");
            labelsToFind.Add(labelWeight_kg_Height_m);
            LabelInfo labelWeight_kg = new LabelInfo("Weight (kg)");
            labelWeight_kg_Height_m.Childs.Add(labelWeight_kg);
            LabelInfo labelHeight_m = new LabelInfo("Height(m)");
            labelWeight_kg_Height_m.Childs.Add(labelHeight_m);
            LabelInfo labelAddress = new LabelInfo("Address", 1.11f, null);
            labelsToFind.Add(labelAddress);
            LabelInfo labelLicense_No = new LabelInfo("License No.");
            labelsToFind.Add(labelLicense_No);
            LabelInfo labelExpiration_Date = new LabelInfo("Expiration Date");
            labelsToFind.Add(labelExpiration_Date);
            LabelInfo labelAgency_Code = new LabelInfo("Agency Code");
            labelsToFind.Add(labelAgency_Code);
            LabelInfo labelBlood_Type = new LabelInfo("Blood Type");
            labelsToFind.Add(labelBlood_Type);
            LabelInfo labelEyes_Color = new LabelInfo("Eyes Color");
            labelsToFind.Add(labelEyes_Color);
            LabelInfo labelRestrictions = new LabelInfo("Restrictions");
            labelsToFind.Add(labelRestrictions);
            LabelInfo labelConditions = new LabelInfo("Conditions");
            labelsToFind.Add(labelConditions);

            double centerYInInchOfField_Last_Name_First_Name_Middle_Name = 0.75f;
            double centerYInInchOfField_Nationality = 1.0f;
            double centerYInInchOfField_Address1 = 1.21f;
            double centerYInInchOfField_Address2 = 1.30f;
            double centerYInInchOfField_License_No = 1.52f;
            double centerYInInchOfField_Blood_Type = 1.69f;
            double centerYInInchOfField_Restrictions = 1.69f;

            Dictionary<string, MatchTemplateInfo> dicMatchTemplateInfo = new Dictionary<string, MatchTemplateInfo>();
            MatchTemplateInfo matchTmplPHDL_Flag = new MatchTemplateInfo("PHDL_Flag", "Flag", 0.8f, 0.39f, 0.26f);
            dicMatchTemplateInfo.Add("PHDL_Flag", matchTmplPHDL_Flag);
            MatchTemplateInfo matchTmplPHDL_Logo = new MatchTemplateInfo("PHDL_Logo", "Flag", 0.8f, 3.0f, 0.30f);
            dicMatchTemplateInfo.Add("PHDL_Logo", matchTmplPHDL_Logo);

            ScanPHDLResult result = new ScanPHDLResult();

            string LAST_NAME_FISRT_MIDDLE_NAME = "";
            string LAST_NAME = "";
            string FISRT_MIDDLE_NAME = "";
            string NATIONALITY_SEX_DOB = "";
            string NATIONALITY = "";
            string SEX = "";
            string DOB = "";
            string ADDRESS_1 = "";
            Line lineAddress1 = null;
            string ADDRESS_2 = "";
            Line lineAddress2 = null;
            string ADDRESS = "";
            string LICENSE_NO_EXPIRY = "";
            string LICENSE_NO = "";
            string EXPIRY = "";
            const double ACCEPTABLE_DIFF_IN_LINE = 0.1f;

            List<Line> linesField = new List<Line>();   // lines valid and not label
            List<LabelInfo> labelsFound = new List<LabelInfo>();

            List<Line> lsLinesInSameLine = new List<Line>();
            Line lineMerged = null;
            List<Line> lsLineMerged = new List<Line>();
            // find labels exactly match
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

                if(lsLinesInSameLine.Count == 0)
                {
                    lsLinesInSameLine.Add(line);
                    lineMerged = line;
                    continue;
                }
                
                if(IsLineInTheSameLine(lineMerged, line))
                {
                    lsLinesInSameLine.Add(line);
                    lineMerged = lineMerged.MergedLine(line);
                    continue;
                }

                Line lineMergedToCheck = lineMerged;
                lineMerged = line;  // for next turn
                Line[] arLinesInSameLine = lsLinesInSameLine.ToArray();
                Line[] linesToAddFields = FindLabelInLine(ref labelsFound, arLinesInSameLine, lineMergedToCheck, labelsToFind.ToArray(), labelsAboveFields);
                lsLinesInSameLine.Clear();
                lsLinesInSameLine.Add(line);

                if (linesToAddFields != null && linesToAddFields.Length > 0)
                {
                    linesField.AddRange(linesToAddFields);
                    lsLineMerged.Add(lineMergedToCheck);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] lineMergedToCheck: {lineMergedToCheck.Text} is not field.");
                }

            }// foreach lines in other columns

            // find labels in the last line
            if (lineMerged != null)
            {
                Line[] arLinesInSameLine = lsLinesInSameLine.ToArray();
                Line[] linesToAddFields = FindLabelInLine(ref labelsFound, arLinesInSameLine, lineMerged, labelsToFind.ToArray(), labelsAboveFields);
                lsLinesInSameLine.Clear();
                if (linesToAddFields != null && linesToAddFields.Length > 0)
                {
                    linesField.AddRange(linesToAddFields);
                    lsLineMerged.Add(lineMerged);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] lineMergedToCheck: {lineMerged.Text} is not field.");
                }
            }

            // calc pixel per inch
            double? ppi = LabelInfo.CalcPixelPerInch(labelsFound);
            if(ppi == null)
            {
                Console.WriteLine("CalcPixelPerInch failed");
            }
            else
            {
                const double labelHeightFilterInInch = 0.08f;
                double labelHeightFilterInPixel = (double)labelHeightFilterInInch * ppi.Value;

                //
                // calc top and left edge
                //
                double? topEdgeYOfIDImageInPixel;
                double? leftEdgeXOfIDImageInPixel;
                CalcLeftTopEdgeInPixel(labelsFound, ppi, out leftEdgeXOfIDImageInPixel, out topEdgeYOfIDImageInPixel);
                if (topEdgeYOfIDImageInPixel != null && leftEdgeXOfIDImageInPixel != null)
                {
                    int widthOfIDImageInPixel = (int)(3.35f * ppi.Value);
                    int heightOfIDImageInPixel = (int)(2.15f * ppi.Value);
                    SKRectI rect = new SKRectI(
                        (int)leftEdgeXOfIDImageInPixel - (int)(ppi * 0.1),
                        (int)topEdgeYOfIDImageInPixel - (int)(ppi * 0.1),
                        (int)leftEdgeXOfIDImageInPixel + widthOfIDImageInPixel + (int)(ppi * 0.1),
                        (int)topEdgeYOfIDImageInPixel + heightOfIDImageInPixel + (int)(ppi * 0.1));
                    if (rect.Top < 0) rect.Top = 0;
                    if (rect.Left < 0) rect.Left = 0;
                    if (rect.Right > imageSrc.Width) rect.Right = imageSrc.Width;
                    if (rect.Bottom > imageSrc.Height) rect.Bottom = imageSrc.Height;
                    SKImage imageIDSrc = imageSrc.Subset(rect);
                    //SKData dataIDPng = imageIDSrc.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                    double rate = 200.0f / ppi.Value;
                    SKBitmap bmpID = SKBitmap.FromImage(imageIDSrc);
                    SKBitmap bmpID200ppi = bmpID.Resize(new SKSizeI((int)(imageIDSrc.Width * rate), (int)(imageIDSrc.Height * rate)), SKFilterQuality.High);
                    SKData dataID200ppiPng = bmpID200ppi.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                    result.CardImage200ppiPngB64 = Convert.ToBase64String(dataID200ppiPng.ToArray());
                    //using (FileStream fs = new FileStream("ID.png", FileMode.Create))
                    //{
                    //    dataID200ppiPng.SaveTo(fs);
                    //}

                    if (matchTemplatePHDL != null)
                    {
                        MatchTemplateResult matchTemplateResult = matchTemplatePHDL.DoMatchTemplate(dataID200ppiPng.ToArray());
                        if (matchTemplateResult != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTemplateResult.MatchResult: {matchTemplateResult.MatchResult}");
                            SKImage imgID200ppi = SKImage.FromBitmap(bmpID200ppi);

                            foreach (string key in matchTemplateResult.MatchResult.Keys)
                            {
                                MatchTemplateResultItem matchTemplateResultItem = matchTemplateResult.MatchResult[key];
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] key: {key} MatchResult: {matchTemplateResultItem.MatchResult} x: {matchTemplateResultItem.LocX} y: {matchTemplateResultItem.LocY} w: {matchTemplateResultItem.Width} h: {matchTemplateResultItem.Height}");
                                if (dicMatchTemplateInfo.ContainsKey(key))
                                {
                                    MatchTemplateResultInfo matchTemplateResultInfo = new MatchTemplateResultInfo();
                                    matchTemplateResultInfo.Title = key;
                                    matchTemplateResultInfo.MatchTemplateInfo = dicMatchTemplateInfo[key];
                                    matchTemplateResultInfo.MatchTemplateInfo.MatchResult = matchTemplateResultItem.MatchResult;
                                    matchTemplateResultInfo.MatchTemplateInfo.ResultLocX = matchTemplateResultItem.LocX;
                                    matchTemplateResultInfo.MatchTemplateInfo.ResultLocY = matchTemplateResultItem.LocY;
                                    matchTemplateResultInfo.MatchTemplateInfo.ResultWidth = matchTemplateResultItem.Width;
                                    matchTemplateResultInfo.MatchTemplateInfo.ResultHeight = matchTemplateResultItem.Height;
                                    double? dist = matchTemplateResultInfo.GetDistanceFromExpectedCenterInInch();
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] key: {key} dist: {dist} ");
                                    result.MatchTemplateResults.Add(key, matchTemplateResultInfo);
                                }
                                /*
                                using (FileStream fs = new FileStream(matchTemplateMyKadResultItem.GetName() + ".png", FileMode.Create))
                                {
                                    SKRectI rectLandmark = new SKRectI((int)matchTemplateMyKadResultItem.LocX, (int)matchTemplateMyKadResultItem.LocY, (int)matchTemplateMyKadResultItem.LocX + matchTemplateMyKadResultItem.Width, (int)matchTemplateMyKadResultItem.LocY + matchTemplateMyKadResultItem.Height);
                                    SKImage imageLandmark = imgID200ppi.Subset(rectLandmark);
                                    SKData dataLandmark = imageLandmark.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                                    dataLandmark.SaveTo(fs);
                                }
                                */
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTemplateResult is null");
                    }
                }

                /*
                List<double> lsTopEdgeYOfIDImageInPixelCalculated = new List<double>();
                foreach (LabelInfo label in labelsFound)
                {
                    double? topEdgeYOfIDImageInPixelCalculated = label.CalcTopEdgeYOfIDImageInPixel(ppi.Value);
                    if (topEdgeYOfIDImageInPixelCalculated != null)
                    {
                        lsTopEdgeYOfIDImageInPixelCalculated.Add(topEdgeYOfIDImageInPixelCalculated.Value);
                        double? y = label.PredictCenterYInPixel(ppi, topEdgeYOfIDImageInPixelCalculated);
                        System.Diagnostics.Debug.WriteLine($"{label.Title} PredictCenterYInPixel: {y}");
                    }
                }
                double? topEdgeYOfIDImageInPixel;
                if (lsTopEdgeYOfIDImageInPixelCalculated.Count > 0)
                {
                    topEdgeYOfIDImageInPixel = lsTopEdgeYOfIDImageInPixelCalculated.Average();
                }
                else
                {
                    topEdgeYOfIDImageInPixel = null;
                }
                */

                if(linesField.Count > 0)
                {
                    // remove lines predicted as label because of height
                    int removedFromLinesField = linesField.RemoveAll(l => l.ExtGetHeight() < labelHeightFilterInPixel);
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] removed {removedFromLinesField} lines predicted as label removed from linesField [height < labelHeightInPixel ({labelHeightFilterInPixel})].");
                    int removedFromLinesMerged = lsLineMerged.RemoveAll(l => l.ExtGetHeight() < labelHeightFilterInPixel);
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] removed {removedFromLinesMerged} lines predicted as label removed from lsLineMerged [height < labelHeightInPixel ({labelHeightFilterInPixel})].");
                }

                if (linesField.Count > 0)
                {
                    //var linesLeftOrder = linesField.OrderBy(l => l.BoundingBox[0]);
                    var linesLeftOrder = linesField.OrderBy(l => l.ExtGetLeft());
                    int countLinesField = linesField.Count;
                    int idxMedianLinesField = countLinesField / 2;
                    double? leftMedian = null;
                    if (linesLeftOrder != null && linesLeftOrder.Count() > 0)
                    {
                        leftMedian = linesLeftOrder.ElementAt(idxMedianLinesField).BoundingBox[0];
                    }
                    int numLinesField = linesField.Count;
                    int idxMainFields = 0;

                    // predit y in inch and expected field for each file line  
                    foreach (Line line in linesField)
                    {
                        double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                        System.Diagnostics.Debug.WriteLine($"line: {line.Text} EstimateCenterYInInch: {y}");
                    }

                    try
                    {
                        // filter lines near to lien of name field
                        Line[] mergedLinesNearToLast_Name_First_Name_Middle_Name = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Last_Name_First_Name_Middle_Name - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToLast_Name_First_Name_Middle_Name)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToLast_Name_First_Name_Middle_Name: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Last_Name_First_Name_Middle_Name)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the label
                                if (labelLast_Name_First_Name_Middle_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= labelLast_Name_First_Name_Middle_Name.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (labelLast_Name_First_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= labelLast_Name_First_Name.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (labelMiddle_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMiddle_Name.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (labelLast_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMiddle_Name.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (labelFirst_Name_Middle_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= labelFirst_Name_Middle_Name.LineMacthed.ExtGetVerticalCenter()))
                                    continue;
                                if (labelFirst_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= labelFirst_Name.LineMacthed.ExtGetVerticalCenter()))
                                    continue;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> LAST_NAME_FISRT_MIDDLE_NAME");
                                LAST_NAME_FISRT_MIDDLE_NAME = line.Text;
                                lsLineMerged.Remove(line);
                                //heightName = line.ExtGetHeight();
                                //bottomName = line.ExtGetBottom();
                                break;
                            }
                        }

                        /*
                        if (string.IsNullOrEmpty(LAST_NAME_FISRT_MIDDLE_NAME))
                        {
                            Line[] linesNearToLast_Name_First_Name_Middle_Name = linesField.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Last_Name_First_Name_Middle_Name - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in linesNearToLast_Name_First_Name_Middle_Name)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"linesNearToLast_Name_First_Name_Middle_Name: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_Last_Name_First_Name_Middle_Name)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is under the label
                                    if (labelLast_Name_First_Name_Middle_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= labelLast_Name_First_Name_Middle_Name.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (labelLast_Name_First_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= labelLast_Name_First_Name.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (labelMiddle_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMiddle_Name.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (labelLast_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= labelMiddle_Name.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (labelFirst_Name_Middle_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= labelFirst_Name_Middle_Name.LineMacthed.ExtGetVerticalCenter()))
                                        continue;
                                    if (labelFirst_Name.IsLabelFound && (line.ExtGetVerticalCenter() <= labelFirst_Name.LineMacthed.ExtGetVerticalCenter()))
                                        continue;

                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> LAST_NAME_FISRT_MIDDLE_NAME");
                                    LAST_NAME_FISRT_MIDDLE_NAME = line.Text;
                                    heightName = line.ExtGetHeight();
                                    bottomName = line.ExtGetBottom();
                                    break;
                                }
                            }
                        }
                        */
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }


                    try
                    {
                        // filter lines near to lien of nationality field
                        Line[] mergedLinesNearToNationality = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Nationality - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToNationality)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToNationality: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Nationality)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the label
                                if (labelNationality.IsLabelFound)
                                {
                                    if (line.ExtGetVerticalCenter() <= labelNationality.LineMacthed.ExtGetVerticalCenter())
                                        continue;
                                    if (!labelNationality.IsFieldInLineJustUnderTheLabel(line))
                                        continue;
                                }

                                if (labelSex.IsLabelFound)
                                {
                                    if (line.ExtGetVerticalCenter() < labelSex.LineMacthed.ExtGetVerticalCenter())
                                        continue;
                                    if (!labelSex.IsFieldInLineJustUnderTheLabel(line))
                                        continue;
                                }

                                if (labelDateOfBirth.IsLabelFound)
                                {
                                    if (line.ExtGetVerticalCenter() < labelDateOfBirth.LineMacthed.ExtGetVerticalCenter())
                                        continue;
                                    if (!labelDateOfBirth.IsFieldInLineJustUnderTheLabel(line))
                                        continue;
                                }

                                string[] fields = line.Text.Split(SEPARATOR_BLANK, StringSplitOptions.RemoveEmptyEntries);
                                bool isFieldFound = false;
                                foreach (string aField in fields)
                                {
                                    if (string.IsNullOrEmpty(NATIONALITY))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {aField} --> NATIONALITY");
                                        NATIONALITY = aField;
                                        isFieldFound = true;
                                        continue;
                                    }
                                    if (string.IsNullOrEmpty(SEX))
                                    {
                                        if (aField == "M" || aField == "F")
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {aField} --> SEX");
                                            SEX = aField;
                                            isFieldFound = true;
                                            continue;
                                        }
                                    }
                                    if (string.IsNullOrEmpty(DOB))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {aField} --> DOB");
                                        DOB = aField;
                                        isFieldFound = true;
                                        break;
                                    }
                                }
                                if(isFieldFound){
                                    lsLineMerged.Remove(line);
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(NATIONALITY))
                        {
                            Line lineNationarity = null;
                            List<Line> lsLinesNearNationality = new List<Line>();
                            Line[] linesNearToNationality = linesField.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Nationality - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            foreach (Line line in linesNearToNationality)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"linesNearToNationality: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_Nationality)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is under the label
                                    if (labelNationality.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() <= labelNationality.LineMacthed.ExtGetVerticalCenter())
                                            continue;

                                        if (string.IsNullOrEmpty(NATIONALITY))
                                        {
                                            if (line.Text == "PHL")
                                            {
                                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> NATIONALITY");
                                                NATIONALITY = line.Text;
                                                lineNationarity = line;
                                                continue;
                                            }

                                            if (labelNationality.IsFieldJustUnderTheLabel(line))
                                            {
                                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> NATIONALITY");
                                                NATIONALITY = line.Text;
                                                lineNationarity = line;
                                                continue;
                                            }
                                        }
                                    }

                                    if (lineNationarity != null)
                                    {
                                        // filter only lines on the same line of nationality
                                        if (IsLineInTheSameLine(line, lineNationarity))
                                        {
                                            lsLinesNearNationality.Add(line);
                                        }
                                        continue;
                                    }

                                    lsLinesNearNationality.Add(line);
                                }
                            }

                            if (lsLinesNearNationality.Count > 0)
                            {
                                Line[] sortedLinesNearNationality = lsLinesNearNationality.OrderBy(l => l.ExtGetLeft()).ToArray();
                                foreach (Line line in sortedLinesNearNationality)
                                {
                                    if (string.IsNullOrEmpty(NATIONALITY))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> NATIONALITY");
                                        NATIONALITY = line.Text;
                                        lineNationarity = line;
                                        continue;
                                    }
                                    if (string.IsNullOrEmpty(SEX))
                                    {
                                        if (line.Text == "M" || line.Text == "F")
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> SEX");
                                            SEX = line.Text;
                                            continue;
                                        }
                                    }
                                    if (string.IsNullOrEmpty(DOB))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> DOB");
                                        DOB = line.Text;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        Line[] mergedLinesNearToAddress1 = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address1 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress1)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress1: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address1)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the label
                                if (labelAddress.IsLabelFound && (line.ExtGetVerticalCenter() < labelAddress.LineMacthed.ExtGetVerticalCenter()))
                                    continue;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS_1");
                                lineAddress1 = line;
                                lsLineMerged.Remove(line);
                                ADDRESS_1 = lineAddress1.Text;
                                break;
                            }
                        }
                        /*
                        Line[] linesNearToAddress1 = linesField.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address1 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress1)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"linesNearToAddress1: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address1)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the label
                                if (labelAddress.IsLabelFound && (line.ExtGetVerticalCenter() <= labelAddress.LineMacthed.ExtGetVerticalCenter()))
                                    continue;

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS_1");
                                if (lineAddress1 == null)
                                {
                                    lineAddress1 = line;
                                    ADDRESS_1 = lineAddress1.Text;
                                }
                                else
                                {
                                    if (IsLineInTheSameLine(lineAddress1, line))
                                    {
                                        lineAddress1 = lineAddress1.MergedLine(line);
                                        ADDRESS_1 = lineAddress1.Text;
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {lineAddress1.Text} --> ADDRESS_1");
                                        ADDRESS_1 = lineAddress1.Text;
                                        break;
                                    }
                                }
                            }
                        }
                        */
                        Line[] mergedLinesNearToAddress2 = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address2 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress2)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearToAddress2: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address2)) <= (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the Addres1
                                if (lineAddress1 != null && (line.ExtGetVerticalCenter() <= lineAddress1.ExtGetVerticalCenter()))
                                    continue;

                                // the 2nd line of Address
                                if (IsFieldJustUnderTheLine(lineAddress1, line))
                                {
                                    // the 1st line of Address
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS_2");
                                    lineAddress2 = line;
                                    lsLineMerged.Remove(line);
                                    ADDRESS_2 = lineAddress2.Text;
                                    break;
                                }
                            }
                        }
                        /*
                        Line[] linesNearToAddress2 = linesField.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_Address2 - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearToAddress2)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"linesNearToAddress1: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_Address2)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the Addres1
                                if (lineAddress1 != null && (line.ExtGetVerticalCenter() <= lineAddress1.ExtGetVerticalCenter()))
                                    continue;

                                // the 2nd line of Address
                                if (IsFieldJustUnderTheLine(lineAddress1, line))
                                {
                                    // the 1st line of Address
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS_2");
                                    if (lineAddress2 == null)
                                    {
                                        lineAddress2 = line;
                                        ADDRESS_2 = lineAddress2.Text;
                                    }
                                    else
                                    {
                                        if (IsLineInTheSameLine(lineAddress2, line))
                                        {
                                            lineAddress2 = lineAddress2.MergedLine(line);
                                            ADDRESS_2 = lineAddress2.Text;
                                        }
                                        else
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {lineAddress2.Text} --> ADDRESS_2");
                                            ADDRESS_2 = lineAddress2.Text;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        */
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }

                    try
                    {
                        Line[] mergedLinesNearLicenseNo = lsLineMerged.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_License_No - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                        foreach (Line line in mergedLinesNearLicenseNo)
                        {
                            double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                            System.Diagnostics.Debug.WriteLine($"mergedLinesNearLicenseNo: {line.Text} EstimateCenterYInInch: {y}");
                            if (Math.Abs((decimal)(y - centerYInInchOfField_License_No)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                            {
                                // check if the line is under the label
                                if (labelLicense_No.IsLabelFound)
                                {
                                    if (line.ExtGetVerticalCenter() <= labelLicense_No.LineMacthed.ExtGetVerticalCenter())
                                        continue;
                                    if (!labelLicense_No.IsFieldInLineJustUnderTheLabel(line))
                                        continue;
                                }

                                if (labelExpiration_Date.IsLabelFound)
                                {
                                    if (line.ExtGetVerticalCenter() <= labelExpiration_Date.LineMacthed.ExtGetVerticalCenter())
                                        continue;
                                    if (!labelExpiration_Date.IsFieldInLineJustUnderTheLabel(line))
                                        continue;
                                }

                                if (labelAgency_Code.IsLabelFound)
                                {
                                    if (line.ExtGetVerticalCenter() <= labelAgency_Code.LineMacthed.ExtGetVerticalCenter())
                                        continue;
                                    if (!labelAgency_Code.IsFieldInLineJustUnderTheLabel(line))
                                        continue;
                                }

                                if (string.IsNullOrEmpty(LICENSE_NO_EXPIRY))
                                {
                                    //System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> LICENSE_NO_EXPIRY");
                                    //LICENSE_NO_EXPIRY = line.Text;
                                    string[] splited = line.Text.Split(SEPARATOR_BLANK, StringSplitOptions.RemoveEmptyEntries);
                                    if (splited != null && splited.Length > 0)
                                    {
                                        string strLicenseNo = "";
                                        string strExpiryDate = "";
                                        if (splited.Length > 1)
                                        {
                                            strLicenseNo = splited[0].Trim();
                                            strExpiryDate = splited[1].Trim().Replace(" ", "").Trim();
                                        }
                                        else
                                        {
                                            strLicenseNo = splited[0].Trim();
                                        }

                                        // check validity of license number
                                        StringBuilder sbNumInLicenseNo = new StringBuilder();
                                        foreach(char c in strLicenseNo)
                                        {
                                            if(c >= '0' && c <= '9')
                                            {
                                                sbNumInLicenseNo.Append(c); 
                                            }
                                        }
                                        if(sbNumInLicenseNo.Length > 6)
                                        {
                                            // license number seems valid, take it.
                                            LICENSE_NO_EXPIRY = line.Text;
                                            lsLineMerged.Remove(line);
                                            LICENSE_NO = strLicenseNo;
                                            EXPIRY = strExpiryDate;
                                            lsLineMerged.Remove(line);
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(LICENSE_NO))
                        {
                            Line[] linesNearLicenseNo = linesField.OrderBy(l => Math.Abs((decimal)(centerYInInchOfField_License_No - l.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel))))?.ToArray();
                            // filter lines near to license no
                            Line lineLicenseNo = null;
                            List<Line> lsLinesNearLicenseNo = new List<Line>();
                            foreach (Line line in linesNearLicenseNo)
                            {
                                double? y = line.EstimateCenterYInInch(ppi, topEdgeYOfIDImageInPixel);
                                System.Diagnostics.Debug.WriteLine($"linesNearLicenseNo: {line.Text} EstimateCenterYInInch: {y}");
                                if (Math.Abs((decimal)(y - centerYInInchOfField_License_No)) < (decimal)ACCEPTABLE_DIFF_IN_LINE)
                                {
                                    // check if the line is under the label
                                    if (labelLicense_No.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() <= labelLicense_No.LineMacthed.ExtGetVerticalCenter())
                                            continue;
                                    }

                                    if (labelExpiration_Date.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() <= labelExpiration_Date.LineMacthed.ExtGetVerticalCenter())
                                            continue;
                                    }

                                    if (labelAgency_Code.IsLabelFound)
                                    {
                                        if (line.ExtGetVerticalCenter() <= labelAgency_Code.LineMacthed.ExtGetVerticalCenter())
                                            continue;
                                    }

                                    if (string.IsNullOrEmpty(LICENSE_NO_EXPIRY))
                                    {
                                        if (labelLicense_No.IsLabelFound && labelLicense_No.IsFieldJustUnderTheLabel(line)
                                            || labelExpiration_Date.IsLabelFound && labelExpiration_Date.IsFieldUnderTheLabel(line)
                                            || labelAgency_Code.IsLabelFound && labelAgency_Code.IsFieldUnderTheLabel(line)
                                            || lineAddress1 != null && lineAddress1.ExtGetBottom() < line.ExtGetTop()
                                            )
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> LICENSE_NO_EXPIRY");
                                            LICENSE_NO_EXPIRY = line.Text;
                                            lineLicenseNo = line;
                                            lsLineMerged.Remove(line);
                                            // LICENSE_NO_EXPIRY -> documentNumber, documentExpirationDate
                                            string[] splited = LICENSE_NO_EXPIRY.Split(SEPARATOR_BLANK, 2, StringSplitOptions.RemoveEmptyEntries);
                                            if (splited != null && splited.Length > 0)
                                            {
                                                if (splited.Length > 1)
                                                {
                                                    LICENSE_NO = splited[0].Trim();
                                                    EXPIRY = splited[1].Trim().Replace(" ", "").Trim();
                                                }
                                                else
                                                {
                                                    LICENSE_NO = splited[0].Trim(); ;
                                                }
                                            }
                                            continue;
                                        }
                                    }

                                    if (lineLicenseNo != null)
                                    {
                                        // filter only lines on the same line of nationality
                                        if (IsLineInTheSameLine(line, lineLicenseNo))
                                        {
                                            lsLinesNearLicenseNo.Add(line);
                                        }
                                        continue;
                                    }

                                    lsLinesNearLicenseNo.Add(line);
                                }
                            }

                            // 
                            Line[] sortedLinesNearLicenseNo = lsLinesNearLicenseNo.OrderBy(l => l.ExtGetLeft()).ToArray();
                            foreach (Line line in sortedLinesNearLicenseNo)
                            {
                                if (string.IsNullOrEmpty(LICENSE_NO_EXPIRY))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> LICENSE_NO_EXPIRY");
                                    LICENSE_NO_EXPIRY = line.Text;
                                    lineLicenseNo = line;
                                    // LICENSE_NO_EXPIRY -> documentNumber, documentExpirationDate
                                    string[] splited = LICENSE_NO_EXPIRY.Split(SEPARATOR_BLANK, 2, StringSplitOptions.RemoveEmptyEntries);
                                    if (splited != null && splited.Length > 0)
                                    {
                                        if (splited.Length > 1)
                                        {
                                            LICENSE_NO = splited[0].Trim();
                                            EXPIRY = splited[1].Trim().Replace(" ", "").Trim();
                                        }
                                        else
                                        {
                                            LICENSE_NO = splited[0].Trim(); ;
                                        }
                                    }
                                    continue;
                                }
                                if (string.IsNullOrEmpty(LICENSE_NO))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> LICENSE_NO");
                                    LICENSE_NO = line.Text;
                                    continue;
                                }
                                if (string.IsNullOrEmpty(EXPIRY))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> EXPIRY");
                                    EXPIRY = line.Text;
                                    break;
                                }
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}");
                    }
                }
            } // ppi != null

            // map to result and convert format 
            List<string> lsMissingFields = new List<string>();

            // NAME -> lastNameOrFullName 
            if (string.IsNullOrEmpty(LAST_NAME_FISRT_MIDDLE_NAME))
            {
                lsMissingFields.Add("LAST_NAME_FISRT_MIDDLE_NAME");
            }
            else
            {
                result.lastNameOrFullName = LAST_NAME_FISRT_MIDDLE_NAME;
            }


            string[] namesSplit = LAST_NAME_FISRT_MIDDLE_NAME.Split(SEPARATOR_COMMA_DOT_BLANK, 2);
            if (namesSplit != null && namesSplit.Length > 0)
            {
                result.lastNameOrFullName = namesSplit[0];

                if (namesSplit.Length > 1)
                {
                    if (namesSplit.Length > 2)
                    {
                        result.firstName = namesSplit[1];
                        result.middleName = namesSplit[2];
                    }
                    else
                    {
                        result.firstName = namesSplit[1];
                    }
                }
            }

            // LICENSE_NO -> documentNumber
            if (string.IsNullOrEmpty(LICENSE_NO))
            {
                lsMissingFields.Add("LICENSE_NO");
            }
            else
            {
                result.documentNumber = LICENSE_NO;
            }

            // EXPIRY "yyyy/MM/dd" -> documentExpirationDate "yyyy-MM-dd"
            if (EXPIRY.Length == 10)
            {
                try
                {
                    int yyyy = int.Parse(EXPIRY.Substring(0, 4));
                    int MM = int.Parse(EXPIRY.Substring(5, 2));
                    int dd = int.Parse(EXPIRY.Substring(8, 2));
                    result.documentExpirationDate = $"{yyyy:0000}-{MM:00}-{dd:00}";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    lsMissingFields.Add("EXPIRY");
                }
            }
            else
            {
                lsMissingFields.Add("EXPIRY");
            }

            // nationality 3 letter code
            if (string.IsNullOrEmpty(NATIONALITY))
            {
                lsMissingFields.Add("NATIONALITY");
            }
            else
            {
                Code.Country country = FindCountry(NATIONALITY);
                if (country != null)
                    result.nationality = country.ncode;
                else
                    result.nationality = NATIONALITY;
            }

            if (string.IsNullOrEmpty(SEX))
            {
                lsMissingFields.Add("SEX");
            }
            else
            {
                result.gender = SEX;
            }

            // DOB "yyyy/MM/dd" -> documentExpirationDate "yyyy-MM-dd"
            try
            {
                result.dateOfBirth = "";
                if (DOB.Length == 10)
                {
                    int yyyy = int.Parse(DOB.Substring(0, 4));
                    int MM = int.Parse(DOB.Substring(5, 2));
                    int dd = int.Parse(DOB.Substring(8, 2));
                    result.dateOfBirth = $"{yyyy:0000}-{MM:00}-{dd:00}";
                }
                else
                {
                    lsMissingFields.Add("DOB");
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                lsMissingFields.Add("DOB");
            }

            // ADDRESS_1, ADDRESS_2 -> addressLine1, addressLine2
            if (string.IsNullOrEmpty(ADDRESS_1))
            {
                lsMissingFields.Add("ADDRESS_1");
            }
            else
            {
                result.addressLine1 = ADDRESS_1;
            }

            result.addressLine2 = ADDRESS_2;

            // determine success or not
            if (lsMissingFields.Count == 0)
            {
                result.Success = true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExtractFieldsFromReadResultOfPHDL result NOT success");
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
#endif
        bool IsLineAboveOrSmallerThanLabel(Line line, LabelInfo label)
        {
            if (label.IsLabelFound)
            {
                if (label.Bottom > line.ExtGetBottom())
                    return true;

                if (label.Height * 0.7 > line.ExtGetHeight())
                {
                    if (label.LineMacthed.BoundingBox.Count == 8 && line.BoundingBox.Count == 8)
                    {
                        // if the label is a polygon and line is a polygon, height is confident and can be used to filter.
                        if (label.Height * 0.7 > line.ExtGetHeight())
                        {
                            System.Diagnostics.Debug.WriteLine($"IsLineAboveOrSmallerThanLabel polygon line: {line.Text} Height * 0.7 = {label.Height * 0.7} label: {label} Height: {line.ExtGetHeight()}");
                            return true;
                        }
                    }
                    else if (label.LineMacthed.BoundingBox.Count == 8 && line.Baseline != null && line.Baseline.Count == 4)
                    {
                        // if the label is a polygon and line is a polygon, height is confident and can be used to filter.
                        if (label.Height * 0.7 > line.ExtGetHeight())
                        {
                            System.Diagnostics.Debug.WriteLine($"IsLineAboveOrSmallerThanLabel baseline line: {line.Text} Height * 0.7 = {label.Height * 0.7} label: {label} Height: {line.ExtGetHeight()}");
                            return true;
                        }
                    }
                    else
                    {
                        // filter only if the label is small enough
                        if (label.Height * 0.5 > line.ExtGetHeight())
                        {
                            System.Diagnostics.Debug.WriteLine($"IsLineAboveOrSmallerThanLabel baseline line: {line.Text} Height * 0.5 = {label.Height * 0.5} label: {label} Height: {line.ExtGetHeight()}");
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        bool IsLineBelowOrSmallerThanLabel(Line line, LabelInfo label)
        {
            if (label.IsLabelFound)
            {
                if (label.Top < line.ExtGetTop())
                    return true;

                if (label.Height * 0.7 > line.ExtGetHeight())
                {
                    if (label.LineMacthed.BoundingBox.Count == 8 && line.BoundingBox.Count == 8)
                    {
                        // if the label is a polygon and line is a polygon, height is confident and can be used to filter.
                        if (label.Height * 0.7 > line.ExtGetHeight())
                        {
                            System.Diagnostics.Debug.WriteLine($"IsLineAboveOrSmallerThanLabel polygon line: {line.Text} Height * 0.7 = {label.Height * 0.7} label: {label} Height: {line.ExtGetHeight()}");
                            return true;
                        }
                    }
                    else if (label.LineMacthed.BoundingBox.Count == 8 && line.Baseline != null && line.Baseline.Count == 4)
                    {
                        // if the label is a polygon and line is a polygon, height is confident and can be used to filter.
                        if (label.Height * 0.7 > line.ExtGetHeight())
                        {
                            System.Diagnostics.Debug.WriteLine($"IsLineAboveOrSmallerThanLabel baseline line: {line.Text} Height * 0.7 = {label.Height * 0.7} label: {label} Height: {line.ExtGetHeight()}");
                            return true;
                        }
                    }
                    else
                    {
                        // filter only if the label is small enough
                        if (label.Height * 0.5 > line.ExtGetHeight())
                        {
                            System.Diagnostics.Debug.WriteLine($"IsLineAboveOrSmallerThanLabel baseline line: {line.Text} Height * 0.5 = {label.Height * 0.5} label: {label} Height: {line.ExtGetHeight()}");
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        bool IsFieldJustUnderTheLine(Line line, Line field, double alowedVerticallDiffBetweenLineAndField = 1.0f)
        {
            if (line != null)
            {
                if (Math.Abs((double)(field.ExtGetLeft() - line.ExtGetLeft())) < line.ExtGetHeight() * 3
                    && (double)(field.ExtGetTop() - line.ExtGetBottom()) < (line.ExtGetHeight() * alowedVerticallDiffBetweenLineAndField)
                    && (double)(field.ExtGetVerticalCenter() - line.ExtGetVerticalCenter()) > 0)
                {
                    return true;
                }
            }
            return false;
        }
        bool IsFieldsUnderTheLine(Line line, Line field)
        {
            if ((double)(field.ExtGetTop() - line.ExtGetBottom()) >= 0)
            {
                return true;
            }
            return false;
        }

        bool IsFieldInSameLeftEdgeOfLine(Line line, Line field)
        {
            //if (Math.Abs((double)(field.BoundingBox[0].Value - line.ExtGetLeft())) < line.ExtGetLeft() * 3)
            if (Math.Abs((double)(field.ExtGetLeft() - line.ExtGetLeft())) < line.ExtGetLeft() * 3)
            {
                return true;
            }
            return false;
        }

#if false
        public static ScanIDeKTPResult? ExtractFieldsFromReadResultOfIDeKTP(IList<Line> linesAll)
        {
            LabelInfo labelNIK = new("NIK");
            LabelInfo labelNama = new("Nama");
            LabelInfo labelTempatTglLahir = new("Tempat/Tgl Lahir");
            LabelInfo labelJenisKelamin = new("Jenis Kelamin");
            LabelInfo labelAlamat = new("Alamat");
            LabelInfo labelRT_RW = new("RT/RW");
            LabelInfo labelKel_Desa = new("Kel/Desa");
            LabelInfo labelKecamatan = new("Kecamatan");
            LabelInfo labelAgama = new("Agama");
            LabelInfo labelStatus_Perkawinan = new("Status Perkawinan");
            LabelInfo labelPekerjaan = new("Pekerjaan");
            LabelInfo labelKewarganegaraan = new("Kewarganegaraan");
            //LabelInfo labelBerlakuHingga = new("Berlaku Hingga"); // Berlaku Hingga (expiry date) is deprecated

            ScanIDeKTPResult? result = new ScanIDeKTPResult();

            string PROVINSI = "";   // 1st line
            Confidence confidence_PROVINSI = new Confidence();
            string KAB_KOTA = "";   // 2nd line (Kabupaten (Regency) or Kota (City))
            Confidence confidence_KAB_KOTA = new Confidence();
            string NIK = "";
            Confidence confidence_NIK = new Confidence();
            string NAMA = "";
            Confidence confidence_NAMA = new Confidence();
            string TEMPAT_TGL_LAHIR = ""; //PLACE_OF_BIRTH_DOB
            Confidence confidence_TEMPAT_TGL_LAHIR = new Confidence();
            //string PLACE_OF_BIRTH = ""; //PLACE_OF_BIRTH_DOB
            //Confidence confidence_PLACE_OF_BIRTH = new Confidence();
            //string DOB = ""; //PLACE_OF_BIRTH_DOB
            //Confidence confidence_DOB = new Confidence();
            string JENIS_KELAMIN = "";   // GENDER
            Confidence confidence_JENIS_KELAMIN = new Confidence();
            Line? valueJENIS_KELAMIN = null;
            string ALAMAT = "";
            Confidence confidence_ALAMAT = new Confidence();
            string RT_RW = "";
            Confidence confidence_RT_RW = new Confidence();
            string KEL_DESA = "";
            Confidence confidence_KEL_DESA = new Confidence();
            string KECAMATAN = "";
            Confidence confidence_KECAMATAN = new Confidence();
            string AGAMA = "";
            Confidence confidence_AGAMA = new Confidence();
            string STATUS_PERKAWINAN = "";
            Confidence confidence_STATUS_PERKAWINAN = new Confidence();
            string PEKERJAAN = "";
            Confidence confidence_PEKERJAAN = new Confidence();
            string KEWARGANEGARAAN = "";    // Nationality
            Confidence confidence_KEWARGANEGARAAN = new Confidence();
            //Line? valueKEWARGANEGARAAN = null;
            //string BERLAK_HINGGA = "";  // Expiry
            //Confidence confidence_BERLAK_HINGGA = new Confidence();

            List<Line> linesField = new List<Line>();   // lines valid and not label
            List<Line> linesFieldOrLabel = new List<Line>();   // lines valid and not label

            // find labels exactly match
            foreach (Line line in linesAll)
            {
                string text = line.Text.Trim();
                double[] confidences = line.ExtGetConfidenceArray();
                Confidence confidence = new Confidence(confidences);
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] linesAll {line.Text} Height:{line.ExtGetHeight()} Min:{confidence.Min} Avg:{confidence.Avg} Max:{confidence.Max}");
                if (confidence.Avg < 0.5)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   confidence.avg:{confidence.Avg} < 0.5 --> ignored");
                    continue;
                }

                double? angle = line.ExtGetAngle();
                if (angle == null || Math.Abs((decimal)angle) > 10)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   angle:{angle} > 10 --> ignored");
                    continue;
                }

                if (!labelNIK.HasConfidence)
                {
                    if (labelNIK.MatchTitleExactly(line))
                        continue;
                }
                if (!labelNama.HasConfidence)
                {
                    if (labelNama.MatchTitleExactly(line))
                        continue;
                }
                if (!labelTempatTglLahir.HasConfidence)
                {
                    if (labelTempatTglLahir.MatchTitleWithSeparator(line, ":", out TEMPAT_TGL_LAHIR/*, mSpellSuggestion*/))
                    {
                        confidence_TEMPAT_TGL_LAHIR = confidence;
                        continue;
                    }
                    if (labelTempatTglLahir.MatchTitleWithSeparator(line, ";", out TEMPAT_TGL_LAHIR/*, mSpellSuggestion*/))
                    {
                        confidence_TEMPAT_TGL_LAHIR = confidence;
                        continue;
                    }
                    //if (labelTempatTglLahir.MatchTitleRegex(line, @"Tempat\/Tgl[ ]*Lahir"))
                    if (labelTempatTglLahir.MatchTitleRegex(line, @"Tempa.\/Tgl[ ]*Lahir"))
                    {
                        confidence_TEMPAT_TGL_LAHIR = confidence;
                        continue;
                    }
                    if (labelTempatTglLahir.MatchTitleFollowedByField(line, out TEMPAT_TGL_LAHIR/*, mSpellSuggestion*/))
                    {
                        confidence_TEMPAT_TGL_LAHIR = confidence;
                        continue;
                    }
                    if (labelTempatTglLahir.MatchTitleExactly(line))
                        continue;
                }
                if (!labelJenisKelamin.HasConfidence)
                {
                    if (labelJenisKelamin.MatchTitleRegex(line, @"Jenis[ ]*Kelamin"))
                    {
                        confidence_JENIS_KELAMIN = confidence;
                        continue;
                    }
                    if (labelJenisKelamin.MatchTitleExactly(line))
                        continue;
                }
                if (!labelAlamat.HasConfidence)
                {
                    if (labelAlamat.MatchTitleExactly(line))
                        continue;
                }
                if (!labelRT_RW.HasConfidence)
                {
                    if (labelRT_RW.MatchTitleExactly(line))
                        continue;
                }
                if (!labelKel_Desa.HasConfidence)
                {
                    if (labelKel_Desa.MatchTitleExactly(line))
                        continue;
                }
                if (!labelKecamatan.HasConfidence)
                {
                    if (labelKecamatan.MatchTitleExactly(line))
                        continue;
                }
                if (!labelAgama.HasConfidence)
                {
                    if (labelAgama.MatchTitleExactly(line))
                        continue;
                }
                if (!labelStatus_Perkawinan.HasConfidence)
                {
                    if (labelStatus_Perkawinan.MatchTitleWithSeparator(line, ":", out STATUS_PERKAWINAN/*, mSpellSuggestion*/))
                    {
                        confidence_STATUS_PERKAWINAN = confidence;
                        continue;
                    }
                    if (labelStatus_Perkawinan.MatchTitleWithSeparator(line, ";", out STATUS_PERKAWINAN/*, mSpellSuggestion*/))
                    {
                        confidence_STATUS_PERKAWINAN = confidence;
                        continue;
                    }
                    if (labelStatus_Perkawinan.MatchTitleFollowedByField(line, out STATUS_PERKAWINAN/*, mSpellSuggestion*/))
                    {
                        confidence_STATUS_PERKAWINAN = confidence;
                        continue;
                    }
                    if (labelStatus_Perkawinan.MatchTitleExactly(line))
                        continue;
                }
                if (!labelPekerjaan.HasConfidence)
                {
                    if (labelPekerjaan.MatchTitleExactly(line))
                        continue;
                }
                if (!labelKewarganegaraan.HasConfidence)
                {
                    if (labelKewarganegaraan.MatchTitleWithSeparator(line, ":", out KEWARGANEGARAAN/*, mSpellSuggestion*/))
                    {
                        confidence_KEWARGANEGARAAN = confidence;
                        continue;
                    }
                    if (labelKewarganegaraan.MatchTitleWithSeparator(line, ";", out KEWARGANEGARAAN/*, mSpellSuggestion*/))
                    {
                        confidence_KEWARGANEGARAAN = confidence;
                        continue;
                    }
                    if (labelKewarganegaraan.MatchTitleFollowedByField(line, out KEWARGANEGARAAN/*, mSpellSuggestion*/))
                    {
                        confidence_KEWARGANEGARAAN = confidence;
                        continue;
                    }
                    if (labelKewarganegaraan.MatchTitleExactly(line))
                        continue;
                }
                //if (!labelBerlakuHingga.HasConfidence)
                //{
                //    if (labelBerlakuHingga.MatchTitleExactly(line))
                //        continue;
                //}

                linesFieldOrLabel.Add(line);
            }// foreach lines in other columns

            // find labels not found yet, and fields
            foreach (Line line in linesFieldOrLabel)
            {
                string text = line.Text.Trim();
                double[] confidences = line.ExtGetConfidenceArray();
                Confidence confidence = new Confidence(confidences);
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] linesFieldOrLabel {line.Text} Height:{line.ExtGetHeight()} Min:{confidence.Min} Avg:{confidence.Avg} Max:{confidence.Max}");
                if (confidence.Avg < 0.5)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   confidence.avg:{confidence.Avg} < 0.5 --> ignored");
                    continue;
                }

                double? angle = line.ExtGetAngle();
                if (angle == null || Math.Abs((decimal)angle) > 10)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   angle:{angle} > 10 --> ignored");
                    continue;
                }

                if (!labelNIK.HasConfidence)
                {
                    if (labelNIK.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelNama.HasConfidence)
                {
                    if (labelNama.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelTempatTglLahir.HasConfidence)
                {
                    if (labelTempatTglLahir.MatchTitleWithSeparator(line, ":", out TEMPAT_TGL_LAHIR/*, mSpellSuggestion*/))
                    {
                        confidence_TEMPAT_TGL_LAHIR = confidence;
                        continue;
                    }
                    if (labelTempatTglLahir.MatchTitleWithSeparator(line, ";", out TEMPAT_TGL_LAHIR/*, mSpellSuggestion*/))
                    {
                        confidence_TEMPAT_TGL_LAHIR = confidence;
                        continue;
                    }
                    if (labelTempatTglLahir.MatchTitleFollowedByField(line, out TEMPAT_TGL_LAHIR/*, mSpellSuggestion*/))
                    {
                        confidence_TEMPAT_TGL_LAHIR = confidence;
                        continue;
                    }
                    if (labelTempatTglLahir.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelJenisKelamin.HasConfidence)
                {
                    if (labelJenisKelamin.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelAlamat.HasConfidence)
                {
                    if (labelAlamat.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelRT_RW.HasConfidence)
                {
                    if (labelRT_RW.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelKel_Desa.HasConfidence)
                {
                    if (labelKel_Desa.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelKecamatan.HasConfidence)
                {
                    if (labelKecamatan.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelAgama.HasConfidence)
                {
                    if (labelAgama.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelStatus_Perkawinan.HasConfidence)
                {
                    if (labelStatus_Perkawinan.MatchTitleWithSeparator(line, ":", out STATUS_PERKAWINAN/*, mSpellSuggestion*/))
                    {
                        confidence_STATUS_PERKAWINAN = confidence;
                        continue;
                    }
                    if (labelStatus_Perkawinan.MatchTitleWithSeparator(line, ";", out STATUS_PERKAWINAN/*, mSpellSuggestion*/))
                    {
                        confidence_STATUS_PERKAWINAN = confidence;
                        continue;
                    }
                    if (labelStatus_Perkawinan.MatchTitleFollowedByField(line, out STATUS_PERKAWINAN/*, mSpellSuggestion*/))
                    {
                        confidence_STATUS_PERKAWINAN = confidence;
                        continue;
                    }
                    if (labelStatus_Perkawinan.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelPekerjaan.HasConfidence)
                {
                    if (labelPekerjaan.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelKewarganegaraan.HasConfidence)
                {
                    if (labelKewarganegaraan.MatchTitleWithSeparator(line, ":", out KEWARGANEGARAAN/*, mSpellSuggestion*/))
                    {
                        confidence_KEWARGANEGARAAN = confidence;
                        continue;
                    }
                    if (labelKewarganegaraan.MatchTitleWithSeparator(line, ";", out KEWARGANEGARAAN/*, mSpellSuggestion*/))
                    {
                        confidence_KEWARGANEGARAAN = confidence;
                        continue;
                    }
                    if (labelKewarganegaraan.MatchTitleFollowedByField(line, out KEWARGANEGARAAN/*, mSpellSuggestion*/))
                    {
                        confidence_KEWARGANEGARAAN = confidence;
                        continue;
                    }
                    if (labelKewarganegaraan.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                //if (!labelBerlakuHingga.HasConfidence)
                //{
                //    if (labelBerlakuHingga.MatchTitle(line/*, mSpellSuggestion*/))
                //        continue;
                //}

                linesField.Add(line);
            }// foreach lines in other columns

            // sort from top to bottom
            var linesLeftOrder = linesField.OrderBy(l => l.BoundingBox[0]);

            //int countLinesField = linesField.Count;
            //int idxMedianLinesField = countLinesField / 2;
            //double? leftMedian = linesLeftOrder.ElementAt(idxMedianLinesField).BoundingBox[0];

            //double? leftEdgeOfBlock = linesField.Min(l => l.BoundingBox[0]);
            //double? rightEdgeOfBlock = linesField.Max(l => l.BoundingBox[2]);
            //double? topEdgeOfBlock = linesField.Min(l => l.BoundingBox[1]);
            //double? bottomEdgeOfBlock = linesField.Max(l => l.BoundingBox[5]);
            //double? sumLeft = linesLeftOrder.Take(5).Sum(l => l.BoundingBox[0]);
            //double? avgLeft = sumLeft / 5;
            //double? acceptableRangeOfLeftEdge = (rightEdgeOfBlock - leftEdgeOfBlock) / 20;
            //double? h_center = leftEdgeOfBlock + (rightEdgeOfBlock - leftEdgeOfBlock) / 2;
            //double? v_center = topEdgeOfBlock + (bottomEdgeOfBlock - topEdgeOfBlock) / 2;
            //double? h_leftSideEdge = leftEdgeOfBlock + (rightEdgeOfBlock - leftEdgeOfBlock) / 3;

            double? heightName = null;
            double? bottomName = null;
            int numLinesField = linesField.Count;
            int idxMainColumn = 0;

            foreach (Line line in linesField)
            {
                string text = line.Text.Trim();

                double[] confidences = line.ExtGetConfidenceArray();
                Confidence confidence = new Confidence(confidences);
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} Height:{line.ExtGetHeight()} Min:{confidence.Min} Avg:{confidence.Avg} Max:{confidence.Max}");

                if (heightName.HasValue)
                {
                    if (line.ExtGetHeight() < heightName * 0.65)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   Height:{line.ExtGetHeight()} < heightName:{heightName} * 0.65 = {heightName * 0.65} --> ignored");
                        //numLinesInMainColumn--;
                        numLinesField--;
                        continue;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] idxMainColumn:{idxMainColumn} numLinesField:{numLinesField}");

                // 
                // https://www.lingonomad.com/blogs/indonesia/administrative-divisions#:~:text=The%205%20Administrative%20Divisions%20of%20Indonesia%201%201.,5.%20Sub-district%2C%20known%20as%20%E2%80%9CKelurahan%E2%80%9D%20in%20Indonesian%20
                // https://en.wikipedia.org/wiki/List_of_regencies_and_cities_of_Indonesia
                if (string.IsNullOrEmpty(PROVINSI))
                {
                    // 1st line is PROVINSI
                    if (line.Text.Length > 4 && line.Text.Substring(0, 4) == "PROV")
                    {
                        PROVINSI = line.Text.Trim();
                        confidence_PROVINSI = confidence;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {PROVINSI} --> PROVINSI");
                    }
                    continue;
                }

                if (string.IsNullOrEmpty(KAB_KOTA))
                {
                    // 2nd line is KAB_KOTA
                    if (line.Text.Length > 3 && line.Text.Substring(0, 3) == "KAB" || line.Text.Substring(0, 3) == "KOT")
                    {
                        KAB_KOTA = line.Text.Trim();
                        confidence_KAB_KOTA = confidence;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {KAB_KOTA} --> KAB_KOTA");
                    }
                    continue;
                }

                if (string.IsNullOrEmpty(NIK))
                {
                    // NIK (IDNUM)
                    if (!labelNIK.HasConfidence
                        || labelNIK.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        NIK = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {NIK} --> NIK");
                        confidence_NIK = confidence;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(NAMA))
                {
                    // Name
                    if (!labelNama.HasConfidence
                        || labelNama.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        NAMA = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {NAMA} --> NAMA");
                        confidence_NAMA = confidence;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(TEMPAT_TGL_LAHIR))
                {
                    // Place Of Birth, DOB
                    if (!labelTempatTglLahir.HasConfidence
                        || labelTempatTglLahir.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        TEMPAT_TGL_LAHIR = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {TEMPAT_TGL_LAHIR} --> TEMPAT_TGL_LAHIR");
                        confidence_TEMPAT_TGL_LAHIR = confidence;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(JENIS_KELAMIN))
                {
                    // Gender
                    if (!labelJenisKelamin.HasConfidence
                        || labelJenisKelamin.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        JENIS_KELAMIN = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {JENIS_KELAMIN} --> JENIS_KELAMIN");
                        confidence_JENIS_KELAMIN = confidence;
                        idxMainColumn++;
                        valueJENIS_KELAMIN = line;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(ALAMAT))
                {
                    // Alamat  (Addr line 1)
                    if (!labelAlamat.HasConfidence
                        || labelAlamat.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        ALAMAT = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {ALAMAT} --> ALAMAT");
                        confidence_ALAMAT = confidence;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(RT_RW))
                {
                    // RT_RW (Addr line 2)
                    if (!labelRT_RW.HasConfidence
                        || labelRT_RW.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        RT_RW = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {RT_RW} --> RT_RW");
                        confidence_RT_RW = confidence;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(KEL_DESA))
                {
                    // KEL_DESA (Addr line 3)
                    if (!labelKel_Desa.HasConfidence
                        || labelKel_Desa.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        KEL_DESA = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {KEL_DESA} --> KEL_DESA");
                        confidence_KEL_DESA = confidence;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(KECAMATAN))
                {
                    // KECAMATAN (Addr line 4)
                    if (!labelKecamatan.HasConfidence
                        || labelKecamatan.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        KECAMATAN = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {KECAMATAN} --> KECAMATAN");
                        confidence_KECAMATAN = confidence;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(AGAMA))
                {
                    // AGAMA (Religion)
                    if (!labelAgama.HasConfidence
                        || labelAgama.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        AGAMA = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {AGAMA} --> AGAMA");
                        confidence_AGAMA = confidence;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(STATUS_PERKAWINAN))
                {
                    // STATUS_PERKAWINAN (Marital Status)
                    if (!labelStatus_Perkawinan.HasConfidence
                        || labelStatus_Perkawinan.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        STATUS_PERKAWINAN = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {STATUS_PERKAWINAN} --> STATUS_PERKAWINAN");
                        confidence_STATUS_PERKAWINAN = confidence;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(PEKERJAAN))
                {
                    // PEKERJAAN (Job Status)
                    if (!labelPekerjaan.HasConfidence
                        || labelPekerjaan.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        PEKERJAAN = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {PEKERJAAN} --> PEKERJAAN");
                        confidence_PEKERJAAN = confidence;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(KEWARGANEGARAAN))
                {
                    // KEWARGANEGARAAN (Nationality)
                    if (!labelKewarganegaraan.HasConfidence
                        || labelKewarganegaraan.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        KEWARGANEGARAAN = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {KEWARGANEGARAAN} --> KEWARGANEGARAAN");
                        confidence_KEWARGANEGARAAN = confidence;
                        idxMainColumn++;
                        continue;
                    }
                }
                /*
                if (string.IsNullOrEmpty(BERLAK_HINGGA))
                {
                    // BERLAK_HINGGA (Expiry)
                    if (!labelBerlakuHingga.HasConfidence
                        || labelBerlakuHingga.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        BERLAK_HINGGA = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {BERLAK_HINGGA} --> BERLAK_HINGGA");
                        confidence_BERLAK_HINGGA = confidence;
                        idxMainColumn++;
                        continue;
                    }
                }
                */
                // Unknown
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> UNKNOWN");
                idxMainColumn++;
            }// foreach lines in main column

            // map to result and convert format 
            List<string> lsMissingFields = new List<string>();

            // PROVINSI
            if (string.IsNullOrEmpty(PROVINSI))
            {
                lsMissingFields.Add("PROVINSI");
            }
            else
            {
                result.provinsi = PROVINSI;
                result.provinsiConfidence = confidence_PROVINSI;
            }

            // KAB/LPTA
            if (string.IsNullOrEmpty(KAB_KOTA))
            {
                lsMissingFields.Add("KAB_KOTA");
            }
            else
            {
                result.kabKota = KAB_KOTA;
                result.kabKotaConfidence = confidence_KAB_KOTA;
            }

            // NAME -> lastNameOrFullName 
            if (string.IsNullOrEmpty(NAMA))
            {
                lsMissingFields.Add("NAMA");
            }
            else
            {
                result.lastNameOrFullName = NAMA;
                result.lastNameOrFullNameConfidence = confidence_NAMA;
            }

            // NIK -> documentNumber
            if (string.IsNullOrEmpty(NIK))
            {
                lsMissingFields.Add("NIK");
            }
            else
            {
                result.documentNumber = NIK;
                result.documentNumberConfidence = confidence_NIK;
            }

            // TEMPAT_TGL_LAHIR -> Place of birth, Date Of Birth
            if (TEMPAT_TGL_LAHIR.Length >= 10)
            {
                try
                {
                    // dd-MM-yyyy
                    //Regex regexDDMMYYYY = new Regex(@"\d{2}-\d{2}-\d{4}");
                    Regex regexDDMMYYYY = new Regex(@"\d{2}[ -]\d{2}[ -]\d{4}");
                    MatchCollection matches = regexDDMMYYYY.Matches(TEMPAT_TGL_LAHIR);
                    if (matches.Count >= 0)
                    {
                        string yyyyMMdd = matches[0].Value;
                        if (yyyyMMdd.Length == 10)
                        {
                            int dd = int.Parse(yyyyMMdd.Substring(0, 2));
                            int MM = int.Parse(yyyyMMdd.Substring(3, 2));
                            int yyyy = int.Parse(yyyyMMdd.Substring(6, 4));
                            result.dateOfBirth = $"{yyyy:0000}-{MM:00}-{dd:00}";
                            result.dateOfBirthConfidence = confidence_TEMPAT_TGL_LAHIR;

                            int posDoB = TEMPAT_TGL_LAHIR.IndexOf(yyyyMMdd);
                            if (posDoB > 0)
                            {
                                // extract place of birth
                                result.placeOfBirth = TEMPAT_TGL_LAHIR.Substring(0, posDoB).Trim().Trim(',');
                                result.placeOfBirthConfidence = confidence_TEMPAT_TGL_LAHIR;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
            if (string.IsNullOrEmpty(result.dateOfBirth))
            {
                lsMissingFields.Add("dateOfBirth");
            }
            if (string.IsNullOrEmpty(result.placeOfBirth))
            {
                lsMissingFields.Add("placeOfBirth");
            }

            // Gender
            if (valueJENIS_KELAMIN != null)
            {
                if (CheckCharInLine(valueJENIS_KELAMIN, new Regex("LAKI[-| ]*LAKI")))
                {
                    result.gender = "M";
                    result.genderConfidence = confidence_JENIS_KELAMIN;
                }
                else if (CheckCharInLine(valueJENIS_KELAMIN, "PEREMPUAN"))
                {
                    result.gender = "F";
                    result.genderConfidence = confidence_JENIS_KELAMIN;
                }
                else
                {
                    // unknown...
                    result.gender = valueJENIS_KELAMIN.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                    result.genderConfidence = confidence_JENIS_KELAMIN;
                    lsMissingFields.Add("gender");
                }
            }
            else
            {
                lsMissingFields.Add("gender");
            }

            // Address
            if (string.IsNullOrEmpty(ALAMAT))
            {
                lsMissingFields.Add("ALAMAT");
            }
            else
            {
                result.addressLine1 = ALAMAT;
                result.addressLine1Confidence = confidence_ALAMAT;
                result.alamat = ALAMAT;
                result.alamatConfidence = confidence_ALAMAT;
            }

            //result.addressLine2 = $"{RT_RW} {KEL_DESA} {KECAMATAN}";
            //result.addressLine2Confidence = confidence_RT_RW + confidence_KEL_DESA + confidence_KECAMATAN;
            if (string.IsNullOrEmpty(RT_RW))
            {
                lsMissingFields.Add("RT_RW");
            }
            else
            {
                string[] arrRtRw = RT_RW.Split('/');
                if (arrRtRw.Length > 0)
                {
                    result.rt = arrRtRw[0].Trim();
                    result.rtConfidence = confidence_RT_RW;
                }
                if (arrRtRw.Length > 1)
                {
                    result.rw = arrRtRw[1].Trim();
                    result.rwConfidence = confidence_RT_RW;
                }
            }
            if (string.IsNullOrEmpty(KEL_DESA))
            {
                lsMissingFields.Add("KEL_DESA");
            }
            result.addressLine2 = $"{RT_RW} {KEL_DESA}";
            result.addressLine2Confidence = confidence_RT_RW + confidence_KEL_DESA;
            result.kelDesa = KEL_DESA;
            result.kelDesaConfidence = confidence_KEL_DESA;

            if (string.IsNullOrEmpty(KECAMATAN))
            {
                lsMissingFields.Add("KECAMATAN");
            }
            else
            {
                result.addressTown = $"{KECAMATAN}";
                result.addressTownConfidence = confidence_KECAMATAN;
                result.kecamatan = KECAMATAN;
                result.kecamatanConfidence = confidence_KECAMATAN;
            }

            // Marital Status
            /*
            Civil Status:		
            S	Single	
            M	Married	
            X	Separated	
            W	Widow/er

            1. Belum Kawin = SINGLE --> S (Single)	
            2. Kawin = MARRIED --> M (Married)
            3. Cerai Hidup = DIVORCED --> X	(Separated)
            4. Cerai Mati = WIDOWED --> W (Widow/er)
            */
            if (!string.IsNullOrWhiteSpace(STATUS_PERKAWINAN))
            {
                if (CheckCharInLine(STATUS_PERKAWINAN, confidence_STATUS_PERKAWINAN, "BELUM KAWIN")) // Single
                {
                    result.maritalStatus = "S";
                    result.maritalStatusConfidence = confidence_STATUS_PERKAWINAN;
                }
                else if (CheckCharInLine(STATUS_PERKAWINAN, confidence_STATUS_PERKAWINAN, "KAWIN"))   // Married
                {
                    result.maritalStatus = "M";
                    result.maritalStatusConfidence = confidence_STATUS_PERKAWINAN;
                }
                else if (CheckCharInLine(STATUS_PERKAWINAN, confidence_STATUS_PERKAWINAN, "CERAI HIDUP"))   // Separated
                {
                    result.maritalStatus = "X";
                    result.maritalStatusConfidence = confidence_STATUS_PERKAWINAN;
                }
                else if (CheckCharInLine(STATUS_PERKAWINAN, confidence_STATUS_PERKAWINAN, "CERAI MATI"))   // Widow/er
                {
                    result.maritalStatus = "W";
                    result.maritalStatusConfidence = confidence_STATUS_PERKAWINAN;
                }
                else
                {
                    // unknown...
                    result.maritalStatus = STATUS_PERKAWINAN;
                    result.maritalStatusConfidence = confidence_STATUS_PERKAWINAN;
                    lsMissingFields.Add("STATUS_PERKAWINAN");
                }
            }
            else
            {
                lsMissingFields.Add("STATUS_PERKAWINAN");
            }

            // nationality 3 letter code
            result.nationality = KEWARGANEGARAAN;
            result.nationalityConfidence = confidence_KEWARGANEGARAAN;
            if (!string.IsNullOrEmpty(KEWARGANEGARAAN))
            {
                if (CheckCharInLine(KEWARGANEGARAAN, confidence_KEWARGANEGARAAN, "WNI"))
                {
                    result.nationality = "IDN";
                }
            }
            if (string.IsNullOrEmpty(result.nationality))
            {
                lsMissingFields.Add("STATUS_PERKAWINAN");
            }

            /*
            // BERLAK_HINGGA "dd-MM-yyyy" -> documentExpirationDate "yyyy-MM-dd"
            if (BERLAK_HINGGA.Length >= 10)
            {
                try
                {
                    // dd-MM-yyyy
                    Regex regexDDMMYYYY = new Regex(@"\d{2}-\d{2}-\d{4}");
                    MatchCollection matches = regexDDMMYYYY.Matches(BERLAK_HINGGA);
                    if (matches.Count >= 0)
                    {
                        string yyyyMMdd = matches[0].Value;
                        if (yyyyMMdd.Length == 10)
                        {
                            int dd = int.Parse(yyyyMMdd.Substring(0, 2));
                            int MM = int.Parse(yyyyMMdd.Substring(3, 2));
                            int yyyy = int.Parse(yyyyMMdd.Substring(6, 4));
                            result.documentExpirationDate = $"{yyyy:0000}-{MM:00}-{dd:00}";
                            result.documentExpirationDateConfidence = confidence_BERLAK_HINGGA;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
            if (string.IsNullOrEmpty(result.documentExpirationDate))
            {
                lsMissingFields.Add("BERLAK_HINGGA");
            }
            */

            // determine success or not
            if (result.confidences.Worst > 0 && result.confidences.Avg > 0.7 && lsMissingFields.Count == 0)
            {
                result.Success = true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExtractFieldsFromReadResultOfIDeKTP result NOT success");
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
#endif
#if false
        public static ScanIDDLResult? ExtractFieldsFromReadResultOfIDDL(IList<Line> linesAll)
        {
            /* https://en.wikipedia.org/wiki/Driving_license_in_Indonesia
            1. [NAME]                                                                                    
            2. PLACE, DATE OF BIRTH:[DOB in dd-mm-yyyy]
            3. (BLOOD TYPE (A/B/AB/O) - [Sex: Pria=Male, Wanita=Female]
            4. (ADDRESS)
            5. OCCUPATION: [occupation in Indonesia]
            6. PROVINCE OF REGISTRATION 
            */
            LabelInfo labelINDONESIA = new("INDONESIA");
            LabelInfo labelSURAT_IZIN_MENGEMUDI = new("SURAT IZIN MENGEMUDI");
            LabelInfo labelDRIVING_LICENSE = new("DRIVING LICENSE");

            ScanIDDLResult? result = new ScanIDDLResult();

            Regex regexIDNum = new Regex(@"\d{4}-\d{4}-\d{6}");
            int idxOfItem = -1;

            string IDNUM = "";
            Confidence confidence_IDNUM = new Confidence();
            string L1_NAME = "";
            Confidence confidence_L1_NAME = new Confidence();
            string L2_PLACE_DATE_OF_BIRTH = ""; // XXXX, DD-MM-YYYY
            Confidence confidence_L2_PLACE_DATE_OF_BIRTH = new Confidence();
            string L3_BLOODTYPE_SEX = "";    // A/B/AB/O - PRIA/WANITA
            Confidence confidence_L3_BLOODTYPE_SEX = new Confidence();
            string L4_ADDRESS1 = "";
            Confidence confidence_L4_ADDRESS1 = new Confidence();
            string L4_ADDRESS2 = "";
            Confidence confidence_L4_ADDRESS2 = new Confidence();
            string L4_ADDRESS3 = "";
            Confidence confidence_L4_ADDRESS3 = new Confidence();
            string L5_OCCUPATION = "";
            Confidence confidence_L5_OCCUPATION = new Confidence();
            string L6_PROVINCE_OF_REGISTRATION = "";
            Confidence confidence_L6_PROVINCE_OF_REGISTRATION = new Confidence();

            List<Line> linesField = new List<Line>();   // lines valid and not label
            List<Line> linesFieldOrLabel = new List<Line>();   // lines valid and not label

            // find labels exactly match
            foreach (Line line in linesAll)
            {
                string text = line.Text.Trim();
                double[] confidences = line.ExtGetConfidenceArray();
                Confidence confidence = new Confidence(confidences);
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] linesAll {line.Text} Height:{line.ExtGetHeight()} Min:{confidence.Min} Avg:{confidence.Avg} Max:{confidence.Max}");
                if (confidence.Avg < 0.5)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   confidence.avg:{confidence.Avg} < 0.5 --> ignored");
                    continue;
                }

                double? angle = line.ExtGetAngle();
                if (angle == null || Math.Abs((decimal)angle) > 10)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   angle:{angle} > 10 --> ignored");
                    continue;
                }

                if (!labelINDONESIA.HasConfidence)
                {
                    if (labelINDONESIA.MatchTitleExactly(line))
                        continue;
                }
                if (!labelSURAT_IZIN_MENGEMUDI.HasConfidence)
                {
                    if (labelSURAT_IZIN_MENGEMUDI.MatchTitleExactly(line))
                        continue;
                }
                if (!labelDRIVING_LICENSE.HasConfidence)
                {
                    if (labelDRIVING_LICENSE.MatchTitleExactly(line))
                        continue;
                }

                try
                {
                    Match matchIDNUM = regexIDNum.Match(text);
                    if (matchIDNUM.Success)
                    {
                        IDNUM = matchIDNUM.Value;
                        idxOfItem = 0;
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }

                if (idxOfItem > -1)
                {
                    string strLineNumAndBlank = "";
                    string strLineFollowingLineNum = "";
                    //string strRexEx = @"^\d{1}[.,]?\D";
                    string strRexEx = $"^{idxOfItem + 1}[.,]?\\s?";
                    if (MatchRegExInLine(line, strRexEx, out strLineNumAndBlank))
                    {
                        strLineFollowingLineNum = line.Text.Substring(strLineNumAndBlank.Length).Trim();
                        idxOfItem++;
                        switch (idxOfItem)
                        {
                            //1. [NAME]                                                                                    
                            case 1:
                                L1_NAME = strLineFollowingLineNum;
                                confidence_L1_NAME = confidence;
                                break;
                            //2. PLACE, DATE OF BIRTH:[DOB in dd-mm-yyyy]
                            case 2:
                                L2_PLACE_DATE_OF_BIRTH = strLineFollowingLineNum;
                                confidence_L2_PLACE_DATE_OF_BIRTH = confidence;
                                break;
                            //3. (BLOOD TYPE (A/B/AB/O) - [Sex: Pria=Male, Wanita=Female]
                            case 3:
                                L3_BLOODTYPE_SEX = strLineFollowingLineNum;
                                confidence_L3_BLOODTYPE_SEX = confidence;
                                break;
                            //4. (ADDRESS)
                            case 4:
                                L4_ADDRESS1 = strLineFollowingLineNum;
                                confidence_L4_ADDRESS1 = confidence;
                                break;
                            //5. OCCUPATION: [occupation in Indonesia]
                            case 5:
                                L5_OCCUPATION = strLineFollowingLineNum;
                                confidence_L5_OCCUPATION = confidence;
                                break;
                            //6. PROVINCE OF REGISTRATION 
                            case 6:
                                L6_PROVINCE_OF_REGISTRATION = strLineFollowingLineNum;
                                confidence_L6_PROVINCE_OF_REGISTRATION = confidence;
                                break;
                            default:
                                System.Diagnostics.Debug.WriteLine($"Unexpected idxOfItem:{idxOfItem} line.Text:{line.Text}");
                                break;
                        }
                    }
                    else
                    {
                        if (idxOfItem == 4)
                        {
                            if (string.IsNullOrEmpty(L4_ADDRESS2))
                            {
                                L4_ADDRESS2 = line.Text.Trim();
                                confidence_L4_ADDRESS2 = confidence;
                            }
                            else if (string.IsNullOrEmpty(L4_ADDRESS3))
                            {
                                L4_ADDRESS3 = line.Text.Trim();
                                confidence_L4_ADDRESS3 = confidence;
                            }
                        }
                    }
                }

                linesFieldOrLabel.Add(line);
            }// foreach lines in other columns

            // map to result and convert format 
            List<string> lsMissingFields = new List<string>();

            // NAME -> lastNameOrFullName 
            if (string.IsNullOrEmpty(L1_NAME))
            {
                lsMissingFields.Add("NAME");
            }
            result.lastNameOrFullName = L1_NAME;
            result.lastNameOrFullNameConfidence = confidence_L1_NAME;

            // IDNUM -> documentNumber
            if (string.IsNullOrEmpty(IDNUM))
            {
                lsMissingFields.Add("IDNUM");
            }
            result.documentNumber = IDNUM;
            result.documentNumberConfidence = confidence_IDNUM;

            // Place of birth, Date Of Birth
            if (L2_PLACE_DATE_OF_BIRTH.Length >= 10)
            {
                try
                {
                    // dd-MM-yyyy
                    Regex regexDDMMYYYY = new Regex(@"\d{2}-\d{2}-\d{4}");
                    Match matche = regexDDMMYYYY.Match(L2_PLACE_DATE_OF_BIRTH);
                    if (matche.Success)
                    {
                        string yyyyMMdd = matche.Value;
                        if (yyyyMMdd.Length == 10)
                        {
                            int dd = int.Parse(yyyyMMdd.Substring(0, 2));
                            int MM = int.Parse(yyyyMMdd.Substring(3, 2));
                            int yyyy = int.Parse(yyyyMMdd.Substring(6, 4));
                            result.dateOfBirth = $"{yyyy:0000}-{MM:00}-{dd:00}";
                            result.dateOfBirthConfidence = confidence_L2_PLACE_DATE_OF_BIRTH;

                            int posDoB = L2_PLACE_DATE_OF_BIRTH.IndexOf(yyyyMMdd);
                            if (posDoB > 0)
                            {
                                // extract place of birth
                                result.placeOfBirth = L2_PLACE_DATE_OF_BIRTH.Substring(0, posDoB).Trim().Trim(',').Trim('.');
                                result.placeOfBirthConfidence = confidence_L2_PLACE_DATE_OF_BIRTH;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
            if (string.IsNullOrEmpty(result.dateOfBirth))
            {
                lsMissingFields.Add("dateOfBirth");
            }
            if (string.IsNullOrEmpty(result.placeOfBirth))
            {
                lsMissingFields.Add("placeOfBirth");
            }

            // Gender
            if (L3_BLOODTYPE_SEX != null)
            {
                // dd-MM-yyyy
                Regex regexBloodType = new Regex("^(A|B|AB|O)");
                Match matche = regexBloodType.Match(L3_BLOODTYPE_SEX);
                if (matche.Success)
                {
                    string bloodType = matche.Value;
                    string strSex = L3_BLOODTYPE_SEX.Substring(bloodType.Length).Trim().Trim(',').Trim('.').Trim('-').Trim();
                    if (CheckCharInLine(strSex, confidence_L3_BLOODTYPE_SEX, "PRIA"/*, mSpellSuggestion*/))
                    {
                        result.gender = "M";
                        result.genderConfidence = confidence_L3_BLOODTYPE_SEX;
                    }
                    else if (CheckCharInLine(L3_BLOODTYPE_SEX, confidence_L3_BLOODTYPE_SEX, "WANITA"/*, mSpellSuggestion*/))
                    {
                        result.gender = "F";
                        result.genderConfidence = confidence_L3_BLOODTYPE_SEX;
                    }
                    else
                    {
                        // unknown...
                        int posSex = L3_BLOODTYPE_SEX.IndexOf(bloodType);
                        if (posSex > 0)
                        {
                            result.gender = L3_BLOODTYPE_SEX.Substring(0, posSex).Trim().Trim(',').Trim('.').Trim('-').Trim();
                            result.genderConfidence = confidence_L3_BLOODTYPE_SEX;
                            lsMissingFields.Add("gender");
                        }
                    }
                }
            }
            if (string.IsNullOrEmpty(result.gender))
            {
                lsMissingFields.Add("gender");
            }

            // Address
            result.addressLine1 = L4_ADDRESS1;
            result.addressLine1Confidence = confidence_L4_ADDRESS1;
            if (string.IsNullOrEmpty(L4_ADDRESS1))
            {
                lsMissingFields.Add("ADDRESS");
            }

            result.addressLine2 = $"{L4_ADDRESS2} {L4_ADDRESS3}";
            result.addressLine2Confidence = confidence_L4_ADDRESS2;
            if (confidence_L4_ADDRESS3.Avg > 0)
                result.addressLine2Confidence += confidence_L4_ADDRESS3;

            // determine success or not
            if (result.confidences.Worst > 0 && result.confidences.Avg > 0.7 && lsMissingFields.Count == 0)
            {
                result.Success = true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExtractFieldsFromReadResultOfIDDL result NOT success");
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
#endif
#if false
        public static ScanPassportMRZResult? ExtractFieldsFromReadResultOfPassportMRZ(IList<Line> linesAll)
        {
            ScanPassportMRZResult? result = new ScanPassportMRZResult();

            string ISSUING_COUNTRY = "";
            Confidence confidence_ISSUING_COUNTRY = new Confidence();
            string FULL_NAME = "";
            Confidence confidence_FULL_NAME = new Confidence();
            string SURNAME = "";
            Confidence confidence_SURNAME = new Confidence();
            string GIVEN_NAME = "";
            Confidence confidence_GIVEN_NAME = new Confidence();
            string PASSPORT_NUMBER = "";
            Confidence confidence_PASSPORT_NUMBER = new Confidence();
            string NATIONALITY = "";
            Confidence confidence_NATIONALITY = new Confidence();
            string DOB = "";
            Confidence confidence_DOB = new Confidence();
            string SEX = "";
            Confidence confidence_SEX = new Confidence();
            string EXPIRY = "";
            Confidence confidence_EXPIRY = new Confidence();
            string PERSONAL_NUMBER = "";
            Confidence confidence_PERSONAL_NUMBER = new Confidence();

            string MRZ1 = "";
            Confidence confidence_MRZ1 = new Confidence();
            string MRZ2 = "";
            Confidence confidence_MRZ2 = new Confidence();

            // find MRZ which start with 'P>'
            //https://en.wikipedia.org/wiki/Machine-readable_passport

            foreach (Line line in linesAll)
            {
                string strLine = line.Text.Trim().Replace(" ", ""); // remove all blank space
                double[] confidences = line.ExtGetConfidenceArray();
                Confidence confidence = new Confidence(confidences);

                if (string.IsNullOrEmpty(MRZ1))
                {
                    /*
                    P<JPNTATEISHI << TAKUMI <<<<<<<<<<<<<<<<<<<<<<<
                    TZ11450519JPN7104181M2608042 <<<<<<<<<<<<<< 02
                    (blank space in MRZ line should be removed)
                    P<JPNTATEISHI<<TAKUMI<<<<<<<<<<<<<<<<<<<<<<<
                    TZ11450519JPN7104181M2608042<<<<<<<<<<<<<<02
                    */
                    string strLastOrFullName = "";
                    string strFirstName = "";
                    string strLastName = "";
                    if (strLine.StartsWith("P") && strLine.Length == 44)
                    {
                        MRZ1 = strLine;
                        confidence_MRZ1 = confidence;
                        System.Diagnostics.Debug.WriteLine($"MRZ1: {MRZ1}");
                        /*
                        1       1   alpha P, indicating a passport
                        2       1   alpha +< Type(for countries that distinguish between different types of passports)
                        3–5     3   alpha Issuing country or organization(ISO 3166 - 1 alpha - 3 code with modifications)
                        6–44    39  alpha +< Surname, followed by two filler characters, 
                                    followed by given names. Given names are separated by single filler characters. 
                                    Some countries do not differentiate between surname and given name(i.e.no two filler characters), such as the Malaysian Passport
                        */
                        // 3-5 (3) Issuing country or organization(ISO 3166 - 1 alpha - 3 code with modifications)
                        ISSUING_COUNTRY = strLine.Substring(2, 3);
                        confidence_ISSUING_COUNTRY = confidence;

                        // 6–44 (39) Surname, followed by two filler characters, 
                        //              followed by given names. Given names are separated by single filler characters. 
                        //              Some countries do not differentiate between surname and given name(i.e.no two filler characters), such as the Malaysian Passport
                        string blockName = strLine.Substring(5);
                        string[] names = blockName.Split("<<");
                        List<string> lsName = new List<string>();
                        foreach (string name in names)
                        {
                            if (!string.IsNullOrEmpty(name) && name != "<")
                            {
                                lsName.Add(name);
                            }
                        }
                        if (lsName.Count > 0)
                        {
                            if (lsName.Count == 1)
                            {
                                FULL_NAME = lsName[0].Replace("<", " ");
                                confidence_FULL_NAME = confidence;
                                System.Diagnostics.Debug.WriteLine($"FULL_NAME: {FULL_NAME}");
                            }
                            else if (lsName.Count == 2)
                            {
                                SURNAME = lsName[0].Replace("<", " ");
                                confidence_SURNAME = confidence;
                                GIVEN_NAME = lsName[1].Replace("<", " ");
                                confidence_GIVEN_NAME = confidence;
                                System.Diagnostics.Debug.WriteLine($"SURNAME: {SURNAME}");
                                System.Diagnostics.Debug.WriteLine($"GIVEN_NAME: {GIVEN_NAME}");
                            }
                            else
                            {
                                FULL_NAME = blockName.Replace("<", " ").Trim();
                                System.Diagnostics.Debug.WriteLine($"FULL_NAME: {FULL_NAME}");
                            }
                        }
                        System.Diagnostics.Debug.WriteLine("----------");
                    }
                }
                else
                {
                    MRZ2 = strLine;
                    confidence_MRZ2 = confidence;
                    System.Diagnostics.Debug.WriteLine($"MRZ2: {MRZ2}");
                    /*
                    1–9	9	alpha+num+<	Passport number
                    10	1	numeric	Check digit over digits 1–9
                    11–13	3	alpha+<	Nationality or Citizenship (ISO 3166-1 alpha-3 code with modifications)
                    14–19	6	numeric	Date of birth (YYMMDD)
                    20	1	numeric	Check digit over digits 14–19
                    21	1	alpha+<	Sex (M, F or < for male, female or unspecified)
                    22–27	6	numeric	Expiration date of passport (YYMMDD)
                    28	1	numeric	Check digit over digits 22–27
                    29–42	14	alpha+num+<	Personal number (may be used by the issuing country as it desires)
                    43	1	numeric+<	Check digit over digits 29–42 (may be < if all characters are <)
                    44	1	numeric	Check digit over digits 1–10, 14–20, and 22–43
                    */
                    // 1-9  (9) Passport number
                    PASSPORT_NUMBER = strLine.Substring(0, 9);
                    confidence_PASSPORT_NUMBER = confidence;
                    System.Diagnostics.Debug.WriteLine($"PASSPORT_NUMBER: {PASSPORT_NUMBER}");

                    // 10   (1)	Check digit over digits 1–9
                    string CheckDigit_1_9 = strLine.Substring(9, 1);
                    string digits_1_9 = strLine.Substring(0, 9);
                    int chk = CalcCheckSum(digits_1_9);
                    if (CheckDigit_1_9 != chk.ToString())
                    {
                        System.Diagnostics.Debug.WriteLine($"!!! CheckDigit_1_9:{CheckDigit_1_9} does not match calculated number:{chk} !!!");
                        throw new Exception($"!!! CheckDigit_1_9:{CheckDigit_1_9} does not match calculated number:{chk} !!!");
                    }

                    // 11–13 (3) Nationality or Citizenship (ISO 3166-1 alpha-3 code with modifications)
                    NATIONALITY = strLine.Substring(10, 3);
                    confidence_NATIONALITY = confidence;
                    System.Diagnostics.Debug.WriteLine($"NATIONALITY: {NATIONALITY}");

                    // 14–19 (6) Date of birth (YYMMDD)
                    DOB = strLine.Substring(13, 6);
                    confidence_DOB = confidence;
                    System.Diagnostics.Debug.WriteLine($"DateOfBirth: {DOB}");

                    // 20    (1) Check digit over digits 14–19
                    string CheckDigit_14_19 = strLine.Substring(19, 1);
                    string digits_14_19 = strLine.Substring(13, 6);
                    chk = CalcCheckSum(digits_14_19);
                    if (CheckDigit_14_19 != chk.ToString())
                    {
                        System.Diagnostics.Debug.WriteLine($"!!! CheckDigit_14_19:{CheckDigit_14_19} does not match calculated number:{chk} !!!");
                        throw new Exception($"!!! CheckDigit_14_19:{CheckDigit_14_19} does not match calculated number:{chk} !!!");
                    }

                    // 21	 (1) Sex (M, F or < for male, female or unspecified)
                    SEX = strLine.Substring(20, 1);
                    confidence_SEX = confidence;
                    System.Diagnostics.Debug.WriteLine($"SEX: {SEX}");

                    // 22–27 (6) Expiration date of passport (YYMMDD)
                    EXPIRY = strLine.Substring(21, 6);
                    confidence_EXPIRY = confidence;
                    System.Diagnostics.Debug.WriteLine($"EXPIRY: {EXPIRY}");

                    // 28    (1) Check digit over digits 22–27
                    string CheckDigit_22_27 = strLine.Substring(27, 1);
                    string digits_22_27 = strLine.Substring(21, 6);
                    chk = CalcCheckSum(digits_22_27);
                    if (CheckDigit_22_27 != chk.ToString())
                    {
                        System.Diagnostics.Debug.WriteLine($"!!! digits_22_27:{CheckDigit_22_27} does not match calculated number:{chk} !!!");
                        throw new Exception($"!!! digits_22_27:{CheckDigit_22_27} does not match calculated number:{chk} !!!");
                    }

                    // 29–42 (14) Personal number (may be used by the issuing country as it desires)
                    PERSONAL_NUMBER = strLine.Substring(28, 14).Replace("<", " ").Trim();
                    confidence_PERSONAL_NUMBER = confidence;
                    System.Diagnostics.Debug.WriteLine($"PERSONAL_NUMBER: {PERSONAL_NUMBER}");

                    // 43    (1) Check digit over digits 29–42 (may be < if all characters are <)
                    string CheckDigit_29_42 = strLine.Substring(42, 1);
                    string digits_29_42 = strLine.Substring(28, 14);
                    if (CheckDigit_29_42 != "<")
                    {
                        chk = CalcCheckSum(digits_29_42);
                        if (CheckDigit_29_42 != chk.ToString())
                        {
                            System.Diagnostics.Debug.WriteLine($"!!! CheckDigit_29_42:{CheckDigit_29_42} does not match calculated number:{chk} !!!");
                            throw new Exception($"!!! CheckDigit_29_42:{CheckDigit_29_42} does not match calculated number:{chk} !!!");
                        }
                    }
                    else
                    {
                        if (digits_29_42 != "<<<<<<<<<<<<<<")
                        {
                            System.Diagnostics.Debug.WriteLine($"!!! digits_29_42:{digits_29_42} does not match the check digit:{CheckDigit_29_42} which expect all chars are '<' !!!");
                            throw new Exception($"!!! digits_29_42:{digits_29_42} does not match the check digit:{CheckDigit_29_42} which expect all chars are '<' !!!");
                        }
                    }

                    // 44    (1) Check digit over digits 1–10, 14–20, and 22–43
                    string CheckDigit_1_10__14_20__22_43 = strLine.Substring(43, 1);
                    string digits_1_10__14_20__22_43 = strLine.Substring(0, 10) + strLine.Substring(13, 7) + strLine.Substring(21, 22);
                    chk = CalcCheckSum(digits_1_10__14_20__22_43);
                    if (CheckDigit_1_10__14_20__22_43 != chk.ToString())
                    {
                        System.Diagnostics.Debug.WriteLine($"!!! CheckDigit_1_10__14_20__22_43:{CheckDigit_1_10__14_20__22_43} does not match calculated number:{chk} !!!");
                        throw new Exception($"!!! CheckDigit_1_10__14_20__22_43:{CheckDigit_1_10__14_20__22_43} does not match calculated number:{chk} !!!");
                    }

                    System.Diagnostics.Debug.WriteLine("----------");
                    result.Success = true;
                    break;
                }
            }// foreach lines in other columns

            // map to result and convert format 
            List<string> lsMissingFields = new List<string>();

            if (result.Success)
            {
                // ISSUING_COUNTRY
                Country? country = FindCountryBy3LetterCode(ISSUING_COUNTRY);
                if (country != null)
                {
                    result.SetCountry(country);
                }
                else
                {
                    lsMissingFields.Add("ISSUING_COUNTRY");
                }

                if (!string.IsNullOrEmpty(FULL_NAME))
                {
                    // FULL_NAME -> lastNameOrFullName 
                    result.lastNameOrFullName = FULL_NAME;
                    result.lastNameOrFullNameConfidence = confidence_FULL_NAME;
                }
                else
                {
                    // SURNAME -> lastNameOrFullName 
                    result.lastNameOrFullName = SURNAME;
                    result.lastNameOrFullNameConfidence = confidence_SURNAME;
                    // GIVEN_NAME -> firstName
                    result.firstName = GIVEN_NAME;
                    result.firstNameConfidence = confidence_GIVEN_NAME;
                }
                if (string.IsNullOrEmpty(result.lastNameOrFullName))
                {
                    lsMissingFields.Add("NAME");
                }

                // PASSPORT_NUMBER
                result.documentNumber = PASSPORT_NUMBER;
                result.documentNumberConfidence = confidence_PASSPORT_NUMBER;
                if (string.IsNullOrEmpty(result.documentNumber))
                {
                    lsMissingFields.Add("PASSPORT_NUMBER");
                }

                // NATIONALITY
                if (!string.IsNullOrEmpty(NATIONALITY))
                {
                    Country? nationality = Code.FindCountryBy3LetterCode(NATIONALITY);
                    if (nationality != null)
                    {
                        result.nationality = nationality.ncode;
                    }
                }
                if (string.IsNullOrEmpty(NATIONALITY))
                {
                    lsMissingFields.Add("PASSPORT_NUMBER");
                }

                // DOB
                if (!string.IsNullOrEmpty(DOB))
                {
                    /*
                    https://www.ibm.com/docs/en/i/7.3?topic=mcdtdi-conversion-2-digit-years-4-digit-years-centuries
                    If a 2-digit year is moved to a 4-digit year, the century (1st 2 digits of the year) are chosen as follows:
                      - If the 2-digit year is greater than or equal to 40, the century used is 1900. In other words, 19 becomes the first 2 digits of the 4-digit year.
                      - If the 2-digit year is less than 40, the century used is 2000. In other words, 20 becomes the first 2 digits of the 4-digit year.
                    */
                    string dobYY = DOB.Substring(0, 2);
                    string dobMM = DOB.Substring(2, 2);
                    string dobDD = DOB.Substring(4, 2);
                    int dd = int.Parse(dobDD);
                    int MM = int.Parse(dobMM);
                    int yy = int.Parse(dobYY);
                    int yyyy = 1900 + yy;
                    if (yy < 40)
                    {
                        yyyy = 2000 + yy;
                    }
                    result.dateOfBirth = $"{yyyy:0000}-{MM:00}-{dd:00}";
                    result.dateOfBirthConfidence = confidence_DOB;
                }
                if (string.IsNullOrEmpty(result.dateOfBirth))
                {
                    lsMissingFields.Add("dateOfBirth");
                }

                // SEX
                result.gender = SEX;
                result.genderConfidence = confidence_SEX;
                if (string.IsNullOrEmpty(result.gender))
                {
                    lsMissingFields.Add("SEX");
                }

                // EXPIRY
                if (!string.IsNullOrEmpty(EXPIRY))
                {
                    string expYY = EXPIRY.Substring(0, 2);
                    string expMM = EXPIRY.Substring(2, 2);
                    string expDD = EXPIRY.Substring(4, 2);
                    int dd = int.Parse(expDD);
                    int MM = int.Parse(expMM);
                    int yy = int.Parse(expYY);
                    int yyyy = 1900 + yy;
                    if (yy < 40)
                    {
                        yyyy = 2000 + yy;
                    }
                    result.documentExpirationDate = $"{yyyy:0000}-{MM:00}-{dd:00}";
                    result.documentExpirationDateConfidence = confidence_EXPIRY;
                }
                else
                {
                    lsMissingFields.Add("EXPIRY");
                }

                // PERSONAL_NUMBER
                if (!string.IsNullOrEmpty(PERSONAL_NUMBER))
                {
                    result.personalNumber = PERSONAL_NUMBER;
                    result.personalNumberConfidence = confidence_PERSONAL_NUMBER;
                }
                else
                {
                    // PERSONAL_NUMBER is optional
                }

                // determine success or not
                if (result.confidences.Worst > 0 && result.confidences.Avg > 0.7 && lsMissingFields.Count == 0)
                {
                    result.Success = true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ExtractFieldsFromReadResultOfPassportMRZ result NOT success");
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
            }

            return result;
        }
#endif

        string CorrectFalseParsedNumericLine(string text)
        {
            string ret = "";
            foreach (char c in text)
            {
                switch (c)
                {
                    case 'o':
                    case 'O':
                        ret += '0';
                        break;
                    case 'l':
                    case 'L':
                        ret += '1';
                        break;
                    case 'b':
                        ret += '6';
                        break;
                    case 'd':
                        ret += '8';
                        break;
                    default:
                        ret += c;
                        break;
                }
            }
            return ret;
        }

        Code.Country FindCountry(string nameOrCode)
        {
            nameOrCode = nameOrCode.ToUpper();

            Code.Country country = null;
            country = Code.FindCountryBy2LetterCode(nameOrCode);
            if(country == null)
            {
                country = Code.FindCountryBy3LetterCode(nameOrCode);
            }
            if (country == null)
            {
                country = Code.FindCountryByName(nameOrCode);
            }

            return country;
        }

        public bool CheckCharInLine(Line line, string textExpected)
        {
            return CheckCharInLine(line.Text, textExpected);
            /*
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
            */
        }

        public bool CheckCharInLine(string text, string textExpected)
        {
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CheckCharInLine text:{text} value:{textExpected}");
            try
            {
                bool bRet = false;
                int countCharIn = 0;
                int countCharNotIn = 0;
                text = text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();

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
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CheckCharInLine [{text}] exception:{ex}");
                return false;
            }
        }

        public bool CheckCharInLine(Line line, Regex regexLine)
        {
            return CheckCharInLine(line.Text, regexLine);
            /*
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
            */
        }
        public bool CheckCharInLine(string text, Regex regexLine)
        {
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CheckCharInLine text:{text} regexLine:{regexLine}");
            try
            {
                bool bRet = false;
                text = text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();

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
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CheckCharInLine [{text}] exception:{ex}");
                return false;
            }
        }
#if true
        static public List<Line> OCRLinesWithTesseractB64(string b64Image, string language = "eng" )
        {
            using (var image = Pix.LoadFromMemory(Convert.FromBase64String(b64Image)))
            {
                return OCRLinesWithTesseractPix(image);
            }
        }

        static public List<Line> OCRLinesWithTesseractEncodedData(byte[] data)
        {
            using (var image = Pix.LoadFromMemory(data))
            {
                return OCRLinesWithTesseractPix(image);
            }
        }

        static TesseractEngine _tesseractEngine = null;

        static TesseractEngine GetTesseractEngine()
        {
            if(_tesseractEngine == null)
            {
                //_tesseractEngine = new TesseractEngine("./tessdata", "eng", EngineMode.Default);

                // load tessdata
                string modulePath = Assembly.GetExecutingAssembly().Location;
                Console.WriteLine("Module Path: " + modulePath);
                FileInfo fiModule = new FileInfo(modulePath);
                if (!fiModule.Exists)
                {
                    System.Diagnostics.Debug.WriteLine("GetTesseractEngine - Module path Not Found!");
                    return null;
                }

                // find template folder in module folder
                string strPathModuleDir = (fiModule.DirectoryName == null) ? "" : fiModule.DirectoryName;
                string strPathTessdataDir = Path.Combine(strPathModuleDir, "tessdata");
                if (!Directory.Exists(strPathTessdataDir))
                {
                    System.Diagnostics.Debug.WriteLine($"Matching Template Directory {strPathTessdataDir} Not Found!");
                    return null;
                }
                else
                {
                    _tesseractEngine = new TesseractEngine(strPathTessdataDir, "eng", EngineMode.Default);
                }
            }
            return _tesseractEngine;
        }

        static public void ReleaseStaticResources()
        {
            if (_tesseractEngine != null)
            {
                _tesseractEngine.Dispose();
                _tesseractEngine = null;
            }
        }

        static readonly char[] TRIM_CHARS = { '\n', ' ', '|' };

        static List<Line> OCRLinesWithTesseractPix(Pix image)
        {
            List<Line> lines = new List<Line>();
            using (var page = GetTesseractEngine().Process(image))
            {
                float fConf = page.GetMeanConfidence();
                if (fConf > 0)
                {
                    ResultIterator ri = page.GetIterator();
                    ri.Begin();

                    do
                    {
                        string text = ri.GetText(PageIteratorLevel.TextLine);
                        if (!string.IsNullOrEmpty(text))
                        {
                            text = text.Trim(TRIM_CHARS);
                            if (text.Length == 0)
                                continue;

                            System.Diagnostics.Debug.WriteLine(text);
                            Rect rcBoundingBox;
                            Line line = new Line();
                            line.Text = text;
                            line.Confidence = fConf;
                            //System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] Text: {line.Text} ({fConf})");
                            if (ri.TryGetBoundingBox(PageIteratorLevel.TextLine, out rcBoundingBox))
                            {
                                //line.BoundingBox = new List<double?> { (double)rcBoundingBox.X1, (double)rcBoundingBox.Y1, (double)rcBoundingBox.X2, (double)rcBoundingBox.Y1,
                                //        (double)rcBoundingBox.X2, (double)rcBoundingBox.Y2, (double)rcBoundingBox.X1, (double)rcBoundingBox.Y2 };
                                line.BoundingBox = new List<double?> { (double)rcBoundingBox.X1, (double)rcBoundingBox.Y1, (double)rcBoundingBox.X2, (double)rcBoundingBox.Y2 };
                                //System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] rcBoundingBox: {rcBoundingBox.X1} {rcBoundingBox.Y1} {rcBoundingBox.X2} {rcBoundingBox.Y2}");
                            }

                            Rect rcBaseline;
                            if (ri.TryGetBaseline(PageIteratorLevel.TextLine, out rcBaseline))
                            {
                                //System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] rcBaseLine: {rcBaseline.X1} {rcBaseline.Y1} {rcBaseline.X2} {rcBaseline.Y2}");
                                line.Baseline = new List<double?> { (double)rcBaseline.X1, (double)rcBaseline.Y1, (double)rcBaseline.X2, (double)rcBaseline.Y2 };
                            }
                            lines.Add(line);
                        }
                    } while (ri.Next(PageIteratorLevel.TextLine));
                }
            }

            return lines;
        }

        static string ResizeImageIfTooLarge(string imageSrcB64)
        {
            byte[] dataImageSrc = Convert.FromBase64String(imageSrcB64);
            SKImage imageSrcTemp = SKImage.FromEncodedData(dataImageSrc);
            if (imageSrcTemp.Width > 2000 || imageSrcTemp.Height > 2000)    // max 2000 x 2000
            {
                SKBitmap bmpSrcTemp = SKBitmap.FromImage(imageSrcTemp);
                int newWidth = bmpSrcTemp.Width;
                int newHeight = bmpSrcTemp.Height;
                if (imageSrcTemp.Width >= imageSrcTemp.Height)
                {
                    float rate = (float)2000 / (float)bmpSrcTemp.Width;
                    newWidth = 2000;
                    newHeight = (int)((float)bmpSrcTemp.Height * rate);
                }
                else
                {
                    float rate = (float)2000 / (float)bmpSrcTemp.Height;
                    newHeight = 2000;
                    newWidth = (int)((float)bmpSrcTemp.Width * rate);
                }
                SKSizeI sizeNew = new SKSizeI(newWidth, newHeight);
                imageSrcTemp = SKImage.FromBitmap(bmpSrcTemp.Resize(sizeNew, SKFilterQuality.None));
                SKData skData = imageSrcTemp.Encode(SKEncodedImageFormat.Jpeg, 90);
                dataImageSrc = skData.ToArray();
                imageSrcB64 = Convert.ToBase64String(dataImageSrc);
            }

            if (dataImageSrc.Length > 2000000)   // max 2MB
            {
                // convert to jpeg
                SKData dataImageSrcJpeg = imageSrcTemp.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 80);
                dataImageSrc = dataImageSrcJpeg.ToArray();
                imageSrcB64 = Convert.ToBase64String(dataImageSrc);
            }

            return imageSrcB64;
        }
#endif
        public static ImgProcLib.MatchTemplateIDCard? LoadMatchTemplate(string strPathTmplDir, string name)
        {
            if (!Directory.Exists(strPathTmplDir))
            {
                Console.WriteLine($"Matching Template Directory {strPathTmplDir} Not Found!");
            }
            else
            {
                DirectoryInfo? diTmpl = new DirectoryInfo(strPathTmplDir);
                string tmpl_name = Path.Combine(diTmpl.FullName, name);
                if (Directory.Exists(tmpl_name))
                {
                    ImgProcLib.MatchTemplateIDCard matchTemplate = new ImgProcLib.MatchTemplateIDCard();
                    if (!matchTemplate.Init(tmpl_name))
                    {
                        Console.WriteLine($"Failed to initialize Matching Template. Template Directory: {tmpl_name}");
                        matchTemplate = null;
                    }
                    return matchTemplate;
                }
            }
            return null;
        }
    }
}
