*>--- conformance-case
*> id: pb420-initialize-float-to-value-85
*> rules: [GR-14.9.20.4-4]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB420 — the EDITION EDGE of the arm the '85 positive cannot reach. USAGE COMP-2 is COBOL-85
*> latitude (13.18.60.4 GR13's implementor-defined float usages) and the group below is otherwise ordinary
*> COBOL-85 source, so the ONLY thing this program asks for beyond the edition is the INITIALIZE VALUE
*> phrase itself: ISO 14.9.20.2's general format acquired `[ ALL | category-name ] TO VALUE` at COBOL-2002,
*> and with it general rule 5c1 and general rule 6a, the operand-selection and sender rules that phrase
*> carries. At --std 85 the initialize-to-value construct row refuses it, COBOLNET0831.
*> It compiles and runs at 2002 and above — the positive witness for the same statement over the same
*> receiver kind is conformance-suite/cases/14.9.20-INITIALIZE/pb420_initialize_float_value_default_2002, whose V1 arm reads the
*> VALUE clause's literal back out of the float leaves; and the COBOL-85-reachable half of GR4's implicit
*> MOVE into a float receiver (the REPLACING phrase and the bare form) is
*> conformance-suite/cases/14.9.20-INITIALIZE/pb420_initialize_float_receiver_85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB420NV85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 F USAGE COMP-2 VALUE 2.5.
          05 N PIC 9(3) VALUE 7.
       PROCEDURE DIVISION.
       MAIN.
           INITIALIZE G ALL TO VALUE.
           STOP RUN.
