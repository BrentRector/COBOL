"""PB3173 (S0b) — the editions of the 148 rows whose `editions` was empty, decided from the standard.

Each row is assigned a BAND and a REASON. A reason names the facility whose existence bounds the rule and the
evidence for that facility's introducing edition, each piece of which this script CHECKS before it writes a batch:
  ("cite", clause, text)       -> scripts/spec/cite.py --check must print OK
  ("construct", id, edition)   -> tests/version-matrix/constructs.json row `id` has introducedIn == edition
  ("intrinsic", NAME, edition) -> IntrinsicCatalog.cs row NAME is gated at that edition (the COBOLNET1502 window)
  ("reserved", WORD, edition)  -> tests/version-matrix/reserved-words.json first reserves WORD at that edition
  ("row", rule-id, band)       -> the inventory row rule-id (a decided sibling) carries that band
Ran once from the worktree root (adjudication/pb3173-editions/README.md):  python <this> <out-batch.json>
"""
import json, pathlib, re, subprocess, sys

A85, A02, A14, A23 = "85,2002,2014,2023", "2002,2014,2023", "2014,2023", "2023"
FIRST = {A85: 85, A02: 2002, A14: 2014, A23: 2023}

R = {
 "DIR": ("a compiler-directive rule; the 7.3 compiler-directive facility is a COBOL-2002 introduction (COBOL-85 has "
         "no >> directive line: docs/VERSION_CHANGE_REFERENCE.md Table 7 row 7.21, a derived edge)",
         [("cite", "7.3.3", "A compiler directive may be specified anywhere in a compilation group"),
          ("construct", "turn-directive-2002", 2002), ("construct", "listing-directive-2002", 2002),
          ("construct", "page-directive-2002", 2002)]),
 "COPY": ("the COPY statement exists at every edition (X3.23-1985 text manipulation); later phrases such as LEADING/"
          "TRAILING (Table 7 row 7.33) and the 2023 removal of REPLACING non-pseudo-text (Annex E.2 item 1) change "
          "its content, not its existence",
          [("cite", "7.2.3.1", "The COPY statement"), ("row", "GR-7.2.3.4-1", A85)]),
 "REPLACE": ("the REPLACE statement exists at every edition (an ANSI X3.23-1985 text-manipulation statement); "
             "ALSO/LAST and LEADING/TRAILING are its 2002 additions (constructs replace-also-last-2002)",
             [("cite", "7.2.4.1", "REPLACE statement"), ("row", "SR-7.2.4.3-1", A85)]),
 "MCS": ("an asynchronous-messaging (MCS) rule; the facility, its message-tag class and USAGE MESSAGE-TAG are a "
         "COBOL-2023 introduction (Annex E.3.2 item 1; MESSAGE-TAG first reserved at 2023)",
         [("cite", "E.3.2", "A method of allowing communication between run units via messages is provided"),
          ("construct", "async-messaging-2023", 2023), ("reserved", "MESSAGE-TAG", 2023)]),
 "COMMIT": ("the commit and rollback facility is a COBOL-2023 introduction (Annex E.3.2 item 2)",
            [("cite", "E.3.2", "Commit and rollback facility"), ("reserved", "COMMIT", 2023)]),
 "FMTSEL": ("an A.4.8 FORMAT / SELECT WHEN file-handling rule; the FORMAT clause is post-85 (FORMAT is first "
            "reserved at 2002, so no COBOL-85 source can write it) and is not a 2023 introduction (Annex E.3 lists "
            "none), so the facility's edge is 2002 (derived)",
            [("cite", "13.18.24.1", "The FORMAT clause specifies"), ("reserved", "FORMAT", 2002)]),
 "OO": ("an object-orientation rule; classes, objects and object references are a COBOL-2002 introduction",
        [("cite", "9.3.6", "the parameter or returning item in the invocation"),
         ("construct", "class-definition-2002", 2002), ("construct", "usage-object-reference-2002", 2002)]),
 "SB": ("conditioned on standard-binary arithmetic, which the ARITHMETIC clause's STANDARD-BINARY phrase introduced "
        "at 2014; obsolete at 2023 but not removed (Annex F.2 item 3)",
        [("cite", "11.9.5.2", "If the STANDARD-BINARY phrase is specified"),
         ("cite", "F.2", "STANDARD-BINARY arithmetic and STANDARD BINARY Intermediate Data Item"),
         ("construct", "arithmetic-standard-binary-2014", 2014)]),
 "SD": ("conditioned on standard-decimal arithmetic, a 2014 introduction (the STANDARD-DECIMAL phrase)",
        [("cite", "11.9.5.2", "If the STANDARD-DECIMAL phrase is specified"),
         ("construct", "arithmetic-standard-decimal-2014", 2014)]),
 "SBSD": ("conditioned on standard-binary or standard-decimal arithmetic, both 2014 introductions",
          [("construct", "arithmetic-standard-binary-2014", 2014),
           ("construct", "arithmetic-standard-decimal-2014", 2014)]),
 "FD": ("a FLOAT-DECIMAL clause rule (the OPTIONS paragraph clause for the standard decimal floating-point usages), "
        "a 2014 introduction",
        [("cite", "11.9.9.1", "The FLOAT-DECIMAL clause specifies the encoding and the endianness"),
         ("construct", "options-float-decimal-2014", 2014), ("construct", "usage-float-decimal16-2014", 2014)]),
 "FLOATSTD": ("the default endianness of the standard floating-point usages, set by the FLOAT-BINARY and "
              "FLOAT-DECIMAL clauses: 2014 introductions",
              [("construct", "options-float-binary-2014", 2014), ("construct", "options-float-decimal-2014", 2014)]),
 "SPLIT": ("the record-key-name-1 SOURCE (split-key) form of RECORD KEY / ALTERNATE RECORD KEY; absent from "
           "X3.23-1985 (RECORD KEY IS data-name only) and not a 2023 introduction (Annex E.3 lists none); taken as "
           "2002 with the inventory's own FMT-12.4.5.12.2 band. ⚠ DERIVED, LOW CONFIDENCE: the repo holds no 2002 "
           "or 2014 text to tell 2002 from 2014, so this edge is overturnable in one batch",
           [("cite", "12.4.5.12.4", "Record-key-name-1 defines a record key consisting of the concatenation"),
            ("row", "FMT-12.4.5.12.2", A02)]),
 "CONST": ("a constant-entry rule; the constant entry (level 01 CONSTANT) is a COBOL-2002 introduction",
           [("cite", "13.10.3", "The length of data-name-1 or data-name-2 shall not be dependent"),
            ("construct", "constant-entry-2002", 2002), ("reserved", "CONSTANT", 2002)]),
 "VAL85": ("a VALUE-clause rule on a construct that exists at COBOL-85 (a numeric-edited item's nonnumeric VALUE "
           "literal; a level-88 THROUGH range); national literals are its 2002 extension, not its origin",
           [("row", "SR-13.18.63.3-6", A85), ("row", "SR-13.18.63.3-8", A85)]),
 "VALTAB": ("a format 2 (table) VALUE rule; the table form is a COBOL-2002 addition (constructs value-table-format-2002)",
            [("construct", "value-table-format-2002", 2002), ("row", "GR-13.18.63.4-11", A02)]),
 "BYCONTENT": ("the BY CONTENT argument passing of COBOL-85's CALL; the REPOSITORY / NESTED legs it mentions are "
               "later refinements of the same rule",
               [("cite", "14.2.3", "If the argument is passed by content"), ("row", "GR-14.2.3-8", A85)]),
 "ALLOC": ("the ALLOCATE and FREE statements are COBOL-2002 introductions",
           [("construct", "allocate-2002", 2002), ("construct", "free-2002", 2002), ("reserved", "ALLOCATE", 2002)]),
 "SCREEN": ("the screen-format ACCEPT (A.4.2 screen handling; the SCREEN SECTION) is a COBOL-2002 introduction",
            [("reserved", "SCREEN", 2002), ("construct", "end-accept-2002", 2002)]),
 "STOP85": ("STOP RUN and normal run-unit termination exist at every edition", [("row", "GR-14.9.42.4-6", A85)]),
 "ZL": ("the zero-length literal is a COBOL-2014 introduction (Table 7 row 7.30)",
        [("cite", "8.3.3.1", "it is known as a zero-length literal"), ("construct", "zero-length-literal-2014", 2014)]),
 "NATIVE31": ("the 31-digit limit is stated for native arithmetic, the mode the ARITHMETIC clause (2002) names; "
              "the rule's form is the 2002 one",
              [("construct", "options-arithmetic-native-2002", 2002), ("row", "GR-11.9.5.2-1", A02)]),
 "EC": ("conditioned on the exception-condition (EC-) facility, a COBOL-2002 introduction",
        [("construct", "use-after-exception-condition-2002", 2002), ("row", "DOC-A.1-69", A02)]),
 "NAT": ("about national characters / national data, a COBOL-2002 introduction",
         [("construct", "national-data-2002", 2002), ("construct", "alphabet-national-2002", 2002)]),
 "CHARSET02": ("about the compile-time / runtime coded-character-set architecture (several encodings, conversion of "
               "literals) that COBOL-2002 introduced with national data; ⚠ derived",
               [("construct", "national-data-2002", 2002), ("row", "DOC-A.1-36", A02), ("row", "DOC-A.1-38", A02)]),
 "HEX": ("hexadecimal literals are a COBOL-2002 introduction (Table 7 row 7.29)",
         [("construct", "hex-alphanumeric-literal-2002", 2002), ("row", "DOC-A.1-95", A02), ("row", "DOC-A.1-97", A02)]),
 "FLOATLIT": ("floating-point numeric literals and data are a COBOL-2002 introduction",
              [("cite", "8.3.3.3.3", "Floating-point"), ("construct", "usage-float-short-2002", 2002),
               ("row", "DOC-A.1-81", A02)]),
 "PROTO": ("function-prototypes are a COBOL-2002 introduction",
           [("construct", "function-prototype-2002", 2002)]),
 "PROGPTR": ("the program-address-identifier (ADDRESS OF PROGRAM) is a COBOL-2002 introduction",
             [("construct", "set-program-pointer-2002", 2002), ("construct", "address-identifier-2002", 2002)]),
 "LOCK": ("record locking is a COBOL-2002 introduction",
          [("construct", "record-lock-phrase-2002", 2002), ("row", "DOC-A.1-153", A02)]),
 "ENTRYCONV": ("the ENTRY-CONVENTION clause is a COBOL-2014 introduction",
               [("construct", "options-entry-convention-2014", 2014)]),
 "VLG": ("a variable-length group (one with a dynamic-length elementary item or a dynamic-capacity table, 8.5.1.12.1) "
         "exists from 2014, the edition of both",
         [("cite", "8.5.1.12.1", "A variable-length group is a group item whose data description has at least one"),
          ("construct", "occurs-dynamic-2014", 2014), ("row", "DOC-A.1-62", A14)]),
 "TIMEFORM": ("the standard numeric time form and the fractional-seconds time formats (15.3.3) serve the FORMATTED-* "
              "and SECONDS-PAST-MIDNIGHT functions, all 2014 introductions",
              [("intrinsic", "FORMATTED-TIME", 2014), ("intrinsic", "SECONDS-PAST-MIDNIGHT", 2014),
               ("construct", "seconds-past-midnight-2014", 2014)]),
 "DELFILE": ("the DELETE FILE statement is a COBOL-2023 introduction (Annex E.3.3 item 15)",
             [("cite", "E.3.3", "The DELETE FILE statement causes the removal"), ("construct", "delete-file-2023", 2023)]),
 "MODNAME": ("the MODULE-NAME function is a COBOL-2023 introduction (Annex E.3.3 item 28)",
             [("cite", "E.3.3", "The MODULE-NAME intrinsic function has been added"), ("intrinsic", "MODULE-NAME", 2023)]),
 "SMALLEST": ("the SMALLEST-ALGEBRAIC function is a COBOL-2023 introduction (Annex E.3.3 item 29)",
              [("cite", "E.3.3", "The SMALLEST-ALGEBRAIC intrinsic function has been added"),
               ("intrinsic", "SMALLEST-ALGEBRAIC", 2023)]),
 "POP": ("the PUSH and POP directives are a COBOL-2023 introduction (Annex E.3.3 item 38)",
         [("cite", "E.3.3", "The PUSH and POP directives are added"), ("construct", "pop-directive-2023", 2023)]),
 "USAGE02": ("BINARY-SHORT/-LONG/-DOUBLE and FLOAT-SHORT/-LONG/-EXTENDED are COBOL-2002 introductions",
             [("construct", "usage-binary-char-family-2002", 2002), ("construct", "usage-float-short-2002", 2002),
              ("reserved", "BINARY-SHORT", 2002)]),
 "UTF": ("the UCS-4 / UTF-8 / UTF-16 alphabet phrases belong to the national-character facility of COBOL-2002; "
         "⚠ derived",
         [("construct", "alphabet-national-2002", 2002)]),
 "TEXTSTAGE": ("names the LISTING and PAGE directives, COBOL-2002 introductions (the directive facility, Table 7 row "
               "7.21), beside COPY's SUPPRESS phrase",
               [("construct", "listing-directive-2002", 2002), ("construct", "page-directive-2002", 2002)]),
 "BASE85": ("a facility that exists at every edition (the COBOL-85 nucleus, I-O, inter-program and SPECIAL-NAMES "
            "implementor-names; its arithmetic, the only arithmetic of COBOL-85, is what later editions name native)",
            [("row", "GR-14.7.5-5", A85)]),
}

