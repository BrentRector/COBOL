#!/usr/bin/env python3
"""THE WORK REGISTER — one note per work item under `kb/Work/`, and the only place work is authored.

⛔ WHY THIS EXISTS. "What is left to do" was authored in FIVE places, each in a different format, and three of
them declared themselves canonical:

    docs/COBOLNET_REARCHITECTURE_PLAN.md §0 NEXT   "the ONLY live-state SSOT"      buried in 3,614 lines
    docs/rearchitecture/CONFORMANCE-FIX-QUEUE.md   owns its own LANDED tally       2,484 lines of prose
    kb/Remaining Work Tracker.md                   "the canonical remaining-work tracker"   5 days stale
    §11 analysis backlog                           17 analyses                     inside the plan
    the queue's RESIDUE bucket                     ~10 findings                    prose, never itemised

When three artifacts each claim to be the source of truth, none is — and the cost was measurable: the residue
findings include a WRONG-ANSWER defect (`EXCEPTION-STATEMENT` returns `GO` where Table 12 requires `GO TO`) that
no work list could see, because it lived inside a paragraph.

⭐ THE FORM IS ONE NOTE PER ITEM, TRACKED IN GIT, WITH FRONTMATTER. Not one big JSON: a note gives Obsidian a
graph node (link an item to its spec clause and its code-reference note), gives git a per-item history instead of
a 2,484-line diff, and gives Bases every view without a query language. The forensic prose — repro, citation, why
a previous summary was wrong — lives in the note BODY, which is where it already was. Nothing is discarded.

    python scripts/spec/work.py migrate    # ONE TIME: fold the five registers into kb/Work/
    python scripts/spec/work.py check      # validate frontmatter; non-zero on a bad/missing field
    python scripts/spec/work.py next       # the ranked work list (session-probe prints this)
    python scripts/spec/work.py next --cluster PB2108 [--json]   # one campaign's open notes in blocked_by order
    python scripts/spec/work.py stats      # counts by kind/status/harm
    python scripts/spec/work.py parity     # the frontmatter reader vs. its C# twin's fixture (--json for the gate)

⭐ `inventory_rows:` — THE BACK-LINK TO THE P14 TRACEABILITY INVENTORY, and the register's newest field
(2026-08-31). A note lists the `rule-id`s it owns; that list is the SSOT for ownership, and the note's prose
"Rows:" line is narrative beside it. It exists because the two artifacts answering "what is left to do" had
NOTHING holding them together: of 138 inventory rows verdicted PARTIAL / NOT-IMPLEMENTED / DIVERGES, **131 were
invisible to `next`** when it was first measured — ranked by nothing, owed by nobody, and counted by the
burn-down the whole time.

⛔ The field is enforced by `DefectiveRowCoverageDriftTests` (tests/Cobol.Net.Tests.Unit) and NOT here, on
purpose: the predicate has to keep holding as the INVENTORY changes underneath a note, so it belongs to
something that runs every build, and validating a rule-id against the inventory from Python as well would be
the same rule in two places. What that gate asserts is that every defective-verdict row is claimed by a note
whose status is not terminal — so **flipping a note to `landed` without re-verdicting its rows turns the
battery red**, which is exactly the event that used to pass unnoticed.

⭐ `closes_rows:` — THE OTHER DIRECTION, and the register's newest field (owner decision 2026-09-19,
`kb/Work/PB245`). `inventory_rows` is what a note CLAIMS while it is open; `closes_rows` is what its landing
CLOSED, and it survives the landing. Without it, "which rows did this fix close" was answerable only by
re-measuring: fourteen §15 rows held the GAP open on mechanisms seven landings had already closed, and thirteen
closed CONFORMS the first time anyone looked. A landing therefore writes BOTH halves in the same change set —
the rows leave `inventory_rows` as they are re-verdicted, and they arrive in `closes_rows`. A landing that
closed no row says so with `closes_rows: []` **and** a `closes_rows_reason:`; silence is what the field
replaced. The spelling is `closes_rows`, not the decision's `closes-rows`: every multiword key in this
frontmatter is snake_case, and `kb/Work.base` addresses a property as `note.<key>`, where a hyphen would
at best need quoting and at worst read as an operator. Enforced by `ClosesRowsBackLinkDriftTests`
(tests/Cobol.Net.Tests.Unit); the SHAPE half is :func:`closes_rows_shape` here.

⛔ AND THE READER ITSELF IS A GATE. `parse_frontmatter` has a C# twin (`tests/_shared/WorkRegister.cs`), the two
read one file format, and they disagreed for months about a list WRAPPED across two lines (`kb/Work/PB875`):
this side truncated it, that side discarded it, and both called the note well-formed.
`tests/version-matrix/work-frontmatter-parity-cases.json` is the fixture both evaluate, and the C# gate runs
`work.py parity --json` so the comparison is against this engine RUN, not against an assumption about it.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
QUEUE = REPO / "docs" / "rearchitecture" / "CONFORMANCE-FIX-QUEUE.md"
PLAN = REPO / "docs" / "COBOLNET_REARCHITECTURE_PLAN.md"
WORK = REPO / "kb" / "Work"

KINDS = {"defect", "analysis", "adjudication", "decision"}
STATUSES = {"open", "half", "landed", "owner", "blocked", "retired"}
#: The statuses that mean an item is DONE. A view of open work shall exclude every one of them — see
#: :func:`check_base_agrees`, which is what stops the next one added here from being half-applied.
TERMINAL_STATUSES = ("landed", "retired")
HARM = {"wrong-answer", "crashes", "silent", "rejects-legal-source", "under-rejects", "process"}

PB_HEAD = re.compile(
    r"^### (?P<id>PB\d+)\s*·\s*\[(?P<sev>[A-Z]+)\]\s*·\s*(?P<area>[^·]+?)\s*·\s*(?P<rest>.*)$", re.M)
FLAGS = re.compile(r"^>\s*⚙\s*\*\*flags:\*\*\s*(?P<flags>.+)$", re.M)
BLOCKED = re.compile(r"blocked-by:\s*(PB\d+)")
CLAUSE = re.compile(r"§(\d+(?:\.\d+)*)")
A_ROW = re.compile(r"^\|\s*(?P<id>A\d+[a-z]?)\s*\|\s*(?P<title>.+?)\s*\|(?P<rest>.*)\|\s*$", re.M)


def status_of(rest: str) -> str:
    """⛔ MATCH THE PHRASE "HALF LANDED", NOT ⚠ OR THE BARE WORD "HALF", AND TEST IT FIRST. A first cut read
    ``"HALF" in rest or "⚠" in rest`` and misfiled THREE COMPLETED items (PB1, PB14, PB32) as still open, because
    a landed entry routinely narrates the half it closed — "LANDED 2026-08-03 (the CS1503 half; the wrong-VALUE
    half is now PB38)". Putting finished work back on a work list is the one failure a work list must not have."""
    up = rest.upper()
    if "HALF LANDED" in up:
        return "half"
    if "⚖" in rest or "NEEDS-OWNER-DECISION" in up:
        return "owner"
    if "✅" in rest or "LANDED" in up or "RETIRED" in up:
        return "landed"
    return "open"


