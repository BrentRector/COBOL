#!/usr/bin/env python3
"""Hold FROZEN evidence to its bargain: a claim the tree has since REFUTED must SAY SO, in machine-readable form.

    python scripts/spec/audit_evidence_supersession.py             # report
    python scripts/spec/audit_evidence_supersession.py --check     # exit 1 on any finding (the gate)
    python scripts/spec/audit_evidence_supersession.py --anchors    # also list line-coordinate drift (not gating)
    python scripts/spec/audit_evidence_supersession.py --self-test # prove every check FAILS on a real defect

⛔ WHY THIS EXISTS. `docs/rearchitecture/evidence/*.json` is FROZEN evidence — an agent's raw output, recorded as
it was said (owner decision, `kb/Work/PB785`, 2026-09-06). It is NOT a document CLAUDE.md rule 6 makes current:
repairing a sentence inside it would rewrite the record of what was found, which is the opposite of what these
files are for, and `scripts/spec/citation_corpus.py` already names the directory in its `FROZEN` tuple so BOTH
citation audits skip it. The freeze buys fidelity and it costs currency, and the cost is not theoretical:
`PHASE-13-grammar-batch-research.json:81` says *"C6-A (WRITE BEFORE AND AFTER ADVANCING) IS FULLY LANDED
end-to-end — grammar `writeBeforeAfter : writeAdvancePhrase writeAdvancePhrase?` … C6-A must NOT be
re-implemented"*, and that judgment is what `kb/Work/PB712` refuted — the rule rejected the ONE printed spelling
of the 2023 combination and accepted a two-operand form the standard never prints. A future scout arriving by
grep reads a keyword-dense JSON full of `LANDED` verdicts and is told not to look. **That is exactly how PB712's
defect was built in the first place.**

So the bargain is: the record is never edited, and a refuted claim carries a `superseded_by` object — placed as
the FIRST key of the object holding it, so a reader meets the refutation BEFORE the claim — plus a top-of-file
`_frozen` banner saying what the directory is and pointing at the convention. This audit is the drift check that
keeps the bargain true as the tree moves under an unmovable record (CLAUDE.md rule 5: pair the structure with a
test so "automatic" stays automatic).

⛔ WHAT IS GATED, AND WHY EACH ONE IS A CLAIM THE TREE CAN CONTRADICT.

  · `banner` — every `*.json` in the frozen directory carries a top-level `_frozen` string naming the
    `superseded_by` convention. Without it the next file added to the directory inherits the freeze silently and
    a reader has no way to know the file is a record rather than a claim. The banner is required to NAME the
    marker key, so the convention is discoverable from any file that has it, not only from this docstring.
  · `path` — a `path:line` citation whose FILE no longer exists. A path with a LINE NUMBER on it is, by
    construction, a measurement of a file that existed when it was written; a bare path is very often a
    PROPOSAL (`tests/conformance/2023/alt_key_suppress.cob` — a golden the research asked for, never written).
    That distinction is not a heuristic, it is measured: on the tree at the time of writing, 122 bare paths in
    this directory did not exist and every single one was a proposal, while ZERO line-anchored citations were
    dangling. Checking bare paths would be 122 false reds and would have killed the gate on day one.
  · `rule` — an ANTLR rule name CLAIMED to be in the grammar that is not in `src/Cobol.Net.Frontend/Grammar`.
    Two extraction arms, both narrow on purpose (below).
  · `marker` — a `superseded_by` that does not carry `note`, `date` and `why`. An exemption is a SUPPRESSION,
    and an empty suppression is a mute button: the marker has to say which note refuted the claim, when, and
    what the tree says instead, or it is not a marker.

⛔ HOW A "CLAIMED RULE NAME" IS EXTRACTED, AND WHY IT IS NARROW. The first draft took every camelCase word out
of any string that mentioned a `.g4` file, and it accused four correct sites at once: `popMode` (an ANTLR lexer
COMMAND, `-> popMode`), `nameSlot`/`subscriptTrigger` (JSON field names quoted inside prose), and — the
instructive one — `editingClause` in *"dataDescriptionClause alternatives; no editingClause"*, a NEGATIVE claim
that the rule does not exist, which is still TRUE. A checker that fires on a correct negative claim teaches the
reader to ignore it. The two arms kept are the ones where the text puts a name in a DECLARATION position:

  1. ADJACENCY — a camelCase identifier immediately beside a `<something>.g4:<line>` citation, on either side,
     through nothing but punctuation: `CobolIO.g4:357-366 — writeBeforeAfter/writeAdvancePhrase`,
     `` `fileControlClauses` (…/CobolIO.g4:46) ``. The citation says WHERE, the adjacent name says WHAT.
  2. DECLARATION — a backticked ANTLR production, `` `writeBeforeAfter : writeAdvancePhrase writeAdvancePhrase?` ``,
     wherever it appears and whether or not a path is nearby. This arm is what reaches the `summary` prose of
     line 81, which cites no file at all — the primary refuted claim would otherwise be invisible to the gate
     that exists for it. ⛔ Only the BODY is checked, and only when the HEAD is a rule that EXISTS: this corpus
     also writes out productions for rules it is PROPOSING (`mcsFacilityStatement : (RECEIVE | SEND) (~DOT)*`
     under `risks`, `commitStatement` under `corrections`), which the grammar has no opinion about. A head that
     exists makes the line a statement about how that rule reads TODAY; a head that does not is a sketch.

⚠ COORDINATE DRIFT IS REPORTED, NEVER GATED. A citation like `CobolIO.g4:146-149 — alternateKeyClause` names a
rule that still exists, in the file it says, at lines that have since moved (it is at :205 today). Nine such
drifts were measured in this directory. They are NOT supersessions — nothing about the claim is refuted, only
its coordinates decayed — and gating on them would force nine markers that say something false, or force
re-pointing the record, which is the one thing the freeze forbids. The `_frozen` banner states the true and
permanent fact instead — *resolve the NAME, never the line number* — and this audit prints the drift COUNT every
run so that sentence keeps its evidence. `--anchors` lists them.

⚠ IT NEEDS NO SUBMODULE, and that is measured rather than hoped. Its two siblings must announce a loud SKIP
when `specs/` is absent because they read the standard; this one reads only the tree it checks against. The 46
`specs/ISO_COBOL.md:<line>` citations in the corpus point at a file TRACKED IN THIS REPOSITORY — the private
submodule is `specs-private/`, which no evidence file cites and whose prefix this pattern does not even match —
so `--check` behaves identically in a checkout with `submodules: false`.

⛔ THE POPULATION IS ASSERTED, NEVER ASSUMED. Zero files or zero citations is a REFUSAL (exit 2), not a pass: a
green that never looked at anything is the failure shape `feedback_green_gates_arent_evidence` is about. The
directory is additionally cross-checked against `citation_corpus.FROZEN` — if someone takes it out of the frozen
set, this audit fails loudly rather than going on guarding a file the citation audits have started editing.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys

import citation_corpus  # noqa: E402  (same directory; the ONE definition of "frozen")
import work  # noqa: E402  (work.tracked_paths: a path exists when the COMMITTED tree has it; PB1957 2026-10-07)

REPO = pathlib.Path(__file__).resolve().parents[2]

#: The frozen directory this audit guards. ⛔ One name, and it is CHECKED against `citation_corpus.FROZEN`
#: rather than trusted: the two facts "the citation audits skip it" and "this audit guards it" are the same
#: fact, and they must not be able to drift apart. `spec-reconciliation/` — the other frozen directory — is
#: deliberately out of scope: it is evidence about the SPEC PDF against its transcription, and 456 files there
#: carry FOUR line-anchored citations into the tree between them, so the drift this audit measures is not the
#: drift that directory can suffer.
EVIDENCE = "docs/rearchitecture/evidence"

GRAMMAR = REPO / "src" / "Cobol.Net.Frontend" / "Grammar"

#: The banner key, the marker key, and the fields a marker must carry to be one.
BANNER_KEY = "_frozen"
MARKER_KEY = "superseded_by"
MARKER_FIELDS = ("note", "date", "why")

#: A citation is a repo-relative path WITH a line (or line range) on it — see the docstring: the line number is
#: what separates a measurement of the tree from a proposal about it.
CITE = re.compile(
    r"(?<![\w/.-])((?:src|tests|docs|scripts|kb|specs|data|samples)/[A-Za-z0-9_./+-]*\.[A-Za-z0-9_]+)"
    r":(\d+)(?:\s*[-–]\s*(\d+))?")
_IDENT = r"[A-Za-z][A-Za-z0-9_]*"
#: Arm 1 — the name(s) immediately AFTER a citation, through punctuation only (`… — writeBeforeAfter/writeAdvancePhrase`).
AFTER = re.compile(r"^[\s—–\-:`*\"'(,]*(" + _IDENT + r"(?:\s*/\s*" + _IDENT + r")*)")
#: Arm 1 — the name immediately BEFORE it (``` `fileControlClauses` (…g4:46) ```).
BEFORE = re.compile(r"(" + _IDENT + r")[\s`*\"'(\[]*$")
#: Arm 2 — a backticked ANTLR production written out in prose. The SPACE before the `:` is load-bearing: it is
#: how ANTLR is written here (`rule\n    : alt`) and it is how a JSON field quoted in the same prose is NOT
#: (`` `introducedIn: 2023` ``, `` `"nameSlot": true` ``), which the first draft read as five productions.
DECL = re.compile(r"`\s*(" + _IDENT + r")\s+:\s*([^`]+)`")
#: A parser rule is camelCase. Lexer tokens and lexer MODES are ALL-CAPS and are not checked: `PICMODE` and
#: `PIC_STRING` are not rules, and the ALL-CAPS shape collides with ordinary emphasis in this prose (LANDED,
#: NOT, VERIFIED), which is a false-positive generator with no upside.
CAMEL = re.compile(r"^[a-z][A-Za-z0-9_]*[A-Z]")


def grammar_rules(grammar_dir: pathlib.Path) -> dict[str, list[tuple[str, int, int]]]:
    """Every rule declared in the ANTLR grammar → the file and line SPAN it occupies.

    ⛔ The declaration and its `:` are OFTEN SEPARATED BY A COMMENT — `performStatement` is followed by
    ``// Out-of-line: explicit forms …`` before its first alternative, and `valueClause` the same. A reader of
    the first draft of this function would have been told those two rules do not exist, which is the exact
    false accusation this audit is supposed to make impossible.
    """
    out: dict[str, list[tuple[str, int, int]]] = {}
    for g in sorted(grammar_dir.rglob("*.g4")):
        lines = g.read_text(encoding="utf-8").splitlines()
        for i, line in enumerate(lines):
            m = re.match(r"^(?:fragment\s+)?(" + _IDENT + r")\s*(:|$)", line)
            if not m:
                continue
            if m.group(2) != ":":
                j = i + 1
                while j < len(lines) and (not lines[j].strip() or lines[j].lstrip().startswith("//")):
                    j += 1
                if j >= len(lines) or not lines[j].lstrip().startswith(":"):
                    continue
            end = i
            while end < len(lines) and not re.sub(r"//.*$", "", lines[end]).rstrip().endswith(";"):
                end += 1
            out.setdefault(m.group(1), []).append((g.name, i + 1, min(end, len(lines) - 1) + 1))
    return out


def claims(text: str) -> tuple[list[tuple[str, int, int, str]], list[tuple[str, list[str]]]]:
    """The claims one string makes about the tree: (path, lo, hi, rule-or-empty) citations, and productions.

    A production comes back as (declared name, the rule names in its body) — see :func:`scan` for why only the
    body of a production whose HEAD already exists is a claim about the grammar.
    """
    cites: list[tuple[str, int, int, str]] = []
    declared: list[tuple[str, list[str]]] = []
    for m in CITE.finditer(text):
        lo, hi = int(m.group(2)), int(m.group(3) or m.group(2))
        named: list[str] = []
        if m.group(1).endswith(".g4"):
            a = AFTER.match(text[m.end():])
            if a:
                named += [x.strip() for x in a.group(1).split("/")]
            b = BEFORE.search(text[:m.start()])
            if b:
                named.append(b.group(1))
            named = [n for n in named if CAMEL.match(n)]
        cites.append((m.group(1), lo, hi, ""))
        for n in named:
            cites.append((m.group(1), lo, hi, n))
    for d in DECL.finditer(text):
        if CAMEL.match(d.group(1)):
            declared.append((d.group(1), [n for n in re.findall(_IDENT, d.group(2)) if CAMEL.match(n)]))
    return cites, declared


class Finding:
    __slots__ = ("kind", "rel", "line", "where", "says")

    def __init__(self, kind: str, rel: str, line: int, where: str, says: str) -> None:
        self.kind, self.rel, self.line, self.where, self.says = kind, rel, line, where, says


def _line_of(raw: list[str], needle: str) -> int:
    for n, line in enumerate(raw, 1):
        if needle and needle in line:
            return n
    return 0


def scan(files: list[pathlib.Path], rules: dict[str, list[tuple[str, int, int]]],
         root: pathlib.Path) -> tuple[list[Finding], list[Finding], dict[str, int]]:
    """Returns (gating findings, coordinate-drift findings, population counts)."""
    findings: list[Finding] = []
    drift: list[Finding] = []
    stats = {"files": 0, "citations": 0, "ruleclaims": 0, "markers": 0}
    tracked = work.tracked_paths(root)   # never the filesystem: a build output exists only in a built checkout

    for f in sorted(files):
        rel = f.relative_to(root).as_posix()
        raw = f.read_text(encoding="utf-8").splitlines()
        try:
            doc = json.loads("\n".join(raw))
        except json.JSONDecodeError as e:
            findings.append(Finding("json", rel, e.lineno, "", f"is not parseable JSON: {e.msg}"))
            continue
        stats["files"] += 1

        banner = doc.get(BANNER_KEY) if isinstance(doc, dict) else None
        if not isinstance(banner, str) or not banner.strip():
            findings.append(Finding("banner", rel, 1, "", (
                f"carries no top-level {BANNER_KEY!r} banner — nothing in the file tells a reader it is a "
                f"RECORD rather than a claim")))
        elif MARKER_KEY not in banner:
            findings.append(Finding("banner", rel, _line_of(raw, BANNER_KEY), "", (
                f"the {BANNER_KEY!r} banner does not name the {MARKER_KEY!r} convention, so the marker is "
                f"discoverable only from the audit's own source")))

        def visit(node: object, where: str, exempt: bool) -> None:
            if isinstance(node, dict):
                marker = node.get(MARKER_KEY)
                if marker is not None:
                    stats["markers"] += 1
                    missing = ([k for k in MARKER_FIELDS if not str(marker.get(k, "")).strip()]
                               if isinstance(marker, dict) else list(MARKER_FIELDS))
                    if missing:
                        findings.append(Finding("marker", rel, _line_of(raw, f'"{MARKER_KEY}"'), where, (
                            f"{MARKER_KEY} is missing {', '.join(missing)} — an exemption that does not say "
                            f"which note refuted the claim, when, and what the tree says instead is a mute "
                            f"button, not a marker")))
                    exempt = True
                for k, v in node.items():
                    visit(v, f"{where}.{k}" if where else k, exempt)
                return
            if isinstance(node, list):
                for i, v in enumerate(node):
                    visit(v, f"{where}[{i}]", exempt)
                return
            if not isinstance(node, str):
                return
            cites, declared = claims(node)
            for path, lo, hi, rule in cites:
                if not rule:
                    stats["citations"] += 1
                    if path not in tracked:
                        if not exempt:
                            findings.append(Finding("path", rel, _line_of(raw, f"{path}:{lo}"), where,
                                                    f"cites {path}:{lo} — that file no longer exists"))
                    continue
                stats["ruleclaims"] += 1
                spans = rules.get(rule)
                if spans is None:
                    if not exempt:
                        findings.append(Finding("rule", rel, _line_of(raw, f"{path}:{lo}"), where, (
                            f"cites grammar rule `{rule}` at {path}:{lo} — no rule of that name is in the "
                            f"grammar today")))
                elif not any(n == pathlib.Path(path).name and not (hi < s or lo > e) for (n, s, e) in spans):
                    drift.append(Finding("anchor", rel, _line_of(raw, f"{path}:{lo}"), where, (
                        f"`{rule}` is cited at {path}:{lo}-{hi}; it is at "
                        f"{', '.join(f'{n}:{s}-{e}' for n, s, e in spans)} today")))
            # ⛔ ONLY THE BODY OF A PRODUCTION WHOSE HEAD ALREADY EXISTS. `mcsFacilityStatement : (RECEIVE |
            # SEND) (~DOT)*` and `commitStatement : …` are written out in the `risks` and `designDefects` of
            # this very corpus as rules that COULD be added — proposals, which the grammar has no opinion
            # about, and reading them as claims accused four correct sites. But `writeBeforeAfter :
            # writeAdvancePhrase writeAdvancePhrase?` names a rule that DOES exist, so it is a statement about
            # how that rule reads today, and every rule its body names must be there too. That is the arm that
            # reaches the `summary` prose of line 81 — the primary refuted claim, which cites no file at all
            # and would otherwise be invisible to the gate that exists for it.
            for head, body in declared:
                if head not in rules:
                    continue
                for rule in body:
                    stats["ruleclaims"] += 1
                    if rule not in rules and not exempt:
                        findings.append(Finding("rule", rel, _line_of(raw, f"`{head} :"), where, (
                            f"writes the production `{head} : …` with `{rule}` in its body — `{head}` is a "
                            f"real rule, so this is a claim about how it reads today, and `{rule}` is not in "
                            f"the grammar")))

        visit(doc, "", False)

    # One production naming the same absent rule twice is ONE defect, not two — a finding list that counts a
    # repetition as a second site makes the verdict line lie about how much is wrong.
    def dedupe(fs: list[Finding]) -> list[Finding]:
        seen: set[tuple[str, str, int, str, str]] = set()
        out = []
        for f in fs:
            key = (f.kind, f.rel, f.line, f.where, f.says)
            if key not in seen:
                seen.add(key)
                out.append(f)
        return out

    return dedupe(findings), dedupe(drift), stats


def _report(findings: list[Finding], drift: list[Finding], stats: dict[str, int], show_anchors: bool) -> None:
    print(f"{stats['files']} frozen evidence JSON(s) · {stats['citations']} line-anchored citation(s) · "
          f"{stats['ruleclaims']} grammar-rule claim(s) · {stats['markers']} {MARKER_KEY} marker(s)")
    print(f"⛔ {len(findings)} UNMARKED CLAIM(S) THE TREE CONTRADICTS — these are defects")
    for f in findings:
        print(f"\n  {f.rel}:{f.line} [{f.kind}] {f.where}\n     {f.says}")
    print(f"\n— {len(drift)} line-coordinate drift(s): a claimed rule that still exists but has MOVED. "
          f"Reported, never gating — the frozen record cannot be re-pointed, and the {BANNER_KEY} banner says so.")
    if show_anchors:
        for f in drift:
            print(f"  {f.rel}:{f.line} {f.where}\n     {f.says}")


def self_test() -> int:
    """Prove every gated check FAILS on a constructed defect, and that the marker SILENCES the ones it should.

    ⛔ A suppression that suppresses nothing is not one, and a check that has never contradicted anything is not
    a check. Each arm below is run twice — once on a document built to break it, once on the same document with
    the marker in place — because either half alone is compatible with a checker that always says the same thing.
    """
    import subprocess
    import tempfile
    rules = {"realRule": [("Fake.g4", 10, 12)]}
    banner = f"FROZEN — a refuted claim carries {MARKER_KEY}."
    marker = {"note": "PB712", "date": "2026-09-06", "why": "refuted"}
    cases = [
        ("banner", {"a": "x"}, None),
        ("banner", {BANNER_KEY: "frozen, no convention named", "a": "x"}, None),
        ("path", {BANNER_KEY: banner, "e": ["src/Cobol.Net.Nope/Gone.cs:12 — a claim"]}, MARKER_KEY),
        ("rule", {BANNER_KEY: banner, "e": ["src/Cobol.Net.Frontend/Grammar/Core/CobolIO.g4:10 — ghostRule"]},
         MARKER_KEY),
        ("rule", {BANNER_KEY: banner, "e": ["grammar `realRule : ghostRule ghostRule?` exists"]}, MARKER_KEY),
        ("marker", {BANNER_KEY: banner, "e": {MARKER_KEY: {"note": "PB712"}, "t": "x"}}, None),
    ]
    ok = True
    with tempfile.TemporaryDirectory() as d:
        root = pathlib.Path(d)
        subprocess.run(["git", "-C", d, "init", "-q"], check=True)   # the path arm reads the git index
        p = root / "case.json"
        for kind, doc, silencer in cases:
            p.write_text(json.dumps(doc, indent=1), encoding="utf-8")
            fires = any(f.kind == kind for f in scan([p], rules, root)[0])
            print(f"  {'ok  ' if fires else 'FAIL'} {kind:<7} fires on a document built to break it")
            ok &= fires
            if silencer:
                marked = {BANNER_KEY: doc[BANNER_KEY], MARKER_KEY: marker,
                          **{k: v for k, v in doc.items() if k != BANNER_KEY}}
                p.write_text(json.dumps(marked, indent=1), encoding="utf-8")
                silent = not any(f.kind == kind for f in scan([p], rules, root)[0])
                print(f"  {'ok  ' if silent else 'FAIL'} {kind:<7} silent on the same document WITH {MARKER_KEY}")
                ok &= silent
        # The coordinate drift arm reports and never gates — both halves matter. The cited FILE has to exist,
        # or the `path` arm fires first and "gates nothing" would pass for the wrong reason.
        (root / "src" / "Cobol.Net.Frontend" / "Grammar" / "Core").mkdir(parents=True, exist_ok=True)
        (root / "src" / "Cobol.Net.Frontend" / "Grammar" / "Core" / "Fake.g4").write_text("", encoding="utf-8")
        subprocess.run(["git", "-C", d, "add", "-A"], check=True)
        p.write_text(json.dumps({BANNER_KEY: banner,
                                 "e": ["src/Cobol.Net.Frontend/Grammar/Core/Fake.g4:900 — realRule"]}, indent=1),
                     encoding="utf-8")
        f2, drift, _ = scan([p], rules, root)
        moved = len(drift) == 1 and not f2
        print(f"  {'ok  ' if moved else 'FAIL'} anchor  a MOVED rule is reported as drift and gates nothing")
        ok &= moved
    print("SELF-TEST:", "PASS" if ok else "FAIL")
    return 0 if ok else 1


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--check", action="store_true", help="exit 1 on any finding (the gate)")
    ap.add_argument("--anchors", action="store_true", help="list the line-coordinate drift as well")
    ap.add_argument("--self-test", action="store_true", help="prove every check fires on a real defect")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if args.self_test:
        return self_test()

    if EVIDENCE not in citation_corpus.FROZEN:
        print(f"⛔ {EVIDENCE} is no longer in citation_corpus.FROZEN — the citation audits have started editing "
              f"the record this audit guards. One of the two is wrong; REFUSING to report either way.")
        return 2
    files = sorted((REPO / EVIDENCE).glob("*.json"))
    findings, drift, stats = scan(files, grammar_rules(GRAMMAR), REPO)
    if stats["files"] == 0 or stats["citations"] == 0:
        print(f"⛔ NOT RUN — {stats['files']} file(s), {stats['citations']} citation(s) under {EVIDENCE}. "
              f"A gate that looked at nothing is not green, it is unmeasured.")
        return 2
    _report(findings, drift, stats, args.anchors)
    return 1 if (findings and args.check) else 0


if __name__ == "__main__":
    sys.exit(main())