def fn(name, ed):
    return (f"the {name} function exists from {ed} (IntrinsicCatalog.cs, the COBOLNET1502 edition window)",
            [("intrinsic", name, ed)])

D = {
 "SR-7.2.3.3-10": (A02, "DIR"), "SR-7.2.4.3-10": (A02, "DIR"), "SR-7.3.3-8": (A02, "DIR"),
 "GR-8.4.3.10.4-4": (A23, "MCS"), "GR-8.8.4.2.1-9": (A23, "MCS"),
 "GR-9.1.13.7-5": (A02, "FMTSEL"), "GR-13.18.13.4-4": (A02, "FMTSEL"),
 "GR-9.3.6-L4.1": (A02, "OO"), "GR-9.3.6-L4.2": (A02, "OO"), "GR-9.3.6-L4.3": (A02, "OO"),
 "GR-11.9.5.2-2": (A14, "SB"), "GR-11.9.11.2-2": (A14, "SB"), "GR-14.7.5-6": (A14, "SB"),
 **{f"SR-11.9.9.3-{i}": (A14, "FD") for i in range(1, 7)},
 "SR-12.4.5.6.3-6": (A02, "SPLIT"), "GR-12.4.5.6.4-2": (A02, "SPLIT"),
 "SR-12.4.5.12.3-5": (A02, "SPLIT"), "GR-12.4.5.12.4-2": (A02, "SPLIT"),
 "SR-13.10.3-4": (A02, "CONST"),
 "SR-13.18.63.3-7": (A85, "VAL85"), "SR-13.18.63.3-26": (A85, "VAL85"),
 "GR-13.18.63.4-12": (A02, "VALTAB"), "GR-13.18.63.4-13": (A02, "VALTAB"), "GR-13.18.63.4-15": (A02, "VALTAB"),
 "GR-14.2.3-9": (A85, "BYCONTENT"),
 "GR-14.6.11-3": (A02, "ALLOC"), "GR-14.6.11-4": (A02, "OO"), "GR-14.6.11-7": (A23, "MCS"),
 "GR-14.9.1.4-24": (A02, "SCREEN"),
 **{f"SR-14.9.31.3-{i}": (A23, "MCS") for i in range(1, 4)},
 **{f"GR-14.9.31.4-{i}": (A23, "MCS") for i in range(1, 6)},
 **{f"SR-14.9.38.3-{i}": (A23, "MCS") for i in range(1, 5)},
 **{f"GR-14.9.38.4-{i}": (A23, "MCS") for i in range(1, 9)},
 "FMT-14.9.31.2": (A23, "MCS"), "FMT-14.9.38.2": (A23, "MCS"),
 "GR-14.9.42.4-1": (A85, "STOP85"),
 "AR-15.58.3-1": (A02, fn("LOWEST-ALGEBRAIC", 2002)), "RV-15.58.4-2": (A02, fn("LOWEST-ALGEBRAIC", 2002)),
 "AR-15.58.3-2": (A14, "SD"), "AR-15.58.3-3": (A14, "SB"),
 "AR-15.61.3-1": (A85, fn("MEDIAN", 85)),
 "AR-15.63.3-3": (A14, "ZL"),
 "RV-15.63.4-1": (A85, fn("MIN", 85)), "RV-15.63.4-2": (A85, fn("MIN", 85)), "FMT-15.63.2": (A85, fn("MIN", 85)),
 "AR-15.64.3-2": (A85, fn("MOD", 85)),
 "RV-15.66.4-4": (A02, fn("NATIONAL-OF", 2002)), "FMT-15.66.2": (A02, fn("NATIONAL-OF", 2002)),
 "AR-15.67.3-3": (A02, "NATIVE31"), "AR-15.67.3-4": (A14, "SBSD"), "FMT-15.67.2": (A85, fn("NUMVAL", 85)),
 "AR-15.68.3-6": (A02, "NATIVE31"), "AR-15.68.3-7": (A14, "SBSD"),
 "AR-15.69.3-2": (A02, fn("NUMVAL-F", 2002)), "AR-15.69.3-3": (A14, "SBSD"),
 "AR-15.69.3-4": (A02, fn("NUMVAL-F", 2002)), "RV-15.69.4-1": (A02, fn("NUMVAL-F", 2002)),
 "FMT-15.69.2": (A02, fn("NUMVAL-F", 2002)),
 "FMT-7.2.3.2": (A85, "COPY"), "FMT-7.2.4.2": (A85, "REPLACE"), "FMT-12.4.6.3.2": (A23, "COMMIT"),
 # Annex A.1 implementor-defined items: the editions at which the facility the item documents exists.
 **{f"DOC-A.1-{n}": (A85, "BASE85") for n in (1, 2, 5, 13, 15, 19, 56, 58, 59, 92, 93, 123, 127, 129, 149, 158,
                                               179, 183, 184, 189, 190, 191, 195, 205, 208, 211, 215, 221, 222)},
 "DOC-A.1-18": (A02, "EC"), "DOC-A.1-70": (A02, "EC"), "DOC-A.1-90": (A02, "EC"),
 "DOC-A.1-30": (A02, "DIR"), "DOC-A.1-204": (A02, "DIR"), "DOC-A.1-200": (A02, "TEXTSTAGE"),
 "DOC-A.1-33": (A02, "NAT"), "DOC-A.1-185": (A02, "NAT"),
 "DOC-A.1-34": (A02, "CHARSET02"), "DOC-A.1-37": (A02, "CHARSET02"),
 "DOC-A.1-47": (A14, "FD"), "DOC-A.1-48": (A14, "FLOATSTD"),
 "DOC-A.1-50": (A23, "DELFILE"), "DOC-A.1-57": (A14, "VLG"), "DOC-A.1-64": (A14, "ENTRYCONV"),
 "DOC-A.1-82": (A02, "FLOATLIT"), "DOC-A.1-87": (A14, fn("FORMATTED-CURRENT-DATE", 2014)),
 "DOC-A.1-88": (A02, "PROTO"), "DOC-A.1-96": (A02, "HEX"), "DOC-A.1-98": (A02, "HEX"),
 "DOC-A.1-112": (A14, "TIMEFORM"), "DOC-A.1-202": (A14, "TIMEFORM"),
 **{f"DOC-A.1-{n}": (A23, "MODNAME") for n in range(133, 138)},
 "DOC-A.1-140": (A23, "POP"), "DOC-A.1-142": (A02, "PROGPTR"),
 "DOC-A.1-144": (A85, fn("RANDOM", 85)), "DOC-A.1-145": (A85, fn("RANDOM", 85)),
 "DOC-A.1-152": (A02, "LOCK"), "DOC-A.1-171": (A14, fn("SECONDS-PAST-MIDNIGHT", 2014)),
 "DOC-A.1-180": (A23, "SMALLEST"), "DOC-A.1-188": (A02, "UTF"), "DOC-A.1-207": (A02, "USAGE02"),
 "DOC-A.1-212": (A23, "MCS"),
}

