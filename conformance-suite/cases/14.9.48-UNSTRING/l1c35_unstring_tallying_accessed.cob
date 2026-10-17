      *>--- conformance-case
      *> id: l1c35_unstring_tallying_accessed
      *> rules: [GR-14.9.48.4-6, GR-14.9.48.4-14]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.48.4 GR6 + GR14 — TALLYING counts receivers accessed
      *> GR6: "The data item referenced by identifier-8 is a counter
      *>   that is
      *> incremented by 1 for each occurrence of the data item
      *>   referenced by
      *> identifier-4 accessed during the UNSTRING operation."
      *> OK  §14.9.48.4 6)  (General rules)
      *> GR14: "... the content of the data item referenced by
      *>   identifier-8
      *> contains a value equal to its value at the beginning of the
      *> execution of the statement plus a value equal to the number of
      *> identifier-4 receiving data items accessed during execution of
      *>   the
      *> statement."
      *> OK  §14.9.48.4 14)  (General rules)
      *> Which receivers are accessed:
      *> OK  §14.9.48.4 11) g) "... is repeated until either all the
      *>     characters are exhausted in the data item referenced by
      *>     identifier-1, or until there are no more receiving areas"
      *>     (its "12b through 12f" cross-reference is a typo for
      *>       11b-11f)
      *> OK  §14.9.48.4 15) b) "all receiving areas have been acted
      *>   upon,
      *>     and the data item referenced by identifier-1 contains
      *>     characters that have not been examined"
      *> OK  §14.9.48.4 11) b) "If the DELIMITED BY phrase is not
      *>   specified,
      *>     the number of characters examined is equal to the size of
      *>       the
      *>     current receiving area"
      *> 15 a) (pointer < 1) is an overflow at initiation: nothing is
      *>   moved.
      *> Derivation (receivers X1..X4 PIC XX preset to "zz"):
      *> T1 T=05  "AA,BB,CC" BY "," INTO X1 X2 X3: 3 accessed -> 5+3 =
      *>   08.
      *> T2 T=00  "AA,BB" INTO X1..X4: S exhausted after X2 -> 02; X3
      *>   and
      *>          X4 not accessed and keep "zz".
      *> T3 T=07  "AB,CD," INTO X1 X2 X3: the trailing "," is the last
      *>          character, so S is exhausted after X2 -> 7+2 = 09, X3
      *>            zz.
      *> T4 T=04  WITH POINTER 0: overflow at initiation (15a), no
      *>   receiver
      *>          accessed -> 04, OVF.
      *> T5 T=00  "A,B,C" INTO X1 X2: both accessed, "C" unexamined ->
      *>          overflow (15b) -> 02, OVF.
      *> T6 T=00  "ABCDEFG" without DELIMITED BY INTO Y1 Y2 Y3 (PIC
      *>   X(3)):
      *>          3 characters each, Y3 gets "G" -> 03.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C35C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S8 PIC X(8) VALUE "AA,BB,CC".
       01 S5 PIC X(5) VALUE "AA,BB".
       01 S6 PIC X(6) VALUE "AB,CD,".
       01 S5B PIC X(5) VALUE "A,B,C".
       01 S7 PIC X(7) VALUE "ABCDEFG".
       01 X1 PIC XX.
       01 X2 PIC XX.
       01 X3 PIC XX.
       01 X4 PIC XX.
       01 Y1 PIC X(3).
       01 Y2 PIC X(3).
       01 Y3 PIC X(3).
       01 T  PIC 99.
       01 P  PIC 99.
       PROCEDURE DIVISION.
           MOVE 5 TO T
           PERFORM PRESET
           UNSTRING S8 DELIMITED BY "," INTO X1 X2 X3 TALLYING IN T
           END-UNSTRING
           DISPLAY "T1 T=" T " [" X1 X2 X3 "]"
           MOVE 0 TO T
           PERFORM PRESET
           UNSTRING S5 DELIMITED BY "," INTO X1 X2 X3 X4 TALLYING IN T
           END-UNSTRING
           DISPLAY "T2 T=" T " [" X1 X2 X3 X4 "]"
           MOVE 7 TO T
           PERFORM PRESET
           UNSTRING S6 DELIMITED BY "," INTO X1 X2 X3 TALLYING IN T
           END-UNSTRING
           DISPLAY "T3 T=" T " [" X1 X2 X3 "]"
           MOVE 4 TO T
           MOVE 0 TO P
           PERFORM PRESET
           UNSTRING S8 DELIMITED BY "," INTO X1 X2
               WITH POINTER P TALLYING IN T
               ON OVERFLOW DISPLAY "T4 OVF"
           END-UNSTRING
           DISPLAY "T4 T=" T " [" X1 X2 "]"
           MOVE 0 TO T
           PERFORM PRESET
           UNSTRING S5B DELIMITED BY "," INTO X1 X2 TALLYING IN T
               ON OVERFLOW DISPLAY "T5 OVF"
           END-UNSTRING
           DISPLAY "T5 T=" T " [" X1 X2 "]"
           MOVE 0 TO T
           UNSTRING S7 INTO Y1 Y2 Y3 TALLYING IN T
           END-UNSTRING
           DISPLAY "T6 T=" T " [" Y1 Y2 Y3 "]"
           STOP RUN.
       PRESET.
           MOVE "zz" TO X1 X2 X3 X4.
