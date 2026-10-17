*>--- conformance-case
*> id: l1_use_sr14_pair_twice_one_statement
*> rules: [SR-14.9.49.3-14]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.49.3 SR14 - "The same pair of exception-name-2 and file-name-2 shall not be
      *> specified in more than one USE statement within the same procedure division." The
      *> program below writes the pair (EC-I-O-AT-END, F1) twice inside a SINGLE Format-3 USE
      *> statement; one statement is not more than one statement, so the source is LEGAL and
      *> shall compile. It is one of the two ACCEPTING complements of SR14 (the other -
      *> a repeated BARE exception-name-1 - is l1_use_sr14_bare_name_repeated in this same
      *> directory; the rejecting half is
      *> tests/conformance/negative/l1-use-sr14-pair-in-two-statements).
      *> The Format-3 figure (§14.9.49.2, rendered from the printed page) writes the scope as
      *> `exception-name-2 { FILE file-name-2 } ... ...`: the inner braces carry their own
      *> ellipsis, so one entry may name several files and nothing restricts them to being
      *> distinct.
      *> Placed in the 2002 corpus because Format 3 arrives with the exception-condition model:
      *> a green run at --std 2002 proves the acceptance holds at the EARLIEST edition that has
      *> the format, not only at the default one.
      *>
      *> DERIVATION - every expected line follows from the rule text, nothing from the compiler.
      *>  * The >>TURN directive enables checking for the EC-I-O family; without it no format-3
      *>    declarative can be selected (§14.6.13.1: an exception condition is set to exist only
      *>    if the checking for it is enabled).
      *>  * OPEN OUTPUT of an absent, non-optional file is a normal open (§14.9.27.4 GR4's table,
      *>    OUTPUT row) and the WRITE and CLOSE that follow complete successfully.
      *>  * The second READ reaches end of file: §14.9.30.4 GR24 a) - "The I-O status of the file
      *>    connector associated with file-name-1 is set to '10' to indicate the at end condition,
      *>    and, if enabled, the EC-I-O-AT-END exception condition is set to exist" (§9.1.13.4
      *>    rule 1 a) gives the same 10).
      *>  * The declarative is selected by §14.9.49.4 GR3 c) - "All format 3 USE statements in
      *>    which file-name-2 is specified and exception-name-2 is a level-3 exception-name are
      *>    examined ... If the exception condition that was raised matches exception-name-2 and
      *>    the exception condition is associated with file-name-2, that declarative is executed."
      *>    EC-I-O-AT-END is a level-3 name and F1 is the file the condition is associated with.
      *>  * FUNCTION EXCEPTION-STATUS "returns an alphanumeric value that is the exception-name
      *>    associated with the last exception status" (§15.33.1), so "S: EC-I-O-AT-END" pins
      *>    WHICH condition selected the declarative rather than merely that some handler ran.
      *>  * RESUME AT NEXT STATEMENT transfers control to an implicit CONTINUE that "immediately
      *>    follows the end of the statement that was executing when control was transferred to
      *>    the exception processing procedure" (§14.9.33.4 GR2 a)), so "AFTER" is reached.
      *>  * HITS=1 is the falsifiable part: the pair is written twice and the handler shall run
      *>    ONCE - the two spellings are one selection entry, not two declaratives.
>>TURN EC-I-O CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1S14P1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "l1s14p1-1.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST1.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(8).
       WORKING-STORAGE SECTION.
       01 ST1  PIC XX VALUE "??".
       01 HITS PIC 9  VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       AT-END-SECT SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O-AT-END FILE F1 FILE F1.
       AT-END-PARA.
           ADD 1 TO HITS
           DISPLAY "S: " FUNCTION EXCEPTION-STATUS
           DISPLAY "DECL " HITS " ST1=" ST1
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN.
           OPEN OUTPUT F1
           MOVE "REC-ONE " TO R1
           WRITE R1
           CLOSE F1
           OPEN INPUT F1
           READ F1
           DISPLAY "R1=" R1 " ST1=" ST1
           READ F1
           DISPLAY "AFTER ST1=" ST1
           CLOSE F1
           DISPLAY "HITS=" HITS
           STOP RUN.
