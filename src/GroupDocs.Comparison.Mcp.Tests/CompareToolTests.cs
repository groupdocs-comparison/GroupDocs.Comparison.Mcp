using GroupDocs.Mcp.Core;
using GroupDocs.Mcp.Core.Licensing;
using GroupDocs.Comparison.Mcp.Tools;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GroupDocs.Comparison.Mcp.Tests;

public class CompareToolTests
{
    private readonly Mock<IFileResolver> _resolver = new();
    private readonly Mock<ILicenseManager> _licenseManager = new();
    private readonly Mock<IFileStorage> _storage = new();
    private readonly OutputHelper _output;

    public CompareToolTests()
    {
        _output = new OutputHelper(_storage.Object, Microsoft.Extensions.Options.Options.Create(new McpConfig()));
    }

    [Fact]
    public async Task Compare_WhenSourceResolverThrows_PropagatesException()
    {
        _resolver
            .Setup(r => r.ResolveAsync(It.IsAny<FileInput>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("missing-source.pdf"));

        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() =>
            CompareTool.Compare(
                _resolver.Object,
                _storage.Object,
                _licenseManager.Object,
                _output,
                new FileInput { FilePath = "missing-source.pdf" },
                new FileInput { FilePath = "target.pdf" }));

        Assert.Contains("missing-source.pdf", ex.Message);
    }

    [Fact]
    public async Task Compare_WhenSourceResolverThrows_DoesNotWriteToStorage()
    {
        _resolver
            .Setup(r => r.ResolveAsync(It.IsAny<FileInput>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("missing-source.pdf"));

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            CompareTool.Compare(
                _resolver.Object,
                _storage.Object,
                _licenseManager.Object,
                _output,
                new FileInput { FilePath = "missing-source.pdf" },
                new FileInput { FilePath = "target.pdf" }));

        _storage.Verify(
            s => s.WriteFileAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Compare_SetsLicense_BeforeResolving()
    {
        var sequence = new List<string>();

        _licenseManager
            .Setup(l => l.SetLicense())
            .Callback(() => sequence.Add("license"));

        _resolver
            .Setup(r => r.ResolveAsync(It.IsAny<FileInput>(), It.IsAny<CancellationToken>()))
            .Callback(() => sequence.Add("resolve"))
            .ThrowsAsync(new InvalidOperationException("short-circuit"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CompareTool.Compare(
                _resolver.Object,
                _storage.Object,
                _licenseManager.Object,
                _output,
                new FileInput { FilePath = "source.pdf" },
                new FileInput { FilePath = "target.pdf" }));

        Assert.Equal(new[] { "license", "resolve" }, sequence);
    }

    [Fact]
    public async Task Compare_PassesSourceFileInputToResolver_Unchanged()
    {
        var sourceInput = new FileInput { FilePath = "source.docx" };
        FileInput? capturedFirst = null;

        _resolver
            .Setup(r => r.ResolveAsync(It.IsAny<FileInput>(), It.IsAny<CancellationToken>()))
            .Callback<FileInput, CancellationToken>((fi, _) => { capturedFirst ??= fi; })
            .ThrowsAsync(new InvalidOperationException("short-circuit"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CompareTool.Compare(
                _resolver.Object,
                _storage.Object,
                _licenseManager.Object,
                _output,
                sourceInput,
                new FileInput { FilePath = "target.docx" }));

        // First Resolve call should be for the source file.
        Assert.Same(sourceInput, capturedFirst);
    }
}
