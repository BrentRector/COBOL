"""Lane 1 input builder — one self-contained file per golden-writer agent.

Rows: every inventory row that is CONFORMS-but-untested (verdict CONFORMS, empty test-ref, state GAP) plus every
DOCUMENTED-NON-SUPPORT row still GAP (owes a witness). Grouped by subject FAMILY so one agent sees interlocking
rules together, capped so no file exceeds MAX rows.
"""
import collections
import json
import pathlib
import re
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8")
REPO = pathlib.Path(r"E:\COBOL")
OUT = pathlib.Path(__file__).resolve().parent / "in"
OUT.mkdir(parents=True, exist_ok=True)
MAX = 14
# ⛔ Every slug carries a LANE PREFIX (argv[1], e.g. gl2). Lane 2 (2026-09-25) reused lane 1's bare slugs, and the
# misc-p2 writer drafted golden-lane-1's misc-p2 rows from the repo's adjudication/golden-lane-1/scratch/in/ copy
# instead of its own input: 12 foreign drafts, 0 of its 13 rows. A slug must be unique across every lane ever run.
if len(sys.argv) < 2 or not re.fullmatch(r"[a-z][a-z0-9]*", sys.argv[1]):
    sys.exit("usage: build_lane1_inputs.py <lane-prefix>   (e.g. gl3 — unique per lane; slugs become <prefix>-<family>)")
LANE = sys.argv[1]

cat = {r["id"]: r for r in json.loads((REPO / "docs/rearchitecture/spec-rule-catalog.json").read_text(encoding="utf-8"))["rules"]}
inv = json.loads((REPO / "tests/version-matrix/traceability-inventory.json").read_text(encoding="utf-8"))

conf = [r for r in inv if r["verdict"] == "CONFORMS" and r["state"] == "GAP"]
dns = [r for r in inv if r["verdict"] == "DOCUMENTED-NON-SUPPORT" and r["state"] == "GAP"]
assert all(not r["test-ref"] for r in conf), "a CONFORMS-GAP row with a test-ref is a non-qualifying ref; inspect"

FAMILY = {
    "datetime-format": ["FORMATTED-DATETIME", "FORMATTED-TIME", "FORMATTED-DATE", "FORMATTED-CURRENT-DATE",
                        "TEST-FORMATTED-DATETIME", "COMBINED-DATETIME"],
    "date-integer": ["DATE-OF-INTEGER", "DAY-TO-YYYYDDD", "INTEGER-OF-DATE", "INTEGER-OF-FORMATTED-DATE",
                     "DATE-TO-YYYYMMDD", "INTEGER-OF-DAY"],
    "string": ["FIND-STRING", "CONCAT", "LOWER-CASE", "SUBSTITUTE", "TRIM", "DISPLAY-OF", "NATIONAL-OF",
               "CHAR-NATIONAL", "LENGTH", "BYTE-LENGTH"],
    "math-transcendental": ["LOG10", "LOG", "E", "EXP10", "EXP", "FACTORIAL", "PI", "SQRT", "ACOS", "ASIN", "ATAN",
                            "COS", "SIN", "TAN"],
    "math-integer-algebraic": ["INTEGER", "INTEGER-PART", "FRACTION-PART", "MOD", "REM", "SIGN", "ABS",
                               "LOWEST-ALGEBRAIC", "HIGHEST-ALGEBRAIC", "NUMVAL", "NUMVAL-C", "NUMVAL-F"],
    "statistics": ["MAX", "MIN", "MEAN", "MEDIAN", "MIDRANGE", "ORD-MAX", "ORD-MIN", "RANDOM",
                   "STANDARD-DEVIATION", "VARIANCE"],
    "exception-module": ["EXCEPTION-STATEMENT", "EXCEPTION-LOCATION", "EXCEPTION-LOCATION-N", "EXCEPTION-STATUS",
                         "MODULE-NAME"],
    "io-statements": ["DELETE statement", "CLOSE statement", "ACCEPT statement", "DISPLAY statement"],
    "misc-statements": ["CANCEL statement", "ALLOCATE statement", "CONTINUE statement",
                        "Standard-decimal arithmetic", "Arithmetic expressions"],
}
subj2fam = {}
for fam, names in FAMILY.items():
    for n in names:
        subj2fam[n if "statement" in n or " " in n else f"{n} function"] = fam


def family_of(subject: str) -> str:
    return subj2fam.get(subject, "misc")


groups = collections.defaultdict(list)
for r in conf:
    groups[family_of(r["subject"])].append(r)
groups["dns-witness"] = dns
unknown = [r["subject"] for r in conf if family_of(r["subject"]) == "misc"]
if unknown:
    print("⚠ unmapped subjects:", sorted(set(unknown)))


