*>--- conformance-case
*> id: pb416-initialize-replacing-zero-to-alphabetic
*> rules: [SR-14.9.20.3-4, SR-14.9.25.3-6]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB416 — ISO 14.9.20.3 SR4 again, but reaching a MOVE rule that TABLE 16 CANNOT STATE:
*> 14.9.25.3 SR6, "The figurative constant ZERO shall not be moved to an alphabetic data item."
*> The table cannot be the rule that refuses this, because 8.3.3.6.4 GR4 gives ZERO no fixed category — it is
*> "the numeric value '0', one or more of the boolean character '0', or one or more of the character '0' …
*> depending on context" — so a figurative constant has no Table-16 row at all. SR6 is written precisely to
*> cover what the table cannot, and SR4's "shall be valid" means the WHOLE MOVE statement, not the table alone.
*> ALL ZERO is the same figurative constant (8.3.3.6.2 Format 1 prints ALL as an optional, un-underlined word)
*> and is refused identically; ALL "0" is Format 6's ALL-literal, a DIFFERENT figurative constant that Table 16
*> puts in the Alphanumeric row, which the Alphabetic column admits.
*> Measured before PB416: this compiled clean and stored "0000" into a PIC A(4) item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB416NZA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 AB PIC A(4) VALUE "wxyz".
       PROCEDURE DIVISION.
       MAIN.
           INITIALIZE G REPLACING ALPHABETIC DATA BY ZERO.
           STOP RUN.
