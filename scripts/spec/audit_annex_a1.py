#!/usr/bin/env python3
"""Audit `docs/CONFORMANCE.md` §7 against Annex A.1 — the implementor-defined documentation register.

    python scripts/spec/audit_annex_a1.py             # report coverage + findings
    python scripts/spec/audit_annex_a1.py --check     # exit non-zero on any finding (gate form)
    python scripts/spec/audit_annex_a1.py --json      # + one machine-readable `JSON {...}` line
    python scripts/spec/audit_annex_a1.py --self-test # prove every check can fail, for its own reason

⛔ WHY THIS EXISTS. ISO §4.2.5 requires the implementor to SPECIFY every A.1 element identified as *required*
and to DOCUMENT every element identified as *requiring user documentation*; owner decision D13 makes those part
of the definition of v1.0. `docs/CONFORMANCE.md` §7 is that register. Its rows are keyed by A.1 ITEM NUMBER,
and nothing checked those numbers — so an entry could sit under the wrong obligation indefinitely, which
CONFORMANCE.md §7's own preamble calls the worse failure: "an undocumented determination and a wrongly-
documented one are both non-conformance, and the second is worse."

⚙ THE KEY IS NOW THE INVENTORY RULE-ID `DOC-A.1-<n>`, not the bare number, because the P14 inventory row for
item *n* carries `docs/CONFORMANCE.md#DOC-A.1-<n>` as a code-location COMPUTED from its own rule-id
(`inventory-schema.json` → `kinds.DOC.anchor-template`). Three checks follow from that and are here rather than
in the C# gate, because they are about THIS document: every key parses as a rule-id of a real A.1 item; the set
of `DOC-A.1-<n>` TOKENS in the whole file equals the set of §7 row keys, so a prose mention can never masquerade
as a filed determination; and each row's `Pinned by` cell AGREES with the inventory row's own spec-derived
`test-ref`, so a determination and its evidence are one artifact instead of two that drift apart.

⭐ IT FOUND ONE ON ITS FIRST RUN. The determination for the maximum fractional-seconds precision (§15.3.3.2)
was filed as **item 87**; item 87 is *FORMATTED-CURRENT-DATE (accuracy of returned time)* — a different, still
undocumented obligation. The determination belongs to **item 202**, "Time formats and corresponding function
values (maximum precision not less than nine fractional digits)". That is the INHERITED-CITATION failure mode
CLAUDE.md rule 1 names: the quoted text was genuinely in the standard and the NUMBER was never re-derived.

The authority is `docs/rearchitecture/spec-rule-catalog.json`'s DOC rows, which `extract_rule_catalog.py`
parses straight out of Annex A.1 — so this script adds no second copy of the register (feedback_one_rule_one_place).
"""
from __future__ import annotations

import collections
import json
import pathlib
import re
import sys

# ⛔ THE §7 TABLE PARSER IS NO LONGER WRITTEN HERE. It was, and then three readers needed it — this audit, which
# CHECKS the rows; `Schema.anchor_obliged`, which asks whether an item has a determination at all; and
# `DerivedSelector`'s `determination-prefix` arm, which reads what the determination SAYS. A markdown table read
# three ways is `feedback_one_rule_one_place` waiting to happen, so it moved down into the module every consumer
# already imports, and the two names this file used keep working unchanged.
from inventory_schema import derivation_rows, load_schema
from inventory_schema import register_cells as cells_of
from inventory_schema import section7_rows

ROOT = pathlib.Path(__file__).resolve().parents[2]
CATALOG = ROOT / 'docs' / 'rearchitecture' / 'spec-rule-catalog.json'
CONFORMANCE = ROOT / 'docs' / 'CONFORMANCE.md'

#: Words that must appear in a §7 row for it to be judged as naming the same element as the catalog. Kept
#: deliberately weak — this checks that the NUMBER and the ELEMENT are not wildly divorced, which is the defect
#: class observed; it is not a prose-similarity metric, and pretending otherwise would produce noise nobody reads.
STOPWORDS = {'the', 'of', 'a', 'an', 'and', 'or', 'for', 'in', 'to', 'is', 'not', 'when', 'clause',
             'statement', 'item', 'data', 'this', 'that', 'with', 'by', 'be', 'may', 'shall', 'its'}


def words(text: str) -> set[str]:
    return {w for w in re.findall(r'[a-z0-9-]+', text.lower()) if w not in STOPWORDS and len(w) > 2}


def clauses(text: str) -> set[str]:
    """Every dotted clause number in a string — `§15.3.3.2`, `(15.4.1, Numeric …)`, `13.18.60.4 GR11`."""
    return set(re.findall(r'\b(\d+(?:\.\d+)+)\b', text))