def fm_line(k: str, v) -> str:
    if isinstance(v, bool):
        return f"{k}: {str(v).lower()}"
    if isinstance(v, list):
        return f"{k}: [{', '.join(str(x) for x in v)}]"
    return f'{k}: "{v}"' if isinstance(v, str) and (":" in v or v.strip() != v) else f"{k}: {v}"


def write_note(item: dict, body: str) -> str:
    keys = ["id", "kind", "status", "severity", "area", "wrong_answer", "crashes", "silent",
            "rejects_legal_source", "under_rejects", "process_only", "blocked", "blocked_by", "spec_refs"]
    fm = ["---", f'title: "{item["id"]} — {item["summary"][:70]}"']
    fm += [fm_line(k, item[k]) for k in keys]
    fm += ["tags: [cobolsharp, work, " + item["kind"] + "]", "---", "",
           f'# {item["id"]} — {item["summary"]}', ""]
    return "\n".join(fm) + body.rstrip() + "\n"


def harm_fields(flags: list[str]) -> dict:
    return {
        "wrong_answer": "wrong-answer" in flags, "crashes": "crashes" in flags,
        "silent": "silent" in flags, "rejects_legal_source": "rejects-legal-source" in flags,
        "under_rejects": "under-rejects" in flags, "process_only": "process" in flags,
    }


