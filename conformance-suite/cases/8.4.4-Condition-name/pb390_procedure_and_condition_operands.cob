      *>--- conformance-case
      *> id: pb390_procedure_and_condition_operands
      *> rules: [SR-8.4.4.3-1, SR-14.9.25.3-12, SR-14.9.28.3-12, SR-14.9.28.3-13, SR-14.9.39.3-6, FMT-8.4.4.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB390 - the LEGAL half of the operand rules whose violations used to be delivered as a
      *> run-time "not implemented" abort. Everything here is admitted by the rule that refuses its
      *> siblings, and the expected output is derived from the GENERAL RULES, not from a run:
      *>   PERFORM P-ONE            - procedure-name-1 is a paragraph of this source element (14.9.28.3 SR12)
      *>   PERFORM S-TWO            - a SECTION is a procedure-name; 14.9.28.4 GR4 executes its whole
      *>                              paragraph range, first statement of its first paragraph through the
      *>                              last of its last, so S2A then S2B
      *>   PERFORM P-ONE THRU P-TWO - GR4/GR5b, procedure-name-1's start through procedure-name-2's end
      *>   PERFORM S-TWO-A OF S-TWO - 8.4.2.2 qualification is part of the operand
      *>   SET WS-FLAG-YES TO TRUE  - 14.9.39.3 SR6 SATISFIED: a level-88 condition-name IS associated with
      *>                              a conditional variable, and 14.9.39.4 moves its VALUE literal there
      *>   SET SW-M TO ON           - 14.9.39.3 SR5 SATISFIED: mnemonic-name-1 is associated with an
      *>                              external switch whose status may be altered, so the switch-status
      *>                              condition SW-ON is then true (8.8.4.6)
      *>   MOVE CORRESPONDING G1 TO G2 - 14.9.25.3 SR12 SATISFIED: both operands are group items and
      *>                              neither is reference-modified, so 14.7.6 pairs A with A and B with B
      *>   GO TO M-STOP             - 14.9.17.4 GR1 transfers control, so the paragraphs between are NOT
      *>                              executed a second time; their absence from the output is the evidence
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB390ADMITTED.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-1 IS SW-M ON STATUS IS SW-ON OFF STATUS IS SW-OFF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-FLAG PIC X VALUE "N".
          88 WS-FLAG-YES VALUE "Y".
       01 G1.
          05 A PIC 9(3) VALUE 111.
          05 B PIC 9(3) VALUE 222.
       01 G2.
          05 A PIC 9(3) VALUE 0.
          05 B PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
       MAIN SECTION.
       M-START.
           PERFORM P-ONE.
           PERFORM S-TWO.
           PERFORM P-ONE THRU P-TWO.
           PERFORM S-TWO-A OF S-TWO.
           SET WS-FLAG-YES TO TRUE.
           DISPLAY "FLAG=" WS-FLAG.
           SET SW-M TO ON.
           IF SW-ON
               DISPLAY "SWITCH=ON"
           ELSE
               DISPLAY "SWITCH=OFF"
           END-IF.
           MOVE CORRESPONDING G1 TO G2.
           DISPLAY "CORR=" G2.
           GO TO M-STOP.
       P-ONE.
           DISPLAY "ONE".
       P-TWO.
           DISPLAY "TWO".
       S-TWO SECTION.
       S-TWO-A.
           DISPLAY "S2A".
       S-TWO-B.
           DISPLAY "S2B".
       Z-END SECTION.
       M-STOP.
           STOP RUN.
