      *>--- conformance-case
      *> id: pb488_condition_name_associations_ok_85
      *> rules: [SR-13.16.3-24]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB488 - the COBOL-85 half of the ISO 13.16.3 SR24 over-rejection guard. The screen is
      *> UNGATED, so the shapes SR24 does NOT exclude have to keep compiling at the oldest edition too. Every
      *> leg here is one COBOL-85 can write; the 2023-only shapes (a STRONG type declaration's elementary
      *> member, a national group) are pinned by tests/conformance/2023/pb488_condition_name_associations_ok.
      *>
      *> 1) CONSECUTIVE 88s all associate with P - SR24 a) excludes an 88 from BEING a conditional variable,
      *>    which is what lets a run of them share one. P = "AB": P-AB true, P-CD false, P-AB-OR-CD true.
      *> 2) AN ALPHANUMERIC GROUP OF ALL-DISPLAY MEMBERS - SR24 c) excludes only a group "containing items
      *>    with a usage other than display". 8.8.4.2.1 makes AG's value its members' concatenation "ABCD".
      *> 3) AN 88 AFTER A LEVEL-66 ALIAS - b) excludes the alias, not the entries after it. Q-AB is true.
      *> 4) A GROUP CONTAINING AN OCCURS DEPENDING ON TABLE - 8.5.1.12.1's variable-length group is a group
      *>    with a dynamic-length item or a dynamic-capacity table subordinate, which an ODO group is not, so
      *>    h) does not reach it. "ZZZZZ" differs in the first character position under any extent, so N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB488-COND-ASSOC-OK-85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P PIC X(2) VALUE "AB".
       88 P-AB VALUE "AB".
       88 P-CD VALUE "CD".
       88 P-AB-OR-CD VALUE "AB" "CD".
       01 AG.
       88 AG-COND VALUE "ABCD".
          05 AGX PIC X(2) VALUE "AB".
          05 AGY PIC X(2) VALUE "CD".
       01 REC.
          05 RA PIC X(2) VALUE "AB".
          05 RB PIC X(2) VALUE "CD".
       66 R1 RENAMES RA THRU RB.
       01 Q PIC X(2) VALUE "AB".
       88 Q-AB VALUE "AB".
       01 ODO-G.
       88 ODO-COND VALUE "ZZZZZ".
          05 ODO-N PIC 9 VALUE 2.
          05 ODO-E PIC X(2) OCCURS 1 TO 3 DEPENDING ON ODO-N.
       PROCEDURE DIVISION.
           IF P-AB DISPLAY "P-AB=Y" ELSE DISPLAY "P-AB=N" END-IF
           IF P-CD DISPLAY "P-CD=Y" ELSE DISPLAY "P-CD=N" END-IF
           IF P-AB-OR-CD DISPLAY "P-EITHER=Y" ELSE DISPLAY "P-EITHER=N" END-IF
           IF AG-COND DISPLAY "AG=Y" ELSE DISPLAY "AG=N" END-IF
           IF Q-AB DISPLAY "Q-AB=Y" ELSE DISPLAY "Q-AB=N" END-IF
           IF ODO-COND DISPLAY "ODO=Y" ELSE DISPLAY "ODO=N" END-IF
           STOP RUN.
