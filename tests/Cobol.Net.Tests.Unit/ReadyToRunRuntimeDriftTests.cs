// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CobolNet.CodeGen;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE RUNTIME A COMPILED PROGRAM LOADS IS THE READYTORUN IMAGE FOR ITS PLATFORM, AND THE TESTS AND THE PACKAGE
/// LOAD THE SAME ONE (kb/Work PB2528, owner decision 2026-10-07; DESIGN-test-build-ci §3.16). Every COBOL program runs
/// in its own process with <c>Cobol.Net.Runtime.dll</c> beside it; precompiled, a program starts about a quarter
/// faster than with the JIT-compiled assembly (the measurement is in kb/Work PB2528). <c>src/Cobol.Net.Runtime</c> builds one Release image per platform of
/// <c>CobolRuntimeReadyToRunIdentifiers</c> (<c>Directory.Build.props</c>), every referencing project carries them at
/// <c>ReadyToRun/&lt;rid&gt;/</c>, and <c>AssemblyPackager.RuntimePath</c> picks this platform's. A broken build step,
/// a renamed folder or a platform-naming slip would not fail a single program: the program would run, correctly, on
/// the JIT-compiled assembly, and the gates would certify a runtime the package does not ship. Only these checks see
/// it. They read the images' PE headers, so they do not trust the build's own naming: a ReadyToRun image carries a
/// managed native header, and its machine field is the architecture combined with the operating system
/// (<c>IMAGE_FILE_MACHINE_NATIVE_OS_OVERRIDE</c> in the .NET runtime's <c>pedecoder.h</c>).
/// <para>An impact-recording build (tools/impact/ImpactRecording.targets) turns the images off, because its probes
/// are woven into the portable assembly; there the arms require the portable assembly instead.</para>
/// </summary>
public sealed class ReadyToRunRuntimeDriftTests
{
    private const string RuntimeFile = "Cobol.Net.Runtime.dll";

