// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A PHYSICAL FILE'S IDENTITY TO THE RUN UNIT IS ITS ABSOLUTE HOST PATH UNDER THE HOST'S CASE RULE (kb/Work PB2748).
/// Table 19's arbitration (§9.1.15: <i>"the sharing mode and the open mode of that OPEN statement shall be allowed by
/// all other file connectors that are currently associated with the physical file"</i>) and the keyed record stores
/// are keyed on the physical file. They were keyed on the verbatim ASSIGN text compared case-insensitively, so two
/// spellings of one file (<c>data.dat</c>, <c>./data.dat</c>, the full path) were two physical files, and on a
/// case-sensitive host two files (<c>Cust.dat</c>, <c>cust.dat</c>) were one: the second OPEN OUTPUT answered '61'
/// against a file it never shared, and a keyed one killed the run unit in the shared store table.
/// <see cref="CobolFile.ResolveHostPath"/> now makes the path absolute and <see cref="HostFile.PhysicalFileComparer"/>
/// carries the case rule; the structural half keeps every table keyed on the physical file on that comparer.
/// </summary>
public sealed class PhysicalFileIdentityDriftTests
{
    [Fact]
    public void TwoSpellingsOfOneFile_ResolveToOneHostPath()
    {
        string dir = Path.GetTempPath();
        string plain = CobolFile.ResolveHostPath(Path.Combine(dir, "pb2748-id.dat"));
        Assert.True(Path.IsPathFullyQualified(plain), plain);
        Assert.Equal(plain, CobolFile.ResolveHostPath(Path.Combine(dir, "pb2748-sub", "..", "pb2748-id.dat")));
        Assert.Equal(plain, CobolFile.ResolveHostPath(Path.Combine(dir, ".", "pb2748-id.dat")));
        Assert.Equal(Path.Combine(Environment.CurrentDirectory, "pb2748-id.dat"), CobolFile.ResolveHostPath("pb2748-id.dat"));
        Assert.Equal(Path.Combine(Environment.CurrentDirectory, "custmast.txt"), CobolFile.ResolveHostPath("CUSTMAST"));
        Assert.Equal("", CobolFile.ResolveHostPath(""));
    }

    [Fact]
    public void TheCaseRule_IsTheHostFileSystems()
    {
        bool sameFile = HostFile.PhysicalFileComparer.Equals("/d/Cust.dat", "/d/cust.dat");
        Assert.Equal(OperatingSystem.IsWindows() || OperatingSystem.IsMacOS(), sameFile);
    }

    /// <summary>On a case-sensitive host, two files whose names differ only in case are two physical files: each
    /// OPEN OUTPUT succeeds, and each keyed file keeps its own records.</summary>
    [Fact]
    public void OnACaseSensitiveHost_TwoFilesDifferingInCase_AreTwoPhysicalFiles()
    {
        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()) return;
        string dir = Path.Combine(Path.GetTempPath(), $"pb2748-case-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            var reg = new FileRegistry();
            foreach (var (name, file) in new[] { ("U", "Cust.dat"), ("L", "cust.dat") })
                reg.RegisterIndexed(name, Path.Combine(dir, file), recordWidth: 8, optional: false,
                    accessMode: (int)KeyedAccess.Dynamic, primeOffset: 0, primeLength: 4, varyMin: -1, varyMax: -1);
            reg.OpenStatic("U", FileOpenMode.Output);
            reg.OpenStatic("L", FileOpenMode.Output);
            Assert.Equal("00", reg.Status("U"));
            Assert.Equal("00", reg.Status("L"));
            Assert.Equal("00", reg.WriteShared("U", "0001UUUU", -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
            Assert.Equal("00", reg.WriteShared("L", "0001LLLL", -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
            reg.Close("U");
            reg.Close("L");
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch (IOException) { } }
    }

    /// <summary>⛔ THE STRUCTURAL HALF: every table keyed on the physical file uses the one comparer, and no file under
    /// <c>Cobol.Net.Runtime/IO</c> compares a host path with a comparer of its own.</summary>
    [Fact]
    public void EveryPhysicalFileTable_UsesTheOneComparer()
    {
        foreach (var file in new[] { TestRepo.Src("Cobol.Net.Runtime", "IO", "KeyedStoreTable.cs"),
                     TestRepo.Src("Cobol.Net.Runtime", "IO", "Sharing", "PhysicalFileTable.cs") })
        {
            string byHost = File.ReadAllLines(file).Single(l => l.Contains("_byHost = new(", StringComparison.Ordinal));
            Assert.Contains("HostFile.PhysicalFileComparer", byHost, StringComparison.Ordinal);
        }
        string io = TestRepo.Src("Cobol.Net.Runtime", "IO");
        var offenders = Directory.EnumerateFiles(io, "*.cs", SearchOption.AllDirectories)
            .SelectMany(f => File.ReadAllLines(f).Select((l, i) => (f, i, l)))
            .Where(t => t.l.Contains("HostPath", StringComparison.Ordinal) && !t.l.TrimStart().StartsWith("//", StringComparison.Ordinal)
                && (t.l.Contains("StringComparison.", StringComparison.Ordinal) || t.l.Contains("StringComparer.", StringComparison.Ordinal)))
            .Select(t => $"{Path.GetRelativePath(io, t.f)}:{t.i + 1}: {t.l.Trim()}")
            .ToList();
        Assert.True(offenders.Count == 0, "A host path compared outside HostFile.PhysicalFileComparer:\n  " + string.Join("\n  ", offenders));
    }
}
