*>--- conformance-case
*> id: pb485_constant_entry_spelling
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.10 Constant entry -- the CONFORMING spelling of what a
      *> MicroFocus/GnuCOBOL level 78 is used for, and the positive half
      *> of the COBOLNET1746 screen: rejecting 78 is only correct if the
      *> standard way to name a compile-time constant still works. K is a
      *> level-01 CONSTANT entry, substituted at compile time, and it is
      *> used exactly where the rejected 78 entry was -- as the leftmost
      *> character position of a reference modification. kb/Work PB485.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB485P2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  V PIC X(20) VALUE SPACES.
       01  K CONSTANT AS 5.
       PROCEDURE DIVISION.
           MOVE "AB" TO V (K:2)
           DISPLAY V
           STOP RUN.
