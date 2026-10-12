## Entry 1883 — 2026-10-04 23:30 PDT — Battery #88: every internal leg green; the GnuCOBOL differential's 43 flips are all adjudicated against the ISO text (22 new rejections, every one legitimate), and the baseline is refreshed

Battery #88 ran cold in its own worktree at `6e4b5deb0`, owed by every landing since #87 (146 commits touching `src/` or `tests/` as of the
morning, then wave 1018). Result: Conformance 10,503, Unit 31,464, Characterization 35, NIST 362 MATCH with 0 regressions, the guard ALL GREEN,
citations and witness loss clean; the GnuCOBOL differential printed `43 PER-CASE FLIP(S)`, so the script's verdict was NOT GREEN. The differential is a
regression net, never authority, so each flip needed the standard. 22 flips were cases GnuCOBOL's default dialect (ISO plus its extensions) accepts and we
now reject: the only direction that can hide a defect, because rejecting legal source is debt. Four read-only adjudicators (Opus, one per mechanism group:
data-division rules, procedure-division and program structure, reference format and directives and COPY and NULL, relations) judged every case with
`cite.py --check` on every citation; the harness refused their report files, so they returned text. Verdict: all 22 are LEGIT_REJECT, each for a rule the standard states
(§13.18.38.3 SR1 a) OCCURS at level 01, §8.5.1.3.2 equal subordinate level-numbers, §14.4.1 sections, §14.5.3 scope terminators, §10.7.3 end markers, §13.7.3 4) LINKAGE
reference, §6.2.2 / §6.3.3 debugging lines, §11.2.1 comment paragraphs, §7.3.12 `>>DISPLAY`, §7.2.3.4 COPY library location, §8.4.3.10.3 NULL, §8.8.4.2.13 and §8.8.4.2.5
relations). The other 21 moved toward agreement (16 now AGREE_REJECT, 5 now AGREE_ACCEPT). The baseline was rewritten (43 rows, all attributed here) and the plan's
battery reference now names #88.

The adjudication earned five register notes. I reproduced the two substantive claims before filing: `COMPUTE X=1*2` compiles with no diagnostic although §8.7.1 requires a space on
each side of an operator, and `IF N < (K + 2)` compiles when K is an index-name; PB2017 (that gap plus COBOLNET2631 advising a space that changes the program's meaning under
DECIMAL-POINT IS COMMA), PB2018 (the index-name case, which may re-open R29), PB2019 (END PROGRAM reports SR2 for an SR3 break), PB2020 (the differential ignores the suite's AT_XFAIL marker,
and COBOLNET1619 cites the DEFINE clause) and PB2021 (the unadjudicated `SET ADDRESS OF x DOWN BY 32` parse failure). Friction: none of the subagents could write its report file, which the
adjudicator brief assumed; the brief should ask for text.

**Files:** `tests/external/gnucobol-verdict-baseline.tsv`, `docs/COBOLNET_REARCHITECTURE_PLAN.md`, `kb/Work/PB2017.md` to `PB2021.md`. No diagnostic code used.

