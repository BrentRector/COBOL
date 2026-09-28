// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Frontend.Diagnostics;

/// <summary>
/// A descriptor for a specific diagnostic: code, default severity, message template.
/// </summary>
public sealed record DiagnosticDescriptor(
    string Code,
    DiagnosticSeverity DefaultSeverity,
    string MessageTemplate);

/// <summary>
/// Central registry of all CBL diagnostic descriptors.
/// Each phase adds descriptors as a partial class block.
/// </summary>
public static partial class DiagnosticDescriptors
{
    // ══════════════════════════════════════
    // CBL0901–0905: MOVE enforcement
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL0901 = new("CBL0901", DiagnosticSeverity.Error,
        "Illegal MOVE: {0} to {1}");
    public static readonly DiagnosticDescriptor CBL0902 = new("CBL0902", DiagnosticSeverity.Error,
        "MOVE CORRESPONDING: source '{0}' must be a group item");
    public static readonly DiagnosticDescriptor CBL0903 = new("CBL0903", DiagnosticSeverity.Error,
        "MOVE CORRESPONDING: target '{0}' must be a group item");
    public static readonly DiagnosticDescriptor CBL0904 = new("CBL0904", DiagnosticSeverity.Error,
        "MOVE of figurative constant {0} to numeric target not allowed");
    public static readonly DiagnosticDescriptor CBL0905 = new("CBL0905", DiagnosticSeverity.Error,
        "MOVE to level-88 condition name not allowed");

    // ══════════════════════════════════════
    // CBL1001–1004: VALUE clause enforcement
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL1001 = new("CBL1001", DiagnosticSeverity.Warning,
        "VALUE clause not allowed on group item '{0}'");
    public static readonly DiagnosticDescriptor CBL1002 = new("CBL1002", DiagnosticSeverity.Error,
        "Initial VALUE for '{0}' incompatible with data category");
    public static readonly DiagnosticDescriptor CBL1003 = new("CBL1003", DiagnosticSeverity.Warning,
        "Extra VALUE items for '{0}' are ignored");
    public static readonly DiagnosticDescriptor CBL1004 = new("CBL1004", DiagnosticSeverity.Error,
        "Condition value incompatible with parent item '{0}'");

    // ══════════════════════════════════════
    // CBL1101–1105: OCCURS / DEPENDING ON
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL1101 = new("CBL1101", DiagnosticSeverity.Error,
        "DEPENDING ON '{0}' must be integer numeric");
    public static readonly DiagnosticDescriptor CBL1102 = new("CBL1102", DiagnosticSeverity.Warning,
        "DEPENDING ON '{0}' must be declared before table '{1}'");
    public static readonly DiagnosticDescriptor CBL1103 = new("CBL1103", DiagnosticSeverity.Error,
        "OCCURS key '{0}' not subordinate to table '{1}'");
    public static readonly DiagnosticDescriptor CBL1104 = new("CBL1104", DiagnosticSeverity.Error,
        "OCCURS key '{0}' cannot be a group item");
    public static readonly DiagnosticDescriptor CBL1105 = new("CBL1105", DiagnosticSeverity.Error,
        "SEARCH on non-table item '{0}'");

    // ══════════════════════════════════════
    // CBL1201–1205: SEARCH / SEARCH ALL
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL1201 = new("CBL1201", DiagnosticSeverity.Error,
        "SEARCH VARYING '{0}' must be index or integer");
    public static readonly DiagnosticDescriptor CBL1202 = new("CBL1202", DiagnosticSeverity.Error,
        "SEARCH ALL on non-table item '{0}'");
    public static readonly DiagnosticDescriptor CBL1203 = new("CBL1203", DiagnosticSeverity.Error,
        "KEY '{0}' not an OCCURS key of table '{1}'");
    public static readonly DiagnosticDescriptor CBL1204 = new("CBL1204", DiagnosticSeverity.Warning,
        "SEARCH ALL requires KEY phrase or OCCURS key for '{0}'");
    public static readonly DiagnosticDescriptor CBL1205 = new("CBL1205", DiagnosticSeverity.Error,
        "SEARCH ALL WHEN must be simple key comparison");

    // ══════════════════════════════════════
    // CBL1301–1304: STRING
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL1301 = new("CBL1301", DiagnosticSeverity.Error,
        "STRING INTO target must be alphanumeric or group");
    public static readonly DiagnosticDescriptor CBL1302 = new("CBL1302", DiagnosticSeverity.Error,
        "STRING source must be alphanumeric or group");
    public static readonly DiagnosticDescriptor CBL1303 = new("CBL1303", DiagnosticSeverity.Error,
        "STRING source cannot be numeric");
    public static readonly DiagnosticDescriptor CBL1304 = new("CBL1304", DiagnosticSeverity.Error,
        "STRING POINTER must be integer numeric");

    // ══════════════════════════════════════
    // CBL1401–1406: UNSTRING
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL1401 = new("CBL1401", DiagnosticSeverity.Error,
        "UNSTRING source must be alphanumeric or group");
    public static readonly DiagnosticDescriptor CBL1402 = new("CBL1402", DiagnosticSeverity.Error,
        "UNSTRING INTO target must be alphanumeric or group");
    public static readonly DiagnosticDescriptor CBL1403 = new("CBL1403", DiagnosticSeverity.Error,
        "UNSTRING DELIMITER must be alphanumeric");
    public static readonly DiagnosticDescriptor CBL1404 = new("CBL1404", DiagnosticSeverity.Error,
        "UNSTRING COUNT must be integer numeric");
    public static readonly DiagnosticDescriptor CBL1405 = new("CBL1405", DiagnosticSeverity.Error,
        "UNSTRING POINTER must be integer numeric");
    public static readonly DiagnosticDescriptor CBL1406 = new("CBL1406", DiagnosticSeverity.Error,
        "UNSTRING TALLYING must be integer numeric");

    // ══════════════════════════════════════
    // CBL1501–1503: INSPECT
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL1501 = new("CBL1501", DiagnosticSeverity.Error,
        "INSPECT target must be alphanumeric or group");
    public static readonly DiagnosticDescriptor CBL1502 = new("CBL1502", DiagnosticSeverity.Error,
        "INSPECT TALLYING target must be integer numeric");
    public static readonly DiagnosticDescriptor CBL1503 = new("CBL1503", DiagnosticSeverity.Error,
        "INSPECT character operand must be alphanumeric");

