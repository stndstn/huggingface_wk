//using ImgProcLib;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using ScanID;
using ScanIDWebApi.Model;
using SkiaSharp;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

//const string BASEADDR_URL_FLORENCE = "http://127.0.0.1:8085/";
const string BASEADDR_URL_FLORENCE = "http://192.168.12.255:8085/";
//const string BASEADDR_URL_FLORENCE = "";
//const string BASEADDR_URL_OMNI_PARSER = "http://127.0.0.1:8086/";
const string BASEADDR_URL_OMNI_PARSER = "";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

string[] allowedOrigins = GetAllowedOrigins(builder.Configuration);
if (allowedOrigins == null || allowedOrigins.Length < 1)
{
    Console.WriteLine("Warning! AppSettings:AllowedOrigins section is missing or empty. " +
        "This setting is mandatory to be set as " +
        "only those origin URsL will be allowed in the API for CORS!");
}

string ocrUrl = builder.Configuration.GetValue<string>("OcrUrl");
if(string.IsNullOrEmpty(ocrUrl))
    ocrUrl = BASEADDR_URL_FLORENCE;

//app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors(options => options
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .WithOrigins(allowedOrigins)
                  .AllowCredentials());

//
// load matching template and initialize ImgProcLib.MatchTemplateMyKad object
//
string tempDir = $"{Path.GetTempPath()}CSeKYCWebApi\\";
Directory.CreateDirectory(tempDir);

// find template folder in module folder
// get module path 
string modulePath = Assembly.GetExecutingAssembly().Location;
Console.WriteLine("Module Path: " + modulePath);
FileInfo fiModule = new FileInfo(modulePath);
if (!fiModule.Exists)
{
    Console.Error.WriteLine("Module File Not Found!");
    return;
}
string strPathModuleDir = (fiModule.DirectoryName == null) ? "" : fiModule.DirectoryName;
string strPathTmplDir = Path.Combine(strPathModuleDir, "tmpl");
/*
ImgProcLib.MatchTemplateIDCard? matchTemplateMyKad = ScanIDOCR.LoadMatchTemplate(strPathTmplDir, "mykad_fr");
ImgProcLib.MatchTemplateIDCard? matchTemplateMYDL = ScanIDOCR.LoadMatchTemplate(strPathTmplDir, "mydl_fr");
ImgProcLib.MatchTemplateIDCard? matchTemplatePHDL = ScanIDOCR.LoadMatchTemplate(strPathTmplDir, "phdl_fr");
ImgProcLib.MatchTemplateIDCard? matchTemplatePHUMID = ScanIDOCR.LoadMatchTemplate(strPathTmplDir, "phumid_fr");
ImgProcLib.MatchTemplateIDCard? matchTemplatePHNI = ScanIDOCR.LoadMatchTemplate(strPathTmplDir, "phni_fr");
Dictionary<string, ImgProcLib.MatchTemplateIDCard?> matchTemplatesMY = new Dictionary<string, ImgProcLib.MatchTemplateIDCard?>(){
    { "MYKAD", matchTemplateMyKad },
    { "MYDL", matchTemplateMYDL },
};
Dictionary<string, ImgProcLib.MatchTemplateIDCard?> matchTemplatesPH = new Dictionary<string, ImgProcLib.MatchTemplateIDCard?>(){
    { "PHDL", matchTemplatePHDL },
    { "PHUMID", matchTemplatePHUMID },
    { "PHUMID1", matchTemplatePHUMID },
    { "PHUMID2", matchTemplatePHUMID },
    { "PHNI", matchTemplatePHNI }
};
*/

app.MapGet("/tempdir", () =>
{
    string tempDir = $"{Path.GetTempPath()}ScanIDWebApi\\";
    return Results.Ok(tempDir);
});

app.MapGet("/version", () =>
{
    //using System.Reflection;
    //using System.Diagnostics;
    Assembly assembly = Assembly.GetExecutingAssembly();
    FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
    string productVersion = fileVersionInfo.ProductVersion;
    string version = $"Product Version:{fileVersionInfo.ProductVersion} File Version:{fileVersionInfo.FileVersion}";
    return Results.Ok(version);
});

