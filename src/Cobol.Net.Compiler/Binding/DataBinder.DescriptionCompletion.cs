// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Generic;
using System.Linq;

using CobolNet.Binding.Model;

namespace CobolNet.Binding;

/// <summary>
/// ⛔ A LENGTH PHRASE MEASURES THE COMPLETED DESCRIPTION (kb/Work PB2465). §13.10.4 GR5 / GR6 determine a constant's
/// BYTE-LENGTH OF / LENGTH OF value "as specified in the BYTE-LENGTH intrinsic function" / "the LENGTH intrinsic
/// function", which measure the item AS DESCRIBED — after every clause that decides its size has applied. Four of those
/// clauses apply through the description-completion passes of <see cref="Passes.BindPipeline"/>, which run once over
/// the whole forest after the DATA DIVISION is bound: the §13.16.3 SR9 VALUE-implied PICTURE
/// (<see cref="SynthesizeImpliedPictures"/>), TYPE and SAME AS composition (<see cref="ExpandTypes"/>), group USAGE
/// (§13.18.60.4 GR1, <see cref="UsageInheritancePass"/>) and group SIGN (§13.18.52 GR1, <see cref="InheritSignClauses"/>).
/// A constant entry is bound when the section walk reaches it or when a reference demands its value — both BEFORE the
/// pipeline — so the operand used to be measured half-described: a COMP-5 group's PIC 9(4) leaf 4 bytes where it
/// occupies 2, a record holding a TYPE'd member 1 position where it is 6, a VALUE-implied picture 1 where it is 4, a
/// group SIGN LEADING SEPARATE leaf 3 where it is 4 — and the program compiled and ran on the wrong value.
/// <para><see cref="CompleteDescriptionAhead"/> brings the operand's subtree to its completed description at the
/// moment it is measured, through the SAME bodies those passes run (one rule in one place): the subtree's implied
/// PICTUREs, its TYPE / SAME AS expansions (a type declaration or SAME AS source described later is bound on demand,
/// as a length operand is), and the usage and SIGN walks started at the operand with what its ancestors hand down
/// (<see cref="InheritedUsageAt"/>, <see cref="InheritedSignAt"/>). The subtree is recorded, and the pipeline's walks
/// pass over it (<see cref="UsageInheritanceWalk"/>, <see cref="InheritSignWalk"/>); the other two passes are
/// idempotent. A subtree inside a record whose description is still OPEN (an entry bound ahead of the walk,
/// kb/Work PB1941) completes the same way: its ancestors' clauses are already bound, and they are what it inherits.</para>
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>The subtrees completed ahead of the pipeline — each the operand of a constant's length phrase.</summary>
    private readonly HashSet<DataItem> _completedAhead = new(ReferenceEqualityComparer.Instance);

    /// <summary>The PICTURE profile each entry of a subtree completed ahead had before its ancestors' USAGE and SIGN
    /// reached it — what a clause copy of that entry reads (<see cref="CopyEntryDescription"/>).</summary>
    private readonly Dictionary<DataItem, PicInfo?> _picAsWrittenAhead = new(ReferenceEqualityComparer.Instance);

    /// <summary>A CLAUSE copy (TYPE, SAME AS) of <paramref name="from"/>'s entry: <see cref="CopyEntryDescription"/>, then
    /// the PICTURE profile as WRITTEN when a constant's length phrase completed <paramref name="from"/> ahead. The
    /// pipeline expands every clause before the §13.18.60.4 GR1 / §13.18.52 GR1 walks, so a copy reads the entry as
    /// it stood before its ancestors' USAGE and SIGN reached it: `01 S SAME AS E.` with E under GROUP-USAGE NATIONAL
    /// stays PIC 9(3) DISPLAY (§13.18.49.4 GR3 transfers a usage only from a group S itself is subordinate to),
    /// whether or not `CONSTANT AS BYTE-LENGTH OF E` measured E first (train 1047 review of kb/Work PB2465).</summary>
    private void CopyClauseDescription(DataItem from, DataItem to, DescriptionCopyScope scope)
    {
        bool takesPicture = to.Pic is null;
        CopyEntryDescription(from, to, scope);
        if (takesPicture && _picAsWrittenAhead.TryGetValue(from, out var written)) to.Pic = written;
    }

    /// <summary>Complete <paramref name="item"/>'s description now — the subtree's SR9 implied PICTUREs, TYPE and SAME
    /// AS expansions, and §13.18.60.4 GR1 / §13.18.52 GR1 inheritance from its ancestors — unless it, or a subtree
    /// holding it, is complete already. See the class remarks.</summary>
    private void CompleteDescriptionAhead(DataItem item)
    {
        for (var d = item; d is not null; d = d.Parent)
            if (_completedAhead.Contains(d)) return;
        ComposeAhead(item);
        // The walks below replace each entry's PICTURE profile with the effective one; a later TYPE or SAME AS clause
        // that names an entry of this subtree copies the profile as written (CopyEntryDescription).
        foreach (var d in PreOrder(item)) _picAsWrittenAhead.TryAdd(d, d.Pic);
        UsageInheritanceWalk(item, InheritedUsageAt(item));
        InheritSignWalk(item, InheritedSignAt(item));
        _completedAhead.Add(item);
    }

    /// <summary>The COMPOSITION half of <see cref="CompleteDescriptionAhead"/>: <paramref name="item"/>'s subtree gets its
    /// §13.16.3 SR9 implied PICTUREs and its TYPE and SAME AS expansions, in the pipeline's order. Every step is idempotent
    /// (an expanded clause is marked expanded), so the pipeline passes over what this did.</summary>
    private void ComposeAhead(DataItem item)
    {
        // §13.18.57.4 GR3 makes a type declaration's implied PICTURE part of the description a TYPE clause copies, so
        // the declarations this subtree may copy own theirs first — the pipeline's order (SynthesizeImpliedPictures
        // before ExpandTypes). A declaration whose description is still open is not complete and copies nothing yet.
        foreach (var declaration in TypeDecls.Values.Where(t => !IsDescriptionOpen(t)))
            SynthesizeImpliedPicturesUnder(declaration);
        SynthesizeImpliedPicturesUnder(item);
        // ExpandTypes' order: every TYPE clause, then every SAME AS clause (a SAME AS source copies its expanded TYPE).
        foreach (var typed in PreOrder(item).Where(i => i.TypeRefName is not null).ToList()) ExpandType(typed);
        foreach (var sameAs in PreOrder(item).Where(i => i.SameAsName is not null).ToList()) ExpandSameAs(sameAs, []);
    }

    /// <summary>⛔ A LENGTH OPERAND MAY BE A SUBORDINATE THAT A TYPE OR SAME AS CLAUSE SUPPLIES (kb/Work PB2844).
    /// §13.18.57.4 GR2 a) makes a TYPE'd group's subject "a group whose subordinate elements have the same names,
    /// descriptions, and hierarchy as the subordinate elements of type-name-1" (§13.18.49.4 GR2 a) says the same of SAME
    /// AS), so `01 V TYPE T.` has the subordinate `M OF V` — but those subordinates exist only once the clause is
    /// expanded, and the pipeline expands after the DATA DIVISION is bound, while a constant's length phrase is
    /// evaluated during it. When a length operand's name finds no item, every bound subject whose composition is still
    /// pending (outside a type declaration, which <see cref="ExpandTemplate"/> composes, and outside an open
    /// description, which is not complete) is composed now (<see cref="ComposeAhead"/>); true when one was.</summary>
    private bool ComposePendingSubjectsAhead()
    {
        var pending = _entryItems.Values
            .Where(i => (i.TypeRefName is not null || i.SameAsName is not null)
                && !InTypeDeclaration(i) && !IsDescriptionOpen(i))
            .ToList();
        foreach (var subject in pending) ComposeAhead(subject);
        return pending.Count > 0;
    }

    /// <summary>Bind, out of source order, the type declaration <paramref name="typeName"/> names when the section walk
    /// has not reached it — a TYPE clause expanded ahead of the pipeline (<see cref="CompleteDescriptionAhead"/>) may
    /// name one declared later, which §13.18.57 permits — and give it its §13.16.3 SR9 implied PICTUREs, which it must
    /// own before it is copied. False when nothing was bound; after the walk nothing is described later, so in the
    /// pipeline this is a no-op.</summary>
    private bool BindLaterTypeDeclaration(string typeName)
    {
        if (!IsDescribedLater(typeName) || !BindLaterRecords(typeName)) return false;
        if (TryFindTypeDecl(typeName, out var declaration)) SynthesizeImpliedPicturesUnder(declaration);
        return true;
    }
}
