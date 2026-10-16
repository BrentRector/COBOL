// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet;
using CobolNet.Binding;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A FLOATING-POINT OPERAND AT AN ISO §15.3 TYPE-6 (INTEGER) POSITION, UNDER <c>--permissive</c>, ANSWERS AS THE
/// INTEGER IT HOLDS — asserted over EVERY integer position of EVERY verified intrinsic argument schema, not over the
/// functions a defect happened to name (kb/Work PB651).
/// </summary>
/// <remarks>
/// <para><b>The rule.</b> ISO §15.3 type 6: "An arithmetic expression that will always result in an integer value or
/// an integer data item shall be specified." A floating-point item is neither, so STRICT conformance refuses it there
/// (COBOLNET1627, kb/Work PB248) and only <c>--permissive</c> binds it. What it then means is the determination
/// docs/CONFORMANCE.md DOC-A.1-124 records: an operand represents an integer exactly when its value has no nonzero
/// fraction digit, the same value gets the same answer at every rule that asks, and with EC-ARGUMENT-FUNCTION
/// checking off a non-integer is TRUNCATED to the integer the implementor defines. So a <c>FLOAT-LONG</c> holding
/// 5 and one holding 5.5 must both answer exactly what the integer literal 5 answers.</para>
///
/// <para><b>Why a drift test and not more programs (kb/Work PB651).</b> PB248 moved a whole CLASS of source — a float
/// at an integer position — out of the strict lane, and every mechanism reasoned "the binder rejects that, so this
/// path is unreachable" became potentially wrong in the permissive lane, which nothing probed. PB635 measured it once:
/// ten functions emitted calls to <c>…Real</c> bodies that did not exist (Roslyn CS0117 on a program the front end had
/// accepted). <c>FloatIntegerArgumentPermissiveTests</c> covers the functions those defects named; this covers the
/// POPULATION, derived from <see cref="IntrinsicArgumentRules.Verified"/> — the spec-verified schemas the strict
/// screen itself reads — so a new integer position, or a schema that gains one, fails here until a fixture probes it.</para>
///
/// <para><b>The probe is the RUN.</b> One program, every fixture three times: over a <c>FLOAT-LONG</c> holding the
/// integer, over one holding that integer plus one half, and over the integer literal — the control. A backend error
/// is the PB635 crash; a value that differs from the control is a function whose answer depends on how its argument
/// happened to be stored, which is the invariant kb/Work PB21 was opened for.</para>
/// </remarks>
public sealed class IntrinsicIntegerPositionPermissiveDriftTests : CobolNetTestBase
{
    /// <summary>One reference per (function, 1-based OPERAND position) — the schema's convention, which skips a NAME
    /// argument (STANDARD-COMPARE's ordering-name, §15.3 type 12). <c>{A}</c> is the integer operand; the value is a
    /// non-negative integer inside the position's own §15.x.3 value rule, so the control is a legal call and a
    /// one-half increment truncates back to it.</summary>
    private static readonly Dictionary<string, (string Reference, int Value)> Fixtures =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ANNUITY#2"] = ("FUNCTION ANNUITY(0.05 {A})", 3),                           // §15.9.3 r3
            ["BASECONVERT#2"] = ("FUNCTION BASECONVERT(\"17\" {A} 16)", 10),             // §15.12.3 r1
            ["BASECONVERT#3"] = ("FUNCTION BASECONVERT(\"17\" 10 {A})", 16),
            ["BOOLEAN-OF-INTEGER#1"] = ("FUNCTION BOOLEAN-OF-INTEGER({A} 8)", 5),        // §15.13.3 r1/r2
            ["BOOLEAN-OF-INTEGER#2"] = ("FUNCTION BOOLEAN-OF-INTEGER(5 {A})", 8),
            ["CHAR#1"] = ("FUNCTION CHAR({A})", 66),                                     // §15.15.3 r1
            ["CHAR-NATIONAL#1"] = ("FUNCTION CHAR-NATIONAL({A})", 66),                   // §15.16.3 r1
            ["COMBINED-DATETIME#1"] = ("FUNCTION COMBINED-DATETIME({A} 3600)", 143951),  // §15.17.3 r1
            ["DATE-OF-INTEGER#1"] = ("FUNCTION DATE-OF-INTEGER({A})", 143951),           // §15.22.3 r1
            ["DATE-TO-YYYYMMDD#1"] = ("FUNCTION DATE-TO-YYYYMMDD({A} 50 2000)", 950215), // §15.23.3 r1/r2/r4
            ["DATE-TO-YYYYMMDD#2"] = ("FUNCTION DATE-TO-YYYYMMDD(950215 {A} 2000)", 50),
            ["DATE-TO-YYYYMMDD#3"] = ("FUNCTION DATE-TO-YYYYMMDD(950215 50 {A})", 2000),
            ["DAY-OF-INTEGER#1"] = ("FUNCTION DAY-OF-INTEGER({A})", 143951),             // §15.24.3 r1
            ["DAY-TO-YYYYDDD#1"] = ("FUNCTION DAY-TO-YYYYDDD({A} 50 2000)", 95046),      // §15.25.3 r1/r2/r4
            ["DAY-TO-YYYYDDD#2"] = ("FUNCTION DAY-TO-YYYYDDD(95046 {A} 2000)", 50),
            ["DAY-TO-YYYYDDD#3"] = ("FUNCTION DAY-TO-YYYYDDD(95046 50 {A})", 2000),
            ["FACTORIAL#1"] = ("FUNCTION FACTORIAL({A})", 5),                            // §15.36.3 r1
            ["FIND-STRING#3"] = ("FUNCTION FIND-STRING(\"abcabc\" \"c\" START AFTER {A})", 1), // §15.37.3 r3
            ["FORMATTED-DATE#2"] = ("FUNCTION FORMATTED-DATE(\"YYYYMMDD\" {A})", 143951),          // §15.39.3 r3
            ["FORMATTED-DATETIME#2"] = ("FUNCTION FORMATTED-DATETIME(\"YYYYMMDDThhmmss\" {A} 3600)", 143951),
            ["FORMATTED-DATETIME#4"] = ("FUNCTION FORMATTED-DATETIME(\"YYYYMMDDThhmmss+hhmm\" 143951 3600 {A})", 60),
            ["FORMATTED-TIME#3"] = ("FUNCTION FORMATTED-TIME(\"hhmmss+hhmm\" 3600 {A})", 60),     // §15.41.3 r4
            ["INTEGER-OF-DATE#1"] = ("FUNCTION INTEGER-OF-DATE({A})", 19950215),         // §15.46.3 r1
            ["INTEGER-OF-DAY#1"] = ("FUNCTION INTEGER-OF-DAY({A})", 1995046),            // §15.47.3 r1
            ["MOD#1"] = ("FUNCTION MOD({A} 3)", 7),                                      // §15.64.3 r1
            ["MOD#2"] = ("FUNCTION MOD(7 {A})", 3),
            ["RANDOM#1"] = ("FUNCTION RANDOM({A})", 5),                                  // §15.75.3 r1/r2
            ["STANDARD-COMPARE#3"] = ("FUNCTION STANDARD-COMPARE(\"A\" \"B\" {A})", 1),  // §15.85.3 r6
            ["TEST-DATE-YYYYMMDD#1"] = ("FUNCTION TEST-DATE-YYYYMMDD({A})", 19950215),   // §15.90.3 r1
            ["TEST-DAY-YYYYDDD#1"] = ("FUNCTION TEST-DAY-YYYYDDD({A})", 1995046),        // §15.91.3 r1
            ["YEAR-TO-YYYY#1"] = ("FUNCTION YEAR-TO-YYYY({A} 50 2000)", 95),             // §15.100.3 r1/r2/r4
            ["YEAR-TO-YYYY#2"] = ("FUNCTION YEAR-TO-YYYY(95 {A} 2000)", 50),
            ["YEAR-TO-YYYY#3"] = ("FUNCTION YEAR-TO-YYYY(95 50 {A})", 2000),
        };

    /// <summary>Every §15.3 type-6 OPERAND position a verified schema declares, up to the catalog row's own maximum
    /// argument count (a uniform schema's kind repeats as a TAIL, so the row's arity bounds it).</summary>
    private static List<string> IntegerPositions()
    {
        var keys = new List<string>();
        foreach (var (name, schema) in IntrinsicArgumentRules.Verified)
        {
            Assert.True(IntrinsicCatalog.TryGet(name, out var sig),
                $"IntrinsicArgumentRules.Verified names '{name}', which the catalog does not — a schema nothing can call.");
            int positions = Math.Min(sig.MaxArgs, IntrinsicFloatLaneArgumentRunDriftTests.PastEveryDeclaredPosition);
            for (int i = 0; i < positions; i++)
                if (schema.At(i) is { Kind: 'i' })
                    keys.Add($"{name}#{i + 1}");
        }
        return keys;
    }

    [Fact]
    public void EveryIntegerPosition_HasAFixture()
    {
        var keys = IntegerPositions();
        Assert.True(keys.Count >= 30,
            $"only {keys.Count} §15.3 type-6 positions found in IntrinsicArgumentRules.Verified — the schema sweep "
            + "shrank unexpectedly; check the table before lowering this floor.");
        var missing = keys.Where(k => !Fixtures.ContainsKey(k)).ToList();
        var stale = Fixtures.Keys.Where(k => !keys.Contains(k, StringComparer.OrdinalIgnoreCase)).ToList();
        Assert.True(missing.Count == 0,
            "a verified intrinsic schema declares a §15.3 type-6 position with no permissive float fixture — add one, "
            + "or a float at that position reaches the renderer under --permissive unprobed (kb/Work PB651):\n  "
            + string.Join("\n  ", missing));
        Assert.True(stale.Count == 0,
            "a fixture names a position no verified schema types as integer — remove it or fix the schema:\n  "
            + string.Join("\n  ", stale));
    }

    [Fact]
    public void AFloatAtAnIntegerPosition_UnderPermissive_AnswersAsTheIntegerItHolds()
    {
        var keys = IntegerPositions().Order(StringComparer.Ordinal).ToList();
        var data = new StringBuilder();
        var proc = new StringBuilder();
        for (int n = 0; n < keys.Count; n++)
        {
            var (reference, value) = Fixtures[keys[n]];
            data.AppendLine($"01 FI{n:00} USAGE FLOAT-LONG VALUE {value}.");
            data.AppendLine($"01 FH{n:00} USAGE FLOAT-LONG VALUE {value}.5.");
            proc.AppendLine($"    DISPLAY \"{keys[n]}/I=\" {reference.Replace("{A}", value.ToString())}");
            proc.AppendLine($"    DISPLAY \"{keys[n]}/F=\" {reference.Replace("{A}", $"FI{n:00}")}");
            proc.AppendLine($"    DISPLAY \"{keys[n]}/H=\" {reference.Replace("{A}", $"FH{n:00}")}");
        }
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB651INTPOS.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            {data}PROCEDURE DIVISION.
            {proc}    STOP RUN.
            """;
        string srcPath = Path.Combine(TempDir, "pb651intpos.cob");
        string dllPath = Path.Combine(TempDir, "pb651intpos.dll");
        File.WriteAllText(srcPath, source);
        var compiled = CompilerDriver.Compile(new CompilerDriver.Options(
            srcPath, dllPath, DialectLevel: 2023, Permissive: true, SourceFormat: InitialReferenceFormat.Free));

        // ⛔ A BACKEND error is the PB635 signature: Roslyn refusing generated C# for source the front end accepted.
        Assert.True(compiled.Success,
            $"--permissive must accept a float at every §15.3 type-6 position and generate C# that compiles "
            + $"({compiled.Status}):\n  {string.Join("\n  ", compiled.Errors)}");
        var unwarned = keys.Where(k => !compiled.Warnings.Any(w => w.Contains("COBOLNET1627", StringComparison.Ordinal)
            && w.Contains(k[..k.IndexOf('#')], StringComparison.OrdinalIgnoreCase))).ToList();
        Assert.True(unwarned.Count == 0,
            "--permissive bound a float at a §15.3 type-6 position WITHOUT the COBOLNET1627 warning that names the "
            + "extension:\n  " + string.Join("\n  ", unwarned));

        var (ran, stdout, detail) = RunAssembly(dllPath);
        Assert.True(ran, detail);
        var lines = stdout.Split("\r\n").ToDictionary(l => l[..l.IndexOf('=')], l => l[(l.IndexOf('=') + 1)..]);
        var offenders = new List<string>();
        foreach (string key in keys)
        {
            string control = lines[$"{key}/I"];
            foreach (string probe in new[] { "F", "H" })
                if (lines[$"{key}/{probe}"] != control)
                    offenders.Add($"{key} over a FLOAT-LONG holding {Fixtures[key].Value}{(probe == "H" ? ".5" : "")}: "
                        + $"[{lines[$"{key}/{probe}"]}], but the integer literal {Fixtures[key].Value} answers [{control}]");
        }
        Assert.True(offenders.Count == 0,
            "a float at an ISO §15.3 type-6 position must answer as the integer it holds — exactly, or truncated with "
            + "EC-ARGUMENT-FUNCTION checking off (docs/CONFORMANCE.md DOC-A.1-124). These answered differently from the "
            + "integer literal, so the function's value depended on how its argument was stored (kb/Work PB651):\n  "
            + string.Join("\n  ", offenders));
    }
}
