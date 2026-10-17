      *>--- conformance-case
      *> id: l1_use_sr8_file_named_twice
      *> rules: [SR-14.9.49.3-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.49.3 SR8 - "The same file-name shall not appear in more than one USE AFTER
      *> EXCEPTION statement within the same procedure division." ONE statement is not more than
      *> one statement, so this program - whose single Format-1 USE writes F1 twice and then F2 -
      *> is LEGAL and shall compile. It is the ACCEPTING complement of that rule (its rejecting
      *> half is conformance-suite/cases/14.9.49-USE/l1-use-sr8-file-in-two-statements).
      *> The Format-1 figure (§14.9.49.2, rendered from the printed page) writes the operand list
      *> as `{ file-name-1 } ...`: an ellipsis over a single brace group, with nothing restricting
      *> the repetition. §14.9.49.4 GR6 a) then associates the one procedure with the file - "If
      *> file-name-1 is specified, the associated procedure is executed when the condition
      *> described in the USE statement occurs" - however many times the name is written, so the
      *> repetition changes no general rule either.
      *> Placed in the 85 corpus because SR8 is a FORMAT 1 rule and Format 1 exists at every
      *> edition: a green run at --std 85 proves the acceptance is not a 2023-only relaxation.
      *> The written-twice F1 must also bind ONCE, not twice - the declarative dispatch the
      *> emitter generates is a switch over the file, and two identical case labels would not
      *> compile - so a green run is itself the proof of the single binding.
      *>
      *> DERIVATION - every expected line follows from the rule text, nothing from the compiler.
      *>  * OPEN OUTPUT of an absent, non-optional file: §14.9.27.4 GR4's table gives "Normal
      *>    open" for the OUTPUT row when the file is unavailable (it is created), and
      *>    §9.1.13.2 rule 1 gives the successful value - I-O status "00". WRITE and CLOSE of
      *>    that file likewise complete successfully, so both status items read 00 before the
      *>    reading pass begins.
      *>  * The first READ returns the one record just written: successful completion, I-O
      *>    status "00" (§9.1.13.2 rule 1), and the record area holds it.
      *>  * The second READ finds no next logical record: §9.1.13.4 rule 1 a) - "I-O status = 10.
      *>    A sequential READ statement is attempted and no next or prior logical record exists
      *>    in the physical file because ... the end of the physical file has been reached" -
      *>    which is also what §14.9.30.4 GR24 a) sets: "The I-O status of the file connector
      *>    associated with file-name-1 is set to '10' to indicate the at end condition". No AT
      *>    END phrase is written, so §14.9.49.4 GR6 selects the declarative, and GR6 a) is the
      *>    applicable arm for BOTH files: F1 because the statement names it (twice), F2 because
      *>    the same statement names it once.
      *>  * Control comes back after each handler: EC-I-O-AT-END is NON-fatal (§14.6.13.1.6's
      *>    exception-name table marks it NF), so §14.9.49.4 GR7 b) applies - "control is
      *>    returned to an implicit CONTINUE statement following the input-output statement whose
      *>    execution caused the exception". Hence the AFTER- lines are reached and HITS reaches 2.
      *>  * The status values the SECOND handler displays are ST1=00 ST2=10, not ST1=10: CLOSE F1
      *>    runs between the two readings and completes successfully, so §9.1.13.2 rule 1 has
      *>    already reset F1's status to 00 by then. Both items are displayed precisely because
      *>    that pair is what distinguishes the F2 invocation from a re-entry for F1.
      *>  * HITS=2 is the falsifiable part: were the repeated F1 to bind a second declarative
      *>    entry, or were the operand list to stop at the repetition and leave F2 unscoped, this
      *>    count and the DECL lines would differ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1USE8P.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "l1use8p-1.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST1.
           SELECT F2 ASSIGN TO "l1use8p-2.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST2.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(8).
       FD F2.
       01 R2 PIC X(8).
       WORKING-STORAGE SECTION.
       01 ST1  PIC XX VALUE "??".
       01 ST2  PIC XX VALUE "??".
       01 HITS PIC 9  VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       IO-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1 F1 F2.
       IO-PARA.
           ADD 1 TO HITS
           DISPLAY "DECL " HITS " ST1=" ST1 " ST2=" ST2.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN.
           OPEN OUTPUT F1
           MOVE "REC-ONE " TO R1
           WRITE R1
           CLOSE F1
           OPEN OUTPUT F2
           MOVE "REC-TWO " TO R2
           WRITE R2
           CLOSE F2
           OPEN INPUT F1
           READ F1
           DISPLAY "R1=" R1 " ST1=" ST1
           READ F1
           DISPLAY "AFTER-F1 ST1=" ST1
           CLOSE F1
           OPEN INPUT F2
           READ F2
           DISPLAY "R2=" R2 " ST2=" ST2
           READ F2
           DISPLAY "AFTER-F2 ST2=" ST2
           CLOSE F2
           DISPLAY "HITS=" HITS
           STOP RUN.
