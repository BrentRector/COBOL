using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>kb/Work PB978 — the DATA DIVISION resolvers bound the FIRST candidate of an ambiguous reference: the
/// report binder's CONTROL / SOURCE / SUM lookup, the OCCURS DEPENDING ON fallback, the file-control key and ASSIGN
/// USING operands and the RENAMES operands each read <c>[0]</c> (or <c>FirstOrDefault()</c>) off a candidate list, so
/// source ISO §8.4.2.2.3 SR1 refuses compiled clean and ran on whichever item was declared first. The fix is a TYPE:
/// <see cref="NameCandidates{T}"/> can be counted, narrowed and read only when it holds exactly one item, and the
/// first-declared member is granted by the ONE ambiguity verdict alone, under <c>--permissive</c>. These tests keep
/// both halves true.</summary>
[Trait("Sweep", "tree")]
public sealed class DataNameResolutionDriftTests
{
    /// <summary>The candidate set offers no way to read "the first" — no indexer, no enumeration, no public list.</summary>
    [Fact]
    public void TheCandidateSet_CannotBeIndexedOrEnumerated()
    {
        var t = typeof(NameCandidates<DataItem>);
        Assert.False(typeof(IEnumerable).IsAssignableFrom(t), "NameCandidates must not be enumerable (FirstOrDefault would return)");
        Assert.DoesNotContain(t.GetProperties(BindingFlags.Public | BindingFlags.Instance), p => p.GetIndexParameters().Length > 0);
        Assert.DoesNotContain(t.GetProperties(BindingFlags.Public | BindingFlags.Instance),
            p => typeof(IEnumerable).IsAssignableFrom(p.PropertyType) && p.PropertyType != typeof(string));
        var qc = typeof(DataBinder).GetMethod("QualifiedCandidates", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(qc);
        Assert.Equal(t, qc!.ReturnType);
        var sc = typeof(DataBinder).GetMethod("SubtreeCandidates", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(sc);
        Assert.Equal(t, sc!.ReturnType);
    }

    private static readonly Regex PermissiveRead = new(@"\.FirstDeclaredForPermissive\b", RegexOptions.Compiled);

    /// <summary>The first-declared survivor is read by <c>DataBinder.UniqueOrReportAmbiguous</c> only.</summary>
    [Fact]
    public void TheFirstDeclaredSurvivor_IsReadOnlyByTheOneAmbiguityVerdict()
    {
        var root = TestRepo.Src("Cobol.Net.Compiler");
        var files = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                     && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .ToList();
        Assert.True(files.Count > 50, $"the scan found only {files.Count} source files under {root} — it measured nothing");
        var readers = files.SelectMany(f => File.ReadAllLines(f).Select((l, i) => (f, l, i)))
            .Where(x => PermissiveRead.IsMatch(x.l))
            .Select(x => $"{Path.GetFileName(x.f)}:{x.i + 1}")
            .ToList();
        Assert.True(readers.Count == 1 && readers[0].StartsWith("DataBinder.cs:", StringComparison.Ordinal),
            "FirstDeclaredForPermissive is read outside DataBinder.UniqueOrReportAmbiguous: " + string.Join(", ", readers)
            + " — an ambiguous data-name reference is §8.4.2.2.3 SR1's error, and only the ONE verdict may apply the "
            + "--permissive first-declared disposition (kb/Work PB978).");
    }

    /// <summary>A candidate list taken straight off the name index is never indexed by <c>[0]</c> in the DATA
    /// DIVISION binder: <c>Symbols.TryResolve(…, out var X)</c> / <c>ByName.TryGetValue(…, out var X)</c> followed by
    /// <c>X[0]</c>. The one named exemption forces storage in a pre-pass and binds nothing (DataBinder.Ptr — the
    /// SET statement's own resolution reports the ambiguity).</summary>
    [Fact]
    public void NoDataDivisionResolver_IndexesARawCandidateList()
    {
        var dir = TestRepo.Src("Cobol.Net.Compiler", "Binding");
        var files = Directory.EnumerateFiles(dir, "DataBinder*.cs").ToList();
        Assert.True(files.Count >= 5, $"the scan found only {files.Count} DataBinder files — it measured nothing");
        var findings = new List<string>();
        foreach (var f in files.Where(f => !f.EndsWith("DataBinder.Ptr.cs", StringComparison.Ordinal)))
            findings.AddRange(RawIndexings(File.ReadAllText(f)).Select(h => $"{Path.GetFileName(f)}: {h}"));
        Assert.True(findings.Count == 0, "a DATA DIVISION resolver reads [0] off a raw candidate list:\n  "
            + string.Join("\n  ", findings) + "\nResolve through QualifiedCandidates / SubtreeCandidates and "
            + "UniqueOrReportAmbiguous (kb/Work PB978).");
    }

    private static readonly Regex RawList = new(@"(?:TryResolve|TryGetValue)\([^;]*?out\s+var\s+(\w+)\)", RegexOptions.Compiled);

    private static IEnumerable<string> RawIndexings(string source)
    {
        // Code only: a comment QUOTING the retired first-match line is history, not a resolver.
        string text = string.Join("\n", source.Split('\n').Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)));
        foreach (Match m in RawList.Matches(text))
        {
            string v = m.Groups[1].Value;
            if (Regex.IsMatch(text, $@"\b{Regex.Escape(v)}\s*\[\s*0\s*\]")) yield return $"'{v}[0]' after {m.Value}";
        }
    }

    /// <summary>The raw-list scan's failure branch, fired once on the exact pre-PB978 ODO fallback.</summary>
    [Fact]
    public void TheRawListScan_ActuallyFails_OnThePb978OdoFallback()
    {
        const string pre = """
            if (!Symbols.TryResolve(depName, ScopeOf(RootOf(item)), out var cands))
            {
                continue;
            }
            dep = cands[0];
            """;
        Assert.Single(RawIndexings(pre));
    }
}
