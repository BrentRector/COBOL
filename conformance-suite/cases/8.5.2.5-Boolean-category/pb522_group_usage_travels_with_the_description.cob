*>--- conformance-case
*> id: pb522_group_usage_travels_with_the_description
*> rules: [GR-8.5.2.5-3, GR-8.5.2.10-3, GR-13.16.4-1, GR-13.16.4-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB522 — a data description COPY carries the GROUP-USAGE clause.
      *>
      *> THE RULE. ISO §13.18.57.4 GR1: the TYPE clause's effect "is as though the data description identified
      *> by type-name-1 had been coded in place of the TYPE clause, excluding the level-number, name, alignment,
      *> and the GLOBAL, SELECT WHEN, and TYPEDEF clauses specified for type-name-1". §13.18.49.4 GR1 is the same
      *> sentence for SAME AS, excluding only the level-number, name, CONSTANT RECORD, EXTERNAL, GLOBAL,
      *> REDEFINES and SELECT WHEN. Both state what is LEFT OUT, and GROUP-USAGE (§13.18.29) is in NEITHER list,
      *> so it travels — as §13.18.49.4 GR4, which propagates a GROUP-USAGE from data-name-1's SUPERORDINATE
      *> onto the subject, confirms. §13.18.58.4 GR1 reproduces a type declaration's subordinate entries whole,
      *> so a GROUP-USAGE clause written on a group NESTED inside a TYPEDEF travels too.
      *>
      *> EXPECTED VALUES, DERIVED — never measured. §8.5.2.5 item 3 / §8.5.2.10 item 3 make a group "explicitly
      *> or implicitly described with a GROUP-USAGE clause with the BIT [NATIONAL] phrase" of category boolean
      *> [national], and §15.50.4 then fixes the UNIT the LENGTH function counts in:
      *>   r1) a BIT GROUP item -> boolean positions;
      *>   r2) a NATIONAL GROUP item -> national character positions;
      *>   r3) anything else -> alphanumeric character positions.
      *> §13.16.4 GR1/GR2 imply the clause for every group SUBORDINATE to a bit / national group, so the inner
      *> groups answer in the same unit as their records. Hence:
      *>   a national group over PIC N(4)      = 4 national character positions;
      *>   a bit group over PIC 1(3) USAGE BIT = 3 boolean positions;
      *>   a bit group over PIC 1(5) USAGE BIT = 5 boolean positions;
      *>   the nested national group over PIC N(3) = 3 national character positions.
      *> Every record below is paired with a BYTE-IDENTICAL INLINE CONTROL, so the golden states the rule rather
      *> than a number: the copy shall measure exactly what its written-out spelling measures.
      *>
      *> §13.18.29.4 GR2b additionally treats a national group "as though it were an elementary data item of
      *> usage national and class and category national described with PICTURE N(m), where m is the length of the
      *> group" — so the whole group is ONE national operand. CMP= pins that: the copy is an elementary
      *> national operand exactly as its control is, which an alphanumeric group could not be.
      *>
      *> Before this fix BOTH copy helpers omitted GroupUsage, so every VIA- / SAME- record below bound as an
      *> ORDINARY alphanumeric group and the four lines read A=8 8 CTL=4 4 / B=1 1 CTL=3 3 / C=8 8 1 1 CTL=4 5
      *> / D=6 CTL=3 — every copy disagreeing with its own byte-identical control, with no diagnostic.
      *>
      *> COBOL-2002: GROUP-USAGE, TYPEDEF/TYPE and SAME AS are all 2002 introductions — the negative twin
      *> conformance-suite/cases/13.16-Data-description/pb522-group-usage-copy-below-2002.cob pins the gate below it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB522GUCOPY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
      *> (A) TYPE over a NATIONAL group type declaration, and its inline control.
       01 NDEF IS TYPEDEF GROUP-USAGE NATIONAL.
          02 NG1.
             03 NN PIC N(4).
       01 VIA-TYPE-N TYPE NDEF.
       01 INLINE-N GROUP-USAGE NATIONAL.
          02 ING1.
             03 INN PIC N(4).
      *> (B) TYPE over a BIT group type declaration, and its inline control.
       01 BDEF IS TYPEDEF GROUP-USAGE BIT.
          02 BG1.
             03 BB PIC 1(3) USAGE BIT.
       01 VIA-TYPE-B TYPE BDEF.
       01 INLINE-B GROUP-USAGE BIT.
          02 IBG1.
             03 IBB PIC 1(3) USAGE BIT.
      *> (C) SAME AS over a national and over a bit record.
       01 SRC-N GROUP-USAGE NATIONAL.
          02 SNG.
             03 SNN PIC N(4).
       01 SAME-N SAME AS SRC-N.
       01 SRC-B GROUP-USAGE BIT.
          02 SBG.
             03 SBN PIC 1(5) USAGE BIT.
       01 SAME-B SAME AS SRC-B.
      *> (D) A GROUP-USAGE clause on a group NESTED inside a type declaration (§13.18.58.4 GR1).
       01 MIXDEF IS TYPEDEF.
          02 PLAIN-X PIC X(2).
          02 INNER-N GROUP-USAGE NATIONAL.
             03 INNER-NN PIC N(3).
       01 VIA-TYPE-MIX TYPE MIXDEF.
       01 INLINE-MIX.
          02 IPLAIN-X PIC X(2).
          02 IINNER-N GROUP-USAGE NATIONAL.
             03 IINNER-NN PIC N(3).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "A=" FUNCTION LENGTH(VIA-TYPE-N)
               " " FUNCTION LENGTH(NG1 OF VIA-TYPE-N)
               " CTL=" FUNCTION LENGTH(INLINE-N)
               " " FUNCTION LENGTH(ING1)
           DISPLAY "B=" FUNCTION LENGTH(VIA-TYPE-B)
               " " FUNCTION LENGTH(BG1 OF VIA-TYPE-B)
               " CTL=" FUNCTION LENGTH(INLINE-B)
               " " FUNCTION LENGTH(IBG1)
           DISPLAY "C=" FUNCTION LENGTH(SAME-N)
               " " FUNCTION LENGTH(SNG OF SAME-N)
               " " FUNCTION LENGTH(SAME-B)
               " " FUNCTION LENGTH(SBG OF SAME-B)
               " CTL=" FUNCTION LENGTH(SRC-N)
               " " FUNCTION LENGTH(SRC-B)
           DISPLAY "D=" FUNCTION LENGTH(INNER-N OF VIA-TYPE-MIX)
               " CTL=" FUNCTION LENGTH(IINNER-N)
           MOVE N"WXYZ" TO VIA-TYPE-N
           MOVE N"WXYZ" TO INLINE-N
           IF VIA-TYPE-N = INLINE-N
               DISPLAY "CMP=OK"
           ELSE
               DISPLAY "CMP=BAD"
           END-IF
           STOP RUN.