R.update({
 "COLSYN": ("COL, COLS and COLUMNS are first reserved at 2002 (COBOL-85's report writer spells only COLUMN), so the "
            "synonym rule exists from 2002",
            [("cite", "13.18.14.3", "COLUMN, COL, COLUMNS, and COLS are synonyms"), ("reserved", "COL", 2002)]),
 "PRESENT": ("the rule is stated in terms of the PRESENT WHEN clause, a COBOL-2002 report-writer introduction "
             "(PRESENT first reserved at 2002)",
             [("construct", "report-present-when-2002", 2002), ("reserved", "PRESENT", 2002)]),
 "FLOAT02": ("FLOAT-SHORT / FLOAT-LONG / FLOAT-EXTENDED are COBOL-2002 introductions",
             [("construct", "usage-float-short-2002", 2002), ("reserved", "FLOAT-SHORT", 2002)]),
 "INITVAL": ("a file-section VALUE clause that takes effect only through INITIALIZE belongs to INITIALIZE's VALUE "
             "phrase, a COBOL-2002 introduction (COBOL-85 permits no VALUE clause in the file section outside a "
             "condition-name entry); derived",
             [("construct", "initialize-to-value-2002", 2002)]),
})

# PB3173's sibling sweep: bands that are not a run from the introducing edition THROUGH 2023 (a start edition written
# alone, or endpoints read as a list: every catalog rule is a rule of the 2023 text, so 2023 is always in its band),
# and well-formed bands in a decided row's facility that contradict the same evidence.  rid -> (old, new, reason)
CORR = {
 **{rid: ("2002", A02, "CONST") for rid in ("SR-13.10.3-2", "SR-13.10.3-3", "SR-13.10.3-8", "GR-13.10.4-1",
                                            "GR-13.10.4-2", "GR-13.10.4-3", "GR-13.10.4-4", "GR-13.10.4-5",
                                            "FMT-13.10.2")},
 "SR-13.18.14.3-1": ("85", A02, "COLSYN"), "SR-13.18.14.3-3": ("85", A85, "BASE85"),
 "SR-13.18.14.3-6": ("85", A85, "BASE85"), "SR-13.18.14.3-7": ("85", A02, "PRESENT"),
 "SR-13.18.14.3-8": ("85", A02, "PRESENT"), "GR-14.9.22.4-22": ("85", A85, "BASE85"),
 "GR-14.9.6.4-1": ("85,2002,2014", A85, "BASE85"), "GR-14.9.27.4-6": ("85,2002,2014", A85, "BASE85"),
 "GR-14.9.49.4-3": ("85,2002,2014", A85, "BASE85"),
 "GR-13.18.63.4-1": ("85,2002,2023", A02, "FLOAT02"), "GR-13.18.63.4-2": ("85,2023", A02, "INITVAL"),
 "GR-14.7.5-7": (A02, A14, "SD"), "GR-13.18.13.4-5": (A85, A02, "FMTSEL"),
 **{rid: (A85, A02, "FMTSEL") for rid in (
     [f"SR-13.18.24.3-{i}" for i in range(1, 4)] + [f"GR-13.18.24.4-{i}" for i in range(1, 13)]
     + [f"SR-13.18.51.3-{i}" for i in range(1, 7)] + [f"GR-13.18.51.4-{i}" for i in range(1, 6)]
     + ["FMT-13.18.24.2", "FMT-13.18.51.2", "DOC-A.1-84", "DOC-A.1-85", "DOC-A.1-173"])},
 "AR-15.69.3-1": (A14, A02, fn("NUMVAL-F", 2002)), "RV-15.69.4-2": (A14, A02, fn("NUMVAL-F", 2002)),
 "AR-15.64.3-1": (A02, A85, fn("MOD", 85)),
}

