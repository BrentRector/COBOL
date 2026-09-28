// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Cli;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The <c>cobol</c> CLI argument grammar (Program.BuildParser over System.CommandLine). These lock the behaviour
/// the old hand-rolled <c>switch</c> got WRONG: a value option must never consume a following flag as its value
/// (e.g. <c>--nist --run</c> once ate <c>--run</c> as the NIST name, so the program never ran), both the
/// <c>--opt value</c> and <c>--opt=value</c> spellings must work, and <c>--nist</c>'s test name is optional
/// (derived from the source file). Parsing is exercised through the pure resolver, never a spawned process.
/// </summary>
public sealed class CliParserTests
{
    private static CliOptions Resolve(params string[] args)
    {
        var (command, resolve) = Program.BuildParser();
        var parse = command.Parse(args);
        Assert.True(parse.Errors.Count == 0, string.Join("; ", parse.Errors.Select(e => e.Message)));
        return resolve(parse);
    }

    private static IReadOnlyList<string> Errors(params string[] args)
        => Program.BuildParser().Command.Parse(args).Errors.Select(e => e.Message).ToList();

    [Theory]
    [InlineData("-o", "out.dll")]         // space form
    [InlineData("-o=out.dll")]            // inline form (the old parser rejected this as "unknown option")
    [InlineData("--output", "out.dll")]   // the long alias
    [InlineData("--output=out.dll")]
    public void OutputOption_BothForms(params string[] outputArgs)
    {
        var opts = Resolve(["a.cob", .. outputArgs]);
        Assert.Equal("out.dll", opts.OutputPath);
        Assert.Equal("a.cob", opts.SourcePath);
    }

    [Fact]
    public void ValueOption_DoesNotConsumeFollowingFlag()
    {
        // The regression that motivated the rewrite: `--nist --run` must NOT bind "--run" as the NIST name.
        var opts = Resolve("a.cob", "--nist", "--run");
        Assert.True(opts.Run);                                 // --run took effect
        Assert.Equal("a", opts.NistTestName);                 // NIST enabled, name derived from the file
    }

    [Fact]
    public void Nist_OptionalName_DerivedFromSource()
    {
        Assert.Equal("NC101A", Resolve("dir/NC101A.cob", "--nist").NistTestName);   // no name → the file base name
        Assert.Equal("XYZ", Resolve("dir/NC101A.cob", "--nist", "XYZ").NistTestName); // explicit name wins
        Assert.Null(Resolve("a.cob").NistTestName);                                  // no --nist → NIST off
    }

    [Fact]
    public void Std_Defaulting()
    {
        Assert.Equal(2023, Resolve("a.cob").DialectLevel);                 // no --std ⇒ latest
        Assert.Equal(85, Resolve("a.cob", "--nist").DialectLevel);        // --nist ⇒ COBOL-85 (DEVLOG 519)
        Assert.Equal(2002, Resolve("a.cob", "--nist", "--std", "2002").DialectLevel);   // explicit --std wins over --nist
        Assert.Equal(2014, Resolve("a.cob", "--std=2014").DialectLevel);   // inline form
    }

    [Fact]
    public void Std_RejectsUnknownYear()
        => Assert.Contains(Errors("a.cob", "--std", "99"), m => m.Contains("--std must be one of"));

    [Fact]
    public void Source_RequiredAndSingle()
    {
        Assert.NotEmpty(Errors());                                   // no source ⇒ error (was: silent)
        Assert.NotEmpty(Errors("a.cob", "b.cob"));                   // two sources ⇒ error (was: silent overwrite)
    }

    [Fact]
    public void Copy_Repeatable()
    {
        var opts = Resolve("a.cob", "--copy", "d1", "--copy", "d2");
        Assert.Equal(["d1", "d2"], opts.CopyPaths);
    }

    /// <summary>⛔ <c>--sign-encoding</c> (kb/Work PB803, owner decision 2026-09-09) — the DISPLAY over-punch
    /// convention, Annex A.1 items 177/178. Both spellings, the documented IBM default when the option is absent,
    /// and a REFUSAL for an unrecognized value: it is refused by the parser exactly as <c>--std</c>'s year is,
    /// because nothing about the SOURCE is wrong — a bad option is a CLI argument error, never a COBOLNET
    /// diagnostic. `ebcdic` is the value most likely to be tried (it is GnuCOBOL's spelling of the default
    /// convention), so it is the one asserted refused rather than a nonsense string.</summary>
    [Fact]
    public void SignEncoding_BothValues_DefaultsToIbm_AndRejectsAnythingElse()
    {
        Assert.Equal(CobolNet.Runtime.SignEncoding.Ibm, Resolve("a.cob").SignEncoding);
        Assert.Equal(CobolNet.Runtime.SignEncoding.Ibm, Resolve("a.cob", "--sign-encoding", "ibm").SignEncoding);
        Assert.Equal(CobolNet.Runtime.SignEncoding.Ascii, Resolve("a.cob", "--sign-encoding=ascii").SignEncoding);
        Assert.Equal(CobolNet.Runtime.SignEncoding.Ascii, Resolve("a.cob", "--sign-encoding", "ASCII").SignEncoding);
        Assert.Contains(Errors("a.cob", "--sign-encoding", "ebcdic"),
            m => m.Contains("--sign-encoding must be one of"));
        // The option must not swallow a following flag, the defect the whole file exists for.
        Assert.NotEmpty(Errors("a.cob", "--sign-encoding", "--run"));
    }

    /// <summary>kb/Work PB1362 — <c>--source-format</c>: ISO §7.3.24.3 2) "The default reference format of a
    /// compilation group is fixed form", so an ABSENT option is fixed form; <c>free</c> and <c>auto</c> are the
    /// §4.2.10 3) selection of the nonstandard behavior; anything else is a CLI argument error.</summary>
    [Fact]
    public void SourceFormat_DefaultsToFixed_SelectsFreeOrAuto_AndRejectsAnythingElse()
    {
        const CobolNet.Frontend.Preprocessor.InitialReferenceFormat Fixed = CobolNet.Frontend.Preprocessor.InitialReferenceFormat.Fixed;
        Assert.Equal(Fixed, Resolve("a.cob").SourceFormat);
        Assert.Equal(Fixed, Resolve("a.cob", "--source-format", "fixed").SourceFormat);
        Assert.Equal(CobolNet.Frontend.Preprocessor.InitialReferenceFormat.Free,
            Resolve("a.cob", "--source-format=FREE").SourceFormat);
        Assert.Equal(CobolNet.Frontend.Preprocessor.InitialReferenceFormat.Auto,
            Resolve("a.cob", "--source-format", "auto").SourceFormat);
        Assert.Contains(Errors("a.cob", "--source-format", "variable"), m => m.Contains("--source-format must be one of"));
        Assert.NotEmpty(Errors("a.cob", "--source-format", "--run"));
    }

    [Fact]
    public void Flags_And_Full_Sweep_Pattern()
    {
        // The version-continuity-sweep.sh invocation: SRC -o OUT --nist NAME --std 85 --permissive.
        var opts = Resolve("prog.cob", "-o", "p.dll", "--nist", "PROG", "--std", "85", "--permissive");
        Assert.Equal("prog.cob", opts.SourcePath);
        Assert.Equal("p.dll", opts.OutputPath);
        Assert.Equal("PROG", opts.NistTestName);
        Assert.Equal(85, opts.DialectLevel);
        Assert.True(opts.Permissive);
        Assert.False(opts.Run);
    }
}
