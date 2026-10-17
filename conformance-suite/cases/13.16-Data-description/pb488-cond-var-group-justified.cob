*>--- conformance-case
*> id: pb488-cond-var-group-justified
*> rules: [SR-13.16.3-24]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.16.3 syntax rule 24 - "A condition-name may be associated with any data description
*> entry that contains a level-number except the following: ... d) A group containing items described with a
*> JUSTIFIED or SYNCHRONIZED clause."
*>
*> The JUSTIFIED member makes the group's character image depend on a right-justified store, so the group
*> cannot be a conditional variable. 13.18.31.3 SR1 confines JUSTIFIED to the elementary item level, so this
*> is the only spelling of the JUSTIFIED half of d). kb/Work PB488 measured it compiling and evaluating.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB488-COND-VAR-GROUP-JUST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
       88 G-COND VALUE "ABCD".
          05 X PIC X(2) JUSTIFIED RIGHT.
          05 Y PIC X(2) VALUE "CD".
       PROCEDURE DIVISION.
           IF G-COND DISPLAY "T" ELSE DISPLAY "F" END-IF
           STOP RUN.
