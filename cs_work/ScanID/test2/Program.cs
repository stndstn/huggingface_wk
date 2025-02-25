// See https://aka.ms/new-console-template for more information
using Newtonsoft.Json.Linq;
//using ScanIDLib;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;
//using ScanIDDotNet;
using ScanID;
//using ConsoleApp1;

const string BASEADDR_URL_FLORENCE = "http://127.0.0.1:8085/";
//const string BASEADDR_URL_FLORENCE = "";
const string BASEADDR_URL_OMNI_PARSER = "http://127.0.0.1:8086/";
//const string BASEADDR_URL_OMNI_PARSER = "";

// read test json 
if (args.Length != 1)
{
    Console.WriteLine("Usage: test1.exe <test json file>");
    return;
}

string testfile = args[0];
FileInfo fi = new FileInfo(testfile);
if (!fi.Exists)
{
    Console.WriteLine($"File [{testfile}] not found.");
    return;
}

JArray jArray = JArray.Parse(File.ReadAllText(testfile));

JsonSerializerOptions jsonOptions = new JsonSerializerOptions() { AllowTrailingCommas = true };

// for each of json array, OCR test image and comapre the result with expected data.
foreach (JObject jObject in jArray)
{
    string imageFileName = jObject["image"].ToString();
    string imageBackFileName = "";
    if (jObject.ContainsKey("imageBack"))
    {
        imageBackFileName = jObject["imageBack"].ToString();
    }
    FileInfo? fiImage = null;
    FileInfo? fiImageBack = null;
    if (File.Exists(imageFileName))
    {
        fiImage = new FileInfo(imageFileName);
    }
    else
    {
        if (fi.DirectoryName == null)
        {
            Console.WriteLine($"Test json file [{fi.Name}], directory not found.");
            continue;
        }
        // search in the same folder as test json
        fiImage = new FileInfo(Path.Combine(fi.DirectoryName, imageFileName));
        if (!fiImage.Exists)
        {
            Console.WriteLine($"Image file [{imageFileName}] not found.");
            continue;
        }
    }

    if (!string.IsNullOrEmpty(imageBackFileName))
    {
        if (File.Exists(imageBackFileName))
        {
            fiImageBack = new FileInfo(imageBackFileName);
        }
        else
        {
            // search in the same folder as test json
            fiImageBack = new FileInfo(Path.Combine(fi.DirectoryName, imageBackFileName));
            if (!fiImageBack.Exists)
            {
                Console.WriteLine($"Image file [{imageBackFileName}] not found.");
                continue;
            }
        }
    }

    /*
        https://localhost/csekycwebapiazfacedebug/ScanTextFromIDImageFileWithPrompt
        https://localhost/csekycwebapiazface/ScanTextFromIDImageFileWithPrompt
        https://localhost/csekycwebapiazface/ScanTextFromIDImageFile
     */
    Console.WriteLine("try ScanTextFromIDImageFile...");
    OCRIDImageByCloud("https://localhost/csekycwebapiazface/ScanTextFromIDImageFile", fiImage, fiImageBack);
    //Console.WriteLine("try ScanTextFromIDImageFileWithPrompt...");
    //OCRIDImageByCloud("https://localhost/csekycwebapiazface/ScanTextFromIDImageFileWithPrompt", fiImage, fiImageBack, expectedDataJson);

} // foreach

