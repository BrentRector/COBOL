## Entry 1848 — 2026-10-03 12:20 PDT — A cobc exists in WSL after all; the CALL-CONVENTION probe becomes PB1945

**What went wrong, honestly.** The session told the owner, and let two implementer reports repeat, that GnuCOBOL's `cobc`
was absent from WSL. The evidence was a single `which cobc` that printed nothing. The owner answered, twice, that it was
present. A proper search (the repo scripts, the DEVLOG, `find / -xdev`, `/usr/local`, the shell history) then found no
binary either, because the owner installed one only afterwards: Ubuntu's `apt` package, **GnuCOBOL 3.1.2.0** at
`/usr/bin/cobc`. The repo's corpus pins 3.2 and the differential never runs `cobc` (it compares our accept/reject with
verdicts extracted from the 3.2 test suite), so no script had a reason to know where a binary lived. The failure was the
assertion of absence from one probe. The memory `verify-absence-before-claiming` records the rule: search how the project
finds the tool, report what was searched, ask for the path.

**What the 3.1.2 `cobc` says about `>>CALL-CONVENTION`** (`cobc -fsyntax-only -free`, an empty program): `COBOL`, `EXTERN`,
`STDCALL` and `STATIC` are accepted; `C`, `PASCAL` and an arbitrary word are rejected with `expecting COBOL or EXTERN or
STDCALL or STATIC`. That agrees with wave 1007's PB1383, which admits only `COBOL` (train 1007, DEVLOG 1847), on every unknown
word. EXTERN, STDCALL and STATIC are implementor-defined under §7.3.9.3 GR3 (`cite.py --check` OK) and would need a mapping
to native calls, so whether to keep them unsupported or implement them is `kb/Work/PB1945` (a decision note, no code change).
A confirmation on 3.2 is owed, because 3.1.2 is not the version the corpus pins; GnuCOBOL 4.0-early-dev exists but its own
page says anything may change between builds, so it is informational only.

**Local-model spike (scratchpad only, nothing in the tree).** The T1 retrieval test ran on the RTX 5090: 1,440 notes with
`spec_refs` against 3,019 clause chunks. Hit in the top 10: BM25 0.713, qwen3-embedding:4b 0.740, embeddinggemma 0.767; MRR 0.445,
0.503, 0.535. A modest gain, not yet an adoption; the hybrid, digest and lead-matching tests are not run.

**Files:** `kb/Work/PB1945.md`. No code changed, no diagnostic code used.

