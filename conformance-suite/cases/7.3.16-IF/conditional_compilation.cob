*>--- conformance-case
*> id: conditional_compilation
*> rules: [GR-7.3.16.4-2, FMT-7.3.16.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 7.3.11 / 7.3.16 — conditional compilation: >>DEFINE + >>IF/>>ELSE/>>END-IF defined-condition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CCTEST.
       PROCEDURE DIVISION.
       MAIN.
>>DEFINE DBG AS 1
>>IF DBG IS DEFINED
           DISPLAY "DEBUG-ON".
>>ELSE
           DISPLAY "DEBUG-OFF".
>>END-IF
           STOP RUN.