    // ══════════════════════════════════════
    // CBL1601–1605: START
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL1601 = new("CBL1601", DiagnosticSeverity.Error,
        "START not allowed: file not INDEXED or RELATIVE");
    public static readonly DiagnosticDescriptor CBL1602 = new("CBL1602", DiagnosticSeverity.Error,
        "START KEY must be comparison expression");
    public static readonly DiagnosticDescriptor CBL1603 = new("CBL1603", DiagnosticSeverity.Error,
        "START KEY operand not a record key of file");
    public static readonly DiagnosticDescriptor CBL1604 = new("CBL1604", DiagnosticSeverity.Error,
        "START KEY comparison operands incompatible");
    public static readonly DiagnosticDescriptor CBL1605 = new("CBL1605", DiagnosticSeverity.Error,
        "START requires KEY phrase for file");

    // ══════════════════════════════════════
    // CBL1701–1704: READ
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL1701 = new("CBL1701", DiagnosticSeverity.Error,
        "READ NEXT/PREVIOUS invalid for organization/access");
    public static readonly DiagnosticDescriptor CBL1702 = new("CBL1702", DiagnosticSeverity.Error,
        "READ KEY not allowed on non-indexed file");
    public static readonly DiagnosticDescriptor CBL1703 = new("CBL1703", DiagnosticSeverity.Error,
        "READ KEY not a record/alternate key of file");
    public static readonly DiagnosticDescriptor CBL1704 = new("CBL1704", DiagnosticSeverity.Error,
        "READ INTO target must be alphanumeric or group");

    // ══════════════════════════════════════
    // CBL1801–1803: WRITE
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL1801 = new("CBL1801", DiagnosticSeverity.Error,
        "WRITE FROM source incompatible with record");
    public static readonly DiagnosticDescriptor CBL1802 = new("CBL1802", DiagnosticSeverity.Error,
        "WRITE ADVANCING value must be numeric");
    public static readonly DiagnosticDescriptor CBL1803 = new("CBL1803", DiagnosticSeverity.Error,
        "WRITE ADVANCING item must be integer numeric");

    // ══════════════════════════════════════
    // CBL1902: REWRITE
    // ══════════════════════════════════════
    // CBL1901 ("REWRITE not allowed for file organization") was removed (DEVLOG 238): REWRITE is
    // valid for sequential, relative, and indexed organizations per ISO §14.9.35.
    public static readonly DiagnosticDescriptor CBL1902 = new("CBL1902", DiagnosticSeverity.Error,
        "REWRITE FROM source incompatible with record");

    // ══════════════════════════════════════
    // CBL2001: DELETE
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL2001 = new("CBL2001", DiagnosticSeverity.Error,
        "DELETE not allowed for file organization");

    // ══════════════════════════════════════
    // CBL2101–2102: RETURN
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL2101 = new("CBL2101", DiagnosticSeverity.Error,
        "RETURN: file is not sort/merge");
    public static readonly DiagnosticDescriptor CBL2102 = new("CBL2102", DiagnosticSeverity.Error,
        "RETURN INTO target must be alphanumeric or group");

    // ══════════════════════════════════════
    // CBL2201: RELEASE
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL2201 = new("CBL2201", DiagnosticSeverity.Error,
        "RELEASE: not a record for sort/merge file");

    // ══════════════════════════════════════
    // CBL2301–2308: PERFORM
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL2301 = new("CBL2301", DiagnosticSeverity.Error,
        "PERFORM paragraph '{0}' not found");
    public static readonly DiagnosticDescriptor CBL2302 = new("CBL2302", DiagnosticSeverity.Warning,
        "PERFORM THRU out of order: '{0}' does not precede '{1}'");
    public static readonly DiagnosticDescriptor CBL2303 = new("CBL2303", DiagnosticSeverity.Error,
        "PERFORM TIMES must be integer numeric");
    public static readonly DiagnosticDescriptor CBL2304 = new("CBL2304", DiagnosticSeverity.Error,
        "PERFORM UNTIL condition must be boolean");
    public static readonly DiagnosticDescriptor CBL2305 = new("CBL2305", DiagnosticSeverity.Error,
        "PERFORM VARYING control must be integer/index");
    public static readonly DiagnosticDescriptor CBL2306 = new("CBL2306", DiagnosticSeverity.Error,
        "PERFORM VARYING FROM must be numeric");
    public static readonly DiagnosticDescriptor CBL2307 = new("CBL2307", DiagnosticSeverity.Error,
        "PERFORM VARYING BY must be numeric");
    public static readonly DiagnosticDescriptor CBL2308 = new("CBL2308", DiagnosticSeverity.Error,
        "PERFORM VARYING UNTIL must be boolean");

    // ══════════════════════════════════════
    // CBL2401–2402: IF
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL2401 = new("CBL2401", DiagnosticSeverity.Error,
        "IF condition must be boolean");
    public static readonly DiagnosticDescriptor CBL2402 = new("CBL2402", DiagnosticSeverity.Error,
        "Comparison operands incompatible");

    // ══════════════════════════════════════
    // CBL2501–2503: EVALUATE
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL2501 = new("CBL2501", DiagnosticSeverity.Error,
        "EVALUATE WHEN type incompatible with subject");
    public static readonly DiagnosticDescriptor CBL2502 = new("CBL2502", DiagnosticSeverity.Warning,
        "EVALUATE missing WHEN OTHER");
    public static readonly DiagnosticDescriptor CBL2503 = new("CBL2503", DiagnosticSeverity.Error,
        "EVALUATE TRUE WHEN must be boolean");

    // ══════════════════════════════════════
    // CBL2601–2605: Arithmetic enforcement
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL2601 = new("CBL2601", DiagnosticSeverity.Error,
        "Arithmetic operand must be numeric");
    public static readonly DiagnosticDescriptor CBL2602 = new("CBL2602", DiagnosticSeverity.Error,
        "Arithmetic result '{0}' must be numeric");
    public static readonly DiagnosticDescriptor CBL2603 = new("CBL2603", DiagnosticSeverity.Error,
        "ROUNDED item '{0}' must be numeric");
    public static readonly DiagnosticDescriptor CBL2604 = new("CBL2604", DiagnosticSeverity.Error,
        "SIZE ERROR phrase requires a numeric operation");
    public static readonly DiagnosticDescriptor CBL2605 = new("CBL2605", DiagnosticSeverity.Error,
        "DIVIDE remainder '{0}' must be integer numeric");

