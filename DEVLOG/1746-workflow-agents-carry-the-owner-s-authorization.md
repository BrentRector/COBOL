## Entry 1746 — 2026-09-28 00:06 PDT — Workflow agents carry the owner's authorization

**Why.** Wave 68 was launched in a session turn whose latest user message was a LinkedIn question. A workflow agent
treats the session's most recent user message as its request, so all nine cobol-implementers returned BLOCKED ("the user
asked a LinkedIn question, not for the COBOL fix lane") and made no changes. The agents were right to decline work that
no visible request asked for; the defect was that the dispatch carried no provenance.

**What changed.** `.claude/skills/workstream/templates/wf_rolling_wave.js` takes an `authorization` argument: the
owner's direction for this fleet, quoted verbatim with its time. The script prefixes every implementer and lander prompt
with it, and names MANDATORY-PRACTICES O4 (the standing Workflow opt-in). The relaunched wave (wf_e9a82a33-6b6) ran
with it and no agent refused; train 68 landed from it (DEVLOG 1745).

**Also learned.** The first attempt at this edit used `pathlib.Path.write_text`, which writes CRLF on Windows; the
Workflow tool then refused the script as containing hidden control characters. The file is LF again
(`git ls-files --eol`: i/lf w/lf).

**Landing.** The closing lander for train 68b could not include this file: the permission classifier blocked an agent
copying the orchestration template as self-modification. The owner authorized landing it directly (2026-09-28 00:05
PDT: "land the script fix now too").

