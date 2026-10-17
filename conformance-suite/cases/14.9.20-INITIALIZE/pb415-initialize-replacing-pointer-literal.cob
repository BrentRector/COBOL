*>--- conformance-case
*> id: pb415-initialize-replacing-pointer-literal
*> rules: [SR-14.9.20.3-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB415 — ISO 14.9.20.3 SR3: "For each DATA-POINTER, FUNCTION-POINTER, MESSAGE-TAG,
*> OBJECT-REFERENCE, or PROGRAM-POINTER phrase specified as the category-name in the REPLACING phrase,
*> identifier-2 shall be specified." literal-1 is what the rule excludes, and 14.9.20.4 GR4 says why: for
*> exactly those five categories "the implicit statement is: SET receiving-operand TO sending-operand", and
*> no format of the SET statement (14.9.39) admits a literal as its sending operand.
*> The rule was UNREACHABLE before PB415 — the five category-names could not be spelled at all, so the
*> inventory row SR-14.9.20.3-3 stood NOT-IMPLEMENTED with no program able to demonstrate it either way.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB415NLIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P1 USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           INITIALIZE P1 REPLACING DATA-POINTER DATA BY "ZZZZ".
           STOP RUN.