def migrate() -> int:
    text = QUEUE.read_text(encoding="utf-8")
    WORK.mkdir(parents=True, exist_ok=True)
    written = 0

    # ── 1. The 39 PB defects. The BODY IS COPIED VERBATIM — this migration must not paraphrase evidence. ──────
    ms = list(PB_HEAD.finditer(text))
    for i, m in enumerate(ms):
        body = text[m.end(): ms[i + 1].start() if i + 1 < len(ms) else len(text)]
        # A trailing section heading that is not a PB entry (the residue block) belongs to nobody.
        body = re.split(r"^### ⚠ RESIDUE", body, flags=re.M)[0]
        fmm = FLAGS.search(body)
        flags = [f.strip() for f in fmm.group("flags").split("·")] if fmm else []
        flags = [f for f in flags if not f.startswith("blocked-by")]
        rest = m.group("rest")
        item = {
            "id": m.group("id"), "kind": "defect", "status": status_of(rest),
            "severity": m.group("sev"), "area": m.group("area").strip(),
            "summary": re.sub(r"[⛔✅⚠⚖◑]", "", rest).strip(" —-"),
            "blocked_by": sorted(set(BLOCKED.findall(body))),
            "spec_refs": sorted({c for c in CLAUSE.findall(body)})[:12],
            **harm_fields(flags),
        }
        item["blocked"] = bool(item["blocked_by"])
        (WORK / f'{item["id"]}.md').write_text(write_note(item, "\n" + body.strip() + "\n"), encoding="utf-8")
        written += 1

    # ── 2. The 17 §11 analyses. ───────────────────────────────────────────────────────────────────────────────
    plan = PLAN.read_text(encoding="utf-8")
    for m in A_ROW.finditer(plan):
        cells = [c.strip() for c in m.group("rest").split("|")]
        title = re.sub(r"\*\*", "", m.group("title"))
        summary = title.split("—")[0].strip()[:110]
        st = "landed" if re.search(r"CLOSED|✅ *DONE", " ".join(cells), re.I) else \
             "blocked" if re.search(r"BLOCKED", " ".join(cells), re.I) else "open"
        item = {
            "id": m.group("id"), "kind": "analysis", "status": st, "severity": "MAJOR",
            "area": "analysis", "summary": summary, "blocked_by": [], "blocked": st == "blocked",
            "spec_refs": sorted({c for c in CLAUSE.findall(m.group(0))})[:6],
            **harm_fields(["process"]),
        }
        body = "\n" + "\n".join(f"- {c}" for c in [title] + cells if c) + "\n"
        (WORK / f'{item["id"]}.md').write_text(write_note(item, body), encoding="utf-8")
        written += 1

    # ── 3. The residue findings, finally itemised. ────────────────────────────────────────────────────────────
    # ⚠ THE HEADING SAYS 16 AND THE PROSE ENUMERATES FEWER. That discrepancy is preserved, not papered over: the
    # items that exist become notes, and R00 records that the count and the enumeration disagree, so nobody later
    # reads "16" as a list they can work.
    rm = re.search(r"^### ⚠ RESIDUE — (?P<n>\d+) findings.*?\n(?P<body>(?:^>.*\n)+)", text, re.M)
    if rm:
        blob = " ".join(l.lstrip("> ").rstrip() for l in rm.group("body").splitlines())
        blob = blob.split("See the evidence ledger")[0]
        parts = [p.strip(" .·") for p in blob.split("·")]
        parts = [p for p in parts if len(p) > 25]
        for n, p in enumerate(parts, 1):
            wrong = bool(re.search(r"returns .* where|truncat|mis-fold|degrades SILENTLY|contaminates", p, re.I))
            rej = bool(re.search(r"refused|rejects|no lexer rule", p, re.I))
            crash = bool(re.search(r"throws at run time", p, re.I))
            flags = [f for f, on in (("wrong-answer", wrong), ("rejects-legal-source", rej),
                                     ("crashes", crash), ("silent", "silent" in p.lower()))]
            flags = [f for f, on in (("wrong-answer", wrong), ("rejects-legal-source", rej),
                                     ("crashes", crash), ("silent", "silent" in p.lower())) if on]
            item = {
                "id": f"R{n:02d}", "kind": "defect", "status": "open", "severity": "MINOR",
                "area": "residue", "summary": re.sub(r"\s+", " ", p)[:110],
                "blocked_by": [], "blocked": False,
                "spec_refs": sorted({c for c in CLAUSE.findall(p)})[:6], **harm_fields(flags),
            }
            (WORK / f'{item["id"]}.md').write_text(
                write_note(item, f"\n{p}\n\n> Migrated from the fix queue's RESIDUE block, which never itemised "
                                 f"these — so no work list could see them.\n"), encoding="utf-8")
            written += 1
        declared = int(rm.group("n"))
        if declared != len(parts):
            item = {"id": "R00", "kind": "adjudication", "status": "open", "severity": "MINOR",
                    "area": "residue", "blocked_by": [], "blocked": False, "spec_refs": [],
                    "summary": f"the residue block declares {declared} findings but enumerates {len(parts)}",
                    **harm_fields(["process"])}
            (WORK / "R00.md").write_text(write_note(item,
                f"\nThe fix queue's residue heading says **{declared} findings**; the prose beneath it enumerates "
                f"**{len(parts)}**. The missing {declared - len(parts)} were never written down, so they cannot be "
                f"worked and cannot be verified as already fixed. Reconcile against the batch-4 evidence ledger "
                f"(`docs/rearchitecture/evidence/PHASE-B-15.32-15.44-findings.md`) and either itemise or retire "
                f"the count.\n"), encoding="utf-8")
            written += 1
    print(f"migrated {written} work items into {WORK.relative_to(REPO)}")
    return 0


#: The frontmatter block: everything between the opening `---` line and the closing one. `\r?\n` because every
#: note in the register is CRLF in the working tree (`core.autocrlf`) and LF in the object store, and the C# twin
#: reads the file's BYTES — a reader that hard-codes one of the two answers "this is not a note" for a whole
#: register on the other checkout.
FRONT = re.compile(r"\A---\r?\n(?P<body>.*?)\r?\n---[ \t]*(?:\r?\n|\Z)", re.S)
#: A frontmatter key. Anything else on a line — a colon inside a title, a wrapped list's continuation — is not
#: one. A VALUE is a list when it opens with `[`, whatever its key: list-ness is a property of the value, so a
#: field added to the register tomorrow needs no edit here (and no second copy of a key vocabulary in C#).
FM_KEY = re.compile(r"\A[a-z_]+\Z")


def _split_list(inner: str) -> list[str]:
    """`"A", B , ` → `['A', 'B']` — the ONE list-member grammar, quoted or bare."""
    return [m for m in (x.strip().strip('"').strip("'").strip() for x in inner.split(",")) if m]


