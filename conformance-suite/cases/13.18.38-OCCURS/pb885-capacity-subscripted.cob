*>--- conformance-case
*> id: pb885-capacity-subscripted
*> rules: [SR-13.18.38.3-31]
*> source-format: free
*> portable: unverified
*>---
      *> A CAPACITY IN OPERAND SHALL NOT BE SUBSCRIPTED (kb/Work PB885's sibling sweep).
      *> ISO/IEC 1989:2023 §13.18.38.3 SR31: "Data-name-3 shall not be subscripted." The capture was the
      *> whole reference's text, so CAPACITY IN CAP3 (1) silently DEFINED a register spelled CAP3(1); it is
      *> now the ONE data-name-n screen (COBOLNET2024). OCCURS DYNAMIC is COBOL-2014, so 1985 and 2002
      *> reject it earlier, under the edition gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB885CPS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T3.
          05 E3 PIC X OCCURS DYNAMIC CAPACITY IN CAP3 (1).
       PROCEDURE DIVISION.
           STOP RUN.
