// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// WHY a <see cref="StorageCell"/>'s life ended (kb/Work PB1216). ISO §13.18.5.4 GR4 makes EC-BOUND-PTR the answer to a
/// reference "while its address is not NULL and not a valid address of storage", and §8.6.5 says the association of a
/// pointer with its storage "may cease to exist because the actual data no longer exists, as specified 8.6.4". §8.6.4
/// names three ways data stops existing that this implementation models on a cell — and FREE (§14.9.15.4 GR1a), the
/// fourth, which only an ALLOCATEd area has. The reason travels into the EC-BOUND-PTR detail so the diagnostic says
/// which rule the dangling reference broke.
/// </summary>
public enum StorageEnd : byte
{
    /// <summary>The storage has not ended (never the reason of an <see cref="StorageCell.End"/> call).</summary>
    None = 0,

    /// <summary>Released by FREE (§14.9.15.4 GR1a).</summary>
    Freed,

    /// <summary>The runtime element's activation returned: a LOCAL-STORAGE item persists only "while that instance of the
    /// runtime element is in active state" (§8.6.4), and an INITIAL program's items "while the program is in active
    /// state".</summary>
    ActivationEnded,

    /// <summary>A CANCEL of the program that contains the item (§8.6.4: a static item persists until "the execution of a
    /// CANCEL statement of a program that directly or indirectly contains the items"; §14.9.5.4 GR3).</summary>
    Cancelled,
}
