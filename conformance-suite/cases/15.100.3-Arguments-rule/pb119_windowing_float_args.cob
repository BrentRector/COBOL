*>--- conformance-case
*> id: pb119_windowing_float_args
*> rules: [AR-15.100.3-5]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB119 — the date-windowing functions' FLOAT lane with argument-3 OMITTED (ISO 15.100.3 r3: "If
      *> argument-2 is omitted, the function shall be evaluated as though 50 were specified" and the omitted
      *> argument-3 defaults to NUMVAL(CURRENT-DATE(1:4)) per r5 — the EXECUTION year). The defect passed the
      *> omitted argument-3 to the core as an explicit 0, which the r4 "greater than 1600" screen rejected: the
      *> 15.3 default 0 came back (or the run aborted under checking) on conforming source. The exact lane was
      *> correct — the two-arm dispatch shape (#6); the sweep fixed all three windowing twins.
      *> The float ARGUMENTS force the Real lane. Deterministic checks: the WRITTEN-argument-3 forms are
      *> clock-free and hand-derived per 15.100.4 r1 (maximum-year = arg2 + arg3 - 1; returned = arg1 + 100 *
      *> int((maximum-year - arg1) / 100)): YEAR-TO-YYYY(50.0 60 3000): max 3059, (3059-50)/100 -> 30 ->
      *> 50 + 3000 = 3050. The OMITTED form windows on the execution year: any run of this suite is past 2020,
      *> so YEAR-TO-YYYY(99.0) lies in (1900, 2200) — pre-fix it was 0.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB119WF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(9).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION YEAR-TO-YYYY(50.0 60 3000)
           IF R = 3050 DISPLAY "WRIT OK" ELSE DISPLAY "WRIT BAD " R END-IF
           COMPUTE R = FUNCTION YEAR-TO-YYYY(99.0)
           IF R > 1900 AND R < 2200
               DISPLAY "OMIT OK" ELSE DISPLAY "OMIT BAD " R END-IF
           COMPUTE R = FUNCTION DATE-TO-YYYYMMDD(590401.0 20 1985)
           IF R = 19590401 DISPLAY "DATE OK" ELSE DISPLAY "DATE BAD " R END-IF
           COMPUTE R = FUNCTION DAY-TO-YYYYDDD(59100.0 20 1985)
           IF R = 1959100 DISPLAY "DAY OK" ELSE DISPLAY "DAY BAD " R END-IF
           COMPUTE R = FUNCTION DATE-TO-YYYYMMDD(590401.0)
           IF R > 19000000 AND R < 22000000
               DISPLAY "DOMIT OK" ELSE DISPLAY "DOMIT BAD " R END-IF
           STOP RUN.
