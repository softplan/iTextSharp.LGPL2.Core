using System.IO;
using iTextSharp.text.pdf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace iTextSharp.LGPLv2.Core.FunctionalTests;

[TestClass]
public class PdfReaderRC5Tests
{
    [TestCleanup]
    public void TestCleanup()
        => PdfReader.AllowOpenWithFullPermissions = false;

    [TestMethod]
    public void Deve_Abrir_Pdf_Com_Criptografia_R5()
    {
        var caminho = TestUtils.GetPdfsPath("pdf_RC5_encryption.pdf");

        using var reader = new PdfReader(File.ReadAllBytes(caminho));

        Assert.IsTrue(reader.IsEncrypted());
        Assert.IsTrue(reader.NumberOfPages > 0);
    }
    
    [TestMethod]
    public void Deve_Abrir_Pdf_Com_Criptografia_R5_With_Two_Pages()
    {
        var caminho = TestUtils.GetPdfsPath("pdf_RC5_encryption_2pages.pdf");

        using var reader = new PdfReader(File.ReadAllBytes(caminho));

        Assert.IsTrue(reader.IsEncrypted());
        Assert.IsTrue(reader.NumberOfPages > 0);
    }

    [TestMethod]
    public void Deve_Abrir_Pdf_R5_Com_Restricao_De_Owner()
    {
        PdfReader.AllowOpenWithFullPermissions = true;

        var caminho = TestUtils.GetPdfsPath("pdf_RC5_encryption_2.pdf");

        using var reader = new PdfReader(File.ReadAllBytes(caminho));

        Assert.IsTrue(reader.IsEncrypted());
        Assert.IsTrue(reader.NumberOfPages > 0);
    }

    [TestMethod]
    public void Deve_Lancar_Erro_Ao_Modificar_Pdf_R5_Sem_AllowOpenWithFullPermissions()
    {
        PdfReader.AllowOpenWithFullPermissions = false;

        var caminho = TestUtils.GetPdfsPath("pdf_RC5_encryption_2.pdf");
        using var reader = new PdfReader(File.ReadAllBytes(caminho));
        using var output = new MemoryStream();

        var ex = Assert.ThrowsException<BadPasswordException>(
            () => _ = new PdfStamper(reader, output));

        Assert.AreEqual("PdfReader not opened with owner password", ex.Message);
    }
    
    
    [TestMethod]
    public void Deve_Lancar_Erro_Ao_Modificar_Pdf_R5_2Pages_Sem_AllowOpenWithFullPermissions()
    {
        PdfReader.AllowOpenWithFullPermissions = false;

        var caminho = TestUtils.GetPdfsPath("pdf_RC5_encryption_owner_readonly_2pages.pdf");
        using var reader = new PdfReader(File.ReadAllBytes(caminho));
        using var output = new MemoryStream();

        var ex = Assert.ThrowsException<BadPasswordException>(
            () => _ = new PdfStamper(reader, output));

        Assert.AreEqual("PdfReader not opened with owner password", ex.Message);
    }
}
