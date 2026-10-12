#!/usr/bin/env python3
"""Back-fill `closes_rows:` onto the notes that have already landed — DERIVED, never asserted.

    python scripts/spec/backfill_closes_rows.py               # report what it would write; writes nothing
    python scripts/spec/backfill_closes_rows.py --apply       # write the frontmatter
    python scripts/spec/backfill_closes_rows.py --suspects    # the rows a landed note's closure did NOT reach
    python scripts/spec/backfill_closes_rows.py --self-test   # prove every derivation step here can FAIL

⭐ WHY THIS EXISTS. `closes_rows` (owner decision 2026-09-19, `kb/Work/PB245`) is the back-link from a landed fix
to the traceability-inventory rows it CLOSED. Every landing from here on writes it in its own change set — but
five hundred notes had already landed without it, and a field that is empty on the whole history is a field no
reader trusts and no gate can enforce. The back-fill is therefore part of the mechanism, not a tidy-up.

⛔ AND IT IS DERIVED FROM THE APPLIED BATCHES, NOT FROM RECALL. Verdicts are only ever written by
`record_verdicts.py`, and every application of one is a commit that changed
`tests/version-matrix/traceability-inventory.json`. Walking that file's history recovers exactly what each
landing changed: a row that moved from a NON-resolving verdict (or from no verdict at all) to a resolving one is
a row some landing closed, and the commit that did it is the landing. Nothing here reads a summary of the work;
it reads the work.

THE ATTRIBUTION RULE, in order — the first arm that yields a note wins:

  1. the PB ids named in the ROW'S OWN `notes` text after the change, intersected with the ids named in the
     COMMIT MESSAGE — the strongest evidence, because two independent records agree;
  2. the ids named in the row's `notes` alone — the batch said which note it was closing;
  3. the commit's single id, when the commit names EXACTLY ONE — a landing of one cluster;
  4. otherwise UNATTRIBUTED. A train that names five notes and a row that names none cannot be divided, and
     guessing would put a row on a note that did not close it, which is the failure this whole field exists to
     stop (`feedback_verdict_evidence_invariant`: a missing observation is not a negative one).

A second source joins that one: a TERMINAL note still holding a row in `inventory_rows` that the inventory
now calls OK. The register's own discipline says a row leaves `inventory_rows` when it is re-verdicted, so a
landed note still holding a closed row is a landing whose rows closed while it owned them.

⛔ WHAT IS WRITTEN. Both sources are filtered by the row's `state` TODAY — the schema's computed OK/GAP, which
is what the burn-down counts and is strictly stronger than 'the verdict resolves' (a CONFORMS row whose
covering test is missing is still GAP). A row that closed and later regressed is not a closed row, and
claiming it would turn `ClosesRowsBackLinkDriftTests` red for a reason that is not the note's fault. A `kind: defect` note that derives nothing gets `closes_rows: []` plus a
`closes_rows_reason` that SAYS it is derived, so a reader can tell a back-filled answer from a stated one.

⛔ IDEMPOTENT, AND IT NEVER OVERWRITES A HUMAN. A note that already carries `closes_rows` is left exactly as it
is and reported when the derivation disagrees: re-running this after an implementer has written the field by
hand must not silently replace their answer with a derived one.
"""
from __future__ import annotations

import argparse
import collections
import json
import pathlib
import re
import subprocess
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import work  # noqa: E402  (same directory; this is the register's ONE reader)

REPO = pathlib.Path(__file__).resolve().parents[2]
INVENTORY_REL = "tests/version-matrix/traceability-inventory.json"
SCHEMA = REPO / "tests" / "version-matrix" / "inventory-schema.json"
PB = re.compile(r"\b(PB\d+[a-z]?)\b")
#: The one sentence a back-filled empty answer carries. It names its own provenance on purpose: a reader can
#: then tell a DERIVED "nothing" from an implementer's stated one, and re-derive it rather than believe it.
DERIVED_REASON = ("no inventory row is attributable to this landing — derived by "
                  "scripts/spec/backfill_closes_rows.py from the inventory's own commit history, re-derivable")


def git(*args: str) -> str:
    return subprocess.run(["git", *args], cwd=REPO, capture_output=True, text=True,
                          encoding="utf-8", errors="replace", check=True).stdout