def corresponds(element: str, entry: dict, register: dict[int, dict] | None = None) -> bool:
    """Does a §7 row name the A.1 item it is filed under?

    ⚠ THE CLAUSE IS THE IDENTITY, not the prose. The first draft of this check compared WORDS only, and
    promptly rejected a correct row ("Evaluation of an exact-family intrinsic whose ARGUMENT is
    floating-point") against A.1-92's paraphrase ("Function returned values (characteristics,
    representation, …)") — the register's own preamble says its headers are a *paraphrase* of the normative
    detail, so demanding lexical overlap with a paraphrase is demanding the wrong thing. A row that cites a
    clause A.1 cross-references for that item is naming that item, whatever words it chose.

    ⛔ AND THE OVERLAP IS RELATIVE, not absolute — the lesson of DOC-A.1-209. That row carried the **USAGE
    BIT** determination under item 209 (**USAGE DISPLAY**) and this check printed `ok` for it from the day it
    landed, because a bare "≥ 2 shared words" threshold cannot separate siblings inside one A.1 family: the
    row shared `usage` + `representation` with item 209 and that was the whole test. Words like those are
    near-universal in A.1's own headers (`character` 26, `value` 23, `representation` 16, `usage` 14 subjects
    each), so an absolute threshold measures vocabulary, not identity — and a green check that never looked
    at what it was checking is what let a misfiled determination ship, and then propagated the wrong item
    number into a landed golden's comment.

    So the row must fit the item it is filed under **at least as well as it fits every other A.1 item**. No
    tuned constant: the misfiling scored 2 against item 209 and 3 against each of 205/208/210/211/215, which
    is the finding. Measured 2026-09-03 over all 47 §7 rows — every correctly filed row is a best (or tied)
    match for its own item, so the rule adds no noise, and it generalizes: any future determination whose
    prose fits some other A.1 element better than its own number is flagged without an edit here.
    """
    if clauses(element) & clauses(entry['text']):
        return True
    mine = len(words(element) & words(entry['subject']))
    if mine < 2:
        return False
    if register is None:
        return True
    return not any(len(words(element) & words(e['subject'])) > mine for e in register.values())


def load_register() -> dict[int, dict]:
    catalog = json.loads(CATALOG.read_text(encoding='utf-8'))
    return {r['ordinal']: r for r in catalog['rules'] if r.get('kind') == 'DOC'}


#: A §7 row key: the inventory rule-id of the A.1 item the row discharges.
ITEM_KEY_RX = re.compile(r'^DOC-A\.1-(\d+)$')
#: The same token ANYWHERE in the file. The set of these must equal the set of row keys — a determination is
#: filed by being a ROW, and a passing prose mention that resolved as an anchor would be a silent false claim.
TOKEN_RX = re.compile(r'\bDOC-A\.1-\d+\b')


def unreachable_items() -> dict[int, str]:
    """A.1 items that CANNOT ARISE, because the feature they document is one we decline.

    ⛔ THE STANDARD SAYS SO ITSELF, in Annex A.1's own preamble — this is not an inference from §4.2.7:

        "Required: The element shall be provided by the implementor. When the element is part of a feature
         that is optional or processor-dependent, the item is not required if the optional or
         processor-dependent feature is not implemented."   (cite.py --check A.1 → OK)

    §4.2.5 ¶1 and A.4.1's closing line say the same from the other side ("The provisions of this clause apply
    to required normative elements … and to optional language elements for which an implementor claims
    support"). So DOC-A.1-84's own sentence "This item is required" is answered head-on: it is required only
    while the FORMAT clause is claimed, and it is not.

    ⛔ AND THE LIST IS NOT WRITTEN HERE. It is DERIVED from the `derived-verdicts` selectors in
    `inventory-schema.json` — the same predicate the batch generator and the C# drift test read. The failure
    this prevents was measured on 2026-09-02: the A.4 derived landing stamps DOC-A.1-84, -85, -173 and -86
    DOCUMENTED-NON-SUPPORT in the traceability inventory, while this script's REMAINING counter — which
    CONFORMANCE.md §7 names as the count's owner — went on counting them open. One item, two registers, two
    answers, which is exactly the harm CLAUDE.md rule 8 exists to stop. Deriving it means the next declined
    module is handled with no edit here at all.

    ⛔ AND A DECLINED ITEM §7 DOES DOCUMENT IS NOT WITHDRAWN (PB280 Q1, 2026-09-02). The verdict alone stopped
    being the discriminator the moment DOCUMENTED-NON-SUPPORT acquired its second DOC-row ground: an item that
    is OPTIONAL and NOT PROVIDED carries a §7 determination *stating the non-provision*, which is a discharged
    obligation, not a withdrawn one. Counting it here would remove it from the denominator while it stayed in
    the numerator — REMAINING would fall by two for no work done — and would print "⊘ CANNOT ARISE" about an
    element this register documents in full. The presence of the row is the test, and it is the same fact
    `Schema.anchor_obliged` keys on, read through the same parser.
    """
    from inventory_schema import load_catalog, load_schema  # local: keeps this script standalone-runnable
    schema = load_schema()
    rules = load_catalog()
    determined = schema.register_items()
    out: dict[int, str] = {}
    for name, sel in schema.derived.items():
        if sel.verdict != 'DOCUMENTED-NON-SUPPORT':
            continue
        for rid in sel.select(rules):
            if rid.startswith('DOC-A.1-') and rid not in determined:
                out[int(rid.rsplit('-', 1)[1])] = name
    return out


GLOSS_RX = re.compile(r'`\s*\([^)]*\)\s*$')


