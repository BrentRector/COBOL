// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

/// <summary>
/// ⛔ THE ONE SCREEN OF A PROCEDURE DIVISION HEADER'S FORMAL PARAMETERS AND RETURNING ITEM (kb/Work PB1145) — the rules
/// of ISO §14.2.2 SR1 / SR5 / SR6 and the §14.2.1 using-phrase format, asked once for every source element that has
/// such a header: <c>DataBinder.CallBindLinkage</c> (program and function) and <c>DataBinder.OoBindMethodData</c>
/// (method) each used to carry a private, DIFFERENT subset — the program arm checked REDEFINES / BASED on USING items
/// only, the method arm neither, and neither recorded which names it had already seen, so
/// <c>USING A A</c> reached the backend as a duplicate member (CS0102) and a REDEFINES / BASED formal reached it as a
/// missing one (CS0103). The <c>RaisingPhrase.Partition</c> precedent: the header's phrase rules live in ONE place, and
/// each binder is a caller.
/// <list type="bullet">
/// <item><b>SR1</b> — "A particular user-defined word shall not appear more than once as data-name-1", and the entry
/// "shall not contain a BASED clause or a REDEFINES clause" (<see cref="AdmitFormal"/>).</item>
/// <item><b>SR5</b> — the RETURNING item's entry shall not contain a BASED or REDEFINES clause; <b>SR6</b> — it shall
/// not be the same as a formal (<see cref="CheckReturning"/>).</item>
/// <item><b>§14.2.1</b> — OPTIONAL is printed only in the BY REFERENCE alternative (<see cref="OptionalNeedsByReference"/>).</item>
/// </list>
/// One instance screens one header; the level-01/77-in-the-LINKAGE-SECTION half of SR1 / SR5 is the caller's, because
/// it is decided by the lookup that FINDS the item.
/// </summary>
internal sealed class ProcedureHeaderScreen(EditionContext edition, string where)
{
    private readonly HashSet<DataItem> _formals = new(ReferenceEqualityComparer.Instance);

    private void Refuse(string requirement, string rule) =>
        edition.Error(DiagnosticCatalog.ProcedureHeaderParameter, $"{where}: {requirement} (ISO §14.2.2 {rule})");

    /// <summary>Screen one formal parameter (data-name-1) against SR1. Returns false when the parameter shall not be
    /// admitted as a formal — a repeated one, whose second occurrence would be a second formal of the same item.
    /// A BASED clause is cleared after the report (the flag discipline: the entry binds as an ordinary,
    /// already-diagnosed one, never as a carrier-resident formal over a based backing).</summary>
    public bool AdmitFormal(string written, DataItem item)
    {
        bool admitted = _formals.Add(item);
        if (!admitted)
            Refuse($"data-name-1 '{written}' appears more than once in the USING phrase — \"A particular user-defined "
                + "word shall not appear more than once as data-name-1\"", "SR1");
        if (item.RedefinesTargetName is not null)
            Refuse($"formal parameter '{written}' shall not contain a REDEFINES clause — \"The data description entry "
                + "for data-name-1 shall not contain a BASED clause or a REDEFINES clause\"", "SR1");
        if (item.IsBased)
        {
            Refuse($"formal parameter '{written}' shall not contain a BASED clause — \"The data description entry for "
                + "data-name-1 shall not contain a BASED clause or a REDEFINES clause\"", "SR1");
            item.IsBased = false;
        }
        return admitted;
    }

    /// <summary>Screen the RETURNING item (data-name-2) against SR5 and SR6, after every formal was admitted.</summary>
    public void CheckReturning(string written, DataItem item)
    {
        if (item.RedefinesTargetName is not null)
            Refuse($"RETURNING item '{written}' shall not contain a REDEFINES clause — \"The data description entry "
                + "for data-name-2 shall not contain a BASED clause or a REDEFINES clause\"", "SR5");
        if (item.IsBased)
        {
            Refuse($"RETURNING item '{written}' shall not contain a BASED clause — \"The data description entry for "
                + "data-name-2 shall not contain a BASED clause or a REDEFINES clause\"", "SR5");
            item.IsBased = false;
        }
        if (_formals.Contains(item))
            Refuse($"RETURNING item '{written}' is also a formal parameter — \"Data-name-2 shall not be the same as "
                + "data-name-1\"", "SR6");
    }

    /// <summary>§14.2.2 SR2 — "Each data-name-1 specified in a BY VALUE phrase shall be defined as a data item of class
    /// numeric, message-tag, object, or pointer." The class numeric (fixed-point or floating-point) and the managed
    /// classes (object and the three pointer categories) are the carried legs of the §14.2.3 GR10 detached value copy;
    /// class message-tag is the MCS module (not modeled, so undeclarable). Asked of every BY VALUE formal by BOTH
    /// header arms — the program/function arm and the method arm (kb/Work PB1051) — so the class set is written once.</summary>
    public void ByValueClass(string written, DataItem item)
    {
        if (!(item.IsElementary && item.Pic?.Category is PicCategory.Numeric or PicCategory.Pointer
                or PicCategory.ProgramPointer or PicCategory.FunctionPointer or PicCategory.ObjectReference))
            edition.Error("COBOLNET1553",
                $"{where}: BY VALUE formal parameter '{written}' shall be of class numeric, message-tag, object, "
                + "or pointer (ISO §14.2.2 SR2)");
    }

    /// <summary>§14.2.1's using-phrase prints OPTIONAL only inside the BY REFERENCE alternative
    /// (<c>{ [BY REFERENCE] { [OPTIONAL] data-name-1 }… | BY VALUE { data-name-1 }… }…</c>): after a BY VALUE phrase,
    /// <c>OPTIONAL LY</c> continues the BY VALUE alternative, which has no OPTIONAL.</summary>
    public void OptionalNeedsByReference(string written) =>
        edition.Error(DiagnosticCatalog.ProcedureHeaderParameter,
            $"{where}: formal parameter '{written}' is written OPTIONAL after a BY VALUE phrase — the OPTIONAL word "
            + "belongs to the BY REFERENCE alternative of the using-phrase only (ISO §14.2.1)");
}
