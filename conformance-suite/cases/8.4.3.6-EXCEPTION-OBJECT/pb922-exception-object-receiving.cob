*>--- conformance-case
*> id: pb922-exception-object-receiving
*> rules: [SR-8.4.3.6.3-1, SR-8.4.3.6.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB922 - ISO 8.4.3.6.3 SR1: "EXCEPTION-OBJECT shall not be specified as a receiving operand."
      *> SR1 is a rule about EVERY receiving operand in the language, not about SET - the one statement that had
      *> an arm for it - so a MOVE whose receiver is the predefined object reference is refused by SR1, with
      *> SR1's words. Before this, the reference drew COBOLNET1639 ("'EXCEPTION-OBJECT' is not defined - Check
      *> the spelling, or declare the item") beside COBOLNET0901 ("'EXCEPTION-OBJECT' is a reserved word in
      *> COBOL-2023 and cannot be used as a user-defined word"): two diagnostics that contradict each other, and
      *> neither of them the rule the program broke.
      *> NOT rejected below 2002: there the word is an ordinary user-defined word and the same statement is
      *> conforming source - conformance-suite/cases/_unassigned/pb922_exception_object_user_word_85.cob is the witness.
      *> Positive at the introducing edition: conformance-suite/cases/8.4.3.1-Identifier/pb922_exception_object_reference.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB922NEGRECV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U             USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
           MOVE U TO EXCEPTION-OBJECT
           STOP RUN.
