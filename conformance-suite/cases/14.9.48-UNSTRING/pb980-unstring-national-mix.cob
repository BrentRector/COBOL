*>--- conformance-case
*> id: pb980-unstring-national-mix
*> rules: [SR-14.9.48.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB980. ISO 14.9.48.3 SR3: "If any of identifier-1, identifier-2, identifier-3,
      *> identifier-4, identifier-5, literal-1, or literal-2 are of category national, then all shall be
      *> of category national." The sender S and the delimiter are alphanumeric and the receiver NR is
      *> national; before the fix this compiled clean and ran (NR=[AB ]).
      *> Category national (PICTURE N) is a COBOL-2002 introduction, so 85 rejects the DECLARATION
      *> instead and is not claimed here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB980NEGU.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S   PIC X(4) VALUE "AB,7".
       01 NR  PIC N(3).
       PROCEDURE DIVISION.
           UNSTRING S DELIMITED BY "," INTO NR
           DISPLAY "NR=[" NR "]"
           STOP RUN.
