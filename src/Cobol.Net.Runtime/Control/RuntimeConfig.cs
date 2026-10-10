// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Runtime.Collation;
using CobolNet.Runtime.Collation.Cache;
using CobolNet.Runtime.Collation.Cldr;
using CobolNet.Runtime.IO;

namespace CobolNet.Runtime;

/// <summary>
/// One environment variable the runtime reads: its name (or, for a FAMILY, its pattern), what it configures, the
/// values it accepts, and the source that declares the constant and reads it. Immutable; produced by
/// <see cref="RuntimeConfig"/>. <see cref="CurrentValue"/> is the only thing here that touches the process, and it
/// only reads.
/// </summary>
/// <param name="Name">The variable name (<c>COBOL_USER_LOCALE</c>), or the family pattern (<c>COBOL_&lt;SWITCH-NAME&gt;</c>) when <see cref="IsPattern"/>.</param>
/// <param name="Subsystem">Which subsystem reads it ("locale", "collation", "switches", "clock").</param>
/// <param name="Purpose">What it configures, in one sentence.</param>
/// <param name="Values">The values it accepts, and what an unset variable means.</param>
/// <param name="DeclaringType">The type that declares the name constant (and reads the variable).</param>
/// <param name="DeclaredIn">The declaring source file, relative to <c>src/Cobol.Net.Runtime/</c> — the drift test looks there for the constant and the read.</param>
/// <param name="IsPattern">True for a family of variables whose names are computed at run time; the entry then names the
/// family's members through <see cref="FamilyMember"/>.</param>
public sealed record ConfigEntry(string Name, string Subsystem, string Purpose, string Values, Type DeclaringType, string DeclaredIn, bool IsPattern = false)
{
    /// <summary>For a family entry (<see cref="IsPattern"/>), the test that says whether a variable name is one the runtime
    /// reads (<c>SwitchStore.IsVariableName</c>); a family that declares none matches no name. Null for an exact entry.</summary>
    public Func<string, bool>? FamilyMember { get; init; }

    /// <summary>The variable's current value in this process (null when unset, or for a pattern entry).</summary>
    public string? CurrentValue => IsPattern ? null : Environment.GetEnvironmentVariable(Name);

    /// <summary>True when the variable is set to a non-blank value.</summary>
    public bool IsSet => !string.IsNullOrWhiteSpace(CurrentValue);

    /// <summary>Does <paramref name="variableName"/> belong to this entry — the exact name, or (for a pattern) a
    /// name the family can produce?</summary>
    public bool Matches(string variableName) => IsPattern
        ? FamilyMember?.Invoke(variableName) ?? false
        : string.Equals(Name, variableName, StringComparison.Ordinal);

    public override string ToString() => $"{Name} ({Subsystem}): {Purpose}";
}

