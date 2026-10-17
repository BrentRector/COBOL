*>--- conformance-case
*> id: pb140-close-norewind-indexed
*> rules: [SR-14.9.6.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.6.3 SR1: WITH NO REWIND on an indexed-organization file
      *> (kb/Work PB140; the WITH LOCK phrase alone is unrestricted).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB140N5.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT I ASSIGN TO "i.dat"
               ORGANIZATION INDEXED RECORD KEY IK.
       DATA DIVISION.
       FILE SECTION.
       FD I.
       01 I-REC.
          02 IK PIC X(4).
          02 IV PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           CLOSE I WITH NO REWIND
           STOP RUN.
