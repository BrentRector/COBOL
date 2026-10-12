"""Lane 3 merge — turn one batch's adjudications + refutations on disk into a record_verdicts batch and a findings digest.

    python merge_batch.py <out-dir> <batch-label>

Reads out-<slug>.json (adjudicator results; falls back to adjudicate-<slug>.jsonl checkpoints), refute-<slug>.jsonl
(refuter checkpoints: one verdict per line). A CONFORMS record whose file has NO refuter checkpoint is DOWNGRADED to
'unrefuted' and EXCLUDED from the batch — the doctrine is that only a refuted closing verdict is recorded. Refuted rows
take the refuter's corrected verdict with the reason appended to notes. Defective verdicts (PARTIAL / NOT-IMPLEMENTED /
DIVERGES) need a register note in `notes` before record_verdicts accepts them — the digest lists the mechanisms so the
note-writer can allocate ids and the notes field is filled in a second pass (this script leaves a `@@NOTE@@` marker).
"""
import json, pathlib, sys, collections, re
sys.stdout.reconfigure(encoding="utf-8")
out = pathlib.Path(sys.argv[1]); label = sys.argv[2]

#: A refuter telling the registrar that the row is NOT this lane's to record — the inventory already
#: carries an OWNER verdict (DOCUMENTED-NON-SUPPORT, D13) and re-recording it would OVERWRITE that
#: verdict with an adjudicator's. Lane-3 batch b3 produced three of these (SR-13.18.63.3-36/-38/-39,
#: all "DOCUMENTED-NON-SUPPORT (unchanged -- the existing inventory row stands; do NOT record
#: CONFORMS)"): the vocabulary scan below would have taken them for PARTIAL and DOWNGRADED a closed
#: owner row. A refuter that names an owner verdict, or that says the existing row stands, EXCLUDES.
OWNER_ROW = re.compile(
    r"(?:DOCUMENTED[- ]NON[- ]SUPPORT"
    r"|\bexisting inventory row stands\b"
    r"|\bdo\s+NOT\s+record\b)", re.I)

#: A refuter telling the registrar to drop the cited evidence. Either order, within one sentence.
TESTREF_CLEARED = re.compile(
    r"(?:test-?ref[^.\n]{0,120}\b(?:WITHDRAWN|CLEARED?|EMPTY)\b"
    r"|\b(?:WITHDRAWN|CLEARED?|EMPTY)\b[^.\n]{0,120}test-?ref)", re.I)

#: ⛔ AN OWNER VERDICT IS NOT AN AGENT'S TO REPLACE — IN EITHER DIRECTION. D13 reserves
#: DOCUMENTED-NON-SUPPORT to the owner, and lane-3 b3's refuters caught three rows where an adjudicator
#: would have overwritten one with CONFORMS. The SAME batch then carried a FOURTH, which no refuter saw
#: because that row was never adjudicated CONFORMS: GR-13.18.63.4-24, DOCUMENTED-NON-SUPPORT → DIVERGES.
#: Downgrading looks harmless — it does not fake a closure — but it still moves the burn-down denominator
#: on an agent's say-so and detaches the row from the notes that own the declined modules (PB260/PB261/
#: PB281-PB283). So the rule is structural, read from the inventory rather than hand-listed: a record whose
#: row ALREADY carries an owner verdict is EXCLUDED and raised as an owner question. The measurement is not
#: lost — it lands as a kb/Work note that claims the row.
OWNER_VERDICTS = {"DOCUMENTED-NON-SUPPORT"}
_inv = pathlib.Path("tests/version-matrix/traceability-inventory.json")
OWNED_ROWS = ({r["rule-id"] for r in json.loads(_inv.read_text(encoding="utf-8"))
               if r.get("verdict") in OWNER_VERDICTS} if _inv.exists() else set())

