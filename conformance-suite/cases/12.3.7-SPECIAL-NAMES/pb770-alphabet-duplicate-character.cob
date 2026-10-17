*>--- conformance-case
*> id: pb770-alphabet-duplicate-character
*> rules: [SR-12.3.7.3-14]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.7.3 SR14 a: "A given character shall not be specified more than once in that ALPHABET
      *> clause." 'A' is already inside "A" THROUGH "Z", so this phrase specifies it twice. Both the
      *> alphanumeric and the national arm carried a `// SR14a duplicate - first wins (diagnostic later)`
      *> comment at the assignment and silently kept the first occurrence (kb/Work PB770 leg a). "First wins"
      *> survives only as the RECOVERY posture, so the rest of the program still binds; it is not the answer.
      *> (The CROSS-NOTATION duplicate - x'41' against 'A' - is pinned by pb770-alphabet-syn-definition-1849,
      *> which needs the 2002+ hexadecimal literal format.)
     
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB770ALPHABETDUPLICA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET TESTME IS "A" THROUGH "Z", "A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FILLER PIC X.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
