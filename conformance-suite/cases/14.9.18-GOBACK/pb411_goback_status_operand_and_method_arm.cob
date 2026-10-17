*>--- conformance-case
*> id: pb411_goback_status_operand_and_method_arm
*> rules: [SR-14.9.18.3-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB411 — THE TERMINATION-STATUS OPERAND, at the edition that introduced it on GOBACK.
      *>
      *> Two rules, one screen, and the golden exists because BOTH were wrong and in OPPOSITE directions.
      *>
      *> LEG 1 — an ALPHANUMERIC GROUP is a legal status operand. ISO 14.9.18.3 syntax rule 6 (and its
      *> word-for-word twin 14.9.42.3 syntax rule 2 for STOP RUN): "Identifier-2 shall reference an integer data
      *> item or a data item with usage display or usage national." 8.5.2.1: "An alphanumeric group item is
      *> treated as though it had a usage of display." The group therefore SATISFIES the second alternative.
      *> Before the fix both verbs rejected it with COBOLNET1704 - a diagnostic quoting the very rule the operand
      *> meets - because the screen asked the item's elementary PICTURE, which a group has none of, and read that
      *> absence as "no usage at all".
      *>
      *> LEG 2 — the METHOD arm owes the same rules. 14.9.18.3's syntax rules carry no context qualifier at all;
      *> only General rules 7, 8, 9 and 10 do, and each of those opens "If the GOBACK ... is executing in a main
      *> program". So a method's GOBACK owes SR6/SR7/SR8 (and the COBOL-2023 introduction gate) in full, and
      *> carries no operating-system indication at all - a method is never a main program. Before the fix the
      *> method arm of the bind read only the RETURNING and RAISING phrases and dropped the status phrase in
      *> silence, so neither the syntax rules nor the edition gate ever ran there.
      *>
      *> WHAT THE OUTPUT PROVES. "IN-METHOD" and "AFTER-INVOKE" prove the method's GOBACK ... WITH ERROR STATUS
      *> compiled (SR6 admits the group) and returned to the INVOKE. The run unit then ends through its own
      *> STOP RUN, so the method's ERROR status is INERT exactly as GR7-GR10 require: the DISPLAY after the
      *> INVOKE is reached, and nothing about the method return indicates an error termination.
      *>
      *> The exit-code half of the rule (14.9.18.4 GR10 / 14.9.42.4 GR5, docs/CONFORMANCE.md item 192's published
      *> determination) is asserted where a golden cannot reach - StopGobackExitCodeTests, which runs the program
      *> and reads its status - because a corpus golden compares stdout only.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB411GSO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CB411.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CB411.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CB411 "NEW" RETURNING O.
           INVOKE O "PING".
           DISPLAY "AFTER-INVOKE".
           STOP RUN.
       END PROGRAM PB411GSO.

       IDENTIFICATION DIVISION.
       CLASS-ID. CB411 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-STATUS-GRP.
          05 WS-SG-A PIC X VALUE "4".
          05 WS-SG-B PIC X VALUE "1".
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "IN-METHOD".
           GOBACK WITH ERROR STATUS WS-STATUS-GRP.
       END METHOD PING.
       END OBJECT.
       END CLASS CB411.
