## Entry 1254 — 2026-08-08 23:06 PDT — The eight CONFORMS-but-untested rows close on four new spec-derived goldens, and the NUMVAL-F diagram earns its render

The probe's standing "8 CONFORMS still test-needed" line goes to zero. Each row had a verdict and a
code-location but no spec-derived covering test, which is exactly one golden from OK — so four goldens
were written FROM the rules (all nine citations validated with cite.py --check first), probed through
the CLI, and recorded with record_verdicts.py: max_min_leftmost_tie (RV-15.59.4-2's leftmost-tie via
§8.8.4.2 space-padded equality, r3's size-of-selected proven by FUNCTION LENGTH in both tie
directions and away from first position, plus the { argument-1 } … repetition of FMT-15.59.2 and
FMT-15.63.2), national_of_argument_forms (FMT-15.66.2's optional argument-2, both shapes),
numval_argument_formats (FMT-15.67.2 across the leading-sign and trailing-CR argument shapes, edited
receivers so every output character derives from §13.18.40.5), numvalf_decimal_comma_and_spaces
(AR-15.69.3-4's DECIMAL-POINT IS COMMA argument in its own source unit; RV-15.69.4-1's
spaces-ignored), and RV-15.65.4-8 was already covered by module_name.cob's N-NST leg — that row
needed only its test-ref recorded, the PB37 shape again.

The probe run earned its keep once: three goldens matched their derivations byte-for-byte on the
first run, and the fourth diverged on exactly the cases I had mis-derived — NUMVAL-F's exponent sign
is REQUIRED. The §15.69.3 r1 format puts the exponent's +/− in BRACES (a required choice) where the
leading sign sits in optional square brackets, so "2.5E2" is not a legal argument at all; the printed
page (898, rendered per rule 1 since the diagram was load-bearing) confirms the transcription, and
the compiler had it right all along — the golden's inputs were corrected to signed exponents and all
four lines then matched. The rule this re-earns: derive, then RUN, and treat a mismatch as a question
about the derivation before it is a question about the compiler.

Recorded via an 8-record batch (existing verdicts/code-locations/notes carried, test-refs added):
GAP 4,233 → 4,225, resolved 78 → 86. CorpusRunner 514/514 with the four enabled;
SpecTraceabilityInventoryDriftTests 10/10; kb/Conformance regenerated in the same run.

