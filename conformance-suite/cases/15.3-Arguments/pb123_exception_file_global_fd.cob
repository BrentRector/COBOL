*>--- conformance-case
*> id: pb123_exception_file_global_fd
*> rules: [AR-15.3-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB123 — FUNCTION EXCEPTION-FILE naming a CONTAINER's GLOBAL FD from a contained program.
      *> ISO 13.18.30: the file-name of a GLOBAL FD is a global name, visible in every directly/indirectly
      *> contained program (13.18.27 GR1-2); 15.28.3 r1 admits "the name of a file connector that is
      *> specified in an FD statement". BindExceptionFileArg resolved against the program's OWN FD list while
      *> every other file-name site rides FilesByName (into which the binder merges containers' GLOBAL FDs),
      *> so this legal reference drew "not the name of a file connector" — the two-arm-dispatch shape. The
      *> never-touched connector answers 15.28.4 r2a's two spaces. The sweep's sibling rides along: the
      *> UNQUALIFIED LINAGE-COUNTER (8.4.3.14) in the contained program resolves to the container's GLOBAL
      *> LINAGE file — the only LINAGE file visible — instead of drawing "no file description entry contains
      *> a LINAGE clause" (the register's value is not pinned here; its RESOLUTION is the defect).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB123EF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-G ASSIGN TO "pb123ef.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F-G IS GLOBAL
           LINAGE IS 10 LINES.
       01 F-REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB123IN"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB123IN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(2).
       PROCEDURE DIVISION.
       M-IN.
           MOVE FUNCTION EXCEPTION-FILE(F-G) TO R
           IF R = SPACES DISPLAY "EF OK" ELSE DISPLAY "EF BAD " R END-IF
           IF LINAGE-COUNTER >= 0
               DISPLAY "LC OK" ELSE DISPLAY "LC BAD" END-IF
           GOBACK.
       END PROGRAM PB123IN.
       END PROGRAM PB123EF.