def parse_frontmatter(text: str) -> dict | None:
    """One note's frontmatter as `key -> str | bool | list[str]`, or `None` when the file is not a note.

    ⛔ THIS IS ONE HALF OF A TWO-LANGUAGE READER, AND THE HALVES DISAGREED. Its C# twin is
    `tests/_shared/WorkRegister.cs`; `tests/version-matrix/work-frontmatter-parity-cases.json` is the fixture
    both evaluate, and `work.py parity --json` is how the C# gate runs THIS engine for real rather than assuming
    it. `kb/Work/PB875` is the note recording what the disagreement cost: a list WRAPPED across two lines — the
    normal YAML shape, and the shape `kb/Work/PB205` had carried for months — was read here as a truncated list
    and in C# as NO list at all, so a note went on claiming four inventory rows in the register while the gate
    that enforces claims saw none, and `work.py check` called the same note well-formed.

    ⛔ AND IT FAILED **OPEN**. Both halves answered a malformed value with emptiness, which reads exactly like a
    note that claims nothing — the one answer a register must never invent. An unterminated list is now an ERROR
    CODE in `_errors` (the key is left ABSENT, never silently empty), and the codes are what the parity fixture
    compares: two readers that reject one note for two different reasons look identical under "it was rejected".

    ⛔ A RUNAWAY IS BOUNDED BY THE NEXT KEY, and that is measured, not assumed. A list left open by a missing
    bracket used to keep eating lines until some later line happened to end in `]` — so `tags: [cobolsharp,
    work, defect]` silently became three of `inventory_rows`' members and the malformation was never reported.
    A line that begins a new frontmatter key ends the open list with the error instead; no continuation of a
    real wrapped list can look like one, because its members are rule-ids, clause numbers and tag words.
    """
    m = FRONT.match(text)
    if m is None:
        return None
    out: dict = {"_errors": []}
    key: str | None = None        # the list key whose value is still being accumulated across lines
    buf = ""
    for raw in m.group("body").splitlines():
        line = raw.strip()
        if key is not None:
            head, sep, _ = line.partition(":")
            if sep and FM_KEY.match(head.strip()):
                out["_errors"].append(f"unterminated-list:{key}")
                key, buf = None, ""
            else:
                buf += " " + line
                if buf.endswith("]"):
                    out[key] = _split_list(buf[buf.find("[") + 1:-1])
                    key, buf = None, ""
                continue
        k, sep, v = line.partition(":")
        k, v = k.strip(), v.strip()
        if not sep or not FM_KEY.match(k):
            continue              # a continuation of a wrapped SCALAR, a comment, a blank line
        if not v.startswith("["):
            out[k] = True if v == "true" else False if v == "false" else v.strip('"')
        elif v.endswith("]"):
            out[k] = _split_list(v[1:-1])
        else:
            key, buf = k, v
    if key is not None:
        out["_errors"].append(f"unterminated-list:{key}")
    # The note BODY, so check() can catch the status written a second time in the H1 heading.
    out["_body"] = text[m.end():]
    return out


def load(work_dir: pathlib.Path = WORK) -> list[dict]:
    """Every note under `work_dir` (the register; a test passes a fixture directory)."""
    items = []
    for p in sorted(work_dir.glob("*.md")):
        d = parse_frontmatter(p.read_text(encoding="utf-8"))
        if d is None:
            continue
        d["_file"] = p.name
        items.append(d)
    return items


# The `# PB45 — LANDED — …` heading writes the status a SECOND time, beside the frontmatter. That is one rule in
# two places, and it drifted in FIVE notes before anyone looked (PB26/PB36/PB41/PB43/PB45 all read OPEN while
# their frontmatter said landed) — because flipping `status:` is what `work.py next` reads, so nothing ever
# contradicted the heading. A reader opening the note sees the heading first.
HEADING_STATUS = re.compile(r"^#\s+\S+\s+—\s+(?P<status>[A-Z][A-Z\- ]*?)\s+—", re.M)
# Heading spellings that mean the same thing as a frontmatter status.
HEADING_ALIASES = {"closed": "landed", "fixed": "landed", "done": "landed"}


def check_base_agrees() -> list[str]:
    """`kb/Work.base`'s **Fix next** filter shall select on the same harm flags as :data:`HARM_FLAGS`.

    ⛔ THE PREDICATE IS WRITTEN TWICE AND CANNOT BE WRITTEN ONCE — `work.py` is Python and the Bases view is a
    YAML expression Obsidian evaluates, with no import between them. Both copies read `wrong_answer or crashes`
    and both were wrong the same way, which is what hid nine open items; CLAUDE.md advertises the two as "the
    same list", so a reader has no way to notice when they stop being it. Since the copies must exist, this
    holds them together.
    """
    base = REPO / "kb" / "Work.base"
    if not base.exists():
        return [f"{base.relative_to(REPO)} is missing — the Fix next view is half the register's UI"]
    text = base.read_text(encoding="utf-8")
    m = re.search(r"name:\s*Fix next\s*\n\s*filters:\s*\n(?P<body>(?:\s{6,}.*\n)+)", text)
    if m is None:
        return ["kb/Work.base: no 'Fix next' view with a filters block — work.py check cannot verify it "
                "agrees with HARM_FLAGS, and CLAUDE.md tells readers the two are the same list"]
    # The one filter line that ORs the harm flags together.
    line = next((l for l in m.group("body").splitlines() if " or " in l and "wrong_answer" in l), "")
    named = set(re.findall(r"[a-z_]+", line)) & set(HARM_FLAGS)
    missing = [f for f in HARM_FLAGS if f not in named]
    out = [] if not missing else [
        f"kb/Work.base 'Fix next' does not select on {missing} — an item whose only harm flag is one of those "
        f"is invisible in the view while work.py next ranks it. Update the filter to match HARM_FLAGS."]

    # ⛔ THE SAME DUPLICATION ONE PROPERTY OVER. Every view of OPEN work spells the terminal statuses out one by
    # one, and `retired` was added to STATUSES after those views were written — so the first retired note went on
    # showing in "Open but no harm flag set" as though it were open, while `work.py next` (status in open/half)
    # correctly ignored it. Two readers of one register, disagreeing about what DONE means, is exactly the shape
    # `check_base_agrees` exists for. The invariant is written so the NEXT terminal status is automatic rather
    # than remembered: a view that filters out ANY terminal status filters out ALL of them. A view that does not
    # filter on status at all (`Everything`, `Analyses`) is not a view of open work and is left alone.
    for v in re.finditer(r"^    name:\s*(?P<name>.+?)\s*\n\s*filters:\s*\n\s*and:\s*\n(?P<body>(?:\s{6,}-.*\n)+)",
                         text, re.M):
        body = v.group("body")
        if not any(f'status != "{s}"' in body for s in TERMINAL_STATUSES):
            continue
        absent = [s for s in TERMINAL_STATUSES if f'status != "{s}"' not in body]
        if absent:
            out.append(
                f"kb/Work.base view {v.group('name')!r} hides some terminal statuses but not {absent} — an item "
                f"in one of those states still shows there as open work, while work.py counts it done. Add "
                f"`- status != \"<status>\"` for each of {list(TERMINAL_STATUSES)}.")
    return out


