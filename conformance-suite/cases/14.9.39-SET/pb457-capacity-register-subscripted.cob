*>--- conformance-case
*> id: pb457-capacity-register-subscripted
*> rules: [SR-14.9.39.3-29]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB457 - ISO 13.18.38.3 SR31, "Data-name-3 shall not be subscripted." The compiler used to
      *> refuse this with COBOLNET1639 "'C2(2)' is not defined", which is false: SR30's first sentence makes
      *> C2 a declared name of the source element. The REJECTION was right; only the reason was wrong.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB457-CAP-SUB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T0.
          05 IN-E PIC 9(3) OCCURS DYNAMIC CAPACITY IN C2 FROM 1 TO 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET C2 (2) TO 4.
           STOP RUN.
