---
title: Lookup — Diagnostic Codes (COBOLNET####)
area: spec-lookup
status: draft
last_updated: 2026-07-23
related_files:
  - docs/DIAGNOSTICS.md
  - src/Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs
  - tests/version-matrix/constructs.json
tags:
  - cobolsharp
  - spec
  - lookup
---

# Lookup — Diagnostic Codes (COBOLNET####)

Every diagnostic code → its meaning, severity, ISO §, the **pipeline phase** that raises it, and the construct(s) it
gates. The generated SSOT for the first-class descriptors is [[docs/DIAGNOSTICS]] (source
`DiagnosticCatalog.cs`, drift-guarded). A **`Code` may repeat** — the emitted number is byte-stable, the kebab-case
**Id** is the identity. This note adds the **Phase** and **construct** columns the catalog doesn't carry (so it also
serves as the diagnostic→phase map). See [[kb/Semantics/Validation Rules]] and
[[kb/Compiler/Pipeline-to-ISO-Mapping]].

> **Phase legend** — **Preprocess** = directive/text stage · **Bind** = binder-invariant check · **Validate** =
> `VersionConformancePass` (edition gate) / `FlagConformancePass`. See [[kb/Semantics/Passes]].

## Edition & conformance band
| Code | Meaning (Id) | Sev | ISO § | Phase | Construct / where |
|---|---|---|---|---|---|
| `COBOLNET0900` | construct used below its introducing edition (`edition-introduction`) | Error | §ann. per construct | Validate **and** Preprocess | most edition-gated constructs (the generic intro gate) → [[kb/Spec/Lookup/Construct Catalogue]]; **also every ISO §7.3 compiler directive** — the whole family shares this one band, checked where the text-manipulation stage recognizes a `>>` word (kb/Work PB725 retired `>>TURN`'s bespoke COBOLNET0875) |
| `COBOLNET0901` | word reserved in the targeted edition used as a user word (`edition-reserved-word`) | Error | §8.9 | Validate | the §8.9 reserved-word funnel → [[kb/Spec/Lookup/Semantic Rules]] |
| `COBOLNET0902` | construct removed by the targeted edition (`edition-removed-construct`) | Error / Warn (permissive) | Annex E.2 | Validate | ALTER, STOP literal, LABEL RECORDS, OPEN REVERSED, … |
| `COBOLNET0903` | obsolete / archaic element (`edition-obsolete-flag`) | Warning | §4.2.12/.13, Annex F.2 | Validate | NEXT SENTENCE, EXIT PROGRAM (archaic 2023), col-7 continuation |
| `COBOLNET1533` | strong-type: class-condition / compare / MOVE mismatch (3 ids) | Error | §8.8.4.4.3 SR1 · §8.8.4.2.3 SR1 · §14.9.25.3 SR2 | Bind | strongly-typed group misuse |
| `COBOLNET1535` | strong-group ordering illegal / TYPEDEF-RENAMES staged (2 ids) | Error | §8.8.4.2.3 SR4 · §13.18.58.4 GR1 | Bind | strong-type ordering |
| `COBOLNET1560`* | §4.2.6 processor-dependent unsupported (band) | Warning | §4.2.6 ¶3 | Validate | documented non-support surface (see 1578–1580) |

## Documented non-support (recognized + warned/rejected)
| Code | Meaning (Id) | Sev | ISO § | Phase | Facility |
|---|---|---|---|---|---|
| `COBOLNET1578` | MCS SEND/RECEIVE unsupported | Warning | §4.2.6 / Annex A.3.4 / §14.9.31/.38 | Validate | asynchronous messaging |
| `COBOLNET1579` | COMMIT/ROLLBACK unsupported (behaves as CONTINUE) | Warning | §4.2.6 / A.3.6-7 / §14.9.7/.36 | Validate | commit/rollback |
| `COBOLNET1580` | VALIDATE unsupported (optional + obsolete 2023) | Warning | §4.2.7 / A.4.14 / §14.9.50 | Validate | VALIDATE |

## Directive diagnostics (Preprocess / directive passes)
| Code | Meaning (Id) | Sev | ISO § | Phase | Directive |
|---|---|---|---|---|---|
| `COBOLNET0718` | `>>TURN` malformed | Error | §7.3.25.2/.3 SR1/SR3 | Preprocess | `>>TURN` |
| `COBOLNET0719` | `>>TURN` file-name after a non-EC-I-O exception | Error | §7.3.25.3 SR4 | Preprocess | `>>TURN` |
| `COBOLNET1911` | a directive operand its own general format does not admit — ONE producer for every directive whose operand is a closed word set, off the row's `directiveOperand` column (kb/Work PB794; it retired the per-directive `COBOLNET0883` and `COBOLNET1576`) | Error | §7.3.3 SR6 + each directive's general format | Preprocess | `>>SOURCE FORMAT` · `>>LISTING` · `>>PAGE` · `>>PUSH` · `>>POP` · `>>DISPLAY` · `>>CALL-CONVENTION` · `>>PROPAGATE` · `>>LEAP-SECOND` · `>>REF-MOD-ZERO-LENGTH` |
| `COBOLNET1618` | `>>DEFINE` redefinition without OVERRIDE | Error | §7.3.11.3 SR2 | Preprocess | `>>DEFINE` |
| `COBOLNET1619` | compiler-directive expression malformed | Error | §7.3.6/.7/.8 | Preprocess | CC expressions |
| `COBOLNET1620` | `>>FLAG-02` 2002→2014 incompatibility flag | Warning | §7.3.14 | Validate | `>>FLAG-02` (`FlagConformancePass`) |
| `COBOLNET1621` | `>>FLAG-14` 2014→2023 incompatibility flag | Warning | §7.3.15 | Validate | `>>FLAG-14` (`FlagConformancePass`) |
| `COBOLNET1622` | `>>FLAG-02/14` directive malformed | Error | §7.3.14.2/.15.2 | Preprocess | `>>FLAG` |
| `COBOLNET1623` | `>>COBOL-WORDS` malformed / SR1–SR5 | Error | §7.3.10.2/.3 | Preprocess | `>>COBOL-WORDS` |

## Data, value, constant, type (Bind)
| Code | Meaning (Id) | Sev | ISO § | Phase | Where |
|---|---|---|---|---|---|
| `COBOLNET0801` | fixed-point item/literal > 31 digits | Error | §8.3.3.3.2 | Bind | digit capacity |
| `COBOLNET0802` | fixed-point > 18 digits pre-2002 | Error | §8.3.3.3.2 | Bind | digit capacity (edition) |
| `COBOLNET1540` | concat operands not same class | Error | §8.8.3.2 SR1 | Bind | `&` concatenation |
| `COBOLNET1541` | concat operand is an ALL figurative | Error | §8.8.3.2 SR1 | Bind | concatenation |
| `COBOLNET1545` | concat result > 8191 chars | Error | §8.8.3.2 SR2–4 | Bind | concatenation |
| `COBOLNET1547` | constant-entry syntax-rule violation | Error | §13.10.3 / §7.3.6.2 | Bind | CONSTANT |
| `COBOLNET1548` | constant used as a receiver | Error | §13.10.3 SR2 | Bind | CONSTANT |
| `COBOLNET1549` | CONSTANT RECORD structural rule | Error | §13.18.15.3 | Bind | CONSTANT RECORD |
| `COBOLNET1555` | SAME AS subject-entry rule | Error | §13.18.49.3 | Bind | SAME AS |
| `COBOLNET1556` | SAME AS referenced-entry rule | Error | §13.18.49.3 | Bind | SAME AS |
| `COBOLNET1557` | SAME AS cyclic reference | Error | §13.18.49.3 SR3/4 | Bind | SAME AS / TYPE |
| `COBOLNET1558` | EXTERNAL type misuse | Error | §13.18.22 SR5/GR2 | Bind | EXTERNAL TYPEDEF |
| `COBOLNET1570` | numeric-edited VALUE oversize (2023) | Error / Warn | §13.18.63 SR4/5 | Validate | VALUE (edition) |
| `COBOLNET1625` | numeric VALUE out of PICTURE range | Error | §13.18.63.3 SR2/3 | Bind | VALUE |
| `COBOLNET1577` | method REDEFINES out of method scope | Error | §13.18.44.3 | Bind | OO method data |
| `COBOLNET1582` | file COLLATING SEQUENCE malformed | Error | §12.4.5.7.3 SR3-8 | Bind | indexed key collation |
| `COBOLNET1583` | file COLLATING alphabet undeclared/wrong class | Error | §12.4.5.7.3 SR1/2/7 | Bind | indexed key collation |
| `COBOLNET1584` | NATIONAL file collating not implemented | Warning | §12.4.5.7 | Bind | indexed key collation |
| `COBOLNET1559` | report-group PRESENT WHEN / VARYING rule | Error | §13.15.3 / §13.18.64.3 | Bind | Report Writer |

## External-file consistency (2023 — Bind + Validate)
| Code | Meaning (Id) | Sev | ISO § | Phase | Construct |
|---|---|---|---|---|---|
| `COBOLNET1573` | corresponding SELECTs must share the external FILE STATUS item | Error | §12.4.5.3 GR1(i) / §14.8.4.2 | Bind→Validate | external files |
| `COBOLNET1575` | corresponding SELECTs must share the external RELATIVE KEY item | Error | §12.4.5.3 GR1(h) | Bind→Validate | external relative files |
| `COBOLNET1624` | FILE STATUS / RELATIVE KEY / LINAGE items must themselves be external | Error | §14.8.4.2 / Annex E.2 item 9 | Bind→Validate | external files |
| `COBOLNET1574` | FUNCTION EXCEPTION-FILE[-N] arg is not a declared file | Error | §15.28.3 / §15.29.3 | Bind | intrinsics |
| `COBOLNET1572` | MERGE inside a SORT/MERGE procedure (2023) | Error | §14.9.24 | Validate | MERGE |
| `COBOLNET1571` | X3.23-1985 USE FOR DEBUGGING subject staged | Error | VCR Table 7 | Bind | DEBUGGING |

## The `COBOLNET0899` family — recognized-but-not-implemented (staged loud)
One code, **~40 stable Ids**, all sharing suppress key `recognized-not-implemented` (mute as a group). Raised at
**Bind** as a loud stage marker (never a silent no-op — D8). Spans: national data (`national-data`,
`national-through-range`), OO residue (`oo-based-in-class`, `oo-factory-object-reference`,
`oo-interface-property-prototype`, `oo-method-declaratives`, `oo-method-raising-last`,
`oo-external-method-working-storage`), pointers (`usage-function-pointer`, `program-pointer-restricted`), linkage
(`any-length-returning`, `by-value-formal-carrier`, `optional-formal`), constants
(`constant-byte-length`, `constant-from-compilation-variable`), recursion (`recursive-*-working-storage`),
strong-group comparison members (`strong-group-comparison-member`), arithmetic (`arithmetic-standard-intrinsic`), and a large
**Report Writer** cluster (`report-*` — CODE clause, NEXT GROUP, multiple LINE, OCCURS-in-group, rolled/cross SUM, …).
`construct-staged-not-implemented` is the generic marker. These are the implementation backlog — cross-reference
[[kb/Work.base|the work register]].

## Supplementary — string-literal codes (not yet in the descriptor catalog)
Surfaced from `constructs.json` and the binders; the every-code→descriptor migration is the P7 follow-on, so these
aren't in `DIAGNOSTICS.md` yet.
| Code | Meaning | Phase | Construct / where |
|---|---|---|---|
| `COBOLNET0805` | arithmetic composite excludes GIVING resultants | Bind | arithmetic (`ArithmeticBinder`) |
| `COBOLNET0809` | MOVE operand of class index/pointer/object (SR1) | Bind | MOVE (`MoveBinder`) |
| `COBOLNET0810` | ALTER removed 2002 | Validate | `alter-removed-2002` |
| `COBOLNET0811` | bare GO TO removed 2002 | Validate | `bare-goto-removed-2002` |
| `COBOLNET0815` | ACCEPT FROM DATE YYYYMMDD/DAY YYYYDDD (2002) | Validate | `accept-four-digit-year-2002` |
| `COBOLNET0816` | END-ACCEPT (2002) | Validate | `end-accept-2002` (pending) |
| `COBOLNET0817` | DISPLAY/ACCEPT mnemonic not I/O-capable device | Bind | `AcceptDisplayBinder` |
| `COBOLNET0818` | ACCEPT receiver of class index | Bind | `AcceptDisplayBinder` |
| `COBOLNET0830`–`0833` | INITIALIZE WITH FILLER / TO VALUE / TO DEFAULT / THEN REPLACING (2002) | Validate | `initialize-*-2002` |
| `COBOLNET0845` | INSPECT BACKWARD (2023) | Validate | `inspect-backward-2023` |
| `COBOLNET0870`–`0874` | table SORT / RELEASE-literal / national collating / DATA RECORDS / out-of-range key | Bind/Validate | SORT family (`SortBinder`) |
| `COBOLNET0876`–`0880` | RAISE/RESUME/SET-LAST-EXCEPTION / USE-AFTER-EC / GOBACK forms | Bind | EC + `goback-*` |
| `COBOLNET0882` | CALL … ON OVERFLOW removed 2023 | Validate | `call-on-overflow-removed-2023` |
| `COBOLNET0884` | CALL … RETURNING (2002) | Validate | `call-returning-2002` |
| `COBOLNET0885` | PROGRAM-ID RECURSIVE (2002) | Bind/Validate | `program-id-recursive-2002` |
| `COBOLNET0893` | CURRENCY WITH PICTURE SYMBOL (2002) | Bind | `currency-picture-symbol-2002` |
| `COBOLNET0898` | malformed COLLATING SEQUENCE / national-key class | Bind | `SortBinder` |
| `COBOLNET1502` | intrinsic used outside its edition window | Validate | post-85 intrinsics |
| `COBOLNET1511` | boolean-expression formation rule | Bind | `ConditionBinder` |
| `COBOLNET1564` | USAGE FLOAT-BINARY/DECIMAL-128/16/34 unsupported | Bind | `usage-float-*` (pending) |
| `COBOLNET1567` | COBOL word > 30 chars pre-2023 / 63-char relaxation | Bind/Validate | `word-length-63-2023` |
| `COBOLNET0710`/`0712`/`0713`/`0714` | RAISE/RESUME level-3 / placement / GLOBAL / target rules | Bind | `EcBinder` |

## See also
- [[kb/Semantics/Validation Rules]] — the diagnostic descriptor structure & bands.
- [[kb/Semantics/Passes]] — which pass raises which band.
- [[kb/Spec/Lookup/Semantic Rules]] · [[kb/Spec/Lookup/Construct Catalogue]]
- [[kb/Compiler/Pipeline-to-ISO-Mapping]] — the phase each diagnostic belongs to.

## Backlinks
- [[kb/Spec/Lookup/Index]] · [[kb/Semantics/Validation Rules]] — link here.

> \* `COBOLNET1560` is cited from the conformance record ([[docs/CONFORMANCE]] §4) as the
> §4.2.6 warning band and the locale-reject; they may render as string-literals rather than first-class descriptors.
