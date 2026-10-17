*>--- conformance-case
*> id: pb367_use_procedure_is_the_section_ec_io
*> rules: [GR-14.9.49.4-3, GR-14.9.49.4-12]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB367 - the EC-I-O arm. USE format 3 whose exception-name BEGINS WITH EC-I-O is selected
      *> and returned through a DIFFERENT generated selector than the format-1 file/mode one and than the
      *> plain EC dispatcher (the __IoCheckEc path, 14.9.49.4 GR12 rather than GR7 or GR13), and it took the
      *> same shape-derived handler end - so it gets its own witness. The general case, the format-1 and
      *> plain-format-3 arms and the full derivation are in
      *> tests/conformance/2023/pb367_use_procedure_is_the_section.cob.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN:
      *>  . The second READ hits the end of the file with no AT END phrase; 9.1.13 sets I-O status 10 and, with
      *>    checking enabled, raises EC-I-O-AT-END, which 14.9.49.4 GR3 e) matches to D1 (a level-3 name, no
      *>    FILE phrase).
      *>  . 14.9.49.3 SR1 - "The remainder of the section shall consist of zero, one, or more procedural
      *>    paragraphs that define the procedures to be used": the use procedure is D1-P1, D1-EX, D1-P2 and
      *>    D1-LAST, all of them => IO-A FS=10 / IO-B. 14.9.14.4 GR1 makes the bare EXIT a no-op.
      *>  . 14.9.14.4 GR7 - D1-LAST's EXIT SECTION passes control "to an unnamed empty paragraph immediately
      *>    following the last paragraph of the current section, preceding any return mechanisms for that
      *>    section", so D1-TAIL never runs and the USE return mechanism fires.
      *>  . 14.9.49.4 GR12 - "control is transferred to the invoking routine in the input-output control
      *>    system", and then GR12 b): the I-O status does not indicate a fatal EC-I-O condition, so control is
      *>    returned to an implicit CONTINUE following the READ => AFTER-READ FS=10.
       >>TURN EC-I-O CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB367EIO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb367-ecio.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS1.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 F1-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01 FS1 PIC XX VALUE "00".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EC EC-I-O-AT-END.
       D1-P1.
           DISPLAY "IO-A FS=" FS1.
       D1-EX.
           EXIT.
       D1-P2.
           DISPLAY "IO-B".
       D1-LAST.
           EXIT SECTION.
       D1-TAIL.
           DISPLAY "IO-TAIL-MUST-NOT-RUN".
           STOP RUN.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT F1.
           WRITE F1-REC FROM "AAA".
           CLOSE F1.
           OPEN INPUT F1.
           READ F1.
           READ F1.
           DISPLAY "AFTER-READ FS=" FS1.
           CLOSE F1.
           STOP RUN.
