using System.Reflection;
using System.Runtime.InteropServices;

namespace GroupDocs.Comparison.Mcp;

/// Maps the Windows-style <c>gdiplus.dll</c> P/Invoke to the platform's libgdiplus
/// on Linux/macOS.
///
/// GroupDocs.Comparison renders the marked-up result and walks page content through
/// System.Drawing.Common, whose GDI+ interop P/Invokes <c>gdiplus.dll</c>. On Windows
/// that's built-in. On Linux/macOS there is no <c>gdiplus.dll</c>: the system library
/// is <c>libgdiplus.so</c> / <c>libgdiplus.dylib</c>, and the .NET native-library
/// loader does NOT map <c>gdiplus.dll</c> to it (it only probes gdiplus.dll /
/// libgdiplus.dll / gdiplus.dll.so|dylib / libgdiplus.dll.so|dylib). So `compare` and
/// `analyze_changes` fail on non-Windows — including inside the Docker image — even
/// when libgdiplus IS installed, with:
///
///   DllNotFoundException: Unable to load shared library 'gdiplus.dll'
///     ← System.Drawing.Gdip ← Windows.Win32.Graphics.GdiPlus.GdiPlusInitialization
///
/// This resolver intercepts that load and points it at the real libgdiplus. Windows
/// keeps its built-in GDI+ (the resolver is a no-op there).
///
/// libgdiplus must still be present: Docker installs it; native (dnx / global tool)
/// users install it per the README (apt <c>libgdiplus</c> / brew <c>mono-libgdiplus</c>).
internal static class GdiPlusResolver
{
    /// Registers the resolver on System.Drawing.Common. Call once, before any tool
    /// can trigger a GDI+ P/Invoke (i.e. at host startup).
    public static void Register()
    {
        if (OperatingSystem.IsWindows())
            return; // built-in gdiplus.dll — nothing to remap

        Assembly systemDrawing;
        try
        {
            // The gdiplus.dll DllImport is declared in System.Drawing.Common, so the
            // resolver must be attached to THAT assembly (resolvers are per-declaring-assembly).
            systemDrawing = Assembly.Load("System.Drawing.Common");
        }
        catch
        {
            return; // not present — leave default behavior
        }

        try
        {
            NativeLibrary.SetDllImportResolver(systemDrawing, Resolve);
        }
        catch (InvalidOperationException)
        {
            // A resolver is already registered for this assembly — don't clobber it.
        }
    }

    private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!libraryName.StartsWith("gdiplus", StringComparison.OrdinalIgnoreCase))
            return IntPtr.Zero; // not GDI+ — let the default loader handle it

        foreach (var candidate in Candidates)
            if (NativeLibrary.TryLoad(candidate, out var handle))
                return handle;

        return IntPtr.Zero; // fall back to default probing (surfaces the original error)
    }

    private static string[] Candidates => OperatingSystem.IsMacOS()
        ? new[]
        {
            "libgdiplus.dylib",                                          // dyld search path
            "/opt/homebrew/lib/libgdiplus.dylib",                        // Apple Silicon brew (symlinked)
            "/opt/homebrew/opt/mono-libgdiplus/lib/libgdiplus.dylib",    // Apple Silicon brew (keg)
            "/usr/local/lib/libgdiplus.dylib",                           // Intel brew (symlinked)
            "/usr/local/opt/mono-libgdiplus/lib/libgdiplus.dylib",       // Intel brew (keg)
        }
        : new[]
        {
            "libgdiplus.so",
            "libgdiplus.so.0",
            "/usr/lib/x86_64-linux-gnu/libgdiplus.so",                   // Debian/Ubuntu apt
            "/usr/lib/libgdiplus.so",
            "/usr/lib64/libgdiplus.so",                                  // RHEL/Fedora
        };
}
