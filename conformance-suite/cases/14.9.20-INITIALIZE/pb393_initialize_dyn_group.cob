*>--- conformance-case
*> id: pb393_initialize_dyn_group
*> rules: [GR-14.9.20.4-10]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB393. ISO 1989:2023 14.9.20.4 GR10: "When a group containing a dynamic-capacity table is
      *> initialized, all the elements of the table up to current capacity, if any, are initialized, whether or
      *> not the INITIALIZED phrase is present in the OCCURS clause, and the current capacity of the table is
      *> left unchanged."
      *> The table is grown to capacity 3 first, so "up to CURRENT capacity" is 3 - not the FROM 2 seed - and
      *> the AFTER line proves both halves of the rule at once: every one of the three elements is the
      *> 14.9.20.4 GR6c alphanumeric default SPACES, and CAP is still 3. The fixed member D-HDR is initialized
      *> in definition order (GR8) by the same walk.
      *> Before this fixture the child walk met the dynamic member on an explicit arm that staged COBOLNET1527
      *> loud - a deferral written before GR10 was read, since GR10 does not defer this case, it SPECIFIES it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB393INIDYN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ED PIC ZZ9.
       01 GD.
          05 D-HDR PIC X(3).
          05 D-T   PIC X(2) OCCURS DYNAMIC CAPACITY IN D-CAP FROM 2.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "HHH" TO D-HDR.
           MOVE "AA" TO D-T (1).
           MOVE "BB" TO D-T (2).
           MOVE "CC" TO D-T (3).
           MOVE D-CAP TO ED.
           DISPLAY "BEFORE CAP=[" ED "] H=[" D-HDR "] T=[" D-T (1)
               "][" D-T (2) "][" D-T (3) "]".
           INITIALIZE GD.
           MOVE D-CAP TO ED.
           DISPLAY "AFTER  CAP=[" ED "] H=[" D-HDR "] T=[" D-T (1)
               "][" D-T (2) "][" D-T (3) "]".
           STOP RUN.
