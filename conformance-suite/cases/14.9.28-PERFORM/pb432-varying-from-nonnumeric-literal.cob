*>--- conformance-case
*> id: pb432-varying-from-nonnumeric-literal
*> rules: [SR-14.9.28.3-3, FMT-14.9.28.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB432 — ISO §14.9.28.3 SR3: "Each literal shall be numeric." The varying-phrase's FROM slot
      *> is parsed as a superset of the brace group so the LEGAL figurative ZERO can reach the binder at all
      *> (§8.3.3.6.3 SR1 permits it precisely because SR3 restricts the literal to numeric); the prohibition
      *> half is enforced at bind by the ONE numeric-context literal reading, which names SR3 as the rule that
      *> closes this operand list rather than citing §8.8.1.1 alone.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB432NEGSPACE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM VARYING I FROM SPACE BY 1 UNTIL I > 3
               CONTINUE
           END-PERFORM
           STOP RUN.
