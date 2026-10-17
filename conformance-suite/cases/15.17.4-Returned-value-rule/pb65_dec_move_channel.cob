*>--- conformance-case
*> id: pb65_dec_move_channel
*> rules: [RV-15.17.4-1, RV-15.27.3-3, RV-15.35.4-1, RV-15.73.3-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.4.1 r1 / §14.9.25 — the MOVE channel of a STANDARD-DECIMAL function result
      *> (fix-queue PB65). MOVE was the one numeric consumer without the Dec-carrier store case, so
      *> MOVE FUNCTION E failed at backend compile (CS1503) while COMPUTE was exact. Now both
      *> channels land through the SDIDI final transfer: MOVE FUNCTION E into 9V9(30) is the
      *> 31-digit truncation of the exact §15.27.3 r3 constant; EXP10(30)/EXP10(15) is the §15.35.4
      *> r1 EAE evaluated in SDIDI form = 10^15 exactly; and COMBINED-DATETIME of a COMP-2 seconds
      *> value agrees byte-for-byte between MOVE and COMPUTE (the §8.8.1.5.1 lift is the same lift).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB56RES.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 EW  PIC 9V9(30).
       01 CF2 COMP-2.
       01 M13 PIC 9(6)V9(10).
       01 C13 PIC 9(6)V9(10).
       01 RT  PIC 9(16).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION E TO EW
           DISPLAY "MOVE-E=" EW
           COMPUTE RT = FUNCTION EXP10(30) / FUNCTION EXP10(15)
           DISPLAY "RATIO =" RT
           MOVE 45296.3 TO CF2
           MOVE FUNCTION COMBINED-DATETIME(143951, CF2) TO M13
           COMPUTE C13 = FUNCTION COMBINED-DATETIME(143951, CF2)
           DISPLAY "MOVE13=" M13
           DISPLAY "COMP13=" C13
           STOP RUN.