def resolving_verdicts() -> set[str]:
    """The verdicts the SCHEMA marks as resolving — never a list written here (`feedback_one_rule_one_place`)."""
    schema = json.loads(SCHEMA.read_text(encoding="utf-8"))
    return {k for k, v in schema["verdicts"].items() if v.get("resolves")}


def pb_ids(text: str) -> set[str]:
    return set(PB.findall(text or ""))


def attribute(commit_message: str, row_notes: str) -> set[str]:
    """Which note(s) a single row's closure belongs to. See the four arms in the module docstring."""
    from_row, from_commit = pb_ids(row_notes), pb_ids(commit_message)
    both = from_row & from_commit
    if both:
        return both
    if from_row:
        return from_row
    if len(from_commit) == 1:
        return from_commit
    return set()


def verdicts_at(sha: str) -> dict[str, tuple[str, str]]:
    """`rule-id -> (verdict, notes)` as of one commit, or `{}` when the file is not in that tree."""
    try:
        raw = git("show", f"{sha}:{INVENTORY_REL}")
    except subprocess.CalledProcessError:
        return {}
    return {r["rule-id"]: (r.get("verdict") or "", r.get("notes") or "") for r in json.loads(raw)}


def closures_from_history(resolving: set[str], progress=None) -> tuple[dict[str, set[str]], dict]:
    """`note id -> rule-ids it closed`, walked over every commit that touched the inventory."""
    log = git("log", "--reverse", "--format=%H%x1f%B%x1e", "--", INVENTORY_REL)
    commits = [c.strip() for c in log.split("\x1e") if c.strip()]
    closed: dict[str, set[str]] = collections.defaultdict(set)
    stats = {"commits": len(commits), "transitions": 0, "unattributed": 0, "by-arm": collections.Counter()}
    previous: dict[str, tuple[str, str]] = {}
    for i, entry in enumerate(commits):
        sha, _, message = entry.partition("\x1f")
        current = verdicts_at(sha)
        if progress and i % 25 == 0:
            progress(i, len(commits))
        for rule_id, (verdict, notes) in current.items():
            was = previous.get(rule_id, ("", ""))[0]
            if verdict not in resolving or was in resolving:
                continue
            stats["transitions"] += 1
            ids = attribute(message, notes)
            stats["by-arm"]["attributed" if ids else "unattributed"] += 1
            if not ids:
                stats["unattributed"] += 1
            for note_id in ids:
                closed[note_id].add(rule_id)
        previous = current
    return closed, stats


def derive(items: list[dict], state_now: dict[str, str],
           closed: dict[str, set[str]]) -> dict[str, list[str]]:
    """What each TERMINAL note's `closes_rows` should say — history, plus the closed rows it still holds.

    ⚠ THE FILTER IS THE ROW'S `state`, NOT ITS VERDICT. `state` is what the schema computes from the
    verdict AND the evidence that verdict requires, and it is what the burn-down counts: a CONFORMS row
    whose covering test is missing is still a GAP row, and writing it here would claim work the GAP has
    not been told about — while turning `ClosesRowsBackLinkDriftTests` red, since that gate asks the same
    question. (Measured 2026-09-20: the two answers coincide on all 4,348 rows. The stronger one is free.)
    """
    out: dict[str, list[str]] = {}
    for it in items:
        if it.get("status") not in work.TERMINAL_STATUSES:
            continue
        held = {r for r in (it.get("inventory_rows") or []) if state_now.get(r) == "OK"}
        rows = {r for r in closed.get(it.get("id"), set()) if state_now.get(r) == "OK"} | held
        out[it["_file"]] = sorted(rows)
    return out


# ── writing the field ────────────────────────────────────────────────────────────────────────────────────────

#: The register's own line width. A note is READ by people in a diff and in Obsidian, and PB283 closes 77 rows —
#: 5 kB on one line. The field is therefore WRAPPED like the rest of the register's long lists, which also means
#: the back-fill dogfoods the reader repair: `kb/Work/PB875`'s wrapped-list shape is now the shape three hundred
#: notes carry, so a reader that regresses to dropping it fails loudly on the next build instead of quietly.
WRAP = 110


