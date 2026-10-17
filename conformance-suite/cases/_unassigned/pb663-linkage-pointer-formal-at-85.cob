*>--- conformance-case
*> id: pb663-linkage-pointer-formal-at-85
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.60 / 8.5.2.6 - USAGE POINTER is a COBOL-2002 introduction, so the LINKAGE formal
*> whose carrier kb/Work PB663 added (the class-pointer managed slot, CobolArgAdapt.Slot) does not exist
*> below 2002 at all: the version-conformance pass rejects the data description entry itself with
*> COBOLNET0900 (registry row usage-pointer-2002). The BELOW-EDITION half of the pair whose positive is
*> tests/conformance/2002/pb663_linkage_managed_formal_carrier.cob - the carrier landing must not have
*> made a 2002 construct reachable at 85.
*> The FORMAT-1 half of the same rule (14.9.4.3 SR10 - a Format 1 CALL shall pass no class-pointer item
*> BY REFERENCE, which is why the callee arm is reachable only through a Format 2 activation) is already
*> pinned by pb132-call-byref-pointer; it is deliberately NOT duplicated here.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB663P85P10.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB663C85P10.
DATA DIVISION.
LINKAGE SECTION.
01 L-PTR USAGE POINTER.
PROCEDURE DIVISION USING L-PTR.
M.
    EXIT PROGRAM.
END PROGRAM PB663C85P10.
END PROGRAM PB663P85P10.
