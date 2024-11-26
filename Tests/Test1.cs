using Microsoft.Playwright;
using System.Diagnostics.CodeAnalysis;

//[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]
namespace Tests;

[TestClass]
[UsesVerify]
public partial class TestClass1 //: VerifyBase
{
    [Ignore]
    [TestMethod]
    public async Task ImageComparison()
    {
        const string filename = "sample.1.png"; //Phash Only works with .png files

        await VerifyFile(filename);
        //await Verify(File.OpenRead(filename));
        //await Verify(File.OpenRead(filename), "jpg");
        //await Verify(File.ReadAllBytes(filename), "jpg");

        #region ImageHash
        //await VerifyFile(filename)
        //    //new PerceptualHash() //new AverageHash() //new DifferenceHash()
        //    .UseImageHash(100, algorithm: new CoenM.ImageHash.HashAlgorithms.PerceptualHash());

        //await ThrowsTask(async () =>
        //{
        //    await VerifyFile(filename).UseMethodName("FailInner"); //must be unique (different from the method name)
        //})
        ////.DisableDiff() //Do not launch vs code for text diff
        //.IgnoreStackTrace()
        //.ScrubLinesContaining("clipboard", "DiffEngineTray"); //Removes any lines that contains that string
        #endregion

        #region Phash
        //await VerifyFile(filename)
        //    .PhashCompareSettings(
        //        threshold: .8f,
        //        sigma: 4f,
        //        gamma: 2f,
        //        angles: 170);
        #endregion

        #region ImageSharp.Compare
        //await VerifyFile(filename).UseImageHash(5); //Images must be the same size
        #endregion

        #region ImageSharp
        //await VerifyFile(filename).DisableDiff().EncodeAsJpeg();
        #endregion

        #region ImageMagick
        //await VerifyFile(filename)
        //.ImageMagickComparer(0.001, ImageMagick.ErrorMetric.PerceptualHash)
        //.ImageMagickBackground(ImageMagick.MagickColor.FromRgb(255, 255, 255))
        //;
        #endregion
    }

    [TestMethod]
    public async Task CompareComplexObject()
    {
        //Default: Compare JSON texts (sensitive about property position!)
        //Verify.Quibble: Compare JSON property values (NOT sensitive about property position)

        var complexObject = new
        {
            Name = "Foo",
            Age = 25,
            Address = new
            {
                Street = "123 Main St",
                City = "Redmond",
                State = "WA",
                Zip = 98052
            }
        };

        await Verify(complexObject);
    }


    static IPlaywright PlaywrightInstance = null!;
    static IBrowser Browser = null!;
    IPage Page = null!;

    //[TestInitialize]
    public async Task InitializePlaywright()
    {
        // wait for target server to start
        //await SocketWaiter.Wait(port: 5000);

        PlaywrightInstance ??= await Playwright.CreateAsync();
        Browser ??= await PlaywrightInstance.Chromium.LaunchAsync(new() { /*Headless = false*/ });
        Page = await Browser.NewPageAsync();
        await Page.GotoAsync("http://localhost:5000");
        await Assertions.Expect(Page).ToHaveTitleAsync("Home");
    }

    [TestMethod]
    public async Task Playwright_Page()
    {
        await InitializePlaywright();

        //Page test
        await Verify(Page); //.DisableDiff()

        //await Verify(Page)
        //    .PageScreenshotOptions(new() { Quality = 50, Type = ScreenshotType.Jpeg }); //.DisableDiff()
    }

    [TestMethod]
    public async Task Playwright_LocatorElement()
    {
        await InitializePlaywright();

        var element = Page.Locator("#app > div > div > div.collapse.nav-scrollable");

        //Element test using ILocator
        await Verify(element);

        //await Verify(element)
        //    .LocatorScreenshotOptions(new() { Quality = 50, Type = ScreenshotType.Jpeg });
    }

    [Ignore]
    [TestMethod]
    public async Task Playwright_SelectorElement()
    {
        await InitializePlaywright();

        var element = await Page.QuerySelectorAsync("#app > div > div > div.collapse.nav-scrollable");

        //Element test
        await Verify(element);

        //await Verify(element)
        //    .ElementScreenshotOptions(new() { Quality = 50, Type = ScreenshotType.Jpeg });
    }
}

public static class ModuleInitializer
{
    public static readonly string SnapshotDir = Path.Combine(AppContext.BaseDirectory[..AppContext.BaseDirectory.IndexOf("bin")], "_snapshots");

    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void Init()
    {
        //Compare similarity
        //https://github.com/VerifyTests/Verify.Phash //NOT Good (only windows)
        //VerifyPhash.Initialize();

        //Compare similarity
        //https://github.com/VerifyTests/Verify.ImageHash //Good
        VerifyImageHash.Initialize();

        //Compare similarity
        //https://github.com/VerifyTests/Verify.ImageSharp.Compare (so so)
        //VerifyImageSharpCompare.Initialize(); //Images must be the same size

        //Compare pixel by pixel and similarity (not good but better in pixel by pixel)
        //https://github.com/VerifyTests/Verify.ImageMagick
        //VerifyImageMagick.Initialize();

        //Compare pixel by pixel
        //https://github.com/VerifyTests/Verify.ImageSharp //NOT Good
        //VerifyImageSharp.Initialize();

        //Compares rendered html and screenshot pixel by pixel (maybe there is a way to disable html comparison) (fails on CI/CD, needs to be run on the same OS or use a custom comparer)
        //https://github.com/VerifyTests/Verify.HeadlessBrowsers
        VerifyPlaywright.Initialize(installPlaywright: true);
        //OS specific rendering
        //The rendering can very slightly between different OS versions.This can make verification on different machines(eg CI) problematic.
        //A [custom](https://github.com/VerifyTests/Verify/blob/main/docs/comparer.md) comparer can to mitigate this.

        //Load all plugins (Verify.*.dll)
        //VerifierSettings.InitializePlugins();

        //Compares JSON property values (NOT sensitive to property positions)
        //https://github.com/VerifyTests/Verify.Quibble
        //VerifierSettings.UseStrictJson();
        //VerifyQuibble.Initialize();

        DerivePathInfo((_, _, type, method) => new PathInfo(SnapshotDir, type.Name, method.Name));
    }
}

