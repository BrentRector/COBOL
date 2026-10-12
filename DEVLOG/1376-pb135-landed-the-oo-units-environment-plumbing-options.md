## Entry 1376 — 2026-08-29 06:36 PDT — PB135 LANDED: the OO units' environment plumbing — [options-paragraph] in all five skeletons with the §11.9.4 GR1 inheritance chain, and the device-mnemonic walk reaching class configuration

One root shape, two mechanisms. (1) §10.6.1 prints [options-paragraph] in every source-unit skeleton and
spec :42527 places ENTRY-CONVENTION "in the OPTIONS paragraph of a class" — yet no OO skeleton parsed it
(ARITHMETIC IS STANDARD-DECIMAL in a class was a bare syntax error, which also broke §4.2.6's
identification obligation for the STANDARD-BINARY reject in OO units). The five skeletons now carry
optionsParagraph?, OptionsBinder gained the paragraph-level core (the OO units hold the paragraph on the
SKELETON, not inside an identification body), and the §11.9.4 GR1 chain is real: the CLASS model is the
baseline, the OBJECT/FACTORY paragraphs override clause by clause into their own DataBinder channels
(CallInheritOptions — the containee seeding mechanism the program path already had), and a METHOD's own
paragraph swaps in around ITS body's statement binding — in the per-pc pass, where statements actually
bind; the first cut put the swap in the roster REGISTRATION loop, which binds nothing and restored
nothing. The golden derives both: the class-level DEFAULT ROUNDED MODE NEAREST-EVEN gives M1's bare
ROUNDED 5/2 → 2, and M2's own NEAREST-AWAY-FROM-ZERO paragraph gives 5/2 → 3 with no leak back.

(2) The refuter's probe-the-hidden-axis finding: MnemonicRegistry.Of walked parse-tree parents for a
ProgramUnitContext only, so a method's chain topped out at compilationGroup with an EMPTY device map —
DISPLAY UPON / ACCEPT FROM / WRITE ADVANCING mnemonic-name drew COBOLNET0817 on legal source while the
SWITCH map, built from the SAME SPECIAL-NAMES rule through OoDriver's synthetic unit, resolved. The walk
now reads each OO ancestor's own configuration (nearest scope wins; a method may not declare one — the
§12.3.3 SR2 rejection already stood). SR-14.9.11.3-2 DIVERGES → CONFORMS.