def field_lines(rows: list[str]) -> list[str]:
    """`closes_rows: [...]`, wrapped at :data:`WRAP` with two-space continuations."""
    members = [f'"{r}"' for r in rows]
    lines, current = [], "closes_rows: ["
    for i, member in enumerate(members):
        piece = member + ("," if i + 1 < len(members) else "")
        if current.strip() not in ("closes_rows: [", "") and len(current) + 1 + len(piece) > WRAP:
            lines.append(current)
            current = "  " + piece
        else:
            current += ("" if current.endswith("[") else " ") + piece
    return lines + [current + "]"]


def insert_field(text: str, lines: list[str]) -> str:
    """Add `lines` to the frontmatter, after `inventory_rows` where there is one, else before `tags`/the fence.

    ⛔ THE ANCHOR IS THE TWIN FIELD, NOT A LINE NUMBER. `closes_rows` reads beside `inventory_rows` — the claim
    and what the claim became — and a reader who finds one finds the other. Notes whose frontmatter is ordered
    differently (the migration wrote several shapes) fall back to the closing fence, never to a guess.

    ⛔ AND IT IS BYTE-FAITHFUL. Two notes in the register (`PB261`, `PB283`) carry a stray lone CR, which makes
    git treat them as NOT text — so a rewrite that normalized their line endings would show as a 200-line diff
    on a one-line change, and nothing in the landing would say which lines were really touched. The file's own
    line ending is DETECTED and re-used, the reader never translates (`newline=""`), and every line this
    function does not insert comes out exactly as it went in.
    """
    m = work.FRONT.match(text)
    if m is None:
        raise ValueError("not a note: no frontmatter")
    eol = "\r\n" if "\r\n" in text[:m.end()] else "\n"
    rows = m.group("body").split(eol)
    anchor = next((i for i, ln in enumerate(rows) if ln.strip().startswith("inventory_rows:")), None)
    if anchor is None:
        anchor = next((i for i, ln in enumerate(rows) if ln.strip().startswith("tags:")), None)
        anchor = len(rows) - 1 if anchor is None else anchor - 1
    # A wrapped list ends where its bracket closes, so the field lands AFTER the whole value, never inside it.
    while anchor + 1 < len(rows) and "]" not in "".join(rows[: anchor + 1]).rsplit("[", 1)[-1]:
        anchor += 1
    new_body = eol.join(rows[: anchor + 1] + lines + rows[anchor + 1:])
    return text[: m.start("body")] + new_body + text[m.end("body"):]


def apply_to_note(path: pathlib.Path, rows: list[str], want_reason: bool) -> str:
    """Write the field into one note. Returns what was done: `added`, `kept` or `differs`."""
    with open(path, encoding="utf-8", newline="") as fh:      # newline="" — translate NOTHING
        text = fh.read()
    item = work.parse_frontmatter(text)
    if item is None:
        return "skipped"
    if "closes_rows" in item:
        return "kept" if sorted(item["closes_rows"]) == rows else "differs"
    lines = field_lines(rows)
    if not rows and want_reason:
        lines.append(f'closes_rows_reason: "{DERIVED_REASON}"')
    with open(path, "w", encoding="utf-8", newline="") as fh:
        fh.write(insert_field(text, lines))
    return "added"


def suspects(items: list[dict], rows: list[dict]) -> list[str]:
    """Rows the inventory still counts as a GAP whose forensic `notes` name a note that has already LANDED.

    ⚠ THIS IS A MEASUREMENT, NOT A GATE, and the distinction is the honest part. A landed note named in a row's
    prose is not proof the row is stale — the note may be named for a reason that is not closure, which is why
    the register's back-link is the NOTE→ROW direction. But PB245's whole finding is that this population is
    where rot hides, and it had never been counted outside one clause family. It is counted here so the next
    golden round is aimed rather than sampled.
    """
    status = {it.get("id"): it.get("status") for it in items}
    out = []
    for r in rows:
        if r.get("state") == "OK":
            continue
        landed = sorted(i for i in pb_ids(r.get("notes", "")) if status.get(i) in work.TERMINAL_STATUSES)
        if landed:
            out.append(f'{r["rule-id"]}: verdict {r.get("verdict") or "(none)"}, state {r.get("state") or "(none)"} — its notes name '
                       f'{", ".join(landed)}, already terminal')
    return out


# ── self-test ────────────────────────────────────────────────────────────────────────────────────────────────

