## Entry 1723 — 2026-09-26 21:40 PDT — Repository renamed COBOL; product named WiseOwl COBOL

**Owner decisions (2026-09-26).** The GitHub repository is renamed `BrentRector/CobolSharp` → **`BrentRector/COBOL`**
("it has nothing publicly facing related to C#"), and the private spec submodule `BrentRector/CobolSharp-private` →
**`BrentRector/COBOL-private`**. The product is **WiseOwl COBOL**; its NuGet package is **`WiseOwl.COBOL`** (capital
COBOL: the industry spelling on the public surface; C# identifiers keep .NET's `Cobol` casing and are not renamed).
The owner renames the local folder `E:\CobolSharp` → `E:\COBOL` after this session ends. Both GitHub renames were
made with `gh repo rename` after every lander had finished; GitHub redirects the old names.

**Reference sweep (live files only; DEVLOG, kb/Work, adjudication/ and evidence/ keep their history as written).**
`BrentRector/CobolSharp(-private)` → `BrentRector/COBOL(-private)` in 15 places (.gitmodules + `git submodule sync`,
CONTRIBUTING, SECURITY, README, the issue-form links, `scripts/cloud/setup-env.sh`, `scripts/hooks/session_start.py`,
`scripts/session-probe.ps1`, the cloud brief, the legacy CLI's package URLs), and the local folder path `E:\CobolSharp`
→ `E:\COBOL` in 295 places across 51 live files (the workstream skill and its templates, `.claude/workflows`, scripts,
current design docs); `origin` re-pointed. `.claude/settings.local.json` (the owner's permission allowlist) is not
touched. The product name replaces "CobolSharp"/"COBOL.NET" in CONTRIBUTING, SECURITY, CODE_OF_CONDUCT, the issue
forms and CLAUDE.md; `CLA.md` is renamed and bumped to **version 1.1**, and gist e56c12d8… (`CLA.md`) updated to match
(no one had signed). R50's package ids become `WiseOwl.COBOL` / `WiseOwl.COBOL.Runtime` in the design doc, R50,
PB1613, PB1614 and DOC_INDEX.

**README.md rewritten for the current compiler** (owner: "Various READMEs also will need updating"): title WiseOwl
COBOL, `git clone …/COBOL.git` / `cd COBOL`, a status section measured on 2026-09-26 (4,347 rules, all adjudicated,
3,286 closed, GAP 1,061; 9,046 conformance / 29,491 unit / 33 characterization tests; the four declined facilities),
the edition model, coverage stated as measured rather than claimed, the planned NuGet/doc-site distribution, and
links to the community files. The other READMEs were reviewed: none names the repository or the product except
`tests/Cobol.Net.Benchmarks` (`CobolSharp.sln`, unchanged — the solution file is not renamed).

