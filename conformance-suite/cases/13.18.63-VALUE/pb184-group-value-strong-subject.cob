*>--- conformance-case
*> id: pb184-group-value-strong-subject
*> rules: [SR-13.18.63.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR1: "The subject of the entry shall not be a strongly-typed group item or a
      *> variable-length group."  GS is described with a TYPE clause referencing a type declaration specifying
      *> the STRONG phrase, so it is a strongly-typed group item (8.5.3.1, first bullet), and it specifies a
      *> group-level VALUE clause.
      *> ⛔ WHY SR1 IS PART OF THE SAME SCREEN AND NOT A LATER ONE: it is what keeps SR14's second conjunct
      *> honest.  13.18.29.4 GR3 makes a group an ALPHANUMERIC group item only when no GROUP-USAGE clause is
      *> specified or implied AND it "is not strongly typed and is not a variable-length group".  With SR1
      *> unenforced this program drew COBOLNET1702 naming SR14 - a rule 13.18.29.4 GR3 says does not reach a
      *> strongly-typed group at all.  The right answer is one verdict against the rule the source violates.
      *> Twin: pb184-group-value-variable-length-subject covers SR1's other shape.
      *> Edition band: STRONG TYPEDEF is COBOL-2002, so 85 rejects the declaration itself (COBOLNET0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB184N5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ST IS TYPEDEF STRONG.
          05 SX PIC X(2).
          05 SY PIC X(2).
       01 GS TYPE ST VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY SX OF GS
           STOP RUN.