def pins_of(cell: str) -> list[str]:
    """The test-refs a `Pinned by` cell names — backticks are legibility, not content."""
    if cell.strip() in ('', '—', '-'):
        return []
    # A ref may carry a trailing gloss OUTSIDE its backticks (`conformance:cases/14.9.8-COMPUTE/x` (out-of-range half)); the gloss
    # is prose for the reader, not part of the ref, so it is dropped before the ref is compared or resolved.
    refs = [GLOSS_RX.sub('`', p.strip()).strip('`').strip() for p in cell.split(';') if p.strip()]
    return [r for r in refs if r]


SELF_TEST_HEADER = ('## 7. Annex A.1\n\n| A.1 item | Element | Our determination | Pinned by |\n'
                    '|---|---|---|---|\n')

#: A fabricated §8 DERIVATION register carrying one row keyed for A.1 item 2 — the second legitimate home a
#: `DOC-A.1-<n>` token acquired on 2026-09-03 (kb/Work PB386). The heading is read from the SCHEMA so renaming
#: the section cannot leave this fixture silently testing a section that no longer exists.
DERIVATION_SELF_TEST_ROWS = (
    '\n' + load_schema().derivation.heading + '\n\n'
    '| Rule | Arm | Names | The derivation | Signed |\n|---|---|---|---|---|\n'
    '| DRV-DOC-A.1-2 | undefined-A.2 | A.2 item 4 | a fabricated derivation | owner: 2026-09-03 |\n')


