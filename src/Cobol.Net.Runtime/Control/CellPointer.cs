// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// A data-pointer VALUE: a byte(character)-granular window position inside one <see cref="StorageCell"/>
/// (ISO §8.5.2.6 — a data pointer identifies a storage address; §14.9.39 Format 10 moves it by bytes).
/// Structural equality via <see cref="ManagedPointer.SameTarget"/>: same cell, same offset. The pointer also records the
/// cell's <see cref="StorageCell.Generation"/> at the moment it was taken (kb/Work PB1216): once that life of the
/// storage has ended — FREE, the end of the activation that owned it, a CANCEL — the address is no longer a valid
/// address of storage (§13.18.5.4 GR4), which <c>CobolPtr.Deref</c> tests.
/// </summary>
public sealed class CellPointer(StorageCell cell, long offset, int generation) : ManagedPointer
{
    /// <summary>A pointer into the cell's CURRENT life.</summary>
    public CellPointer(StorageCell cell, long offset) : this(cell, offset, cell.Generation) { }

    /// <summary>The <see cref="StorageCell.Generation"/> this pointer was taken in.</summary>
    public int Generation { get; } = generation;

    /// <summary>The addressed storage cell.</summary>
    public StorageCell Cell { get; } = cell;

    /// <summary>The character-position offset into <see cref="Cell"/> (0-based; byte = character in the
    /// alphanumeric/zoned character model).</summary>
    public long Offset { get; } = offset;
}
