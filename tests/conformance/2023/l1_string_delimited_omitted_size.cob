      *> ISO §14.9.43.3 SR9 — a DELIMITED phrase omitted immediately
      *> preceding INTO implies DELIMITED BY SIZE for that final group.
      *> Each trailing phraseless sender holds a SPACE, the delimiter of
      *> the group before it, so a wrongly inherited delimiter (or any
      *> delimiter at all) would cut it short; SIZE moves it whole
      *> (§14.9.43.4 GR3 c). A phraseless run BEFORE a DELIMITED phrase
      *> is part of that phrase's group (the repeated sender group of
      *> the general format), so only the final group takes the
      *> implied SIZE.
      *> Every receiver starts as 20 '.'; GR7 leaves untouched positions
      *> as they were, and POINTER (GR6) ends one past the last char.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1STRDO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S1 PIC X(5) VALUE "AB CD".
       01 S2 PIC X(4) VALUE "EF G".
       01 S3 PIC X(3) VALUE "H I".
       01 R  PIC X(20).
       01 P  PIC 99.
       PROCEDURE DIVISION.
       MAIN-P.
      *> T1: S1 by SPACE -> "AB"; S3 implied SIZE -> "H I". P = 1+5.
           MOVE ALL "." TO R
           MOVE 1 TO P
           STRING S1 DELIMITED BY SPACE S3 INTO R WITH POINTER P
           DISPLAY "T1 [" R "] " P
      *> T2: two senders in the final phraseless group, both SIZE:
      *> "AB" + "EF G" + "H I". P = 1+9.
           MOVE ALL "." TO R
           MOVE 1 TO P
           STRING S1 DELIMITED BY SPACE S2 S3 INTO R WITH POINTER P
           DISPLAY "T2 [" R "] " P
      *> T3: no DELIMITED phrase anywhere — the only group immediately
      *> precedes INTO: "EF G" + "H I". P = 1+7.
           MOVE ALL "." TO R
           MOVE 1 TO P
           STRING S2 S3 INTO R WITH POINTER P
           DISPLAY "T3 [" R "] " P
      *> T4: S1 S2 form ONE group delimited by SPACE ("AB" + "EF"); S3
      *> alone is the final phraseless group, SIZE: "H I". P = 1+7.
           MOVE ALL "." TO R
           MOVE 1 TO P
           STRING S1 S2 DELIMITED BY SPACE S3 INTO R WITH POINTER P
           DISPLAY "T4 [" R "] " P
      *> T5: literal senders: "X Y" by " " -> "X"; "Z Z" SIZE. P = 1+4.
           MOVE ALL "." TO R
           MOVE 1 TO P
           STRING "X Y" DELIMITED BY " " "Z Z" INTO R WITH POINTER P
           DISPLAY "T5 [" R "] " P
           STOP RUN.