    // ══════════════════════════════════════
    // CBL3001–3004: Flow analysis
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL3001 = new("CBL3001", DiagnosticSeverity.Warning,
        "Paragraph '{0}' is unreachable");
    public static readonly DiagnosticDescriptor CBL3002 = new("CBL3002", DiagnosticSeverity.Warning,
        "Fall-through from section '{0}' into '{1}'");
    public static readonly DiagnosticDescriptor CBL3003 = new("CBL3003", DiagnosticSeverity.Warning,
        "Paragraph '{0}' must terminate with EXIT");
    public static readonly DiagnosticDescriptor CBL3004 = new("CBL3004", DiagnosticSeverity.Warning,
        "PERFORM cycle: '{0}' -> '{1}'");

    // ══════════════════════════════════════
    // CBL3101–3111: Scope & symbols
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL3101 = new("CBL3101", DiagnosticSeverity.Error,
        "Duplicate data-name '{0}'");
    public static readonly DiagnosticDescriptor CBL3102 = new("CBL3102", DiagnosticSeverity.Error,
        "Duplicate condition-name '{0}' in '{1}'");
    public static readonly DiagnosticDescriptor CBL3103 = new("CBL3103", DiagnosticSeverity.Error,
        "Duplicate section name '{0}'");
    public static readonly DiagnosticDescriptor CBL3104 = new("CBL3104", DiagnosticSeverity.Error,
        "Duplicate paragraph name '{0}'");
    public static readonly DiagnosticDescriptor CBL3105 = new("CBL3105", DiagnosticSeverity.Error,
        "GLOBAL not allowed in this context");
    public static readonly DiagnosticDescriptor CBL3106 = new("CBL3106", DiagnosticSeverity.Warning,
        "LOCAL '{0}' shadows GLOBAL '{1}'");
    public static readonly DiagnosticDescriptor CBL3107 = new("CBL3107", DiagnosticSeverity.Error,
        "Name '{0}' conflicts with symbol '{1}'");
    public static readonly DiagnosticDescriptor CBL3108 = new("CBL3108", DiagnosticSeverity.Error,
        "USING parameter '{0}' not in LINKAGE SECTION");
    public static readonly DiagnosticDescriptor CBL3109 = new("CBL3109", DiagnosticSeverity.Error,
        "RETURNING item '{0}' not in LINKAGE SECTION");
    public static readonly DiagnosticDescriptor CBL3110 = new("CBL3110", DiagnosticSeverity.Error,
        "VALUE not allowed in LINKAGE item '{0}'");
    public static readonly DiagnosticDescriptor CBL3111 = new("CBL3111", DiagnosticSeverity.Error,
        "REDEFINES not allowed for LINKAGE item '{0}'");
    public static readonly DiagnosticDescriptor CBL3112 = new("CBL3112", DiagnosticSeverity.Error,
        "REDEFINES level mismatch: '{0}' is level {1}, target '{2}' is level {3}");
    public static readonly DiagnosticDescriptor CBL3113 = new("CBL3113", DiagnosticSeverity.Error,
        "Cannot REDEFINES special-level item '{0}' (level {1})");
    public static readonly DiagnosticDescriptor CBL3114 = new("CBL3114", DiagnosticSeverity.Error,
        "REDEFINES target '{0}' is subordinate to OCCURS item '{1}'");

    // ══════════════════════════════════════
    // CBL3115–3118: EXTERNAL / GLOBAL clauses
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL3115 = new("CBL3115", DiagnosticSeverity.Error,
        "EXTERNAL clause on '{0}' is only allowed on level-01 items in WORKING-STORAGE SECTION");
    public static readonly DiagnosticDescriptor CBL3116 = new("CBL3116", DiagnosticSeverity.Error,
        "GLOBAL clause on '{0}' is only allowed on level-01 items");
    public static readonly DiagnosticDescriptor CBL3117 = new("CBL3117", DiagnosticSeverity.Error,
        "EXTERNAL clause on '{0}' cannot be combined with REDEFINES");
    public static readonly DiagnosticDescriptor CBL3118 = new("CBL3118", DiagnosticSeverity.Warning,
        "EXTERNAL clause on '{0}': shared storage not yet supported at runtime; item treated as internal");
    public static readonly DiagnosticDescriptor CBL3119 = new("CBL3119", DiagnosticSeverity.Warning,
        "GLOBAL clause on '{0}': nested program visibility not yet supported at runtime; item treated as local");

    // ══════════════════════════════════════
    // CBL3120–3127: Reference-resolution / SPECIAL-NAMES / SCREEN semantic errors.
    // These replaced the bare ad-hoc "SEM" code (DEVLOG 304): every diagnostic now carries a
    // registry descriptor so it can be documented, suppressed, and asserted on by code.
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL3120 = new("CBL3120", DiagnosticSeverity.Error,
        "{0} target '{1}' is not a paragraph or section");
    public static readonly DiagnosticDescriptor CBL3121 = new("CBL3121", DiagnosticSeverity.Error,
        "{0} target '{1}' is not a declared file");
    public static readonly DiagnosticDescriptor CBL3122 = new("CBL3122", DiagnosticSeverity.Warning,
        "Paragraph '{0}' has a name that matches a COBOL keyword — this may indicate a parsing error " +
        "(e.g., an unconsumed keyword from a statement clause)");
    public static readonly DiagnosticDescriptor CBL3123 = new("CBL3123", DiagnosticSeverity.Error,
        "CURRENCY SIGN clause: expected a picture SYMBOL after WITH PICTURE, but found '{0}'");
    public static readonly DiagnosticDescriptor CBL3124 = new("CBL3124", DiagnosticSeverity.Error,
        "CURRENCY symbol cannot be {0}");
    public static readonly DiagnosticDescriptor CBL3125 = new("CBL3125", DiagnosticSeverity.Error,
        "SYMBOLIC CHARACTERS: {0} name(s) but {1} ordinal(s) — counts must be equal");
    public static readonly DiagnosticDescriptor CBL3126 = new("CBL3126", DiagnosticSeverity.Error,
        "SCREEN item: HIGHLIGHT and LOWLIGHT are mutually exclusive");
    public static readonly DiagnosticDescriptor CBL3127 = new("CBL3127", DiagnosticSeverity.Error,
        "SCREEN item: USING cannot be combined with FROM or TO");

