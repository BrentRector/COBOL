*>--- conformance-case
*> id: pb961-set-content-snan-below-2014
*> rules: [GR-14.9.39.4-35]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 14.9.39.2 Format 15 (SET CONTENT ... TO FLOAT-NOT-A-NUMBER-SIGNALING, 14.9.39.4 GR35) is a
      *> COBOL-2014 facility: the value words are 8.9-reserved from 2014 and Annex E records no SET change into
      *> 2023 beyond E.3.3, so below 2014 the statement is refused with the edition gate, COBOLNET0900 - as is
      *> its receiver: 14.9.39.3 SR32 confines identifier-14 to a STANDARD floating-point usage, and
      *> FLOAT-BINARY-32 (13.18.60.4 GR14) is itself 2014, so no legal pre-2014 receiver exists (a COMP-2 would
      *> draw SR32's COBOLNET1940 instead). The positive twin, which pins the stored signaling-NaN bits, is
      *> 2014/pb961_signaling_nan_binary32 (kb/Work PB961).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB961BELOW2014.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D USAGE FLOAT-BINARY-32.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET CONTENT OF D TO FLOAT-NOT-A-NUMBER-SIGNALING
           STOP RUN.
