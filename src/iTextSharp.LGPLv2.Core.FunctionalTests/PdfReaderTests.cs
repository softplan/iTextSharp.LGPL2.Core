using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace iTextSharp.LGPLv2.Core.FunctionalTests;

[TestClass]
public class PdfReaderTests
{
    [TestMethod]
    public void Detect_Blank_Pages_In_Pdf()
    {
        // value where we can consider that this is a blank image
        // can be much higher or lower depending of what is considered as a blank page
        const int blankThreshold = 20;

        var pdfFile = CreateSamplePdfFile();
        using var reader = new PdfReader(pdfFile);

        var blankPages = 0;
        for (var pageNum = 1; pageNum <= reader.NumberOfPages; pageNum++)
        {
            // first check, examine the resource dictionary for /Font or /XObject keys.
            // If either are present -> not blank.
            var pageDict = reader.GetPageN(pageNum);
            var resDict = (PdfDictionary)pageDict.Get(PdfName.Resources);

            var hasFont = resDict.Get(PdfName.Font) != null;
            if (hasFont)
            {
                Console.WriteLine($"Page {pageNum} has font(s).");
                continue;
            }

            var hasImage = resDict.Get(PdfName.Xobject) != null;
            if (hasImage)
            {
                Console.WriteLine($"Page {pageNum} has image(s).");
                continue;
            }

            var content = reader.GetPageContent(pageNum);
            if (content.Length <= blankThreshold)
            {
                Console.WriteLine($"Page {pageNum} is blank");
                blankPages++;
            }
        }

        Assert.AreEqual(1, blankPages, $"{reader.NumberOfPages} page(s) with {blankPages} blank page(s).");
    }


    [TestMethod]
    public void Test_Extract_Text()
    {
        var pdfFile = CreateSamplePdfFile();
        using var reader = new PdfReader(pdfFile);
        var streamBytes = reader.GetPageContent(1);
        var tokenizer = new PrTokeniser(new RandomAccessFileOrArray(streamBytes));

        var stringsList = new List<string>();
        while (tokenizer.NextToken())
        {
            if (tokenizer.TokenType == PrTokeniser.TK_STRING)
            {
                stringsList.Add(tokenizer.StringValue);
            }
        }

        Assert.IsTrue(stringsList.Contains("Hello DNT!"));
    }

    private static byte[] CreateSamplePdfFile()
    {
        using var stream = new MemoryStream();
        using (var document = new Document())
        {
            // step 2
            var writer = PdfWriter.GetInstance(document, stream);
            // step 3
            document.AddAuthor(TestUtils.Author);
            document.Open();
            // step 4
            document.Add(new Paragraph("Hello DNT!"));

            document.NewPage();
            // we don't add anything to this page: newPage() will be ignored
            document.Add(new Phrase(""));

            document.NewPage();
            writer.PageEmpty = false;
        }

        return stream.ToArray();
    }

    [TestMethod]
    public void Test_Draw_Text()
    {
        var pdfFilePath = TestUtils.GetOutputFileName();
        using (var fileStream = new FileStream(pdfFilePath, FileMode.Create))
        {
            using (var pdfDoc = new Document(PageSize.A4))
            {
                var pdfWriter = PdfWriter.GetInstance(pdfDoc, fileStream);

                pdfDoc.AddAuthor(TestUtils.Author);
                pdfDoc.Open();

                pdfDoc.Add(new Paragraph("Test"));

                var cb = pdfWriter.DirectContent;
                var bf = BaseFont.CreateFont();
                cb.BeginText();
                cb.SetFontAndSize(bf, 12);
                cb.MoveText(88.66f, 367);
                cb.ShowText("ld");
                cb.MoveText(-22f, 0);
                cb.ShowText("Wor");
                cb.MoveText(-15.33f, 0);
                cb.ShowText("llo");
                cb.MoveText(-15.33f, 0);
                cb.ShowText("He");
                cb.EndText();

                var tmp = cb.CreateTemplate(250, 25);
                tmp.BeginText();
                tmp.SetFontAndSize(bf, 12);
                tmp.MoveText(0, 7);
                tmp.ShowText("Hello People");
                tmp.EndText();
                cb.AddTemplate(tmp, 36, 343);
            }
        }

        TestUtils.VerifyPdfFileIsReadable(pdfFilePath);
    }

    [TestMethod]
    [DataRow("pdf_com_imagem_corrompida.pdf")]
    [DataRow("issue81.pdf")]
    public async Task DeveAbrirODocumentoEExtrairAssinaturasEQuebrarEmPaginasComSucesso(string fileName)
    {
        var caminhoPdf = TestUtils.GetPdfsPath(fileName);
        var reader = new PdfReader(caminhoPdf);
        ExtractSignatures(reader);
        await IterarPelasPaginasAsync(reader);
    }
    
    protected async Task IterarPelasPaginasAsync(PdfReader reader)
    {
            foreach (var numberOfPage in Enumerable.Range(1, reader.NumberOfPages).ToList())
            {
                var paginaTempFile = Path.GetTempFileName();
                try
                {
                    await GetPageAsync(reader, numberOfPage, paginaTempFile);
                }
                finally
                {
                    if(File.Exists(paginaTempFile)) File.Delete(paginaTempFile);
                }
            }
    }
    
    public async Task GetPageAsync(PdfReader reader,int pageNumber, string fileName)
    {
        using (FileStream fileStream = File.Create(fileName))
            await this.InternalGetPageAsync(reader, pageNumber, fileStream);
    }
    
    private Task InternalGetPageAsync(PdfReader reader,int pageNumber, Stream stream)
    {
        return Task.Run((Action) (() =>
        {
            Document document = new Document();
            PdfCopy pdfCopy = new PdfCopy(document, stream)
            {
                PdfVersion = reader.PdfVersion
            };
            PdfImportedPage importedPage = pdfCopy.GetImportedPage(reader, pageNumber);
            document.Open();
            pdfCopy.AddPage(importedPage);
            document.Close();
        }));
    }

    public static List<Dictionary<string, string>> ExtractSignatures(PdfReader reader)
    {
        var signatures = new List<Dictionary<string, string>>();

        {
            var acroFields = reader.AcroFields;
            var signatureNames = acroFields.GetSignatureNames();

            foreach (var name in signatureNames)
            {
                var pkcs7 = acroFields.VerifySignature(name);
                var signatureData = new Dictionary<string, string>
                {
                    { "SignName", pkcs7.SignName },
                    { "Reason", pkcs7.Reason },
                    { "Location", pkcs7.Location },
                    { "SignDate", pkcs7.SignDate.ToString(CultureInfo.InvariantCulture) },
                    { "Subject", pkcs7.SigningCertificate.SubjectDN.ToString() },
                    { "Issuer", pkcs7.SigningCertificate.IssuerDN.ToString() },
                    { "SerialNumber", pkcs7.SigningCertificate.SerialNumber.ToString() }
                };

                signatures.Add(signatureData);
            }

            return signatures;
        }
    }
}