app.MapPost("/ScanTextFromIDImageBase64", async (IDImageBase64 idImageBase64, HttpRequest request) =>
{
    /*
    string body = "";
    using (StreamReader stream = new StreamReader(request.Body))
    {
        body = await stream.ReadToEndAsync();
    }
    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] request body: {body}");
    */
    //MyKad

    ScanIDResult? scanIDResult = null;
    string err = "";
    string country = idImageBase64.country;
    var idType = idImageBase64.idType;
    string docType = "";
    //Dictionary<string, ImgProcLib.MatchTemplateIDCard?> matchTemplates = null;

    if (request.Query.ContainsKey("country"))
    {
        if (string.IsNullOrEmpty(country))
        {
            country = request.Query["country"].ToString().ToUpper();
            if (string.IsNullOrEmpty(country))
            {
                err = "query.country is empty";
            }
        }
    }

    if (request.Query.ContainsKey("idType"))
    {
        if (string.IsNullOrEmpty(idType))
        {
            idType = request.Query["idType"].ToString().ToUpper();
        }
    }

    ScanIDOCR scanIDOCR = null;

    switch (country)
    {
        case "MY":
            switch (idType)
            {
                case "MY":
                    docType = "MYKAD";
                    break;
                case "DL":
                    docType = "MYDL";
                    break;
                default:
                    //err = $"idType '{idType}' not supported for country '{country}'";
                    break;
            }
            scanIDOCR = ScanIDOCR.Create(strPathTmplDir, "MY");
            break;
        case "PH":
            switch (idType)
            {
                case "DL":
                    docType = "PHDL";
                    break;
                case "NI":
                    docType = "PHNI";
                    break;
                case "UI":
                    docType = "PHUMID";
                    break;
                default:
                    //err = $"idType '{idType}' not supported for country '{country}'";
                    break;
            }
            scanIDOCR = ScanIDOCR.Create(strPathTmplDir, "PH");
            break;
        default:
            err = $"country '${country}' is not supported";
            break;
    }

    if (!string.IsNullOrEmpty(err))
    {
        scanIDResult = new ScanIDResult();
        scanIDResult.Success = false;
        scanIDResult.Error = err;
        return Results.Ok(scanIDResult);
    }

    try
    {
        if(scanIDOCR != null)
        {
            scanIDResult = scanIDOCR.ScanIDB64(ocrUrl, idImageBase64.base64, idImageBase64.base64Back, docType);
        }
        return Results.Ok(scanIDResult);
    }
    catch (Exception ex)
    {
        err = ex.Message;
        return Results.BadRequest(err);
    }
});

app.MapPost("/ScanTextFromIDImageFile", (HttpRequest request) =>
//app.MapPost("/ScanTextFromIDImageFile", (HttpContext ctx) =>
{
    string err = "";
    ScanIDResult? scanIDResult = null;
    string country = "";
    string idType = "";
    string docType = "";

    // parameter 'country' is mandatory 
    if (request.Form.ContainsKey("country"))
    {
        country = request.Form["country"].ToString().ToUpper();
        if (string.IsNullOrEmpty(country))
            err = "form.country is empty";
    }
    if (request.Query.ContainsKey("country"))
    {
        if (string.IsNullOrEmpty(country))
        {
            err = "";
            country = request.Query["country"].ToString().ToUpper();
            if (string.IsNullOrEmpty(country))
                err = "form.country is empty";
        }
    }
    if (!string.IsNullOrEmpty(err))
    {
        scanIDResult = new ScanIDResult();
        scanIDResult.Success = false;
        scanIDResult.Error = err;
        return Results.Ok(scanIDResult);
    }

    // parameter 'idType' is optional 
    if (request.Form.ContainsKey("idType"))
    {
        idType = request.Form["idType"].ToString().ToUpper();
    }
    if (request.Query.ContainsKey("idType"))
    {
        if (string.IsNullOrEmpty(idType))
        {
            idType = request.Query["idType"].ToString().ToUpper();
        }
    }

    ScanIDOCR? scanIDOCR = null;

    switch (country)
    {
        case "MY":
            switch (idType)
            {
                case "MY":
                    docType = "MYKAD";
                    break;
                case "DL":
                    docType = "MYDL";
                    break;
                default:
                    //err = $"idType '{idType}' not supported for country '{country}'";
                    break;
            }
            scanIDOCR = ScanIDOCR.Create(strPathTmplDir, "MY");
            break;
        case "PH":
            switch (idType)
            {
                case "DL":
                    docType = "PHDL";
                    break;
                case "NI":
                    docType = "PHNI";
                    break;
                case "UI":
                    docType = "PHUMID";
                    break;
                default:
                    //err = $"idType '{idType}' not supported for country '{country}'";
                    break;
            }
            scanIDOCR = ScanIDOCR.Create(strPathTmplDir, "PH");
            break;
        default:
            err = $"country '${country}' is not supported";
            break;
    }

    try
    {
        var files = request.Form.Files;
        byte[] dataImageSrc = null;
        string imageFilename = "";
        byte[] dataImageSrcBack = null;
        string imageFilenameBack = "";
        string imageSrcB64 = "";
        string imageBackSrcB64 = "";
        foreach (var file in files)
        {
            if (file.Name == "idImage")
            {
                using (MemoryStream ms = new MemoryStream())
                using (Stream s = file.OpenReadStream())
                {
                    s.CopyTo(ms);
                    dataImageSrc = ms.ToArray();
                    imageSrcB64 = Convert.ToBase64String(dataImageSrc);
                }
                imageFilename = file.FileName;
            }
            else if (file.Name == "idImageBack")
            {
                using (MemoryStream ms = new MemoryStream())
                using (Stream s = file.OpenReadStream())
                {
                    s.CopyTo(ms);
                    dataImageSrcBack = ms.ToArray();
                    imageBackSrcB64 = Convert.ToBase64String(dataImageSrcBack);
                }
                imageFilenameBack = file.FileName;
            }
        }
        if (dataImageSrc == null)
        {
            //return Results.BadRequest("File named 'idImage' not found in the form.");
            err = "File named 'idImage' not found in the form.";
        }
        else
        {
            //List<LabelInfo> labelsFound = new List<LabelInfo>();
            //List<Line> linesNotLabel = new List<Line>();
            if (scanIDOCR != null)
            {
                scanIDResult = scanIDOCR.ScanIDB64(ocrUrl, imageSrcB64, imageBackSrcB64, docType);
            }

            return Results.Ok(scanIDResult);
        }
    }
    catch (Exception ex)
    {
        err = ex.ToString();
        //return Results.BadRequest(err);
        //ctx.Response.StatusCode = 400;
        //await ctx.Response.WriteAsJsonAsync(err);
    }
    return Results.BadRequest(err);
});

