## Entry 1319 — 2026-08-18 12:43 PDT — PB94: the VALUE clause's literal-class rules (SR2 / SR4) are enforced — error strict, the representable vendor leniency with a warning under --permissive, and no more `abcL`

**What landed.** kb/Work **PB94** (MAJOR, crashes + under-rejects): `01 V PIC 9 VALUE "abc".` reached the C# backend
(`error CS0103: The name 'abcL' does not exist`), `01 W PIC 99 VALUE "7".` / `01 X PIC X(2) VALUE 12.` compiled
silently, `01 A PIC 9(3) VALUE SPACES.` silently initialized to ZERO and `VALUE ALL "1"` on a numeric item was
CS1003. `DataBinder.ValidateValueCategory` carried the national (§13.18.63.3 SR5) and boolean (SR10) halves of the
VALUE literal-class rule and nothing for the two categories every program uses: SR2 — "If the category of the
subject of the entry is numeric, all literals in the VALUE clause shall be numeric" — and SR4 — "If the item is
of category alphabetic, alphanumeric, or alphanumeric-edited literals in the VALUE clause shall be alphanumeric
literals". Both are ALL FORMATS syntax rules at every edition (the condition-name VALUEs of Format 3 ride the same
funnel — SR24 imports rules 10 and 17 on top of the ALL FORMATS set), and CCVS-85's only quoted VALUE on a numeric
picture (`NC107A`: `PICTURE 999 VALUE "000" BLANK WHEN ZERO`) is a numeric-EDITED item by §13.18.8 GR2, where
SR6/SR7 admit it — the rules and the corpus agree.

The validator now RETURNS the raw text to store and grows two legs, both **COBOLNET1657** (`ValueLiteralClass`,
the documented-dialect-leniency seam `EditionContext.Removed`): SR2 — an alphanumeric literal or `ALL "…"` on a
numeric subject is an error strict; under `--permissive` a digits-only content (ALL digits repeated to the digit
width, §8.3.3.6.4 GR2) is stored as that NUMBER with a warning, and a content that is not numeric in form is an
error on both axes; a character figurative (SPACE / QUOTE / HIGH-VALUE / LOW-VALUE) is an error strict and ZERO
with a warning saying so under `--permissive` (a native numeric item holds no character fill — the former silent
zero, made loud); SR4 — a numeric literal on an alphabetic / alphanumeric / alphanumeric-edited subject is an
error strict and, under `--permissive`, its characters as MOVE would store them, with a warning. The four call
sites (the item VALUE, the 88 single values, the 88 THROUGH range) store what the validator returns.

**Evidence.** Negatives `pb94-value-alphanumeric-on-numeric`, `pb94-value-numeric-on-alphanumeric`,
`pb94-value-nonnumeric-on-numeric` (1657, all editions); `ValueLiteralClassTests` (7 permissive rows and their
values — `[07]`, `[012]`, `[15]`, `[111]`, `[000]`, `[12 ]`, `[12 ]`; the both-axes error at 85 and 2023; five
legal shapes incl. `PIC 999 VALUE "000" BLANK WHEN ZERO` (SR8: no blanking on an alphanumeric VALUE); an 88 on a
numeric variable). Inventory SR-13.18.63.3-2 and -4 → CONFORMS (GAP 3960 → 3958).

**Gate.** Characterization 33/33 · Conformance (filtered: Value/Nist/VersionMatrix/Corpus/Data/Condition/
Initialize/Table) 3461/3461 · Unit (filtered) 3255/3255. Battery #16 owed for the PB93 + PB94 batch.