def self_test() -> int:
    """Prove every check can FAIL, and for its OWN reason (feedback_green_gates_arent_evidence).

    A gate that has never been shown to fail is not evidence — and one that fails for the wrong reason is
    worse, because it reads as working. Each case below breaks exactly one rule.
    """
    register = load_register()
    good = '| DOC-A.1-2 | ACCEPT statement device used when FROM is unspecified, §14.9.1 GR5 | det | — |'
    cases = [
        ('control: a correct row passes', good, None),
        ('a wrong item number is caught',
         '| DOC-A.1-87 | **Maximum digit positions in the decimal fraction of the seconds subfield**, '
         '§15.3.3.2 | det | — |',
         'do not correspond'),
        # ⛔ THE MISFILING THAT SHIPPED, replayed verbatim (landed under item 209 until 2026-09-03). It shares
        # TWO words with its own item — `usage`, `representation` — so the old absolute "≥ 2 shared words"
        # threshold printed `ok` for it. It shares THREE with items 205/208/210/211/215, and that is the
        # finding: a row must fit its own item at least as well as it fits any other.
        ('a SIBLING-FAMILY misfiling (2 shared words, another item fits better) is caught',
         '| DOC-A.1-209 | **USAGE BIT clause — alignment and representation of data**, §13.18.60.4 GR5 + '
         '§8.5.1.6.3 | det | — |',
         'do not correspond'),
        ('an item number outside A.1 is caught',
         '| DOC-A.1-999 | Something, §14.9.1 | det | — |', 'not an A.1 item number'),
        ('an unnumbered row is caught',
         '| — (§15.4.1) | Something voluntary | det | — |', 'carry no A.1 item number'),
        # ⚙ THE KEY FORM ITSELF. A bare number was the key until 2026-09-02 and is no longer a row key at all:
        # it cannot be an anchor (`#7` matched the digit 7 anywhere in the file), so a row still spelling it
        # discharges nothing and must be reported rather than silently skipped.
        ('the OLD bare-number key form is caught',
         '| 2 | ACCEPT statement device used when FROM is unspecified, §14.9.1 GR5 | det | — |',
         'carry no A.1 item number'),
        # ⛔ THE TOKEN SET: a `DOC-A.1-<n>` token anywhere but a row key would resolve as a filed determination
        # under the C# gate's word search over the whole file.
        ('a DOC-A.1 token for an ITEM WITH NO ROW is caught',
         good + '\n\nSee DOC-A.1-19 for the CANCEL case.', 'outside a row key'),
        # ⚠ THE CASE THE FIRST IMPLEMENTATION MISSED. A set difference stayed green here, because the row key
        # already put this token in the set; only a COUNT sees a second occurrence — and a second occurrence is
        # precisely what survives the row it shadows being deleted.
        ('a DOC-A.1 token DUPLICATING an item that does have a row is caught',
         good + '\n\nSee DOC-A.1-2 for the ACCEPT case.', 'appears 2×'),
        # ⚙ AND THE WIDENING (kb/Work PB386, 2026-09-03). §8 keys an owner-signed DERIVATION by `DRV-` plus the
        # inventory rule-id, so a `DOC-A.1-<n>` token now has TWO legitimate homes. The pair below is what
        # proves the widening did not blunt the check: the same §8 row that must be ACCEPTED is present in both
        # cases, and the prose mention beside it is still caught.
        ('a §8 DERIVATION row key is a legitimate SECOND occurrence of its item token',
         good + '\n' + DERIVATION_SELF_TEST_ROWS, None),
        ('…and a PROSE mention beside that §8 row is STILL caught',
         good + '\n' + DERIVATION_SELF_TEST_ROWS + '\nSee DOC-A.1-2 for the ACCEPT case.', 'appears 3×'),
    ]
    rc = 0
    print('=== audit_annex_a1 --self-test ===')
    for name, row, want in cases:
        text = SELF_TEST_HEADER + row + '\n'
        findings = evaluate(register, text)[0]
        if want is None:
            ok = not findings
        else:
            ok = any(want in f for f in findings)
        if ok:
            print(f'  ok: {name}')
        else:
            print(f'  SELF-TEST FAILED: {name} -> {findings}')
            rc = 1

    # ── the `Pinned by` ↔ inventory agreement check, driven on a fabricated pair ──────────────────────
    from inventory_schema import load_schema
    schema = load_schema()
    # ⚠ Every fabricated row carries a `kind` AND a `verdict`, because `check_pins` now asks
    # `schema.anchor_obliged` — a row with neither is exempt by accident and every case below would pass
    # vacuously. That is not hypothetical: the first version of these fixtures omitted both, and when the
    # exemption was added (kb/Work PB315) two cases silently stopped testing anything until the fixtures were
    # made real rows.
    def doc_row(test_ref: str, verdict: str = 'CONFORMS') -> dict:
        return {'rule-id': 'DOC-A.1-87', 'kind': 'DOC', 'verdict': verdict, 'test-ref': test_ref}

    pin_cases = [
        ('control: the row names the test the inventory closes on',
         {87: ['unit:CobolDateWindowingTests.NowFunctions_OnePinnedClock_OneInstant']},
         [doc_row('unit:CobolDateWindowingTests.NowFunctions_OnePinnedClock_OneInstant')], None),
        # ⛔ THE A11 SHAPE ONE LAYER DOWN — the evidence half. Item 171's test closing item 87 is exactly what
        # the first back-fill did, and it is invisible to every check that asks only whether the method exists.
        ('a test the row does not name is caught',
         {87: ['unit:CobolDateWindowingTests.NowFunctions_OnePinnedClock_OneInstant']},
         [doc_row('unit:CobolDateWindowingTests.SecondsPastMidnight_PinnedClock_ExactTicks')],
         'does not name in'),
        ('an inventory row closing with no §7 row at all is caught',
         {}, [doc_row('conformance:cases/14.9.8-COMPUTE/whatever')], 'no row for item 87'),
        ('a NON-spec-derived ref is not required to be named (it can never close a row)',
         {87: []}, [doc_row('nist:IF128A')], None),
        ('a row with no verdict evidence at all is silent',
         {87: []}, [doc_row('')], None),
        # ⛔ THE WITHDRAWN-ITEM ARM (kb/Work PB315). A DOCUMENTED-NON-SUPPORT DOC row whose module withdrew the
        # item has NO §7 row — demanding one reported three correctly-closed rows as findings. The control above
        # proves the check still fires for a claiming row.
        ('a DECLINED item closing on its witness needs no §7 row',
         {}, [doc_row('conformance:wiseowl/cases/13.4.5-File-description/a48-format-clause-declined', 'DOCUMENTED-NON-SUPPORT')], None),
        # ⛔ AND ITS TWIN, WHICH IS WHY THE SKIP IS KEYED ON THE REGISTER AND NOT ON THE VERDICT (PB280 Q1). An
        # OPTIONAL element §7 records as "Not provided." carries the SAME verdict and a REAL determination, so
        # its witness is held to that row's `Pinned by` like any other. Keyed on the verdict, this case passes
        # vacuously — the exact shape of a gate that reads as working while inspecting nothing.
        ('a DECLINED item that §7 DOES document still owes the agreement',
         {87: ['unit:Cls.TheWitness']},
         [doc_row('unit:Cls.SomeOtherTest', 'DOCUMENTED-NON-SUPPORT')], 'does not name in'),
    ]
    for name, pinned, inv, want in pin_cases:
        got = check_pins(pinned, inv, schema)
        ok = (not got) if want is None else any(want in f for f in got)
        if ok:
            print(f'  ok: {name}')
        else:
            print(f'  SELF-TEST FAILED: {name} -> {got}')
            rc = 1
    # review R3-1 (PB3018): the `Pinned by` resolution check, proven able to fail for each of its reasons.
    pin_resolution_cases = [
        ('a pin naming a real xUnit method resolves',
         {70: ['unit:CarrierLandingFormTests.Store_PastTheCarrier_KeepsTheLowOrderDigits']}, None),
        ('a pin naming a method the class no longer declares is caught',
         {70: ['unit:CarrierLandingFormTests.ToScaled_PastTheCarrier_RaisesChecked_SaturatesIntermediate_X']},
         'declares no method'),
        ('a bare class pin (the whole family) resolves', {70: ['unit:CarrierLandingFormTests']}, None),
        # Each gloss case is ONE real ref plus a gloss: with the gloss kept in the ref (the origin/main pins_of)
        # the ref names no file, so the case fails and nothing else can make it pass.
        ('a gloss outside the backticks after an xUnit class pin is not part of the ref',
         {70: pins_of('`unit:CarrierLandingFormTests` (the family)')}, None),
        ('a gloss outside the backticks after a golden pin is not part of the ref',
         {70: pins_of('`conformance:cases/_unassigned/pb3017_sdidi_divide_remainder_beyond_the_int128_carrier` (the half)')},
         None),
        ('a pin naming a missing class is caught', {70: ['unit:NoSuchTestClass.Whatever']}, 'no NoSuchTestClass.cs'),
        ('a pin naming a missing golden is caught', {70: ['conformance:cases/14.9.8-COMPUTE/no_such_case_at_all']}, 'does not exist'),
        ('a pin of an unknown form is caught', {70: ['vibes:it-looked-right']}, 'not a test-ref form'),
    ]
    for name, pins, want in pin_resolution_cases:
        got = check_pin_resolution(pins, schema, ROOT)
        ok = (not got) if want is None else any(want in f for f in got)
        if ok:
            print(f'  ok: {name}')
        else:
            print(f'  SELF-TEST FAILED: {name} -> {got}')
            rc = 1
    # kb/Work R23 — the source-citation sweep, proven able to fail for BOTH of its reasons.
    import tempfile
    with tempfile.TemporaryDirectory() as td:
        fake = pathlib.Path(td) / 'Cobol.Net.Fake' / 'X.cs'
        fake.parent.mkdir(parents=True)
        fake.write_text('// precision (implementor item 999)\n// and the real one (implementor item 2)\n',
                        encoding='utf-8', newline="\n")
        sweep_cases = [
            ('a citation of a nonexistent A.1 item is caught',
             sweep_source_citations(register, {2: 'x'}, pathlib.Path(td)), 'no item 999'),
            ('a citation §7 does not discharge is caught',
             sweep_source_citations(register, {}, pathlib.Path(td)), 'undischarged'),
            ('a discharged citation passes',
             [f for f in sweep_source_citations(register, {2: 'x', 999: 'y'}, pathlib.Path(td))
              if 'item 2' in f], None),
        ]
        for name, got, want in sweep_cases:
            ok = (not got) if want is None else any(want in f for f in got)
            if ok:
                print(f'  ok: {name}')
            else:
                print(f'  SELF-TEST FAILED: {name} -> {got}')
                rc = 1
    print('=== audit_annex_a1 --self-test: ' + ('ALL GREEN (every check proven able to fail)' if not rc else 'FAILED') + ' ===')
    return rc


