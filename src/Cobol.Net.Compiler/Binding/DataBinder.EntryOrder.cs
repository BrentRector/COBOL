// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Cst;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Model;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// The ORDER in which the data division's records bind (kb/Work PB1231). The binder walks each section's entries in
/// source order, and a record's description is read once, as it is bound. A constant entry, though, may measure a data
/// item described AFTER it — §13.10.3 SR4 ("The length of data-name-1 or data-name-2 shall not be dependent, directly
/// or indirectly, upon the value of constant-name-1") forbids only the circular case, and no clause of §13.10 or §8.4
/// orders the two — and an entry BEFORE that item may need the constant's value: <c>01 A PIC X(K). 01 K CONSTANT AS
/// LENGTH OF W. 01 W PIC X(7).</c> describes A as X(7).
/// <para>So the records bind in DEPENDENCY order and stand in SOURCE order. When a constant's value is demanded and
/// its operand's record has not been reached, that record — its level-01 or level-77 entry and every entry up to the
/// next one, its 66 and 88 entries included — is bound then, through the same <see cref="BindEntries"/> walk, and
/// parked (<see cref="_preboundRecordRoots"/>). The section's own walk, on reaching it, places the parked roots
/// exactly where it would have placed them and skips the entries, so the forest, the file's record list and every
/// later pass see source order. A record that is still being described cannot be split this way: measuring an item
/// later in an OPEN record is <see cref="DiagnosticCatalog.ConstantLengthOperandBoundLater"/>, and measuring an item
/// whose own description is open — the description that referenced the constant — is the SR4 cycle.</para>
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>Where one data description entry stands: the section run it belongs to (the same entry list the
    /// section's walk binds), its index there, and the section.</summary>
    private readonly record struct DataEntryLocation(
        IReadOnlyList<Core.DataDescriptionEntryContext> Run, int Index, EntrySection Section);

    /// <summary>Every data-name a data description entry of this unit declares (levels 01-49, 66 and 77; never a
    /// constant or a condition-name), with each place it is declared.</summary>
    private readonly Dictionary<string, List<DataEntryLocation>> _dataEntryLocations = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The entries whose bind has begun, in or out of source order — a record whose first entry is here is
    /// bound or being bound.</summary>
    private readonly HashSet<Core.DataDescriptionEntryContext> _entriesBound = [];

    /// <summary>One entry whose description is being bound right now: the entry, its walk's level stack and the level
    /// the entry nests at. The stack items of a LOWER level are its ancestors — open groups whose subordinates are
    /// still being described; the stack items at or above it are complete siblings and their subordinates.</summary>
    private sealed record OpenDescription(Core.DataDescriptionEntryContext Entry, Stack<DataItem> Stack, int Level);

    /// <summary>The descriptions being bound right now, outermost first (an out-of-order record bind nests).</summary>
    private readonly List<OpenDescription> _openDescriptions = [];

    /// <summary>The records bound out of source order, by their first entry: the roots the section walk places there.</summary>
    private readonly Dictionary<Core.DataDescriptionEntryContext, List<DataItem>> _preboundRecordRoots = [];

    /// <summary>Every entry of a record bound out of source order — the section walk skips them.</summary>
    private readonly HashSet<Core.DataDescriptionEntryContext> _preboundEntries = [];

    /// <summary>Record where every data description entry under <paramref name="scope"/> stands, BEFORE any of it binds:
    /// the section runs exactly as <see cref="BindFileSection"/>, the WORKING-STORAGE / LOCAL-STORAGE walks and
    /// <see cref="CallBindLinkage"/> hand them to <see cref="BindEntries"/>. (An FD or SD with no file-name binds no
    /// records, so it contributes none.)</summary>
    private void DeclareDataEntryRuns(Core.DataDivisionContext scope)
    {
        if (scope.fileSection() is { } fs)
        {
            foreach (var fd in fs.fileDescriptionEntry())
                if (fd.fileName() is not null) DeclareRun(fd.dataDescriptionEntry(), EntrySection.File);
            foreach (var sd in fs.sortMergeDescriptionEntry())
                if (sd.fileName() is not null) DeclareRun(sd.dataDescriptionEntry(), EntrySection.File);
        }
        if (scope.workingStorageSection() is { } ws) DeclareRun(ws.dataDescriptionEntry(), EntrySection.WorkingStorage);
        if (scope.localStorageSection() is { } ls) DeclareRun(ls.dataDescriptionEntry(), EntrySection.LocalStorage);
        if (scope.linkageSection() is { } lk)
            DeclareRun([.. lk.linkageEntry().Select(e => e.dataDescriptionEntry()).Where(e => e is not null).Select(e => e!)],
                EntrySection.Linkage);
    }

    private void DeclareRun(Core.DataDescriptionEntryContext[] run, EntrySection section)
    {
        for (int i = 0; i < run.Length; i++)
        {
            var e = run[i];
            if (e.dataDescriptionBody()?.constantEntryBody() is not null || EntryLevel(e) == 88) continue;
            if (e.dataName()?.GetText() is not { } name || name.Equals("FILLER", StringComparison.OrdinalIgnoreCase)) continue;
            if (!_dataEntryLocations.TryGetValue(name, out var list)) _dataEntryLocations[name] = list = [];
            if (!list.Any(l => ReferenceEquals(l.Run[l.Index], e)))   // a scope declared twice adds nothing
                list.Add(new DataEntryLocation(run, i, section));
        }
    }

    /// <summary>The entry's level-number, or -1 when it is unreadable (LevelNumberPass has reported it).</summary>
    private static int EntryLevel(Core.DataDescriptionEntryContext e) =>
        int.TryParse(e.levelNumber()?.GetText(), out int level) ? level : -1;

    /// <summary>Does the entry open a record — a level-01 or level-77 data description entry (a constant entry is
    /// level 01 too, but describes no record and ends the one before it)?</summary>
    private static bool OpensRecord(Core.DataDescriptionEntryContext e) =>
        EntryLevel(e) is 1 or 77 && e.dataDescriptionBody()?.constantEntryBody() is null;

    private static bool EndsRecord(Core.DataDescriptionEntryContext e) =>
        EntryLevel(e) is 1 or 77;

    /// <summary>Is <paramref name="name"/> declared by a data description entry whose bind has not begun?</summary>
    private bool IsDescribedLater(string name) =>
        _dataEntryLocations.TryGetValue(name, out var locs) && locs.Any(l => !_entriesBound.Contains(l.Run[l.Index]));

    /// <summary>Is <paramref name="name"/> declared by an entry whose own description is being bound right now?</summary>
    private bool IsBeingDescribed(string name) =>
        _openDescriptions.Any(d => string.Equals(d.Entry.dataName()?.GetText(), name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Is <paramref name="item"/>'s description still open — an ancestor of an entry being bound, so a group
    /// whose subordinates are still being described?</summary>
    private bool IsDescriptionOpen(DataItem item) =>
        _openDescriptions.Any(d => item.Level < d.Level && d.Stack.Contains(item));

    /// <summary>Bind, now and out of source order, every record that declares <paramref name="name"/> and whose bind has
    /// not begun; true when one was bound. The roots are parked for the section walk to place
    /// (<see cref="TakePreboundRecord"/>). A record whose first entry has begun binding is skipped: it is bound
    /// already, or open — and an item later in an open record cannot be measured yet.</summary>
    private bool BindLaterRecords(string name)
    {
        if (!_dataEntryLocations.TryGetValue(name, out var locs)) return false;
        bool any = false;
        foreach (var (run, index, section) in locs.ToList())
        {
            int start = index;
            while (start > 0 && !EndsRecord(run[start])) start--;
            if (!OpensRecord(run[start]) || _entriesBound.Contains(run[start])) continue;
            int end = start + 1;
            while (end < run.Count && !EndsRecord(run[end])) end++;
            var record = run.Skip(start).Take(end - start).ToList();
            var lastRoot = _lastRoot;
            var roots = BindEntries(record, _rootNames, section, outOfOrder: true);
            _lastRoot = lastRoot;
            _preboundRecordRoots[run[start]] = roots;
            _preboundEntries.UnionWith(record);
            any = true;
        }
        return any;
    }

    // ── The REPORT SECTION's entries (kb/Work PB1226, §13.10.3 SR11) ─────────────────────────────────────────────

    /// <summary>Where one report group description entry stands: its RD's entry list, its index there and the
    /// report-name. A report entry is no <see cref="DataItem"/> that <see cref="ReferenceResolver.FindItem"/> sees, so
    /// a constant's length phrase finds it here.</summary>
    private sealed record ReportEntryLocation(Core.ReportGroupEntryContext[] Entries, int Index, string ReportName)
    {
        public Core.ReportGroupEntryContext Entry => Entries[Index];

        /// <summary>An ELEMENTARY report item: no entry follows it at a higher level-number (§13.15 — the level
        /// hierarchy; the replay subtree of <c>BindReportEntries</c> is measured the same way).</summary>
        public bool IsElementary => Index + 1 >= Entries.Length || ReportEntryLevel(Entries[Index + 1]) <= ReportEntryLevel(Entry);

        /// <summary>The names that qualify the entry, innermost first: each named report group description entry it
        /// is subordinate to, then its report-name (§8.4.2.2.3 SR4; §8.4.2.2.2 Format 1's file-report-qualifier).</summary>
        public IEnumerable<string> Qualification
        {
            get
            {
                int level = ReportEntryLevel(Entry);
                for (int j = Index - 1; j >= 0 && level > 1; j--)
                {
                    int l = ReportEntryLevel(Entries[j]);
                    if (l >= level) continue;
                    level = l;
                    if (Entries[j].dataName().NameOrNull() is { } name) yield return name;
                }
                yield return ReportName;
            }
        }
    }

    private static int ReportEntryLevel(Core.ReportGroupEntryContext e) =>
        int.TryParse(e.levelNumber()?.GetText(), out int level) ? level : -1;

    /// <summary>Every named report group description entry of this unit, by name.</summary>
    private readonly Dictionary<string, List<ReportEntryLocation>> _reportEntryLocations = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The report group description entries the report binder has reached.</summary>
    private readonly HashSet<Core.ReportGroupEntryContext> _reportEntriesBound = [];

    /// <summary>Each elementary report item's printable <see cref="DataItem"/> (its first repetition's — every
    /// repetition shares the one description), by its entry.</summary>
    private readonly Dictionary<Core.ReportGroupEntryContext, DataItem> _reportEntryItems = [];

    private void DeclareReportEntries(Core.ReportDescriptionEntryContext rd)
    {
        if (rd.reportName()?.GetText() is not { } report) return;
        var entries = rd.reportGroupEntry();
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].dataName().NameOrNull() is not { } name) continue;
            if (!_reportEntryLocations.TryGetValue(name, out var list)) _reportEntryLocations[name] = list = [];
            if (!list.Any(l => ReferenceEquals(l.Entry, entries[i])))
                list.Add(new ReportEntryLocation(entries, i, report));
        }
    }

    /// <summary>The report group description entries <paramref name="name"/> OF/IN <paramref name="qualifiers"/>
    /// names: each qualifier, in the order written, is a name the entry is subordinate to (§8.4.2.2.3 SR4).</summary>
    private List<ReportEntryLocation> ReportEntryCandidates(string name, IReadOnlyList<string> qualifiers)
    {
        if (!_reportEntryLocations.TryGetValue(name, out var locs)) return [];
        return [.. locs.Where(l =>
        {
            int q = 0;
            foreach (var outer in l.Qualification)
                if (q < qualifiers.Count && outer.Equals(qualifiers[q], StringComparison.OrdinalIgnoreCase)) q++;
            return q == qualifiers.Count;
        })];
    }

    /// <summary>For the section walk: whether <paramref name="entry"/> belongs to a record already bound out of source
    /// order (skip it), and, at the record's first entry, the roots to place there.</summary>
    private bool TakePreboundRecord(Core.DataDescriptionEntryContext entry, out List<DataItem>? roots)
    {
        roots = _preboundRecordRoots.Remove(entry, out var parked) ? parked : null;
        return _preboundEntries.Contains(entry);
    }
}
