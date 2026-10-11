## Entry 1293 — 2026-08-17 22:22 PDT — PB60: a container's CONFIGURATION SECTION and OPTIONS now reach its contained programs

**The battery first.** The four-landing batch (entries 1289–1292) measured `=== BATTERY: ALL GREEN ===` on
`f58c7547`: Conformance 4372/4372, Unit 4164/4164, characterization 33/33, `guard-fast` NIST 353/0 audit-clean,
GnuCOBOL differential 0 per-case flips (1323 cases). Recorded as the §9 reference; §0 says no gate is owed.
The next landing was drafted apply-ready in the scratchpad while it ran (memory `work_through_the_freeze`) and
applied the minute it finished.

**The row.** AR-15.67.3-5 — the sweep's p02b: an outermost `SPECIAL-NAMES. DECIMAL-POINT IS COMMA.` and a
contained program with no configuration section (it may not have one, §12.3.3 SR1). Outer: `NUMVAL("123,45")` =
123.45, `NUMVAL("123.45")` = 0 — right. Inner: `NUMVAL("123,45")` = 0, `NUMVAL("123.45")` = 123.45 — the exact
inversion of §15.67.3 r5, undiagnosed. §12.3.4 GR1: "The entries explicitly or implicitly specified in the
configuration section of a source unit that contains other source units apply to each directly or indirectly
contained source unit." The binder's `DataBinder.Switches#DecimalPointIsComma` was per-source-unit, and the
only inheritance `BinderDriver.BindUnitData` performed was the REPOSITORY sets — after Bind, when it no longer
matters for a literal or a PICTURE.

**The shape, not the case.** ONE `DataBinder.InheritConfiguration(container)` copying the WHOLE
configuration-derived state — DECIMAL-POINT, CURRENCY SIGN (symbol + string), CLASS names, ALPHABETs (both
classes), switch mnemonics and status condition-names, OBJECT-COMPUTER's PROGRAM COLLATING SEQUENCE (both
classes), SOURCE-COMPUTER's DEBUGGING MODE, the REPOSITORY function/intrinsic/property specifiers — called
BEFORE the containee binds (units bind container-first, so one level carries the ancestry), and §11.9.4 GR1's
OPTIONS inheritance on the same call: `OptionsBinder.Bind(program, edition, baseline)` starts from the
container's model and lets the containee's own OPTIONS override clause by clause. §12.3.3 SR1 gets its
diagnostic — COBOLNET1643, a catalogued descriptor (`DIAGNOSTICS.md` regenerated) — and the section is still
walked afterwards so a second diagnostic never masks the first. The Format-4 device mnemonics were already
inherited by `MnemonicRegistry.Of`'s enclosing-unit parse-tree walk — a second mechanism for the same rule,
correct today, flagged in the code doc for folding in when the environment division gets a bound model.

**Measured after.** 2023 golden `pb60_nested_configuration_inheritance` (an outermost program with OPTIONS
ARITHMETIC IS STANDARD-DECIMAL, a reversed ALPHABET as the PROGRAM COLLATING SEQUENCE, a CLASS, CURRENCY SIGN
"#", DECIMAL-POINT IS COMMA and a switch; a contained program proving all six — NV=123,450000, NVP=0, ED=
#1.234,50, CLASS=YES, PCS=REV, SW=ON — and ARITH=2,000000 (SDIDI arithmetic inherited: 2/7*7 exact); a second
contained program with its own `OPTIONS. ARITHMETIC IS NATIVE.` giving ARITH2=1,999999, the override) and 85
golden `nested_special_names_inheritance` (the edition-invariant five) both matched their spec-derived `.out`
first run; negative `config-section-in-contained-program` rejects at all four editions. Inventory:
AR-15.67.3-5, GR-12.3.4-1, SR-12.3.3-1, GR-11.9.4-1 → CONFORMS (GAP 4142 → 4138 — the three blank rows are the
very rules this landing implements and tests, not exploration). Gates: Conformance 3142/3142 over a broad filter
(Corpus|Nested|Config|Special|Class|Alphabet|Collat|Options|Arithmetic|Diagnostic|SpecTraceability|
VersionMatrix|Call|Repository|Function|Oo — the grammar moved) · Unit 61/61 (Diagnostic|Grammar|
SpecTraceability|Options|Parser|Special) · characterization 33/33 · SpecTraceability 10/10.

**Two grammar over-restrictions the golden surfaced.** `CLASS HEXDIG IS "0" THRU "9" "A" THRU "F"` was a parse
error: `classValueSet` REQUIRED a comma between literal groups where §12.3.7.2's diagram juxtaposes them (the
comma is the optional separator it is everywhere) — fixed to `COMMA?`, and the sweep found the same shape on the
OO in-line invocation's `argumentList` (fixed with it; `functionArgList` already had `SEPARATOR?`). And
`OBJECT-COMPUTER. PROGRAM COLLATING SEQUENCE IS REV.` without a computer-name is a parse error where §12.3.6.2
brackets computer-name-1 optional — but X3.23-1985 required the name, so the loosening needs the introduction
gate + a VCR row + the edition sweep: registered as **PB78**, the golden writes `OBJECT-COMPUTER. XX` like every
existing one. PB60 sits at 14 of 15; the last row is the CURRENCY SIGN SET model (AR-15.68.3-3).