app.MapPost("/ScanTextFromIDImageBase64Az", async (IDImageBase64 idImageBase64, HttpRequest request) =>
{
    /*
    string body = "";
    using (StreamReader stream = new StreamReader(request.Body))
    {
        body = await stream.ReadToEndAsync();
    }
    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] request body: {body}");
    */
    //MyKad

    ScanIDResult? scanIDResult = null;
    string err = "";
    string country = idImageBase64.country;
    var idType = idImageBase64.idType;
    string docType = "";
    //Dictionary<string, ImgProcLib.MatchTemplateIDCard?> matchTemplates = null;

    if (request.Query.ContainsKey("country"))
    {
        if (string.IsNullOrEmpty(country))
        {
            country = request.Query["country"].ToString().ToUpper();
            if (string.IsNullOrEmpty(country))
            {
                err = "query.country is empty";
            }
        }
    }

    if (request.Query.ContainsKey("idType"))
    {
        if (string.IsNullOrEmpty(idType))
        {
            idType = request.Query["idType"].ToString().ToUpper();
        }
    }

    try
    {
        switch (country)
        {
            case "MY":
                scanIDResult = OCRIDImageB64ByAzure(ScanIDOCR.Create(strPathTmplDir, "MY"), idImageBase64.base64, idImageBase64.base64Back);
                break;
            case "PH":
                scanIDResult = OCRIDImageB64ByAzure(ScanIDOCR.Create(strPathTmplDir, "PH"), idImageBase64.base64, idImageBase64.base64Back);
                break;
            default:
                err = $"country '${country}' is not supported";
                break;
        }
    }
    catch (Exception ex)
    {
        err = ex.Message;
        return Results.BadRequest(err);
    }

    if (!string.IsNullOrEmpty(err))
    {
        scanIDResult = new ScanIDResult();
        scanIDResult.Success = false;
        scanIDResult.Error = err;
        return Results.Ok(scanIDResult);
    }

    return Results.Ok(scanIDResult);
});

