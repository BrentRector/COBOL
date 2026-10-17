*>--- conformance-case
*> id: l1_sync_abbreviation
*> rules: [SR-13.18.55.3-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.55.3 SR2 — "SYNC is an abbreviation for SYNCHRONIZED."
      *> The abbreviation is accepted EVERYWHERE the full word is, in every
      *> form of the clause, and produces the identical layout.
      *>
      *> THE RULE. §13.18.55.3 SR2: "SYNC is an abbreviation for
      *> SYNCHRONIZED." An abbreviation is not a second clause with similar
      *> behaviour — it is the SAME clause spelled shorter, so every place
      *> §13.18.55 reaches has to answer identically to both spellings, and
      *> nowhere may accept one and refuse the other.
      *>
      *> WHERE THE CLAUSE CAN BE WRITTEN, and therefore what this golden has
      *> to pair.
      *>   §13.18.55.3 SR1: "The SYNCHRONIZED clause may be specified for
      *>   group and elementary items." — so both an elementary entry and a
      *>   group entry, and a level-77 entry, which §8.5.1.3.2 GR3 makes an
      *>   elementary item ("Entries that specify noncontiguous data items
      *>   that are not subdivisions of other items, and are not themselves
      *>   subdivided, have been assigned the special level-number 77").
      *>   §13.18.55.2's general format offers the optional LEFT / RIGHT
      *>   phrase, governed by GR4 and GR5 — so all three forms (bare, LEFT,
      *>   RIGHT) are written in both spellings.
      *> Group SYNCHRONIZED is a COBOL-2023 introduction, which is why the
      *> GROUP leg lives in the 2023 corpus; the companion witness
      *> negative/l1_sync_abbrev_group_below_2023 proves the ABBREVIATION
      *> reaches that same edition gate below 2023, which is the half a
      *> positive golden cannot show.
      *>
      *> THE NUMBERS, AND WHY THEY ARE NOT ARBITRARY.
      *> The absolute answers come from the §13.18.55.4 GR9 implementor
      *> determination, docs/CONFORMANCE.md DOC-A.1-195: COBOL.NET performs
      *> NO physical alignment for SYNCHRONIZED and generates NO implicit
      *> FILLER for it. Combined with DOC-A.1-205 (a BINARY/COMP item of 3-4
      *> digits occupies 2 bytes) and DOC-A.1-209 (USAGE DISPLAY is one byte
      *> per character position):
      *>   a PIC S9(4) COMP elementary item is 2 bytes — the L77 leg;
      *>   PIC X + PIC S9(4) COMP + PIC X is 1 + 2 + 1 = 4 bytes — the ELEM,
      *>   LEFT, RIGHT and GROUP legs.
      *> SR2 itself is the EQUALITY of the two numbers on each line; the
      *> absolute value is what keeps the leg honest, since a pair that read
      *> "4 4" while the true answer was 3 would be two wrong answers in
      *> agreement.
      *>
      *> WHAT WOULD BREAK EACH LEG. If SYNC and SYNCHRONIZED were separate
      *> constructs, the failure would not be a different number — it would be
      *> a COMPILE error on whichever spelling one of the five positions did
      *> not accept, which is exactly what this golden's five position-pairs
      *> are for. The IMG leg then compares the two byte images directly:
      *> both groups hold "a", -1234 and "z", and §14.9.25.4 GR4 makes the
      *> group move to an alphanumeric item a byte transfer, so X1 = X2 is a
      *> comparison of the stored bytes, not of the described values.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SYN01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77 K1 PIC S9(4) COMP SYNC.
       77 K2 PIC S9(4) COMP SYNCHRONIZED.
       01 E1.
          05 E1A PIC X VALUE "a".
          05 E1B PIC S9(4) COMP SYNC VALUE -1234.
          05 E1C PIC X VALUE "z".
       01 E2.
          05 E2A PIC X VALUE "a".
          05 E2B PIC S9(4) COMP SYNCHRONIZED VALUE -1234.
          05 E2C PIC X VALUE "z".
       01 LF1.
          05 LF1A PIC X.
          05 LF1B PIC S9(4) COMP SYNC LEFT.
          05 LF1C PIC X.
       01 LF2.
          05 LF2A PIC X.
          05 LF2B PIC S9(4) COMP SYNCHRONIZED LEFT.
          05 LF2C PIC X.
       01 RT1.
          05 RT1A PIC X.
          05 RT1B PIC S9(4) COMP SYNC RIGHT.
          05 RT1C PIC X.
       01 RT2.
          05 RT2A PIC X.
          05 RT2B PIC S9(4) COMP SYNCHRONIZED RIGHT.
          05 RT2C PIC X.
       01 GRA SYNC.
          05 GRA-A PIC X.
          05 GRA-N PIC S9(4) COMP.
          05 GRA-Z PIC X.
       01 GRB SYNCHRONIZED.
          05 GRB-A PIC X.
          05 GRB-N PIC S9(4) COMP.
          05 GRB-Z PIC X.
       01 X1 PIC X(4).
       01 X2 PIC X(4).
       01 IMGF PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "L77=" FUNCTION BYTE-LENGTH(K1)
               " " FUNCTION BYTE-LENGTH(K2)
           DISPLAY "ELEM=" FUNCTION BYTE-LENGTH(E1)
               " " FUNCTION BYTE-LENGTH(E2)
           DISPLAY "LEFT=" FUNCTION BYTE-LENGTH(LF1)
               " " FUNCTION BYTE-LENGTH(LF2)
           DISPLAY "RIGHT=" FUNCTION BYTE-LENGTH(RT1)
               " " FUNCTION BYTE-LENGTH(RT2)
           DISPLAY "GROUP=" FUNCTION BYTE-LENGTH(GRA)
               " " FUNCTION BYTE-LENGTH(GRB)
           MOVE E1 TO X1
           MOVE E2 TO X2
           MOVE "NO" TO IMGF
           IF X1 = X2
               MOVE "YES" TO IMGF
           END-IF
           DISPLAY "IMG=" IMGF
           STOP RUN.
