*>--- conformance-case
*> id: l1_sync_size_unchanged
*> rules: [GR-13.18.55.4-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.55.4 GR6 — any storage-position adjustment the
      *> SYNCHRONIZED clause causes does NOT change the SIZE of the
      *> synchronized item itself.
      *>
      *> THE RULE. §13.18.55.4 GR6: "Any adjustment in storage position
      *> resulting from the SYNCHRONIZED clause does not affect the size of
      *> the synchronized data item."
      *> GR2 is what makes the rule non-trivial: it reserves the bytes
      *> "between the leftmost and rightmost natural boundaries delimiting
      *> this data item" and says "If the number of bytes required to store
      *> this data item is less than the number of bytes between those natural
      *> boundaries, the unused bytes or portions thereof shall not be used
      *> for any other data item" — and then GR2 a) and b) put those unused
      *> bytes in the size of the CONTAINING group and in a REDEFINES of it.
      *> GR6 is the boundary of that: the containing group may grow, the
      *> SYNCHRONIZED ITEM may not.
      *>
      *> WHAT THIS MEASURES, AND WHY IT IS NOT THE EXISTING WITNESS. The
      *> shipped golden conformance:2002/l1_byte_length_implicit_filler
      *> measures the GROUP's byte length around a synchronized item (its
      *> SYNC=3 leg, §13.18.55.4 GR9 b). GR6 is about the synchronized ITEM's
      *> own length, so every leg below applies FUNCTION BYTE-LENGTH
      *> (§15.14.1, "an integer equal to the length of the argument in bytes")
      *> to the ELEMENTARY item, never to its group.
      *>
      *> THE SHAPE OF EACH LEG. Each pair is the same description twice, once
      *> with the clause and once without, and each is preceded inside its
      *> group by a one-byte item so that a natural-boundary adjustment WOULD
      *> be needed if the implementation performed one. GR6 requires the two
      *> numbers to agree; the no-clause twin is the reference GR6 names.
      *>
      *> THE ABSOLUTE NUMBERS, DERIVED.
      *> COMP  — docs/CONFORMANCE.md DOC-A.1-205 (the §13.18.60.4 GR4
      *>         determination): a BINARY/COMP item of 3-4 digits occupies
      *>         2 bytes. So 2, with the clause and without it.
      *> SEP   — §13.18.52.4 GR6 a): with SEPARATE CHARACTER the operational
      *>         sign "is presumed to be the leading ... character position of
      *>         the data item ...; this character position is not a digit
      *>         position", so PIC S9(4) SIGN IS LEADING SEPARATE is 4 digit
      *>         positions plus one more character position = 5, at one byte
      *>         per character position (DOC-A.1-209). So 5 and 5 — the
      *>         separate sign position is not disturbed by the clause.
      *> ALNUM — DOC-A.1-209, one byte per character position: PIC X(3) is 3.
      *>         §13.18.55.3 SR1 permits the clause on any elementary item, so
      *>         an alphanumeric carrier is in scope.
      *> LR    — GR4 ("SYNCHRONIZED LEFT ... will begin at the left byte of
      *>         the natural boundary") and GR5 ("SYNCHRONIZED RIGHT ... will
      *>         terminate on the right byte of the natural boundary") are the
      *>         two forms that name a boundary explicitly, and GR6 says
      *>         "ANY adjustment", so both must still answer 2.
      *>
      *> WHAT WOULD BREAK IT. An implementation that padded a synchronized
      *> item out to its natural boundary and counted the pad in the ITEM's
      *> length would answer COMP=4 2 or LR=4 4; one that placed a
      *> SYNCHRONIZED RIGHT item by left-padding it inside its own extent
      *> would answer LR=2 4. Both are printed differences.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SYN02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CB1.
          05 CB1A PIC X.
          05 CB1B PIC S9(4) COMP SYNC.
       01 CB2.
          05 CB2A PIC X.
          05 CB2B PIC S9(4) COMP.
       01 SD1.
          05 SD1A PIC X.
          05 SD1B PIC S9(4) SIGN IS LEADING SEPARATE SYNC.
       01 SD2.
          05 SD2A PIC X.
          05 SD2B PIC S9(4) SIGN IS LEADING SEPARATE.
       01 AN1.
          05 AN1A PIC X.
          05 AN1B PIC X(3) SYNC.
       01 AN2.
          05 AN2A PIC X.
          05 AN2B PIC X(3).
       01 LR1.
          05 LR1A PIC X.
          05 LR1B PIC S9(4) COMP SYNC LEFT.
          05 LR1C PIC X.
          05 LR1D PIC S9(4) COMP SYNC RIGHT.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "COMP=" FUNCTION BYTE-LENGTH(CB1B)
               " " FUNCTION BYTE-LENGTH(CB2B)
           DISPLAY "SEP=" FUNCTION BYTE-LENGTH(SD1B)
               " " FUNCTION BYTE-LENGTH(SD2B)
           DISPLAY "ALNUM=" FUNCTION BYTE-LENGTH(AN1B)
               " " FUNCTION BYTE-LENGTH(AN2B)
           DISPLAY "LR=" FUNCTION BYTE-LENGTH(LR1B)
               " " FUNCTION BYTE-LENGTH(LR1D)
           STOP RUN.
