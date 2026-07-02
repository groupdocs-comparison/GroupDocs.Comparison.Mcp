using GroupDocs.Mcp.Core;
using GroupDocs.Mcp.Core.Licensing;
using GroupDocs.Comparison.Mcp.Tools;
using Moq;
using Xunit;

namespace GroupDocs.Comparison.Mcp.Tests;

public class AnalyzeChangesToolTests
{
    private readonly Mock<IFileResolver> _resolver = new();
    private readonly Mock<ILicenseManager> _licenseManager = new();

    [Fact]
    public async Task AnalyzeChanges_WhenSourceResolverThrows_PropagatesException()
    {
        _resolver
            .Setup(r => r.ResolveAsync(It.IsAny<FileInput>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("missing-source.pdf"));

        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() =>
            AnalyzeChangesTool.AnalyzeChanges(
                _resolver.Object,
                _licenseManager.Object,
                new FileInput { FilePath = "missing-source.pdf" },
                new FileInput { FilePath = "target.pdf" }));

        Assert.Contains("missing-source.pdf", ex.Message);
    }

    [Fact]
    public async Task AnalyzeChanges_SetsLicense_BeforeResolving()
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
            AnalyzeChangesTool.AnalyzeChanges(
                _resolver.Object,
                _licenseManager.Object,
                new FileInput { FilePath = "source.pdf" },
                new FileInput { FilePath = "target.pdf" }));

        Assert.Equal(new[] { "license", "resolve" }, sequence);
    }

    [Fact]
    public async Task AnalyzeChanges_ResolvesSourceBeforeTarget()
    {
        var sourceInput = new FileInput { FilePath = "source.docx" };
        FileInput? capturedFirst = null;

        _resolver
            .Setup(r => r.ResolveAsync(It.IsAny<FileInput>(), It.IsAny<CancellationToken>()))
            .Callback<FileInput, CancellationToken>((fi, _) => { capturedFirst ??= fi; })
            .ThrowsAsync(new InvalidOperationException("short-circuit"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AnalyzeChangesTool.AnalyzeChanges(
                _resolver.Object,
                _licenseManager.Object,
                sourceInput,
                new FileInput { FilePath = "target.docx" }));

        // First Resolve call should be for the source file.
        Assert.Same(sourceInput, capturedFirst);
    }
}
