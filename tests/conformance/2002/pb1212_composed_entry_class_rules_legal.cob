      *> kb/Work PB1212 - the positive twin (compile-only).  13.18.5.3 SR1 bars BASED only from class OBJECT, so `BASED` on an entry whose TYPE is a USAGE POINTER type
      *> (class pointer) is legal; 13.18.22.3 SR4 bars EXTERNAL from class object or pointer, so EXTERNAL on a TYPE of an ordinary group type is legal; and SR5 is satisfied
      *> when the strong type declaration of an EXTERNAL file's record is itself external (IS TYPEDEF STRONG EXTERNAL is not needed here: the file's record uses a WEAK type).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W13PPB1212COMPOSEDOK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "W13PPB1212COMPOSEDOK.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F IS EXTERNAL.
       01  R TYPE WT.
       WORKING-STORAGE SECTION.
       01  WT TYPEDEF.
           05  A PIC X(3).
       01  PT USAGE POINTER TYPEDEF.
       01  P TYPE PT BASED.
       01  G TYPE WT EXTERNAL.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
