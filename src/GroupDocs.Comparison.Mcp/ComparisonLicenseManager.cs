using GroupDocs.Mcp.Core;
using GroupDocs.Mcp.Core.Licensing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GroupDocs.Comparison.Mcp;

public class ComparisonLicenseManager : LicenseManager
{
    public ComparisonLicenseManager(IOptions<McpConfig> config, ILogger<LicenseManager> logger) : base(config, logger) { }

    // Identifies the engine in get_license_status. Without this the tool would report the
    // server's own version, because this class lives in the server assembly.
    protected override Type? EngineMarkerType => typeof(GroupDocs.Comparison.Comparer);

    protected override void SetLicenseFromPath(string licensePath)
    {
        new GroupDocs.Comparison.License().SetLicense(licensePath);
    }

    protected override void SetMeteredKeyCore(string publicKey, string privateKey)
    {
        new GroupDocs.Comparison.Metered().SetMeteredKey(publicKey, privateKey);
    }

    protected override MeteredConsumption ReadConsumptionCore()
    {
        // Static on the engine, and only meaningful once a metered key is applied —
        // Core guarantees this is called in metered mode only.
        return new MeteredConsumption
        {
            Quantity = GroupDocs.Comparison.Metered.GetConsumptionQuantity(),
            Credit = GroupDocs.Comparison.Metered.GetConsumptionCredit()
        };
    }
}
