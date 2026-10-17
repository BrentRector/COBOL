*>--- conformance-case
*> id: pb695_delete_file_not_exception_bare
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB695 family 3 - the SECOND arm of DELETE FILE's exception phrase.
      *> ISO 14.9.10.2 Format 2 prints `[ ON EXCEPTION imperative-statement-3 ]` and
      *> `[ NOT ON EXCEPTION imperative-statement-4 ]`, and 5.2.6.4's choice indicators make the two
      *> phrases writable in EITHER order. Measured on printed page 635 / folio 605: EXCEPTION carries
      *> an underline rectangle in both phrases (95.5% cover) and NOT carries one (89.5%), while
      *> NEITHER occurrence of ON has any rule in its band - so ON is an optional word (8.3.2.4.3) in
      *> all four places. PB134 relaxed only the positive-first arm; writing the NEGATIVE phrase first
      *> and omitting ON was a COBOL0001 until family 3.
      *>
      *> EXPECTED VALUES, DERIVED FROM 14.9.10.4's FORMAT 2 rules:
      *>   GR13 - "The file connector referenced by file-name-1 shall not be open. If the file is open
      *>          the I-O status value in the file connector referenced by file-name-1 is set to '41'."
      *>          The first DELETE runs while the file is still OPEN, so it is UNSUCCESSFUL, the
      *>          EXCEPTION phrase runs (EXC1) and 9.1.13 leaves 41 in the FILE STATUS item.
      *>   GR21  - "If the execution of the DELETE FILE statement is successful ... b) If the NOT ON
      *>          EXCEPTION phrase is specified ... control is transferred to imperative-statement-4."
      *>          The second DELETE runs after CLOSE, deletes the file and reports NOEXC2 with 00.
      *>   GR14  - "If the file associated with file-name-1 is not present, the execution of the DELETE
      *>          FILE statement is successful and the I-O status value ... is set to '05'." The third
      *>          DELETE names the file just removed, so it too is successful: NOEXC3 with 05.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB695DELNOT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb695-del-not-bare.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(4).
       WORKING-STORAGE SECTION.
       01 WS-FS PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           MOVE "ABCD" TO R.
           WRITE R.
           DELETE FILE F
               NOT EXCEPTION DISPLAY "NOEXC1"
               EXCEPTION DISPLAY "EXC1"
           END-DELETE.
           DISPLAY "ST1=" WS-FS.
           CLOSE F.
           DELETE FILE F
               NOT EXCEPTION DISPLAY "NOEXC2"
               EXCEPTION DISPLAY "EXC2"
           END-DELETE.
           DISPLAY "ST2=" WS-FS.
           DELETE FILE F
               NOT EXCEPTION DISPLAY "NOEXC3"
               EXCEPTION DISPLAY "EXC3"
           END-DELETE.
           DISPLAY "ST3=" WS-FS.
           DISPLAY "DONE".
           STOP RUN.