def evaluate(register: dict[int, dict], text: str) -> tuple[list[str], dict[int, str], list[str], dict[int, list[str]]]:
    """The checks, over a §7 body — separated from reporting so --self-test can drive them."""
    findings: list[str] = []
    numbered: dict[int, str] = {}
    unnumbered: list[str] = []
    pinned: dict[int, list[str]] = {}
    keys: collections.Counter[str] = collections.Counter()
    for key, element, _determination, pins in section7_rows(text):
        if (m := ITEM_KEY_RX.match(key)) is not None:
            n = int(m.group(1))
            keys[key] += 1
            if n not in register:
                findings.append(f'§7 row "{key}" is not an A.1 item number (A.1 has {len(register)} items)')
                continue
            numbered[n] = element
            pinned.setdefault(n, []).extend(pins_of(pins))
            if not corresponds(element, register[n], register):
                findings.append(
                    f'§7 item {n} says "{element[:70]}" but A.1-{n} is "{register[n]["subject"][:70]}" — the '
                    f'NUMBER and the ELEMENT do not correspond, and neither do their clauses')
        else:
            unnumbered.append(element)
    if unnumbered:
        findings.append(f'{len(unnumbered)} §7 row(s) carry no A.1 item number, so no obligation is discharged '
                        f'by them and nothing can check them')

    # ⛔ THE TOKEN COUNT — not the token SET. `DOC-A.1-<n>` is the ANCHOR an inventory row computes and the C#
    # gate resolves by a word search over this WHOLE file, so a stray occurrence outside a §7 row key lets a
    # passing prose mention satisfy a traceability claim. A determination is filed by BEING A ROW, so the number
    # of occurrences of each token equals the number of §7 rows keyed by it (items 56 and 92 legitimately have
    # two rows each, and therefore two occurrences each).
    #
    # ⚠ IT WAS WRITTEN AS A SET DIFFERENCE FIRST, AND THAT VERSION HAD A HOLE THIS FOUND: driven against the
    # real file with `See DOC-A.1-50 for the DELETE FILE case.` inserted as prose, it stayed GREEN, because
    # item 50's own row key put the same token in the set. A set cannot see a DUPLICATE, and the duplicate is
    # exactly the shape that survives the row it shadows being deleted.
    #
    # ⚙ AND IT HAD TO LEARN THE SECOND REGISTER (kb/Work PB386, 2026-09-03). §8 keys an owner-signed DERIVATION
    # by the inventory rule-id it closes, so `DRV-DOC-A.1-19` is a second, LEGITIMATE occurrence of the item-19
    # token. The invariant is therefore stated over BOTH registers — a row key here or a row key there — which
    # keeps it exactly as sharp against the thing it was written for: a PROSE mention, which is a key of neither.
    if (deriv := load_schema().derivation) is not None:
        for row in derivation_rows(text, deriv.heading):
            if row.key.startswith('DRV-') and ITEM_KEY_RX.match(row.key[len('DRV-'):]):
                keys[row.key[len('DRV-'):]] += 1

    seen_tokens = collections.Counter(TOKEN_RX.findall(text))
    for token, count in sorted(seen_tokens.items()):
        if count != keys[token]:
            findings.append(
                f'"{token}" appears {count}× in CONFORMANCE.md but is the key of {keys[token]} register row(s) '
                f'(§7 determinations + §8 derivations) — an inventory row anchors on that token by a word '
                f'search over the WHOLE file, so an occurrence outside a row key resolves as a filed '
                f'determination. Cite items as "A.1 item N" in prose; the token is a row key and nothing else')
    return findings, numbered, unnumbered, pinned