#: A traceability-inventory rule-id, by SHAPE only — `AR-15.7.3-1`, `SR-13.18.40.3-22`, `FMT-14.9.32.2`,
#: `GR-7.2.3.4-L2.1`. Whether the id names a row that EXISTS, and whether that row's verdict resolves, is the
#: C# gate's question and is deliberately not asked here (see :func:`closes_rows_shape`).
RULE_ID_SHAPE = re.compile(r"\A[A-Z]{2,4}-[0-9A-Za-z.]+(?:-[0-9A-Za-z.]+)?\Z")


def closes_rows_shape(it: dict) -> list[str]:
    """The `closes_rows` / `closes_rows_reason` back-link, checked for SHAPE — and only for shape.

    ⭐ THE FIELD (owner decision 2026-09-19, `kb/Work/PB245`). `inventory_rows` is a note's CLAIM while it is
    open; `closes_rows` is what its landing CLOSED, and it is the back-link the register never had. Without it
    "which rows did this fix close" was answerable only by re-measuring, and fourteen §15 rows held the GAP open
    on mechanisms seven landings had already closed — thirteen of them closed CONFORMS on first re-measurement.

    ⛔ WHAT IS CHECKED HERE, AND WHAT IS NOT — the same division `record_verdicts.py` draws, for the same reason.
    This validates what is decidable from the NOTE alone: the value is a list, each member is SPELLED like a
    rule-id, and a reason that is present is not empty. Whether a named row exists, whether its verdict resolves,
    and whether a landed defect note said anything at all belong to `ClosesRowsBackLinkDriftTests`
    (tests/Cobol.Net.Tests.Unit) — those predicates have to keep holding as the INVENTORY changes underneath a
    note that nobody is editing, so they belong to something that runs every build, and asking them here as well
    would be one rule in two places (`feedback_one_rule_one_place`).
    """
    bad: list[str] = []
    for e in it.get("_errors", []):
        bad.append(f'{it["_file"]}: frontmatter {e} — a wrapped list whose bracket never closes is read as '
                   f'NOTHING by both readers of this register (kb/Work/PB875); close it, or put it on one line')
    rows = it.get("closes_rows")
    if rows is not None and not isinstance(rows, list):
        bad.append(f'{it["_file"]}: closes_rows is {rows!r}, not a list — write `closes_rows: [RULE-ID, …]`')
    elif rows:
        for r in rows:
            if not RULE_ID_SHAPE.match(r):
                bad.append(f'{it["_file"]}: closes_rows names {r!r}, which is not spelled like an inventory '
                           f'rule-id (e.g. AR-15.7.3-1) — it is a row id, never a note id')
    reason = it.get("closes_rows_reason")
    if reason is not None and (not isinstance(reason, str) or not reason.strip()):
        bad.append(f'{it["_file"]}: closes_rows_reason is present but empty — a landing that closed no '
                   f'inventory row says WHY in it, and an empty value is the silence the field replaced')
    return bad


def check() -> int:
    if not WORK.exists():
        print(f"⛔ {WORK.relative_to(REPO)} does not exist — run: python scripts/spec/work.py migrate")
        return 1
    items, bad = load(), []
    for it in items:
        for f in ("id", "kind", "status", "area"):
            if not it.get(f):
                bad.append(f'{it["_file"]}: missing {f}')
        if it.get("kind") not in KINDS:
            bad.append(f'{it["_file"]}: kind {it.get("kind")!r} not in {sorted(KINDS)}')
        if it.get("status") not in STATUSES:
            bad.append(f'{it["_file"]}: status {it.get("status")!r} not in {sorted(STATUSES)}')
        # The H1's status word, when it carries one, shall agree with the frontmatter.
        if (h := HEADING_STATUS.search(it.get("_body", ""))) is not None:
            spelled = h.group("status").strip().lower()
            meant = HEADING_ALIASES.get(spelled, spelled)
            if meant in STATUSES and meant != it.get("status"):
                bad.append(f'{it["_file"]}: heading says {h.group("status").strip()!r} but '
                           f'status is {it.get("status")!r} — a landed note that still reads OPEN is how the '
                           f'register lies to a reader (the frontmatter is what work.py next reads)')
        # ⛔ AN OPEN DEFECT WITH NO HARM FLAG IS INVISIBLE TO `next`, AND SILENCE IS HOW IT STAYS THAT WAY.
        # `actionable` selects on HARM_FLAGS, so a defect that sets none of them is filed and then never
        # ranked — the same disappearance the two-flag predicate used to cause for every false-reject. An item
        # that genuinely does nothing to a user's program says so with `process_only: true`.
        if (it.get("kind") == "defect" and it.get("status") in ("open", "half")
                and not it.get("process_only") and not any(it.get(f) for f in HARM_FLAGS)):
            bad.append(f'{it["_file"]}: open defect with no harm flag set — it can never appear in '
                       f'`work.py next`. Set one of {list(HARM_FLAGS)}, or process_only: true if it '
                       f'genuinely does nothing to a user\'s program.')
        bad += closes_rows_shape(it)
    bad += topology_problems(items)
    ids = [it.get("id") for it in items]
    for i in set(ids):
        if ids.count(i) > 1:
            bad.append(f"duplicate id {i}")
    bad += check_base_agrees()
    if bad:
        print(f"⛔ {len(bad)} problem(s) in the work register:")
        for b in bad[:20]:
            print(f"      {b}")
        return 1
    print(f"✓ {len(items)} work items, all well-formed")
    return 0