records, digest, unrefuted, skipped = [], collections.OrderedDict(), [], []
for f in sorted(out.glob("out-*.json")) + sorted(out.glob("adjudicate-*.jsonl")):
    slug = f.stem.split("-", 1)[1]
    if any(r["_slug"] == slug for r in records):
        continue  # out-file already consumed; the .jsonl is its checkpoint twin
    if f.suffix == ".json":
        data = json.loads(f.read_text(encoding="utf-8"))
        recs, findings = data.get("records", []), data.get("findings", [])
    else:
        recs = [json.loads(l) for l in f.read_text(encoding="utf-8").splitlines() if l.strip()]
        ff = out / f"findings-{slug}.json"
        findings = json.loads(ff.read_text(encoding="utf-8")) if ff.exists() else []
    ref = out / f"refute-{slug}.jsonl"
    refuted = {}
    if ref.exists():
        for l in ref.read_text(encoding="utf-8").splitlines():
            if l.strip():
                v = json.loads(l); refuted[v["rule-id"]] = v
    for r in recs:
        r = dict(r); r["_slug"] = slug
        rid = r["rule-id"]
        if r["verdict"] == "CONFORMS":
            if rid in refuted:
                v = refuted[rid]
                if not v["upheld"]:
                    # A refuter's corrected_verdict is free text around a vocabulary word ("CONFORMS, but with the
                    # test-ref WITHDRAWN …", "CONFORMS (test-ref amended: add conformance:2002/x …)"). Take the word;
                    # apply the two instruction shapes the refuters actually write; anything else is PARTIAL.
                    cv = v.get("corrected_verdict") or ""
                    if OWNER_ROW.search(cv):
                        skipped.append((rid, "owner-verdict-stands")); continue
                    word = next((w for w in ("CONFORMS", "PARTIAL", "DIVERGES", "NOT-IMPLEMENTED", "NEEDS-OWNER-DECISION") if cv.upper().startswith(w)), "PARTIAL")
                    r["verdict"] = word
                    # ⛔ THE CLEARING INSTRUCTION IS NOT ALWAYS IN `corrected_verdict`, AND IT IS NOT ALWAYS
                    # THE WORD "WITHDRAWN". Lane-3 batch b2 produced both misses in one run, each of which
                    # would have CLOSED a row on evidence the refuter had just shown cannot pin the rule:
                    #   GR-14.9.37.4-6  corrected_verdict "CONFORMS (test-ref must be CLEARED …)"
                    #   SR-14.9.44.3-5  corrected_verdict bare "CONFORMS"; the instruction ("record … as
                    #                   CONFORMS with an EMPTY test-ref") was written only in `why`.
                    # So scan BOTH fields, and take any clearing verb adjacent to a test-ref mention.
                    if TESTREF_CLEARED.search(cv) or TESTREF_CLEARED.search(v.get("why") or ""):
                        r["test-ref"] = ""
                    m = re.search(r"amended:\s*add\s+([A-Za-z0-9_:./-]+)", cv)
                    if m:
                        r["test-ref"] = "; ".join(x for x in [r.get("test-ref", ""), m.group(1)] if x)
                    r["notes"] = (r.get("notes", "") + " || REFUTED: " + cv + " — " + v.get("why", "")).strip()
            else:
                unrefuted.append(rid); continue
        if r["verdict"] == "NEEDS-OWNER-DECISION":
            skipped.append((rid, "owner")); continue
        if rid in OWNED_ROWS and r["verdict"] not in OWNER_VERDICTS:
            skipped.append((rid, "owner-verdict-stands")); continue
        if r["verdict"] in ("PARTIAL", "NOT-IMPLEMENTED", "DIVERGES") and "@@NOTE@@" not in r.get("notes", ""):
            r["notes"] = "@@NOTE@@ " + r.get("notes", "")
        records.append(r)
    for fd in findings:
        digest[(slug, fd["mechanism"])] = fd

batch = {"batch": label, "records": [{k: v for k, v in r.items() if not k.startswith("_")} for r in records]}
(out / f"batch-{label}.json").write_text(json.dumps(batch, indent=1, ensure_ascii=False), encoding="utf-8", newline="\n")
(out / f"findings-{label}.json").write_text(json.dumps([{"slug": s, **fd} for (s, _), fd in digest.items()], indent=1, ensure_ascii=False), encoding="utf-8", newline="\n")
c = collections.Counter(r["verdict"] for r in records)
print(f"{label}: {len(records)} records -> batch-{label}.json  {dict(c)}")
print(f"  CONFORMS with test-ref (would close): {sum(1 for r in records if r['verdict']=='CONFORMS' and r.get('test-ref'))}")
print(f"  unrefuted CONFORMS excluded: {len(unrefuted)}  owner-decision excluded: {len(skipped)}")
print(f"  findings (mechanisms): {len(digest)} -> findings-{label}.json")
print(f"  defective records awaiting a note id: {sum(1 for r in records if '@@NOTE@@' in r.get('notes',''))}")
