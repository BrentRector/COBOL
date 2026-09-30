      *> reject-at: 2002 2014 2023
      *> kb/Work PB1308, decision R62 - a METHOD's WORKING-STORAGE SECTION is illegal at EVERY edition.
      *> ISO §13.5.3 SR1 (cite.py --check 13.5.3 "only in a factory definition or an instance definition, but not
      *> in a method definition" -> OK 13.5.3 1)): "Within a class definition, the working-storage section may be
      *> specified only in a factory definition or an instance definition, but not in a method definition."
      *> Annex E.2 lists no 2023 removal of it (it does list EXIT METHOD's), and the vendor documentation that
      *> follows the 2002 OO model states the same sentence, so the ban is original text, not a 2023 window: this
      *> program is refused at 2002 and 2014 exactly as at 2023 (COBOLNET1519, the method-placement class the
      *> FILE / REPORT / SCREEN sections of a method already draw). The method's storage is LOCAL-STORAGE and
      *> LINKAGE; data shared across activations lives in the factory or instance definition - see the positive
      *> golden 2002/pb1308_method_storage_is_local_or_object.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1308N1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1308NC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1 USAGE OBJECT REFERENCE PB1308NC.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1308NC "NEW" RETURNING T1.
           INVOKE T1 "TICK".
           INVOKE T1 "TICK".
           STOP RUN.
       END PROGRAM PB1308N1.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1308NC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TICK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-CTR PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           ADD 1 TO WS-CTR.
           DISPLAY "CTR=" WS-CTR.
       END METHOD TICK.
       END OBJECT.
       END CLASS PB1308NC.
