*>--- conformance-case
*> id: pb415_initialize_function_pointer
*> rules: [SR-14.9.20.3-3, SR-14.9.20.3-8, GR-14.9.20.4-2, GR-14.9.20.4-4, GR-14.9.20.4-5, GR-14.9.20.4-6, FMT-14.9.20.2]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PBF415D.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-ARG PIC 9(9).
       01 L-RES PIC 9(9).
       PROCEDURE DIVISION USING L-ARG RETURNING L-RES.
       F-MAIN.
           COMPUTE L-RES = L-ARG * 2
           GOBACK.
       END FUNCTION PBF415D.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB415FP.
      *> kb/Work PB415 — the COBOL-2014 INITIALIZE category-name FUNCTION-POINTER (ISO §14.9.20.2), at its own
      *> introduction edition. §8.9 reserves FUNCTION-POINTER from 2014, which is the same edition that
      *> introduced the USAGE FUNCTION-POINTER category (§13.18.60); below 2014 the word is refused
      *> COBOLNET0900 by the initialize-category-2014 construct row.
      *>
      *> ⛔ FUNCTION-POINTER IS THE ASYMMETRIC ONE, AND THE ASYMMETRY IS THE PRINTED STANDARD (PB418 verified it
      *> against the licensed PDF at folio 639): §14.9.20.4 GR4's SET-form list, GR6a1's sender list and GR6c's
      *> fill table all name function-pointer, and GR5c1a alone does NOT. With §13.18.63.3 SR9 forbidding a VALUE
      *> clause on a FUNCTION-POINTER item, GR5c1b and GR5c1c are false too — so a function-pointer is NOT a
      *> receiving-operand under the VALUE phrase, and IS one under REPLACING (GR5c2), DEFAULT (GR5c3) and the
      *> bare form (GR5c4).
      *>
      *> EXPECTED, DERIVED FROM THE RULES ABOVE AND WRITTEN DOWN BEFORE THE RUN:
      *>   1[SET]   REPLACING FUNCTION-POINTER … BY FP1 qualifies FP2 through GR5c2 and GR6b gives it FP1 —
      *>            the implicit `SET FP2 TO FP1` of GR4. Before PB415 the category-name did not parse.
      *>   1B[SAME] the receiver holds exactly identifier-2's value.
      *>   2[SET]   ⛔ ALL TO VALUE leaves it ALONE — GR5c1a omits function-pointer, so the item is not a
      *>            receiving-operand under the VALUE phrase and keeps line 1's value. This is the one line that
      *>            would flip if the GR5c1a list were "evened up" with GR6a1's.
      *>   3[NULL]  the BARE form (GR5c4) does reach it, and GR6c's fill table gives the predefined address NULL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PBF415D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FP1 USAGE FUNCTION-POINTER TO PBF415D.
       01 FP2 USAGE FUNCTION-POINTER TO PBF415D.
       PROCEDURE DIVISION.
       MAIN.
           SET FP1 TO ADDRESS OF FUNCTION PBF415D
           SET FP2 TO NULL
           INITIALIZE FP2 REPLACING FUNCTION-POINTER DATA BY FP1
           IF FP2 = NULL THEN DISPLAY "1[NULL]" ELSE DISPLAY "1[SET]" END-IF
           IF FP2 = FP1 THEN DISPLAY "1B[SAME]" ELSE DISPLAY "1B[OTHER]" END-IF

           INITIALIZE FP2 ALL TO VALUE
           IF FP2 = NULL THEN DISPLAY "2[NULL]" ELSE DISPLAY "2[SET]" END-IF

           INITIALIZE FP2
           IF FP2 = NULL THEN DISPLAY "3[NULL]" ELSE DISPLAY "3[SET]" END-IF
           STOP RUN.
       END PROGRAM PB415FP.
