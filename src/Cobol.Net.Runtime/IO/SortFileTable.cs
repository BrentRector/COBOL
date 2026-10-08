// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>The procedure phase of an executing SORT/MERGE (see <see cref="SortStore.Phase"/>). <c>None</c> covers
/// every part of the statement that runs no program procedure: the USING/GIVING transfers and the sequence
/// phase — a USE declarative that runs from an implicit transfer is not in the range of either procedure.</summary>
internal enum ProcedurePhase { None, Input, Output }

/// <summary>The per-SD store of one executing SORT/MERGE statement: released images (in release order — the
/// stability anchor ISO §14.9.40.4 GR3 requires), the USING stream boundaries for MERGE, and the return cursor.</summary>
internal sealed class SortStore
{
    /// <summary>Each released record with the extent table it was released with (D-FRA (v); kb/Work PB1053).</summary>
    public readonly List<StoredFrame> Records = [];
    public readonly List<int> StreamStarts = [];   // MERGE: index where each USING file's records begin
    public int Cursor;
    public int LastReturnedLength;
    /// <summary>The extent table of the most recently RETURNed record — null when it carries none.</summary>
    public RecordExtents? LastReturnedExtents;
    /// <summary>The ALPHANUMERIC collating sequence snapshotted at statement start (ISO §14.6.6 r5) — see
    /// <see cref="CobolSort.Init(string, CobolCollation?, CobolCollation?)"/>.</summary>
    public CobolCollation? Collation;
    /// <summary>Its NATIONAL twin — GR5 determines the two sequences SEPARATELY, so they snapshot
    /// separately and a statement may carry one, both or neither.</summary>
    public CobolCollation? NatCollation;
    /// <summary>Which procedure of the executing SORT/MERGE statement is running — the state §14.9.32.4 GR1
    /// ("within the range of an input procedure being executed by a SORT statement that references the
    /// file-name") and §14.9.34.4 GR1 ("within the range of an output procedure being executed by a MERGE or
    /// SORT statement that references file-name-1") test. The store exists only between
    /// <see cref="CobolSort.Init(string, CobolCollation?, CobolCollation?)"/> and <see cref="CobolSort.Close"/>, so
    /// "no store" is "no statement executing".</summary>
    public ProcedurePhase Phase;
    /// <summary>§14.9.34.4 GR3's latch: the at end condition has occurred for this file in the current output
    /// procedure, so a further RETURN is EC-SORT-MERGE-RETURN.</summary>
    public bool AtEndReached;
}

/// <summary>⛔ THE RUN UNIT'S SORT-MERGE FILE STORES, keyed by COBOL file-name (kb/Work PB1570; the owner is
/// <see cref="RunUnit.SortFiles"/>). A sort-merge file's store exists only while a SORT/MERGE statement of THIS run
/// unit executes (ISO §14.9.40 GR9 phases; §9 sort-merge file model), and the -ACTIVE rules (§14.9.40.4 GR10 / GR13,
/// §14.9.24.4 GR8) name the procedures of "an executing SORT/MERGE statement" — a statement of the same run unit:
/// "A run unit is an independent entity that may be executed without communicating with, or being coordinated with,
/// any other run unit except that it may communicate via messages with other run units, process files, and set and
/// test switches" (§14.6.1), and a sort-merge file is none of those. The table used to be one process-wide static
/// <c>Dictionary</c> on <see cref="CobolSort"/>, so two run units in one process (the <see cref="RunUnit.Run"/> /
/// <see cref="RunUnit.Begin"/> host shape, or two concurrent hosts) shared and mutated each other's sort files, an
/// abandoned statement's store outlived its run unit, and one run unit's procedure phase could raise
/// EC-SORT-MERGE-ACTIVE in another. A run unit executes on one logical thread (<see cref="RunUnit"/>), so the
/// table needs no lock; a new run unit is a new object, so it starts with no store.</summary>
public sealed class SortFileTable
{
    private readonly Dictionary<string, SortStore> _stores = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The store of <paramref name="name"/>, created empty when no statement has begun on it.</summary>
    internal SortStore Get(string name)
    {
        if (!_stores.TryGetValue(name, out var s)) _stores[name] = s = new SortStore();
        return s;
    }

    /// <summary>The store of <paramref name="name"/> when a statement has begun on it, else null.</summary>
    internal SortStore? Find(string name) => _stores.GetValueOrDefault(name);

    /// <summary>Drop the store of <paramref name="name"/> — the statement ended (<see cref="CobolSort.Close"/>).</summary>
    internal void Remove(string name) => _stores.Remove(name);

    /// <summary>Is a procedure of an executing SORT/MERGE statement of this run unit — in one of
    /// <paramref name="phases"/> — running, whatever file it names?</summary>
    internal bool AnyInPhase(ReadOnlySpan<ProcedurePhase> phases)
    {
        foreach (var store in _stores.Values)
            if (phases.Contains(store.Phase))
                return true;
        return false;
    }
}
