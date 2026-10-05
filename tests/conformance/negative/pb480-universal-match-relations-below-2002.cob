      *> reject-at: 85
      *> kb/Work PB480 - the NEGATIVE twin of tests/conformance/2002/pb480_universal_match_relations (and of
      *> pb1112_active_class_universal). Object orientation - CLASS-ID, USAGE OBJECT REFERENCE and the INVOKE
      *> statement (ISO 13.18.60, 14.9.23) - and the national category (PIC N) are COBOL-2002 introductions that do
      *> not exist in COBOL-85, so the program is rejected at --std 85, gated by COBOLNET0900.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB480NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  U USAGE OBJECT REFERENCE.
       01  A PIC N(3) USAGE NATIONAL.
       PROCEDURE DIVISION.
           INVOKE U "TN" USING A
           STOP RUN.
