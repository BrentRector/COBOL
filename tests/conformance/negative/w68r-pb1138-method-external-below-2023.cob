*> reject-at: 2002 2014
*> kb/Work PB1138 - the edition floor of the positive golden
*> 2023/w68r_pb1138_method_external_check: a method activation's external-item
*> check (ISO 14.9.23.4 GR7 d) raises the EC-EXTERNAL family, which is new in
*> COBOL-2023 (Annex E.2 item 9) - enabling it by >>TURN at 2002/2014 is COBOLNET0878,
*> never a silent no-op. (At --std 85 the >>TURN directive and the OO facility are
*> refused first, COBOLNET0900.)
      >>TURN EC-EXTERNAL-FORMAT-CONFLICT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W68RNEG2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS W68RNC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O1 USAGE OBJECT REFERENCE W68RNC.
       01 EXX PIC X(8) EXTERNAL.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE W68RNC "NEW" RETURNING O1.
           INVOKE O1 "PEEK".
           STOP RUN.
       END PROGRAM W68RNEG2.
       IDENTIFICATION DIVISION.
       CLASS-ID. W68RNC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 EXX PIC X(4) EXTERNAL.
       PROCEDURE DIVISION.
       METHOD-ID. PEEK.
       PROCEDURE DIVISION.
       P.
           DISPLAY EXX.
       END METHOD PEEK.
       END OBJECT.
       END CLASS W68RNC.
