// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Cst;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Model;

namespace CobolNet.Binding;

using CobolNet.Runtime;
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
/// later pass see source order. An item later in a record that is still being described is bound the same way at the
/// granularity of its own entry (<see cref="BindLaterEntriesOfOpenRecord"/>, kb/Work PB1941): the entry, its
/// subordinates and each group entry between it and its nearest bound ancestor bind now, and the walk attaches them
/// where they stand — an entry SUBORDINATE to the entry whose OCCURS bound demanded the constant included, since that
/// entry's item exists before its table bounds bind (<see cref="BindTableBounds"/>). Measuring an item whose own
/// description is open — the description that referenced the constant — is the SR4 cycle.</para>
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>Where one data description entry stands: the section run it belongs to (the same entry list the
    /// section's walk binds), its index there, and the section.</summary>
    private readonly record struct DataEntryLocation(
        IReadOnlyList<Core.DataDescriptionEntryContext> Run, int Index, EntrySection Section);

    /// <summary>Every data-name a data description entry of this unit declares (levels 01-49, 66 and 77; never a
    /// constant or a condition-name), with each place it is declared.</summary>
    private readonly Dictionary<string, List<DataEntryLocation>> _dataEntryLocations = new(CobolNames.Comparer);

    /// <summary>The entries whose bind has begun, in or out of source order — a record whose first entry is here is
    /// bound or being bound.</summary>
    private readonly HashSet<Core.DataDescriptionEntryContext> _entriesBound = [];

    /// <summary>One entry whose description is being bound right now: the entry, its walk's level stack and the level
    /// the entry nests at. The stack items of a LOWER level are its ancestors — open groups whose subordinates are
    /// still being described; the stack items at or above it are complete siblings and their subordinates.</summary>
    private sealed record OpenDescription(Core.DataDescriptionEntryContext Entry, Stack<DataItem> Stack, int Level)
    {
        /// <summary>The entry's own item, once it exists while its description is still open: while its table bounds
        /// bind (<see cref="BindTableBounds"/>).</summary>
        public DataItem? Item { get; set; }
    }

    /// <summary>The descriptions being bound right now, outermost first (an out-of-order record bind nests).</summary>
    private readonly List<OpenDescription> _openDescriptions = [];

    /// <summary>The records bound out of source order, by their first entry: the roots the section walk places there.</summary>
    private readonly Dictionary<Core.DataDescriptionEntryContext, List<DataItem>> _preboundRecordRoots = [];

    /// <summary>Every entry of a record bound out of source order — the section walk skips them.</summary>
    private readonly HashSet<Core.DataDescriptionEntryContext> _preboundEntries = [];

    /// <summary>The item each data description entry bound to, by its entry — how an entry bound ahead of its
    /// record's walk finds the already-bound item it is subordinate to (<see cref="BindLaterEntriesOfOpenRecord"/>).</summary>
    private readonly Dictionary<Core.DataDescriptionEntryContext, DataItem> _entryItems = [];

    /// <summary>The entries of an OPEN record bound ahead of its walk (kb/Work PB1941), by entry: the item the walk
    /// attaches when it reaches the entry (an operand's own entry, or a group entry it is subordinate to), or null
    /// for an entry bound as part of an operand's subordinate run, which the walk skips.</summary>
    private readonly Dictionary<Core.DataDescriptionEntryContext, DataItem?> _preboundEntryItems = [];

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
            DeclareRun(lk.dataDescriptionEntry(), EntrySection.Linkage);
    }

    private void DeclareRun(Core.DataDescriptionEntryContext[] run, EntrySection section)
    {
        for (int i = 0; i < run.Length; i++)
        {
            var e = run[i];
            if (e.dataDescriptionBody()?.constantEntryBody() is not null || EntryLevel(e) == 88) continue;
            if (e.dataName()?.GetText() is not { } name || CobolNames.Same(name, "FILLER")) continue;
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
        _openDescriptions.Any(d => CobolNames.Same(d.Entry.dataName()?.GetText(), name));

    /// <summary>Is <paramref name="item"/>'s description still open — the item of the entry being bound, or an ancestor
    /// of it, so a group whose subordinates are still being described?</summary>
    private bool IsDescriptionOpen(DataItem item) =>
        _openDescriptions.Any(d => ReferenceEquals(d.Item, item) || (item.Level < d.Level && d.Stack.Contains(item)));

    /// <summary>The OCCURS clause of each entry whose item exists and whose table bounds are not yet bound
    /// (<see cref="DataItem.TableBoundsPending"/>).</summary>
    private readonly Dictionary<DataItem, Core.OccursClauseContext> _pendingTableBounds = new(ReferenceEqualityComparer.Instance);

    /// <summary>⛔ AN ENTRY'S TABLE BOUNDS BIND AFTER ITS ITEM EXISTS (kb/Work PB1941). <c>05 A3 OCCURS K3. 10 W3 PIC
    /// X(4).</c> with <c>01 K3 CONSTANT AS LENGTH OF W3 (1).</c>: A3's OCCURS bound demands K3, and K3 measures W3, an
    /// entry subordinate to A3. §13.10.3 SR4 ("The length of data-name-1 or data-name-2 shall not be dependent,
    /// directly or indirectly, upon the value of constant-name-1") is not violated — an element's length does not
    /// depend on the occurrence count — so W3 binds ahead of the walk (<see cref="BindLaterEntriesOfOpenRecord"/>)
    /// under A3's item, which therefore exists, linked into its record and known as a table
    /// (<see cref="DataItem.TableBoundsPending"/>), before the bounds are evaluated. A3's description stays open
    /// meanwhile (<see cref="IsDescriptionOpen"/>): a constant that measures A3 or an ancestor is the SR4 cycle.
    /// <para>Each bound is an integer literal or an integer constant-name (§13.10.3 SR2), read by
    /// <see cref="IntegerOperandValue"/>. The table is allocated at its MAXIMUM occurrence count — the last fixed
    /// bound (integer-2 of a Format 2 table, the sole bound of a fixed one) — per §8.5.1.8; the minimum, DEPENDING,
    /// KEY and dynamic-capacity surface is the <see cref="OccursSpec"/>.</para></summary>
    private void BindTableBounds(DataItem item, OpenDescription description)
    {
        if (!_pendingTableBounds.Remove(item, out var occ)) return;
        description.Item = item;
        string where = $"data item '{item.CobolName ?? "FILLER"}'";
        int? occurs = occ.integerOperand() is { Length: > 0 } bounds && IntegerOperandValue(bounds[^1], where) is { } n
            ? n : null;
        item.BindTableBounds(occurs, OdoBindOccursSpec(occ, where, occurs));
    }

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

    /// <summary>⛔ PART OF AN OPEN RECORD, BOUND AHEAD OF ITS WALK (kb/Work PB1941). <c>01 R. 05 A PIC X(K). 05 W PIC
    /// X(7). 01 K CONSTANT AS LENGTH OF W.</c> — A's PICTURE demands K while R is being described, and W is a later
    /// entry of R. §13.10.3 SR4 ("The length of data-name-1 or data-name-2 shall not be dependent, directly or
    /// indirectly, upon the value of constant-name-1") is not violated: W's length does not depend on K, so W can be
    /// measured before the walk reaches it.
    /// <para>For every entry declaring <paramref name="name"/> whose bind has not begun inside a record whose walk HAS
    /// begun: the entry and its subordinate entries are bound now, through the same <see cref="BindEntries"/> walk,
    /// and so is each group entry between it and its nearest already-bound ancestor (that entry alone — its other
    /// subordinates are the walk's). Each item links to its parent at once, so qualification, the OCCURS depth and the
    /// ancestor screens see the real chain, and the walk ATTACHES it when it reaches the entry
    /// (<see cref="TakePreboundEntry"/>), so the parent's members keep source order. A description that does depend
    /// on the constant meets the constant's own cycle check while it binds here — the SR4 violation, reported as such.</para>
    /// <para>An entry subordinate to the entry whose description is being bound right now links to that entry's item,
    /// which exists while the entry's table bounds bind (<see cref="BindTableBounds"/>) — the one place an entry with
    /// subordinates may demand a constant. Before the item exists only the elementary-only clauses read a constant
    /// (PICTURE, VALUE, DYNAMIC LENGTH), so there an entry with a subordinate has no ancestor to link to and its own
    /// clause rule reports the source.</para></summary>
    /// <returns>True when at least one entry was bound.</returns>
    private bool BindLaterEntriesOfOpenRecord(string name)
    {
        if (!_dataEntryLocations.TryGetValue(name, out var locs)) return false;
        bool any = false;
        foreach (var (run, index, section) in locs.ToList())
        {
            if (_entriesBound.Contains(run[index])) continue;
            int start = index;
            while (start > 0 && !EndsRecord(run[start])) start--;
            if (!OpensRecord(run[start]) || !_entriesBound.Contains(run[start])) continue;   // not begun: BindLaterRecords
            if (UnboundAncestry(run, start, index) is not ({ } parent, var chain)) continue;
            foreach (int j in chain)
            {
                // The operand's own entry carries its subordinate run; an ancestor group entry binds alone.
                int end = j == index ? SubordinateRunEnd(run, j) : j + 1;
                var entries = run.Skip(j).Take(end - j).ToList();
                BindEntries(entries, _rootNames, section, aheadOfWalkParent: parent);
                if (!_entryItems.TryGetValue(run[j], out var bound)) break;   // the entry described no item (reported)
                _preboundEntryItems[run[j]] = bound;
                foreach (var subordinate in entries.Skip(1)) _preboundEntryItems[subordinate] = null;
                if (j != index) _preboundGroups[bound] = new DataEntryLocation(run, j, section);
                parent = bound;
                any = true;
            }
        }
        return any;
    }

    /// <summary>The group entries bound ALONE ahead of the walk (<see cref="BindLaterEntriesOfOpenRecord"/>) whose
    /// other subordinates are not bound yet, so whose description is not complete.</summary>
    private readonly Dictionary<DataItem, DataEntryLocation> _preboundGroups = new(ReferenceEqualityComparer.Instance);

    /// <summary>Complete a group bound alone ahead of the walk before it is measured: bind its subordinate entries now,
    /// under it and in source order (an entry already bound ahead attaches at its position), so its description is
    /// whole. The walk then skips those entries and attaches the group itself where it stands. A subordinate whose
    /// description depends on the constant being evaluated meets that constant's cycle check — §13.10.3 SR4, since the
    /// group's length then depends on it.</summary>
    private void CompletePreboundGroup(DataItem group)
    {
        if (!_preboundGroups.Remove(group, out var at)) return;
        int end = SubordinateRunEnd(at.Run, at.Index);
        var entries = at.Run.Skip(at.Index + 1).Take(end - at.Index - 1).ToList();
        BindEntries(entries, _rootNames, at.Section, openGroup: group);
        foreach (var subordinate in entries)
        {
            _preboundEntryItems[subordinate] = null;
            if (_entryItems.TryGetValue(subordinate, out var nested)) _preboundGroups.Remove(nested);
        }
    }

    /// <summary>The nearest already-bound item the entry at <paramref name="index"/> is subordinate to, and the indices
    /// of the entries from the outermost unbound ancestor down to the entry itself; null when an ancestor's bind has
    /// begun but produced no item yet — the entry being described right now — or produced none at all.</summary>
    private (DataItem Parent, List<int> Chain)? UnboundAncestry(
        IReadOnlyList<Core.DataDescriptionEntryContext> run, int start, int index)
    {
        var chain = new List<int> { index };
        int level = EntryLevel(run[index]);
        for (int j = index - 1; j >= start; j--)
        {
            int l = EntryLevel(run[j]);
            if (l is < 1 or 66 or 88 || run[j].dataDescriptionBody()?.constantEntryBody() is not null) continue;
            int nest = l == 77 ? 1 : l;
            if (nest >= level) continue;
            if (_entryItems.TryGetValue(run[j], out var bound)) return (bound, chain);
            if (_entriesBound.Contains(run[j])) return null;
            chain.Insert(0, j);
            level = nest;
        }
        return null;
    }

    /// <summary>One past the last entry subordinate to the entry at <paramref name="index"/>: the next entry at its
    /// level or a lower one, a level-66 entry (which follows the record's last data description entry, §13.18.45.3
    /// SR2) or the next record. Its level-88 entries are inside the run.</summary>
    private static int SubordinateRunEnd(IReadOnlyList<Core.DataDescriptionEntryContext> run, int index)
    {
        int level = EntryLevel(run[index]);
        int end = index + 1;
        while (end < run.Count && !EndsRecord(run[end]) && EntryLevel(run[end]) is var l && l != 66 && (l == 88 || l > level))
            end++;
        return end;
    }

    /// <summary>For the section walk: whether <paramref name="entry"/> was bound ahead of the walk as part of an open
    /// record (<see cref="BindLaterEntriesOfOpenRecord"/>) — with the item to attach at its source position, or null
    /// for an entry bound inside an earlier entry's subordinate run.</summary>
    private bool TakePreboundEntry(Core.DataDescriptionEntryContext entry, out DataItem? item) =>
        _preboundEntryItems.Remove(entry, out item);

    // ── The REPORT SECTION's entries (kb/Work PB1226, §13.10.3 SR11) ─────────────────────────────────────────────

    /// <summary>Where one report group description entry stands: its RD's entry list, its index there and the
    /// report-name. A report entry is no <see cref="DataItem"/> that <see cref="ReferenceResolver.FindItem"/> sees, so
    /// a constant's length phrase finds it here, and its description is read from here
    /// (<see cref="DescribeReportItem"/>) by the report walk and the length phrase alike.</summary>
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
    private readonly Dictionary<string, List<ReportEntryLocation>> _reportEntryLocations = new(CobolNames.Comparer);

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
                if (q < qualifiers.Count && CobolNames.Same(outer, qualifiers[q])) q++;
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