def split(rows, cap):
    """Split at SUBJECT boundaries into near-even parts, never mid-subject."""
    if len(rows) <= cap:
        return [rows]
    by = collections.OrderedDict()
    for r in sorted(rows, key=lambda x: (x["subject"], x["section"], x["ordinal"])):
        by.setdefault(r["subject"], []).append(r)
    n = -(-len(rows) // cap)
    target = -(-len(rows) // n)
    parts, cur = [], []
    for s, rs in by.items():
        if cur and len(cur) + len(rs) > target:
            parts.append(cur); cur = []
        cur += rs
    if cur:
        parts.append(cur)
    return parts


corpus = list((REPO / "tests/conformance").rglob("*.cob"))
corpus_text = {p: p.read_text(encoding="utf-8", errors="replace").upper() for p in corpus}


def goldens_mentioning(subject: str):
    word = subject.replace(" function", "").replace(" statement", "").upper()
    if word in ("E", "PI", "LOG", "MOD", "REM", "MAX", "MIN", "SIN", "COS", "TAN", "EXP", "ABS", "SIGN"):
        rx = re.compile(r"\bFUNCTION\s+" + re.escape(word) + r"\b")
    elif "statement" in subject:
        rx = re.compile(r"^\s+" + re.escape(word) + r"\b", re.M)
    elif " " in word:
        rx = re.compile(re.escape(word.split()[0]))
    else:
        rx = re.compile(r"\b" + re.escape(word) + r"\b")
    hits = [str(p.relative_to(REPO)).replace("\\", "/") for p, t in corpus_text.items() if rx.search(t)]
    return sorted(hits)[:25]


def row_payload(r):
    c = cat[r["rule-id"]]
    return {
        "rule-id": r["rule-id"], "section": r["section"], "kind": r["kind"], "ordinal": r["ordinal"],
        "subject": r["subject"], "editions": r["editions"], "verdict": r["verdict"],
        "code-location": r["code-location"], "adjudicator-notes": r["notes"],
        "rule-text": c.get("text", ""),
    }


CONVENTIONS = {
    "positive-golden": "tests/conformance/<edition>/<name>.cob + <name>.out; the directory selects --std. First line a "
                       "comment `*> ISO §<clause> <rule> — <what is exercised>`. Unique PROGRAM-ID per program (a "
                       "stale same-named assembly is served otherwise). Deterministic output only. Do NOT edit any "
                       "manifest.json — return the entry; the director registers it.",
    "negative-golden": "tests/conformance/negative/<name>.cob + <name>.err (the expected diagnostic substring, e.g. "
                       "COBOLNET1560). FIRST line of the .cob MUST be `*> reject-at: <year> [<year> ...]`. Do NOT edit "
                       "the negative manifest — return the entry.",
    "test-ref-forms": "conformance:<edition>/<case> (positive, spec-derived) · conformance:negative/<case> · "
                      "unit:<Class>.<Method> · conformance-test:<Class>.<Method>. nist:/characterization: and any "
                      "*_MatchesLegacy method are NOT spec-derived and cannot close a row.",
    "compiler": "src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.exe <file.cob> --std <85|2002|2014|2023> --run  (run in a "
                "scratch dir; never dotnet build/test — the tree is shared and a guard refuses builds while a fleet "
                "is live).",
    "citation-check": "python scripts/spec/cite.py --check <clause> \"<quoted text>\"  — every § you cite, no exceptions.",
    "record-shape": "{'rule-id','verdict','code-location','test-ref','editions','notes'} — strings only; editions as "
                    "\"85,2002,2014,2023\".",
}

index = []
for fam, rows in sorted(groups.items()):
    # The witness group is ONE task (two owning notes, one licence) — never split it by subject.
    parts = [rows] if fam == "dns-witness" else split(rows, MAX)
    for i, part in enumerate(parts):
        slug = f"{LANE}-{fam}" if len(parts) == 1 else f"{LANE}-{fam}-p{i + 1}"
        subjects = sorted({r["subject"] for r in part})
        payload = {
            "slug": slug, "family": fam, "row-count": len(part), "subjects": subjects,
            "existing-goldens-by-subject": {s: goldens_mentioning(s) for s in subjects},
            "conventions": CONVENTIONS,
            "rows": [row_payload(r) for r in sorted(part, key=lambda x: (x["subject"], x["section"], x["ordinal"]))],
        }
        if fam == "dns-witness":
            payload["owning-notes"] = ["kb/Work/PB260.md", "kb/Work/PB261.md"]
            payload["posture"] = ("A DOCUMENTED-NON-SUPPORT row closes ONLY on a test proving we DIAGNOSE the declined "
                                  "construct (or that the documented behaviour is what actually happens). Read the two "
                                  "owning notes first; they carry the licence and the source shapes to measure.")
        (OUT / f"in-{slug}.json").write_text(json.dumps(payload, indent=1, ensure_ascii=False), encoding="utf-8")
        index.append((slug, len(part), subjects))

print(f"{len(conf)} CONFORMS-untested + {len(dns)} DNS-untested = {len(conf) + len(dns)} rows → {len(index)} files")
for slug, n, subjects in index:
    print(f"  {slug:32} {n:3}  {', '.join(s.replace(' function', '') for s in subjects)[:110]}")
assert sum(n for _, n, _ in index) == len(conf) + len(dns), "PARTITION INVARIANT: a row was dropped"
