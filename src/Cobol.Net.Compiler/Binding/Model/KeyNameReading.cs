// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>One declaration a word written where a general format prints <c>data-name-1</c> OR
/// <c>record-key-name-1</c> can name — the READ and START KEY phrases (ISO §14.9.30.2, §14.9.41.2) and the key-level
/// COLLATING SEQUENCE clause (§12.4.5.7.2 Format 2). ISO §8.3.2.2 lets one word be both a data-name and a
/// record-key-name, and a record-key-name may be declared by more than one file; §8.4.2.2.1 then requires
/// qualification "unless … No other name has the identical spelling", and §8.4.2.2.3 SR1 makes a reference that
/// qualification leaves non-unique non-conforming. So every reading counts in ONE
/// <see cref="NameCandidates{T}"/> set and meets the one ambiguity verdict
/// (<see cref="DataBinder.UniqueOrReportAmbiguous{T}"/>) — never "the record-key-name first" (kb/Work PB1099 review
/// R1-2, the PB978 first-candidate shape).</summary>
public abstract record KeyNameReading
{
    private KeyNameReading() { }

    /// <summary>A record-key-name (§8.3.2.2.24) declared by a SOURCE phrase of <paramref name="File"/>'s file control
    /// entry: the key at <paramref name="KeyIndex"/> (−1 = the prime key, i = the i-th alternate).</summary>
    public sealed record RecordKeyName(FileModel File, int KeyIndex) : KeyNameReading;

    /// <summary>A data item the same reference identifies as a data-name.</summary>
    public sealed record DataName(DataItem Item) : KeyNameReading;
}
