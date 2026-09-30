using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CobolNet.Tests.Shared;
using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// Version-conformance corpus runner through the LEGACY engine — a DIFFERENTIAL oracle over the shared
/// <c>tests/conformance/&lt;version&gt;/*.cob</c> corpus.
///
/// <para>OPT-IN as of 2026-07-22 (owner decision, DEVLOG 997). The spec-first conformance fix-queue targets bugs the
/// FROZEN legacy oracle SHARES, so this byte-compare gives near-zero correctness signal and pure friction (every
/// spec-derived golden that corrects a shared bug would otherwise need a <see cref="GreenfieldOnly"/> exclusion). The
/// authoritative net is now the greenfield <c>CorpusRunnerTests</c> (same corpus, spec-derived goldens) plus the
/// GnuCOBOL EXTERNAL differential (an independent implementation, not our lineage). This suite therefore does NOT run
/// in the default/required gate — set <c>COBOLSHARP_LEGACY_DIFFERENTIAL=1</c> to enable it (e.g. the P14 Step-0
/// guard-equivalence proof, which still needs the legacy engine while it exists). No new GreenfieldOnly entries are
/// added going forward; the existing ones + <see cref="LegacyDivergent"/> stay for the opt-in run. Full legacy
/// deletion remains a P15 step.</para>
/// </summary>
public sealed class ConformanceTests : EndToEndTestBase
{
    private static readonly string ConformanceRoot = TestRepo.Tests("conformance");

    private static readonly (string Dir, DialectMode Dialect)[] Versions =
    {
        ("2002", DialectMode.Cobol2002),
        ("2014", DialectMode.Cobol2014),
        ("2023", DialectMode.Cobol2023),
    };

    /// <summary>Discover (version, program-name) for every conformance program with an expected-output file. The
    /// legacy differential is OPT-IN (see the class summary): with <c>COBOLSHARP_LEGACY_DIFFERENTIAL</c> unset, yield
    /// a single no-op sentinel so the theory stays alive but compiles nothing through the frozen legacy engine.</summary>
    public static IEnumerable<object[]> Cases()
    {
        if (Environment.GetEnvironmentVariable("COBOLSHARP_LEGACY_DIFFERENTIAL") is null)
        {
            yield return new object[] { "shell", "sentinel" };   // legacy differential decoupled from the gate (DEVLOG 997)
            yield break;
        }
        foreach (var (dir, _) in Versions)
        {
            string versionDir = Path.Combine(ConformanceRoot, dir);
            if (!Directory.Exists(versionDir)) continue;
            foreach (string cob in Directory.GetFiles(versionDir, "*.cob").OrderBy(p => p, StringComparer.Ordinal))
            {
                if (File.Exists(Path.ChangeExtension(cob, ".out")))
                    yield return new object[] { dir, Path.GetFileNameWithoutExtension(cob) };
            }
        }
    }

    /// <summary>Programs whose .out files are ISO-CONFORMING baselines the LEGACY engine cannot reproduce —
    /// the same divergence protocol as scripts/guard.sh's LEGACY_DIVERGENT list (per-program ISO citations).
    /// Both entries are the legacy DISPLAY trailing-space TRIM (non-conforming per ISO §14.9.11.4 GR6 — the
    /// greenfield emits the full field; the .out files were re-baselined at the W3 corpus audit, DEVLOG 597):
    /// the trimmed spaces sit INTERIOR to the expected lines ("DF=[   ]…"), so no whitespace normalization can
    /// bridge them. The greenfield CorpusRunnerTests asserts these baselines; the legacy runner SKIPS the
    /// output comparison (compile+run still asserted) until the G8 cut-over retires it.</summary>
    private static readonly HashSet<(string, string)> LegacyDivergent =
    [
        ("2002", "initialize_phrases"),
        ("2002", "table_value_occurs"),
        // Phase-4 track (a), M2-DATA-3: the .out was re-baselined to FULL-WIDTH DISPLAY (trailing national
        // spaces shown — PAD=P·· etc.) per the same §14.9.11.4 GR6 posture; the legacy's trailing-trim
        // cannot reproduce it. (boolean_data needs no entry — its values are exact-width, no trim exposure.)
        ("2002", "national_data"),
    ];

