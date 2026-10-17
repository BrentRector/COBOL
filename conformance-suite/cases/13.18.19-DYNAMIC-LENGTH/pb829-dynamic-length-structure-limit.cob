*>--- conformance-case
*> id: pb829-dynamic-length-structure-limit
*> rules: [SR-13.18.19.3-4, FMT-12.3.7.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB829 (finisher) - ISO 13.18.19.3 SR4: "If the LIMIT phrase and dynamic-structure-name-1
      *> are both specified, integer-1 shall not be greater than the maximum length associated with
      *> dynamic-length-structure-name-1."  DS-SS is SIGNED SHORT PREFIXED, whose length field holds at
      *> most 32767 (ISO 12.3.7.4 GR18's table), so LIMIT 40000 is COBOLNET2258.  Before the clause had a
      *> grammar the SPECIAL-NAMES entry was COBOL0001 and the reference "not yet supported" (COBOLNET1562).
      *> Below 2014 the clause is the dynamic-length-structure-2014 introduction gate (the version matrix).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB829DX.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DYNAMIC LENGTH STRUCTURE DS-SS IS SIGNED SHORT PREFIXED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D PIC X DYNAMIC LENGTH DS-SS LIMIT IS 40000.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
