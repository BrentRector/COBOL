      *> kb/Work PB2518 - ISO/IEC 1989:2023 section 13.18.44.3 SR10: "The entries giving the new descriptions of the
      *> storage area shall follow the entries defining the area of data-name-2, without intervening entries that
      *> define new storage areas."
      *>   cite.py --check 13.18.44.3 "without intervening entries that define new storage areas" -> OK  10)
      *> SR7 makes several redefinitions of one area each name the entry that originally defined it, so a
      *> redefinition between data-name-2 and the subject is NOT an entry that defines a new storage area, and a
      *> condition-name (level 88) or the subordinates of a redefinition are not entries between them either. The
      *> negative twins are conformance/negative/pb2518-redefines-past-intervening-storage and ...-roots.
      *>
      *> DERIVATION. Group R holds A (four characters, "ABCD"), then the redefinitions B (a group of B1 and B2, two
      *> characters each) and C (two characters), both redefining A, with the 88 AX under A and the subordinates
      *> under B between A and C; D follows them as new storage. The same shape at the roots: W, then W2 and W3.
      *> L1: B1 and B2 are the first and last two characters of A: AB CD.
      *> L2: C starts at the first character of A (13.18.44.4 GR1): AB. The 88 AX is true while A is "ABCD": yes.
      *> L3: MOVE "WXYZ" TO A is seen through B2 as YZ; AX is then false: no. D is its own storage, still Z.
      *> L4: W2 is W's two characters and W3 its first: xy x. MOVE "pq" TO W2 makes W "pq" and W3 "p".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2518OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          05 A PIC X(4) VALUE "ABCD".
             88 AX VALUE "ABCD".
          05 B REDEFINES A.
             10 B1 PIC XX.
             10 B2 PIC XX.
          05 C REDEFINES A PIC XX.
          05 D PIC X VALUE "Z".
       01 W PIC XX VALUE "xy".
       01 W2 REDEFINES W PIC XX.
       01 W3 REDEFINES W PIC X.
       PROCEDURE DIVISION.
           DISPLAY "L1=" B1 " " B2
           IF AX
              DISPLAY "L2=" C " yes"
           ELSE
              DISPLAY "L2=" C " no"
           END-IF
           MOVE "WXYZ" TO A
           IF AX
              DISPLAY "L3=" B2 " yes " D
           ELSE
              DISPLAY "L3=" B2 " no " D
           END-IF
           DISPLAY "L4a=" W2 " " W3
           MOVE "pq" TO W2
           DISPLAY "L4b=" W " " W3
           STOP RUN.