    // ══════════════════════════════════════
    // CBL3128: Undefined data-name reference (ISO §8.4.2.1 uniqueness of reference). Active in ALL
    // dialects (DEVLOG 310): the staged rollout completed once the corpus dry-run was clean (0/349,
    // after the IC228A inherited-GLOBAL fix), closing the assessment's #1 commercial gap (DEVLOG 305/310).
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL3128 = new("CBL3128", DiagnosticSeverity.Error,
        "Undefined data-name '{0}': not declared as a data item, condition-name, index-name, file-name, or special register");

    // ══════════════════════════════════════
    // CBL3201–3206: File status
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL3201 = new("CBL3201", DiagnosticSeverity.Error,
        "FILE STATUS must be a data-name");
    public static readonly DiagnosticDescriptor CBL3202 = new("CBL3202", DiagnosticSeverity.Error,
        "FILE STATUS must be alphanumeric length >= 2");
    public static readonly DiagnosticDescriptor CBL3203 = new("CBL3203", DiagnosticSeverity.Error,
        "FILE STATUS cannot be group item");
    public static readonly DiagnosticDescriptor CBL3204 = new("CBL3204", DiagnosticSeverity.Error,
        "FILE STATUS cannot be REDEFINES/RENAMES");
    public static readonly DiagnosticDescriptor CBL3205 = new("CBL3205", DiagnosticSeverity.Error,
        "File has more than one FILE STATUS");
    public static readonly DiagnosticDescriptor CBL3206 = new("CBL3206", DiagnosticSeverity.Warning,
        "FILE STATUS not checked before next I/O");

    // ══════════════════════════════════════
    // CBL3301–3310: CALL / USING / RETURNING
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL3301 = new("CBL3301", DiagnosticSeverity.Error,
        "CALL argument count mismatch");
    public static readonly DiagnosticDescriptor CBL3302 = new("CBL3302", DiagnosticSeverity.Error,
        "Argument not valid for parameter mode");
    public static readonly DiagnosticDescriptor CBL3303 = new("CBL3303", DiagnosticSeverity.Error,
        "Argument type incompatible with parameter");
    public static readonly DiagnosticDescriptor CBL3304 = new("CBL3304", DiagnosticSeverity.Error,
        "RETURNING item not in LINKAGE SECTION");
    public static readonly DiagnosticDescriptor CBL3305 = new("CBL3305", DiagnosticSeverity.Error,
        "CALL RETURNING type incompatible");
    public static readonly DiagnosticDescriptor CBL3310 = new("CBL3310", DiagnosticSeverity.Warning,
        "Dynamic CALL: parameter list cannot be validated");

    // ══════════════════════════════════════
    // CBL3501–3502: Strict COBOL-85 mode
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL3501 = new("CBL3501", DiagnosticSeverity.Error,
        "Feature not allowed in strict COBOL-85 mode");
    public static readonly DiagnosticDescriptor CBL3502 = new("CBL3502", DiagnosticSeverity.Warning,
        "Feature not part of COBOL-85");

    // ══════════════════════════════════════
    // CBL0601–0602: SELECT/FD consistency
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL0601 = new("CBL0601", DiagnosticSeverity.Warning,
        "FD '{0}' has no matching SELECT in FILE-CONTROL");

    // ══════════════════════════════════════
    // CBL0701: OPEN enforcement
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL0701 = new("CBL0701", DiagnosticSeverity.Error,
        "OPEN EXTEND not allowed on non-sequential file '{0}'");
    public static readonly DiagnosticDescriptor CBL0702 = new("CBL0702", DiagnosticSeverity.Warning,
        "I/O operation on file '{0}' which has not been OPENed");

    // ══════════════════════════════════════
    // Data item classification (Phase 1.1)
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL0801 = new("CBL0801", DiagnosticSeverity.Error,
        "OCCURS not allowed on level 01 or 77 item '{0}'");
    public static readonly DiagnosticDescriptor CBL0802 = new("CBL0802", DiagnosticSeverity.Error,
        "BLANK WHEN ZERO only allowed on numeric DISPLAY item '{0}'");
    public static readonly DiagnosticDescriptor CBL0803 = new("CBL0803", DiagnosticSeverity.Error,
        "JUSTIFIED only allowed on alphanumeric elementary item '{0}'");

    // ══════════════════════════════════════
    // CBL0814–0816: PICTURE / level-number / USAGE validity (ISO §13.18.40.3, §8.5.1.2, §13.18.60.2)
    // CBL0814 is dialect-gated to named-strict modes (illegal-symbol detection — staged like CBL3128);
    // CBL0815 (level number) is unconditional: no valid program has an out-of-range level, so it never
    // fires on the corpus and it replaces a crash-prone int.Parse. (DEVLOG 306)
    // CBL0816 (unsupported COMP-n usage) is unconditional like CBL0815: COMP-n for n not in 1–5 is not a
    // defined USAGE in any dialect, so it never fires on a valid program. (DEVLOG 342)
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL0814 = new("CBL0814", DiagnosticSeverity.Error,
        "Illegal PICTURE '{0}' for item '{1}': '{2}' is not a valid PICTURE symbol");
    public static readonly DiagnosticDescriptor CBL0815 = new("CBL0815", DiagnosticSeverity.Error,
        "Invalid level number '{0}' for '{1}': level numbers must be 1-49, 66, 77, or 88");
    public static readonly DiagnosticDescriptor CBL0816 = new("CBL0816", DiagnosticSeverity.Error,
        "Unsupported USAGE '{0}' for item '{1}': valid COMPUTATIONAL forms are COMP, COMP-1, COMP-2, COMP-3, COMP-4, COMP-5 (and their COMPUTATIONAL spellings)");

    // ══════════════════════════════════════
    // CBL0808: REDEFINES clause ordering (§13.18.44 SR 1)
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL0808 = new("CBL0808", DiagnosticSeverity.Warning,
        "REDEFINES clause should be first clause after data-name in item '{0}'");

    // ══════════════════════════════════════
    // CBL0810–0813: RENAMES (level 66) validation
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL0810 = new("CBL0810", DiagnosticSeverity.Error,
        "RENAMES FROM target '{0}' not found in item '{1}'");
    public static readonly DiagnosticDescriptor CBL0811 = new("CBL0811", DiagnosticSeverity.Error,
        "RENAMES THRU target '{0}' not found in item '{1}'");
    public static readonly DiagnosticDescriptor CBL0812 = new("CBL0812", DiagnosticSeverity.Error,
        "RENAMES cannot reference level-66 or level-88 item '{0}'");
    public static readonly DiagnosticDescriptor CBL0813 = new("CBL0813", DiagnosticSeverity.Error,
        "RENAMES THRU item '{0}' must follow FROM item '{1}' in storage");

