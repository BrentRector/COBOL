      *> PB230 L4 (split from conformance:2002/pb230_incompatible_boolean_sending by kb/Work PB1707) - ISO 14.6.13.2
      *> rule 1, the BOOLEAN sibling of rule 2: a boolean sending item whose content would evaluate to false in a
      *> boolean class condition makes the result of the reference undefined and sets EC-DATA-INCOMPATIBLE, "except
      *> in the following circumstances: - a sending item is referenced in a class condition, or - a sending item is
      *> processed in a VALIDATE statement."
      *> L4 pins the observable that a ZERO-LENGTH reference raises nothing, which is what 14.6.13.2's closing
      *> paragraph requires ("If the content of a sending operand is not referenced by a given execution of a
      *> statement, any incompatible data in that operand is not detected") even though 8.8.4.4.4 GR1 makes the
      *> CLASS CONDITION on a zero-length item false - the two questions differ exactly there, and zero-length
      *> boolean operands are ordinary (8.8.2 NOTE 2 combines two of them into a zero-length result), so raising
      *> on one would reject working programs.  It does NOT claim which reader served it: a reference-modified
      *> result is an elementary alphanumeric item whatever the underlying category (8.4.3.3.4 GR6).  The
      *> corresponding carve-out inside the boolean checked read is stated at CobolBool.Sending.
      *> The reference modification BB (1:0) has a zero length, which is legal only under the COBOL-2023 directive
      *> REF-MOD-ZERO-LENGTH (ISO 7.3.23, cite.py OK); without it a zero length is the fatal EC-BOUND-REF-MOD
      *> (8.4.3.3.4 5) c)), refused at compile time for a literal length (kb/Work PB1707). BB holds "1Q0" + "1"
      *> (XB deposits a character that is no boolean value), so a reference that READ it would raise and print
      *> CAUGHT; none does, so RB receives the zero-length operand's value: 00000000.
       >>REF-MOD-ZERO-LENGTH ON
       >>TURN EC-DATA-INCOMPATIBLE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB230ZEROLEN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GB.
          05 BB PIC 1(4).
       01 XB REDEFINES GB PIC X(4).
       01 RB PIC 1(8).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-INCOMPATIBLE.
       H-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE "1Q01" TO XB.
           DISPLAY "L4 zero-length operand is not incompatible".
           COMPUTE RB = BB (1:0).
           DISPLAY "   RB=[" RB "]".
           STOP RUN.
