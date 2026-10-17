*>--- conformance-case
*> id: pb678-sort-collating-for-forms-at-85
*> rules: [GR-14.9.24.4-5, GR-14.9.40.4-5]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.40.2 - the SORT/MERGE COLLATING SEQUENCE phrase's FOR ALPHANUMERIC /
*> FOR NATIONAL forms (and the bare alphabet-name-2 second word) are COBOL-2002 introductions:
*> alphabet-name-2 exists to name the sequence for keys of class NATIONAL (14.9.40.3 SR2 +
*> 14.9.40.4 GR5a) and the national class arrived in 2002. At --std 85 the version-conformance
*> pass rejects on recognition (COBOLNET0872).
*>
*> THIS CASE EXISTS BECAUSE THE GATE BECAME LOAD-BEARING (kb/Work PB678). Until PB678 the binder
*> resolved and validated alphabet-name-2 and then THREW IT AWAY, so the gate guarded nothing an
*> execution could observe; now the resolved sequence reaches the key comparator, and this is the
*> only thing standing between a COBOL-85 compilation and a 2002 phrase. The key here is
*> deliberately ALPHANUMERIC so 0872 is the ONLY verdict - a national key would also draw the
*> national-class gate and hide which rule did the rejecting.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB678SRT85AN.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    ALPHABET REV-AN IS "CBA".
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SF ASSIGN TO "pb678srt85.tmp".
DATA DIVISION.
FILE SECTION.
SD SF.
01 SR.
   05 SAK PIC X(1).
   05 SP  PIC X(3).
WORKING-STORAGE SECTION.
01 DONE-FLAG PIC X VALUE "N".
PROCEDURE DIVISION.
MAIN.
    SORT SF ON ASCENDING KEY SAK
        COLLATING SEQUENCE FOR ALPHANUMERIC IS REV-AN
        INPUT PROCEDURE IS FEED
        OUTPUT PROCEDURE IS DRAIN.
    STOP RUN.
FEED.
    MOVE "B" TO SAK. MOVE "BBB" TO SP.
    RELEASE SR.
    MOVE "A" TO SAK. MOVE "AAA" TO SP.
    RELEASE SR.
DRAIN.
    PERFORM UNTIL DONE-FLAG = "Y"
        RETURN SF RECORD
            AT END MOVE "Y" TO DONE-FLAG
            NOT AT END DISPLAY SP
        END-RETURN
    END-PERFORM.