# kb/Work R23 — the drift half: a SOURCE comment claiming "implementor item N" is a documentation claim, and
# a claim the register does not carry is the documentation equivalent of a dead lookup (CobolDate cited item
# 171 for months while §7 had no such row). Each citation must name a REAL A.1 item AND a row §7 actually
# carries. The pattern deliberately catches both spellings used in the tree ("implementor item 171",
# "A.1 item 180", "Annex A.1 item 29" is E.3's numbering and is NOT matched — only A.1-anchored forms).
CITATION_RX = re.compile(r'(?:implementor item|A\.1 item)\s+(\d+)')


def sweep_source_citations(register: dict[int, dict], numbered: dict[int, str], src_root: pathlib.Path) -> list[str]:
    """Every `implementor item N` / `A.1 item N` citation in the greenfield source must correspond to a real
    A.1 item that §7 documents. Returns findings; separated from evaluate() so --self-test drives it too."""
    findings: list[str] = []
    for path in sorted(src_root.rglob('*.cs')):
        parts = path.parts
        if 'obj' in parts or 'bin' in parts or 'Generated' in parts:
            continue
        if not any(p.startswith('Cobol.Net.') for p in parts):
            continue
        text = path.read_text(encoding='utf-8', errors='replace')
        for m in CITATION_RX.finditer(text):
            n = int(m.group(1))
            line = text.count('\n', 0, m.start()) + 1
            where = f'{path.name}:{line}'
            if n not in register:
                findings.append(f'{where} cites "{m.group(0)}" but A.1 has no item {n}')
            elif n not in numbered:
                findings.append(f'{where} cites "{m.group(0)}" as documented, but CONFORMANCE.md §7 carries '
                                f'no item-{n} row — the claim is undischarged (A.1-{n}: '
                                f'"{register[n]["subject"][:60]}")')
    return findings


def check_pins(pinned: dict[int, list[str]], inventory: list[dict], schema) -> list[str]:
    """Every SPEC-DERIVED `test-ref` on a DOC inventory row is named by that item's own §7 row(s).

    ⛔ WHY THIS IS THE HALF THE COMPUTED ANCHOR DOES NOT COVER. The anchor makes a mis-filed DETERMINATION
    unspellable; it does nothing about mis-filed EVIDENCE, and that is the half kb/Work A11 actually cost. The
    first back-fill closed item 87 (FORMATTED-CURRENT-DATE accuracy, §15.38.4) on
    `SecondsPastMidnight_PinnedClock_ExactTicks` — item 171's test, which never calls the function — and both
    C# checks were structurally blind to it: `UnresolvedTestRefs` asks only that the method be declared, and
    `IsSpecDerived` only that the form and name qualify. Naming the pin in the DETERMINATION and requiring the
    two to agree is what makes an agent's free choice of "any method that compiles" impossible.

    Direction is deliberate: the inventory's spec-derived refs must be a SUBSET of what §7 names. §7 may name a
    pin the inventory has not recorded yet (the determination is ahead of the batch); the inventory may not
    close on evidence the determination does not claim.

    ⚠ A row the register does not carry at all is skipped, and that is not a hole. A DOC row an A.4 module
    WITHDREW declines the conditioning facility, so Annex A.1's preamble makes the item "not required if the
    optional or processor-dependent feature is not implemented" — §7 must carry no row for it, and demanding one
    would make the register claim a determination the standard does not ask for. Such a row still owes its
    WITNESS test; that obligation lives in `Schema.state_for`, not here. Asking `anchor_for` instead of
    `anchor_obliged` was the same defect as kb/Work PB315's third arm: this check reported three withdrawn items
    as findings on a correct batch.

    ⛔ THE SKIP IS THE REGISTER'S CALL, NOT THE VERDICT'S (PB280 Q1). A DOCUMENTED-NON-SUPPORT row for an
    OPTIONAL element §7 records as "Not provided." HAS a determination, so its witness must be named in that
    row's `Pinned by` exactly as a CONFORMS row's is — and keying the skip on the verdict would have dropped
    precisely those rows from this check at the moment their goldens landed. `pinned` is the register as parsed
    by the caller, so it is also what decides here: the audit's fabricated §7 bodies stay hermetic and the live
    run reads the live document.
    """
    findings: list[str] = []
    determined = {f'DOC-A.1-{n}' for n in pinned}
    for row in inventory:
        rid = row.get('rule-id', '')
        if (m := ITEM_KEY_RX.match(rid)) is None:
            continue
        if not schema.anchor_obliged(row, determined):
            continue
        n = int(m.group(1))
        refs = [r for r in schema.split(row.get('test-ref', '') or '', schema.test_ref_sep)
                if schema.is_spec_derived(r)]
        # ⚙ WIDENED FROM "has a spec-derived test-ref" TO "CLOSES" (kb/Work PB386, 2026-09-03). A row may now
        # close on an owner-signed DERIVATION instead of a test, and the claim this check exists to hold — that a
        # DOC row's evidence and its determination are ONE artifact — is about the closure, not about the form of
        # the evidence. Keyed on the test-ref alone, a derivation-closed item would have been dropped from the
        # check at the exact moment it started moving the burn-down.
        derived = schema.derivation_stands(row)
        if not refs and not derived:
            continue
        if n not in pinned:
            findings.append(f'inventory {rid} closes on '
                            f'{refs or "an owner-signed derivation"} but CONFORMANCE.md §7 carries no row for '
                            f'item {n}')
            continue
        for r in refs:
            if r not in pinned[n]:
                findings.append(
                    f'inventory {rid} cites the spec-derived test "{r}", which its own §7 row does not name in '
                    f'`Pinned by` ({pinned[n] or "empty"}) — the determination and its evidence disagree')
    return findings


