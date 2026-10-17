      *>--- conformance-case
      *> id: pb567_condition_name_qualification
      *> rules: [SR-8.4.2.2.3-2, SR-8.4.2.2.3-3, SR-8.4.2.2.3-4, SR-8.4.2.2.3-5, SR-8.4.4.3-2, SR-13.16.3-23, FMT-8.4.2.2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB567 - the POSITIVE side of 13.16.3 SR23 ("Each
      *> condition-name is subordinate to the data-name with which it is
      *> associated") with 8.4.2.2.3 SR4/SR5: a condition-name qualifies
      *> by its conditional variable and any name that variable may be
      *> qualified by, innermost first; 8.4.2.2.2 Format 2 ends in
      *> [ file-name-1 ], so its record's FILE qualifies it too.
      *> Two IS-A entries: under S OF G1 (S = "A") and S OF G2 (S = "B").
      *> Q1 IS-A OF S OF G1 - full qualification            -> T
      *> Q2 IS-A OF G1 - a NON-immediate ancestor (gap)     -> T
      *> Q3 IS-A OF S OF G2 - the other variable, "B"       -> F
      *> Q4 IS-A IN G2 - IN = OF (SR3)                      -> F
      *> Q5 IS-X OF M OF G3 - three-deep, middle qualifier  -> T
      *> Q6 FA-OK OF FR OF FA-FILE - the FILE qualifier     -> T
      *> Q7 FA-OK OF FA-FILE - the file alone               -> T
      *> Q8 SET IS-A OF G2 TO TRUE then S OF G2             -> A
      *> Before this the file-name leg did not resolve: the condition-name
      *> qualifier walk was a private copy with no file-name arm.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB567CNQ.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA-FILE ASSIGN TO "PB567FA.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD FA-FILE.
       01 FR.
          05 FA-CODE PIC X.
             88 FA-OK VALUE "Y".
       WORKING-STORAGE SECTION.
       01 G1.
          05 S PIC X VALUE "A".
             88 IS-A VALUE "A".
       01 G2.
          05 S PIC X VALUE "B".
             88 IS-A VALUE "A".
       01 G3.
          05 M.
             10 X PIC X VALUE "X".
                88 IS-X VALUE "X".
       PROCEDURE DIVISION.
       MAIN.
           MOVE "Y" TO FA-CODE
           IF IS-A OF S OF G1 DISPLAY "Q1=T" ELSE DISPLAY "Q1=F".
           IF IS-A OF G1 DISPLAY "Q2=T" ELSE DISPLAY "Q2=F".
           IF IS-A OF S OF G2 DISPLAY "Q3=T" ELSE DISPLAY "Q3=F".
           IF IS-A IN G2 DISPLAY "Q4=T" ELSE DISPLAY "Q4=F".
           IF IS-X OF M OF G3 DISPLAY "Q5=T" ELSE DISPLAY "Q5=F".
           IF FA-OK OF FR OF FA-FILE DISPLAY "Q6=T"
               ELSE DISPLAY "Q6=F".
           IF FA-OK OF FA-FILE DISPLAY "Q7=T" ELSE DISPLAY "Q7=F".
           SET IS-A OF G2 TO TRUE
           DISPLAY "Q8=" S OF G2
           STOP RUN.
