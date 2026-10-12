## Entry 1269 — 2026-08-09 16:10 PDT — PB59 family 2 (the trim half): a leading space is content, a digitless argument raises, the §15.4 maximum is declared — four rows close

BaseConvert and HexDigitsToBytes both opened with a symmetric Trim(), so "  FF" silently converted, an
all-spaces BASECONVERT argument fabricated "0" (the guard keyed on the accumulator VALUE, which cannot
tell zero digits from the digit 0), and an all-space HEX source fell through to an empty byte string.
Now: TrimEnd(' ') at both sites (SPACE only — a bare TrimEnd would keep swallowing tabs/NBSP, the same
defect one character narrower), the BaseConvert guard counts DIGITS CONSUMED, the digitless HEX source
raises under §15.19.3 r4 — deliberately NOT r1, the item is not zero length (the mis-citation trap the
triage named) — and Convert's top gains the RUNTIME r1 zero-length screen beside the bind-time literal
twin, with the two-arm relationship written into both comments.

Two determinations settled by derivation, not inheritance: (1) lowercase a–f are NOT BASECONVERT digits —
§15.12.3 r2 names "the basic-letters A to F" and §8.1.3.1 Table 1 defines basic letters as the Latin
CAPITAL letters; §8.1.3.2 GR3a's case-insensitivity governs compilation-group TEXT, not runtime data; the
GPL corpus has no BASECONVERT case, so no vendor latitude question arises; the unadjudicated lowercase arm
is deleted. CONVERT's HEX source keeps both cases — r4's term is "hexadecimal digits", the hex-literal
term defined over both. (2) The §15.4 maximum returned-value length is DECLARED: Annex A.1 item 93
(audit_annex_a1.py 21/199, --check clean) — 8,191 character positions, the literal/concatenation maximum
reused so the bound and the language's largest string constant are one number — documented BEFORE
enforced, then enforced in BaseConvert (an 8,000-digit legal conversion still answers; 8,192+ raises).

Differential rows: the padded-but-valid cases now pinned on BOTH functions (no prior golden had a padded
HEX source — the green-gates-arent-evidence hole the map named), the three invalid-digit shapes with
checking OFF (the §15.3 default) and ON (fatal), and the "0"-one-digit discriminator row. INTRINSICS
design doc gains the value-path bullet. AR-15.12.3-2 DIVERGES→CONFORMS · AR-15.12.3-3 · AR-15.19.3-1 ·
AR-15.19.3-4 PARTIAL→CONFORMS (its usage half stays open under AR-15.19.3-5, family 5). GAP 4178 → 4174.
Gates: Conformance ~BaseConvert|~Convert|~Intrinsic|~Corpus 699/699 · the seven new tests asserted present
· SpecTraceabilityInventory 10/10 · audit --check clean · work.py 126 well-formed.

