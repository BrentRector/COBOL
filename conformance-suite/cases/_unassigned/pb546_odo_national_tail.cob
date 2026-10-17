*>--- conformance-case
*> id: pb546_odo_national_tail
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB546 (the PB3030 geometry) - a variable-length group whose member
      *> holds an OCCURS DEPENDING table of NATIONAL elements as its trailing storage
      *> (ISO 13.18.38.3 SR22). The group's current extent counts the first N
      *> occurrences only (13.18.38.4 GR8), each occupying the two bytes a national
      *> position does (13.18.60.4 GR8, D-N1): H is HX + 2 x 2 bytes = 5 and G is
      *> DL's current 2 + 5 = 7 (15.50.4 r7). DISPLAY, a group MOVE (14.9.25.4 GR9)
      *> and a file record written and read back (8.5.1.11.2) carry exactly those
      *> bytes: a b Q, then 58 31 59 32, the UTF-16BE pairs of NX"5831" and
      *> NX"5932". Each national character is chosen so BOTH of its bytes are
      *> printable: the golden pins every byte as text, where a character such as
      *> N"X" (00 58) would put a NUL in it (NulByteDriftTests).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB546O.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb546o.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R.
          05 RDL PIC X DYNAMIC LENGTH.
          05 RGH.
             10 RGX PIC X.
             10 RNT PIC N OCCURS 1 TO 4 DEPENDING ON N.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 2.
       01 G.
          05 DL PIC X DYNAMIC LENGTH.
          05 H.
             10 HX PIC X.
             10 NT PIC N OCCURS 1 TO 4 DEPENDING ON N.
       01 G2.
          05 DL2 PIC X DYNAMIC LENGTH.
          05 H2.
             10 HX2 PIC X.
             10 NT2 PIC N OCCURS 1 TO 4 DEPENDING ON N.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ab" TO DL
           MOVE "Q" TO HX
           MOVE NX"5831" TO NT(1)
           MOVE NX"5932" TO NT(2)
           DISPLAY "LENGTH-G=" FUNCTION LENGTH(G)
           DISPLAY "LENGTH-H=" FUNCTION LENGTH(H)
           DISPLAY "G=[" G "]"
           MOVE G TO G2
           DISPLAY "G2=[" G2 "]"
           MOVE G TO R
           OPEN OUTPUT F
           WRITE R
           CLOSE F
           MOVE NX"5A5A" TO RNT(1)
           OPEN INPUT F
           READ F
           CLOSE F
           DISPLAY "R=[" R "]"
           DISPLAY "LENGTH-R=" FUNCTION LENGTH(R)
           STOP RUN.