#: The harm flags that make an open item ACTIONABLE — what the defect DOES to a user's program.
#:
#: ⛔ THIS SET USED TO BE `wrong_answer or crashes`, AND THE REGISTER THAT EXISTS TO ANSWER "WHAT DO I DO NOW"
#: COULD NOT SEE A DEFECT THAT REJECTS LEGAL SOURCE. Nine open items were hidden by that predicate when it was
#: measured (2026-08-05) — four of them flagged `rejects_legal_source`, which CLAUDE.md rule 4 calls the one
#: outcome forbidden outright, and five `under_rejects`. PB51 (`COMPUTE WS-N = ZERO` refused as a boolean
#: Format-2 ALL literal) is the case that exposed it: a false REJECT crashes nothing and computes nothing wrong,
#: it simply stops the user's program compiling, so the two-flag predicate scored it as no harm at all.
#:
#: ⚠ `silent` is deliberately ABSENT and is not an omission: it QUALIFIES a wrong answer (silently wrong vs.
#: loudly wrong) rather than naming a harm of its own, so an item flagged silent alone would be one whose
#: frontmatter has not said what it actually does. `process_only` is likewise not here — it marks an item that
#: does nothing to a user's program by definition.
HARM_FLAGS = ("wrong_answer", "crashes", "rejects_legal_source", "under_rejects")


def actionable(items: list[dict]) -> list[dict]:
    sev = {"BLOCKER": 0, "MAJOR": 1, "MINOR": 2, "OWNER": 3}
    live = [i for i in items if i.get("status") in ("open", "half")
            and any(i.get(f) for f in HARM_FLAGS)
            and not i.get("process_only") and not i.get("blocked")]
    return sorted(live, key=lambda i: (sev.get(i.get("severity"), 9), i.get("id", "")))


# ── clusters and the blocked_by topology (kb/Work PB2120) ─────────────────────────────────────────────────────
#
# ⭐ A NAMED CLUSTER IS A PLANNABLE UNIT. `actionable` answers "what harms a user's program"; a campaign (the legacy
# retirement, the architecture review) is `process_only` work that ranks nowhere there, so it was dispatched by
# hand. `cluster:` names the campaign a note belongs to (usually its lead note's id, which may list itself) and
# `blocked_by:` orders the campaign. `cluster_order` is the ONE reader of both: `work.py next --cluster`,
# `plan_wave.py --cluster` and `next_unit.py` all call it, so "ready" means one thing everywhere.
#
# ⛔ `blocked:` IS A SECOND SPELLING OF `blocked_by:`, AND THEY DRIFTED. The fix lane skips a note whose flag is
# true (`.agent-fleet.json` skip_flag), so a flag left true after its blocker landed HIDES a live defect from every
# work list; two were found that way when this was written (PB1940 after PB2087 landed, PB1956 after PB322). So
# `check` holds the flag to the list: when `blocked_by` names notes, `blocked` is true exactly while one of them is
# still open. A flag with an empty list stays legal: it is a note blocked on something that is not a note.


def id_order(nid: str) -> tuple[int, str]:
    """Sort key: a note id by its number (PB98 before PB120), a name without one last."""
    m = re.search(r"\d+", nid or "")
    return (int(m.group()) if m else 1 << 30, nid or "")


def open_blockers(it: dict, by_id: dict[str, dict]) -> list[str]:
    """The note's `blocked_by` entries that are not terminal (an id the register lacks counts as open)."""
    return [b for b in (it.get("blocked_by") or [])
            if by_id.get(b, {}).get("status") not in TERMINAL_STATUSES]


def cluster_members(items: list[dict], lead: str) -> list[dict]:
    """Every note whose `cluster` list names `lead`, terminal or not — THE membership rule, so `cluster_order` and the
    ledger's program section (`gen_ledger.py`) count one population. A list only: a scalar `cluster: PB2108` would
    match by SUBSTRING (`check` reports that shape)."""
    return [i for i in items if isinstance(i.get("cluster"), list) and lead in i["cluster"]]


