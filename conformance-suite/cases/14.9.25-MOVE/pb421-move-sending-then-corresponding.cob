*>--- conformance-case
*> id: pb421-move-sending-then-corresponding
*> rules: [FMT-14.9.25.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB421 — A CORRESPONDING PHRASE AFTER A MOVE SENDING OPERAND. §14.9.25.2 prints exactly two general
*> formats (PDF page 694): Format 1 `MOVE { identifier-1 | literal-1 } TO { identifier-2 } …` and Format 2
*> `MOVE { CORRESPONDING | CORR } identifier-3 TO identifier-4`. Both place the WHOLE sending specification
*> directly after the verb, so neither admits a sending operand FOLLOWED by CORRESPONDING. Neither format
*> changed shape across 1985/2002/2014/2023.
*> WHAT THIS WITNESS PINS: `moveReceivingPhrase` used to carry a second alternative
*> `(CORRESPONDING | CORR) dataReference TO dataReference`, which composed with `MOVE moveSendingOperand
*> moveReceivingPhrase` to admit this shape. The binder could not build it, fell through to a BoundUnsupported,
*> and the compiler EMITTED A PROGRAM: this source compiled with no diagnostic at all and died at run time with
*> `NotImplementedCobolFeatureException: … MOVE CORRESPONDING / unsupported MOVE form`, claiming an
*> unimplemented COBOL feature about source the standard has no format for. "REACHED" never printed.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB421NEG1.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A PIC X(4) VALUE "ABCD".
01 G1.
   05 F1 PIC X(3) VALUE "XYZ".
01 G2.
   05 F1 PIC X(3).
PROCEDURE DIVISION.
MAIN-PARA.
    MOVE A CORRESPONDING G1 TO G2.
    DISPLAY "REACHED".
    STOP RUN.