    // ══════════════════════════════════════
    // COBOL0001: Generic parse error
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOL0001 = new("COBOL0001", DiagnosticSeverity.Error,
        "{0}");

    // ══════════════════════════════════════
    // COBOL0100–0109: Parser — unsupported feature warnings
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOL0100 = new("COBOL0100", DiagnosticSeverity.Warning,
        "ASCENDING/DESCENDING KEY clause in OCCURS is not yet supported. Table created without sort key.");
    public static readonly DiagnosticDescriptor COBOL0101 = new("COBOL0101", DiagnosticSeverity.Warning,
        "BLANK WHEN ZERO may not be recognized. Check that it appears as a single clause on the data item.");
    public static readonly DiagnosticDescriptor COBOL0102 = new("COBOL0102", DiagnosticSeverity.Warning,
        "This SET form may not be supported. Supported forms: SET identifier TO value, SET condition TO TRUE/FALSE, SET index UP/DOWN BY integer.");
    public static readonly DiagnosticDescriptor COBOL0103 = new("COBOL0103", DiagnosticSeverity.Warning,
        "SEARCH statement may not be fully supported.");
    // COBOL0104 (OCCURS DEPENDING ON), 0105 (INSPECT CONVERTING) and 0106 (INITIALIZE REPLACING)
    // were "not yet supported" hints for features that now work; removed (DEVLOG 232).
    public static readonly DiagnosticDescriptor COBOL0107 = new("COBOL0107", DiagnosticSeverity.Warning,
        "EVALUATE with ALSO (multi-subject) may not be fully supported.");
    // COBOL0108 (multi-target SET) and 0109 (PERFORM VARYING … AFTER) were "not yet supported"
    // hints for features that now work; removed (DEVLOG 232).
    public static readonly DiagnosticDescriptor COBOL0110 = new("COBOL0110", DiagnosticSeverity.Warning,
        "Statement not recognized or not yet implemented: '{0}'.");
    // An ERROR (not a warning): the unsupported INVOKE argument forms would otherwise be silently dropped,
    // shifting the RETURNING slot and miscompiling. Fail loudly until OO slice 3+ adds them.
    public static readonly DiagnosticDescriptor COBOL0111 = new("COBOL0111", DiagnosticSeverity.Error,
        "INVOKE argument form not yet supported: only BY REFERENCE data-reference arguments are implemented "
        + "(literal, BY VALUE, and BY CONTENT arguments are a later OO slice).");
    // OO slice 3 deferrals — reported as ERRORs so the unsupported form fails loudly instead of silently
    // dropping (binder would otherwise return null and the statement would vanish with no diagnostic).
    public static readonly DiagnosticDescriptor COBOL0112 = new("COBOL0112", DiagnosticSeverity.Error,
        "INVOKE SELF target not yet supported — it needs a sibling method (multi-method classes), a later OO slice.");
    public static readonly DiagnosticDescriptor COBOL0113 = new("COBOL0113", DiagnosticSeverity.Error,
        "A subclass (INHERITS FROM '{0}') may not declare its own OBJECT data yet — only inherited data is "
        + "supported in this OO slice.");
    public static readonly DiagnosticDescriptor COBOL0114 = new("COBOL0114", DiagnosticSeverity.Error,
        "INHERITS FROM '{0}': base class not found in this compilation group.");
    public static readonly DiagnosticDescriptor COBOL0115 = new("COBOL0115", DiagnosticSeverity.Error,
        "INVOKE SUPER is not valid in class '{0}' — it has no INHERITS FROM base class.");
    // A SECTION inside a METHOD-ID is not yet method-scoped (its paragraphs would not be attributed to the method's
    // dispatch range), so reject it loudly rather than silently skip the section's paragraphs. A later OO slice.
    public static readonly DiagnosticDescriptor COBOL0116 = new("COBOL0116", DiagnosticSeverity.Error,
        "A SECTION inside a METHOD-ID ('{0}') is not yet supported — use paragraphs (a later OO slice adds method-scoped sections).");
    // Multi-method classes work, and a SINGLE-method class may have USING/RETURNING params (oo_method_args) — but a
    // class with MULTIPLE methods where any method has parameters is not yet supported: per-method LINKAGE layout +
    // offset resolution (FindLinkageField is module-level, so it conflates sibling methods' LINKAGE) is a later OO
    // slice. Reject loudly rather than crash at run time with cross-wired parameter buffers.
    public static readonly DiagnosticDescriptor COBOL0117 = new("COBOL0117", DiagnosticSeverity.Error,
        "Class '{0}' has multiple methods with parameters — multi-method classes with USING/RETURNING are not yet "
        + "supported (a later OO slice adds per-method LINKAGE); single-method classes may use parameters.");

    // ══════════════════════════════════════
    // COBOL0200–0201: Parser — reserved word conflicts
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOL0200 = new("COBOL0200", DiagnosticSeverity.Warning,
        "STATUS is a reserved word here. For file status, use 'FILE STATUS IS <data-name>'.");
    public static readonly DiagnosticDescriptor COBOL0201 = new("COBOL0201", DiagnosticSeverity.Warning,
        "PROGRAM is a reserved word. If this is a paragraph name, it cannot be named PROGRAM.");

