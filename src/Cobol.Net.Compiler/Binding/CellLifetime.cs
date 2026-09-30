// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding;

/// <summary>The §8.6.4 storage duration of a cell-backed (<c>ADDRESS OF</c>-taken) record, which decides the event that
/// ENDS its <c>StorageCell</c>'s life (kb/Work PB1216) — classified once, by <c>DataBinder.LifetimeOfCell</c>.</summary>
internal enum CellLifetime
{
    /// <summary>One copy per run unit / class (a RECURSIVE unit's WORKING-STORAGE, a method's WORKING-STORAGE): ended, and
    /// re-seeded in place, by <c>__ResetStatics</c> at a CANCEL.</summary>
    Static,

    /// <summary>LOCAL-STORAGE and non-formal LINKAGE: ended at the exit of the activation that owns it.</summary>
    Activation,

    /// <summary>A program instance's WORKING-STORAGE: ended by <c>ICobolProgram.EndStorage</c> when the instance is
    /// discarded (an INITIAL program's activation exit, any other program's CANCEL).</summary>
    Instance,
}
