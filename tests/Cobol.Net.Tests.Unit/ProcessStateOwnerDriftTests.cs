// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE PROCESS'S OWN STATE (ITS EXIT CODE AND ITS CONSOLE) IS WRITTEN ONLY BY AN ENTRY THAT IS THE PROCESS (kb/Work
/// PB3163, PB3193; docs/CONFORMANCE.md DOC-A.1-167, DOC-A.1-59): the <c>cobol</c> CLI's <c>Main</c> and
/// <c>ProgramTable.RunModule</c>, the generated <c>Main</c>'s one statement. A host that embeds a run unit
/// (<c>ProgramTable.RunMain</c>, <c>RunUnit.Run</c>, a CALL into a module) owns its process, and no statement the run
/// unit executes may change what the host set.
/// <para>Both defects had the same shape and neither was visible to a corpus golden, because every golden's activator
/// is the operating system, where the run unit and the process are one: the <c>RunUnit.ExitStatus</c> setter wrote
/// <c>Environment.ExitCode</c> (PB3163), and every <c>RunMain</c> called <c>StandardStreams.EnsureUtf8</c>, which set
/// the host's console to UTF-8 or replaced its writers (PB3193). The behavioural witnesses are
/// <c>RunUnitHostExitCodeTests</c> and <c>RunUnitHostConsoleTests</c> (Conformance), each a real host in its own
/// process. This sweep is what keeps the NEXT host entry from reaching either write: each process-owned write is
/// listed once in <see cref="Owners"/>, with the one file and the members allowed to make it.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ProcessStateOwnerDriftTests
{
    /// <summary>A class member's declaration line (four-space indent): the enclosing member of the lines below it.</summary>
    private static readonly Regex MemberDeclaration = new(
        @"^ {4}(?:(?:public|private|internal|protected|static|override|virtual|sealed|async|unsafe)\s+)+[^=;(]*?\b(?<name>\w+)\s*(?:<[^>]*>)?\s*\(",
        RegexOptions.NonBacktracking);

    /// <summary>Each process-owned write, the one file allowed to make it, and the members of that file allowed to
    /// (empty: any member of that file, because the file IS the one mechanism).</summary>
    private static readonly (string What, Regex Write, string File, string[] Members)[] Owners =
    [
        ("the process exit code", new Regex(@"\bEnvironment\.ExitCode\s*=(?!=)"),
            Path.Combine("Cobol.Net.Runtime", "Control", "ProgramTable.cs"), ["RunModule"]),
        ("a process exit", new Regex(@"\bEnvironment\.(?:Exit|FailFast)\s*\("), "", []),
        ("the process console", new Regex(@"\bConsole\.(?:SetOut|SetError|SetIn)\s*\(|\bConsole\.(?:OutputEncoding|InputEncoding)\s*=(?!=)"),
            Path.Combine("Cobol.Net.Runtime", "IO", "StandardStreams.cs"), []),
        ("the process console's configuration", new Regex(@"\bStandardStreams\.EnsureUtf8\s*\("),
            Path.Combine("Cobol.Net.Runtime", "Control", "ProgramTable.cs"), ["RunModule"]),
        ("the process console's configuration", new Regex(@"\bStandardStreams\.EnsureUtf8\s*\("),
            Path.Combine("Cobol.Net.Cli", "Program.cs"), ["Main"]),
    ];

    [Fact]
    public void EveryProcessOwnedWrite_IsMadeByAProcessEntry()
    {
        var offenders = new List<string>();
        var seen = new HashSet<int>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(TestRepo.Src(), file);
            string sep = Path.DirectorySeparatorChar.ToString();
            if (rel.Contains(sep + "obj" + sep) || rel.Contains(sep + "bin" + sep)) continue;
            string[] lines = File.ReadAllLines(file);
            string member = "";
            for (int i = 0; i < lines.Length; i++)
            {
                if (MemberDeclaration.Match(lines[i]) is { Success: true } m) member = m.Groups["name"].Value;
                if (lines[i].TrimStart().StartsWith("//", StringComparison.Ordinal)) continue;
                foreach (var what in Owners.Select(o => o.What).Distinct())
                {
                    var rows = Owners.Select((o, k) => (o, k)).Where(r => r.o.What == what).ToList();
                    if (!rows[0].o.Write.IsMatch(lines[i])) continue;
                    int allowed = rows.FindIndex(r => r.o.File == rel && (r.o.Members.Length == 0 || r.o.Members.Contains(member)));
                    if (allowed < 0) offenders.Add($"{rel}:{i + 1} ({member}): writes {what}: {lines[i].Trim()}");
                    else seen.Add(rows[allowed].k);
                }
            }
        }

        Assert.True(offenders.Count == 0,
            "A process-owned write outside the entry that IS the process (kb/Work PB3163, PB3193): a host that embeds the "
            + "run unit would find its exit code or its console changed. Move the write to ProgramTable.RunModule (or the "
            + "CLI's Main), or into StandardStreams for the console. Offending sites:\n  " + string.Join("\n  ", offenders));
        // A row whose owner no longer makes its write is a stale row: the table must describe the tree.
        var stale = Owners.Select((o, k) => (o, k)).Where(r => r.o.File.Length > 0 && !seen.Contains(r.k))
            .Select(r => $"{r.o.File} {string.Join("/", r.o.Members)}: {r.o.What}").ToList();
        Assert.True(stale.Count == 0, "Owner rows that no longer match a write in the tree:\n  " + string.Join("\n  ", stale));
    }
}
