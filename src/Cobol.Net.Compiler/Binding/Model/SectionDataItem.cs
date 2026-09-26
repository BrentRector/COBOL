// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>
/// ⛔ <b>"A DATA ITEM DEFINED IN THE FILE, WORKING-STORAGE, LOCAL-STORAGE, OR LINKAGE SECTION"</b> — the phrase
/// the activation statements write in the same words five times: CALL's identifier-2 (ISO §14.9.4.3 SR3) and
/// RETURNING item (SR7), INVOKE's identifier-3 (§14.9.23.3 SR9) and RETURNING item (SR11), and, through GR9 a)
/// (CALL) and GR6 a) (INVOKE), the test that decides whether a keyword-less argument is assumed BY REFERENCE or
/// BY CONTENT (kb/Work PB1137). One question, answered here once, from what a resolved <see cref="Place"/> IS.
/// <para>A place answers NO when it is not storage the program's DATA DIVISION declares: a special register the
/// standard describes implicitly (a report's PAGE-COUNTER, a REPORT SECTION sum counter, the '85 DEBUG-ITEM, the
/// predefined EXCEPTION-OBJECT) or a temporary the compiler synthesizes for an object property, an inline method
/// invocation or a function's returned value (<see cref="DataItem.IsCompilerTemp"/> on the record root — the ONE
/// temp constructor sets it). Everything else a resolved reference can reach was written in one of the four
/// sections: a SCREEN SECTION name never resolves to a place (CallBinder.OperandUnresolved answers it), a
/// constant-name is a literal (§13.10.4 GR1), and a CAPACITY register is "treated as though implicitly defined at
/// the same level as the entry containing the OCCURS clause" (§13.18.38.3 SR30), i.e. in that entry's section.</para>
/// <para>⚠ An OBJECT PROPERTY answers NO here and is nonetheless a legal BY REFERENCE / RETURNING operand:
/// §8.4.3.9.3 SR5/SR6 let it "be specified wherever a data item with that description would be valid" as a
/// sending / receiving item. The SCREEN therefore exempts it by reference (the caller knows the reference, this
/// model knows only the place), while the GR6 a) / GR9 a) mode test still sees it fail — which is exactly the
/// standard's own worked example, §14.9.4.3 SR20's "BY CONTENT may be omitted when identifier-4 is an object
/// property".</para>
/// </summary>
public static class SectionDataItem
{
    /// <summary>What <paramref name="place"/> references when it is NOT a data item defined in the file,
    /// working-storage, local-storage or linkage section — a phrase for a diagnostic — or <see langword="null"/>
    /// when it is one. A decorator (reference modification, an image view) answers for the item it decorates.</summary>
    public static string? NonSectionKind(Place place) => place switch
    {
        PlaceDecorator d => NonSectionKind(d.Inner),
        ReportPageCounterPlace => "the PAGE-COUNTER special register, a temporary data item maintained for the "
            + "report (ISO §8.4.3.15.4 GR1)",
        ReportSumCounterPlace => "a REPORT SECTION sum counter, a conceptual data item (ISO §13.18.54.4 GR1)",
        DebugRegisterPlace => "the DEBUG-ITEM special register, which no DATA DIVISION entry describes",
        ExceptionObjectPlace => "the predefined object reference EXCEPTION-OBJECT, which no DATA DIVISION entry "
            + "describes (ISO §8.4.3.6.3 SR2)",
        _ => RootOf(place.Item).IsCompilerTemp
            ? "a temporary data item the compiler synthesizes (an object property, an inline method invocation or "
              + "a function's returned value), which no DATA DIVISION entry describes"
            : null,
    };

    private static DataItem RootOf(DataItem item)
    {
        var root = item;
        while (root.Parent is { } p) root = p;
        return root;
    }
}