void OCRIDImageByCloud(string webapiUrl, FileInfo? fiImage, FileInfo? fiImageBack, string expectedDataJson = "")
{
    if(fiImage == null)
    {
        Console.WriteLine("Image file not found.");
        return;
    }

    bool isValid = false;
    StreamContent fileImage = new StreamContent(fiImage.OpenRead());
    MultipartFormDataContent form = new MultipartFormDataContent();
    switch (fiImage.Extension.ToLower())
    {
        case ".pdf":
            fileImage.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            break;
        case ".jpg":
        case ".jpeg":
            fileImage.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            break;
        case ".png":
            fileImage.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            break;
        default:
            throw new Exception($"File type not supported.[{fiImage.Extension}]");
            break;
    }
    form.Add(fileImage, "idImage", fiImage.Name);

    if (fiImageBack != null)
    {
        StreamContent fileImageBack = new StreamContent(fiImageBack.OpenRead());
        switch (fiImageBack.Extension.ToLower())
        {
            case ".pdf":
                fileImageBack.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                break;
            case ".jpg":
            case ".jpeg":
                fileImageBack.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                break;
            case ".png":
                fileImageBack.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                break;
            default:
                throw new Exception($"File type not supported.[{fiImageBack.Extension}]");
                break;
        }
        form.Add(fileImageBack, "idImageBack", fiImageBack.Name);
    }

    Console.Write($"{fiImage.Name} : ");

    // post
    string resultJson = "";
    using (var httpClient = new HttpClient() { Timeout = TimeSpan.FromMinutes(5) })
    {
        DateTime dtStart = DateTime.Now;
        //var response = httpClient.PostAsync($"https://localhost/csekycwebapiazfacedebug/ScanTextFromIDImageFileWithPrompt", form).GetAwaiter().GetResult();
        //var response = httpClient.PostAsync($"https://localhost/csekycwebapiazface/ScanTextFromIDImageFileWithPrompt", form).GetAwaiter().GetResult();
        var response = httpClient.PostAsync(webapiUrl, form).GetAwaiter().GetResult();
        DateTime dtEnd = DateTime.Now;
        var elapsed = dtEnd - dtStart;
        Console.WriteLine(" Elapsed: " + elapsed);
        System.Diagnostics.Debug.WriteLine("StatusCode: " + response.StatusCode);
        System.Diagnostics.Debug.WriteLine("Headers: " + response.Headers);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            System.Diagnostics.Debug.WriteLine("Content: " + response.Content);
            throw new Exception(response.ReasonPhrase);
        }

        if (response.Content != null)
        {
            resultJson = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            System.Diagnostics.Debug.WriteLine("Content: " + resultJson);
            string filenameJsonResult = Path.GetFullPath(fiImage.FullName);
            filenameJsonResult = Path.ChangeExtension(filenameJsonResult, "json");
            using (StreamWriter sw = new StreamWriter(filenameJsonResult))
            {
                sw.Write(resultJson);
            }
        }
    }
    /////////////
}

bool IsScanMYDLResultValid(ScanMYDLResult result, ScanMYDLResult expected)
{
    int countNotMatch = 0;

    if (result == null || expected == null)
    {
        return false;
    }

    if (result.lastNameOrFullName?.Trim() != expected.lastNameOrFullName?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: lastNameOrFullName: " + result.lastNameOrFullName);
        Console.WriteLine("  Expected    : lastNameOrFullName: " + expected.lastNameOrFullName);
    }

    if (result.documentNumber?.Trim() != expected.documentNumber?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: documentNumber: " + result.documentNumber);
        Console.WriteLine("  Expected    : documentNumber: " + expected.documentNumber);
    }

    if (result.nationality?.Trim() != expected.nationality?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: nationality: " + result.nationality);
        Console.WriteLine("  Expected    : nationality: " + expected.nationality);
    }

    if (result.documentExpirationDate?.Trim() != expected.documentExpirationDate?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: documentExpirationDate: " + result.documentExpirationDate);
        Console.WriteLine("  Expected    : documentExpirationDate: " + expected.documentExpirationDate);
    }

    if (result.documentIssueDate?.Trim() != expected.documentIssueDate?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: documentIssueDate: " + result.documentIssueDate);
        Console.WriteLine("  Expected    : documentIssueDate: " + expected.documentIssueDate);
    }

    if (result.addressLine1?.Trim() != expected.addressLine1?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressLine1: " + result.addressLine1);
        Console.WriteLine("  Expected    : addressLine1: " + expected.addressLine1);
    }

    if (result.addressLine2?.Trim() != expected.addressLine2?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressLine2: " + result.addressLine2);
        Console.WriteLine("  Expected    : addressLine2: " + expected.addressLine2);
    }

    if (result.postcode?.Trim() != expected.postcode?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: postcode: " + result.postcode);
        Console.WriteLine("  Expected    : postcode: " + expected.postcode);
    }

    if (result.addressTown?.Trim() != expected.addressTown?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressTown: " + result.addressTown);
        Console.WriteLine("  Expected    : addressTown: " + expected.addressTown);
    }

    return (countNotMatch == 0) ? true : false;
}