    /// <summary>The platforms the build precompiles the runtime for, read from the one place they are declared.</summary>
    private static IReadOnlyList<string> DeclaredPlatforms()
    {
        var props = XDocument.Load(TestRepo.At("Directory.Build.props"));
        string list = props.Descendants("CobolRuntimeReadyToRunIdentifiers").Single().Value;
        return [.. list.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
    }

    /// <summary>This process's platform, derived here independently of the compiler's naming.</summary>
    private static string HostPlatform() =>
        (OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : OperatingSystem.IsLinux() ? "linux" : "other")
        + "-" + RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();

    /// <summary>The PE machine field a ReadyToRun image for <paramref name="platform"/> carries.</summary>
    private static ushort ExpectedMachine(string platform)
    {
        string[] parts = platform.Split('-');
        ushort arch = parts[^1] switch
        {
            "x64" => 0x8664, "arm64" => 0xAA64, "x86" => 0x014C, "arm" => 0x01C4,
            _ => throw new InvalidOperationException($"no PE machine is known here for the architecture of {platform}"),
        };
        ushort os = parts[0] switch
        {
            "win" => 0, "linux" => 0x7B79, "osx" => 0x4644, "freebsd" => 0xADC4,
            _ => throw new InvalidOperationException($"no ReadyToRun operating-system code is known here for {platform}"),
        };
        return (ushort)(arch ^ os);
    }

    private static (bool ReadyToRun, ushort Machine) Header(string path)
    {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        var headers = pe.PEHeaders;
        Assert.NotNull(headers.CorHeader);
        bool r2r = headers.CorHeader!.ManagedNativeHeaderDirectory.Size > 0
                   && (headers.CorHeader.Flags & CorFlags.ILLibrary) != 0;
        return (r2r, (ushort)headers.CoffHeader.Machine);
    }

    /// <summary>Arm 1: the runtime the compiler compiles every program against and deploys beside it is this
    /// platform's ReadyToRun image, built from the sources alone (no source-control stamp, so a commit that leaves the
    /// runtime alone leaves the image, and the compiled-program cache keyed on it, unchanged).</summary>
    [Fact]
    public void TheDeployedRuntime_IsThisPlatformsReadyToRunImage()
    {
#if IMPACT_RECORDING
        Assert.Equal(Path.Combine(AppContext.BaseDirectory, RuntimeFile), AssemblyPackager.RuntimePath);
        Assert.False(Header(AssemblyPackager.RuntimePath).ReadyToRun,
            "an impact-recording build must deploy the probed portable runtime, not a ReadyToRun image");
#else
        string platform = HostPlatform();
        Assert.Equal(platform, AssemblyPackager.ReadyToRunPlatform);
        Assert.Equal(Path.Combine(AppContext.BaseDirectory, "ReadyToRun", platform, RuntimeFile), AssemblyPackager.RuntimePath);
        var (r2r, machine) = Header(AssemblyPackager.RuntimePath);
        Assert.True(r2r, $"{AssemblyPackager.RuntimePath} is not a ReadyToRun image");
        Assert.Equal(ExpectedMachine(platform), machine);
        string? version = FileVersionInfo.GetVersionInfo(AssemblyPackager.RuntimePath).ProductVersion;
        Assert.False(version?.Contains('+') ?? false,
            $"the runtime image carries a source-revision stamp ({version}): every commit would change it");
#endif
    }

    /// <summary>Arm 2: every declared platform has its image in this test host's output and in the CLI's, byte for
    /// byte the same file: the CLI's output is what its publish carries (the publish refuses to finish without every
    /// image, <c>Cobol.Net.Cli.csproj</c> <c>CobolAssertReadyToRunRuntimePublished</c>), and its publish is what the
    /// WiseOwl.COBOL tool package packs. The declared platforms cover every operating system a CI job runs on, so
    /// no CI leg tests a JIT-compiled runtime.</summary>
    [Fact]
    public void EveryDeclaredPlatform_HasOneImage_SharedByTheTestsAndThePackage()
    {
        var declared = DeclaredPlatforms();
#if IMPACT_RECORDING
        Assert.False(Directory.Exists(Path.Combine(AppContext.BaseDirectory, "ReadyToRun")),
            "an impact-recording build must build no ReadyToRun runtime images");
#else
        foreach (string platform in declared)
        {
            string tested = Path.Combine(AppContext.BaseDirectory, "ReadyToRun", platform, RuntimeFile);
            string shipped = TestRepo.SrcBin("Cobol.Net.Cli", "ReadyToRun", platform, RuntimeFile);
            Assert.True(File.Exists(tested), $"the test host has no ReadyToRun runtime image for {platform}: {tested}");
            Assert.True(File.Exists(shipped), $"the CLI has no ReadyToRun runtime image for {platform}: {shipped}");
            var (r2r, machine) = Header(tested);
            Assert.True(r2r, $"{tested} is not a ReadyToRun image");
            Assert.Equal(ExpectedMachine(platform), machine);
            Assert.True(File.ReadAllBytes(tested).AsSpan().SequenceEqual(File.ReadAllBytes(shipped)),
                $"the tests and the CLI carry different runtime images for {platform}");
        }
#endif
        Assert.Contains(HostPlatform(), declared);
        foreach (string platform in CiPlatforms())
            Assert.Contains(platform, declared);
    }

    /// <summary>The platform of every <c>runs-on:</c> runner in the CI workflows. A runner this cannot name fails the
    /// test, so a new kind of CI machine is mapped here, and declared, before it runs a leg.</summary>
    private static IEnumerable<string> CiPlatforms()
    {
        var runsOn = new Regex(@"^\s*runs-on:\s*(?<label>\S+)\s*$", RegexOptions.Multiline);
        var labels = Directory.EnumerateFiles(TestRepo.At(".github", "workflows"), "*.yml")
            .SelectMany(f => runsOn.Matches(File.ReadAllText(f)).Select(m => m.Groups["label"].Value))
            .Distinct().ToList();
        Assert.NotEmpty(labels);
        foreach (string label in labels)
        {
            string arch = label.Contains("arm", StringComparison.Ordinal) ? "arm64" : "x64";
            yield return label switch
            {
                _ when label.StartsWith("ubuntu-", StringComparison.Ordinal) => "linux-" + arch,
                _ when label.StartsWith("windows-", StringComparison.Ordinal) => "win-" + arch,
                _ when label.StartsWith("macos-", StringComparison.Ordinal) => "osx-arm64",
                _ => throw new InvalidOperationException(
                    $"CI runner '{label}' names no platform here: map it, and declare its CobolRuntimeReadyToRunIdentifiers entry"),
            };
        }
    }
}
