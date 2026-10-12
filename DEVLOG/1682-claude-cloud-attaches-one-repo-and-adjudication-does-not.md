## Entry 1682 — 2026-09-24 15:24 PDT — `claude --cloud` attaches one repo — and adjudication does not need the second; the "spec is a private submodule" claim corrected in five docs

**What.** The wave-2 cloud orchestrator was first launched with `claude --cloud` from the owner's terminal. It is not
interactive: it takes ~1 min, prints the session URL and returns. That session attached ONLY BrentRector/CobolSharp
(the docs: "`--cloud` works with a single repository at a time"; no flag or settings key adds one — the
claude.ai/code page does, with "+" or `?repositories=owner/a,owner/b`). It was stopped before doing any work and the
orchestrator re-launched from the web page with both repos (session_019puPmD9TXHu1VpiamtCr3s, credit-billed).

**The correction this exposed.** `specs/ISO_COBOL.md` is TRACKED IN THIS REPO; the private submodule `specs-private`
holds only the licensed PDF, read by `render-spec-page.py` and the figure audits. `cite.py --check` reads the
Markdown. So a single-repo cloud session CAN adjudicate — it only cannot render a page for a load-bearing diagram.
CLAUDE.md ("Spec: … private submodule"), `docs/DOC_INDEX.md` (twice: "the submodule `specs/ISO_COBOL.md`"),
`scripts/hooks/session_start.py` ("without the spec every §/GR citation … is impossible", written in Entry 1677),
`scripts/cloud/setup-env.sh` ("must attach BOTH") and the cloud-adjudicate brief + PB1522 all said otherwise; all
now state the real split. The brief tells a single-repo session to mark a diagram-dependent verdict PARTIAL with
"diagram not rendered". Also removed a stale `.claude/settings.local.json` allow-rule (a one-off grep of the legacy
grammar whose regex `*` made every CLI launch print a wildcard warning).

