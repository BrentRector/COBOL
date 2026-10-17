*>--- conformance-case
*> id: pb890-value-alphabet-without-through
*> rules: [SR-13.18.63.3-31, FMT-13.18.63.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB890 - ISO 1989:2023 13.18.63.3 SR31, first sentence: "Alphabet-name-1 may be specified only
*> when the literals specified in the THROUGH phrase are of class alphanumeric or national."
*> With no THROUGH phrase written anywhere in the clause there are no "literals specified in the THROUGH
*> phrase", so the permission the words "only when" grant is never satisfied and alphabet-name-1 may not be
*> written. This is a SYNTAX rule: it constrains what may be WRITTEN, whether or not the phrase would have
*> an effect at run time. The counter-argument the compiler used to carry - that the phrase is "inert, not
*> erroneous" over a value LIST, because 8.8.4.5.3 GR2 compares singletons under the PROGRAM collating
*> sequence and the IN phrase never reaches them - is a GENERAL-RULE answer to a SYNTAX-RULE question.
*> MEASURED before the fix: this program compiled clean and ran (rc 0). The guard that skipped the screen
*> existed to keep tests/conformance/2002/pb695_value_false_optional_words green, whose line 35 was
*> `88 CN-ORDER VALUE 1 IN AL1 WHEN SET TO FALSE 0.` - a NUMERIC singleton with IN, which SR31 excludes
*> twice over. That golden's operand is now a conforming alphanumeric THROUGH range and the guard is gone.
*> EDITION-INVARIANT: no edition of the clause has ever printed the IN phrase without the THROUGH phrase it
*> orders, and Annex E records no change; MEASURED rejected at all four.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB890A.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    ALPHABET MYALPH IS STANDARD-1.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 CV PIC X VALUE "Y".
   88 CN VALUE "Y" IN MYALPH.
PROCEDURE DIVISION.
    IF CN DISPLAY "IN" END-IF.
    STOP RUN.
