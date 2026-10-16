// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE GATE SCRIPTS RUN ONE PYTHON INTERPRETER, RESOLVED ONE WAY (kb/Work PB1637, PB3197;
/// DESIGN-test-build-ci.md §3.14.11): every bash script under <c>scripts/</c> that runs Python runs <c>"$PY"</c>, resolved by sourcing
/// <c>scripts/python-resolve.sh</c>; every PowerShell twin of a bash script (<c>build-local.ps1</c> beside
/// <c>build-local.sh</c>) runs <c>$PY</c>, resolved by dot-sourcing <c>scripts/python-resolve.ps1</c>; the two
/// resolvers probe the same candidates in the same order against the same version floor; and only a provisioner that
/// installs the interpreter (<c>uv python install</c>) names one directly.
/// </summary>
/// <remarks>
/// <para>
/// A name on PATH is not an interpreter (kb/Work PB1637): on the Windows dev box <c>python3</c> is the Microsoft Store
/// alias, which runs and prints "Python was not found"; on a stock Ubuntu host there is no <c>python</c> at all; and in
/// the cloud image a <c>python3</c> below the floor stays first on PATH, where the scripts' syntax fails to parse. The
/// resolver probes each candidate by RUNNING it against the floor. The R2 review found it bypassed by the gate's own
/// entry points (kb/Work PB3197): <c>build-local.sh</c> and <c>build-local.ps1</c> ran a bare <c>python</c> and
/// <c>linux-gate.sh</c> took <c>command -v python3 || command -v python</c> with no probe and no floor — three
/// resolvers for one job, each new script choosing one by copy-paste. The probe has to run before any Python does, so
/// it is written once per shell that starts a gate; this class holds the two statements equal and every gate script
/// on one of them.
/// </para>
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class PythonInterpreterDriftTests
{
    private const string BashResolver = "python-resolve.sh";
    private const string PwshResolver = "python-resolve.ps1";

    /// <summary>
    /// A Python interpreter named as a COMMAND: <c>python</c>, <c>python3</c> or <c>python3.14</c>, optionally quoted,
    /// where a command starts — the line's start, after <c>; &amp; | ( { ! `</c> or <c>$(</c> (PowerShell's call
    /// operator is <c>&amp;</c>), after a keyword (<c>then do else exec time if while until</c>), after a wrapper that
    /// runs or looks up its argument (<c>command [-v]</c>, <c>which</c>, <c>env</c>, <c>nice [-n N]</c>, a
    /// <c>"${prefix[@]}"</c> array) or after an environment assignment (<c>VAR=value python3</c>). A word inside a longer
    /// name or path (<c>python-resolve.sh</c>, <c>scripts/self_tests.py</c>), a string argument (a self-test's expected
    /// value <c>"python"</c>) and a stand-in's definition (<c>python3() { … }</c>) run nothing.
    /// </summary>
    private static readonly Regex BareInterpreter = new(
        @"(?:^|[;&|({!`]|\$\(|\b(?:then|do|else|exec|time|if|while|until|command(?:\s+-v)?|which|env|nice(?:\s+-n\s*-?\d+)?)\s"
        + @"|""\$\{\w+\[@\]\}""\s|\b\w+=\S*\s)\s*['""]?python(?:3(?:\.\d+)?)?['""]?(?=$|[\s;&|)`])");

    /// <summary>A comment: from a <c>#</c> at the line's start or after whitespace (bash and PowerShell alike).</summary>
    private static readonly Regex Comment = new(@"(?:^|\s)#.*$");

    /// <summary>The script installs the interpreter itself, so no resolver can run before it does.</summary>
    private static readonly Regex Provisioner = new(@"\buv python install\b");

    private static readonly Regex SourcesBashResolver = new(@"(?:^|[\s(;])(?:\.|source)\s+.*python-resolve\.sh");
    private static readonly Regex DotSourcesPwshResolver = new(@"(?:^|[\s({;])\.\s+.*python-resolve\.ps1");
    private static readonly Regex UsesPy = new(@"\$\{?PY\b");

    private static IEnumerable<string> Scripts(string pattern) =>
        Directory.EnumerateFiles(TestRepo.Scripts(), pattern, SearchOption.AllDirectories).Order(StringComparer.Ordinal);

    private static string Rel(string path) => Path.GetRelativePath(TestRepo.Root, path).Replace('\\', '/');

    /// <summary>Each non-comment line that names an interpreter directly, as <c>file:line: text</c>.</summary>
    private static IEnumerable<string> BareInterpreterLines(string path) =>
        File.ReadLines(path)
            .Select((line, i) => (Code: Comment.Replace(line, ""), Number: i + 1, Line: line))
            .Where(x => BareInterpreter.IsMatch(x.Code))
            .Select(x => $"{Rel(path)}:{x.Number}: {x.Line.Trim()}");

    [Fact]
    public void EveryBashScript_RunsTheResolvedInterpreter()
    {
        var resolverUsers = new List<string>();
        var provisioners = new List<string>();
        var offenders = new List<string>();
        foreach (string path in Scripts("*.sh").Where(p => Path.GetFileName(p) != BashResolver))
        {
            string text = File.ReadAllText(path);
            if (Provisioner.IsMatch(text))
            {
                provisioners.Add(Rel(path));
                continue;
            }

            offenders.AddRange(BareInterpreterLines(path));
            bool sources = text.Split('\n').Any(l => SourcesBashResolver.IsMatch(Comment.Replace(l, "")));
            if (sources)
            {
                resolverUsers.Add(Rel(path));
            }
            else if (UsesPy.IsMatch(text))
            {
                offenders.Add($"{Rel(path)}: runs $PY but never sources scripts/{BashResolver}");
            }
        }

        Assert.True(offenders.Count == 0,
            $"a bash script runs Python without THE one resolver: source scripts/{BashResolver} and run \"$PY\" "
            + "(kb/Work PB1637, PB3197) — a name on PATH is not an interpreter:\n  " + string.Join("\n  ", offenders));

        // ⛔ THE SCAN MUST SEE ITS OWN POPULATION: the gate's entry points and the battery source the resolver, and the
        // two environment provisioners are the only exemption. A scan that stopped seeing them would pass over nothing.
        Assert.Superset(new HashSet<string>(StringComparer.Ordinal)
                        {
                            "scripts/build-local.sh", "scripts/linux-gate.sh", "scripts/battery.sh",
                            "scripts/measure-battery-determinism.sh",
                        },
            resolverUsers.ToHashSet(StringComparer.Ordinal));
        Assert.Equal(["scripts/cloud/setup-env.sh", "scripts/wsl/setup-wsl.sh"], provisioners);
    }

    [Fact]
    public void EveryPowerShellTwinOfABashScript_RunsTheResolvedInterpreter()
    {
        var twins = Scripts("*.ps1")
            .Where(p => Path.GetFileName(p) != PwshResolver && File.Exists(Path.ChangeExtension(p, ".sh")))
            .ToList();
        var offenders = new List<string>();
        foreach (string path in twins)
        {
            offenders.AddRange(BareInterpreterLines(path));
            string text = File.ReadAllText(path);
            bool dotSources = text.Split('\n').Any(l => DotSourcesPwshResolver.IsMatch(Comment.Replace(l, "")));
            if (UsesPy.IsMatch(text) && !dotSources)
            {
                offenders.Add($"{Rel(path)}: runs $PY but never dot-sources scripts/{PwshResolver}");
            }
        }

        Assert.True(offenders.Count == 0,
            $"a PowerShell twin of a bash gate script runs Python without THE one resolver: dot-source "
            + $"scripts/{PwshResolver} and run `& $PY` (kb/Work PB3197):\n  " + string.Join("\n  ", offenders));
        Assert.Contains("scripts/build-local.ps1", twins.Select(Rel));
    }

    /// <summary>The candidates a resolver probes, in order, with the override spelled <c>$PYTHON</c> in both shells.</summary>
    private static string[] Candidates(string text, string pattern) =>
        Regex.Match(text, pattern).Groups["list"].Value
            .Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim('\'', '"') switch
            {
                "${PYTHON:+\"$PYTHON\"}" or "$env:PYTHON" => "$PYTHON",
                var name => name,
            })
            .ToArray();

    [Fact]
    public void TheTwoResolvers_ProbeTheSameCandidates_AgainstTheSameFloor()
    {
        string bash = File.ReadAllText(TestRepo.Scripts(BashResolver));
        string pwsh = File.ReadAllText(TestRepo.Scripts(PwshResolver));

        string[] bashCandidates = Candidates(bash, @"for __py_cand in (?<list>.+?); do");
        string[] pwshCandidates = Candidates(pwsh, @"foreach \(\$candidate in @\((?<list>[^)]+)\)\)");
        Assert.Equal(["$PYTHON", "python3", "python"], bashCandidates);
        Assert.Equal(bashCandidates, pwshCandidates);

        const string probe = @"-c '(?<probe>[^']*sys\.version_info >= \(\d+, \d+\)[^']*)'";
        string bashProbe = Regex.Match(bash, probe).Groups["probe"].Value;
        Assert.NotEqual("", bashProbe);
        Assert.Equal(bashProbe, Regex.Match(pwsh, probe).Groups["probe"].Value);
    }
}
