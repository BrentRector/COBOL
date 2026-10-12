## Entry 1725 — 2026-09-27 03:45 PDT — PB1627 landed: ALL literal-1 rides the concatenation rule; ONE figurative spelling

**What.** `ALL literal-1` now takes a full §8.8.3 concatenation expression as its literal-1 (§8.3.3.6.3 SR2), so
`ALL "A" & SPACE` and `ALL "A" & K` (K a constant-name) compile. `ALL K` for a constant-name reads as Format 6
(§13.10.3 SR2) instead of drawing the symbolic-character error. The quoted-only `allLiteralOperand` grammar rule is
deleted. `ConcatFolder.FoldAllLiteral` is the one Format 6 reader, used by the expression, boolean, VALUE and
ALPHABET/CLASS paths. New COBOLNET2491 covers a numeric constant-name as literal-1.

**Fix-up this morning.** Train 64 held PB1627 back because one case failed: PB976C3A printed `ALL"AB"`, since ANTLR
`GetText()` joins tokens with no separator. The same bug was in seven more diagnostic sites plus three hand-built
spellings. All of them now call ONE `ConcatFolder.Spelling(fig)`. No golden pinned the collapsed form.

**Gate.** Characterization 33/33; Conformance filtered 2300/2300; corpus+NIST 3661/3661; Unit 29489/29491. The two
Unit failures are `ExternalCorpusPopulationDriftTests`, which need the untracked GnuCOBOL corpus, and a fresh
worktree does not have it. kb/Work PB1627 → landed (no rows; reason in the note).

