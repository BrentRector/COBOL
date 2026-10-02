      *> kb/Work PB1907, docs/CONFORMANCE.md D-INS4. ISO 14.9.22.4 GR21
      *> and Annex A.2 item 21 f): an INSPECT CONVERTING whose
      *> from-set, to-set or BEFORE/AFTER identifier occupies the same
      *> storage as identifier-1. The standard leaves the result
      *> undefined:
      *>   cite.py --check 14.9.22.4 "If identifier-4, identifier-6, or
      *>   identifier-7 occupies the same storage area as identifier-1,
      *>   the result of the execution of this statement is undefined,
      *>   even if they are defined by the same data description entry."
      *>   -> OK 14.9.22.4 21)
      *>   cite.py --check A.2 "the CONVERTING identifier, TO
      *>   identifier, AFTER identifier, or BEFORE identifier occupies
      *>   the same storage area as the INSPECT identifier"
      *>   -> OK A.2 21) f)
      *>   cite.py --check 4.4 "A COBOL run unit that allows these
      *>   situations to happen is a conforming run unit" -> OK 4.4 2)
      *> This golden pins a DOCUMENTED IMPLEMENTOR CHOICE (GnuCOBOL 3.2,
      *> measured): the from-set, the to-set and the BEFORE/AFTER
      *> values are taken when the statement starts, the conversion
      *> reads identifier-1's image from that moment and the one store
      *> at the end is the only write, so a set never observes a
      *> conversion made by its own statement. Where a character is
      *> repeated in the from-set the first occurrence decides (GR23).
      *> EXPECTED (X5 = "ABCA", X6 = "ABBA", X7 = "ABAB", X8 = "ABBA"):
      *>   F1  CONVERTING X5 TO "WXYZ": the from-set is "ABCA", so
      *>       A->W B->X C->Y, the second A is ignored   [WXYW]
      *>   F2  CONVERTING "AB" TO X6(3:2): the to-set is "BA"
      *>       -> A->B B->A                             [BAAB]
      *>   F3  CONVERTING "AB" TO "BA" AFTER X7(1:1): the delimiter is
      *>       "A", the region starts after the first one: "BAB" ->
      *>       "ABA", so "A" + "ABA"                    [AABA]
      *>   F4  CONVERTING X8(2:2) TO "XY": the from-set is "BB", B->X
      *>                                                [AXXA]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1907IC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X5   PIC X(4) VALUE "ABCA".
       01 X6   PIC X(4) VALUE "ABBA".
       01 X7   PIC X(4) VALUE "ABAB".
       01 X8   PIC X(4) VALUE "ABBA".
       PROCEDURE DIVISION.
       MAIN.
           INSPECT X5 CONVERTING X5 TO "WXYZ"
           DISPLAY "F1=[" X5 "]"
           INSPECT X6 CONVERTING "AB" TO X6(3:2)
           DISPLAY "F2=[" X6 "]"
           INSPECT X7 CONVERTING "AB" TO "BA" AFTER X7(1:1)
           DISPLAY "F3=[" X7 "]"
           INSPECT X8 CONVERTING X8(2:2) TO "XY"
           DISPLAY "F4=[" X8 "]"
           STOP RUN.
