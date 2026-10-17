*>--- conformance-case
*> id: pb980-inspect-national-mix
*> rules: [SR-14.9.22.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB980's INSPECT sibling. ISO 14.9.22.3 SR4: "If any of identifier-1, identifier-3,
      *> identifier-4, identifier-5, identifier-6, identifier-7, literal-1, literal-2, literal-3,
      *> literal-4, or literal-5 references an elementary data item or literal of class boolean or
      *> national, then all shall reference a data item or literal of class boolean or national,
      *> respectively." The national literal N";" replaces within an alphanumeric identifier-1; before
      *> the fix this compiled clean and ran (XS=[AB;C]). The same rule's boolean arm is the same
      *> predicate (AllOrNothingClass), asked with class boolean.
      *> Class national is a COBOL-2002 introduction, so 85 rejects the LITERAL instead and is not
      *> claimed here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB980NEGI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XS  PIC X(4) VALUE "AB,C".
       PROCEDURE DIVISION.
           INSPECT XS REPLACING ALL "," BY N";"
           DISPLAY "XS=[" XS "]"
           STOP RUN.
