*>--- conformance-case
*> id: declined-validate-entry-name-still-0901
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> THE FUNNEL GUARD, not a decline. VersionConformancePass.IsProvableUserWordPosition treats the data
*> description ENTRY-NAME slot as provably a user-defined-word use, and its stated ground used to be "NO
*> dataDescriptionClause alternative begins with a cobolWord-admitted token". The declined-A.4.14
*> validationClause arm makes that FALSE: DEFAULT, DESTINATION, PRESENT, VAL-STATUS and VALIDATE-STATUS all
*> lead an alternative AND all ride cobolWord. This fixture proves the CONCLUSION survives - ANTLR's
*> full-context prediction still lands the word in the NAME slot when the entry parses that way, so the
*> program gets the named 8.9 reserved-word diagnostic and NOT the facility decline.
*> Make it fail once: swap the expected code to COBOLNET1708 and it goes red.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLNAME.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DESTINATION PIC X(4).
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
