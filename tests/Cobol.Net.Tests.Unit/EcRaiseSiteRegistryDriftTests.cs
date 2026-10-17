// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Runtime.CompilerServices;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Binding.Procedure;
using CobolNet.Runtime.Exceptions;
using CobolNet.Tests.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY EXCEPTION-NAME THE CATALOG QUOTES IS DECLARED BY THE BINDER AND RAISED BY THE RUNTIME — OR IT IS DECLINED, WITH
/// A REASON A READER CAN FOLLOW (kb/Work PB3102, PB1762, PB1037; COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN D13).
/// ISO §14.6.13.1.1: "If checking for an exception condition is enabled and an exception status indicator is set as a
/// result of an exception detected during the execution of a statement, the associated exception condition is raised",
/// and "when the general rules for a statement indicate that a specific exception condition exists, it is raised only if
/// checking for that exception condition is enabled". So a name needs BOTH halves, and a name with one is a defect with a
/// shape of its own:
/// <list type="bullet">
/// <item><b>raised but never declared</b> — the PB452 / PB409 / PB326 / PB349 / PB233 / PB1453 shape: the runtime has a
/// raise site whose checking flag no statement ever sets, so <c>&gt;&gt;TURN … CHECKING ON</c> wraps nothing and the
/// declarative never runs, behind a green suite;</item>
/// <item><b>declared but never raised</b> — a dead guard: the statement binds a wrapper for a condition nothing sets;</item>
/// <item><b>neither</b> — a catalogued name no part of the compiler or runtime accounts for (PB1036's four sort-merge names
/// sat in this state for months).</item>
/// </list>
/// A name that is deliberately neither is on <see cref="DeclinedRaiseSites"/>, and every entry names the
/// <c>docs/CONFORMANCE.md</c> row or section that says why — the test reads that text and fails when the anchor does not
/// mention the name.
///
/// <para>⚠ <b>WHAT "RAISED" MEANS HERE, stated because the honest boundary matters.</b> A raise site is a string literal
/// that IS the name (or a quoted copy of it, in text an emitter writes) in <c>src/Cobol.Net.Runtime</c> or in the code
/// generator, outside the catalog and the gate tables; the EC-I-O family is raised through the status map
/// (<see cref="ExceptionCatalog.IoEcOfStatus"/> over every two-digit status) and the three names §13.18.34.4 / §14.9.51.4
/// name outright. That proves a raise site EXISTS, not that it is reachable — reachability is the conformance goldens'
/// job, and the cost of this rule is one literal per name.</para>
///
/// <para>⚠ <b>WHAT IT DOES NOT CATCH: A LOST ROW FOR A NAME ANOTHER SOURCE STILL DECLARES.</b> The registry is per NAME
/// (<see cref="EcBinder.AllDeclaredNames"/>), so it fails when a name loses its LAST declaration, never when one
/// statement kind loses its row. It was seen to fail by removing EC-OO-RESOURCE from the INVOKE row (the failure names
/// the name). Deleting the <c>BoundSetEntry</c> row's EC-PROGRAM-NOT-FOUND leaves it GREEN — <c>ProgramNames</c> (the
/// CALL and CANCEL rows), <c>ActivationSiteNames</c> and the address-operand row still declare the name — and that loss is
/// caught by the goldens that run the row: <c>conformance:cases/8.4.3.13-Program-address-identifier/pb1549_set_program_address_fatal_resume</c> (the ISO
/// ADDRESS OF PROGRAM sender) and <c>conformance:cases/_unassigned/pb3102_set_entry_not_found_declared</c> (the ENTRY senders), both
/// seen red with the row deleted (kb/Work PB3102, review finding R1-1).</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class EcRaiseSiteRegistryDriftTests
{
    private const string Conformance = "docs/CONFORMANCE.md";

    /// <summary>The names no statement declares and no code raises, each with the CONFORMANCE.md row or section that says why.
    /// An anchor <c>DOC-A.1-n</c> is that Annex A.1 row (it must quote the name); <c>§4</c> is "Documented non-support
    /// facilities" (it must quote the name or its level-2 family); <c>§3:D-x</c> is the §3 determination bullet that begins
    /// <c>- **D-x</c> (it must quote the name).</summary>
    private static readonly (string[] Names, string Anchor)[] DeclinedRaiseSites =
    [
        // Table 13's implementor-defined level-2 names (§14.6.13.1.1, "the '-IMP' suffix type"). DOC-A.1-100: "the
        // implementation itself sets only EC-PROGRAM-IMP … The implementation never sets any of the others, so a program
        // sees them only when it raises them itself" (RAISE EXCEPTION); EC-SCREEN-IMP and EC-VALIDATE-IMP belong to modules
        // that are not provided and are refused wherever they are written. EC-PROGRAM-IMP and EC-I-O-IMP are NOT here: the
        // first is raised by ProgramTable.ParentInstance and the second is the status map's '9x' row.
        (["EC-ARGUMENT-IMP", "EC-BOUND-IMP", "EC-CONTINUE-IMP", "EC-DATA-IMP", "EC-EXTERNAL-IMP", "EC-FLOW-IMP",
          "EC-FUNCTION-IMP", "EC-LOCALE-IMP", "EC-MCS-IMP", "EC-OO-IMP", "EC-ORDER-IMP", "EC-OVERFLOW-IMP", "EC-RAISING-IMP",
          "EC-RANGE-IMP", "EC-REPORT-IMP", "EC-SCREEN-IMP", "EC-SIZE-IMP", "EC-SORT-MERGE-IMP", "EC-STORAGE-IMP",
          "EC-VALIDATE-IMP"], "DOC-A.1-100"),
        // The facilities CONFORMANCE.md §4 declines: the Message Control System (item 1), commit and rollback (item 2), the
        // VALIDATE facility (item 3) and screen handling (item 4). Each name is refused or has no setting site.
        (["EC-MCS-ABNORMAL-TERMINATION", "EC-MCS-INVALID-TAG", "EC-MCS-MESSAGE-LENGTH", "EC-MCS-NO-REQUESTER",
          "EC-MCS-NO-SERVER", "EC-MCS-NORMAL-TERMINATION", "EC-MCS-REQUESTOR-FAILED"], "§4"),
        (["EC-FLOW-APPLY-COMMIT", "EC-FLOW-COMMIT", "EC-FLOW-ROLLBACK"], "§4"),
        (["EC-SCREEN-FIELD-OVERLAP", "EC-SCREEN-ITEM-TRUNCATED", "EC-SCREEN-LINE-NUMBER", "EC-SCREEN-STARTING-COLUMN"], "§4"),
        (["EC-VALIDATE-CONTENT", "EC-VALIDATE-FORMAT", "EC-VALIDATE-RELATION", "EC-VALIDATE-VARYING"], "§4"),
        // §15.65.4 r2: MODULE-NAME's returned item is dynamic-length, so "does not fit" cannot happen (DOC-A.1-133).
        (["EC-BOUND-FUNC-RET-VALUE"], "DOC-A.1-133"),
        // Table 13 names it ("EXIT PROGRAM in a global Declarative") and no general rule sets it (§14.9.14.4 GR3 imports
        // GOBACK's GR3 and GR4, not GR6, which sets EC-FLOW-GLOBAL-GOBACK).
        (["EC-FLOW-GLOBAL-EXIT"], "§3:D-GLOBAL-EXIT"),
    ];

    /// <summary>The names raised at the ACTIVATION BOUNDARY, in the ACTIVATING element, for a name the propagation machinery
    /// chooses at RUN time (§14.9.18.4 GR1 b) 3a, §14.6.13.1.5 EXIT/GOBACK item 4). The activating statement carries its own
    /// <see cref="EcCheckingProfile"/> (<c>EcBinder.StampActivators</c>), which answers for ANY catalogued name, so no
    /// statement declares these; the raise site is <c>ExceptionState.StageLastName</c> / <c>ConvertObjectPropagationToNamed</c>.
    /// Each entry names the landed kb/Work note that built the mechanism.</summary>
    private static readonly (string Name, string Note)[] RaisedAtTheActivationBoundary =
    [
        ("EC-RAISING-NOT-SPECIFIED", "PB408"),
        ("EC-OO-EXCEPTION", "PB1121"),
    ];

    private static IEnumerable<string> Level3Names =>
        ExceptionCatalog.Level3Rows.Select(r => r.Name).OrderBy(n => n, StringComparer.Ordinal);

    private static readonly HashSet<string> ComparisonMethods =
        new(["Same", "Equals", "StartsWith", "EndsWith", "Contains", "IndexOf"], StringComparer.Ordinal);

    /// <summary>Is this literal only COMPARED against (<c>CobolNames.Same(name, "EC-X")</c>, <c>name == "EC-X"</c>, a case
    /// label)? Excluding the name from a rule — <c>StageAutomaticPropagation</c> refuses EC-FLOW-GLOBAL-EXIT — is not raising it.</summary>
    private static bool IsComparisonOperand(SyntaxToken token)
    {
        for (var node = token.Parent; node is not null; node = node.Parent)
        {
            switch (node)
            {
                case ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax inv }:
                    string callee = inv.Expression is MemberAccessExpressionSyntax m ? m.Name.Identifier.Text
                        : inv.Expression is IdentifierNameSyntax id ? id.Identifier.Text : "";
                    return ComparisonMethods.Contains(callee);
                case BinaryExpressionSyntax b when b.Kind() is SyntaxKind.EqualsExpression or SyntaxKind.NotEqualsExpression:
                case ConstantPatternSyntax:
                case CaseSwitchLabelSyntax:
                    return true;
                case StatementSyntax or MemberDeclarationSyntax:
                    return false;
            }
        }
        return false;
    }

    /// <summary>The string literals of a source tree that ARE a name or quote one (<c>"EC-X"</c> inside emitted text).</summary>
    private static HashSet<string> NamesQuotedBy(string root, Func<string, bool> skipFile, HashSet<string> universe)
    {
        var found = new HashSet<string>(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (rel.StartsWith("obj/", StringComparison.Ordinal) || rel.StartsWith("bin/", StringComparison.Ordinal)
                || rel.StartsWith("Generated/", StringComparison.Ordinal) || skipFile(rel))
                continue;
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
            foreach (var token in tree.GetRoot().DescendantTokens())
            {
                if (token.Kind() is not (SyntaxKind.StringLiteralToken or SyntaxKind.InterpolatedStringTextToken
                    or SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.MultiLineRawStringLiteralToken))
                    continue;
                string text = token.ValueText;
                if (!text.Contains("EC-", StringComparison.Ordinal)) continue;
                if (IsComparisonOperand(token)) continue;
                if (universe.Contains(text)) { found.Add(text); continue; }
                foreach (string name in universe)
                    if (text.Contains('"' + name + '"', StringComparison.Ordinal))
                        found.Add(name);
            }
        }
        return found;
    }

    /// <summary>The EC-I-O names a connector can set to exist: §9.1.13.1's status map over every two-digit status, and the
    /// three §13.18.34.4 GR6 b) 2 / §14.9.51.4 GR27 a) names a rule gives outright.</summary>
    private static IEnumerable<string> IoRaised()
    {
        for (int i = 0; i < 100; i++)
            if (ExceptionCatalog.IoEcOfStatus(i.ToString("00")) is { } n)
                yield return n;
        yield return ExceptionCatalog.IoLinage;
        yield return ExceptionCatalog.IoEop;
        yield return ExceptionCatalog.IoEopOverflow;
    }

    private static readonly Lazy<HashSet<string>> Raised = new(() =>
    {
        var universe = Level3Names.ToHashSet(StringComparer.Ordinal);
        var raised = NamesQuotedBy(TestRepo.Src("Cobol.Net.Runtime"), rel => rel == "Exceptions/ExceptionCatalog.cs", universe);
        // The code generator writes raise sites into the program (SEARCH, STRING, SET … TO ADDRESS OF …). The gate table
        // itself (EcEmitter.FatalAmbientGates) names every FLAGGED name and is a declaration, not a raise.
        raised.UnionWith(NamesQuotedBy(TestRepo.Src("Cobol.Net.Compiler", "CodeGen"), rel => rel == "EcEmitter.cs", universe));
        raised.UnionWith(IoRaised().Where(ExceptionCatalog.IoMaskNames.Contains));
        return raised;
    });

    private static string ConformanceText => File.ReadAllText(TestRepo.At(Conformance.Split('/')));

    [Fact]
    public void EveryCataloguedLevel3Name_IsDeclaredAndRaised_OrDeclinedWithAReason()
    {
        var declared = EcBinder.AllDeclaredNames;
        var declined = DeclinedRaiseSites.SelectMany(d => d.Names.Select(n => (Name: n, d.Anchor))).ToDictionary(d => d.Name, d => d.Anchor);
        var atBoundary = RaisedAtTheActivationBoundary.Select(b => b.Name).ToHashSet(StringComparer.Ordinal);
        var problems = new List<string>();
        foreach (string name in Level3Names)
        {
            bool isDeclared = declared.Contains(name), isRaised = Raised.Value.Contains(name), isDeclined = declined.ContainsKey(name);
            if (atBoundary.Contains(name))
            {
                if (isDeclared) problems.Add($"{name}: on RaisedAtTheActivationBoundary but an EcBinder table also declares it — remove one");
                if (!isRaised) problems.Add($"{name}: on RaisedAtTheActivationBoundary but no raise site stages it");
            }
            else if (isDeclined)
            {
                if (isDeclared || isRaised)
                    problems.Add($"{name}: on DeclinedRaiseSites ({declined[name]}) but {(isDeclared ? "the binder declares it" : "")}"
                        + $"{(isDeclared && isRaised ? " and " : "")}{(isRaised ? "a raise site exists" : "")} — remove the stale entry");
            }
            else if (isRaised && !isDeclared)
                problems.Add($"{name}: RAISED BUT NEVER DECLARED — a raise site exists, but no EcBinder table (NodeKindRaises, IoRaise, "
                    + "ActivationSiteNames, OperandRaises, AmbientRaises) gives any statement this name, so `>>TURN "
                    + $"{name} CHECKING ON` wraps nothing and the raise is lost (the PB452 / PB1453 shape)");
            else if (isDeclared && !isRaised)
                problems.Add($"{name}: DECLARED BUT NEVER RAISED — an EcBinder table gives statements this name, but no raise site in "
                    + "the runtime or the code generator sets it (a dead guard)");
            else if (!isDeclared && !isRaised)
                problems.Add($"{name}: NEITHER DECLARED NOR RAISED — add the declaration and the raise site, or decline the name on "
                    + "DeclinedRaiseSites with the CONFORMANCE.md row or section that says why");
        }
        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public void EveryDeclinedEntry_NamesACataloguedName_AndItsAnchorQuotesIt()
    {
        string doc = ConformanceText;
        var names = Level3Names.ToHashSet(StringComparer.Ordinal);
        string section4 = doc[doc.IndexOf("\n## 4. ", StringComparison.Ordinal)..doc.IndexOf("\n## 5. ", StringComparison.Ordinal)];
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var problems = new List<string>();
        foreach (var (entryNames, anchor) in DeclinedRaiseSites)
        {
            foreach (string name in entryNames)
            {
                if (!names.Contains(name)) problems.Add($"{name}: not a catalogued level-3 name");
                if (!seen.Add(name)) problems.Add($"{name}: listed twice");
                if (anchor == "§4")
                {
                    string family = ExceptionCatalog.Level2OfName(name) ?? "";
                    if (!section4.Contains(name, StringComparison.Ordinal) && !section4.Contains(family, StringComparison.Ordinal))
                        problems.Add($"{name}: {Conformance} §4 quotes neither the name nor its family {family}");
                }
                else if (anchor.StartsWith("§3:", StringComparison.Ordinal))
                {
                    string bullet = "- **" + anchor["§3:".Length..];
                    int at = doc.IndexOf(bullet, StringComparison.Ordinal);
                    if (at < 0) problems.Add($"{name}: {Conformance} has no determination bullet beginning '{bullet}'");
                    else
                    {
                        int end = doc.IndexOf("\n- **", at + bullet.Length, StringComparison.Ordinal);
                        if (!doc[at..(end < 0 ? doc.Length : end)].Contains(name, StringComparison.Ordinal))
                            problems.Add($"{name}: the {anchor} determination of {Conformance} does not quote the name");
                    }
                }
                else
                {
                    string rowStart = $"| {anchor} |";
                    int at = doc.IndexOf(rowStart, StringComparison.Ordinal);
                    if (at < 0) problems.Add($"{name}: {Conformance} has no row {anchor}");
                    else
                    {
                        int end = doc.IndexOf('\n', at);
                        if (!doc[at..end].Contains(name, StringComparison.Ordinal))
                            problems.Add($"{name}: the {anchor} row of {Conformance} does not quote the name");
                    }
                }
            }
        }
        foreach (var (name, note) in RaisedAtTheActivationBoundary)
        {
            if (!names.Contains(name)) problems.Add($"{name}: not a catalogued level-3 name");
            string file = TestRepo.Kb("Work", note + ".md");
            if (!File.Exists(file) || !File.ReadAllText(file).Contains("status: landed", StringComparison.Ordinal))
                problems.Add($"{name}: kb/Work/{note}.md is not a landed note");
            // The activating statement's profile answers for the name: >>TURN EC-ALL CHECKING ON enables it.
            if (!EcCheckingProfile.FromEvents([(ExceptionCatalog.EcAll, true)]).Enabled(name))
                problems.Add($"{name}: an EcCheckingProfile does not enable it, so the activating statement could not raise it");
        }
        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    /// <summary>The I-O family is declared per FILE a statement touches (<see cref="EcBinder.IoFilesOf"/>), and that list was
    /// the one hand-kept enumeration left in the binder (DELETE FILE was the verb once missing from it, kb/Work PB141). A bound
    /// statement carrying a FileModel is an I-O statement unless it is on this list, and each exemption says why.</summary>
    private static readonly Dictionary<string, string> NotAnIoConnector = new(StringComparer.Ordinal)
    {
        ["BoundSort"] = "File is the SD sort file, not a connector; the USING and GIVING files are (EcBinder.IoFilesOf reads them)",
        ["BoundMerge"] = "File is the SD merge file, not a connector; the USING and GIVING files are",
        ["BoundRelease"] = "File is the SD sort file RELEASE writes to; its conditions are the sort-merge names (EcBinder.NodeKindRaises)",
        ["BoundReturn"] = "File is the SD sort file RETURN reads from; its conditions are the sort-merge names (EcBinder.NodeKindRaises)",
    };

    [Fact]
    public void EveryBoundStatementCarryingAFile_IsAnIoStatementTheBinderDeclares()
    {
        var asm = typeof(BoundStatement).Assembly;
        var problems = new List<string>();
        int checkedTypes = 0;
        foreach (var t in asm.GetTypes().Where(t => t is { IsAbstract: false, IsClass: true } && typeof(BoundStatement).IsAssignableFrom(t)))
        {
            var fileProps = t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.PropertyType == typeof(FileModel)).ToList();
            if (fileProps.Count == 0) continue;
            checkedTypes++;
            if (NotAnIoConnector.ContainsKey(t.Name)) continue;
            var stmt = (BoundStatement)RuntimeHelpers.GetUninitializedObject(t);
            var file = (FileModel)RuntimeHelpers.GetUninitializedObject(typeof(FileModel));
            foreach (var p in fileProps)
                t.GetField($"<{p.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(stmt, file);
            if (!EcBinder.IoFilesOf(stmt).Contains(file))
                problems.Add($"{t.Name} carries a FileModel ({string.Join(", ", fileProps.Select(p => p.Name))}) but EcBinder.IoFilesOf does not "
                    + "name it, so `>>TURN EC-I-O CHECKING ON` over it wraps nothing — add its arm, or exempt it in NotAnIoConnector with the reason");
        }
        Assert.True(checkedTypes >= 10, $"the scan read only {checkedTypes} file-carrying statement types — the reflection scan no longer sees them");
        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }
}