    // ══════════════════════════════════════
    // COBOL0300–0312: Parser — syntax guidance
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOL0300 = new("COBOL0300", DiagnosticSeverity.Warning,
        "THROUGH/THRU is not recognized in this context. Check PERFORM or VALUE THROUGH syntax.");
    public static readonly DiagnosticDescriptor COBOL0301 = new("COBOL0301", DiagnosticSeverity.Warning,
        "Missing space before string literal.");
    public static readonly DiagnosticDescriptor COBOL0302 = new("COBOL0302", DiagnosticSeverity.Warning,
        "Missing space after string literal.");
    public static readonly DiagnosticDescriptor COBOL0303 = new("COBOL0303", DiagnosticSeverity.Warning,
        "In a MOVE statement, did you forget TO before the target?");
    public static readonly DiagnosticDescriptor COBOL0304 = new("COBOL0304", DiagnosticSeverity.Warning,
        "Missing period after paragraph name — the parser is treating it as a qualified reference.");
    public static readonly DiagnosticDescriptor COBOL0305 = new("COBOL0305", DiagnosticSeverity.Warning,
        "Unexpected token in SPECIAL-NAMES. Check implementor-name or mnemonic-name syntax.");
    public static readonly DiagnosticDescriptor COBOL0306 = new("COBOL0306", DiagnosticSeverity.Warning,
        "{0} appears without a matching {1} statement.");
    public static readonly DiagnosticDescriptor COBOL0307 = new("COBOL0307", DiagnosticSeverity.Warning,
        "A period may be missing at the end of the previous sentence.");
    public static readonly DiagnosticDescriptor COBOL0308 = new("COBOL0308", DiagnosticSeverity.Warning,
        "A data-name is expected here, not a literal.");
    public static readonly DiagnosticDescriptor COBOL0309 = new("COBOL0309", DiagnosticSeverity.Warning,
        "A literal value is expected here, not a data-name.");
    public static readonly DiagnosticDescriptor COBOL0310 = new("COBOL0310", DiagnosticSeverity.Warning,
        "Missing BY keyword. INDEXED BY requires 'INDEXED BY <index-name>'.");
    // COBOL0311 ("NOT = / NOT > / NOT < abbreviated condition not yet supported") was a hint for a
    // feature that now works; removed (DEVLOG 232).
    public static readonly DiagnosticDescriptor COBOL0312 = new("COBOL0312", DiagnosticSeverity.Warning,
        "Unexpected token in FILE-CONTROL paragraph. Check SELECT/ASSIGN TO syntax.");
    // W1.5 (VERSION_TEST_MATRIX_DESIGN P2.8): a non-ISO vendor statement (JSON/XML) behind a parse error —
    // NOT the 0900 edition band (no ISO edition has the construct; owner decision 2, DEVLOG 581).
    public static readonly DiagnosticDescriptor COBOL0313 = new("COBOL0313", DiagnosticSeverity.Error,
        "{0}");

    // ══════════════════════════════════════
    // COBOLNET0900: the edition-gating band's INTRODUCTION code, emitted from the PARSE layer (W1.5).
    // The bind-layer twin lives in CobolNet.Validation.EditionCodes.Introduction — same code string, one
    // policy, two emit layers: a grammar-predicate rejection surfaces here (ReservedWordEditionHints), a bind-time
    // gate routes through ConstructRegistry.Check. Keep the code text identical.
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOLNET0900 = new(CobolNet.Editions.EditionCodes.Introduction,
        DiagnosticSeverity.Error, "{0}");

    // ══════════════════════════════════════
    // COBOLNET2072/2073: the required imperative-statement operand and the misplaced WHEN OTHER (kb/Work PB396).
    // Parse-layer twins of catalogue descriptors — the CODE is single-sourced from DiagnosticCatalog so the two
    // registries cannot drift, exactly as COBOLNET0900 single-sources EditionCodes.Introduction. The message is
    // composed at the CobolErrorStrategy site (it names the enclosing statement and the offending token).
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOLNET2072 = new(
        CobolNet.Editions.Diagnostics.DiagnosticCatalog.RequiredImperativeMissing.Code,
        DiagnosticSeverity.Error, "{0}");
    public static readonly DiagnosticDescriptor COBOLNET2073 = new(
        CobolNet.Editions.Diagnostics.DiagnosticCatalog.WhenOtherOutOfPosition.Code,
        DiagnosticSeverity.Error, "{0}");

    // ══════════════════════════════════════
    // COBOLNET2172/2173: a written shape NO general format of its clause prints (kb/Work PB412, PB421) — the
    // GO TO complement (§14.9.17.2) and a CORRESPONDING phrase after a MOVE sending operand (§14.9.25.2).
    // Same single-sourcing as 2072/2073 above: the CODE comes from the catalogue descriptor, the message is
    // composed at the CobolErrorStrategy site from the format's own cardinalities.
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOLNET2172 = new(
        CobolNet.Editions.Diagnostics.DiagnosticCatalog.GoToFormatShape.Code,
        DiagnosticSeverity.Error, "{0}");
    public static readonly DiagnosticDescriptor COBOLNET2173 = new(
        CobolNet.Editions.Diagnostics.DiagnosticCatalog.MoveCorrespondingPosition.Code,
        DiagnosticSeverity.Error, "{0}");

    // ══════════════════════════════════════
    // COBOLNET2418/2419: the parse-layer twins of the figurative-spelling keyword refusal (kb/Work PB510) and the
    // §13.18.40.3 SR7 PICTURE separator-period rule (kb/Work PB569). Same single-sourcing as 2072/2073.
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOLNET2418 = new(
        CobolNet.Editions.Diagnostics.DiagnosticCatalog.FigurativeSpellingNotTheKeyword.Code,
        DiagnosticSeverity.Error, "{0}");
    public static readonly DiagnosticDescriptor COBOLNET2419 = new(
        CobolNet.Editions.Diagnostics.DiagnosticCatalog.PictureTrailingSymbolNotLast.Code,
        DiagnosticSeverity.Error, "{0}");

    // ══════════════════════════════════════
    // COBOLNET2269: a statement written in a shape none of its general formats prints (kb/Work PB909) — the
    // parse-layer twin, for the shapes the grammar now REFUSES rather than admits and hands to the binder: the
    // SEARCH phrases no SEARCH format prints (NOT AT END, a KEY phrase, a second Format-2 WHEN; kb/Work PB446).
    // Same single-sourcing as 2172/2173: ONE code, the message composed at the CobolErrorStrategy site.
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOLNET2269 = new(
        CobolNet.Editions.Diagnostics.DiagnosticCatalog.StatementFormatShape.Code,
        DiagnosticSeverity.Error, "{0}");