app.MapPost("/ScanTextFromIDImageFileAz", (HttpRequest request) =>
{
    string err = "";
    ScanIDResult? scanIDResult = null;
    string country = "";
    string idType = "";
    //string docType = "";

    // parameter 'country' is mandatory 
    if (request.Form.ContainsKey("country"))
    {
        country = request.Form["country"].ToString().ToUpper();
        if (string.IsNullOrEmpty(country))
            err = "form.country is empty";
    }
    if (request.Query.ContainsKey("country"))
    {
        if (string.IsNullOrEmpty(country))
        {
            err = "";
            country = request.Query["country"].ToString().ToUpper();
            if (string.IsNullOrEmpty(country))
                err = "form.country is empty";
        }
    }
    if (!string.IsNullOrEmpty(err))
    {
        scanIDResult = new ScanIDResult();
        scanIDResult.Success = false;
        scanIDResult.Error = err;
        return Results.Ok(scanIDResult);
    }

    // parameter 'idType' is optional 
    if (request.Form.ContainsKey("idType"))
    {
        idType = request.Form["idType"].ToString().ToUpper();
    }
    if (request.Query.ContainsKey("idType"))
    {
        if (string.IsNullOrEmpty(idType))
        {
            idType = request.Query["idType"].ToString().ToUpper();
        }
    }

    try
    {
        var files = request.Form.Files;
        byte[] dataImageSrc = null;
        //string imageFilename = "";
        byte[] dataImageSrcBack = null;
        //string imageFilenameBack = "";
        //string imageSrcB64 = "";
        //string imageBackSrcB64 = "";
        foreach (var file in files)
        {
            if (file.Name == "idImage")
            {
                using (MemoryStream ms = new MemoryStream())
                using (Stream s = file.OpenReadStream())
                {
                    s.CopyTo(ms);
                    dataImageSrc = ms.ToArray();
                    //imageSrcB64 = Convert.ToBase64String(dataImageSrc);
                }
                //imageFilename = file.FileName;
            }
            else if (file.Name == "idImageBack")
            {
                using (MemoryStream ms = new MemoryStream())
                using (Stream s = file.OpenReadStream())
                {
                    s.CopyTo(ms);
                    dataImageSrcBack = ms.ToArray();
                    //imageBackSrcB64 = Convert.ToBase64String(dataImageSrcBack);
                }
                //imageFilenameBack = file.FileName;
            }
        }
        if (dataImageSrc == null)
        {
            //return Results.BadRequest("File named 'idImage' not found in the form.");
            err = "File named 'idImage' not found in the form.";
        }
        else
        {
            switch (country)
            {
                case "MY":
                    scanIDResult = OCRIDImageDataByAzure(ScanIDOCR.Create(strPathTmplDir, "MY"), dataImageSrc, dataImageSrcBack);
                    break;
                case "PH":
                    scanIDResult = OCRIDImageDataByAzure(ScanIDOCR.Create(strPathTmplDir, "PH"), dataImageSrc, dataImageSrcBack);
                    break;
                default:
                    err = $"country '${country}' is not supported";
                    break;
            }
            return Results.Ok(scanIDResult);
        }
    }
    catch (Exception ex)
    {
        err = ex.ToString();
        //return Results.BadRequest(err);
        //ctx.Response.StatusCode = 400;
        //await ctx.Response.WriteAsJsonAsync(err);
    }
    return Results.BadRequest(err);
});

app.Run();


string[] GetAllowedOrigins(IConfiguration config)
{
    string value = config.GetValue<string>("AllowedOrigins");
    if (string.IsNullOrWhiteSpace(value))
        value = "*";
    return value.Split(";");
}

/*
ScanIDResult? OCRIDImageFileByAzure(ScanIDOCR scanIDOCR, FileInfo fiImage, FileInfo fiImageBack)
{
    StreamContent fileImage = new StreamContent(fiImage.OpenRead());
    byte[] fileImageBytes = fileImage.ReadAsByteArrayAsync().GetAwaiter().GetResult();
    byte[]? fileImageBackBytes = null;
    if (fiImageBack != null)
    {
        StreamContent fileImageBack = new StreamContent(fiImageBack.OpenRead());
        fileImageBackBytes = fileImageBack.ReadAsByteArrayAsync().GetAwaiter().GetResult();
    }
    return OCRIDImageDataByAzure(scanIDOCR, fileImageBytes, fileImageBackBytes);
}
*/
ScanIDResult? OCRIDImageB64ByAzure(ScanIDOCR scanIDOCR, string fileImageBase64, string fileImageBackBase64)
{
    byte[] fileImageBytes = Convert.FromBase64String(fileImageBase64);
    byte[]? fileImageBackBytes = null;
    if (!string.IsNullOrEmpty(fileImageBackBase64))
    {
        fileImageBackBytes = Convert.FromBase64String(fileImageBackBase64);
    }
    return OCRIDImageDataByAzure(scanIDOCR, fileImageBytes, fileImageBackBytes);
}

ScanIDResult? OCRIDImageDataByAzure(ScanIDOCR scanIDOCR, byte[]? fileImageBytes, byte[]? fileImageBackBytes)
{
    if(fileImageBytes == null || fileImageBytes.Length == 0)
    {
        return null;
    }

    string fileImageBase64 = Convert.ToBase64String(fileImageBytes);
    string fileImageBackBase64 = "";
    if (fileImageBackBytes != null && fileImageBackBytes.Length > 0)
    {
        fileImageBackBase64 = Convert.ToBase64String(fileImageBackBytes);
    }

    ScanIDResult? scanIDResult = ScanID.AzureVisionLib.GetScanResultAsync(fileImageBytes, fileImageBackBytes, scanIDOCR).GetAwaiter().GetResult();
    System.Diagnostics.Debug.WriteLine("ScanIDResult: " + scanIDResult);
    return scanIDResult;
}

