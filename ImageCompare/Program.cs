using Codeuctivity.ImageSharpCompare;
using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;
using Shipwreck.Phash;
using Shipwreck.Phash.Bitmaps;
using System.Drawing;

var projectDir = AppContext.BaseDirectory[..AppContext.BaseDirectory.IndexOf("bin")];
var img1 = Path.Combine(projectDir, "img1.png");
var img2 = Path.Combine(projectDir, "img2.webp");
var img11 = Path.Combine(projectDir, "img11.png");

#region ImageHash
//https://github.com/coenm/ImageHash
Console.WriteLine("============================== CoenM.ImageSharp.ImageHash ==============================");
var averageHash1 = GetAverageHash(img1);
var averageHash2 = GetAverageHash(img2);
var averageSimilarity = CompareHash.Similarity(averageHash1, averageHash2);
Console.WriteLine($"AverageHash Similarity: {averageSimilarity}");

var perceptualHash1 = GetPerceptualHash(img1);
var perceptualHash2 = GetPerceptualHash(img2);
var perceptualSimilarity = CompareHash.Similarity(perceptualHash1, perceptualHash2);
Console.WriteLine($"PerceptualHash Similarity: {perceptualSimilarity}");

var differenceHash1 = GetDifferenceHash(img1);
var differenceHash2 = GetDifferenceHash(img2);
var differenceSimilarity = CompareHash.Similarity(differenceHash1, differenceHash2);
Console.WriteLine($"DifferenceHash Similarity: {differenceSimilarity}");
#endregion

Console.WriteLine();
Console.WriteLine();

#region PHash
//https://github.com/pgrho/phash
Console.WriteLine("============================== Shipwreck.Phash ==============================");
var hash1 = ComputeHash(img1);
var hash2 = ComputeHash(img11);
var score = ImagePhash.GetCrossCorrelation(hash1, hash2);
Console.WriteLine($"Cross Correlation: {score * 100}");
#endregion

Console.WriteLine();
Console.WriteLine();

#region ImageSharp.Compare
//https://github.com/Codeuctivity/ImageSharp.Compare
Console.WriteLine("============================== ImageSharp.Compare ==============================");

var calc0 = Path.Combine(projectDir, "calc0.jpg");
var calc1 = Path.Combine(projectDir, "calc1.jpg");

var calcDiff = ImageSharpCompare.CalcDiff(calc0, calc1); //Must be same size

Console.WriteLine($"PixelErrorCount: {calcDiff.PixelErrorCount}");
Console.WriteLine($"PixelErrorPercentage: {calcDiff.PixelErrorPercentage}");
Console.WriteLine($"AbsoluteError: {calcDiff.AbsoluteError}");
Console.WriteLine($"MeanError: {calcDiff.MeanError}");

using var fileStreamDifferenceMask = File.Create(Path.Combine(projectDir, "cacl_difference_mask.png"));
using var maskImage = ImageSharpCompare.CalcDiffMaskImage(calc0, calc1);
await SixLabors.ImageSharp.ImageExtensions.SaveAsPngAsync(maskImage, fileStreamDifferenceMask);
#endregion

#region Methods
static ulong GetAverageHash(string filename)
{
    var hashAlgorithm = new AverageHash();
    using var stream = File.OpenRead(filename);
    return hashAlgorithm.Hash(stream);
}

static ulong GetDifferenceHash(string filename)
{
    var hashAlgorithm = new DifferenceHash();
    using var stream = File.OpenRead(filename);
    return hashAlgorithm.Hash(stream);
}

static ulong GetPerceptualHash(string filename)
{
    var hashAlgorithm = new PerceptualHash();
    using var stream = File.OpenRead(filename);
    return hashAlgorithm.Hash(stream);
}

static Digest ComputeHash(string filename)
{
    //Windows Only because of using System.Drawing.Common package
    var bitmap = (Bitmap)Image.FromFile(filename);
    return ImagePhash.ComputeDigest(bitmap.ToLuminanceImage());
}
#endregion
