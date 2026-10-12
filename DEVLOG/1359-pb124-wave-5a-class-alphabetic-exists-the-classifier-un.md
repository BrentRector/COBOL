## Entry 1359 — 2026-08-29 01:26 PDT — PB124 wave 5a: class ALPHABETIC exists — the classifier un-folds PIC A, and every string row's kind was re-derived from its own rule's WORDING

AR-15.3-1 (batch 7): CobolClass had no Alphabetic member, so a PIC A item classified alphanumeric and passed
the category-worded rows — NUMVAL(PIC-A) bound clean at a measured dozen Table-21 positions whose cells
print no Alph. The refuter also showed the fold was a CHOICE, not a limit (PicInfo.IsAlphabetic already
existed for §15.18.4 r3). The member now exists — §8.5.2.1 Table 2's own first row — and ClassOfPlace
un-folds PIC A before the category table, the same shape as the Usage.Index arm.

The old 's' union split row by row on each rule's own wording, checked against Table 21's cells: CLASS-worded
rows ("class alphabetic, alphanumeric, or national" — UPPER-CASE, LOWER-CASE, TRIM, SUBSTITUTE, ORD,
REVERSE, FIND-STRING, STANDARD-COMPARE, LOCALE-COMPARE) keep 's', which gains Alphabetic; CATEGORY-worded
rows ("of category alphanumeric or national" — the NUMVAL family with its TEST- twins, the FORMATTED-*
family, SECONDS-FROM-FORMATTED-TIME, INTEGER-OF-FORMATTED-DATE, LOCALE-DATE/-TIME) move to the new 't' kind
and now reject PIC A, per Table 2's closing sentence ("refers to the CATEGORY unless class is specifically
indicated"). CONCAT's 'c' gains the member its rule always named. Cross rules take alphabetic+alphanumeric
as ONE block — §15.59.3 r2 writes the exception outright, §15.87.3 r2 / §15.96.3 r2 spell the same two
blocks — so TRIM(PIC-A "c") and MAX(PIC-A "zz") stay legal, pinned in the golden beside ORD over a
ref-modded PIC A (GR6 keeps the class). One more table wobble recorded in the verdict: Table 21's
TEST-NUMVAL-C cell prints Alph2 against the §15.68.3 r2 import it summarizes; the rule text governs.
AR-15.3-1 → CONFORMS. PB124 stays open — GR-15.2-2/-6, AR-15.3-5/-10/-11/-13 remain.

