*>--- conformance-case
*> id: pb88-string-into-variable-length-group
*> rules: [SR-14.9.43.3-11, SR-14.9.48.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.43.3 SR11: identifier-1, -2 or -3 "shall not specify a variable-length group" - a group with a
      *> DYNAMIC LENGTH elementary item subordinate to it (8.5.1.12). kb/Work PB88: unchecked before; COBOLNET1651.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB88NSTRVLG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 VG.
          05 VA PIC X(2).
          05 VD PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION.
           STRING "AB" DELIMITED SIZE INTO VG.
           STOP RUN.
