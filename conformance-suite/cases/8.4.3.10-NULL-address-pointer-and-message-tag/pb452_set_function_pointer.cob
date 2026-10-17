*>--- conformance-case
*> id: pb452_set_function_pointer
*> rules: [GR-8.4.3.10.4-2, SR-8.4.3.12.3-2, SR-8.4.3.12.3-3,
*>    GR-8.4.3.12.4-1, GR-8.4.3.12.4-2, GR-8.4.3.12.4-3,
*>    GR-13.18.60.4-26, SR-14.9.39.3-20, GR-14.9.39.4-14,
*>    GR-14.9.39.4-15, DOC-A.1-174, DOC-A.1-210]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 §14.9.39.2 Format 8 (function-pointer-assignment) end to end, at the edition the
      *> category was introduced. kb/Work PB452 + PB817.
      *>
      *> §13.18.60.4 GR26 — "A data description entry that specifies the USAGE FUNCTION-POINTER clause
      *> specifies a function-pointer data item … that may contain the address of a function"; the TO phrase is
      *> unbracketed in the printed general format (folio 503), so FP1/FP2 are restricted to PBF8DBL.
      *> §8.4.3.12.4 GR1 b) / GR2 — ADDRESS OF FUNCTION function-prototype-name-1 yields the address of the
      *> function identified by the externalized function-name in its FUNCTION-ID paragraph.
      *> §14.9.39.4 GR14 — "The address identified by identifier-13 is stored in each data item referenced by
      *> identifier-12 in the order specified."
      *> §8.8.4.2.16 — two function-pointers are equal when they identify the same function, or are both NULL.
      *> §8.4.3.10.4 GR2 — the predefined address NULL is guaranteed not to be any function's address.
      *>
      *> EXPECTED OUTPUT, derived line by line:
      *>   BOTH-SET   FP1 and FP2 both hold PBF8DBL's address after the two receivers are set in one statement
      *>              (GR14's "each data item referenced by identifier-12"), so neither equals NULL.
      *>   SAME       FP3 took FP1's address through the plain Format-8 carrier copy, so FP3 = FP1 (§8.8.4.2.16).
      *>   NULL1      SET FP1 TO NULL stores the predefined NULL function address in FP1 only …
      *>   NOTNULL3   … and leaves FP3, an independent item, holding the address (GR14 changes only identifier-12).
      *>   OF-OPTIONAL  `SET FP2 TO ADDRESS FUNCTION PBF8DBL` — OF is NOT underlined in the printed
      *>              §8.4.3.12.2 figure (folio 141), so §8.3.2.4.3 makes the OF-less spelling conforming and it
      *>              must produce the very same address: FP2 = FP3 again.
      *>
      *> BEFORE/AFTER 000000042 IS THE ANNEX A.1 ITEM 174 WITNESS (docs/CONFORMANCE.md §7 row DOC-A.1-174).
      *> §14.9.39.4 GR15 makes "the effect of the SET statement on the function whose address is being stored
      *> in the function-pointer" implementor-defined, and this implementation's determination is NONE: the
      *> function is neither activated nor initialized nor otherwise disturbed. Activating PBF8DBL(21) before
      *> the first SET and after the last one must therefore print the identical 000000042 both times.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PBF8DBL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-ARG PIC S9(4).
       01 L-RES PIC S9(9).
       PROCEDURE DIVISION USING L-ARG RETURNING L-RES.
           COMPUTE L-RES = L-ARG * 2
           GOBACK.
       END FUNCTION PBF8DBL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB452F8.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PBF8DBL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FP1 USAGE FUNCTION-POINTER TO PBF8DBL.
       01 FP2 USAGE FUNCTION-POINTER TO PBF8DBL.
       01 FP3 USAGE FUNCTION-POINTER TO PBF8DBL.
       01 WS-R PIC 9(9).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE WS-R = FUNCTION PBF8DBL(21)
           DISPLAY "BEFORE " WS-R
           SET FP1 FP2 TO ADDRESS OF FUNCTION PBF8DBL
           IF FP1 NOT = NULL AND FP2 NOT = NULL
               DISPLAY "BOTH-SET"
           ELSE
               DISPLAY "NOT-BOTH-SET"
           END-IF
           SET FP3 TO FP1
           IF FP3 = FP1
               DISPLAY "SAME"
           ELSE
               DISPLAY "DIFF"
           END-IF
           SET FP1 TO NULL
           IF FP1 = NULL
               DISPLAY "NULL1"
           ELSE
               DISPLAY "NOTNULL1"
           END-IF
           IF FP3 = NULL
               DISPLAY "NULL3"
           ELSE
               DISPLAY "NOTNULL3"
           END-IF
           SET FP2 TO ADDRESS FUNCTION PBF8DBL
           IF FP2 = FP3
               DISPLAY "OF-OPTIONAL"
           ELSE
               DISPLAY "OF-REJECTED"
           END-IF
           COMPUTE WS-R = FUNCTION PBF8DBL(21)
           DISPLAY "AFTER " WS-R
           STOP RUN.
       END PROGRAM PB452F8.
