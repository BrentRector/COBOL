## Entry 1220 â 2026-08-08 10:28 PDT â R18: EXP and its own spelled-out definition finally agree â by sharing one body, not by tuning two

Under ARITHMETIC IS STANDARD-DECIMAL, `FUNCTION EXP(2)` and `FUNCTION E ** 2` returned different wrong
answers, and `FUNCTION EXP10(0.5)` returned a different value per RECEIVER â Â§15.4.1 r1 says the function
SHALL equal its equivalent arithmetic expression, and Â§15.34.4/Â§15.35.4 write those EAEs down. The fix is
deliberately anticlimactic: a standard-mode arm ahead of the float dispatch that folds E and PI to their
exact 34-digit Â§15.27.3 r3 / Â§15.73.3 r3 constants (new `CobolDec.E`/`CobolDec.Pi`) and renders EXP/EXP10
through the SAME `CobolDec.Pow` the hand-written `**` already uses. Equality by construction; `EXP10(23)` is
now exactly 10^23 (Â§8.8.1.5.4 r2a-d repeated multiplication) where binary64 gave â¦991611392.

PI was the unnamed sweep sibling (same rule shape as E, one line). My first guess at its clause â Â§15.72.3
â was ORD-MIN's; cite.py caught it before it shipped anywhere. Â§15.73.3.

The guards earned their keep twice, in opposite directions. My new `switch (sig.RuntimeMethod)` STOLE the
IntrinsicRealArgDriftTests scan anchor (it keys on the FIRST such switch) and the guard promptly flagged
"Exp" as an exact arm missing ExpReal â wrong diagnosis, right instinct: the arm is now an if-chain with a
comment saying why, and the anchor is honest again. In the SAME sitting the guard turned out to be unable to
SEE any digit-bearing method name â `[A-Za-z]+` never matched `Exp10`, the PB21 under-count shape â so any
future `Xxx2` arm was structurally invisible; widened to `[A-Za-z0-9]+`. And the RuntimeApi characterization
guard forced the new emission through `DecE`/`DecPi`/`DecFrom` helpers.

Residual, noted in the register note rather than forked: a NON-integer exponent's precision is
`CobolDec.Pow`'s binary64-seeded r2e approximation â shared with every hand-written `**`, so refining Pow
refines both spellings at once. The F19 receiver pair now holds prefixes of ONE SDIDI (receiver-scale
rounding at the final transfer is Â§14.7's job) and the golden pins it with the latitude stated.

Golden `exp_standard_decimal_eae`. Gate: Unit 4083/4083 Â· characterization 33/33 Â· wave-local
Intrinsic|Arithmetic|StandardDecimal|Corpus 677/677. Next: R24 (date/time) Â· R12 Â· R15, 12 actionable.