    /// <summary>Programs exercising features the FROZEN legacy engine never implemented (the OO deep-dive's
    /// "Never landed in legacy" list): greenfield-ONLY coverage — CorpusRunnerTests compiles, runs, and
    /// byte-compares them under the 2002 manifest run contract; the legacy runner skips them ENTIRELY (the
    /// legacy is kept only as a differential oracle until the G8 cut-over — no new legacy features, the
    /// DEVLOG-457 owner directive).</summary>
    private static readonly HashSet<(string, string)> GreenfieldOnly =
    [
        // ALLOCATE based-item INITIALIZED: the §14.9.3 GR7 lowering (INITIALIZE … WITH FILLER ALL TO VALUE
        // THEN TO DEFAULT) never landed in legacy — its CIL emitter only zero-fills (CilEmitter.cs), so the
        // GR7-conforming .out (VALUE members honored, numerics ZERO, edited zero, SPACES) is greenfield-only.
        // Wave H recognize-and-name (§4.2.6 ¶3): the MCS / COMMIT-ROLLBACK / VALIDATE statements exist only
        // in the greenfield grammar+binder (COBOLNET1578/1579/1580). The frozen legacy compiler has no such
        // statements at all — it still fails these programs at parse — so the golden is greenfield-only.
        ("2023", "wave_h_facilities_inert"),
        // §12.4.5.6 SUPPRESS WHEN on ALTERNATE RECORD KEY (COBOL-2023): the greenfield IndexedConnector filters
        // suppressed records off the alternate access path (invisible to alt READ/START) while leaving the prime
        // path intact. The frozen legacy compiler has no SUPPRESS WHEN grammar — greenfield-only.
        ("2023", "altkey_suppress_when"),
        // The §24 EC-seam batch goldens (F10 · V3 · V4): each exercises a >>TURN EC-… CHECKING ON directive over a
        // 2023-only construct (SET SIZE dynamic-length, COMP-2 float exceptions, EC-RANGE SEARCH/PERFORM-VARYING/
        // THROUGH). The frozen legacy compiler has neither the directive nor these constructs — it fails them at
        // parse — so the greenfield CorpusRunnerTests owns their byte-compare (DEVLOG 980–984).
        ("2023", "ec_storage_not_avail"),   // SET SIZE → EC-STORAGE-NOT-AVAIL (F10)
        ("2023", "ec_data_not_finite"),     // NaN/±Inf float sending operand → EC-DATA-NOT-FINITE (V3)
        ("2023", "ec_data_overflow"),       // MOVE overflow to a single-precision float → EC-DATA-OVERFLOW (V3)
        ("2023", "ec_range_search"),        // serial/SEARCH-ALL out-of-range → EC-RANGE-SEARCH-INDEX/-NO-MATCH (V4a)
        ("2023", "ec_range_perform_varying"),   // index-name varied from a non-positive item → EC-RANGE-PERFORM-VARYING (V4b)
        ("2023", "ec_range_invalid"),       // inverted THROUGH range → EC-RANGE-INVALID (V4c)
        // CONFORMANCE-FIX-QUEUE CA31/CA32 (EXIT PERFORM / EXIT PERFORM CYCLE inside a multi-level inline PERFORM
        // VARYING, §14.9.14.4 GR5a/GR5b): these are SPEC-DERIVED goldens fixing a bug the frozen legacy oracle
        // SHARES (a bare break/continue leaves/cycles only the innermost lowered loop) — the legacy runner would
        // mismatch the spec-correct .out (CA31) or INFINITE-LOOP (CA32, the augment is skipped). Greenfield owns
        // their byte-compare; the legacy oracle is frozen and not corrected (DEVLOG-457).
        ("2023", "exit_perform_multilevel"),   // EXIT PERFORM leaves the whole PERFORM (CA31)
        ("2023", "exit_perform_cycle"),        // EXIT PERFORM CYCLE runs loop control (CA32)
        // CONFORMANCE-FIX-QUEUE CA1/CA2 (INITIALIZE spec-derived goldens): CA1 (§14.9.20.4 GR7 dynamic-length →
        // length 0) uses PIC X DYNAMIC LENGTH — a 2014 clause the frozen legacy grammar cannot parse. CA2
        // (§14.9.20.4 GR4/GR6c INITIALIZE program-pointer → SET TO NULL) fixes a bug the legacy oracle SHARES
        // (INITIALIZE of a pointer/object-ref emitted nothing). Greenfield owns their byte-compare.
        ("2014", "initialize_dynamic_length"),   // INITIALIZE dynamic-length item → length 0 (CA1)
        ("2002", "initialize_program_pointer"),  // INITIALIZE program-pointer → SET TO NULL (CA2)
        // SUPPRESS PRINTING (§14.9.45): the statement exists only in the greenfield grammar+binder+RW engine.
        // The frozen legacy compiler has no SUPPRESS grammar at all — it fails the program at parse — so the
        // golden (a suppressed detail's amount still rolls into the control total, §13.18.54.4 GR7/GR2) is
        // greenfield-only.
        ("2002", "rw_suppress"),
        // §12.4.5.7 file-control COLLATING SEQUENCE: per-key collating weights on the greenfield IndexedConnector
        // (reversed/equivalence-class key ordering + uniqueness). The frozen legacy compiler has no such clause,
        // so this golden is greenfield-only.
        ("2002", "file_collating_seq"),
        ("2002", "allocate_initialized"),
        // ARITHMETIC IS STANDARD full consumption (P10 Step 12): SDIDI exponentiation (§8.8.1.5.4 — CobolDec.Pow),
        // the decimal128 range ECs (§8.8.1.5.2 r2), float→SDIDI operand conversion (§8.8.1.5.1), and the
        // §15.4.1 r1 MEAN-in-SDIDI equality never landed in legacy (its standard-arith engine covers only the
        // basic four ops) — greenfield-only.
        ("2002", "arith_standard"),
        ("2002", "oo_factory"),         // the FACTORY paragraph (ISO §11.4) — net-new in the greenfield (DEVLOG 604)
        ("2002", "oo_factory_file"),    // FACTORY-paragraph FILE-CONTROL + FILE SECTION (M2-OO-1i inc 3) — net-new (DEVLOG 646)
        ("2002", "oo_object_file"),     // OBJECT-paragraph per-object file connector (M2-OO-1i inc 4) — net-new (DEVLOG 647)
        ("2002", "oo_object_file_two_instances"),   // per-object connector independence (M2-OO-1i inc 4) — net-new (DEVLOG 647)
        ("2002", "oo_external_file_shared"),   // EXTERNAL FD shared program<->object: one connector + record area (M2-OO-1i inc 5) — net-new (DEVLOG 648)
        ("2002", "oo_object_report"),   // REPORT SECTION in an OBJECT paragraph (M2-OO-1i review — Report Writer wired into the class path) — net-new (DEVLOG 649)
        ("2002", "oo_method_sort"),     // OBJECT-paragraph SD sorted by a method (M2-OO-1i review — SD keeps a static key) — net-new (DEVLOG 649)
        // Phase-6a floats (D16): the raw-float-DISPLAY goldens use CobolFloat.Display (invariant shortest round-trip,
        // §14.9.11 GR1 implementor-defined) — NOT the legacy oracle's form — and float→PIC rounding — so GreenfieldOnly.
        ("2002", "float_move"),         // float→float MOVE, raw-float DISPLAY (DEVLOG 650)
        ("2002", "float_neg"),          // negative/fractional literal → float, raw-float DISPLAY (DEVLOG 650)
        ("2002", "float_rounded"),      // float→PIC 9(2) truncate vs ROUNDED (DEVLOG 650)
        ("2002", "float_compare"),      // float vs fixed/int comparison (DEVLOG 650)
        ("2002", "float_literal"),      // floating-point exponent-form literals 1.5E3 (§8.3.3.3.3, DEVLOG 651)
        ("2002", "float_edited"),       // float source → numeric-edited receiver (DEVLOG 651)
        ("2002", "float_intrinsic"),    // SQRT/÷ into a float receiver, full binary64 precision (DEVLOG 651)
        ("2002", "float_88"),           // fractional level-88 VALUE on a float item (DEVLOG 651)
        ("2002", "oo_override_final"),  // METHOD-ID OVERRIDE/IS FINAL attributes (§11.7) — net-new (DEVLOG 605)
        // These three gained the SR4a-required OVERRIDE attribute (the strict §11.7 wave) — the frozen legacy
        // grammar cannot parse it; the greenfield CorpusRunnerTests byte-compares them:
        ("2002", "oo_inherit"),
        ("2002", "oo_super"),
        ("2002", "oo_self_polymorphic"),
        // The INTERFACE/PROPERTY wave (§11.5/§11.6/§13.18.42) — net-new in the greenfield (DEVLOG 606):
        // DISPLAY-OF/NATIONAL-OF on the greenfield substrate (P10 national wave): the legacy oracle carries
        // its OWN pass-through DISPLAY-OF approximation (no repertoire substitution) — never a match target.
        ("2002", "national_intrinsics"),
        // ALPHABET … FOR NATIONAL + PROGRAM COLLATING SEQUENCE FOR NATIONAL (P10 Step 4, §12.3.7/§12.3.6):
        // the legacy engine never modeled a national collating sequence (its SemanticBuilder keeps the 85
        // single-name PCS view) — greenfield-only.
        ("2002", "alphabet_national"),
        // P10 Step-11 EC-N wave: the national EC twins EXCEPTION-FILE-N/EXCEPTION-LOCATION-N (§15.29/§15.31)
        // and CHAR-NATIONAL/ORD-over-national (§15.16/§15.70.4 r2) — the frozen legacy has no EC model and no
        // national result category; greenfield CorpusRunner byte-compares both.
        ("2002", "exception_file_n"),
        ("2002", "char_national"),
        // P10 Step 14: §8.8.3 concatenation expressions (the & operator) — the frozen legacy grammar has no
        // & token at all; greenfield CorpusRunner byte-compares the golden.
        ("2002", "literal_concat"),
        // P11 Step 2: FUNCTION BOOLEAN-OF-INTEGER / INTEGER-OF-BOOLEAN (§15.13/§15.45) — the frozen legacy
        // has no boolean intrinsic functions (its catalog predates the greenfield IntrinsicCatalog);
        // greenfield CorpusRunner byte-compares the golden.
        ("2002", "intrinsics_boolean_conv"),
        // P11 Step 5: the Y2K windowing trio + SECONDS-PAST-MIDNIGHT (§15.23/§15.25/§15.100/§15.80) — the
        // frozen legacy has none of the 2002 date functions; greenfield CorpusRunner byte-compares the golden.
        ("2002", "intrinsics_date_window"),
        // P11 Step 6: the TEST validator quartet (§15.90/§15.91/§15.93/§15.94) — the frozen legacy has no
        // TEST-* functions; greenfield CorpusRunner byte-compares the golden.
        ("2002", "intrinsics_test_validators"),
        // P11 Step 7: BYTE-LENGTH (§15.14) — the frozen legacy has no BYTE-LENGTH function (its national
        // byte-geometry differs anyway); greenfield CorpusRunner byte-compares the golden.
        ("2002", "intrinsics_byte_length"),
        // P11 Step 7: SMALLEST-ALGEBRAIC (§15.83, new-in-2023) — the frozen legacy predates it; greenfield
        // CorpusRunner byte-compares the golden.
        ("2023", "intrinsics_smallest_algebraic"),
        // P10 Step 15: §13.10 constant entries + §13.18.15 CONSTANT RECORD — the compile-time constant table
        // (DataBinder.Constants.cs) never landed in legacy; greenfield CorpusRunner byte-compares both.
        ("2002", "constant_entry"),
        ("2002", "constant_record"),
        // P10 Step 6: qualified/subscripted ADDRESS OF (§8.4.3.11 GR1 occurrence addressing) — the pointer
        // subsystem never landed in legacy; greenfield CorpusRunner byte-compares it.
        ("2002", "address_of_qualified"),
        // P10 Step 7: USAGE PROGRAM-POINTER (§13.18.60 GR24 — SET TO ENTRY / CALL-through-pointer /
        // relations on the ProgramPointer carrier) — never landed in legacy.
        ("2002", "program_pointer"),
        // P10 RECURSIVE-WS slice: the §13.5.4 GR1 / §14.6.2.3.2/.3 activation-state model (a RECURSIVE
        // unit's WS = ONE static last-used copy; LOCAL-STORAGE = per-activation initial state; CANCEL
        // resets the statics per §14.9.5 GR3) — the legacy byte engine re-initializes a recursive unit's
        // WS per activation and never bound program-level LOCAL-STORAGE; greenfield-only coverage.
        ("2002", "recursive_ws"),
        // Report-group PRESENT WHEN + VARYING + multiple COLUMN (§13.18.41/§13.18.64/§13.18.14, P10 Step 13)
        // — the 2002 RW clauses never landed in the legacy Report Writer; greenfield-only coverage.
        ("2002", "rw_present_when"),
        ("2002", "oo_interface"),
        ("2002", "oo_interface_conformance"),
        ("2002", "oo_class_env"),   // class-level env + object-own env both bind (DEVLOG-738 fix) — OBJECT-paragraph env never landed in legacy   // the §4.2.2 interface conformance leg (P9 Step 12) — interfaces never landed in legacy
        ("2002", "oo_interface_covariant"),
        ("2002", "oo_property"),
        ("2002", "oo_property_methods"),
        // The property-REFERENCE increment (§8.4.3.9.4 GR1–GR3 desugar) — net-new (DEVLOG 607):
        ("2002", "oo_property_ref"),
        ("2002", "oo_property_explicit_ref"),
        ("2002", "oo_property_factory_ref"),
        // The UNIVERSAL wave (D10, §13.18.60.4/§14.9.23 GR7c) — net-new (DEVLOG 608):
        ("2002", "oo_universal"),
        ("2002", "oo_universal_name"),
        ("2002", "oo_universal_inherit"),
        ("2002", "oo_universal_relation"),
        // The EC-OO wave (§14.9.29 RAISE identifier / §14.9.49 F4 / §14.6.13.1.5) — net-new (DEVLOG 609):
        ("2002", "oo_ec_raise_object"),
        ("2002", "oo_ec_goback_raising"),
        // The ANY LENGTH clause (§13.18.2, PHASE-09 Step 11) — never landed in the frozen legacy grammar; the
        // greenfield CorpusRunnerTests byte-compares all three legs (method / contained program / function):
        ("2002", "oo_any_length"),
        ("2002", "any_length_contained"),
        ("2002", "any_length_function"),
        // Phase-4 track (e), DEVLOG 611: ARITHMETIC IS STANDARD routes fixed-point through the standard
        // DECIMAL intermediate (§8.8.1.2/§8.8.1.4 — 2/7*7 = 2.00000); the FROZEN legacy engine does not
        // implement that routing (it gives the native-clipped 1.99997), so this is greenfield-only.
        ("2014", "options_paragraph"),
        // P7 Step 12 (FUNCTION-arg grammar): the golden pins §8.8.1.2 r3 — consecutive same-level operations
        // execute LEFT to right, INCLUDING ** (2 ** 3 ** 2 = 64). The frozen legacy's arithmetic parser folds
        // ** right-associatively (512) — a known legacy non-conformance; greenfield-only.
        ("2014", "func_expr_arg"),
        // Same family (P3 step 9): ARITHMETIC IS STANDARD-DECIMAL routes the fixed-point intermediate through the
        // standard DECIMAL data item (§8.8.1.4 — 2/7*7 = 2.00000); the frozen legacy engine doesn't implement it.
        ("2014", "arithmetic_standard_decimal"),
        // Phase-4 track (d), DEVLOG 612: DELETE FILE (§14.9.10 Format 2) is a 2023 construct the frozen
        // legacy grammar cannot parse; the greenfield CorpusRunner byte-compares the sequential leg.
        ("2023", "delete_file"),
        ("2023", "delete_file_absent"),
        // P10 Step 8: the DELETE FILE '62' file-sharing conflict (§9.1.13.9 item 2 / §14.9.10 GR15 + RETRY) —
        // same 2023 construct family; greenfield CorpusRunner byte-compares it.
        ("2023", "delete_file_sharing"),
        // Phase 5 intrinsics, DEVLOG 628: the 2023 CONCAT (§15.18) + BASECONVERT (§15.12) intrinsics — the frozen
        // legacy has CONCAT but crashes on BASECONVERT (InvalidCastException); the greenfield CorpusRunner
        // byte-compares intrinsics_string_2023.
        ("2023", "intrinsics_string_2023"),
        // Phase 5 intrinsics, DEVLOG 629: FUNCTION TRIM (§15.96) with the 2023 argument-2 char-set form — the
        // frozen legacy trims only spaces (ZERO=0042, ignoring the "0" arg-2) where the greenfield gives ZERO=42;
        // the greenfield CorpusRunner byte-compares intrinsics_trim.
        ("2023", "intrinsics_trim"),
        // Phase 5 intrinsics, DEVLOG 630: FUNCTION FIND-STRING (§15.37) with the LAST / START AFTER / ANYCASE
        // phrase keywords is a 2023 construct the frozen legacy grammar cannot bind; the greenfield CorpusRunner
        // byte-compares find_string.
        ("2023", "find_string"),
        // Phase 5 intrinsics, DEVLOG 631: FUNCTION SUBSTITUTE (§15.87) with per-pair ANYCASE/FIRST/LAST phrase
        // keywords is a 2023 construct the frozen legacy grammar cannot bind; the greenfield CorpusRunner
        // byte-compares substitute.
        ("2023", "substitute"),
        // Phase 5 intrinsics, DEVLOG 632: FUNCTION CONVERT (§15.19) with the source-format/destination-format
        // keywords (ANY/ANUM/HEX/NAT/BYTE) is a 2023 construct the frozen legacy grammar cannot bind; the
        // greenfield CorpusRunner byte-compares intrinsics_convert.
        ("2023", "intrinsics_convert"),
        // Phase 5 intrinsics, DEVLOG 633: FUNCTION MODULE-NAME (§15.65) with the ACTIVATING/CURRENT/NESTED/
        // STACK/TOP-LEVEL keyword is a 2023 construct the frozen legacy grammar cannot bind; the greenfield
        // CorpusRunner byte-compares module_name (a CALL chain with a contained program).
        ("2023", "module_name"),
        // Phase 5 intrinsics, DEVLOG 634: FUNCTION HIGHEST-/LOWEST-ALGEBRAIC (§15.43/§15.58, 2002) and
        // SMALLEST-ALGEBRAIC (§15.83, 2023) are PICTURE-metadata folds the frozen legacy does not implement;
        // the greenfield CorpusRunner byte-compares them.
        ("2002", "highest_lowest_algebraic"),
        ("2023", "smallest_algebraic"),
        // Phase 5 intrinsics, DEVLOG 635: the COBOL-2014 date/time + number family (FORMATTED-*/COMBINED-DATETIME/
        // INTEGER-OF-FORMATTED-DATE/SECONDS-FROM-FORMATTED-TIME/TEST-FORMATTED-DATETIME/NUMVAL-F/TEST-NUMVAL-F,
        // §15.17/38-41/48/69/79/92/95) — the frozen legacy oracle predates them; greenfield CorpusRunner byte-compares.
        ("2014", "formatted_datetime"),
        // Phase-4 track (c), DEVLOG 615/616: the frozen legacy's partial UDF support runs the simple
        // invocation goldens but lacks EXIT FUNCTION's control transfer (§14.9.14 — it falls through to the
        // trailing MOVE, X=9999 not X=0014) and the nested-args legs (GR5a by-ref argument mutation /
        // intrinsic-in-UDF); the greenfield CorpusRunner byte-compares both.
        ("2002", "udf_exit_function"),
        ("2002", "udf_nested_args"),
        // Phase-4 track (c) residue, DEVLOG 624: FUNCTION-ID … IS PROTOTYPE (§11.5 Format 2) is a construct the
        // frozen legacy grammar cannot parse; the greenfield CorpusRunner byte-compares udf_prototype.
        ("2002", "udf_prototype"),
        // P10 Step 9 (DEVLOG 859): category-carrying FUNCTION-ID RETURNING (§8.4.3.2.4 GR1) — alphanumeric/
        // group/numeric-edited/national results; the frozen legacy's partial UDF support carries only numeric
        // results (and has no national category at all); the greenfield CorpusRunner byte-compares it.
        ("2002", "udf_returning_categories"),
        // P10 Step 10: BY VALUE formal parameters in the PD header (§14.2.2 using-phrase / §14.2.3 GR10) and
        // per-evaluation function activation (§8.4.3.2.4 GR1/GR6a — UNTIL/WHEN/EVALUATE-object/short-circuit
        // windows) are net-new in the greenfield — the frozen legacy grammar has no header BY VALUE phrase and
        // its UDF support predates evaluation-cardinality; the greenfield CorpusRunner byte-compares both.
        ("2002", "udf_by_value"),
        ("2002", "udf_per_eval"),
        // Phase-4 track (c) residue, DEVLOG 626: the §8.4.3.2 SR2 FUNCTION-keyword-omitted reference form (via
        // REPOSITORY FUNCTION ALL INTRINSIC) is net-new in the greenfield — the frozen legacy treats MAX/MIN/MOD
        // without FUNCTION as undefined data-names (CBL3128); the greenfield CorpusRunner byte-compares it.
        ("2002", "udf_keyword_omitted"),
        // Phase-4 track (a) increment 2, DEVLOG 621: the boolean operators B-AND/B-OR/B-XOR/B-NOT (§8.7.2)
        // are net-new in the greenfield — the frozen legacy grammar has no boolean-expression support at all;
        // the greenfield CorpusRunner byte-compares boolean_ops.
        ("2002", "boolean_ops"),
        // M2-OO-1h step 1, DEVLOG 637: level-66 RENAMES in a METHOD's LOCAL-STORAGE — a greenfield method-scope
        // data-model reclamation (was staged COBOLNET0899); the frozen legacy predates it. Greenfield CorpusRunner
        // byte-compares oo_method_renames.
        ("2002", "oo_method_renames"),
        // M2-OO-1h step 2, DEVLOG 638: OCCURS … DEPENDING ON in a METHOD's LOCAL-STORAGE (method-scoped
        // data-name-1) — greenfield reclamation (was COBOLNET0899). Greenfield CorpusRunner byte-compares oo_method_odo.
        ("2002", "oo_method_odo"),
        // M2-OO-1h step 3, DEVLOG 639: REDEFINES in METHOD data (scope-aware target; Tier-B string backing routed
        // static/method-local) — greenfield reclamation (was COBOLNET0899). Greenfield CorpusRunner byte-compares both.
        ("2002", "oo_method_redefines_linkage"),
        ("2002", "oo_method_redefines_local"),
        // M2-OO-1h step 4, DEVLOG 640: OCCURS … INDEXED BY in METHOD data (per-method index namespace; §11.7.4 GR5
        // privacy) — greenfield reclamation (was COBOLNET0899). Greenfield CorpusRunner byte-compares both.
        ("2002", "oo_method_indexed_search"),
        ("2002", "oo_method_indexed_two_methods"),
        // Phase-4 track (d), DEVLOG 623: the file-sharing / record-locking subsystem (SHARING / LOCK MODE /
        // RETRY / WITH LOCK / IGNORING LOCK / UNLOCK — §14.9.27/.30/.47) is net-new in the greenfield; the
        // frozen legacy binder has only the by-name CLOSE-WITH-LOCK/38 primitive and cannot bind these clauses.
        // The greenfield CorpusRunner byte-compares file_sharing (two connectors → 61/51/00 in one run unit).
        ("2002", "file_sharing"),
        // P10 Step 8 (§9.1.16 completion): record locking on the SEQUENTIAL organization (ordinal lock
        // identity, WITH LOCK/RETRY/IGNORING/ADVANCING-capable READ, REWRITE/WRITE lock discipline) and the
        // mutating-verb conflict checks (REWRITE/DELETE 51 + RETRY, WRITE WITH LOCK §14.9.51 GR11) — the
        // frozen legacy binder cannot bind any of these clauses; greenfield CorpusRunner byte-compares both.
        ("2002", "file_sharing_seq"),
        ("2002", "file_sharing_mutate"),
        // Phase 6, TYPEDEF / the TYPE clause (data-model D17, §13.18.58/§13.18.57): a type-declaration template +
        // subtree clone. The frozen legacy has no TYPEDEF model — greenfield CorpusRunner only.
        ("2002", "typedef_weak_elem"),
        ("2002", "typedef_weak_group"),
        ("2002", "typedef_strong_ok"),   // TYPEDEF inc 2: a STRONG type + same-type MOVE/compare (D17, §8.5.3.3) — net-new
        ("2002", "typedef_88"),          // TYPEDEF inc 3: level-88 condition-names cloned per TYPE reference (D17, §13.18.58.4 GR1) — net-new
        ("2002", "typedef_indexed"),     // TYPEDEF inc 4: a single INDEXED-BY type reference (D17, §13.18.38) — net-new
        ("2002", "typedef_nested_strong"), // TYPEDEF review (DEVLOG 664): nested strong types + same-type by nearest anchor — net-new
        ("2002", "typedef_odo"),         // TYPEDEF review (DEVLOG 664): cloned OCCURS DEPENDING binds the clone's own counter — net-new
        ("2002", "typedef_same_as"),     // P10 Step 16: SAME AS (§13.18.49) via the ONE CloneItem — net-new, never landed in legacy
        ("2002", "typedef_external"),    // P10 Step 16: EXTERNAL type declaration (§13.18.22 GR3) — net-new, never landed in legacy

        // Phase 6, OCCURS DYNAMIC increment 1 (data-model D9, §13.18.38 Format 4 / §8.5.1.9): the
        // dynamic-capacity table declaration + the growable CobolDynTable<T> storage substrate. The frozen
        // legacy binder/emitter has no dynamic-table model (it predates the D9 rewrite), so although the shared
        // grammar now parses OCCURS DYNAMIC, the legacy engine cannot emit it — greenfield CorpusRunner only.
        ("2014", "dyn_declare"),
        // Phase 6, OCCURS DYNAMIC increment 2 (data-model D9, §13.18.38 GR15 / §14.9.39): the CAPACITY register
        // (an implicit view over the table's current capacity) + SET Format 14 (TO / UP BY / DOWN BY). The frozen
        // legacy has no dynamic-table model — greenfield CorpusRunner only.
        ("2014", "dyn_capacity_read"),
        ("2014", "dyn_capacity_set"),
        ("2014", "dyn_capacity_bounds"),
        // Phase 6, OCCURS DYNAMIC increment 3 (data-model D9, §8.5.1.9.2/.9.3): subscripted element access — a
        // receiving subscript past capacity grows-and-seeds (RefReceiving), a sending OOB is benign (RefSending).
        // Greenfield CorpusRunner only.
        ("2014", "dyn_implicit_grow"),
        ("2014", "dyn_initialized"),
        // Phase 6, OCCURS DYNAMIC increment 4 (data-model D9, §14.9.37 / §14.9.20 GR10): SEARCH bounds over the
        // table's current Capacity (EnterSearch/ExitSearch bracket → EC-FLOW-SEARCH); INITIALIZE re-initializes
        // every occurrence up to Capacity with the statement's category defaults. Greenfield CorpusRunner only.
        ("2014", "dyn_search"),
        ("2014", "dyn_initialize"),
        // Phase 6, OCCURS DYNAMIC adversarial review (wf_3f05d472-ad8) regressions — data-model D9. Greenfield only.
        ("2014", "dyn_nested_group_move"),   // #1 a group MOVE nested below a dynamic level grows via RefReceiving
        ("2014", "dyn_corr"),                // #2 CORRESPONDING excludes a dynamic-table member (§14.7.6 rule 4)
        // PHASE-12 wave 2, DYNAMIC LENGTH elementary items (§8.5.1.10 / §13.18.19): a variable-length min-0 string
        // (CobolDynString + StorageForm.DynamicString). The frozen legacy binder/emitter has no dynamic-length
        // model — it treats `PIC X DYNAMIC LENGTH` as a fixed PIC X (width 1), so the current-length behavior is
        // greenfield-only; CorpusRunnerTests byte-compares both goldens.
        ("2014", "dynamic_length_item"),
        ("2014", "dynamic_length_limit"),
        ("2014", "dynamic_length_figurative"),
        // PHASE-12 wave 3, FLOAT-BINARY-32/64 (§13.18.60.4 GR14/GR15): the IEEE-754 binary32/binary64 interchange
        // USAGES — the frozen legacy grammar has no FLOAT-BINARY-* tokens at all (parse error), so this is
        // greenfield-only; CorpusRunnerTests byte-compares the golden.
        ("2014", "float_binary"),
        // PHASE-13 EC remnants: the EC engine (>>TURN / EXCEPTION-STATUS / the named nonfatal-EC model) is
        // greenfield-only; the frozen legacy has a limited EC model. CorpusRunnerTests byte-compares these.
        ("2023", "ec_size_truncation_prohibited"),
        // EC-BOUND-REF-MOD raise (§8.4.3.3.4) + EC-BOUND-OVERFLOW (§8.5.1.9.6 GR1): >>TURN-gated fatal/nonfatal
        // EC raises with USE-F3 declaratives + FUNCTION EXCEPTION-STATUS — the greenfield EC model the frozen
        // legacy lacks. These two exclusions were OMITTED when the goldens landed (commits 3eebcfd1 / e574af41
        // did not touch this list, DEVLOG 897 latent-miss fix); the legacy runner cannot compile/match them.
        ("2002", "ec_bound_ref_mod"),
        ("2014", "ec_bound_overflow"),
        // REF-MOD-ZERO-LENGTH directive (§7.3.23) — a COBOL-2023 compiler directive + the EC-BOUND-REF-MOD raise;
        // the frozen legacy has neither the directive nor the EC model. CorpusRunnerTests byte-compares it.
        ("2023", "ref_mod_zero_length"),
        // PHASE-13 Wave C batch 1 — COBOL-2023 grammar constructs the frozen legacy compiler has no support for
        // (grammar/binder/runtime landed greenfield-only). CorpusRunnerTests byte-compares these at --std 2023.
        ("2023", "usage_packed_no_sign"),   // USAGE PACKED-DECIMAL WITH NO SIGN (§13.18.60.4 GR11)
        ("2023", "set_size"),               // SET [SIZE OF] dyn-length TO n (§14.9.39 Format 16)
        ("2023", "continue_after"),         // CONTINUE AFTER n SECONDS + EC-CONTINUE-LESS-THAN-ZERO (§14.9.9)
        ("2023", "perform_until_exit"),     // PERFORM … UNTIL EXIT (§14.9.28.4 GR11)
        ("2023", "boolean_shift"),          // boolean shift B-SHIFT-L/R/LC/RC (§8.8.2 rule 8)
        // §8.8.2 rule 7b — a boolean shift MIXED with a binary boolean operator (grouped per the shift's context-
        // inherited precedence). The frozen legacy has no boolean-expression support at all (like boolean_ops/
        // boolean_shift), so this rule-7b golden is greenfield-only. (ledger C2 — the shared BooleanExpressionResolver.)
        ("2023", "boolean_shift_mixed"),
        // §7.2.1 CC-in-COPY — the merged interleaved text-manip driver processes directives INSIDE copybooks; the
        // frozen legacy runs CC before COPY, so a copybook >>IF leaks to the lexer. Greenfield-only.
        ("2023", "cc_in_copy"),
        // §7.3.10 >>COBOL-WORDS — the frozen legacy only recognizes-and-blanks the directive (KnownIgnoredDirectives),
        // so it cannot re-tokenize a synonym/de-reserved word; the token rewriter + composed ReservedWordSet are
        // greenfield-only. CorpusRunnerTests byte-compares these at --std 2023.
        ("2023", "cobol_words_equate"),     // EQUATE DISPLAY WITH SHOW (§7.3.10.4 GR2)
        ("2023", "cobol_words_substitute"), // SUBSTITUTE DISPLAY BY SHOW (§7.3.10.4 GR4)
        ("2023", "cobol_words_undefine"),   // UNDEFINE MOVE, used as a subscripted data-name (§7.3.10.4 GR3)
        ("2023", "cobol_words_intrinsic"),  // EQUATE MAX WITH LARGEST — an intrinsic-function-name synonym (§7.3.10.4 GR2)
        ("2023", "goback_status"),          // GOBACK … WITH NORMAL/ERROR STATUS (§14.9.18.2) — 2023 phrase
        ("2023", "write_before_and_after"), // WRITE … BEFORE AFTER ADVANCING … — ONE advance, both words (§14.9.51.2 Format 1 / GR25 e-f; kb/Work PB712)
        // PHASE-13 Wave G — numeric-edited VALUE 2023 rework (§13.18.63 SR6/SR11; VCR 35 + 86): a figurative ZERO
        // on a numeric-edited item edits per PICTURE (was the left-justified "0000000"), BLANK WHEN ZERO effects
        // init, and a non-zero numeric literal VALUE is permitted. The frozen legacy engine produces the pre-2023
        // zero-fill and has no numeric-literal-on-numeric-edited path — greenfield CorpusRunner byte-compares it.
        ("2023", "value_numeric_edited"),
        // PHASE-13 Wave G VCR 68/69 — FUNCTION EXCEPTION-FILE(file-connector-name) arg form (§15.28.4 r2): a
        // greenfield EC + FileRegistry feature the frozen legacy has no EC model for. CorpusRunner byte-compares it.
        ("2023", "exception_file_arg"),
        // PHASE-13 Wave G VCR 21 — I-O status '04' on a record-sequential READ whose physical record is outside the
        // file's min/max size (§14.9.30 GR14 — the READ statement's record-length rule). New greenfield SequentialConnector behavior; the frozen legacy's
        // record-length handling is not modified here, so the greenfield CorpusRunner byte-compares it.
        ("2002", "io_status_04"),
        // PHASE-13 Wave E VCR 63 — the STRONGLY-TYPED external type declaration (§13.18.22/§13.18.58.3; 2023):
        // the frozen legacy grammar cannot parse STRONG TYPEDEF + EXTERNAL TYPE surface. ⚠ This exclusion was
        // OMITTED when the golden landed (9a33dfa7 — the third instance of the latent-miss class the
        // legacy-suite-on-shared-corpus rule exists for); caught by the Wave E pt3 legacy-suite run (DEVLOG 909).
        ("2023", "external_type_decl"),
        // PHASE-13 review-fix (C9) — the VCR 16 strength-half 2014 continuity witness: a WEAK typedef satisfying
        // TYPE on an EXTERNAL CONSTANT RECORD below 2023. The legacy engine's typedef/constant-record composition
        // diverges on this shape (verified by suite run) — greenfield CorpusRunner byte-compares it.
        ("2014", "external_constant_record_weak_type"),
        // PHASE-13 Wave E VCR 15 — the EC-EXTERNAL-* run-unit conformance raises (§14.8.4 / §14.9.4.4 GR3e):
        // >>TURN-gated activation-entry descriptor checks over the run-unit ExternalTable — the greenfield EC
        // model + descriptor machinery the frozen legacy has neither of. CorpusRunner byte-compares all three.
        ("2023", "ec_external_format_conflict"),
        ("2023", "ec_external_file_mismatch"),
        ("2023", "ec_external_data_mismatch"),
    ];

    [Theory]
    [MemberData(nameof(Cases))]
    public void Conformance(string version, string name)
    {
        if (version == "shell") return;   // legacy differential opt-out sentinel (see Cases / the class summary)
        if (GreenfieldOnly.Contains((version, name))) return;   // never landed in legacy — greenfield coverage only
        DialectMode dialect = Versions.First(v => v.Dir == version).Dialect;
        string source = File.ReadAllText(Path.Combine(ConformanceRoot, version, name + ".cob"));
        string expected = Normalize(File.ReadAllText(Path.Combine(ConformanceRoot, version, name + ".out")));

        var (ok, stdout, stderr) = CompileAndRun(source, dialect);
        Assert.True(ok, $"[conformance {version}/{name}] compile/run failed:\n{stderr}");
        if (LegacyDivergent.Contains((version, name))) return;   // ISO-adjudicated divergence — see the list
        Assert.Equal(expected, Normalize(stdout));
    }

    private static string Normalize(string s) => s.Replace("\r\n", "\n").Replace("\r", "\n").TrimEnd();
}
