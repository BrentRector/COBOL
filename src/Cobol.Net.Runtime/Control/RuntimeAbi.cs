// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The two versions a compiled COBOL module and the runtime it runs on must agree on
/// (docs/rearchitecture/DESIGN-external-repository.md §15.4; kb/Work PB2097). Each side of the activation boundary is
/// guarded by its own recorded version:
/// <list type="bullet">
///   <item><see cref="Version"/> — the RUNTIME side: this assembly's <c>AssemblyVersion</c> (the package version,
///     <c>Directory.Build.props</c>). Its MAJOR changes exactly when the public surface (<c>PublicAPI.Shipped.txt</c>)
///     or a codec's image layout changes incompatibly.</item>
///   <item><see cref="CallAbi"/> — the COMPILER side: the boundary layout the runtime adopts — each boundary item's
///     crossing arm and carrier type, the profile and width a unit registers for it, and the BY VALUE landing. A
///     compiler-only change there keeps the runtime's major and still makes a new caller and an old callee disagree,
///     so it is its own integer.</item>
/// </list>
/// Every module records both, as compiled against, in its <c>[assembly: CobolRepository(…)]</c> and in its
/// <c>__CobolModule.EnsureRegistered()</c>; <see cref="ProgramTable.RegisterModule"/> refuses a module whose major or
/// call ABI differs (<see cref="Skew"/>), however the module is reached. <c>RuntimeAbiPinDriftTests</c> pins both
/// beside the hash of the public surface and of the boundary-layout fixture, so neither half changes without its bump.
/// </summary>
public static class RuntimeAbi
{
    /// <summary>The runtime's own version: this assembly's <c>AssemblyVersion</c>.</summary>
    public static Version Version { get; } = typeof(RuntimeAbi).Assembly.GetName().Version
        ?? throw new InvalidOperationException("the WiseOwl COBOL runtime assembly carries no AssemblyVersion");

    /// <summary>The boundary layout this runtime adopts (§15.4). Bumped whenever a compiler-side crossing decision
    /// changes: <c>CallEmitter.CrossingOf</c>, <c>ProgramEmitter.FormalCrossing</c> / <c>FormalCarrierType</c>, the
    /// profile and width <c>CallEmitter.RegisteredFormal</c> / <c>RegisteredReturning</c> register, or the BY VALUE
    /// landing's arguments.</summary>
    public const int CallAbi = 1;

    /// <summary>Why a module compiled against <paramref name="runtimeVersion"/> and <paramref name="callAbi"/> cannot
    /// run on this runtime, or null when it can: the runtime MAJOR must equal this runtime's (a different major is a
    /// changed public surface or image layout, met later as a <c>MissingMethodException</c> inside the callee or as
    /// argument images the two sides lay out differently) and the call ABI must equal <see cref="CallAbi"/>. A minor,
    /// build or revision difference is compatible by the .NET versioning rule.</summary>
    public static string? Skew(string runtimeVersion, int callAbi)
    {
        if (!System.Version.TryParse(runtimeVersion, out var compiled))
            return $"it records the runtime version '{runtimeVersion}', which is not a version";
        if (compiled.Major != Version.Major)
            return $"it was compiled against WiseOwl COBOL runtime {compiled}, and this run unit runs runtime {Version} (a different major version)";
        if (callAbi != CallAbi)
            return $"it was compiled for call ABI {callAbi}, and this runtime adopts call ABI {CallAbi}";
        return null;
    }
}