public static class VerifyPlaywright
{
    public static bool Initialized { get; private set; }

    public static void Initialize(bool installPlaywright = false)
    {
        if (Initialized)
            throw new("Already Initialized");

        Initialized = true;

        InnerVerifier.ThrowIfVerifyHasBeenRun();

        if (installPlaywright)
            Program.Main(["install"]);

        VerifierSettings.RegisterFileConverter<IPage>(PageToImage);
        VerifierSettings.RegisterFileConverter<IElementHandle>(ElementToImage);
        VerifierSettings.RegisterFileConverter<ILocator>(LocatorToImage);
    }

    #region Converters
    static async Task<ConversionResult> PageToImage(IPage page, IReadOnlyDictionary<string, object> context)
    {
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        byte[] bytes;
        var imageType = "png";
        if (context.GetPageScreenshotOptions(out var options))
        {
            bytes = await page.ScreenshotAsync(options);
            if (options.Type == ScreenshotType.Jpeg)
                imageType = "jpg";
        }
        else
        {
            bytes = await page.ScreenshotAsync(new() { FullPage = true, Type = ScreenshotType.Png });
        }

        return new(
            null,
            [new(imageType, new MemoryStream(bytes))]
        );
    }

    static async Task<ConversionResult> ElementToImage(IElementHandle element, IReadOnlyDictionary<string, object> context)
    {
        byte[] bytes;
        var imageType = "png";
        if (context.GetElementScreenshotOptions(out var options))
        {
            bytes = await element.ScreenshotAsync(options);
            if (options.Type == ScreenshotType.Jpeg)
                imageType = "jpg";
        }
        else
        {
            bytes = await element.ScreenshotAsync(new() { Type = ScreenshotType.Png });
        }

        return new(
            null,
            [new(imageType, new MemoryStream(bytes))]
        );
    }

    static async Task<ConversionResult> LocatorToImage(ILocator locator, IReadOnlyDictionary<string, object> context)
    {
        byte[] bytes;
        var imageType = "png";
        if (context.GetLocatorScreenshotOptions(out var options))
        {
            bytes = await locator.ScreenshotAsync(options);
            if (options.Type == ScreenshotType.Jpeg)
                imageType = "jpg";
        }
        else
        {
            bytes = await locator.ScreenshotAsync(new() { Type = ScreenshotType.Png });
        }

        return new(
            null,
            [new(imageType, new MemoryStream(bytes))]);
    }
    #endregion

    #region Set Options
    public static void PageScreenshotOptions(this VerifySettings settings, PageScreenshotOptions options) =>
        settings.Context["Playwright.PageScreenshotOptions"] = options;

    public static SettingsTask PageScreenshotOptions(this SettingsTask settings, PageScreenshotOptions options)
    {
        settings.CurrentSettings.PageScreenshotOptions(options);
        return settings;
    }

    public static void ElementScreenshotOptions(this VerifySettings settings, ElementHandleScreenshotOptions options) =>
        settings.Context["Playwright.ElementScreenshotOptions"] = options;

    public static SettingsTask ElementScreenshotOptions(this SettingsTask settings, ElementHandleScreenshotOptions options)
    {
        settings.CurrentSettings.ElementScreenshotOptions(options);
        return settings;
    }

    public static void LocatorScreenshotOptions(this VerifySettings settings, LocatorScreenshotOptions options) =>
        settings.Context["Playwright.LocatorScreenshotOptions"] = options;

    public static SettingsTask LocatorScreenshotOptions(this SettingsTask settings, LocatorScreenshotOptions options)
    {
        settings.CurrentSettings.LocatorScreenshotOptions(options);
        return settings;
    }
    #endregion

    #region Get Options
    private static bool GetPageScreenshotOptions(this IReadOnlyDictionary<string, object> context, [NotNullWhen(true)] out PageScreenshotOptions? options)
    {
        if (context.TryGetValue("Playwright.PageScreenshotOptions", out var value))
        {
            options = (PageScreenshotOptions)value;
            ValidateNoPath(options.Path);
            return true;
        }

        options = null;
        return false;
    }

    private static bool GetElementScreenshotOptions(this IReadOnlyDictionary<string, object> context, [NotNullWhen(true)] out ElementHandleScreenshotOptions? options)
    {
        if (context.TryGetValue("Playwright.ElementScreenshotOptions", out var value))
        {
            options = (ElementHandleScreenshotOptions)value;
            ValidateNoPath(options.Path);
            return true;
        }

        options = null;
        return false;
    }

    private static bool GetLocatorScreenshotOptions(this IReadOnlyDictionary<string, object> context, [NotNullWhen(true)] out LocatorScreenshotOptions? options)
    {
        if (context.TryGetValue("Playwright.LocatorScreenshotOptions", out var value))
        {
            options = (LocatorScreenshotOptions)value;
            ValidateNoPath(options.Path);
            return true;
        }

        options = null;
        return false;
    }
    #endregion

    private static void ValidateNoPath(string? path)
    {
        if (path != null)
        {
            throw new("ScreenshotOptions Path not supported.");
        }
    }
}