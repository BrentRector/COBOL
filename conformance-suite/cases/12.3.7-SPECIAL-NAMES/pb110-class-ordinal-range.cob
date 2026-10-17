*>--- conformance-case
*> id: pb110-class-ordinal-range
*> rules: [SR-12.3.7.3-17, GR-12.3.7.4-12]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.7.3 SR17 b2: a numeric literal-5 "shall have a value within the range of one through the maximum
      *> number of characters in the native alphanumeric character set" (65 536 here - the UTF-16 repertoire, CA26),
      *> "or, when the IN phrase is specified, the maximum number of characters in the character set referenced by
      *> alphabet-name-4" - COBOLNET1671 (kb/Work PB110; ordinals above 256 used to degrade to raw text silently).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB110CR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CLASS C2 IS 70000.
       PROCEDURE DIVISION.
           STOP RUN.