    // ══════════════════════════════════════
    // COBOL0400–0412: Bound tree builder
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOL0400 = new("COBOL0400", DiagnosticSeverity.Warning,
        "Procedure name '{0}' is used as both a section and a paragraph; resolving as paragraph.");
    public static readonly DiagnosticDescriptor COBOL0401 = new("COBOL0401", DiagnosticSeverity.Warning,
        "Section '{0}' contains no paragraphs.");
    public static readonly DiagnosticDescriptor COBOL0402 = new("COBOL0402", DiagnosticSeverity.Error,
        "Paragraph or section '{0}' not found. Check spelling or verify it is defined in the PROCEDURE DIVISION.");
    public static readonly DiagnosticDescriptor COBOL0403 = new("COBOL0403", DiagnosticSeverity.Error,
        "{0} CORRESPONDING: '{1}' must be a group item.");
    public static readonly DiagnosticDescriptor COBOL0404 = new("COBOL0404", DiagnosticSeverity.Error,
        "PERFORM VARYING index '{0}' must not be subscripted.");
    public static readonly DiagnosticDescriptor COBOL0405 = new("COBOL0405", DiagnosticSeverity.Error,
        "Item '{0}' is not defined with OCCURS and cannot be subscripted.");
    public static readonly DiagnosticDescriptor COBOL0406 = new("COBOL0406", DiagnosticSeverity.Error,
        "Item '{0}' has {1} OCCURS level(s) but was referenced with {2} subscript(s).");
    public static readonly DiagnosticDescriptor COBOL0407 = new("COBOL0407", DiagnosticSeverity.Error,
        "Item '{0}' exceeds the supported limit of 7 OCCURS levels (found {1}).");
    public static readonly DiagnosticDescriptor COBOL0408 = new("COBOL0408", DiagnosticSeverity.Error,
        "A maximum of 7 subscripts is supported; found {0}.");
    public static readonly DiagnosticDescriptor COBOL0409 = new("COBOL0409", DiagnosticSeverity.Error,
        "Item '{0}' requires {1} subscript(s) but was referenced with {2}.");
    public static readonly DiagnosticDescriptor COBOL0410 = new("COBOL0410", DiagnosticSeverity.Warning,
        "{0} CORRESPONDING: field '{1}' is ambiguous in target group '{2}'.");
    public static readonly DiagnosticDescriptor COBOL0411 = new("COBOL0411", DiagnosticSeverity.Error,
        "{0} CORRESPONDING: '{1}' and '{2}' have incompatible OCCURS clauses.");
    public static readonly DiagnosticDescriptor COBOL0412 = new("COBOL0412", DiagnosticSeverity.Warning,
        "{0} CORRESPONDING: no matching elementary items between '{1}' and '{2}'.");
    public static readonly DiagnosticDescriptor COBOL0413 = new("COBOL0413", DiagnosticSeverity.Warning,
        "User-defined CLASS condition '{0}' is not yet supported; condition will evaluate to false.");

    // ══════════════════════════════════════
    // COBOL0500–0513: Binder (IR lowering)
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOL0500 = new("COBOL0500", DiagnosticSeverity.Error,
        "PERFORM VARYING index '{0}' has no storage location.");
    public static readonly DiagnosticDescriptor COBOL0501 = new("COBOL0501", DiagnosticSeverity.Error,
        "PERFORM target paragraph '{0}' not found in paragraph dispatch table.");
    public static readonly DiagnosticDescriptor COBOL0502 = new("COBOL0502", DiagnosticSeverity.Error,
        "PERFORM TIMES has no target paragraph and no inline statements.");
    public static readonly DiagnosticDescriptor COBOL0503 = new("COBOL0503", DiagnosticSeverity.Error,
        "Unsupported condition shape: {0}");
    public static readonly DiagnosticDescriptor COBOL0504 = new("COBOL0504", DiagnosticSeverity.Error,
        "Cannot normalize comparison operands: left={0}, right={1}");
    public static readonly DiagnosticDescriptor COBOL0505 = new("COBOL0505", DiagnosticSeverity.Error,
        "Unhandled comparison combination: {0} vs {1}");
    public static readonly DiagnosticDescriptor COBOL0506 = new("COBOL0506", DiagnosticSeverity.Error,
        "GO TO target '{0}' not found in paragraph dispatch table.");
    public static readonly DiagnosticDescriptor COBOL0507 = new("COBOL0507", DiagnosticSeverity.Error,
        "GO TO DEPENDING ON requires a selector variable.");
    public static readonly DiagnosticDescriptor COBOL0508 = new("COBOL0508", DiagnosticSeverity.Error,
        "GO TO DEPENDING ON selector '{0}' has no storage location.");
    public static readonly DiagnosticDescriptor COBOL0509 = new("COBOL0509", DiagnosticSeverity.Error,
        "EXIT {0} used outside of any active {1}.");
    public static readonly DiagnosticDescriptor COBOL0510 = new("COBOL0510", DiagnosticSeverity.Error,
        "SET target '{0}' has no storage location.");
    public static readonly DiagnosticDescriptor COBOL0511 = new("COBOL0511", DiagnosticSeverity.Error,
        "SET '{0}' TO: cannot resolve value expression ({1}).");
    public static readonly DiagnosticDescriptor COBOL0512 = new("COBOL0512", DiagnosticSeverity.Error,
        "SET '{0}' UP BY: cannot resolve delta expression ({1}).");
    public static readonly DiagnosticDescriptor COBOL0513 = new("COBOL0513", DiagnosticSeverity.Error,
        "SET '{0}' DOWN BY: cannot resolve delta expression ({1}).");

    // ══════════════════════════════════════
    // COBOL0600: Internal compiler error
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOL0600 = new("COBOL0600", DiagnosticSeverity.Error,
        "Internal compiler error while generating code for '{0}': {1}. Please report this.");

    // ══════════════════════════════════════
    // CBL0906–0908: Additional MOVE enforcement
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL0906 = new("CBL0906", DiagnosticSeverity.Error,
        "MOVE figurative constant {0} to Numeric target is not allowed");
    public static readonly DiagnosticDescriptor CBL0907 = new("CBL0907", DiagnosticSeverity.Error,
        "MOVE of non-integer numeric literal to alphanumeric target is not allowed");
    public static readonly DiagnosticDescriptor CBL0908 = new("CBL0908", DiagnosticSeverity.Error,
        "MOVE ZERO to Alphabetic target is not allowed");

    // ══════════════════════════════════════
    // CBL0804–0807: Additional data item checks
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL0804 = new("CBL0804", DiagnosticSeverity.Error,
        "BLANK WHEN ZERO not allowed with JUSTIFIED on item '{0}'");
    public static readonly DiagnosticDescriptor CBL0805 = new("CBL0805", DiagnosticSeverity.Error,
        "OCCURS not allowed on level 66 (RENAMES) item '{0}'");
    public static readonly DiagnosticDescriptor CBL0806 = new("CBL0806", DiagnosticSeverity.Warning,
        "VALUE clause not allowed on REDEFINES item '{0}'");
    public static readonly DiagnosticDescriptor CBL0807 = new("CBL0807", DiagnosticSeverity.Warning,
        "VALUE clause not allowed on item '{0}' subordinate to OCCURS");

