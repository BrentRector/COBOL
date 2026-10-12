## Entry 1325 — 2026-08-18 16:26 PDT — PB64: the locale facility is a GO — the four owner decisions, the design adopted, and a pause before T1 (the owner is diverting to the collation task next)

**What happened.** With the defect backlog empty (PB99 and PB100 landed, batteries #18/#19 green), `work.py
next` listed only PB64 — the A.4.9 locale module, whose Q1–Q4 were the owner's. The session reached it and asked
the four bare questions in turn; the owner answered all four on 2026-08-18: **Q1 — IMPLEMENT** the module (council
decision 3 of 2026-07-03, documented non-support, is superseded); **Q2 — environment variables**
`COBOL_USER_LOCALE` / `COBOL_SYSTEM_LOCALE` are the implementor mechanism for §8.2.1's two defaults (with the .NET
`CurrentCulture` / `InstalledUICulture` / `INVARIANT` fallbacks — determination L2 as drafted, no compiler
option); **Q3 — yes**, a locale-based collating sequence is offered for INDEXED file keys (L8, the cross-locale
key-order caveat documented); **Q4 — `STANDARD-COMPARE` / `ORDER TABLE` are implemented over Unicode CLDR + UCA
data** (.NET's ICU root collation and tables derived from it — never a hand-vendored ISO 14651 file), and the
conformance statement COBOL.NET makes is fixed VERBATIM by the owner: **"Implements collation behavior consistent
with ISO/IEC 14651 through derived tables and CLDR/UCA data."**

**What landed (docs only, this commit).** The 2026-08-09 agent draft is ADOPTED and renamed:
`docs/rearchitecture/DESIGN-locale-facility.md` — its banner is now the adoption record with the four decisions
verbatim, §4.9 carries the Q4 mapping (`ISO 14651_2020_TABLE1` = the UCA/CLDR root collation; argument-4 levels
1–4 → `CompareOptions`; a CLDR tag as an implementor extension of `ORDER TABLE`; anything else
EC-ORDER-NOT-SUPPORTED), §12's T0 is marked LANDED (PB78 / PB92 / PB100), §7's diagnostic band is re-based (the
drafted 1642–1650 were claimed by other work; the locale band starts at COBOLNET1662, rules keyed (a)–(i)), and
§15's questions carry their answers. Registered in `COBOLNET_DESIGN.md` §0.5 and `DOC_INDEX.md`. kb/Work PB64
records the decisions and the pause; plan §0 carries the handoff.

**⏸ Paused, deliberately.** The owner asked to pause here, save the context, and divert to the COLLATION task
next with guidance to come — that task is the natural home of the design's T2 (the `CobolCollation` collapse:
today's three `CobolString.Compare` / `ThruMember` overload carriers → ONE abstraction, behaviour-free) and T3
(`LocaleCollation` over `CultureInfo.CompareInfo`, ALPHABET `IS LOCALE`, the PCS, SORT/MERGE, HIGH/LOW-VALUE,
ORD/CHAR — design §4.4). Nothing of T1 is coded; the tree is clean after this commit; the next session starts
from the owner's guidance, then plan §0's NEXT (T1 → T7, a battery per increment).

