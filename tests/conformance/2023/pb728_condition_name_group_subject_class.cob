      *> ISO 8.8.4.5.3 GR2 -- "The rules for comparing a conditional variable with a condition-name value are
      *> the same as those specified for relation conditions" -- and 8.8.4.2.1 -- "A national group item or a
      *> bit group item shall be treated as an elementary national data item or an elementary bit data item,
      *> respectively" -- so the SUBJECT'S CLASS decides the comparison for a GROUP exactly as for an
      *> elementary item: 8.8.4.2.9 (the national program collating sequence), 8.8.4.2.8 (a boolean VALUE
      *> comparison with right zero extension, never a collating sequence) and 8.8.4.2.7 (the alphanumeric
      *> program collating sequence). 13.18.29.4 GR1b/GR2b give the group its as-if PICTURE 1(m)/N(m), which is
      *> also the character-position count 8.3.3.6.4 GR2 repeats a figurative to; 14.9.39.4 GR6 names all three
      *> group kinds as SET ... TO TRUE subjects. kb/Work PB728 + PB575.
      *>
      *> THE DISCRIMINATOR: the two program collating sequences are deliberately DIFFERENT, so neither leg can
      *> pass for the wrong reason. AN-EQ puts "A" and "B" at the SAME position (the ALSO phrase), so under it
      *> "BBB" and "AAA" compare EQUAL; NAT-D keeps N"A" and N"B" at DISTINCT positions, so under it N"BBB" and
      *> N"AAA" compare UNEQUAL. A national group weighed on the alphanumeric table therefore answers the
      *> OPPOSITE of the conforming answer.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB728CN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. PB728-COMPUTER
           PROGRAM COLLATING SEQUENCE IS AN-EQ NAT-D.
       SPECIAL-NAMES.
           ALPHABET AN-EQ IS "A" ALSO "B"
           ALPHABET NAT-D FOR NATIONAL IS N"A" N"B".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GN GROUP-USAGE NATIONAL.
          88 GN-IS-A VALUE N"AAA".
          88 GN-ALL-A VALUE ALL N"A".
          05 GN-A PIC N(3).
       01 EN PIC N(3).
          88 EN-IS-A VALUE N"AAA".
          88 EN-ALL-A VALUE ALL N"A".
       01 GA.
          88 GA-IS-A VALUE "AAA".
          05 GA-A PIC X(3).
       01 EA PIC X(3).
          88 EA-IS-A VALUE "AAA".
       01 GB GROUP-USAGE BIT.
          88 GB-ON VALUE B"10".
          88 GB-ALL-1 VALUE ALL B"1".
          05 GB-A PIC 1(3) USAGE BIT.
       01 EB PIC 1(3) USAGE BIT.
          88 EB-ON VALUE B"10".
          88 EB-ALL-1 VALUE ALL B"1".
       PROCEDURE DIVISION.
       MAIN.
      *> (1) NATIONAL. N"BBB" vs the 88 value N"AAA", under NAT-D where N"A" and N"B" hold distinct
      *> positions: 8.8.4.2.9 -> 8.8.4.2.10 rule 1, the first unequal pair decides, so the values are
      *> UNEQUAL and both legs are FALSE. (Weighed on AN-EQ instead, where "A" and "B" share a position,
      *> the group leg would say TRUE.)
           MOVE N"BBB" TO GN-A.
           MOVE N"BBB" TO EN.
           IF GN-IS-A DISPLAY "N-GRP=T" ELSE DISPLAY "N-GRP=F" END-IF.
           IF EN-IS-A DISPLAY "N-ELM=T" ELSE DISPLAY "N-ELM=F" END-IF.
      *> (2) ALPHANUMERIC -- the OVER-REJECTION CONTROL. An ordinary group has no as-if PICTURE; 8.8.4.2.1
      *> treats it as an elementary alphanumeric data item and 8.8.4.2.7 collates it under AN-EQ, where
      *> "BBB" and "AAA" collate EQUAL. Both legs are TRUE and must stay TRUE.
           MOVE "BBB" TO GA-A.
           MOVE "BBB" TO EA.
           IF GA-IS-A DISPLAY "A-GRP=T" ELSE DISPLAY "A-GRP=F" END-IF.
           IF EA-IS-A DISPLAY "A-ELM=T" ELSE DISPLAY "A-ELM=F" END-IF.
      *> (3) BOOLEAN. B"100" vs the 88 value B"10": 8.8.4.2.8 rule 2 extends the shorter operand on the
      *> right with boolean zeros, making it B"100" -- EQUAL, both legs TRUE. The comparison is of
      *> boolean VALUES "regardless of their usage", so AN-EQ never touches it.
           MOVE B"100" TO GB-A.
           MOVE B"100" TO EB.
           IF GB-ON DISPLAY "B-GRP=T" ELSE DISPLAY "B-GRP=F" END-IF.
           IF EB-ON DISPLAY "B-ELM=T" ELSE DISPLAY "B-ELM=F" END-IF.
      *> (4) ALL literal-1 is repeated to the conditional variable's CHARACTER-POSITION count (8.3.3.6.4
      *> GR2). 13.18.29.4 GR1b/GR2b make that count the as-if PICTURE's: 3 boolean positions for GB and 3
      *> national positions for GN -- not the group's storage extent. So ALL B"1" is B"111" and ALL N"A"
      *> is N"AAA", and each leg is TRUE against the matching content.
           MOVE B"111" TO GB-A.
           MOVE B"111" TO EB.
           IF GB-ALL-1 DISPLAY "BA-GRP=T" ELSE DISPLAY "BA-GRP=F" END-IF.
           IF EB-ALL-1 DISPLAY "BA-ELM=T" ELSE DISPLAY "BA-ELM=F" END-IF.
           MOVE N"AAA" TO GN-A.
           MOVE N"AAA" TO EN.
           IF GN-ALL-A DISPLAY "NA-GRP=T" ELSE DISPLAY "NA-GRP=F" END-IF.
           IF EN-ALL-A DISPLAY "NA-ELM=T" ELSE DISPLAY "NA-ELM=F" END-IF.
      *> (5) SET condition-name TO TRUE, 14.9.39.4 GR6 -- "the literal in the VALUE clause associated with
      *> condition-name-1 is placed in the conditional variable ... when the conditional variable is an
      *> alphanumeric group item, bit group item, or national group item ...". All three group kinds are
      *> named by the standard, so all three store and then read back TRUE. GB takes B"10" aligned left
      *> with boolean-zero fill (13.18.63.4 GR7 -> 14.6.8.6), i.e. B"100", so GB-ON is TRUE and GB-ALL-1
      *> (B"111") is FALSE -- which also proves the store really happened.
           MOVE N"BBB" TO GN-A.
           MOVE "BBB" TO GA-A.
           MOVE B"111" TO GB-A.
           SET GN-IS-A TO TRUE.
           SET GA-IS-A TO TRUE.
           SET GB-ON TO TRUE.
           IF GN-IS-A DISPLAY "SN-GRP=T" ELSE DISPLAY "SN-GRP=F" END-IF.
           IF GA-IS-A DISPLAY "SA-GRP=T" ELSE DISPLAY "SA-GRP=F" END-IF.
           IF GB-ON DISPLAY "SB-GRP=T" ELSE DISPLAY "SB-GRP=F" END-IF.
           IF GB-ALL-1 DISPLAY "SB-ALL=T" ELSE DISPLAY "SB-ALL=F" END-IF.
           STOP RUN.