/// <summary>
/// The REGISTRY of every environment variable the WiseOwl COBOL runtime reads (kb/Work PB108) — a diagnostic
/// enumeration, NOT a configuration system: it introduces no knob, loads nothing, overrides nothing, and mutates no
/// runtime state. Each subsystem keeps reading its own variable through its own constant, exactly as before; this
/// class lists those constants in ONE place so that "which environment variables does a WiseOwl COBOL program honor"
/// has one answer (<see cref="All"/>, <see cref="Describe"/>), and so that a NEW variable cannot appear in the
/// runtime unregistered: <c>RuntimeConfigTests</c> scans the runtime sources for every
/// <c>Environment.GetEnvironmentVariable(</c> call and every <c>COBOL…_</c> name literal and asserts each is
/// represented here — and, in the other direction, that every entry here still has a live read (no stale entry).
/// <para>Every entry references the DECLARING constant (<see cref="LocaleState.UserDefaultVariable"/>,
/// <see cref="CacheConfig.SizeVariable"/>, …), so renaming a variable without updating the registry does not
/// compile. The one family whose names are computed — the external switches' <c>COBOL_&lt;SWITCH-NAME&gt;</c>
/// (<see cref="SwitchStore.VariableNameFor"/>) — is a PATTERN entry.</para>
/// <para>The runtime is dependency-free and stays so: there is no appsettings file, no configuration framework
/// and no second source of these values; the environment is the facility ISO/IEC 1989:2023 leaves to the
/// implementor for external switches (§12.3.7, implementor-defined item 191) and the locale defaults
/// (owner decision Q2), and the rest are the collation subsystem's site/diagnostic knobs.</para>
/// </summary>
public static class RuntimeConfig
{
    /// <summary>Every variable the runtime reads, in subsystem order.</summary>
    public static IReadOnlyList<ConfigEntry> All { get; } =
    [
        // ── locale (Control/LocaleState.cs — the L2 defaults a run unit determines ONCE at activation) ──
        new(LocaleState.UserDefaultVariable, "locale",
            "the run unit's USER-DEFAULT locale (LC_ALL USER-DEFAULT; the ambient locale of every locale-based collation, the IS LOCALE phrase) — owner decision Q2",
            "a BCP 47 / CLDR locale tag (\"es-ES\", \"de_AT\", \"zh-Hant\"; a -u- extension such as \"de-u-co-phonebk\" is honored); blank = the root order; unset = the process culture",
            typeof(LocaleState), "Control/LocaleState.cs"),
        new(LocaleState.SystemDefaultVariable, "locale",
            "the run unit's SYSTEM-DEFAULT locale (LC_ALL SYSTEM-DEFAULT)",
            "as COBOL_USER_LOCALE; unset = the installed UI culture",
            typeof(LocaleState), "Control/LocaleState.cs"),

        // ── collation (Collation/ — site data directories, the key cache, the eager warm-up) ──
        new(TailoringRules.EnvDirectory, "collation",
            "a directory of site collation tailoring files (<tag>.tailor), searched before Collation/ and Collation/Locales/ beside the application and the embedded tailorings",
            "a directory path; unset = no site directory",
            typeof(TailoringRules), "Collation/TailoringRules.cs"),
        new(CldrLocaleLoader.EnvDirectory, "collation",
            "a directory of site CLDR collation files (<locale>.xml LDML or the documented <locale>.json mirror), searched before Collation/CLDR/ beside the application and the embedded CLDR pack",
            "a directory path; unset = no site directory",
            typeof(CldrLocaleLoader), "Collation/CLDR/CldrLocaleLoader.cs"),
        new(CacheConfig.SizeVariable, "collation",
            "the collation key cache per collator: disabled, or its maximum number of keys",
            "off | 0 | false = disabled; a positive integer = max entries; unset or anything else = the default (8192)",
            typeof(CacheConfig), "Collation/Cache/CacheConfig.cs"),
        new(CacheConfig.EvictionVariable, "collation",
            "the key cache's eviction strategy",
            "lru (default) = least recently used; fifo | size = oldest first; unset or anything else = the default",
            typeof(CacheConfig), "Collation/Cache/CacheConfig.cs"),
        new(CollationRuntime.WarmupVariable, "collation",
            "decode the collation, grapheme and CLDR tables and resolve the default locale EAGERLY when the first run unit is created (CollationRuntime.Warmup) instead of lazily on first use",
            "any value except 0 | false | off | no = warm up; unset = lazy",
            typeof(CollationRuntime), "Collation/CollationRuntime.cs"),

        // ── clock (IO/Clock.cs — the run unit's clock seam) ──
        new(SystemClock.PinVariable, "clock",
            "pin the run unit's clock (every ACCEPT temporal source and now-intrinsic) for a deterministic run across processes — the temporal conformance goldens' path",
            "an invariant-culture date-time, optionally with a UTC offset (\"2026-06-10T14:30:45.67\", \"…+02:30\"); unset = the system clock",
            typeof(SystemClock), "IO/Clock.cs"),

        // ── file I-O (IO/FileRegistry.cs — the §14.9.27.4 GR10 / A.1 item 129 key-check opt-out) ──
        new(FileRegistry.KeyCheckVariable, "files",
            "whether an INDEXED file's KEY TABLE is part of the §14.9.27.4 GR10 fixed-file-attribute set the OPEN statement validates (docs/CONFORMANCE.md DOC-A.1-129; GnuCOBOL's COB_KEYCHECK) — off means an OPEN whose declared keys disagree with the store header's succeeds instead of answering '39', and the organization, record type and record sizes are still validated",
            "OFF | 0 | FALSE | NO (case-insensitive) = the key table is not validated; unset or anything else = it is; read once per run unit (FileRegistry.Reset)",
            typeof(FileRegistry), "IO/FileRegistry.cs"),

        // ── external switches (Control/SwitchStore.cs — the ONE computed family) ──
        new(SwitchStore.Prefix + "<SWITCH-NAME>", "switches",
            "the initial status of an implementor-defined external switch named in SPECIAL-NAMES (ISO/IEC 1989:2023 §12.3.7.4 GR4, implementor-defined item 191): SWITCH-1 reads COBOL_SWITCH_1 (hyphens become underscores, upper-cased; SwitchStore.VariableNameFor)",
            "ON | 1 | TRUE (case-insensitive) = on; anything else, empty or unset = off; read once, at the switch's first interrogation in the run unit, then SET governs; the switch-names are SWITCH-0 … SWITCH-36 and UPSI-0 … UPSI-7 (SwitchStore.Names)",
            typeof(SwitchStore), "Control/SwitchStore.cs", IsPattern: true) { FamilyMember = SwitchStore.IsVariableName },
    ];

    /// <summary>The entry for a variable name — exact, or the family that can produce it; null when the runtime does
    /// not read it.</summary>
    public static ConfigEntry? Find(string variableName)
    {
        ArgumentNullException.ThrowIfNull(variableName);
        foreach (var e in All) if (!e.IsPattern && e.Matches(variableName)) return e;
        foreach (var e in All) if (e.IsPattern && e.Matches(variableName)) return e;
        return null;
    }

    /// <summary>A human-readable summary — one line per variable: name, its value in this process (or "(unset)";
    /// a pattern entry shows the family), what it configures, the accepted values, where it is declared.</summary>
    public static string Describe()
    {
        var sb = new StringBuilder();
        sb.AppendLine("WiseOwl COBOL runtime — environment variables read (RuntimeConfig; a diagnostic registry, not a configuration system):");
        string? subsystem = null;
        foreach (var e in All)
        {
            if (e.Subsystem != subsystem)
            {
                subsystem = e.Subsystem;
                sb.Append("  [").Append(subsystem).AppendLine("]");
            }
            string value = e.IsPattern ? "(a family — one variable per switch-name)" : e.IsSet ? $"= {e.CurrentValue}" : "(unset)";
            sb.Append("    ").Append(e.Name).Append(' ').AppendLine(value);
            sb.Append("        ").AppendLine(e.Purpose);
            sb.Append("        values: ").AppendLine(e.Values);
            sb.Append("        declared: ").Append(e.DeclaringType.Name).Append(" — ").AppendLine(e.DeclaredIn);
        }
        return sb.ToString();
    }
}
