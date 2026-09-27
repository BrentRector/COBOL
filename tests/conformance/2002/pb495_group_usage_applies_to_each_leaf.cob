      *> kb/Work PB495 — ISO §13.18.60.4 GR1: "If the USAGE clause is specified or implied at a group level,
      *> it applies only to each elementary item in the group. Unless the GROUP-USAGE clause is also specified
      *> or implied, the USAGE clause applies only to each elementary item in the group and not to the group
      *> itself." So a PICTURE-LESS elementary item under a group that wrote a picture-less usage IS an item of
      *> that usage — §13.16.3 SR8 makes the PICTURE not merely optional but PROHIBITED there ("the PICTURE
      *> clause shall not be specified ... for an item whose usage is binary-char, binary-short, binary-long,
      *> binary-double, float-short, float-long, float-extended, index, ...") — and the group is the sum of
      *> those items, never a scalar of the usage's own type.
      *>
      *> EXPECTED VALUES, DERIVED — every width is fixed by the usage, not by any picture:
      *>   BINARY-CHAR/-SHORT/-LONG/-DOUBLE = 1/2/4/8 bytes. §13.18.60.4 GR12 fixes the minimum ranges
      *>     (-2**7..2**7, -2**15..2**15, -2**31..2**31, -2**63..2**63 signed) and GR21 leaves the length to
      *>     the implementor; COBOL.NET's determination is the two's-complement width that exactly spans each
      *>     range (docs/CONFORMANCE.md — 1, 2, 4 and 8 bytes).
      *>   FLOAT-SHORT / FLOAT-LONG = 4/8 bytes. §13.18.60.4 GR13 leaves "the size and permitted range" to the
      *>     implementor with float-short <= float-long; COBOL.NET maps them to IEEE binary32 / binary64.
      *>   INDEX = 8 bytes. §13.18.60.4 GR10 leaves an index data item's representation to the implementor;
      *>     COBOL.NET's is the 8-byte occurrence number.
      *>   The GROUP's byte length is the sum of its leaves: G-TWO holds two BINARY-SHORT items = 4.
      *>   The DISPLAY images on the V= line are the same derivation read as DIGIT POSITIONS: GR12's
      *>     minimum ranges span 3 / 5 / 10 / 19 decimal digits for binary-char/-short/-long/-double, so a
      *>     BINARY-SHORT item renders 5 digits (12 -> "00012") and a BINARY-LONG item 10 with the sign a
      *>     non-DISPLAY usage has no zoned overpunch for (-1 -> "-0000000001"; docs/CONFORMANCE.md
      *>     DOC-A.1-56). The inherited A-SHORT and the written-clause C-SHORT print IDENTICALLY: that is
      *>     GR1's whole content.
      *> Before PB495 each of these leaves was a ZERO-LENGTH cell with no diagnostic, the FLOAT groups emitted
      *> the GROUP as a scalar float (a raw Roslyn CS1061 on every member reference), and `MOVE 12 TO A-SHORT`
      *> crashed the compiler with a NullReferenceException in MoveEmitter.
      *>
      *> COBOL-2002: BINARY-CHAR/-SHORT/-LONG/-DOUBLE and FLOAT-SHORT/-LONG are 2002 additions (COBOLNET0900
      *> below it); USAGE INDEX is COBOL-85. The 2023 twin adds nothing this leg does not already pin.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB495GR1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G-CHAR   USAGE BINARY-CHAR.
           05 A-CHAR.
       01 G-SHORT  USAGE BINARY-SHORT.
           05 A-SHORT.
       01 G-LONG   USAGE BINARY-LONG.
           05 A-LONG.
       01 G-DOUBLE USAGE BINARY-DOUBLE.
           05 A-DOUBLE.
       01 G-FSHORT USAGE FLOAT-SHORT.
           05 A-FSHORT.
       01 G-FLONG  USAGE FLOAT-LONG.
           05 A-FLONG.
       01 G-INDEX  USAGE INDEX.
           05 A-INDEX.
       01 G-TWO    USAGE BINARY-SHORT.
           05 A-ONE.
           05 A-TWO.
      *> The control: the same items with the clause written on the ELEMENTARY entry instead of the group.
      *> GR1 makes the two spellings the same item, so every width below matches its inherited twin.
       01 C-GROUP.
           05 C-SHORT USAGE BINARY-SHORT.
       01 T-IX.
           05 T-E OCCURS 5 INDEXED BY T-X PIC X.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 12 TO A-SHORT
           MOVE -1 TO A-LONG
           MOVE 12 TO C-SHORT
      *> The index data item is set FROM AN INDEX-NAME: ISO 14.9.39.3 SR3 forbids
      *> arithmetic-expression-1 when identifier-1 is of class index (kb/Work PB212).
           SET T-X TO 3
           SET A-INDEX TO T-X
           DISPLAY "W=" FUNCTION BYTE-LENGTH(A-CHAR)
               " " FUNCTION BYTE-LENGTH(A-SHORT)
               " " FUNCTION BYTE-LENGTH(A-LONG)
               " " FUNCTION BYTE-LENGTH(A-DOUBLE)
               " " FUNCTION BYTE-LENGTH(A-FSHORT)
               " " FUNCTION BYTE-LENGTH(A-FLONG)
               " " FUNCTION BYTE-LENGTH(A-INDEX)
           DISPLAY "GRP=" FUNCTION BYTE-LENGTH(G-TWO)
               " CTL=" FUNCTION BYTE-LENGTH(C-SHORT)
               " " FUNCTION BYTE-LENGTH(C-GROUP)
           DISPLAY "V=" A-SHORT " " A-LONG " " C-SHORT
      *> Compared with the INDEX-NAME it was set from: ISO 8.8.4.2.13 row 3 pairs an index data item only
      *> with "an index-name or another index data item" - `A-INDEX = 3` is outside that list (kb/Work PB1468).
           IF A-INDEX = T-X
               DISPLAY "IDX=OK"
           ELSE
               DISPLAY "IDX=BAD"
           END-IF
           STOP RUN.