def check_pin_resolution(pinned: dict[int, list[str]], schema, root: pathlib.Path) -> list[str]:
    """Every test-ref a §7 `Pinned by` cell names RESOLVES on disk, by the same rule the inventory's refs obey.

    ⛔ WHY THIS IS ITS OWN CHECK (review R3-1 of kb/Work PB3018's branch, 2026-10-12). `check_pins` holds the
    INVENTORY's refs to be a subset of what §7 names, and the C# `UnresolvedTestRefs` resolves the INVENTORY's
    refs; neither reads a ref that only §7 carries. A test renamed by a landing (PB3019 renamed
    `ToScaled_PastTheCarrier_RaisesChecked_SaturatesIntermediate_...`) therefore left DOC-A.1-70's `Pinned by`
    naming a method that no longer existed, and a reader following the determination to its evidence found
    nothing; no gate saw it. The determination's own evidence column is held to the inventory's standard: a
    path-template form must name an existing file, an xUnit form a `<Class>.cs` under its test directory that
    declares `<Method>(`.
    """
    findings: list[str] = []
    source: dict[pathlib.Path, str] = {}  # class file -> its text, read once
    class_files: dict[tuple[str, str], list[pathlib.Path]] = {}  # (test-dir, class) -> its files, searched once
    for n in sorted(pinned):
        for ref in pinned[n]:
            scheme, _, body = ref.partition(':')
            body = body.strip()
            form = schema.test_ref_forms.get(scheme)
            if form is None or not body:
                findings.append(f'§7 item {n} `Pinned by` names "{ref}", which is not a test-ref form '
                                f'({sorted(schema.test_ref_forms)}) with a body')
                continue
            if (template := form.get('path-template')) is not None:
                rel = template.replace('{0}', body)
                if not (root / rel).is_file():
                    findings.append(f'§7 item {n} `Pinned by` names "{ref}", but {rel} does not exist')
                continue
            # A bare <Class> pins the whole class (a determination whose evidence is a family of tests); it
            # resolves when the class file exists. <Class>.<Method> additionally needs the method declared.
            cls, dot, method = body.rpartition('.') if '.' in body else (body, '', '')
            if not cls or (dot and not method):
                findings.append(f'§7 item {n} `Pinned by` names "{ref}", which is not <Class> or <Class>.<Method>')
                continue
            key = (form['test-dir'], cls)
            if key not in class_files:
                class_files[key] = list((root / form['test-dir']).rglob(f'{cls}.cs'))
            files = class_files[key]
            if not files:
                findings.append(f'§7 item {n} `Pinned by` names "{ref}", but there is no {cls}.cs under '
                                f'{form["test-dir"]}')
                continue
            if not method:
                continue
            rx = re.compile(r'\b' + re.escape(method) + r'\s*\(')
            for f in files:
                if f not in source:
                    source[f] = f.read_text(encoding='utf-8')
            if not any(rx.search(source[f]) for f in files):
                findings.append(f'§7 item {n} `Pinned by` names "{ref}", but {cls}.cs declares no method '
                                f'"{method}" (a renamed or removed test leaves the determination without its '
                                f'evidence)')
    return findings


