## Entry 1045 — 2026-07-27 03:12 PDT — The Preface, and a gate on the text that licenses distribution

The owner (a lawyer, WA/CA/US Patent Bar) read page 28's grant as permitting publication of the transcription
provided the acknowledgment accompanies it, and asked for the Preface to be added.

Page 28 grants reproduction "in whole or in part … for any other purpose" and requests the acknowledgment
paragraphs "in their entirety **as part of the preface** to any such publication". They were previously present
only in position at page 28 — faithful, but not where the permission asks for them. The Preface now opens the
file and carries them verbatim, and they remain at page 28 too, because the transcription is faithful and the
standard prints them there.

**Our editorial matter is marked as ours.** The Preface opens by stating it is NOT part of ISO/IEC 1989:2023,
names the standard and its copyright holder, quotes the permission relied on, and records that the published
standard governs where the two differ. A reader must never have to guess which words are ISO's.

**New gate: `scripts/spec/verify_acknowledgment.py`.** This text is a condition of distribution, not ordinary
prose — a well-meaning reflow that drops a clause is a different class of mistake from a typo. The check compares
the preface copy AND the in-position copy against **the printed PDF**, not against each other: comparing the two
copies would only prove they agree, while the PDF makes it a test of fidelity. Whitespace and wrapping are
normalised; everything else must match exactly.

**Proven able to fail**, which is the only reason to trust it: deleting one clause from the preface copy produced
`✗ preface vs printed: DIFFERS`, the exact first-differing character, and a non-zero exit — while correctly still
passing the untouched page-28 copy. Restored, both verbatim at 883 characters.

The checker also caught its own first bug: it searched for the boundary markers in raw text, and every one of the
three sources wraps at a different column, so a marker straddling a line break was never found. It failed loudly
rather than silently matching nothing.

**Not done, and deliberately the owner's call:** whether to publish the repaired PDF. I flagged 17 U.S.C. § 1202
(removal of copyright management information) against the counter-argument that the ANSI stamp is his own PII,
there is no concealment intent, and ISO's own notices remain. That is his judgement to make, not mine.

