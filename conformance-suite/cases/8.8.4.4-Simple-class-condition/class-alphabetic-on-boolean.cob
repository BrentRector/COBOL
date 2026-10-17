*>--- conformance-case
*> id: class-alphabetic-on-boolean
*> rules: [SR-8.8.4.4.3-4]
*> source-format: free
*> portable: unverified
*>---
*> ISO 8.8.4.4.3 SR4, as written: "ALPHABETIC, ALPHABETIC-LOWER, ALPHABETIC-UPPER, or class-name-1
*> shall not be specified if the category of the data item referenced by identifier-1 is boolean,
*> numeric, or numeric-edited." W-B is category boolean, so the ALPHABETIC alternative is refused.
*> ⚠ The .err pins SR4's OWN WORDS, not a paraphrase of the boolean third of them: COBOLNET0844 used
*> to say "shall not be specified for a boolean operand" because the screen tested only that one of
*> the three categories the rule names (kb/Work PB571 — the class-condition table landing broadened
*> it to the rule as written, and this case is the one that pinned the narrow wording).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGNB11.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-B PIC 1(4) VALUE B"0101".
PROCEDURE DIVISION.
MAIN.
    IF W-B IS ALPHABETIC DISPLAY "A".
    STOP RUN.
