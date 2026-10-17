*>--- conformance-case
*> id: l1_trim_zero_length_result_2014
*> rules: [RV-15.96.4-4]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.96.4 r4 — TRIM's ZERO-LENGTH RETURN AT THE 2014 EDITION, where TRIM takes argument-1 and the
      *> LEADING/TRAILING phrase only. r4 word for word: "If argument-1 contains only characters that are
      *> argument-2, spaces if argument-2 is not specified, or argument-1 is of length zero, the returned value
      *> is of length zero."
      *>   python scripts/spec/cite.py --check 15.96.4 "the returned value is of length zero"
      *>   ->  OK  §15.96.4 4)  (Returned value rules)
      *>
      *> The row's editions are 2014,2023: TRIM is COBOL-2014, and the EXPLICIT argument-2 form is the COBOL-2023
      *> enhancement (Annex E.3.3 item 31 — "FUNCTION TRIM. The TRIM function has been enhanced to truncate
      *> removing characters other than space."; the edition gate is pinned by
      *> negative/pb124-trim-all-below-2023). So r4's FIRST antecedent is reached HERE through §15.96.3 r3a's
      *> implied argument-2 ("as though an alphanumeric space had been specified") and at 2023 through a written
      *> argument-2 as well (2023/l1_trim_zero_length_result). The BEHAVIOUR is identical across both editions;
      *> only the syntax available to reach the antecedent differs, which is why there are two files.
      *>
      *>   ALLSP  five spaces, no argument-2 -> r3a supplies the space -> "contains only characters that are
      *>          argument-2" -> length 0.
      *>   LEAD   r4 carries no LEADING/TRAILING qualifier and governs those forms too: r1's "leftmost character
      *>   TRAIL  position that does not contain any argument-2" does not exist here, and r2's "rightmost
      *>          character position after which all characters contain argument-2" leaves nothing -> 0 and 0.
      *>   ZEROL  a DYNAMIC LENGTH item (§8.5.1.10) at current length zero after MOVE "" — r4's third antecedent,
      *>          "argument-1 is of length zero", and the only zero-length DATA ITEM §15.96.3 r1 admits ("a data
      *>          item of class alphabetic, alphanumeric, or national").
      *>   CTRL   the non-empty control: "  AB  " under r3 keeps "AB" -> 2.
      *> Measured with FUNCTION LENGTH of the RESULT, because a MOVE into a fixed receiver renders an empty
      *> result and a correct one identically — §14.9.25.4 GR6a puts the "necessary space filling" on the
      *> RECEIVING operand (§14.6.8), never on the function value. §8.4.3.2.1: "A function-identifier references
      *> the unique data item that results from the evaluation of a function", which §15.50.3 r1 admits.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1TRIMR414.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Q-SP PIC X(5) VALUE SPACES.
       01 Q-D  PIC X DYNAMIC LENGTH LIMIT IS 10.
       01 Q-M  PIC X(6) VALUE "  AB  ".
       01 Q-N  PIC 9(2).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION LENGTH(FUNCTION TRIM(Q-SP)) TO Q-N.
           DISPLAY "ALLSP=" Q-N.
           MOVE FUNCTION LENGTH(FUNCTION TRIM(Q-SP LEADING)) TO Q-N.
           DISPLAY "LEAD=" Q-N.
           MOVE FUNCTION LENGTH(FUNCTION TRIM(Q-SP TRAILING)) TO Q-N.
           DISPLAY "TRAIL=" Q-N.
           MOVE "" TO Q-D.
           MOVE FUNCTION LENGTH(FUNCTION TRIM(Q-D)) TO Q-N.
           DISPLAY "ZEROL=" Q-N.
           MOVE FUNCTION LENGTH(FUNCTION TRIM(Q-M)) TO Q-N.
           DISPLAY "CTRL=" Q-N.
           STOP RUN.
       END PROGRAM L1TRIMR414.