bool IsScanMyKadResultValid(ScanMyKadResult result, ScanMyKadResult expected)
{
    int countNotMatch = 0;

    if (result == null || expected == null)
    {
        return false;
    }

    if (result.lastNameOrFullName?.Trim() != expected.lastNameOrFullName?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: lastNameOrFullName: " + result.lastNameOrFullName);
        Console.WriteLine("  Expected    : lastNameOrFullName: " + expected.lastNameOrFullName);
    }

    if (result.documentNumber?.Trim() != expected.documentNumber?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: documentNumber: " + result.documentNumber);
        Console.WriteLine("  Expected    : documentNumber: " + expected.documentNumber);
    }

    if (result.nationality?.Trim() != expected.nationality?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: nationality: " + result.nationality);
        Console.WriteLine("  Expected    : nationality: " + expected.nationality);
    }

    if (result.addressLine1?.Trim() != expected.addressLine1?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressLine1: " + result.addressLine1);
        Console.WriteLine("  Expected    : addressLine1: " + expected.addressLine1);
    }

    if (result.addressLine2?.Trim() != expected.addressLine2?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressLine2: " + result.addressLine2);
        Console.WriteLine("  Expected    : addressLine2: " + expected.addressLine2);
    }

    if (result.postcode?.Trim() != expected.postcode?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: postcode: " + result.postcode);
        Console.WriteLine("  Expected    : postcode: " + expected.postcode);
    }

    if (result.addressTown?.Trim() != expected.addressTown?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressTown: " + result.addressTown);
        Console.WriteLine("  Expected    : addressTown: " + expected.addressTown);
    }

    if (result.gender?.Trim() != expected.gender?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: gender: " + result.gender);
        Console.WriteLine("  Expected    : gender: " + expected.gender);
    }

    return (countNotMatch == 0) ? true : false;
}
#if true
bool IsScanPHNIResultValid(ScanPHNIResult result, ScanPHNIResult expected)
{
    int countNotMatch = 0;

    if (result == null || expected == null)
    {
        return false;
    }

    if (result.lastNameOrFullName?.Trim() != expected.lastNameOrFullName?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: lastNameOrFullName: " + result.lastNameOrFullName);
        Console.WriteLine("  Expected    : lastNameOrFullName: " + expected.lastNameOrFullName);
    }
    if (result.firstName?.Trim() != expected.firstName?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: firstName: " + result.firstName);
        Console.WriteLine("  Expected    : firstName: " + expected.firstName);
    }
    if (result.middleName?.Trim() != expected.middleName?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: middleName: " + result.middleName);
        Console.WriteLine("  Expected    : middleName: " + expected.middleName);
    }

    if (result.dateOfBirth?.Trim() != expected.dateOfBirth?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: dateOfBirth: " + result.dateOfBirth);
        Console.WriteLine("  Expected    : dateOfBirth: " + expected.dateOfBirth);
    }

    if (result.documentNumber?.Trim() != expected.documentNumber?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: documentNumber: " + result.documentNumber);
        Console.WriteLine("  Expected    : documentNumber: " + expected.documentNumber);
    }

    if (result.nationality?.Trim() != expected.nationality?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: nationality: " + result.nationality);
        Console.WriteLine("  Expected    : nationality: " + expected.nationality);
    }

    if (result.addressLine1?.Trim() != expected.addressLine1?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressLine1: " + result.addressLine1);
        Console.WriteLine("  Expected    : addressLine1: " + expected.addressLine1);
    }

    if (result.addressLine2?.Trim() != expected.addressLine2?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressLine2: " + result.addressLine2);
        Console.WriteLine("  Expected    : addressLine2: " + expected.addressLine2);
    }

    if (result.postcode?.Trim() != expected.postcode?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: postcode: " + result.postcode);
        Console.WriteLine("  Expected    : postcode: " + expected.postcode);
    }

    if (result.addressTown?.Trim() != expected.addressTown?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressTown: " + result.addressTown);
        Console.WriteLine("  Expected    : addressTown: " + expected.addressTown);
    }

    if (result.gender?.Trim() != expected.gender?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: gender: " + result.gender);
        Console.WriteLine("  Expected    : gender: " + expected.gender);
    }

    return (countNotMatch == 0) ? true : false;
}
#endif

