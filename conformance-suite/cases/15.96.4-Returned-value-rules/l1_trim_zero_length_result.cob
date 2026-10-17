*>--- conformance-case
*> id: l1_trim_zero_length_result
*> rules: [RV-15.96.4-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.96.4 r4 — FUNCTION TRIM RETURNS A ZERO-LENGTH VALUE ON EACH OF THE RULE'S THREE ANTECEDENTS.
      *> r4 word for word: "If argument-1 contains only characters that are argument-2, spaces if argument-2 is
      *> not specified, or argument-1 is of length zero, the returned value is of length zero."
      *>   python scripts/spec/cite.py --check 15.96.4 "If argument-1 contains only characters that are
      *>   argument-2, spaces if argument-2 is not specified, or argument-1 is of length zero, the returned value
      *>   is of length zero."  ->  OK  §15.96.4 4)  (Returned value rules)
      *>
      *> ⛔ THE INSTRUMENT IS FUNCTION LENGTH OF THE RESULT, NOT A MOVE. r4 is a statement about the LENGTH of
      *> the returned value, and a MOVE into a fixed receiver renders an EMPTY result and a correct one
      *> identically — §14.9.25.4 GR6a: "When an alphanumeric, alphanumeric-edited, national, or national-edited
      *> data item is a receiving operand, alignment and any necessary space filling shall take place as defined
      *> in 14.6.8" — so the fill is the RECEIVER's, not the function value's, and every one of the TRIM call
      *> sites already in the corpus would read the same whether r4 held or not.
      *> §8.4.3.1.2's Format 1 is "function-identifier-1", so a function reference IS an identifier, and
      *> §8.4.3.2.1 says "A function-identifier references the unique data item that results from the evaluation
      *> of a function" — exactly what §15.50.3 r1 admits ("a data item of any class or category").
      *>
      *>   ALLSP  argument-1 is five spaces and NO argument-2 is written, so §15.96.3 r3a supplies argument-2
      *>          "as though an alphanumeric space had been specified" — argument-1 then "contains only
      *>          characters that are argument-2" -> length 0.
      *>   LEAD   r4 carries NO LEADING/TRAILING qualifier, so it governs those forms too: r1's "leftmost
      *>   TRAIL  character position that does not contain any argument-2" does not exist for an all-space
      *>          argument-1, and r2's "rightmost character position after which all characters contain
      *>          argument-2" likewise leaves nothing -> 0 and 0.
      *>   ALLA2  argument-1 is "0000" and argument-2 is the single character "0" — the explicit argument-2 form,
      *>          COBOL-2023 (Annex E.3.3 item 31: "FUNCTION TRIM. The TRIM function has been enhanced to
      *>          truncate removing characters other than space."). r4's FIRST antecedent, read literally -> 0.
      *>   ZEROL  argument-1 is a DYNAMIC LENGTH item (§8.5.1.10) whose CURRENT length is zero after MOVE "" —
      *>          r4's third antecedent, "argument-1 is of length zero". §15.96.3 r1 requires argument-1 to be
      *>          "a DATA ITEM of class alphabetic, alphanumeric, or national", and a dynamic-length elementary
      *>          item at current length zero is the only zero-length data item the language provides; a
      *>          zero-length LITERAL would not satisfy r1, which is why this leg is not written
      *>          FUNCTION TRIM("").
      *>   CTRL   the control that makes the five zeros a MEASUREMENT rather than a broken channel: "  AB  "
      *>          under r3 keeps "AB" -> 2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1TRIMR4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-SP PIC X(5) VALUE SPACES.
       01 T-Z  PIC X(4) VALUE "0000".
       01 T-D  PIC X DYNAMIC LENGTH LIMIT IS 10.
       01 T-M  PIC X(6) VALUE "  AB  ".
       01 T-N  PIC 9(2).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION LENGTH(FUNCTION TRIM(T-SP)) TO T-N.
           DISPLAY "ALLSP=" T-N.
           MOVE FUNCTION LENGTH(FUNCTION TRIM(T-SP LEADING)) TO T-N.
           DISPLAY "LEAD=" T-N.
           MOVE FUNCTION LENGTH(FUNCTION TRIM(T-SP TRAILING)) TO T-N.
           DISPLAY "TRAIL=" T-N.
           MOVE FUNCTION LENGTH(FUNCTION TRIM(T-Z "0")) TO T-N.
           DISPLAY "ALLA2=" T-N.
           MOVE "" TO T-D.
           MOVE FUNCTION LENGTH(FUNCTION TRIM(T-D)) TO T-N.
           DISPLAY "ZEROL=" T-N.
           MOVE FUNCTION LENGTH(FUNCTION TRIM(T-M)) TO T-N.
           DISPLAY "CTRL=" T-N.
           STOP RUN.
       END PROGRAM L1TRIMR4.
