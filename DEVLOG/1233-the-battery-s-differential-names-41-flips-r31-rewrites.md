## Entry 1233 — 2026-08-08 16:17 PDT — The battery's differential names 41 flips; R31 rewrites qualified matching, R32 registers screen names, and every flip gets an owner

The R21+R22+R30 comprehensive battery came back NOT GREEN with every internal leg green (Conformance
4279/4279 · Unit 4089/4089 · characterization 33/33 · NIST 353/0 + CLEAN audit) and ONE red: the
GnuCOBOL differential reporting 41 per-case flips, every single one carrying R30's new COBOLNET1639
(or R22's COBOLNET1543). That is the gate doing exactly its job — the differential is the only leg
that could see what a new rejection does to 1,323 real programs — and the attribution split three ways.

FOURTEEN moved TOWARD the oracle: programs GnuCOBOL rejects that we used to compile clean via the
silent staging R30 killed — including four syn_definition AMBIGUITY cases whose new AGREE_REJECT
verdicts independently validate the R31 rewrite below. THREE were real over-rejections R30 exposed,
all fixed here: syn_definition:931 (R31 — ResolveQualified resolved the OUTERMOST QUALIFIER as a
unique unqualified name first, so legal `Z IN X` — two Xs, exactly one holding a Z — was refused;
§8.4.2.2.1's uniqueness is of the qualified MATCH, and the rewrite goes candidate-set: every
declaration of the name, ancestors consumed inner→outer, the outermost qualifier optionally the owning
FD/SD, exactly one survivor; the file arm's own first-match walk had the same defect and is subsumed);
syn_screen:221 (R32 — a name DECLARED in the documented-unsupported SCREEN SECTION drew "not defined";
ScreenNames now registers them, the 1639 report exempts them, and the staged loud names the 1560
cause); run_misc:1759 (R38's diagnostic half — a declared ALPHABET-name in a data position, the same
honesty via the same mechanism). TWENTY-FOUR are spec-correct strict rejections or open notes:
R34 (recursive COPY REPLACING never materializes the replaced name — legal ISO, rejects_legal_source,
OPEN) · R35 (a REPOSITORY-declared zero-argument UDF referenced bare falls to the data path — the
two-arm-dispatch shape a SIXTH time: PB7 fixed the intrinsic arm of the bare-name form and never asked
the UDF arm; OPEN) · R36 (partial REPLACE, adjudicate) · R37 (RETURN-CODE ×7, LENGTH OF ×8,
SYSTEM-OFFSET — vendor registers, strict rejection correct, vendor-axis decision open) · plus bare
PI/WHEN-COMPILED under R22's SR2. R33 records the sibling found mid-fix: an ambiguous UNQUALIFIED name
still resolves first-match (list[0]), which also makes 1639's unqualified-ambiguity arm dead code —
adjudicate before enforcing.

The baseline regenerated with the follow-up tree: the TSV diff is EXACTLY 38 rows (41 minus the 3
fixed), each owned above; a fresh differential prints 0 PER-CASE FLIPS over 1323 cases. COBOLNET1639's
report now distinguishes not-declared · not-under-the-qualifiers · N-declarations-match (the old text
claimed "not under" for the multiple-match case). Goldens: qualified_unique_match (85) +
qualified-ambiguous-match (negative, all four editions) + screen_section_reference (2023 compile-only).

Gate on this tree: full Unit 4089/4089 · full Conformance 4281/4282 (the one red: the negative
fixture's .err file missing mid-run — written before landing, the case green by name after) ·
characterization 33/33 · the differential legs above. Battery reference advanced to 36ce29fa with the
NOT-GREEN-attributed narrative.

