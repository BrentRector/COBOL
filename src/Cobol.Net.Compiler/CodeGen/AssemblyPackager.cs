// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.InteropServices;

namespace CobolNet.CodeGen;

/// <summary>
/// Packages a successfully-emitted assembly into a runnable deployment (P7 Step 2 — the side-effecting
/// packaging split out of <see cref="RoslynBackend"/>, whose <c>Compile</c> stays pure C#→assembly): writes the
/// <c>.runtimeconfig.json</c> so the output launches via <c>dotnet &lt;name&gt;.dll</c>, and copies
/// <c>Cobol.Net.Runtime.dll</c> next to it so the generated program's runtime calls resolve.
/// </summary>
public static class AssemblyPackager
{
    internal const string RuntimeFileName = "Cobol.Net.Runtime.dll";

    /// <summary>The ReadyToRun platform this process runs on, named as the build names it (<c>win-x64</c>,
    /// <c>linux-x64</c>, <c>osx-arm64</c>): operating-system family and processor architecture, which is exactly what
    /// decides whether a ReadyToRun image's native code is usable here (its PE machine field is the architecture
    /// combined with the operating system). It is not <see cref="RuntimeInformation.RuntimeIdentifier"/>, which a
    /// distribution-built .NET reports as e.g. <c>ubuntu.26.04-x64</c>, naming no image.</summary>
    internal static string ReadyToRunPlatform { get; } =
        (OperatingSystem.IsWindows() ? "win"
            : OperatingSystem.IsMacOS() ? "osx"
            : OperatingSystem.IsLinux() ? "linux"
            : OperatingSystem.IsFreeBSD() ? "freebsd"
            : null) is { } os
            ? os + "-" + RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()
            : RuntimeInformation.RuntimeIdentifier;   // an operating system the build cannot target names no image

    /// <summary>The WiseOwl COBOL runtime assembly the generated program calls (deployed alongside the compiler).
    /// Consumed by BOTH halves of the backend: <see cref="RoslynBackend"/> references it at compile time and
    /// <see cref="Package"/> deploys it at packaging time, so a program is compiled against the very file it runs
    /// with.
    /// <para>It is this platform's READYTORUN IMAGE (kb/Work PB2528; DESIGN-test-build-ci §3.16): the build
    /// precompiles the runtime for each platform in <c>CobolRuntimeReadyToRunIdentifiers</c> and lays each beside
    /// the compiler at <c>ReadyToRun/&lt;rid&gt;/</c>, because a COBOL program is its own process and would otherwise
    /// JIT-compile the runtime's methods at every start. On a platform the build precompiled nothing for, it is the
    /// portable assembly beside the compiler, the same source compiled by the JIT at run time. That is also what an
    /// impact-recording build deploys: it turns the images off so that programs load the probed assembly
    /// (tools/impact/ImpactRecording.targets).</para></summary>
    /// <remarks>Public because the Conformance runner's compiled-program cache keys on this file. Declared after
    /// <see cref="ReadyToRunPlatform"/>, which its initializer reads: static initializers run in declaration
    /// order.</remarks>
    public static string RuntimePath { get; } = ResolveRuntimePath();

    private static string ResolveRuntimePath()
    {
        string image = Path.Combine(AppContext.BaseDirectory, "ReadyToRun", ReadyToRunPlatform, RuntimeFileName);
        return File.Exists(image)   // not a compilation input: the images are part of the compiler's own deployment
            ? image
            : Path.Combine(AppContext.BaseDirectory, RuntimeFileName);
    }

    /// <summary>Package the emitted assembly at <paramref name="outputDllPath"/>: runtimeconfig + runtime deploy.
    /// (The design sketch passed the Roslyn <c>EmitResult</c> + options; the output path is the one input the
    /// packaging actually consumes — reduced accordingly.)</summary>
    /// <returns>Every file packaging wrote, as full paths (kb/Work PB985 — the backend's output set).</returns>
    internal static IReadOnlyList<string> Package(string outputDllPath)
    {
        var written = new List<string> { WriteRuntimeConfig(outputDllPath) };
        if (DeployRuntime(outputDllPath) is { } runtime) written.Add(runtime);
        return written;
    }

    /// <summary>Copy <see cref="RuntimePath"/> next to the compiled program, as <c>Cobol.Net.Runtime.dll</c>, so it
    /// resolves at run time.</summary>
    private static string? DeployRuntime(string outputDllPath)
    {
        string dest = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(outputDllPath))!, RuntimeFileName);
        if (string.Equals(RuntimePath, dest, StringComparison.OrdinalIgnoreCase)   // not a COBOL word: two file-system paths
            || !File.Exists(RuntimePath))   // not a compilation input: the runtime is part of the compiler's own deployment
            return null;
        File.Copy(RuntimePath, dest, overwrite: true);
        return dest;
    }

    /// <summary>
    /// Write the <c>.runtimeconfig.json</c> next to the emitted assembly so it is launchable via
    /// <c>dotnet &lt;name&gt;.dll</c>, targeting the same shared framework the compiler is running on.
    /// </summary>
    private static string WriteRuntimeConfig(string outputDllPath)
    {
        var v = Environment.Version;
        string json = $$"""
        {
          "runtimeOptions": {
            "tfm": "net{{v.Major}}.{{v.Minor}}",
            "framework": {
              "name": "Microsoft.NETCore.App",
              "version": "{{v.Major}}.{{v.Minor}}.0"
            }
          }
        }
        """;
        string path = Path.GetFullPath(Path.ChangeExtension(outputDllPath, ".runtimeconfig.json"));
        File.WriteAllText(path, json);
        return path;
    }
}
