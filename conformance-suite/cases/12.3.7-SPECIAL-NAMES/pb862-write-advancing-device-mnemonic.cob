*>--- conformance-case
*> id: pb862-write-advancing-device-mnemonic
*> rules: [SR-12.3.7.3-7, SR-14.9.51.3-16, DOC-A.1-190, DOC-A.1-221]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB862 - ISO 14.9.51.3 SR16: "When mnemonic-name-1 is specified, the name is associated with a
      *> feature-name specified by the implementor."  SYSOUT is a DEVICE-name (docs/CONFORMANCE.md section 7,
      *> A.1 item 189), and 12.3.7.3 SR7 confines a device's mnemonic-name-3 to ACCEPT and DISPLAY.  The WRITE
      *> binder used to treat ANY SPECIAL-NAMES mnemonic as a zero-line advance, whatever it named; the
      *> feature-names are C01 and CSP (A.1 item 190).  COBOLNET2243.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB862WADV.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYSOUT IS OUT-DEV.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT P-OUT ASSIGN TO "PB862WADVF".
       DATA DIVISION.
       FILE SECTION.
       FD P-OUT.
       01 P-REC PIC X(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT P-OUT.
           MOVE "AAAA" TO P-REC.
           WRITE P-REC AFTER ADVANCING OUT-DEV.
           CLOSE P-OUT.
           STOP RUN.