def main(argv: list[str]) -> int:
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding='utf-8', errors='replace')
        except (AttributeError, ValueError):
            pass

    if '--self-test' in argv:
        return self_test()

    register = load_register()
    text = CONFORMANCE.read_text(encoding='utf-8')
    rows = section7_rows(text)
    findings: list[str] = []

    required = sum(1 for r in register.values() if r['requirement'] == 'required')
    optional = sum(1 for r in register.values() if r['requirement'] == 'optional')
    conditional = sum(1 for r in register.values() if r['requirement'] == 'conditionally required')
    unclassified = sum(1 for r in register.values() if r['requirement'] == 'unclassified')
    unreachable = unreachable_items()
    doc_all = sum(1 for r in register.values() if r['documented'])
    doc_unreachable = sorted(n for n in unreachable if n in register and register[n]['documented'])
    doc_required = doc_all - len(doc_unreachable)

    print('=== ANNEX A.1 REGISTER (from spec-rule-catalog.json, parsed out of the standard) ===')
    print(f'  items {len(register)}  =  required {required} · optional {optional} · '
          f'conditionally required {conditional} · unclassified {unclassified}')
    print(f'  identified as REQUIRING USER DOCUMENTATION: {doc_all}   (not required: {len(register) - doc_all})')
    if doc_unreachable:
        print(f'  ⊘ CANNOT ARISE — the feature is declined, so A.1\'s preamble makes the item not required: '
              f'{len(doc_unreachable)}')
        for n in doc_unreachable:
            print(f'      A.1-{n:<5d} {register[n]["subject"][:62]:<64s} ({unreachable[n]})')
        print(f'  → obligations in scope: {doc_required}')
    if required + optional + conditional + unclassified != len(register):
        findings.append('the requirement classes do not sum to the item count')

    # ⛔ THE CHECKS LIVE IN evaluate() AND NOWHERE ELSE. The reporting loop below only RENDERS; it must not
    # re-decide anything, or --self-test would be proving a different implementation than the one that runs.
    # (The first draft did exactly that — two copies of the same rules, which is the defect this repo keeps
    # re-learning: feedback_one_rule_one_place.)
    findings_from_rows, numbered, unnumbered, pinned = evaluate(register, text)
    findings.extend(findings_from_rows)
    # kb/Work R23 — source comments claiming "implementor item N" must be discharged by a real §7 row.
    src_findings = sweep_source_citations(register, numbered, ROOT / 'src')
    findings.extend(src_findings)
    # The determination and its evidence are ONE artifact: §7's `Pinned by` cell and the inventory row's own
    # spec-derived test-ref must agree. Imported lazily so the register audit still runs where the inventory
    # has not been built (the error a missing inventory raises is a SystemExit with the rebuild command).
    from inventory_schema import load_inventory, load_schema
    findings.extend(check_pins(pinned, load_inventory(), load_schema()))
    findings.extend(check_pin_resolution(pinned, load_schema(), ROOT))

    print('\n=== docs/CONFORMANCE.md §7 ROWS ===')
    seen: set[int] = set()
    for key, element, _determination, _pins in rows:
        if (m := ITEM_KEY_RX.match(key)) is None:
            print(f'  ⚠ (unnumbered) {element[:74]}')
            continue
        n = int(m.group(1))
        if n not in register:
            print(f'  ⛔ {key:<5} NOT AN A.1 ITEM')
            continue
        # ⚠ MORE THAN ONE ROW PER ITEM IS LEGITIMATE, not a finding. One A.1 element can need several
        # determinations (A.1-92 needs one for a function result's text image and one for evaluation under a
        # floating-point argument). The first draft failed on the duplicate and was simply wrong.
        mark = 'ok' if corresponds(element, register[n], register) else '⛔'
        print(f'  {mark} {key:<5} {register[n]["subject"][:74]}'
              + ('   (2nd determination)' if n in seen else ''))
        seen.add(n)

    covered_doc = sorted(n for n in numbered if register[n]['documented'])
    covered_free = sorted(n for n in numbered if not register[n]['documented'])
    print('\n=== COVERAGE ===')
    print(f'  A.1 items with a §7 determination : {len(numbered)} of {len(register)}')
    print(f'    of which the standard REQUIRES documented : {len(covered_doc)} of {doc_required}'
          f'  ({100.0 * len(covered_doc) / doc_required:.1f}%)')
    if covered_free:
        print(f'    documented VOLUNTARILY (A.1 does not require it) : {covered_free}')
    print(f'  ⛔ REMAINING documentation obligations : {doc_required - len(covered_doc)}')

    print('\n=== FINDINGS ===')
    if not findings:
        print('  none — every §7 row names an A.1 item whose element it matches')
    for f in findings:
        print(f'  ⛔ {f}')

    if '--json' in argv:
        # ⛔ ONE LINE, PREFIXED, so a C# gate can find it without parsing the report — the idiom
        # scripts/corpus_sweep.py --json established and ExternalCorpusPopulationDriftTests reads. `filed` is
        # the POPULATION: a run that observed nothing must be distinguishable from a run that found nothing
        # wrong (feedback_verdict_evidence_invariant).
        print('JSON ' + json.dumps({
            'items': len(register),
            'filed': sorted(numbered),
            # The items a DECLINED module withdraws (derived from the `derived-verdicts` selectors, never listed
            # here). Emitted because the C# register gate needs the same set: such an item carries an inventory
            # verdict and must NOT be expected to have a §7 determination — A.1's preamble says so.
            'unreachable': sorted(unreachable),
            'pinned': {str(k): v for k, v in sorted(pinned.items())},
            'documented_required': doc_required,
            'discharged': len(covered_doc),
            'remaining': doc_required - len(covered_doc),
            'src_citations': len(src_findings),
            'findings': findings,
        }, ensure_ascii=False))

    if '--check' in argv:
        return 1 if findings else 0
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
