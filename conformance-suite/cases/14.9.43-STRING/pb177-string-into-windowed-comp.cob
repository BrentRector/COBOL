*>--- conformance-case
*> id: pb177-string-into-windowed-comp
*> rules: [SR-14.9.43.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.43.3 SR1: "all identifiers, except identifier-4, shall be described implicitly or explicitly
      *> as usage display or national" - identifier-4 is the POINTER, so the INTO receiver is covered.
      *> kb/Work PB177 leg D claimed a WINDOWED receiver skipped this screen and was accepted silently. MEASURED
      *> ACROSS SEVEN RECEIVER SHAPES AND DISSOLVED: the bind screen tests the ITEM'S DESCRIPTION (its usage),
      *> which is exactly what leg D asked the fix to be, so native COMP/COMP-5/PACKED/COMP-1 and their Tier-B
      *> REDEFINES windows are all rejected, while a windowed usage-DISPLAY item is correctly ACCEPTED. This
      *> fixture LOCKS the measured fact: the bind screen still carries a StoreAsImage conjunct, so a future
      *> change that promotes a non-display leaf must fail here rather than silently re-open the hole.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB177N6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(4) VALUE "0000".
       01 W REDEFINES A PIC S9(4) COMP.
       PROCEDURE DIVISION.
       MAIN.
           STRING "AB" DELIMITED SIZE INTO W.
           STOP RUN.