def main() -> int:
    out = pathlib.Path(sys.argv[1])
    rows = json.loads(pathlib.Path("tests/version-matrix/traceability-inventory.json").read_text(encoding="utf-8"))
    by = {r["rule-id"]: r for r in rows}
    empty = {r["rule-id"] for r in rows if not r["editions"]}
    bad = []
    if missing := sorted(empty - D.keys()):
        bad.append(f"undecided empty rows: {missing}")
    if extra := sorted(D.keys() - empty):
        bad.append(f"decided rows that are not empty: {extra}")
    constructs = {c["id"]: c for c in json.loads(pathlib.Path("tests/version-matrix/constructs.json")
                                                 .read_text(encoding="utf-8"))["constructs"]}
    words = {w["word"]: w for w in json.loads(pathlib.Path("tests/version-matrix/reserved-words.json")
                                             .read_text(encoding="utf-8"))["words"]}
    catalog_src = pathlib.Path("src/Cobol.Net.Compiler/Binding/IntrinsicCatalog.cs").read_text(encoding="utf-8")
    checked: dict[tuple, str | None] = {}

    def check(ev) -> str | None:
        if ev in checked:
            return checked[ev]
        kind, a, b = ev
        err = None
        if kind == "cite":
            p = subprocess.run([sys.executable, "scripts/spec/cite.py", "--check", a, b], capture_output=True,
                               text=True, encoding="utf-8")
            first = (p.stdout or p.stderr).splitlines()[0] if (p.stdout or p.stderr) else ""
            err = None if p.returncode == 0 and first.startswith("OK") else f"cite {a} {b!r}: {first}"
        elif kind == "construct":
            c = constructs.get(a)
            err = None if c and c["introducedIn"] == b else f"construct {a}: {c and c['introducedIn']} != {b}"
        elif kind == "intrinsic":
            m = re.search(r'Add\(new\("' + re.escape(a) + r'",[^;]*?\b(85|2002|2014|2023)\b\s*(?:,|\))', catalog_src)
            err = None if m and int(m.group(1)) == b else f"intrinsic {a}: {m and m.group(1)} != {b}"
        elif kind == "reserved":
            w = words.get(a)
            first = next((e for e, k in ((85, "r85"), (2002, "r2002"), (2014, "r2014"), (2023, "r2023"))
                          if w and w[k]), None)
            err = None if first == b else f"reserved {a}: first {first} != {b}"
        elif kind == "row":
            got = by.get(a, {}).get("editions")
            err = None if got == b else f"row {a}: {got!r} != {b!r}"
        checked[ev] = err
        return err

    for rid, (old, _, _) in CORR.items():
        if by.get(rid, {}).get("editions") != old:
            bad.append(f"correction {rid}: the row carries {by.get(rid, {}).get('editions')!r}, not {old!r}")
    records = []
    for rid in sorted(D) + sorted(CORR):
        old, band, reason = CORR[rid] if rid in CORR else ("", *D[rid])
        text, evidence = R[reason] if isinstance(reason, str) else reason
        for ev in evidence:
            if err := check(ev):
                bad.append(f"{rid}: {err}")
            if ev[0] in ("construct", "intrinsic", "reserved") and ev[2] != FIRST[band]:
                bad.append(f"{rid}: band {band} does not start at its evidence's edition {ev}")
        row = by[rid]
        cites = "; ".join(f"cite.py --check {e[1]} \"{e[2]}\" OK" for e in evidence if e[0] == "cite")
        what = f"editions corrected from '{old}' to {band}" if old else f"editions {band}"
        note = (f"{what} (kb/Work PB3173, S0b; DESIGN-conformance-suite.md §6 step 0): {text}."
                + (f" {cites}." if cites else ""))
        records.append({"rule-id": rid, "verdict": row["verdict"], "editions": band,
                        "notes": (row["notes"].rstrip() + " || " + note) if row["notes"] else note,
                        "derivation": row.get("derivation", "")})
    for b in bad:
        print("BAD:", b)
    if bad:
        return 1
    out.write_text(json.dumps({"batch": "pb3173-s0b-editions", "records": records}, indent=1, ensure_ascii=False)
                   + "\n", encoding="utf-8", newline="\n")
    print(f"{len(records)} records, {len(checked)} evidence items checked, all OK -> {out}")
    return 0

if __name__ == "__main__":
    sys.exit(main())
