*>--- conformance-case
*> id: l1_length_bit_group_positions
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.50.4 r5 + r3 — FUNCTION LENGTH OF A BIT-BEARING GROUP THAT IS NOT A RECORD, with §15.50.4 r1 as
      *> the control that names which rule is speaking. A BIT-LAYOUT REGRESSION GOLDEN: it closes no inventory
      *> row, and the paragraph below records WHY, so the claim it once carried is not inherited again.
      *> r5: "The returned length shall include the number of implicit FILLER positions, if any, in argument-1."
      *>   python scripts/spec/cite.py --check 15.50.4 "The returned length shall include the number of implicit
      *>   FILLER positions, if any, in argument-1."  ->  OK  §15.50.4 5)  (Returned value rules)
      *> r3: "If argument-1 is other than category boolean or usage national, the returned value is an integer
      *>   equal to the length of argument-1 in alphanumeric character positions."  ->  OK  §15.50.4 3)
      *> r1: "If argument-1 is a bit group item, an elementary boolean data item, a boolean literal, or a type
      *>   declaration for a boolean item, the returned value is an integer equal to the length of argument-1 in
      *>   boolean positions."  ->  OK  §15.50.4 1)
      *>
      *> ⛔ THIS FILE DOES NOT EXERCISE §15.50.4 r9, AND THE FIRST DRAFT'S CLAIM THAT IT DID WAS WRONG.
      *> That draft argued that §8.5.1.6.3's boundary-advance filler — "implicitly described as a filler
      *> elementary bit data item of the necessary number of bits and of the same level number as the next item
      *> within that group", level 05 here — is a SIBLING of the 05 group and therefore outside it, leaving
      *> L1R-AG at a non-integral 11 bits that only r9 could round. The standard's own worked example refutes
      *> that inference. §D.10 2): "The generated filler is not part of the preceding data, but is part of any
      *> groups that contain the item. For example, the filler generated after item-6 is included in group-2 and
      *> group-1, but not in item-6." In that example the filler is annotated at LEVEL 02 while item-6 sits at
      *> level 03 inside group-2 (level 02), and the standard still puts the filler INSIDE group-2 — so a
      *> filler's LEVEL NUMBER and its CONTAINMENT are different questions. (Annex D is INFORMATIVE; it is cited
      *> here as the standard's own reading of §8.5.1.6.3, which assigns the filler a level number and says
      *> nothing at all about which groups contain it.)
      *>   python scripts/spec/cite.py --check D.10 "The generated filler is not part of the preceding data, but
      *>   is part of any groups that contain the item."  ->  OK  §D.10 2)
      *> L1R-AG therefore occupies 11 + 5 = 16 bits = EXACTLY 2 alphanumeric character positions; r9's antecedent
      *> ("argument-1 does not occupy an integral number of positions") is FALSE; and AG=2 is r5 counting the
      *> implicit filler, the same rule 2023/pb43_usage_bit_occupies_bits measures as SPLIT=3. Between
      *> §8.5.1.6.3's boundary-advance bullet and its record-end bullet, every alphanumeric group carrying bit
      *> data ends byte-integral, so NO FUNCTION LENGTH golden can make r9's antecedent true: RV-15.50.4-9 stays
      *> test-needed and wants an owner adjudication recorded per §4.2.16 (r9 vacuous under COBOL.NET's 8-bit
      *> determination plus the filler rules), not a fixture.
      *>
      *> WHAT THIS FILE DOES PIN. §13.18.29.4 GR3 — "If a GROUP-USAGE clause is not specified or implied for a
      *> group item that is not strongly typed and is not a variable-length group, that group item is an
      *> alphanumeric group item" — makes each group below alphanumeric (no GROUP-USAGE, no TYPE, no
      *> dynamic-length or dynamic-capacity subordinate, §8.5.1.12.1), so r3 measures it in alphanumeric
      *> character positions rather than r1 measuring it in boolean ones. §8.1.2 item 3 leaves the number of bits
      *> in a byte to the implementor; COBOL.NET specifies 8 (docs/CONFORMANCE.md).
      *>   AG  11 bits + 5 implicit filler = 16 = 2  — a bit run that does NOT end on a byte boundary
      *>   BG  16 bits, no filler needed   = 16 = 2  — the exactly-integral control; a blanket +1 byte answers 3
      *>   CG   3 bits + 5 implicit filler =  8 = 1  — a sub-byte group; dropping the filler and truncating -> 0
      *>   EB  r1: an ELEMENTARY boolean item is measured in BOOLEAN positions, so neither r3 nor r9 can reach
      *>       it — 11, not 2. This leg is what says which rule is speaking.
      *>   RA/RB/RC — the enclosing records, where r5 counts the same filler one level up:
      *>       11+5+16 = 32 = 4  ·  16+16 = 32 = 4  ·  3+5+16 = 24 = 3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1LENBGP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L1R-A.
          05 L1R-AG.
             10 L1R-AB PIC 1(11) USAGE BIT.
          05 L1R-AT PIC X(2).
       01 L1R-B.
          05 L1R-BG.
             10 L1R-BB PIC 1(16) USAGE BIT.
          05 L1R-BT PIC X(2).
       01 L1R-C.
          05 L1R-CG.
             10 L1R-CB PIC 1(3) USAGE BIT.
          05 L1R-CT PIC X(2).
       01 L1R-EB PIC 1(11) USAGE BIT.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "AG=" FUNCTION LENGTH(L1R-AG).
           DISPLAY "BG=" FUNCTION LENGTH(L1R-BG).
           DISPLAY "CG=" FUNCTION LENGTH(L1R-CG).
           DISPLAY "EB=" FUNCTION LENGTH(L1R-EB).
           DISPLAY "RA=" FUNCTION LENGTH(L1R-A).
           DISPLAY "RB=" FUNCTION LENGTH(L1R-B).
           DISPLAY "RC=" FUNCTION LENGTH(L1R-C).
           STOP RUN.
       END PROGRAM L1LENBGP.
