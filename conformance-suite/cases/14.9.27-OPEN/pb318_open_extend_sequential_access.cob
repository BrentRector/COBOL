*>--- conformance-case
*> id: pb318_open_extend_sequential_access
*> rules: [SR-14.9.27.3-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> OPEN EXTEND KEYS ON THE ACCESS MODE, NOT THE ORGANIZATION (kb/Work PB318).
      *> ISO 14.9.27.3 SR2: "The EXTEND phrase shall be specified only if the access mode of
      *> the file connector referenced by file-name-1 is sequential and the LINAGE clause is not
      *> specified in the file description entry for file-name-1." The screen that now rejects
      *> EXTEND on a RANDOM or DYNAMIC file and on a LINAGE file must still ACCEPT it on a
      *> RELATIVE file whose ACCESS MODE IS SEQUENTIAL.
      *> DERIVATION
      *>  . OPEN OUTPUT + WRITE puts R001 at relative record 1.
      *>  . 14.9.27.4 GR15: EXTEND "positions the file immediately after the last logical record
      *>    for that file", so the WRITE after OPEN EXTEND adds R002 after R001.
      *>  . OPEN INPUT + two sequential READs return R001 then R002; a third READ is AT END.
      *>  . 9.1.13.2 rule 1: '00' for each successful statement.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB318EXS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RLF ASSIGN TO "pb318exs.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST1.
       DATA DIVISION.
       FILE SECTION.
       FD RLF.
       01 RL-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST1 PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RLF
           MOVE "R001" TO RL-REC
           WRITE RL-REC
           CLOSE RLF
           OPEN EXTEND RLF
           DISPLAY "EXTEND=" ST1
           MOVE "R002" TO RL-REC
           WRITE RL-REC
           DISPLAY "WRITE=" ST1
           CLOSE RLF
           OPEN INPUT RLF
           READ RLF AT END DISPLAY "EOF1" END-READ
           DISPLAY RL-REC "|" ST1
           READ RLF AT END DISPLAY "EOF2" END-READ
           DISPLAY RL-REC "|" ST1
           READ RLF AT END DISPLAY "EOF3" END-READ
           CLOSE RLF
           STOP RUN.
