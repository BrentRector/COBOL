## Entry 1225 â 2026-08-08 12:35 PDT â The R24+R15+R16+R12 battery: NOT GREEN, one named flip, and the flip is R16 doing its job

The batch battery for the four landings came back NOT GREEN with every internal leg green (Conformance
4271/4271 Â· Unit 4083/4083 Â· characterization 33/33 Â· guard ALL GREEN with NIST 353/0): the one red was
the GnuCOBOL differential reporting exactly ONE per-case flip â run_file:13011, AGREE_ACCEPT â
WE_REJECT_THEY_ACCEPT, carrying R16's own COBOLNET1637. The case DISPLAYs an index-name, which GnuCOBOL
accepts as its documented extension (printing the occurrence number) and we now reject with the
Â§13.18.38.3 r7 citation. Â§0's mechanical rule classes an AGREEâdivergence flip a REGRESSION; attributed
from the CASE it is the opposite â the same shape as PB40's flip, down to the wording.

One deliberate difference from the PB40 disposition, recorded here so it reads as a choice: COBOLNET1637 is
UNCONDITIONAL â --permissive does not admit the reference â because for the string-context band there is
nothing to accept (the pre-R16 behavior was a runtime abort, not a value), and a permissive coercion for
the numeric band alone would split one rule across modes. The diagnostic names the SET rewrite, which is
the migration path. R29 (the arithmetic band's strict/permissive adjudication) is where any broader
extension decision belongs.

Baseline regenerated with --write-baseline; the TSV diff is EXACTLY ONE ROW (run_file:13011), and a fresh
differential against the committed baseline prints 0 PER-CASE FLIP(S).

