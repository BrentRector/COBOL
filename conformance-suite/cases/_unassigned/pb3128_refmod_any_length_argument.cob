*>--- conformance-case
*> id: pb3128_refmod_any_length_argument
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3128 - a REFERENCE-MODIFIED slice of an ANY LENGTH item is a legal argument whose
      *> extent is known only at run time. 13.18.2.4 GR1 b): the formal is "treated as though there were n
      *> repetitions of the picture symbol ... where n is the length of the corresponding argument" (OK
      *> 13.18.2.4 1) b)); 8.4.3.3.4 GR5: "Reference modification creates a unique data item that is a
      *> subset of the data item referenced by identifier-1" (OK 8.4.3.3.4 5)), GR5 c) running the omitted
      *> length "up to and including the rightmost position of the data item referenced by identifier-1"
      *> (OK 8.4.3.3.4 5) c)). 14.8.2.2 rule 1 bounds a group formal by the argument's bytes (OK 14.8.2.2
      *> 1)), measured for a run-time slice by its MAXIMUM extent (docs/CONFORMANCE.md D-RM1 a)): an ANY
      *> LENGTH item's description fixes no maximum, so every group formal is admitted at compile time and
      *> the evaluated slice decides at the boundary. Before the fix the slice measured ZERO positions and
      *> the typed INVOKE was refused (COBOLNET0828 "larger than the argument (0)"), and the 14.9.25.4 GR1
      *> intermediate of MOVE LA(2:5) TO X1 X2 held ONE character ("x     " twice).
      *> DERIVATION (G = "ABCDEFGHIJ" is LA, a 10-character argument; LG is a 4-character group; TG
      *> displays LG then moves "wxyz" to it):
      *>   typed  LA(1:M) M=6   6 >= 4, shared   -> TG:ABCD|  la=wxyzEFGHIJ
      *>   typed  LA(3:)        8 >= 4, shared   -> TG:yzEF|  la=wxwxyzGHIJ
      *>   typed  LA(1:M) M=2   2 < 4: a copy    -> TG:AB  |  la=wxCDEFGHIJ (write-back LA(1:2))
      *>   MOVE LA(2:5) TO X1 X2 (PIC X(6))      -> x1=xCDEF |x2=xCDEF |
      *>   universal LA(3:) of "ABCDEFGHIJ"      -> TG:CDEF|  la=ABwxyzGHIJ
      *>   g (the caller's G, which LA occupies) -> g=ABwxyzGHIJ
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3128AL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C3128AL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T  USAGE OBJECT REFERENCE C3128AL.
       01 G  PIC X(10) VALUE "ABCDEFGHIJ".
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE C3128AL "NEW" RETURNING T
           INVOKE T "TA" USING G
           DISPLAY "g=" G
           STOP RUN.
       END PROGRAM PB3128AL.

       IDENTIFICATION DIVISION.
       CLASS-ID. C3128AL INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS C3128AL.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG.
          05 LG1 PIC X(4).
       PROCEDURE DIVISION USING LG.
           DISPLAY "TG:" LG "|"
           MOVE "wxyz" TO LG.
       END METHOD TG.
       METHOD-ID. TA.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 M  PIC 9(2).
       01 U  USAGE OBJECT REFERENCE.
       01 X1 PIC X(6).
       01 X2 PIC X(6).
       LINKAGE SECTION.
       01 LA PIC X ANY LENGTH.
       PROCEDURE DIVISION USING LA.
           MOVE 6 TO M
           INVOKE SELF "TG" USING LA(1:M)
           DISPLAY "la=" LA
           INVOKE SELF "TG" USING LA(3:)
           DISPLAY "la=" LA
           MOVE "ABCDEFGHIJ" TO LA
           MOVE 2 TO M
           INVOKE SELF "TG" USING LA(1:M)
           DISPLAY "la=" LA
           MOVE LA(2:5) TO X1 X2
           DISPLAY "x1=" X1 "|x2=" X2 "|"
           MOVE "ABCDEFGHIJ" TO LA
           SET U TO SELF
           INVOKE U "TG" USING LA(3:)
           DISPLAY "la=" LA.
       END METHOD TA.
       END OBJECT.
       END CLASS C3128AL.
