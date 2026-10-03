// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Procedure;

/// <summary>Which sort-merge statement's key rules apply — each prints the same admissibility rule in its own
/// syntax rules, in its own words.</summary>
internal enum SortKeyRules
{
    /// <summary>SORT Format 1 (file) — §14.9.40.3 SR6.</summary>
    FileSort,
    /// <summary>SORT Format 2 (table) — §14.9.40.3 SR14.</summary>
    TableSort,
    /// <summary>MERGE — §14.9.24.3 SR4.</summary>
    Merge,
}

/// <summary>⛔ THE ONE KEY-ADMISSIBILITY PREDICATE of the sort-merge verbs (kb/Work PB1173, PB1052): which data items
/// may be a KEY, asked of the DATA DESCRIPTION alone, by the three statements that name keys. The rule is printed
/// three times — §14.9.40.3 SR6 (SORT Format 1), §14.9.40.3 SR14 (Format 2) and §14.9.24.3 SR4 (MERGE) — and each
/// copy lists the same four shapes in slightly different words, so the statements used to enforce whichever one
/// they remembered: the file verbs asked only whether the key had a character image (which happens to catch a
/// dynamic-length item, and with the wrong diagnosis), the table sort asked nothing at all, and a BOOLEAN key, a
/// POINTER key and an occurs-depending group key compiled clean in every one of them. Written once, here, so a fourth
/// shape is one arm and the three callers cannot drift apart.
/// <para>The arms, with the rule each is:</para>
/// <list type="bullet">
///   <item><b>OCCURS</b> — SORT Format 1 SR6 b) and f) ("Key data names shall not be subject to any OCCURS clauses";
///     "None of the data items identified by key data-names may be described by an entry that either contains an
///     OCCURS clause or is subordinate to an entry that contains an OCCURS clause"), MERGE SR4 b) and f). Format 2
///     has no such arm HERE: its keys lie inside the table, whose own OCCURS is the point, and SR14 e) — an OCCURS
///     BETWEEN the key and data-name-2 — is asked of the walk to data-name-2 (<c>SortBinder.KeyUnderInnerOccurs</c>).</item>
///   <item><b>Class</b> — SR6 c) / SR4 c): "Key data items shall not be of the class boolean, message-tag, object, or
///     pointer"; SR14 c): "boolean, object, or pointer". The class is the OPERAND's (<see cref="DataItem.OperandPic"/>,
///     the ONE operand-category reader), so a bit GROUP is the boolean operand §13.18.29.4 GR1b makes it.</item>
///   <item><b>Shape</b> — SR6 d) / SR4 d): a variable-length group, an occurs-depending-on data item, a
///     dynamic-length elementary item, or an item subordinate to a dynamic-capacity table; SR14 d): a variable-length
///     group or an occurs-depending GROUP item (§13.18.38.4 GR8 — a group with an entry subordinate to it that
///     specifies the DEPENDING phrase). A group holding an occurs-depending table is refused by every statement: its
///     length is not fixed, and the file verbs' SR6 e) takes "the same byte positions" as the key in every record.</item>
/// </list></summary>
internal static class SortKeyAdmission
{
    /// <summary>The refusal text for <paramref name="key"/> under <paramref name="rules"/> — the violated rule quoted
    /// with its clause — or <see langword="null"/> when the key is admissible. The first violation in the order the
    /// rules are printed (b/f, c, d); one is enough to refuse, and naming the others would only restate it.</summary>
    public static string? Violation(DataItem key, SortKeyRules rules)
    {
        string name = key.CobolName ?? key.CsName;
        string clause = rules switch
        {
            SortKeyRules.FileSort => "ISO §14.9.40.3 SR6",
            SortKeyRules.TableSort => "ISO §14.9.40.3 SR14",
            _ => "ISO §14.9.24.3 SR4",
        };
        string verb = rules == SortKeyRules.Merge ? "MERGE" : "SORT";

        // SR6 b) / f): subject to OCCURS — its own entry or any entry above it. The file verbs only (see the type).
        if (rules != SortKeyRules.TableSort && key.IsTableElement)
        {
            // The level nearest the key — the table levels come from DataItem (the one arity walk, kb/Work PB877).
            var n = key.SubscriptLevels()[^1];
            return $"{verb} key '{name}' is "
                + (ReferenceEquals(n, key) ? "described with an OCCURS clause" : $"subordinate to '{n.CobolName ?? n.CsName}', "
                    + "which carries an OCCURS clause")
                + ": \"Key data names shall not be subject to any OCCURS clauses\" and \"None of the data items "
                + "identified by key data-names may be described by an entry that either contains an OCCURS clause "
                + $"or is subordinate to an entry that contains an OCCURS clause\" ({clause} b) and f))";
        }

        // SR6 c) / SR14 c): the key's CLASS.
        string? cls = key.OperandPic?.Category switch
        {
            PicCategory.Boolean => "boolean",
            PicCategory.ObjectReference => "object",
            PicCategory.Pointer or PicCategory.ProgramPointer or PicCategory.FunctionPointer => "pointer",
            _ => null,
        };
        if (cls is not null)
            return $"{verb} key '{name}' is of class {cls}: \"Key data items shall not be of "
                + (rules == SortKeyRules.TableSort ? "class boolean, object, or pointer" : "the class boolean, message-tag, object, or pointer")
                + $"\" ({clause} c))";

        // SR6 d) / SR14 d): the key's SHAPE — its length must not vary.
        string? shape = null;
        if (key.IsDynamicLength)
            shape = rules == SortKeyRules.TableSort ? null : "a dynamic-length elementary item";   // SR14 d) names groups only
        else if (key.IsGroup && ReferenceResolver.HasVariableLengthSubordinate(key))
            shape = "a variable-length group (ISO §8.5.1.12.1)";
        else if (key.IsGroup && DataItem.DescendantsOf(key).Any(d => d.OccursSpec is { Depending: not null }))
            shape = "an occurs-depending group item (ISO §13.18.38.4 GR8)";
        if (shape is not null)
            return $"{verb} key '{name}' is {shape}: \"A key data item shall not "
                + (rules == SortKeyRules.TableSort
                    ? "reference a variable-length group or an occurs-depending group item"
                    : "be a variable-length group, an occurs-depending-on data item, a dynamic-length elementary item "
                        + "or an item subordinate to a dynamic-capacity table")
                + $"\" ({clause} d))";
        return null;
    }
}