def cluster_order(items: list[dict], lead: str) -> dict:
    """The cluster `lead` names: `{"cluster", "named", "notes": [...]}`, `notes` being every NON-TERMINAL note whose
    `cluster` list holds `lead`, whatever its kind, harm flags or `process_only`, in `blocked_by` topological order.

    Each entry is `{"id", "kind", "status", "area", "title", "depth", "ready", "waiting_on", "blockers_in_cluster"}`:
    `depth` is 0 for a note no open cluster member blocks and one past its deepest open in-cluster blocker otherwise
    (`None` inside a cycle); `waiting_on` lists every open blocker, in the cluster or not, plus the reason a note is
    held without one (`status: owner`, `status: blocked`, or `blocked: true` with no `blocked_by`); `ready` is
    `waiting_on == []`. Ties sort by note number. `named` counts every note that names the cluster, landed or not,
    so a caller can tell a LANDED cluster (named > 0, notes empty) from a misspelled one (named == 0)."""
    by_id = {i.get("id"): i for i in items}
    named = cluster_members(items, lead)
    members = {i["id"]: i for i in named if i.get("status") not in TERMINAL_STATUSES}
    depth: dict[str, int | None] = {}

    def depth_of(nid: str, on_path: set[str]) -> int | None:
        """None exactly when the note reaches a cycle, which does not depend on the path taken, so it memoizes."""
        if nid in depth:
            return depth[nid]
        if nid in on_path:
            return None
        on_path.add(nid)
        ds = [depth_of(b, on_path) for b in open_blockers(members[nid], by_id) if b in members]
        on_path.discard(nid)
        depth[nid] = None if None in ds else (1 + max(ds) if ds else 0)
        return depth[nid]

    rows = []
    for nid, it in members.items():
        waiting = open_blockers(it, by_id)
        if it.get("status") in ("owner", "blocked"):
            waiting.append(f"status: {it['status']}")
        elif it.get("blocked") and not it.get("blocked_by"):
            waiting.append("blocked: true (no blocked_by note)")
        rows.append({"id": nid, "kind": it.get("kind", ""), "status": it.get("status", ""), "area": it.get("area", ""),
                     "title": str(it.get("title", "")), "depth": depth_of(nid, set()), "ready": not waiting,
                     "waiting_on": waiting, "blockers_in_cluster": [b for b in waiting if b in members]})
    rows.sort(key=lambda r: (r["depth"] is None, r["depth"] or 0, id_order(r["id"])))
    return {"cluster": lead, "named": len(named), "notes": rows}


def topology_problems(items: list[dict]) -> list[str]:
    """`blocked_by` names notes that exist, `blocked` agrees with it, open notes form no blocking cycle, and
    `cluster` is a list of names (a lead note's id, or a campaign name such as `user-documentation`)."""
    by_id = {i.get("id"): i for i in items}
    bad = []
    for it in items:
        f = it["_file"]
        bb, cl = it.get("blocked_by", []), it.get("cluster", [])
        if not isinstance(cl, list) or not all(isinstance(c, str) and c for c in cl):
            bad.append(f"{f}: cluster must be a list of cluster names, got {cl!r}")
        if not isinstance(bb, list):
            bad.append(f"{f}: blocked_by must be a list of note ids, got {bb!r}")
            continue
        unknown = [b for b in bb if b not in by_id]
        for b in unknown:
            bad.append(f"{f}: blocked_by names {b!r}, which is not a kb/Work note id — name the blocking note "
                       f"(an owner question is a decision note), or keep `blocked: true` with an empty list and "
                       f"say what blocks it in the body")
        if unknown or not bb or it.get("status") in TERMINAL_STATUSES:
            continue
        still = open_blockers(it, by_id)
        if bool(it.get("blocked")) != bool(still):
            bad.append(f"{f}: blocked is {bool(it.get('blocked'))} but its blocked_by {bb} "
                       + (f"still has {still} open" if still else "have all landed or retired")
                       + " — set `blocked: " + ("true" if still else "false")
                       + "` (a stale true hides the note from every work list)")
    live = {i["id"]: i for i in items if i.get("status") not in TERMINAL_STATUSES and isinstance(i.get("blocked_by"), list)}
    state: dict[str, int] = {}

    def visit(nid: str, path: list[str]) -> None:
        state[nid] = 1
        for b in live[nid]["blocked_by"]:
            if b not in live:
                continue
            if state.get(b) == 1:
                bad.append("blocked_by cycle among open notes: " + " -> ".join(path[path.index(b):] + [b]))
            elif b not in state:
                visit(b, path + [b])
        state[nid] = 2

    for nid in sorted(live, key=id_order):
        if nid not in state:
            visit(nid, [nid])
    return bad


def print_cluster(view: dict, as_json: bool) -> int:
    if as_json:
        print(json.dumps(view, ensure_ascii=False))
        return 0 if view["named"] else 1
    lead, notes = view["cluster"], view["notes"]
    if not view["named"]:
        print(f"cluster {lead}: no kb/Work note names it in `cluster:`")
        return 1
    if not notes:
        print(f"cluster {lead}: LANDED ({view['named']} note(s), every one landed or retired)")
        return 0
    ready = sum(n["ready"] for n in notes)
    print(f"cluster {lead}: {len(notes)} open note(s), {ready} ready, {len(notes) - ready} waiting (blocked_by order)")
    for n in notes:
        title = re.sub(rf"^{re.escape(n['id'])}\s*[—-]\s*", "", n["title"])[:90]
        state = "ready  " if n["ready"] else "waiting on " + ", ".join(n["waiting_on"])
        depth = "cyc" if n["depth"] is None else f"{n['depth']:3}"
        print(f"  {depth} {n['id']:7} {n['kind']:9} {n['area'][:18]:18} {state}" + (f"  {title}" if n["ready"] else ""))
    return 0


# ── the cross-language parity fixture ────────────────────────────────────────────────────────────────────────

FIXTURE = REPO / "tests" / "version-matrix" / "work-frontmatter-parity-cases.json"
FIXTURE_REL = "tests/version-matrix/work-frontmatter-parity-cases.json"


def parity_view(text: str) -> dict:
    """What a reader of this register SEES in one note — the normalized answer both engines must give.

    Absent and empty are deliberately the same answer for a list (`[]`) and for a scalar (`""`): the gate asks
    what a note SAYS, and a key that is missing says exactly as much as a key that is empty. What is NOT
    normalized away is `errors` — a malformed value is a third outcome, and collapsing it into "empty" is the
    fail-open that `kb/Work/PB875` is about.
    """
    d = parse_frontmatter(text)
    if d is None:
        return {"note": False, "id": "", "kind": "", "status": "", "inventory_rows": [], "closes_rows": [],
                "closes_rows_reason": "", "errors": []}
    scalar = lambda k: (v if isinstance(v := d.get(k, ""), str) else str(v))       # noqa: E731
    lst = lambda k: (v if isinstance(v := d.get(k, []), list) else [])             # noqa: E731
    return {"note": True, "id": scalar("id"), "kind": scalar("kind"), "status": scalar("status"),
            "inventory_rows": lst("inventory_rows"), "closes_rows": lst("closes_rows"),
            "closes_rows_reason": scalar("closes_rows_reason"), "errors": sorted(d.get("_errors", []))}