bool IsScanPHUMIDResultValid(ScanPHUMIDResult result, ScanPHUMIDResult expected)
{
    int countNotMatch = 0;

    if (result == null || expected == null)
    {
        return false;
    }

    if (result.lastNameOrFullName?.Trim() != expected.lastNameOrFullName?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: lastNameOrFullName: " + result.lastNameOrFullName);
        Console.WriteLine("  Expected    : lastNameOrFullName: " + expected.lastNameOrFullName);
    }
    if (result.firstName?.Trim() != expected.firstName?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: firstName: " + result.firstName);
        Console.WriteLine("  Expected    : firstName: " + expected.firstName);
    }
    if (result.middleName?.Trim() != expected.middleName?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: middleName: " + result.middleName);
        Console.WriteLine("  Expected    : middleName: " + expected.middleName);
    }

    if (result.dateOfBirth?.Trim() != expected.dateOfBirth?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: dateOfBirth: " + result.dateOfBirth);
        Console.WriteLine("  Expected    : dateOfBirth: " + expected.dateOfBirth);
    }

    if (result.documentNumber?.Trim() != expected.documentNumber?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: documentNumber: " + result.documentNumber);
        Console.WriteLine("  Expected    : documentNumber: " + expected.documentNumber);
    }

    if (result.nationality?.Trim() != expected.nationality?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: nationality: " + result.nationality);
        Console.WriteLine("  Expected    : nationality: " + expected.nationality);
    }

    if (result.addressLine1?.Trim() != expected.addressLine1?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressLine1: " + result.addressLine1);
        Console.WriteLine("  Expected    : addressLine1: " + expected.addressLine1);
    }

    if (result.addressLine2?.Trim() != expected.addressLine2?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressLine2: " + result.addressLine2);
        Console.WriteLine("  Expected    : addressLine2: " + expected.addressLine2);
    }

    if (result.postcode?.Trim() != expected.postcode?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: postcode: " + result.postcode);
        Console.WriteLine("  Expected    : postcode: " + expected.postcode);
    }

    if (result.addressTown?.Trim() != expected.addressTown?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressTown: " + result.addressTown);
        Console.WriteLine("  Expected    : addressTown: " + expected.addressTown);
    }

    if (result.gender?.Trim() != expected.gender?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: gender: " + result.gender);
        Console.WriteLine("  Expected    : gender: " + expected.gender);
    }

    return (countNotMatch == 0) ? true : false;
}

bool IsScanPHDLResultValid(ScanPHDLResult result, ScanPHDLResult expected)
{
    int countNotMatch = 0;

    if (result == null || expected == null)
    {
        return false;
    }

    if (result.lastNameOrFullName?.Trim() != expected.lastNameOrFullName?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: lastNameOrFullName: " + result.lastNameOrFullName);
        Console.WriteLine("  Expected    : lastNameOrFullName: " + expected.lastNameOrFullName);
    }
    if (result.firstName?.Trim() != expected.firstName?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: firstName: " + result.firstName);
        Console.WriteLine("  Expected    : firstName: " + expected.firstName);
    }
    if (result.middleName?.Trim() != expected.middleName?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: middleName: " + result.middleName);
        Console.WriteLine("  Expected    : middleName: " + expected.middleName);
    }

    if (result.dateOfBirth?.Trim() != expected.dateOfBirth?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: dateOfBirth: " + result.dateOfBirth);
        Console.WriteLine("  Expected    : dateOfBirth: " + expected.dateOfBirth);
    }

    if (result.documentNumber?.Trim() != expected.documentNumber?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: documentNumber: " + result.documentNumber);
        Console.WriteLine("  Expected    : documentNumber: " + expected.documentNumber);
    }

    if (result.nationality?.Trim() != expected.nationality?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: nationality: " + result.nationality);
        Console.WriteLine("  Expected    : nationality: " + expected.nationality);
    }

    if (result.addressLine1?.Trim() != expected.addressLine1?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressLine1: " + result.addressLine1);
        Console.WriteLine("  Expected    : addressLine1: " + expected.addressLine1);
    }

    if (result.addressLine2?.Trim() != expected.addressLine2?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressLine2: " + result.addressLine2);
        Console.WriteLine("  Expected    : addressLine2: " + expected.addressLine2);
    }

    if (result.postcode?.Trim() != expected.postcode?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: postcode: " + result.postcode);
        Console.WriteLine("  Expected    : postcode: " + expected.postcode);
    }

    if (result.addressTown?.Trim() != expected.addressTown?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressTown: " + result.addressTown);
        Console.WriteLine("  Expected    : addressTown: " + expected.addressTown);
    }

    if (result.gender?.Trim() != expected.gender?.Trim())
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: gender: " + result.gender);
        Console.WriteLine("  Expected    : gender: " + expected.gender);
    }

    return (countNotMatch == 0) ? true : false;
}

