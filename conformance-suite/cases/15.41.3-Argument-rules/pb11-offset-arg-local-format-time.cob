*>--- conformance-case
*> id: pb11-offset-arg-local-format-time
*> rules: [AR-15.41.3-5]
*> source-format: free
*> portable: unverified
*>---
*> PB11 (the VALUE half) - an offset-from-UTC argument-3 supplied for a format whose time portion is LOCAL.
*> 15.41.3 r5: "argument-3 shall not be specified if the time portion of the format in argument-1 is neither a UTC
*> format nor an offset format." A UTC format ends in 'Z' and an offset format carries an explicit '+hhmm' /
*> '+hh:mm' subformat (15.3.3.4-15.3.3.6); a bare 'hhmmss' is LOCAL and has nowhere to put an offset.
*> Decidable at BIND time because rule 1 makes argument-1 a LITERAL, so the zone is known at compile time and
*> the argument's presence is syntactic - which is why this is COBOLNET1633 and not a run-time check.
*> Before this the argument bound cleanly and was then SILENTLY DISCARDED.
*> The CONVERSE IS LEGAL and is pinned by the positive golden pb11_datetime_format_grammar: omitting the
*> argument for a UTC/offset format is evaluated as though 0 were specified (15.40.3 r7 / 15.41.3 r6).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB11OFF.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 R PIC X(30).
PROCEDURE DIVISION.
MAIN.
    MOVE FUNCTION FORMATTED-TIME("hhmmss", 3600, 60) TO R
    STOP RUN.