def _wrapped_list(text: str) -> bool:
    """True when some line opens a `[` that does not close on the same line — the PB875 shape."""
    return any("]" not in line[line.index("["):] for line in text.splitlines() if "[" in line)


def parity_findings(fixture: dict, answers: list[dict]) -> list[str]:
    """Where THIS engine disagrees with the fixture's recorded expectation, field by field."""
    bad = []
    for case, got in zip(fixture["cases"], answers, strict=True):
        for k, want in case["expect"].items():
            # A fixture naming a field this view does not have is a FINDING, not a traceback: the two
            # engines expose the same field set, so a key only one of them knows is exactly a parity bug.
            if k not in got:
                bad.append(f'parity case "{case["name"]}": the fixture expects a field {k!r} that this '
                           f'reader does not report — the two engines no longer describe the same note')
            elif got[k] != want:
                bad.append(f'parity case "{case["name"]}": {k} is {got[k]!r}, fixture says {want!r}')
    return bad


def parity(as_json: bool) -> int:
    """Evaluate `tests/version-matrix/work-frontmatter-parity-cases.json` — the fixture the C# twin evaluates too.

    ⛔ THE FIXTURE IS ALSO THIS ENGINE'S SELF-TEST, AND IT ASSERTS ITS OWN POPULATION. A parity run that measured
    no wrapped list, no malformed value and no non-note file would be green about nothing at all — which is
    precisely the state `work.py check` was in while `kb/Work/PB390` claimed four rows the gate could not see
    (`feedback_green_gates_arent_evidence`, `feedback_verdict_evidence_invariant`).
    """
    if not FIXTURE.exists():
        print(f"⛔ parity fixture not found: {FIXTURE_REL}")
        return 1
    fixture = json.loads(FIXTURE.read_text(encoding="utf-8"))
    answers = [{"name": c["name"], **parity_view(c["text"])} for c in fixture["cases"]]
    findings = parity_findings(fixture, answers)

    # POPULATION — the shapes this register actually contains, each of which broke one reader or the other.
    covered = {
        "a list wrapped across lines": any(_wrapped_list(c["text"]) for c in fixture["cases"]),
        "a malformed value reported as an error": any(a["errors"] for a in answers),
        "a file that is not a note": any(not a["note"] for a in answers),
        "CRLF line endings": any("\r\n" in c["text"] for c in fixture["cases"]),
    }
    findings += [f"the parity fixture carries no case for {what} — it cannot be measuring that shape"
                 for what, ok in covered.items() if not ok]
    if len(fixture["cases"]) < 8:
        findings.append(f'{len(fixture["cases"])} parity case(s) — too few to be measuring the reader')

    # FALSIFICATION — the comparison must be able to fail, or "no disagreement" is a statement about nothing.
    corrupted = json.loads(json.dumps(fixture))
    corrupted["cases"][0]["expect"]["id"] = corrupted["cases"][0]["expect"].get("id", "") + "-X"
    if not parity_findings(corrupted, [{"name": c["name"], **parity_view(c["text"])}
                                       for c in corrupted["cases"]]):
        findings.append("a fixture whose expected id is WRONG produced no finding — the comparison is inert")

    print(f"parity cases  : {len(answers)} from {FIXTURE_REL}")
    if as_json:
        print("JSON " + json.dumps({"findings": findings, "parity": answers}, ensure_ascii=False))
    if findings:
        print(f"\n⛔ {len(findings)} finding(s):")
        for f in findings:
            print(f"   {f}")
        return 1
    print("no findings.")
    return 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("cmd", choices=["migrate", "check", "next", "stats", "parity"])
    ap.add_argument("--top", type=int, default=3)
    ap.add_argument("--json", action="store_true", help="emit one machine-readable JSON line (parity, next --cluster)")
    ap.add_argument("--work", default=str(WORK), help="the register directory (default kb/Work; a test seam)")
    ap.add_argument("--cluster", metavar="LEAD", help="next: the open notes whose `cluster` names LEAD, in blocked_by "
                    "order, whatever their harm flags (a campaign; exit 1 when no note names it)")
    a = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if a.cmd == "migrate":
        return migrate()
    if a.cmd == "check":
        return check()
    if a.cmd == "parity":
        return parity(a.json)
    if a.cluster and a.cmd != "next":
        ap.error("--cluster goes with next")
    items = load(pathlib.Path(a.work))
    if a.cmd == "next" and a.cluster:
        return print_cluster(cluster_order(items, a.cluster), a.json)
    if a.cmd == "next":
        nxt = actionable(items)
        if not nxt:
            print("next   : (nothing unblocked harms a user's program)")
            return 0
        print("next   : " + " · ".join(f'{i["id"]} ({i["area"]})' for i in nxt[:a.top])
              + f"   [{len(nxt)} actionable]")
        return 0
    import collections
    print("kind   :", dict(collections.Counter(i.get("kind") for i in items)))
    print("status :", dict(collections.Counter(i.get("status") for i in items)))
    print("actionable now:", len(actionable(items)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