def self_test() -> int:
    cases: list[tuple[str, bool, str]] = []

    def check(name: str, ok: bool, detail: str = "") -> None:
        cases.append((name, ok, detail))

    # ── the attribution rule, arm by arm ─────────────────────────────────────────────────────────────
    check("arm 1 — commit and row agree, and the INTERSECTION wins over either alone",
          attribute("PB11+PB33: land the cap", "closed by PB33; PB11 recorded the guard") == {"PB11", "PB33"})
    check("arm 1 — a commit id the row does not name is NOT attributed when the row names another",
          attribute("PB11+PB33: land the cap", "closed by PB33") == {"PB33"})
    check("arm 2 — the row names a note the commit does not",
          attribute("Wave 40: nine rows re-verdicted", "closed by PB248") == {"PB248"})
    check("arm 3 — a commit naming exactly one note carries the row",
          attribute("PB125: FACTORIAL's standard-decimal arm", "") == {"PB125"})
    check("arm 4 — a train naming five notes and a row naming none is UNATTRIBUTED, never divided",
          attribute("PB1+PB2+PB3+PB4+PB5: train 39", "") == set())
    check("arm 4 — no evidence at all attributes nothing", attribute("chore: reformat", "") == set())
    check("a note id embedded in a word is not an id", attribute("see APB123X", "") == set())

    # ── the derivation: a row that later REGRESSED is not a closed row ───────────────────────────────
    items = [{"_file": "PB1.md", "id": "PB1", "status": "landed", "kind": "defect",
              "inventory_rows": ["GR-1.1-4", "GR-1.1-5"]},
             {"_file": "PB2.md", "id": "PB2", "status": "open", "kind": "defect", "inventory_rows": []}]
    state_now = {"GR-1.1-1": "OK", "GR-1.1-2": "GAP", "GR-1.1-4": "OK", "GR-1.1-5": "GAP",
                 "GR-1.1-6": "GAP"}   # GR-1.1-6: CONFORMS but untested — a resolving verdict, still a GAP
    got = derive(items, state_now, {"PB1": {"GR-1.1-1", "GR-1.1-2", "GR-1.1-6"}, "PB2": {"GR-1.1-1"}})
    check("a row the inventory still counts as a GAP is DROPPED — including one whose VERDICT resolves",
          got == {"PB1.md": ["GR-1.1-1", "GR-1.1-4"]}, f"got {got}")
    check("an OPEN note derives nothing — the field belongs to a landing", "PB2.md" not in got)

    # ── writing the field ────────────────────────────────────────────────────────────────────────────
    note = ('---\ntitle: "PB9 — x"\nid: PB9\nstatus: landed\ninventory_rows: ["GR-1.1-1"]\n'
            'tags: [cobolsharp, work, defect]\n---\n\n# PB9\n\nbody\n')
    once = insert_field(note, field_lines(["AR-15.7.3-1"]))
    parsed = work.parse_frontmatter(once)
    check("the field is written where a reader finds it — after inventory_rows",
          once.find("inventory_rows:") < once.find("closes_rows:") < once.find("tags:")
          and "closes_rows:" in once, once)
    check("and it reads back as the value written", (parsed or {}).get("closes_rows") == ["AR-15.7.3-1"])
    check("the body is untouched", once.endswith("---\n\n# PB9\n\nbody\n"))
    wrapped = ('---\nid: PB9\nstatus: landed\ninventory_rows: ["GR-1.1-1",\n  "GR-1.1-2"]\ntags: [x]\n---\nbody\n')
    after = insert_field(wrapped, field_lines(["AR-1-1"]))
    read_back = lambda t: work.parse_frontmatter(t) or {}                       # noqa: E731
    check("a WRAPPED inventory_rows is stepped over, never written into",
          read_back(after).get("inventory_rows") == ["GR-1.1-1", "GR-1.1-2"]
          and read_back(after).get("closes_rows") == ["AR-1-1"], after)
    crlf = note.replace("\n", "\r\n")
    out_crlf = insert_field(crlf, field_lines(["AR-1-1"]))
    check("CRLF in, CRLF out — a note a Windows tool wrote CRLF keeps its own line endings (the line-endings "
          "audit, kb/Work PB3029, is what reports the CR)",
          "\r\n" in out_crlf and "\r\r" not in out_crlf and "\n" not in out_crlf.replace("\r\n", "")
          and read_back(out_crlf).get("closes_rows") == ["AR-1-1"], repr(out_crlf[:120]))
    many = [f"GR-13.18.62.4-{i}" for i in range(1, 30)]
    big = insert_field(note, field_lines(many))
    check("a long list is WRAPPED and still reads back WHOLE — the back-fill dogfoods the PB875 repair",
          len(field_lines(many)) > 1 and read_back(big).get("closes_rows") == many,
          str(read_back(big).get("closes_rows")))
    check("no written line runs past the register's width",
          max(len(ln) for ln in field_lines(many)) <= WRAP + 2)
    empty = insert_field(note, field_lines([]) + [f'closes_rows_reason: "{DERIVED_REASON}"'])
    p2 = read_back(empty)
    check("an empty answer carries its reason",
          p2.get("closes_rows") == [] and bool(p2.get("closes_rows_reason")))

    # ⛔ THE COMPARISONS MUST BE ABLE TO FAIL.
    check("insert_field refuses a file that is not a note",
          _raises(lambda: insert_field("# not a note\n", ["closes_rows: []"])))
    check("a derivation that named the WRONG row would be caught by the check above",
          derive(items, state_now, {"PB1": {"GR-1.1-2"}}) != {"PB1.md": ["GR-1.1-1", "GR-1.1-4"]})

    for name, ok, detail in cases:
        print(f"  {'PASS' if ok else 'FAIL'}  {name}" + (f"  — {detail}" if not ok and detail else ""))
    bad = sum(1 for _, ok, _ in cases if not ok)
    print(f"\n{len(cases) - bad}/{len(cases)} self-test case(s) passed")
    return 1 if bad else 0