    // ══════════════════════════════════════
    // CBL1206: SEARCH ALL key equality
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL1206 = new("CBL1206", DiagnosticSeverity.Error,
        "SEARCH ALL WHEN condition must be equality comparison on a key field");

    // ══════════════════════════════════════
    // COBOL0414: CORRESPONDING excludes RENAMES
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOL0414 = new("COBOL0414", DiagnosticSeverity.Error,
        "{0} CORRESPONDING: '{1}' is a RENAMES item and cannot participate.");

    // ══════════════════════════════════════
    // COBOL0415: Arithmetic statement with no valid receiving item / operand
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOL0415 = new("COBOL0415", DiagnosticSeverity.Error,
        "{0} statement has no valid receiving data item or operand — check that the data-names are defined (an undefined name is treated as a literal and cannot receive a result).");

    // ══════════════════════════════════════
    // CBL2606: Sign condition on non-numeric
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL2606 = new("CBL2606", DiagnosticSeverity.Error,
        "Sign condition (POSITIVE/NEGATIVE/ZERO) requires a numeric operand");

    // ══════════════════════════════════════
    // CBL3601–3607: obsolete features (ALTER, bare GO TO, and the generic obsolete-element flag CBL3607)
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL3601 = new("CBL3601", DiagnosticSeverity.Error,
        "ALTER statement deleted from COBOL standard in 2002; not allowed in {0} mode");
    public static readonly DiagnosticDescriptor CBL3602 = new("CBL3602", DiagnosticSeverity.Warning,
        "ALTER statement is obsolete; removed from COBOL-2002 and later standards");
    public static readonly DiagnosticDescriptor CBL3603 = new("CBL3603", DiagnosticSeverity.Error,
        "ALTER target '{0}' is not a paragraph name");
    public static readonly DiagnosticDescriptor CBL3604 = new("CBL3604", DiagnosticSeverity.Error,
        "ALTER target paragraph '{0}' does not contain a GO TO statement");
    public static readonly DiagnosticDescriptor CBL3605 = new("CBL3605", DiagnosticSeverity.Error,
        "Bare GO TO (without target) deleted from COBOL standard in 2002; not allowed in {0} mode");
    public static readonly DiagnosticDescriptor CBL3606 = new("CBL3606", DiagnosticSeverity.Warning,
        "Bare GO TO (without target) is obsolete; removed from COBOL-2002 and later standards");
    // CBL3607: generic obsolete-element flag, parameterized by element name (MULTIPLE FILE TAPE,
    // OPEN ... REVERSED, DATE-COMPILED, ...). Drives the NIST OBSOLETE flagging modules (NC303M, SQ303M).
    public static readonly DiagnosticDescriptor CBL3607 = new("CBL3607", DiagnosticSeverity.Warning,
        "{0} is an obsolete element; removed from COBOL-2002 and later standards");

    // ══════════════════════════════════════
    // CBL3611–3612: non-standard CCVS dialect leniencies (see docs/dialect-strictness.md).
    // Leniency L1: 'KEY' omitted from the INVALID KEY phrase.
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL3611 = new("CBL3611", DiagnosticSeverity.Error,
        "'KEY' is required in the INVALID KEY phrase; the no-KEY form is a non-standard CCVS leniency not allowed in {0} mode");
    public static readonly DiagnosticDescriptor CBL3612 = new("CBL3612", DiagnosticSeverity.Warning,
        "'KEY' omitted from the INVALID KEY phrase; non-standard (accepted as a CCVS leniency)");

    // Leniency L2: 'KEY' omitted from the RELATIVE KEY clause (ISO §12.4.5.13 requires it).
    public static readonly DiagnosticDescriptor CBL3613 = new("CBL3613", DiagnosticSeverity.Error,
        "'KEY' is required in the RELATIVE KEY clause; the no-KEY form is a non-standard CCVS leniency not allowed in {0} mode");
    public static readonly DiagnosticDescriptor CBL3614 = new("CBL3614", DiagnosticSeverity.Warning,
        "'KEY' omitted from the RELATIVE KEY clause; non-standard (accepted as a CCVS leniency)");

    // Leniency L3: 'KEY' omitted from the RECORD KEY / ALTERNATE RECORD KEY clause (ISO §12.4.5.12 requires it).
    public static readonly DiagnosticDescriptor CBL3615 = new("CBL3615", DiagnosticSeverity.Error,
        "'KEY' is required in the RECORD KEY clause; the no-KEY form is a non-standard CCVS leniency not allowed in {0} mode");
    public static readonly DiagnosticDescriptor CBL3616 = new("CBL3616", DiagnosticSeverity.Warning,
        "'KEY' omitted from the RECORD KEY clause; non-standard (accepted as a CCVS leniency)");

    // Leniency L5: 'COLLATING' omitted from the SORT/MERGE COLLATING SEQUENCE phrase (ISO §14.9.45/§14.9.24
    // — COLLATING is a required keyword; CCVS ST139A writes `SEQUENCE alphabet-name`). (L4 is USE…ERROR
    // without STANDARD, still deferred.)
    public static readonly DiagnosticDescriptor CBL3617 = new("CBL3617", DiagnosticSeverity.Error,
        "'COLLATING' is required in the SORT/MERGE COLLATING SEQUENCE phrase; the no-COLLATING form is a non-standard CCVS leniency not allowed in {0} mode");
    public static readonly DiagnosticDescriptor CBL3618 = new("CBL3618", DiagnosticSeverity.Warning,
        "'COLLATING' omitted from the SORT/MERGE SEQUENCE phrase; non-standard (accepted as a CCVS leniency)");

    // ══════════════════════════════════════
    // CBL3620–3622: COPY preprocessing (ISO §7.2.3). All three are unconditional errors: CBL3620 is library
    // text that cannot be located (§7.2.3.4 GR1/GR2 — at every edition and on every path since kb/Work PB1355;
    // the search is the DOC-A.1-40 determination); CBL3621 a circular include; CBL3622 over-deep nesting.
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL3620 = new("CBL3620", DiagnosticSeverity.Error,
        "COPY copybook '{0}' not found. Searched: {1}");
    public static readonly DiagnosticDescriptor CBL3621 = new("CBL3621", DiagnosticSeverity.Error,
        "Circular COPY: copybook '{0}' is already being included");
    public static readonly DiagnosticDescriptor CBL3622 = new("CBL3622", DiagnosticSeverity.Error,
        "COPY nesting too deep (limit {0}); possible recursive include");
}
