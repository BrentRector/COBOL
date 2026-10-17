*>--- conformance-case
*> id: dyn_capacity_set
*> rules: [SR-14.9.39.3-29, SR-14.9.39.3-30, GR-14.9.39.4-30]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> OCCURS DYNAMIC SET Format 14 (increment 2, D9; ISO 14.9.39 GR29): the CAPACITY register is set / raised /
      *> lowered. Lowering below the FROM minimum clamps to it (8.5.1.9.4). FROM 2: INIT 2 -> TO 7 -> UP BY 2 = 9 ->
      *> DOWN BY 3 = 6 -> DOWN BY 100 clamps to the minimum 2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DYN-CAP-SET.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ED       PIC ZZ9.
       01 WS-TABLE.
          05 WS-E PIC 9(3) OCCURS DYNAMIC CAPACITY IN WS-CAP FROM 2 TO 10.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE WS-CAP TO ED.
           DISPLAY "INIT=[" ED "]".
           SET WS-CAP TO 7.
           MOVE WS-CAP TO ED.
           DISPLAY "TO=[" ED "]".
           SET WS-CAP UP BY 2.
           MOVE WS-CAP TO ED.
           DISPLAY "UP=[" ED "]".
           SET WS-CAP DOWN BY 3.
           MOVE WS-CAP TO ED.
           DISPLAY "DOWN=[" ED "]".
           SET WS-CAP DOWN BY 100.
           MOVE WS-CAP TO ED.
           DISPLAY "CLAMP=[" ED "]".
           STOP RUN.