def _raises(fn) -> bool:
    try:
        fn()
    except Exception:  # noqa: BLE001
        return True
    return False


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--apply", action="store_true", help="write the field (default: report only)")
    ap.add_argument("--suspects", action="store_true", help="rows still non-OK whose notes name a landed item")
    ap.add_argument("--self-test", action="store_true", help="prove every derivation step here can fail")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if args.self_test:
        return self_test()

    resolving = resolving_verdicts()
    rows = json.loads((REPO / INVENTORY_REL).read_text(encoding="utf-8"))
    state_now = {r["rule-id"]: r.get("state") or "" for r in rows}
    items = work.load()

    if args.suspects:
        found = suspects(items, rows)
        print(f"rows still counted as a GAP whose `notes` name a TERMINAL work note: {len(found)}")
        for f in found:
            print(f"   {f}")
        return 0

    print(f"walking {INVENTORY_REL} through its history …")
    closed, stats = closures_from_history(
        resolving, progress=lambda i, n: print(f"   {i}/{n} commits", flush=True))
    want = derive(items, state_now, closed)
    print(f"commits touching the inventory : {stats['commits']}")
    print(f"row closures observed          : {stats['transitions']} "
          f"({stats['unattributed']} unattributable to a single note)")
    print(f"terminal notes                 : {len(want)}")
    print(f"  deriving at least one row    : {sum(1 for v in want.values() if v)}")
    print(f"  rows attributed in total     : {sum(len(v) for v in want.values())}")

    done = collections.Counter()
    for file, derived in sorted(want.items()):
        item = next(i for i in items if i["_file"] == file)
        # ⚠ The field is written where it SAYS something: a note that closed rows, and every landed DEFECT,
        # which is the kind the gate obliges. A bare `closes_rows: []` on a landed analysis would assert
        # "closed nothing" about a note whose landing was never going to close a row — noise that reads like
        # evidence, on 171 notes.
        if not derived and item.get("kind") != "defect":
            done["not obliged"] += 1
            continue
        what = (apply_to_note(REPO / "kb" / "Work" / file, derived, item.get("kind") == "defect")
                if args.apply else
                ("kept" if "closes_rows" in item else "added"))
        done[what] += 1
        if what == "differs":
            print(f"   ⚠ {file}: carries a closes_rows that differs from the derivation — left as written")
    print(("wrote" if args.apply else "would write") + f": {dict(done)}")
    if not args.apply:
        print("\n(nothing written — re-run with --apply)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
