*>--- conformance-case
*> id: pb878-invoke-content-binary-long-to-alnum
*> rules: [SR-14.9.25.3-8]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.8.2.3.3 rule 2d: "Otherwise, the conformance rules are the same as for a MOVE statement with
*> the argument as the sending operand and the corresponding formal parameter as the receiving operand." The
*> WHOLE MOVE question - including 14.9.25.3 SR8: "If identifier-1 references a data item described with usage
*> binary-char, binary-short, binary-long, or binary-double, identifier-2 shall reference a numeric or
*> numeric-edited item." A BINARY-LONG argument BY CONTENT to a PIC X(4) formal is therefore non-conforming.
*> kb/Work PB878: the INVOKE screen asked Table 16 alone (a hand list that admitted every integer numeric
*> sender), so this compiled clean and the method printed LK-X=[0000]. Refused with COBOLNET0828.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB878NOO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB878NC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB878NC.
       01 BL USAGE BINARY-LONG VALUE 42.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB878NC "NEW" RETURNING O.
           INVOKE O "TAKE" USING BY CONTENT BL.
           STOP RUN.
       END PROGRAM PB878NOO.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB878NC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-X PIC X(4).
       PROCEDURE DIVISION USING LK-X.
       MAIN.
           DISPLAY "LK-X=[" LK-X "]".
       END METHOD TAKE.
       END OBJECT.
       END CLASS PB878NC.
