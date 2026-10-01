      *> ISO 1989:2014 §13.18.43 (RECORD clause Format 1) over a VARIABLE-LENGTH GROUP record (§8.5.1.12.1).
      *> The record below holds two DYNAMIC LENGTH members around a fixed one, inside a file described with the
      *> fixed-length clause RECORD CONTAINS 20 CHARACTERS. It is LEGAL: §13.18.43.3 SR3 - "No record description entry
      *> for the file may specify a number of bytes greater than integer-1" - bounds the description by its
      *> MAXIMUM (§13.18.43.4 GR8 b), by the determination D-FRA (iv) that a dynamic-length item counts at its maximum size) - that maximum is its LIMIT (§8.5.1.10.1): 9 + 3 + 8 = 20
      *> bytes, EXACTLY integer-1 (the negative pb1562-record-contains-limit-one-over has 10 + 3 + 8 = 21). (Without the LIMIT phrases the maximum is the implementor's, 1,073,741,791 characters per
      *> item, and the entry is refused by SR3 - tests/conformance/negative/pb1562-record-contains-unbounded-dynamic.cob.)
      *> A variable-length group "behaves in all respects as though it were in fact contiguous with its neighbors"
      *> (§8.5.1.11.2), so each record is the concatenation of its members as they stand at the WRITE, and a READ
      *> makes that very record available again: every member takes back exactly the characters it was written
      *> with. The standard leaves the physical form to the implementor (§13.18.43.4 GR2: "The size of records on
      *> physical storage media may be different due to control information required by the operating
      *> environment"); docs/CONFORMANCE.md D-FRA (vi) fixes it for a FIXED-length file. The contiguous image alone
      *> cannot be inverted with two variable members ("AAA" "KEY" "CCCC" and "AAAK" "EYC" "CCC" are the same ten
      *> characters), and a Format 1 file is plain 20-byte blocks (GR6: no frame can carry a table that a second
      *> program describing the file as PIC X(20) would not misread), so each member sits at the position it has
      *> at its maximum size, space padded, and a READ takes it back at that width and drops the padding. Before
      *> kb/Work PB1562 the first record came back as A=[AAAKEYCCCC       ] KY=[   ] C=[].
      *>
      *> WHY EACH LEG CAN FAIL: the members are overwritten with junk before every READ, so a value that is right
      *> can only have come from the file. R1 has both variable members non-empty; R2 shifts the boundary (1 and 6
      *> characters, the same fixed member in a different place); R3 has BOTH variable members empty, the minimum
      *> the description describes (3 bytes).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1562FX.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1562fx.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F RECORD CONTAINS 20 CHARACTERS.
       01  R.
           05 A  PIC X DYNAMIC LENGTH LIMIT 9.
           05 KY PIC X(3).
           05 C  PIC X DYNAMIC LENGTH LIMIT 8.
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           MOVE "AAA" TO A.
           MOVE "KEY" TO KY.
           MOVE "CCCC" TO C.
           WRITE R.
           DISPLAY "W1 " FS.
           MOVE "B" TO A.
           MOVE "K2K" TO KY.
           MOVE "DDDDDD" TO C.
           WRITE R.
           DISPLAY "W2 " FS.
           MOVE "" TO A.
           MOVE "K3K" TO KY.
           MOVE "" TO C.
           WRITE R.
           DISPLAY "W3 " FS.
           CLOSE F.
           OPEN INPUT F.
           MOVE "ZZZZZZZZ" TO A.
           MOVE "???" TO KY.
           MOVE "Y" TO C.
           READ F.
           DISPLAY "R1 A=[" A "] KY=[" KY "] C=[" C "] " FS.
           MOVE "ZZZZZZZZ" TO A.
           MOVE "???" TO KY.
           MOVE "Y" TO C.
           READ F.
           DISPLAY "R2 A=[" A "] KY=[" KY "] C=[" C "] " FS.
           MOVE "ZZZZZZZZ" TO A.
           MOVE "???" TO KY.
           MOVE "Y" TO C.
           READ F.
           DISPLAY "R3 A=[" A "] KY=[" KY "] C=[" C "] " FS.
           CLOSE F.
           STOP RUN.
