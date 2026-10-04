000100 IDENTIFICATION DIVISION.                                         OBSQ34.2
000200 PROGRAM-ID.                                                      OBSQ34.2
000300     OBSQ3A.                                                      OBSQ34.2
000400****************************************************************  OBSQ34.2
000500*                                                              *  OBSQ34.2
000600*    VALIDATION FOR:-                                          *  OBSQ34.2
000700*    " HIGH       ".                                              OBSQ34.2
000800*    USING CCVS85 VERSION 1.0 ISSUED IN JANUARY 1986.          *  OBSQ34.2
000900*                                                              *  OBSQ34.2
001000*    CREATION DATE     /     VALIDATION DATE                   *  OBSQ34.2
001100*    "4.2 ".                                                      OBSQ34.2
001200*                                                              *  OBSQ34.2
001300*    THIS ROUTINE TESTS THE USE OF MULTIPLE FILE CLAUSE           OBSQ34.2
001400*    OF THE I-O-CONTROL PARAGRAPH.  TWO TAPES ARE CREATED         OBSQ34.2
001500*    CONTAINING 4 FILES EACH.  TAPE ONE IS CREATED WITHOUT THE    OBSQ34.2
001600*    USE OF THE NO REWIND OPTION WITH THE OPEN AND CLOSE          OBSQ34.2
001700*   STATEMENTS. IT IS THEN PASSED ON TO OBSQ4A AND OBSQ5A WHERE ITOBSQ34.2
001800*    IS READ AND VALIDATED.  TAPE TWO IS CREATED USING THE        OBSQ34.2
001900*    POSITION PHRASE OF THE MULTIPLE FILE CLAUSE AND WITH THE USE OBSQ34.2
002000*    OF THE NO REWIND OPTION WITH THE OPEN AND CLOSE STATEMENT.   OBSQ34.2
002100*    TAPE TWO IS THEN PASSED ON TO OBSQ5A WHERE IT IS READ AND    OBSQ34.2
002200*    VALIDATED.                                                   OBSQ34.2
002300 ENVIRONMENT DIVISION.                                            OBSQ34.2
002400 CONFIGURATION SECTION.                                           OBSQ34.2
002500 SOURCE-COMPUTER.                                                 OBSQ34.2
002600     XXXXX082.                                                    OBSQ34.2
002700 OBJECT-COMPUTER.                                                 OBSQ34.2
002800     XXXXX083.                                                    OBSQ34.2
002900 INPUT-OUTPUT SECTION.                                            OBSQ34.2
003000 FILE-CONTROL.                                                    OBSQ34.2
003100P    SELECT RAW-DATA   ASSIGN TO                                  OBSQ34.2
003200P    XXXXX062                                                     OBSQ34.2
003300P           ORGANIZATION IS INDEXED                               OBSQ34.2
003400P           ACCESS MODE IS RANDOM                                 OBSQ34.2
003500P           RECORD KEY IS RAW-DATA-KEY.                           OBSQ34.2
003600     SELECT PRINT-FILE ASSIGN TO                                  OBSQ34.2
003700     XXXXX055.                                                    OBSQ34.2
003800     SELECT SQ-FS1 ASSIGN TO                                      OBSQ34.2
003900     XXXXP004                                                     OBSQ34.2
004000     ORGANIZATION IS SEQUENTIAL.                                  OBSQ34.2
004100     SELECT SQ-FS2 ASSIGN TO                                      OBSQ34.2
004200     XXXXP008                                                     OBSQ34.2
004300     ACCESS MODE IS SEQUENTIAL.                                   OBSQ34.2
004400     SELECT SQ-FS3 ASSIGN                                         OBSQ34.2
004500     XXXXP009                                                     OBSQ34.2
004600     ORGANIZATION IS SEQUENTIAL.                                  OBSQ34.2
004700     SELECT SQ-FS4 ASSIGN                                         OBSQ34.2
004800     XXXXP010                                                     OBSQ34.2
004900     ACCESS MODE SEQUENTIAL.                                      OBSQ34.2
005000     SELECT SQ-FS5 ASSIGN                                         OBSQ34.2
005100     XXXXP005.                                                    OBSQ34.2
005200     SELECT SQ-FS6 ASSIGN                                         OBSQ34.2
005300     XXXXP011                                                     OBSQ34.2
005400     ORGANIZATION IS SEQUENTIAL.                                  OBSQ34.2
005500     SELECT SQ-FS7 ASSIGN TO                                      OBSQ34.2
005600     XXXXP012                                                     OBSQ34.2
005700     ORGANIZATION IS SEQUENTIAL                                   OBSQ34.2
005800     ACCESS MODE IS SEQUENTIAL.                                   OBSQ34.2
005900     SELECT SQ-FS8 ASSIGN TO                                      OBSQ34.2
006000     XXXXP013                                                     OBSQ34.2
006100     ACCESS MODE IS SEQUENTIAL.                                   OBSQ34.2
006200 I-O-CONTROL.                                                     OBSQ34.2
006300     MULTIPLE FILE TAPE CONTAINS SQ-FS1,                          OBSQ34.2
006400                                 SQ-FS2,                          OBSQ34.2
006500                                 SQ-FS3,                          OBSQ34.2
006600                                 SQ-FS4;                          OBSQ34.2
006700     MULTIPLE FILE TAPE SQ-FS8 POSITION 4,                        OBSQ34.2
006800                        SQ-FS7 POSITION 3,                        OBSQ34.2
006900                        SQ-FS6 POSITION 2,                        OBSQ34.2
007000                        SQ-FS5 POSITION 1.                        OBSQ34.2
007100 DATA DIVISION.                                                   OBSQ34.2
007200 FILE SECTION.                                                    OBSQ34.2
007300P                                                                 OBSQ34.2
007400PFD  RAW-DATA.                                                    OBSQ34.2
007500P                                                                 OBSQ34.2
007600P01  RAW-DATA-SATZ.                                               OBSQ34.2
007700P    05  RAW-DATA-KEY        PIC X(6).                            OBSQ34.2
007800P    05  C-DATE              PIC 9(6).                            OBSQ34.2
007900P    05  C-TIME              PIC 9(8).                            OBSQ34.2
008000P    05  C-NO-OF-TESTS       PIC 99.                              OBSQ34.2
008100P    05  C-OK                PIC 999.                             OBSQ34.2
008200P    05  C-ALL               PIC 999.                             OBSQ34.2
008300P    05  C-FAIL              PIC 999.                             OBSQ34.2
008400P    05  C-DELETED           PIC 999.                             OBSQ34.2
008500P    05  C-INSPECT           PIC 999.                             OBSQ34.2
008600P    05  C-NOTE              PIC X(13).                           OBSQ34.2
008700P    05  C-INDENT            PIC X.                               OBSQ34.2
008800P    05  C-ABORT             PIC X(8).                            OBSQ34.2
008900 FD  PRINT-FILE.                                                  OBSQ34.2
009000 01  PRINT-REC PICTURE X(120).                                    OBSQ34.2
009100 01  DUMMY-RECORD PICTURE X(120).                                 OBSQ34.2
009200 FD  SQ-FS1                                                       OBSQ34.2
009300     LABEL RECORD IS STANDARD                                     OBSQ34.2
009400          .                                                       OBSQ34.2
009500 01  SQ-FS1R1-F-G-120   PIC X(120).                               OBSQ34.2
009600 FD  SQ-FS2                                                       OBSQ34.2
009700     LABEL RECORD STANDARD                                        OBSQ34.2
009800     BLOCK CONTAINS 5 RECORDS.                                    OBSQ34.2
009900 01  SQ-FS2R1-F-G-120   PIC X(120).                               OBSQ34.2
010000 FD  SQ-FS3                                                       OBSQ34.2
010100     LABEL RECORD STANDARD                                        OBSQ34.2
010200     BLOCK CONTAINS 1200 CHARACTERS                               OBSQ34.2
010300     RECORD CONTAINS 120 CHARACTERS.                              OBSQ34.2
010400 01  SQ-FS3R1-F-G-120   PIC X(120).                               OBSQ34.2
010500 FD  SQ-FS4                                                       OBSQ34.2
010600     LABEL RECORDS STANDARD                                       OBSQ34.2
010700     BLOCK 10 RECORDS                                             OBSQ34.2
010800     RECORD 120 CHARACTERS.                                       OBSQ34.2
010900 01  SQ-FS4R1-F-G-120   PIC X(120).                               OBSQ34.2
011000 FD  SQ-FS5                                                       OBSQ34.2
011100     LABEL RECORDS ARE STANDARD                                   OBSQ34.2
011200     BLOCK CONTAINS 5 RECORDS.                                    OBSQ34.2
011300 01  SQ-FS5R1-F-G-120   PIC X(120).                               OBSQ34.2
011400 FD  SQ-FS6                                                       OBSQ34.2
011500     LABEL RECORD IS STANDARD                                     OBSQ34.2
011600     BLOCK CONTAINS 10 RECORDS.                                   OBSQ34.2
011700 01  SQ-FS6R1-F-G-120   PIC X(120).                               OBSQ34.2
011800 FD  SQ-FS7                                                       OBSQ34.2
011900     LABEL RECORD STANDARD                                        OBSQ34.2
012000     BLOCK CONTAINS 2400 CHARACTERS.                              OBSQ34.2
012100 01  SQ-FS7R1-F-G-120   PIC X(120).                               OBSQ34.2
012200 FD  SQ-FS8                                                       OBSQ34.2
012300     LABEL RECORDS ARE STANDARD                                   OBSQ34.2
012400     BLOCK 120 CHARACTERS                                         OBSQ34.2
012500     RECORD 120.                                                  OBSQ34.2
012600 01  SQ-FS8R1-F-G-120   PIC X(120).                               OBSQ34.2
012700 WORKING-STORAGE SECTION.                                         OBSQ34.2
012800 01  COUNT-OF-RECS PIC 9999.                                      OBSQ34.2
012900 01  FILE-RECORD-INFORMATION-REC.                                 OBSQ34.2
013000     03 FILE-RECORD-INFO-SKELETON.                                OBSQ34.2
013100        05 FILLER                 PICTURE X(48)       VALUE       OBSQ34.2
013200             "FILE=      ,RECORD=      /0,RECNO=000000,UPDT=00".  OBSQ34.2
013300        05 FILLER                 PICTURE X(46)       VALUE       OBSQ34.2
013400             ",ODO=0000,PGM=     ,LRECL=000000,BLKSIZ  =0000".    OBSQ34.2
013500        05 FILLER                 PICTURE X(26)       VALUE       OBSQ34.2
013600             ",LFIL=000000,ORG=  ,LBLR= ".                        OBSQ34.2
013700        05 FILLER                 PICTURE X(37)       VALUE       OBSQ34.2
013800             ",RECKEY=                             ".             OBSQ34.2
013900        05 FILLER                 PICTURE X(38)       VALUE       OBSQ34.2
014000             ",ALTKEY1=                             ".            OBSQ34.2
014100        05 FILLER                 PICTURE X(38)       VALUE       OBSQ34.2
014200             ",ALTKEY2=                             ".            OBSQ34.2
014300        05 FILLER                 PICTURE X(7)        VALUE SPACE.OBSQ34.2
014400     03 FILE-RECORD-INFO          OCCURS  10  TIMES.              OBSQ34.2
014500        05 FILE-RECORD-INFO-P1-120.                               OBSQ34.2
014600           07 FILLER              PIC X(5).                       OBSQ34.2
014700           07 XFILE-NAME           PIC X(6).                      OBSQ34.2
014800           07 FILLER              PIC X(8).                       OBSQ34.2
014900           07 XRECORD-NAME         PIC X(6).                      OBSQ34.2
015000           07 FILLER              PIC X(1).                       OBSQ34.2
015100           07 REELUNIT-NUMBER     PIC 9(1).                       OBSQ34.2
015200           07 FILLER              PIC X(7).                       OBSQ34.2
015300           07 XRECORD-NUMBER       PIC 9(6).                      OBSQ34.2
015400           07 FILLER              PIC X(6).                       OBSQ34.2
015500           07 UPDATE-NUMBER       PIC 9(2).                       OBSQ34.2
015600           07 FILLER              PIC X(5).                       OBSQ34.2
015700           07 ODO-NUMBER          PIC 9(4).                       OBSQ34.2
015800           07 FILLER              PIC X(5).                       OBSQ34.2
015900           07 XPROGRAM-NAME        PIC X(5).                      OBSQ34.2
016000           07 FILLER              PIC X(7).                       OBSQ34.2
016100           07 XRECORD-LENGTH       PIC 9(6).                      OBSQ34.2
016200           07 FILLER              PIC X(7).                       OBSQ34.2
016300           07 CHARS-OR-RECORDS    PIC X(2).                       OBSQ34.2
016400           07 FILLER              PIC X(1).                       OBSQ34.2
016500           07 XBLOCK-SIZE          PIC 9(4).                      OBSQ34.2
016600           07 FILLER              PIC X(6).                       OBSQ34.2
016700           07 RECORDS-IN-FILE     PIC 9(6).                       OBSQ34.2
016800           07 FILLER              PIC X(5).                       OBSQ34.2
016900           07 XFILE-ORGANIZATION   PIC X(2).                      OBSQ34.2
017000           07 FILLER              PIC X(6).                       OBSQ34.2
017100           07 XLABEL-TYPE          PIC X(1).                      OBSQ34.2
017200        05 FILE-RECORD-INFO-P121-240.                             OBSQ34.2
017300           07 FILLER              PIC X(8).                       OBSQ34.2
017400           07 XRECORD-KEY          PIC X(29).                     OBSQ34.2
017500           07 FILLER              PIC X(9).                       OBSQ34.2
017600           07 ALTERNATE-KEY1      PIC X(29).                      OBSQ34.2
017700           07 FILLER              PIC X(9).                       OBSQ34.2
017800           07 ALTERNATE-KEY2      PIC X(29).                      OBSQ34.2
017900           07 FILLER              PIC X(7).                       OBSQ34.2
018000 01  TEST-RESULTS.                                                OBSQ34.2
018100     02 FILLER                    PICTURE X VALUE SPACE.          OBSQ34.2
018200     02 FEATURE                   PICTURE X(20) VALUE SPACE.      OBSQ34.2
018300     02 FILLER                    PICTURE X VALUE SPACE.          OBSQ34.2
018400     02 P-OR-F                    PICTURE X(5) VALUE SPACE.       OBSQ34.2
018500     02 FILLER                    PICTURE X  VALUE SPACE.         OBSQ34.2
018600     02  PAR-NAME.                                                OBSQ34.2
018700       03 FILLER PICTURE X(12) VALUE SPACE.                       OBSQ34.2
018800       03  PARDOT-X PICTURE X  VALUE SPACE.                       OBSQ34.2
018900       03 DOTVALUE PICTURE 99  VALUE ZERO.                        OBSQ34.2
019000       03 FILLER PIC X(5) VALUE SPACE.                            OBSQ34.2
019100     02 FILLER PIC X(10) VALUE SPACE.                             OBSQ34.2
019200     02 RE-MARK PIC X(61).                                        OBSQ34.2
019300 01  TEST-COMPUTED.                                               OBSQ34.2
019400     02 FILLER PIC X(30) VALUE SPACE.                             OBSQ34.2
019500     02 FILLER PIC X(17) VALUE "       COMPUTED=".                OBSQ34.2
019600     02 COMPUTED-X.                                               OBSQ34.2
019700     03 COMPUTED-A                PICTURE X(20) VALUE SPACE.      OBSQ34.2
019800     03 COMPUTED-N REDEFINES COMPUTED-A PICTURE -9(9).9(9).       OBSQ34.2
019900     03 COMPUTED-0V18 REDEFINES COMPUTED-A  PICTURE -.9(18).      OBSQ34.2
020000     03 COMPUTED-4V14 REDEFINES COMPUTED-A  PICTURE -9(4).9(14).  OBSQ34.2
020100     03 COMPUTED-14V4 REDEFINES COMPUTED-A  PICTURE -9(14).9(4).  OBSQ34.2
020200     03       CM-18V0 REDEFINES COMPUTED-A.                       OBSQ34.2
020300         04 COMPUTED-18V0                   PICTURE -9(18).       OBSQ34.2
020400         04 FILLER                          PICTURE X.            OBSQ34.2
020500     03 FILLER PIC X(50) VALUE SPACE.                             OBSQ34.2
020600 01  TEST-CORRECT.                                                OBSQ34.2
020700     02 FILLER PIC X(30) VALUE SPACE.                             OBSQ34.2
020800     02 FILLER PIC X(17) VALUE "       CORRECT =".                OBSQ34.2
020900     02 CORRECT-X.                                                OBSQ34.2
021000     03 CORRECT-A                 PICTURE X(20) VALUE SPACE.      OBSQ34.2
021100     03 CORRECT-N REDEFINES CORRECT-A PICTURE -9(9).9(9).         OBSQ34.2
021200     03 CORRECT-0V18 REDEFINES CORRECT-A    PICTURE -.9(18).      OBSQ34.2
021300     03 CORRECT-4V14 REDEFINES CORRECT-A    PICTURE -9(4).9(14).  OBSQ34.2
021400     03 CORRECT-14V4 REDEFINES CORRECT-A    PICTURE -9(14).9(4).  OBSQ34.2
021500     03      CR-18V0 REDEFINES CORRECT-A.                         OBSQ34.2
021600         04 CORRECT-18V0                    PICTURE -9(18).       OBSQ34.2
021700         04 FILLER                          PICTURE X.            OBSQ34.2
021800     03 FILLER PIC X(50) VALUE SPACE.                             OBSQ34.2
021900 01  CCVS-C-1.                                                    OBSQ34.2
022000     02 FILLER PICTURE IS X(99) VALUE IS " FEATURE              PAOBSQ34.2
022100-    "SS  PARAGRAPH-NAME                                          OBSQ34.2
022200-    "        REMARKS".                                           OBSQ34.2
022300     02 FILLER PICTURE IS X(20) VALUE IS SPACE.                   OBSQ34.2
022400 01  CCVS-C-2.                                                    OBSQ34.2
022500     02 FILLER PICTURE IS X VALUE IS SPACE.                       OBSQ34.2
022600     02 FILLER PICTURE IS X(6) VALUE IS "TESTED".                 OBSQ34.2
022700     02 FILLER PICTURE IS X(15) VALUE IS SPACE.                   OBSQ34.2
022800     02 FILLER PICTURE IS X(4) VALUE IS "FAIL".                   OBSQ34.2
022900     02 FILLER PICTURE IS X(94) VALUE IS SPACE.                   OBSQ34.2
023000 01  REC-SKL-SUB PICTURE 9(2) VALUE ZERO.                         OBSQ34.2
023100 01  REC-CT PICTURE 99 VALUE ZERO.                                OBSQ34.2
023200 01  DELETE-CNT                   PICTURE 999  VALUE ZERO.        OBSQ34.2
023300 01  ERROR-COUNTER PICTURE IS 999 VALUE IS ZERO.                  OBSQ34.2
023400 01  INSPECT-COUNTER PIC 999 VALUE ZERO.                          OBSQ34.2
023500 01  PASS-COUNTER PIC 999 VALUE ZERO.                             OBSQ34.2
023600 01  TOTAL-ERROR PIC 999 VALUE ZERO.                              OBSQ34.2
023700 01  ERROR-HOLD PIC 999 VALUE ZERO.                               OBSQ34.2
023800 01  DUMMY-HOLD PIC X(120) VALUE SPACE.                           OBSQ34.2
023900 01  RECORD-COUNT PIC 9(5) VALUE ZERO.                            OBSQ34.2
024000 01  CCVS-H-1.                                                    OBSQ34.2
024100     02  FILLER   PICTURE X(27)  VALUE SPACE.                     OBSQ34.2
024200     02 FILLER PICTURE X(67) VALUE                                OBSQ34.2
024300     " FEDERAL SOFTWARE TESTING CENTER COBOL COMPILER VALIDATION  OBSQ34.2
024400-    " SYSTEM".                                                   OBSQ34.2
024500     02  FILLER     PICTURE X(26)  VALUE SPACE.                   OBSQ34.2
024600 01  CCVS-H-2.                                                    OBSQ34.2
024700     02 FILLER PICTURE X(52) VALUE IS                             OBSQ34.2
024800     "CCVS85 FSTC COPY, NOT FOR DISTRIBUTION.".                   OBSQ34.2
024900     02 FILLER PICTURE IS X(19) VALUE IS "TEST RESULTS SET-  ".   OBSQ34.2
025000     02 TEST-ID PICTURE IS X(9).                                  OBSQ34.2
025100     02 FILLER PICTURE IS X(40) VALUE IS SPACE.                   OBSQ34.2
025200 01  CCVS-H-3.                                                    OBSQ34.2
025300     02  FILLER PICTURE X(34) VALUE                               OBSQ34.2
025400     " FOR OFFICIAL USE ONLY    ".                                OBSQ34.2
025500     02  FILLER PICTURE X(58) VALUE                               OBSQ34.2
025600     "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".OBSQ34.2
025700     02  FILLER PICTURE X(28) VALUE                               OBSQ34.2
025800     "  COPYRIGHT   1985 ".                                       OBSQ34.2
025900 01  CCVS-E-1.                                                    OBSQ34.2
026000     02 FILLER PICTURE IS X(52) VALUE IS SPACE.                   OBSQ34.2
026100     02 FILLER PICTURE IS X(14) VALUE IS "END OF TEST-  ".        OBSQ34.2
026200     02 ID-AGAIN PICTURE IS X(9).                                 OBSQ34.2
026300     02 FILLER PICTURE X(45) VALUE IS                             OBSQ34.2
026400     " NTIS DISTRIBUTION COBOL 85".                               OBSQ34.2
026500 01  CCVS-E-2.                                                    OBSQ34.2
026600     02  FILLER                   PICTURE X(31)  VALUE            OBSQ34.2
026700     SPACE.                                                       OBSQ34.2
026800     02  FILLER                   PICTURE X(21)  VALUE SPACE.     OBSQ34.2
026900     02 CCVS-E-2-2.                                               OBSQ34.2
027000         03 ERROR-TOTAL PICTURE IS XXX VALUE IS SPACE.            OBSQ34.2
027100         03 FILLER PICTURE IS X VALUE IS SPACE.                   OBSQ34.2
027200         03 ENDER-DESC PIC X(46) VALUE "ERRORS ENCOUNTERED".      OBSQ34.2
027300 01  CCVS-E-3.                                                    OBSQ34.2
027400     02  FILLER PICTURE X(22) VALUE                               OBSQ34.2
027500     " FOR OFFICIAL USE ONLY".                                    OBSQ34.2
027600     02  FILLER PICTURE X(12) VALUE SPACE.                        OBSQ34.2
027700     02  FILLER PICTURE X(58) VALUE                               OBSQ34.2
027800     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".OBSQ34.2
027900     02  FILLER PICTURE X(13) VALUE SPACE.                        OBSQ34.2
028000     02 FILLER PIC X(15) VALUE " COPYRIGHT 1985".                 OBSQ34.2
028100 01  CCVS-E-4.                                                    OBSQ34.2
028200     02 CCVS-E-4-1 PIC XXX VALUE SPACE.                           OBSQ34.2
028300     02 FILLER PIC XXXX VALUE " OF ".                             OBSQ34.2
028400     02 CCVS-E-4-2 PIC XXX VALUE SPACE.                           OBSQ34.2
028500     02 FILLER PIC X(40) VALUE                                    OBSQ34.2
028600      "  TESTS WERE EXECUTED SUCCESSFULLY".                       OBSQ34.2
028700 01  XXINFO.                                                      OBSQ34.2
028800     02 FILLER PIC X(30) VALUE "        *** INFORMATION  ***".    OBSQ34.2
028900     02 INFO-TEXT.                                                OBSQ34.2
029000     04 FILLER PIC X(20) VALUE SPACE.                             OBSQ34.2
029100     04 XXCOMPUTED PIC X(20).                                     OBSQ34.2
029200     04 FILLER PIC X(5) VALUE SPACE.                              OBSQ34.2
029300     04 XXCORRECT PIC X(20).                                      OBSQ34.2
029400 01  HYPHEN-LINE.                                                 OBSQ34.2
029500     02 FILLER PICTURE IS X VALUE IS SPACE.                       OBSQ34.2
029600     02 FILLER PICTURE IS X(65) VALUE IS "************************OBSQ34.2
029700-    "*****************************************".                 OBSQ34.2
029800     02 FILLER PICTURE IS X(54) VALUE IS "************************OBSQ34.2
029900-    "******************************".                            OBSQ34.2
030000 01  CCVS-PGM-ID PIC X(6) VALUE                                   OBSQ34.2
030100     "OBSQ3A".                                                    OBSQ34.2
030200 PROCEDURE DIVISION.                                              OBSQ34.2
030300 CCVS1 SECTION.                                                   OBSQ34.2
030400 OPEN-FILES.                                                      OBSQ34.2
030500P    OPEN I-O RAW-DATA.                                           OBSQ34.2
030600P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            OBSQ34.2
030700P    READ RAW-DATA INVALID KEY GO TO END-E-1.                     OBSQ34.2
030800P    MOVE "ABORTED " TO C-ABORT.                                  OBSQ34.2
030900P    ADD 1 TO C-NO-OF-TESTS.                                      OBSQ34.2
031000P    ACCEPT C-DATE  FROM DATE.                                    OBSQ34.2
031100P    ACCEPT C-TIME  FROM TIME.                                    OBSQ34.2
031200P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO END-E-1.             OBSQ34.2
031300PEND-E-1.                                                         OBSQ34.2
031400P    CLOSE RAW-DATA.                                              OBSQ34.2
031500     OPEN     OUTPUT PRINT-FILE.                                  OBSQ34.2
031600     MOVE CCVS-PGM-ID TO TEST-ID. MOVE CCVS-PGM-ID TO ID-AGAIN.   OBSQ34.2
031700     MOVE    SPACE TO TEST-RESULTS.                               OBSQ34.2
031800     PERFORM  HEAD-ROUTINE THRU COLUMN-NAMES-ROUTINE.             OBSQ34.2
031900     MOVE ZERO TO REC-SKL-SUB.                                    OBSQ34.2
032000     PERFORM CCVS-INIT-FILE 9 TIMES.                              OBSQ34.2
032100 CCVS-INIT-FILE.                                                  OBSQ34.2
032200     ADD 1 TO REC-SKL-SUB.                                        OBSQ34.2
032300     MOVE FILE-RECORD-INFO-SKELETON TO                            OBSQ34.2
032400                  FILE-RECORD-INFO (REC-SKL-SUB).                 OBSQ34.2
032500 CCVS-INIT-EXIT.                                                  OBSQ34.2
032600     GO TO CCVS1-EXIT.                                            OBSQ34.2
032700 CLOSE-FILES.                                                     OBSQ34.2
032800     PERFORM END-ROUTINE THRU END-ROUTINE-13. CLOSE PRINT-FILE.   OBSQ34.2
032900P    OPEN I-O RAW-DATA.                                           OBSQ34.2
033000P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            OBSQ34.2
033100P    READ RAW-DATA INVALID KEY GO TO END-E-2.                     OBSQ34.2
033200P    MOVE "OK.     " TO C-ABORT.                                  OBSQ34.2
033300P    MOVE PASS-COUNTER TO C-OK.                                   OBSQ34.2
033400P    MOVE ERROR-HOLD   TO C-ALL.                                  OBSQ34.2
033500P    MOVE ERROR-COUNTER TO C-FAIL.                                OBSQ34.2
033600P    MOVE DELETE-CNT TO C-DELETED.                                OBSQ34.2
033700P    MOVE INSPECT-COUNTER TO C-INSPECT.                           OBSQ34.2
033800P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO END-E-2.             OBSQ34.2
033900PEND-E-2.                                                         OBSQ34.2
034000P    CLOSE RAW-DATA.                                              OBSQ34.2
034100 TERMINATE-CCVS.                                                  OBSQ34.2
034200S    EXIT PROGRAM.                                                OBSQ34.2
034300STERMINATE-CALL.                                                  OBSQ34.2
034400     STOP     RUN.                                                OBSQ34.2
034500 INSPT. MOVE "INSPT" TO P-OR-F. ADD 1 TO INSPECT-COUNTER.         OBSQ34.2
034600 PASS.  MOVE "PASS " TO P-OR-F.  ADD 1 TO PASS-COUNTER.           OBSQ34.2
034700 FAIL.  MOVE "FAIL*" TO P-OR-F.  ADD 1 TO ERROR-COUNTER.          OBSQ34.2
034800 DE-LETE.  MOVE "*****" TO P-OR-F.  ADD 1 TO DELETE-CNT.          OBSQ34.2
034900     MOVE "****TEST DELETED****" TO RE-MARK.                      OBSQ34.2
035000 PRINT-DETAIL.                                                    OBSQ34.2
035100     IF REC-CT NOT EQUAL TO ZERO                                  OBSQ34.2
035200             MOVE "." TO PARDOT-X                                 OBSQ34.2
035300             MOVE REC-CT TO DOTVALUE.                             OBSQ34.2
035400     MOVE     TEST-RESULTS TO PRINT-REC. PERFORM WRITE-LINE.      OBSQ34.2
035500     IF P-OR-F EQUAL TO "FAIL*"  PERFORM WRITE-LINE               OBSQ34.2
035600        PERFORM FAIL-ROUTINE THRU FAIL-ROUTINE-EX                 OBSQ34.2
035700          ELSE PERFORM BAIL-OUT THRU BAIL-OUT-EX.                 OBSQ34.2
035800     MOVE SPACE TO P-OR-F. MOVE SPACE TO COMPUTED-X.              OBSQ34.2
035900     MOVE SPACE TO CORRECT-X.                                     OBSQ34.2
036000     IF     REC-CT EQUAL TO ZERO  MOVE SPACE TO PAR-NAME.         OBSQ34.2
036100     MOVE     SPACE TO RE-MARK.                                   OBSQ34.2
036200 HEAD-ROUTINE.                                                    OBSQ34.2
036300     MOVE CCVS-H-1 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   OBSQ34.2
036400     MOVE CCVS-H-2 TO DUMMY-RECORD. PERFORM WRITE-LINE 5 TIMES.   OBSQ34.2
036500     MOVE CCVS-H-3 TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.   OBSQ34.2
036600 COLUMN-NAMES-ROUTINE.                                            OBSQ34.2
036700     MOVE CCVS-C-1 TO DUMMY-RECORD. PERFORM WRITE-LINE.           OBSQ34.2
036800     MOVE CCVS-C-2 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   OBSQ34.2
036900     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE.        OBSQ34.2
037000 END-ROUTINE.                                                     OBSQ34.2
037100     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE 5 TIMES.OBSQ34.2
037200 END-RTN-EXIT.                                                    OBSQ34.2
037300     MOVE CCVS-E-1 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   OBSQ34.2
037400 END-ROUTINE-1.                                                   OBSQ34.2
037500      ADD ERROR-COUNTER TO ERROR-HOLD ADD INSPECT-COUNTER TO      OBSQ34.2
037600      ERROR-HOLD. ADD DELETE-CNT TO ERROR-HOLD.                   OBSQ34.2
037700      ADD PASS-COUNTER TO ERROR-HOLD.                             OBSQ34.2
037800*     IF PASS-COUNTER EQUAL TO ERROR-HOLD GO TO END-ROUTINE-12.   OBSQ34.2
037900      MOVE PASS-COUNTER TO CCVS-E-4-1.                            OBSQ34.2
038000      MOVE ERROR-HOLD TO CCVS-E-4-2.                              OBSQ34.2
038100      MOVE CCVS-E-4 TO CCVS-E-2-2.                                OBSQ34.2
038200      MOVE CCVS-E-2 TO DUMMY-RECORD PERFORM WRITE-LINE.           OBSQ34.2
038300  END-ROUTINE-12.                                                 OBSQ34.2
038400      MOVE "TEST(S) FAILED" TO ENDER-DESC.                        OBSQ34.2
038500     IF       ERROR-COUNTER IS EQUAL TO ZERO                      OBSQ34.2
038600         MOVE "NO " TO ERROR-TOTAL                                OBSQ34.2
038700         ELSE                                                     OBSQ34.2
038800         MOVE ERROR-COUNTER TO ERROR-TOTAL.                       OBSQ34.2
038900     MOVE     CCVS-E-2 TO DUMMY-RECORD.                           OBSQ34.2
039000     PERFORM WRITE-LINE.                                          OBSQ34.2
039100 END-ROUTINE-13.                                                  OBSQ34.2
039200     IF DELETE-CNT IS EQUAL TO ZERO                               OBSQ34.2
039300         MOVE "NO " TO ERROR-TOTAL  ELSE                          OBSQ34.2
039400         MOVE DELETE-CNT TO ERROR-TOTAL.                          OBSQ34.2
039500     MOVE "TEST(S) DELETED     " TO ENDER-DESC.                   OBSQ34.2
039600     MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.           OBSQ34.2
039700      IF   INSPECT-COUNTER EQUAL TO ZERO                          OBSQ34.2
039800          MOVE "NO " TO ERROR-TOTAL                               OBSQ34.2
039900      ELSE MOVE INSPECT-COUNTER TO ERROR-TOTAL.                   OBSQ34.2
040000      MOVE "TEST(S) REQUIRE INSPECTION" TO ENDER-DESC.            OBSQ34.2
040100      MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.          OBSQ34.2
040200     MOVE CCVS-E-3 TO DUMMY-RECORD. PERFORM WRITE-LINE.           OBSQ34.2
040300 WRITE-LINE.                                                      OBSQ34.2
040400     ADD 1 TO RECORD-COUNT.                                       OBSQ34.2
040500Y    IF RECORD-COUNT GREATER 50                                   OBSQ34.2
040600Y        MOVE DUMMY-RECORD TO DUMMY-HOLD                          OBSQ34.2
040700Y        MOVE SPACE TO DUMMY-RECORD                               OBSQ34.2
040800Y        WRITE DUMMY-RECORD AFTER ADVANCING PAGE                  OBSQ34.2
040900Y        MOVE CCVS-C-1 TO DUMMY-RECORD PERFORM WRT-LN             OBSQ34.2
041000Y        MOVE CCVS-C-2 TO DUMMY-RECORD PERFORM WRT-LN 2 TIMES     OBSQ34.2
041100Y        MOVE HYPHEN-LINE TO DUMMY-RECORD PERFORM WRT-LN          OBSQ34.2
041200Y        MOVE DUMMY-HOLD TO DUMMY-RECORD                          OBSQ34.2
041300Y        MOVE ZERO TO RECORD-COUNT.                               OBSQ34.2
041400     PERFORM WRT-LN.                                              OBSQ34.2
041500 WRT-LN.                                                          OBSQ34.2
041600     WRITE    DUMMY-RECORD AFTER ADVANCING 1 LINES.               OBSQ34.2
041700     MOVE SPACE TO DUMMY-RECORD.                                  OBSQ34.2
041800 BLANK-LINE-PRINT.                                                OBSQ34.2
041900     PERFORM WRT-LN.                                              OBSQ34.2
042000 FAIL-ROUTINE.                                                    OBSQ34.2
042100     IF COMPUTED-X NOT EQUAL TO SPACE GO TO FAIL-ROUTINE-WRITE.   OBSQ34.2
042200     IF CORRECT-X NOT EQUAL TO SPACE GO TO FAIL-ROUTINE-WRITE.    OBSQ34.2
042300     MOVE "NO FURTHER INFORMATION, SEE PROGRAM." TO INFO-TEXT.    OBSQ34.2
042400     MOVE XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.     OBSQ34.2
042500     GO TO FAIL-ROUTINE-EX.                                       OBSQ34.2
042600 FAIL-ROUTINE-WRITE.                                              OBSQ34.2
042700     MOVE TEST-COMPUTED TO PRINT-REC PERFORM WRITE-LINE           OBSQ34.2
042800     MOVE TEST-CORRECT TO PRINT-REC PERFORM WRITE-LINE 2 TIMES.   OBSQ34.2
042900 FAIL-ROUTINE-EX. EXIT.                                           OBSQ34.2
043000 BAIL-OUT.                                                        OBSQ34.2
043100     IF COMPUTED-A NOT EQUAL TO SPACE GO TO BAIL-OUT-WRITE.       OBSQ34.2
043200     IF CORRECT-A EQUAL TO SPACE GO TO BAIL-OUT-EX.               OBSQ34.2
043300 BAIL-OUT-WRITE.                                                  OBSQ34.2
043400     MOVE CORRECT-A TO XXCORRECT. MOVE COMPUTED-A TO XXCOMPUTED.  OBSQ34.2
043500     MOVE XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.     OBSQ34.2
043600 BAIL-OUT-EX. EXIT.                                               OBSQ34.2
043700 CCVS1-EXIT.                                                      OBSQ34.2
043800     EXIT.                                                        OBSQ34.2
043900 SECT-OBSQ3A-0001 SECTION.                                        OBSQ34.2
044000 SEQ-INIT-001.                                                    OBSQ34.2
044100*             THIS TEST CREATES FILE SQ-FS1 AS THE FIRST FILE     OBSQ34.2
044200*             ON MULTIPLE FILE TAPE ONE.  THIS FILE IS CLOSED     OBSQ34.2
044300*             WITH NO REWIND.                                     OBSQ34.2
044400     PERFORM BUILD-RECORD.                                        OBSQ34.2
044500     MOVE "SQ-FS1" TO XFILE-NAME (1).                             OBSQ34.2
044600     MOVE "RC"    TO CHARS-OR-RECORDS (1).                        OBSQ34.2
044700     MOVE 1        TO XBLOCK-SIZE (1).                            OBSQ34.2
044800     OPEN OUTPUT SQ-FS1.                                          OBSQ34.2
044900 SEQ-TEST-001.                                                    OBSQ34.2
045000     MOVE FILE-RECORD-INFO-P1-120 (1) TO SQ-FS1R1-F-G-120.        OBSQ34.2
045100     WRITE SQ-FS1R1-F-G-120.                                      OBSQ34.2
045200     IF XRECORD-NUMBER (1) EQUAL TO 750                           OBSQ34.2
045300              GO TO SEQ-WRITE-001.                                OBSQ34.2
045400     ADD 1 TO XRECORD-NUMBER (1).                                 OBSQ34.2
045500     GO TO SEQ-TEST-001.                                          OBSQ34.2
045600 SEQ-WRITE-001.                                                   OBSQ34.2
045700     MOVE "CREATE FILE SQ-FS1" TO FEATURE.                        OBSQ34.2
045800     MOVE "SEQ-TEST-001" TO PAR-NAME.                             OBSQ34.2
045900     MOVE "FILE CREATED, RECS =" TO COMPUTED-A.                   OBSQ34.2
046000     MOVE XRECORD-NUMBER (1) TO CORRECT-18V0.                     OBSQ34.2
046100     PERFORM PRINT-DETAIL.                                        OBSQ34.2
046200 SEQ-CLOSE-001.                                                   OBSQ34.2
046300     CLOSE SQ-FS1 WITH NO REWIND.                                 OBSQ34.2
046400 SEQ-INIT-002.                                                    OBSQ34.2
046500*             THIS TEST CREATES FILE SQ-FS2 AS THE SECOND FILE    OBSQ34.2
046600*             ON MULTIPLE FILE TAPE ONE.  THIS FILE IS OPENED     OBSQ34.2
046700*             AND CLOSED WITH NO REWIND.                          OBSQ34.2
046800     PERFORM BUILD-RECORD.                                        OBSQ34.2
046900     MOVE "SQ-FS2" TO XFILE-NAME (1).                             OBSQ34.2
047000     MOVE "RC"     TO CHARS-OR-RECORDS (1).                       OBSQ34.2
047100     MOVE 5        TO XBLOCK-SIZE (1).                            OBSQ34.2
047200     OPEN OUTPUT SQ-FS2 WITH NO REWIND.                           OBSQ34.2
047300 SEQ-TEST-002.                                                    OBSQ34.2
047400     MOVE FILE-RECORD-INFO-P1-120 (1) TO SQ-FS2R1-F-G-120.        OBSQ34.2
047500     WRITE SQ-FS2R1-F-G-120.                                      OBSQ34.2
047600     IF XRECORD-NUMBER (1) EQUAL TO 750                           OBSQ34.2
047700              GO TO SEQ-WRITE-002.                                OBSQ34.2
047800     ADD 1 TO XRECORD-NUMBER (1).                                 OBSQ34.2
047900     GO TO SEQ-TEST-002.                                          OBSQ34.2
048000 SEQ-WRITE-002.                                                   OBSQ34.2
048100     MOVE "CREATE FILE SQ-FS2" TO FEATURE.                        OBSQ34.2
048200     MOVE "SEQ-TEST-002" TO PAR-NAME.                             OBSQ34.2
048300     MOVE "FILE CREATED, RECS =" TO COMPUTED-A.                   OBSQ34.2
048400     MOVE XRECORD-NUMBER (1) TO CORRECT-18V0.                     OBSQ34.2
048500     PERFORM PRINT-DETAIL.                                        OBSQ34.2
048600 SEQ-CLOSE-002.                                                   OBSQ34.2
048700     CLOSE SQ-FS2 WITH NO REWIND.                                 OBSQ34.2
048800 SEQ-INIT-003.                                                    OBSQ34.2
048900*             THIS TEST CREATES FILE SQ-FS3 AS THE THIRD FILE     OBSQ34.2
049000*             ON MULTIPLE FILE TAPE ONE.  THIS FILE IS OPENED     OBSQ34.2
049100*             AND CLOSED WITH NO REWIND.                          OBSQ34.2
049200     PERFORM BUILD-RECORD.                                        OBSQ34.2
049300     MOVE "SQ-FS3" TO XFILE-NAME (1).                             OBSQ34.2
049400     MOVE "CH"     TO CHARS-OR-RECORDS (1).                       OBSQ34.2
049500     MOVE 1200     TO XBLOCK-SIZE (1).                            OBSQ34.2
049600     OPEN OUTPUT SQ-FS3 NO REWIND.                                OBSQ34.2
049700 SEQ-TEST-003.                                                    OBSQ34.2
049800     MOVE FILE-RECORD-INFO-P1-120 (1) TO SQ-FS3R1-F-G-120.        OBSQ34.2
049900     WRITE SQ-FS3R1-F-G-120.                                      OBSQ34.2
050000     IF XRECORD-NUMBER (1) EQUAL TO 750                           OBSQ34.2
050100              GO TO SEQ-WRITE-003.                                OBSQ34.2
050200     ADD 1 TO XRECORD-NUMBER (1).                                 OBSQ34.2
050300     GO TO SEQ-TEST-003.                                          OBSQ34.2
050400 SEQ-WRITE-003.                                                   OBSQ34.2
050500     MOVE "CREATE FILE SQ-FS3" TO FEATURE.                        OBSQ34.2
050600     MOVE "SEQ-TEST-003" TO PAR-NAME.                             OBSQ34.2
050700     MOVE "FILE CREATED, RECS=" TO COMPUTED-A.                    OBSQ34.2
050800     MOVE XRECORD-NUMBER (1) TO CORRECT-18V0.                     OBSQ34.2
050900     PERFORM PRINT-DETAIL.                                        OBSQ34.2
051000 SEQ-CLOSE-003.                                                   OBSQ34.2
051100     CLOSE SQ-FS3 WITH NO REWIND.                                 OBSQ34.2
051200 SEQ-INIT-004.                                                    OBSQ34.2
051300*             THIS TEST CREATES FILE SQ-FS4 AS THE FOURTH AND LASTOBSQ34.2
051400*             FILE ON MULTIPLE FILE TAPE ONE.  THIS FILE IS OPENEDOBSQ34.2
051500*             WITH NO REWIND.                                     OBSQ34.2
051600     PERFORM BUILD-RECORD.                                        OBSQ34.2
051700     MOVE "SQ-FS4" TO XFILE-NAME (1).                             OBSQ34.2
051800     MOVE "RC"     TO CHARS-OR-RECORDS (1).                       OBSQ34.2
051900     MOVE 10       TO XBLOCK-SIZE (1).                            OBSQ34.2
052000     OPEN OUTPUT SQ-FS4 WITH NO REWIND.                           OBSQ34.2
052100 SEQ-TEST-004.                                                    OBSQ34.2
052200     MOVE FILE-RECORD-INFO-P1-120 (1) TO SQ-FS4R1-F-G-120.        OBSQ34.2
052300     WRITE SQ-FS4R1-F-G-120.                                      OBSQ34.2
052400     IF XRECORD-NUMBER (1) EQUAL TO 750                           OBSQ34.2
052500              GO TO SEQ-WRITE-004.                                OBSQ34.2
052600     ADD 1 TO XRECORD-NUMBER (1).                                 OBSQ34.2
052700     GO TO SEQ-TEST-004.                                          OBSQ34.2
052800 SEQ-WRITE-004.                                                   OBSQ34.2
052900     MOVE "CREATE FILE SQ-FS4" TO FEATURE.                        OBSQ34.2
053000     MOVE "SEQ-TEST-004" TO PAR-NAME.                             OBSQ34.2
053100     MOVE "FILE CREATED, RECS =" TO COMPUTED-A.                   OBSQ34.2
053200     MOVE XRECORD-NUMBER (1) TO CORRECT-18V0.                     OBSQ34.2
053300     PERFORM PRINT-DETAIL.                                        OBSQ34.2
053400 SEQ-CLOSE-004.                                                   OBSQ34.2
053500     CLOSE SQ-FS4.                                                OBSQ34.2
053600 SEQ-INIT-005.                                                    OBSQ34.2
053700*             THIS TEST CREATES FILE SQ-FS5 AS THE FIRST FILE ON  OBSQ34.2
053800*             MULTIPLE FILE TAPE TWO.  THE POSITION PHRASE IS     OBSQ34.2
053900*             USED IN THE MULTIPLE FILE CLAUSE.                   OBSQ34.2
054000     PERFORM BUILD-RECORD.                                        OBSQ34.2
054100     MOVE "SQ-FS5" TO XFILE-NAME (1).                             OBSQ34.2
054200     MOVE "RC"     TO CHARS-OR-RECORDS (1).                       OBSQ34.2
054300     MOVE 5        TO XBLOCK-SIZE (1).                            OBSQ34.2
054400     OPEN OUTPUT SQ-FS5.                                          OBSQ34.2
054500 SEQ-TEST-005.                                                    OBSQ34.2
054600     MOVE FILE-RECORD-INFO-P1-120 (1) TO SQ-FS5R1-F-G-120.        OBSQ34.2
054700     WRITE SQ-FS5R1-F-G-120.                                      OBSQ34.2
054800     IF XRECORD-NUMBER (1) EQUAL TO 750                           OBSQ34.2
054900              GO TO SEQ-WRITE-005.                                OBSQ34.2
055000     ADD 1 TO XRECORD-NUMBER (1).                                 OBSQ34.2
055100     GO TO SEQ-TEST-005.                                          OBSQ34.2
055200 SEQ-WRITE-005.                                                   OBSQ34.2
055300     MOVE "CREATE FILE SQ-FS5" TO FEATURE.                        OBSQ34.2
055400     MOVE "SEQ-TEST-005" TO PAR-NAME.                             OBSQ34.2
055500     MOVE "FILE CREATED, RECS =" TO COMPUTED-A.                   OBSQ34.2
055600     MOVE XRECORD-NUMBER (1) TO CORRECT-18V0.                     OBSQ34.2
055700     PERFORM PRINT-DETAIL.                                        OBSQ34.2
055800 SEQ-CLOSE-005.                                                   OBSQ34.2
055900     CLOSE SQ-FS5.                                                OBSQ34.2
056000 SEQ-INIT-006.                                                    OBSQ34.2
056100*             THIS  TEST CREATES FILE SQ-FS6 AS THE SECOND FILE   OBSQ34.2
056200*             ON MULTIPLE FILE TAPE TWO.  THE POSITION PHRASE IS  OBSQ34.2
056300*             USED IN THE MULTIPLE FILE CLAUSE.                   OBSQ34.2
056400     PERFORM BUILD-RECORD.                                        OBSQ34.2
056500     MOVE "SQ-FS6" TO XFILE-NAME (1).                             OBSQ34.2
056600     MOVE "RC"     TO CHARS-OR-RECORDS (1).                       OBSQ34.2
056700     MOVE 10       TO XBLOCK-SIZE (1).                            OBSQ34.2
056800     OPEN OUTPUT SQ-FS6.                                          OBSQ34.2
056900 SEQ-TEST-006.                                                    OBSQ34.2
057000     MOVE FILE-RECORD-INFO-P1-120 (1) TO SQ-FS6R1-F-G-120.        OBSQ34.2
057100     WRITE SQ-FS6R1-F-G-120.                                      OBSQ34.2
057200     IF XRECORD-NUMBER (1) EQUAL TO 750                           OBSQ34.2
057300              GO TO SEQ-WRITE-006.                                OBSQ34.2
057400     ADD 1 TO XRECORD-NUMBER (1).                                 OBSQ34.2
057500     GO TO SEQ-TEST-006.                                          OBSQ34.2
057600 SEQ-WRITE-006.                                                   OBSQ34.2
057700     MOVE "CREATE FILE SQ-FS6" TO FEATURE.                        OBSQ34.2
057800     MOVE "SEQ-TEST-006" TO PAR-NAME.                             OBSQ34.2
057900     MOVE "FILE CREATED, RECS =" TO COMPUTED-A.                   OBSQ34.2
058000     MOVE XRECORD-NUMBER (1) TO CORRECT-18V0.                     OBSQ34.2
058100     PERFORM PRINT-DETAIL.                                        OBSQ34.2
058200 SEQ-CLOSE-006.                                                   OBSQ34.2
058300     CLOSE SQ-FS6.                                                OBSQ34.2
058400 SEQ-INIT-007.                                                    OBSQ34.2
058500*             THIS TEST CREATES FILE SQ-FS7 AS THE THIRD FILE     OBSQ34.2
058600*             ON MULTIPLE FILE TAPE TWO.  THE POSITION PHRASE IS  OBSQ34.2
058700*             USED IN THE MULTIPLE FILE CLAUSE.                   OBSQ34.2
058800     PERFORM BUILD-RECORD.                                        OBSQ34.2
058900     MOVE "SQ-FS7" TO XFILE-NAME (1).                             OBSQ34.2
059000     MOVE "CH"     TO CHARS-OR-RECORDS (1).                       OBSQ34.2
059100     MOVE 2400     TO XBLOCK-SIZE (1).                            OBSQ34.2
059200     OPEN OUTPUT SQ-FS7.                                          OBSQ34.2
059300 SEQ-TEST-007.                                                    OBSQ34.2
059400     MOVE FILE-RECORD-INFO-P1-120 (1) TO SQ-FS7R1-F-G-120.        OBSQ34.2
059500     WRITE SQ-FS7R1-F-G-120.                                      OBSQ34.2
059600     IF XRECORD-NUMBER (1) EQUAL TO 750                           OBSQ34.2
059700              GO TO SEQ-WRITE-007.                                OBSQ34.2
059800     ADD 1 TO XRECORD-NUMBER (1).                                 OBSQ34.2
059900     GO TO SEQ-TEST-007.                                          OBSQ34.2
060000 SEQ-WRITE-007.                                                   OBSQ34.2
060100     MOVE "CREATE FILE SQ-FS7" TO FEATURE.                        OBSQ34.2
060200     MOVE "SEQ-TEST-007" TO PAR-NAME.                             OBSQ34.2
060300     MOVE "FILE CREATED, RECS-=" TO COMPUTED-A.                   OBSQ34.2
060400     MOVE XRECORD-NUMBER (1) TO CORRECT-18V0.                     OBSQ34.2
060500     PERFORM PRINT-DETAIL.                                        OBSQ34.2
060600 SEQ-CLOSE-007.                                                   OBSQ34.2
060700     CLOSE SQ-FS7.                                                OBSQ34.2
060800 SEQ-INIT-008.                                                    OBSQ34.2
060900*             THIS TEST CREATES FILE SQ-FS8 AS THE FOURTH AND LASTOBSQ34.2
061000*             FILE ON MULTIPLE FILE TAPE TWO.  THE POSITION PHRASEOBSQ34.2
061100*             IS USED IN THE MULTIPLE FILE CLAUSE.                OBSQ34.2
061200     PERFORM BUILD-RECORD.                                        OBSQ34.2
061300     MOVE "SQ-FS8" TO XFILE-NAME (1).                             OBSQ34.2
061400     MOVE "CH"     TO CHARS-OR-RECORDS (1).                       OBSQ34.2
061500     MOVE 120      TO XBLOCK-SIZE (1).                            OBSQ34.2
061600     OPEN OUTPUT SQ-FS8.                                          OBSQ34.2
061700 SEQ-TEST-008.                                                    OBSQ34.2
061800     MOVE FILE-RECORD-INFO-P1-120 (1) TO SQ-FS8R1-F-G-120.        OBSQ34.2
061900     WRITE SQ-FS8R1-F-G-120.                                      OBSQ34.2
062000     IF XRECORD-NUMBER (1) EQUAL 750                              OBSQ34.2
062100              GO TO SEQ-WRITE-008.                                OBSQ34.2
062200     ADD 1 TO XRECORD-NUMBER (1).                                 OBSQ34.2
062300     GO TO SEQ-TEST-008.                                          OBSQ34.2
062400 SEQ-WRITE-008.                                                   OBSQ34.2
062500     MOVE "CREATE FILE SQ-FS8" TO FEATURE.                        OBSQ34.2
062600     MOVE "SEQ-TEST-008" TO PAR-NAME.                             OBSQ34.2
062700     MOVE "FILE CREATED, RECS =" TO COMPUTED-A.                   OBSQ34.2
062800     MOVE XRECORD-NUMBER (1) TO CORRECT-18V0.                     OBSQ34.2
062900     PERFORM PRINT-DETAIL.                                        OBSQ34.2
063000 SEQ-CLOSE-008.                                                   OBSQ34.2
063100     CLOSE SQ-FS8.                                                OBSQ34.2
063200 OBSQ3A-END-ROUTINE.                                              OBSQ34.2
063300     MOVE "END OF OBSQ3A VALIDATION TESTS" TO PRINT-REC.          OBSQ34.2
063400     WRITE PRINT-REC AFTER ADVANCING 1 LINE.                      OBSQ34.2
063500 TERMINATE-OBSQ3A.                                                OBSQ34.2
063600     GO TO CCVS-EXIT.                                             OBSQ34.2
063700 BUILD-RECORD.                                                    OBSQ34.2
063800     MOVE "R1-F-G" TO XRECORD-NAME (1).                           OBSQ34.2
063900     MOVE "OBSQ3A" TO XPROGRAM-NAME (1).                          OBSQ34.2
064000     MOVE 120      TO XRECORD-LENGTH (1).                         OBSQ34.2
064100     MOVE 750      TO RECORDS-IN-FILE (1).                        OBSQ34.2
064200     MOVE "SQ"     TO XFILE-ORGANIZATION (1).                     OBSQ34.2
064300     MOVE "S"      TO XLABEL-TYPE (1).                            OBSQ34.2
064400     MOVE 1        TO XRECORD-NUMBER (1).                         OBSQ34.2
064500 CCVS-EXIT SECTION.                                               OBSQ34.2
064600 CCVS-999999.                                                     OBSQ34.2
064700     GO TO CLOSE-FILES.                                           OBSQ34.2
       END PROGRAM OBSQ3A.
000100 IDENTIFICATION DIVISION.                                         OBSQ44.2
000200 PROGRAM-ID.                                                      OBSQ44.2
000300     OBSQ4A.                                                      OBSQ44.2
000400****************************************************************  OBSQ44.2
000500*                                                              *  OBSQ44.2
000600*    VALIDATION FOR:-                                          *  OBSQ44.2
000700*    " HIGH       ".                                              OBSQ44.2
000800*    USING CCVS85 VERSION 1.0 ISSUED IN JANUARY 1986.          *  OBSQ44.2
000900*                                                              *  OBSQ44.2
001000*    CREATION DATE     /     VALIDATION DATE                   *  OBSQ44.2
001100*    "4.2 ".                                                      OBSQ44.2
001200*                                                              *  OBSQ44.2
001300*         THE ROUTINE OBSQ4A READS AND VALIDATES THE MULTIPLE     OBSQ44.2
001400*    FILE TAPE CREATED IN OBSQ3A. THE FOUR FILES CONTAINED ON     OBSQ44.2
001500*    THIS TAPE ARE SQ-FS1, SQ-FS2, SQ-FS3, AND SQ-FS4.  BOTH      OBSQ44.2
001600*    MULTIPLE FILE TAPES ONE AND TWO ARE THEN PASSED ON TO OBSQ5A.OBSQ44.2
001700*    OBSQ4A USES A MULTIPLE FILE CLAUSE WITH THE POSITION PHRASE  OBSQ44.2
001800*    TO PROCESS TAPE ONE.  THIS TAPE WAS CREATED USING OPEN AND   OBSQ44.2
001900*    CLOSE STATEMENTS WITH NO REWIND.                             OBSQ44.2
002000 ENVIRONMENT DIVISION.                                            OBSQ44.2
002100 CONFIGURATION SECTION.                                           OBSQ44.2
002200 SOURCE-COMPUTER.                                                 OBSQ44.2
002300     XXXXX082.                                                    OBSQ44.2
002400 OBJECT-COMPUTER.                                                 OBSQ44.2
002500     XXXXX083.                                                    OBSQ44.2
002600 INPUT-OUTPUT SECTION.                                            OBSQ44.2
002700 FILE-CONTROL.                                                    OBSQ44.2
002800P    SELECT RAW-DATA   ASSIGN TO                                  OBSQ44.2
002900P    XXXXX062                                                     OBSQ44.2
003000P           ORGANIZATION IS INDEXED                               OBSQ44.2
003100P           ACCESS MODE IS RANDOM                                 OBSQ44.2
003200P           RECORD KEY IS RAW-DATA-KEY.                           OBSQ44.2
003300     SELECT PRINT-FILE ASSIGN TO                                  OBSQ44.2
003400     XXXXX055.                                                    OBSQ44.2
003500     SELECT SQ-FS1 ASSIGN TO                                      OBSQ44.2
003600     XXXXP004.                                                    OBSQ44.2
003700     SELECT SQ-FS2 ASSIGN TO                                      OBSQ44.2
003800     XXXXP008.                                                    OBSQ44.2
003900     SELECT SQ-FS3 ASSIGN TO                                      OBSQ44.2
004000     XXXXP009.                                                    OBSQ44.2
004100     SELECT SQ-FS4 ASSIGN TO                                      OBSQ44.2
004200     XXXXP010.                                                    OBSQ44.2
004300 I-O-CONTROL.                                                     OBSQ44.2
004400     MULTIPLE FILE CONTAINS SQ-FS1 POSITION 1,                    OBSQ44.2
004500                            SQ-FS4 POSITION 4,                    OBSQ44.2
004600                            SQ-FS3 POSITION 3,                    OBSQ44.2
004700                            SQ-FS2 POSITION 2.                    OBSQ44.2
004800 DATA DIVISION.                                                   OBSQ44.2
004900 FILE SECTION.                                                    OBSQ44.2
005000P                                                                 OBSQ44.2
005100PFD  RAW-DATA.                                                    OBSQ44.2
005200P                                                                 OBSQ44.2
005300P01  RAW-DATA-SATZ.                                               OBSQ44.2
005400P    05  RAW-DATA-KEY        PIC X(6).                            OBSQ44.2
005500P    05  C-DATE              PIC 9(6).                            OBSQ44.2
005600P    05  C-TIME              PIC 9(8).                            OBSQ44.2
005700P    05  C-NO-OF-TESTS       PIC 99.                              OBSQ44.2
005800P    05  C-OK                PIC 999.                             OBSQ44.2
005900P    05  C-ALL               PIC 999.                             OBSQ44.2
006000P    05  C-FAIL              PIC 999.                             OBSQ44.2
006100P    05  C-DELETED           PIC 999.                             OBSQ44.2
006200P    05  C-INSPECT           PIC 999.                             OBSQ44.2
006300P    05  C-NOTE              PIC X(13).                           OBSQ44.2
006400P    05  C-INDENT            PIC X.                               OBSQ44.2
006500P    05  C-ABORT             PIC X(8).                            OBSQ44.2
006600 FD  PRINT-FILE.                                                  OBSQ44.2
006700 01  PRINT-REC PICTURE X(120).                                    OBSQ44.2
006800 01  DUMMY-RECORD PICTURE X(120).                                 OBSQ44.2
006900 FD  SQ-FS1                                                       OBSQ44.2
007000     LABEL RECORD STANDARD                                        OBSQ44.2
007100                 .                                                OBSQ44.2
007200 01  SQ-FS1R1-F-G-120   PIC X(120).                               OBSQ44.2
007300 FD  SQ-FS2                                                       OBSQ44.2
007400     LABEL RECORD STANDARD                                        OBSQ44.2
007500     BLOCK 5 RECORDS.                                             OBSQ44.2
007600 01  SQ-FS2R1-F-G-120   PIC X(120).                               OBSQ44.2
007700 FD  SQ-FS3                                                       OBSQ44.2
007800     LABEL RECORD STANDARD                                        OBSQ44.2
007900     RECORD CONTAINS 120 CHARACTERS                               OBSQ44.2
008000     BLOCK CONTAINS 1200 CHARACTERS.                              OBSQ44.2
008100 01  SQ-FS3R1-F-G-120   PIC X(120).                               OBSQ44.2
008200 FD  SQ-FS4                                                       OBSQ44.2
008300     LABEL RECORD IS STANDARD                                     OBSQ44.2
008400     RECORD          120 CHARACTERS                               OBSQ44.2
008500     BLOCK CONTAINS 10 RECORDS.                                   OBSQ44.2
008600 01  SQ-FS4R1-F-G-120   PIC X(120).                               OBSQ44.2
008700 WORKING-STORAGE SECTION.                                         OBSQ44.2
008800 77  RECORDS-COUNT       PIC 999   VALUE 0.                       OBSQ44.2
008900 77  RECORDS-IN-ERROR   PIC 999   VALUE 0.                        OBSQ44.2
009000 01  FILE-RECORD-INFORMATION-REC.                                 OBSQ44.2
009100     03 FILE-RECORD-INFO-SKELETON.                                OBSQ44.2
009200        05 FILLER                 PICTURE X(48)       VALUE       OBSQ44.2
009300             "FILE=      ,RECORD=      /0,RECNO=000000,UPDT=00".  OBSQ44.2
009400        05 FILLER                 PICTURE X(46)       VALUE       OBSQ44.2
009500             ",ODO=0000,PGM=     ,LRECL=000000,BLKSIZ  =0000".    OBSQ44.2
009600        05 FILLER                 PICTURE X(26)       VALUE       OBSQ44.2
009700             ",LFIL=000000,ORG=  ,LBLR= ".                        OBSQ44.2
009800        05 FILLER                 PICTURE X(37)       VALUE       OBSQ44.2
009900             ",RECKEY=                             ".             OBSQ44.2
010000        05 FILLER                 PICTURE X(38)       VALUE       OBSQ44.2
010100             ",ALTKEY1=                             ".            OBSQ44.2
010200        05 FILLER                 PICTURE X(38)       VALUE       OBSQ44.2
010300             ",ALTKEY2=                             ".            OBSQ44.2
010400        05 FILLER                 PICTURE X(7)        VALUE SPACE.OBSQ44.2
010500     03 FILE-RECORD-INFO          OCCURS  10  TIMES.              OBSQ44.2
010600        05 FILE-RECORD-INFO-P1-120.                               OBSQ44.2
010700           07 FILLER              PIC X(5).                       OBSQ44.2
010800           07 XFILE-NAME           PIC X(6).                      OBSQ44.2
010900           07 FILLER              PIC X(8).                       OBSQ44.2
011000           07 XRECORD-NAME         PIC X(6).                      OBSQ44.2
011100           07 FILLER              PIC X(1).                       OBSQ44.2
011200           07 REELUNIT-NUMBER     PIC 9(1).                       OBSQ44.2
011300           07 FILLER              PIC X(7).                       OBSQ44.2
011400           07 XRECORD-NUMBER       PIC 9(6).                      OBSQ44.2
011500           07 FILLER              PIC X(6).                       OBSQ44.2
011600           07 UPDATE-NUMBER       PIC 9(2).                       OBSQ44.2
011700           07 FILLER              PIC X(5).                       OBSQ44.2
011800           07 ODO-NUMBER          PIC 9(4).                       OBSQ44.2
011900           07 FILLER              PIC X(5).                       OBSQ44.2
012000           07 XPROGRAM-NAME        PIC X(5).                      OBSQ44.2
012100           07 FILLER              PIC X(7).                       OBSQ44.2
012200           07 XRECORD-LENGTH       PIC 9(6).                      OBSQ44.2
012300           07 FILLER              PIC X(7).                       OBSQ44.2
012400           07 CHARS-OR-RECORDS    PIC X(2).                       OBSQ44.2
012500           07 FILLER              PIC X(1).                       OBSQ44.2
012600           07 XBLOCK-SIZE          PIC 9(4).                      OBSQ44.2
012700           07 FILLER              PIC X(6).                       OBSQ44.2
012800           07 RECORDS-IN-FILE     PIC 9(6).                       OBSQ44.2
012900           07 FILLER              PIC X(5).                       OBSQ44.2
013000           07 XFILE-ORGANIZATION   PIC X(2).                      OBSQ44.2
013100           07 FILLER              PIC X(6).                       OBSQ44.2
013200           07 XLABEL-TYPE          PIC X(1).                      OBSQ44.2
013300        05 FILE-RECORD-INFO-P121-240.                             OBSQ44.2
013400           07 FILLER              PIC X(8).                       OBSQ44.2
013500           07 XRECORD-KEY          PIC X(29).                     OBSQ44.2
013600           07 FILLER              PIC X(9).                       OBSQ44.2
013700           07 ALTERNATE-KEY1      PIC X(29).                      OBSQ44.2
013800           07 FILLER              PIC X(9).                       OBSQ44.2
013900           07 ALTERNATE-KEY2      PIC X(29).                      OBSQ44.2
014000           07 FILLER              PIC X(7).                       OBSQ44.2
014100 01  TEST-RESULTS.                                                OBSQ44.2
014200     02 FILLER                    PICTURE X VALUE SPACE.          OBSQ44.2
014300     02 FEATURE                   PICTURE X(20) VALUE SPACE.      OBSQ44.2
014400     02 FILLER                    PICTURE X VALUE SPACE.          OBSQ44.2
014500     02 P-OR-F                    PICTURE X(5) VALUE SPACE.       OBSQ44.2
014600     02 FILLER                    PICTURE X  VALUE SPACE.         OBSQ44.2
014700     02  PAR-NAME.                                                OBSQ44.2
014800       03 FILLER PICTURE X(12) VALUE SPACE.                       OBSQ44.2
014900       03  PARDOT-X PICTURE X  VALUE SPACE.                       OBSQ44.2
015000       03 DOTVALUE PICTURE 99  VALUE ZERO.                        OBSQ44.2
015100       03 FILLER PIC X(5) VALUE SPACE.                            OBSQ44.2
015200     02 FILLER PIC X(10) VALUE SPACE.                             OBSQ44.2
015300     02 RE-MARK PIC X(61).                                        OBSQ44.2
015400 01  TEST-COMPUTED.                                               OBSQ44.2
015500     02 FILLER PIC X(30) VALUE SPACE.                             OBSQ44.2
015600     02 FILLER PIC X(17) VALUE "       COMPUTED=".                OBSQ44.2
015700     02 COMPUTED-X.                                               OBSQ44.2
015800     03 COMPUTED-A                PICTURE X(20) VALUE SPACE.      OBSQ44.2
015900     03 COMPUTED-N REDEFINES COMPUTED-A PICTURE -9(9).9(9).       OBSQ44.2
016000     03 COMPUTED-0V18 REDEFINES COMPUTED-A  PICTURE -.9(18).      OBSQ44.2
016100     03 COMPUTED-4V14 REDEFINES COMPUTED-A  PICTURE -9(4).9(14).  OBSQ44.2
016200     03 COMPUTED-14V4 REDEFINES COMPUTED-A  PICTURE -9(14).9(4).  OBSQ44.2
016300     03       CM-18V0 REDEFINES COMPUTED-A.                       OBSQ44.2
016400         04 COMPUTED-18V0                   PICTURE -9(18).       OBSQ44.2
016500         04 FILLER                          PICTURE X.            OBSQ44.2
016600     03 FILLER PIC X(50) VALUE SPACE.                             OBSQ44.2
016700 01  TEST-CORRECT.                                                OBSQ44.2
016800     02 FILLER PIC X(30) VALUE SPACE.                             OBSQ44.2
016900     02 FILLER PIC X(17) VALUE "       CORRECT =".                OBSQ44.2
017000     02 CORRECT-X.                                                OBSQ44.2
017100     03 CORRECT-A                 PICTURE X(20) VALUE SPACE.      OBSQ44.2
017200     03 CORRECT-N REDEFINES CORRECT-A PICTURE -9(9).9(9).         OBSQ44.2
017300     03 CORRECT-0V18 REDEFINES CORRECT-A    PICTURE -.9(18).      OBSQ44.2
017400     03 CORRECT-4V14 REDEFINES CORRECT-A    PICTURE -9(4).9(14).  OBSQ44.2
017500     03 CORRECT-14V4 REDEFINES CORRECT-A    PICTURE -9(14).9(4).  OBSQ44.2
017600     03      CR-18V0 REDEFINES CORRECT-A.                         OBSQ44.2
017700         04 CORRECT-18V0                    PICTURE -9(18).       OBSQ44.2
017800         04 FILLER                          PICTURE X.            OBSQ44.2
017900     03 FILLER PIC X(50) VALUE SPACE.                             OBSQ44.2
018000 01  CCVS-C-1.                                                    OBSQ44.2
018100     02 FILLER PICTURE IS X(99) VALUE IS " FEATURE              PAOBSQ44.2
018200-    "SS  PARAGRAPH-NAME                                          OBSQ44.2
018300-    "        REMARKS".                                           OBSQ44.2
018400     02 FILLER PICTURE IS X(20) VALUE IS SPACE.                   OBSQ44.2
018500 01  CCVS-C-2.                                                    OBSQ44.2
018600     02 FILLER PICTURE IS X VALUE IS SPACE.                       OBSQ44.2
018700     02 FILLER PICTURE IS X(6) VALUE IS "TESTED".                 OBSQ44.2
018800     02 FILLER PICTURE IS X(15) VALUE IS SPACE.                   OBSQ44.2
018900     02 FILLER PICTURE IS X(4) VALUE IS "FAIL".                   OBSQ44.2
019000     02 FILLER PICTURE IS X(94) VALUE IS SPACE.                   OBSQ44.2
019100 01  REC-SKL-SUB PICTURE 9(2) VALUE ZERO.                         OBSQ44.2
019200 01  REC-CT PICTURE 99 VALUE ZERO.                                OBSQ44.2
019300 01  DELETE-CNT                   PICTURE 999  VALUE ZERO.        OBSQ44.2
019400 01  ERROR-COUNTER PICTURE IS 999 VALUE IS ZERO.                  OBSQ44.2
019500 01  INSPECT-COUNTER PIC 999 VALUE ZERO.                          OBSQ44.2
019600 01  PASS-COUNTER PIC 999 VALUE ZERO.                             OBSQ44.2
019700 01  TOTAL-ERROR PIC 999 VALUE ZERO.                              OBSQ44.2
019800 01  ERROR-HOLD PIC 999 VALUE ZERO.                               OBSQ44.2
019900 01  DUMMY-HOLD PIC X(120) VALUE SPACE.                           OBSQ44.2
020000 01  RECORD-COUNT PIC 9(5) VALUE ZERO.                            OBSQ44.2
020100 01  CCVS-H-1.                                                    OBSQ44.2
020200     02  FILLER   PICTURE X(27)  VALUE SPACE.                     OBSQ44.2
020300     02 FILLER PICTURE X(67) VALUE                                OBSQ44.2
020400     " FEDERAL SOFTWARE TESTING CENTER COBOL COMPILER VALIDATION  OBSQ44.2
020500-    " SYSTEM".                                                   OBSQ44.2
020600     02  FILLER     PICTURE X(26)  VALUE SPACE.                   OBSQ44.2
020700 01  CCVS-H-2.                                                    OBSQ44.2
020800     02 FILLER PICTURE X(52) VALUE IS                             OBSQ44.2
020900     "CCVS85 FSTC COPY, NOT FOR DISTRIBUTION.".                   OBSQ44.2
021000     02 FILLER PICTURE IS X(19) VALUE IS "TEST RESULTS SET-  ".   OBSQ44.2
021100     02 TEST-ID PICTURE IS X(9).                                  OBSQ44.2
021200     02 FILLER PICTURE IS X(40) VALUE IS SPACE.                   OBSQ44.2
021300 01  CCVS-H-3.                                                    OBSQ44.2
021400     02  FILLER PICTURE X(34) VALUE                               OBSQ44.2
021500     " FOR OFFICIAL USE ONLY    ".                                OBSQ44.2
021600     02  FILLER PICTURE X(58) VALUE                               OBSQ44.2
021700     "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".OBSQ44.2
021800     02  FILLER PICTURE X(28) VALUE                               OBSQ44.2
021900     "  COPYRIGHT   1985 ".                                       OBSQ44.2
022000 01  CCVS-E-1.                                                    OBSQ44.2
022100     02 FILLER PICTURE IS X(52) VALUE IS SPACE.                   OBSQ44.2
022200     02 FILLER PICTURE IS X(14) VALUE IS "END OF TEST-  ".        OBSQ44.2
022300     02 ID-AGAIN PICTURE IS X(9).                                 OBSQ44.2
022400     02 FILLER PICTURE X(45) VALUE IS                             OBSQ44.2
022500     " NTIS DISTRIBUTION COBOL 85".                               OBSQ44.2
022600 01  CCVS-E-2.                                                    OBSQ44.2
022700     02  FILLER                   PICTURE X(31)  VALUE            OBSQ44.2
022800     SPACE.                                                       OBSQ44.2
022900     02  FILLER                   PICTURE X(21)  VALUE SPACE.     OBSQ44.2
023000     02 CCVS-E-2-2.                                               OBSQ44.2
023100         03 ERROR-TOTAL PICTURE IS XXX VALUE IS SPACE.            OBSQ44.2
023200         03 FILLER PICTURE IS X VALUE IS SPACE.                   OBSQ44.2
023300         03 ENDER-DESC PIC X(46) VALUE "ERRORS ENCOUNTERED".      OBSQ44.2
023400 01  CCVS-E-3.                                                    OBSQ44.2
023500     02  FILLER PICTURE X(22) VALUE                               OBSQ44.2
023600     " FOR OFFICIAL USE ONLY".                                    OBSQ44.2
023700     02  FILLER PICTURE X(12) VALUE SPACE.                        OBSQ44.2
023800     02  FILLER PICTURE X(58) VALUE                               OBSQ44.2
023900     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".OBSQ44.2
024000     02  FILLER PICTURE X(13) VALUE SPACE.                        OBSQ44.2
024100     02 FILLER PIC X(15) VALUE " COPYRIGHT 1985".                 OBSQ44.2
024200 01  CCVS-E-4.                                                    OBSQ44.2
024300     02 CCVS-E-4-1 PIC XXX VALUE SPACE.                           OBSQ44.2
024400     02 FILLER PIC XXXX VALUE " OF ".                             OBSQ44.2
024500     02 CCVS-E-4-2 PIC XXX VALUE SPACE.                           OBSQ44.2
024600     02 FILLER PIC X(40) VALUE                                    OBSQ44.2
024700      "  TESTS WERE EXECUTED SUCCESSFULLY".                       OBSQ44.2
024800 01  XXINFO.                                                      OBSQ44.2
024900     02 FILLER PIC X(30) VALUE "        *** INFORMATION  ***".    OBSQ44.2
025000     02 INFO-TEXT.                                                OBSQ44.2
025100     04 FILLER PIC X(20) VALUE SPACE.                             OBSQ44.2
025200     04 XXCOMPUTED PIC X(20).                                     OBSQ44.2
025300     04 FILLER PIC X(5) VALUE SPACE.                              OBSQ44.2
025400     04 XXCORRECT PIC X(20).                                      OBSQ44.2
025500 01  HYPHEN-LINE.                                                 OBSQ44.2
025600     02 FILLER PICTURE IS X VALUE IS SPACE.                       OBSQ44.2
025700     02 FILLER PICTURE IS X(65) VALUE IS "************************OBSQ44.2
025800-    "*****************************************".                 OBSQ44.2
025900     02 FILLER PICTURE IS X(54) VALUE IS "************************OBSQ44.2
026000-    "******************************".                            OBSQ44.2
026100 01  CCVS-PGM-ID PIC X(6) VALUE                                   OBSQ44.2
026200     "OBSQ4A".                                                    OBSQ44.2
026300 PROCEDURE DIVISION.                                              OBSQ44.2
026400 CCVS1 SECTION.                                                   OBSQ44.2
026500 OPEN-FILES.                                                      OBSQ44.2
026600P    OPEN I-O RAW-DATA.                                           OBSQ44.2
026700P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            OBSQ44.2
026800P    READ RAW-DATA INVALID KEY GO TO END-E-1.                     OBSQ44.2
026900P    MOVE "ABORTED " TO C-ABORT.                                  OBSQ44.2
027000P    ADD 1 TO C-NO-OF-TESTS.                                      OBSQ44.2
027100P    ACCEPT C-DATE  FROM DATE.                                    OBSQ44.2
027200P    ACCEPT C-TIME  FROM TIME.                                    OBSQ44.2
027300P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO END-E-1.             OBSQ44.2
027400PEND-E-1.                                                         OBSQ44.2
027500P    CLOSE RAW-DATA.                                              OBSQ44.2
027600     OPEN     OUTPUT PRINT-FILE.                                  OBSQ44.2
027700     MOVE CCVS-PGM-ID TO TEST-ID. MOVE CCVS-PGM-ID TO ID-AGAIN.   OBSQ44.2
027800     MOVE    SPACE TO TEST-RESULTS.                               OBSQ44.2
027900     PERFORM  HEAD-ROUTINE THRU COLUMN-NAMES-ROUTINE.             OBSQ44.2
028000     MOVE ZERO TO REC-SKL-SUB.                                    OBSQ44.2
028100     PERFORM CCVS-INIT-FILE 9 TIMES.                              OBSQ44.2
028200 CCVS-INIT-FILE.                                                  OBSQ44.2
028300     ADD 1 TO REC-SKL-SUB.                                        OBSQ44.2
028400     MOVE FILE-RECORD-INFO-SKELETON TO                            OBSQ44.2
028500                  FILE-RECORD-INFO (REC-SKL-SUB).                 OBSQ44.2
028600 CCVS-INIT-EXIT.                                                  OBSQ44.2
028700     GO TO CCVS1-EXIT.                                            OBSQ44.2
028800 CLOSE-FILES.                                                     OBSQ44.2
028900     PERFORM END-ROUTINE THRU END-ROUTINE-13. CLOSE PRINT-FILE.   OBSQ44.2
029000P    OPEN I-O RAW-DATA.                                           OBSQ44.2
029100P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            OBSQ44.2
029200P    READ RAW-DATA INVALID KEY GO TO END-E-2.                     OBSQ44.2
029300P    MOVE "OK.     " TO C-ABORT.                                  OBSQ44.2
029400P    MOVE PASS-COUNTER TO C-OK.                                   OBSQ44.2
029500P    MOVE ERROR-HOLD   TO C-ALL.                                  OBSQ44.2
029600P    MOVE ERROR-COUNTER TO C-FAIL.                                OBSQ44.2
029700P    MOVE DELETE-CNT TO C-DELETED.                                OBSQ44.2
029800P    MOVE INSPECT-COUNTER TO C-INSPECT.                           OBSQ44.2
029900P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO END-E-2.             OBSQ44.2
030000PEND-E-2.                                                         OBSQ44.2
030100P    CLOSE RAW-DATA.                                              OBSQ44.2
030200 TERMINATE-CCVS.                                                  OBSQ44.2
030300S    EXIT PROGRAM.                                                OBSQ44.2
030400STERMINATE-CALL.                                                  OBSQ44.2
030500     STOP     RUN.                                                OBSQ44.2
030600 INSPT. MOVE "INSPT" TO P-OR-F. ADD 1 TO INSPECT-COUNTER.         OBSQ44.2
030700 PASS.  MOVE "PASS " TO P-OR-F.  ADD 1 TO PASS-COUNTER.           OBSQ44.2
030800 FAIL.  MOVE "FAIL*" TO P-OR-F.  ADD 1 TO ERROR-COUNTER.          OBSQ44.2
030900 DE-LETE.  MOVE "*****" TO P-OR-F.  ADD 1 TO DELETE-CNT.          OBSQ44.2
031000     MOVE "****TEST DELETED****" TO RE-MARK.                      OBSQ44.2
031100 PRINT-DETAIL.                                                    OBSQ44.2
031200     IF REC-CT NOT EQUAL TO ZERO                                  OBSQ44.2
031300             MOVE "." TO PARDOT-X                                 OBSQ44.2
031400             MOVE REC-CT TO DOTVALUE.                             OBSQ44.2
031500     MOVE     TEST-RESULTS TO PRINT-REC. PERFORM WRITE-LINE.      OBSQ44.2
031600     IF P-OR-F EQUAL TO "FAIL*"  PERFORM WRITE-LINE               OBSQ44.2
031700        PERFORM FAIL-ROUTINE THRU FAIL-ROUTINE-EX                 OBSQ44.2
031800          ELSE PERFORM BAIL-OUT THRU BAIL-OUT-EX.                 OBSQ44.2
031900     MOVE SPACE TO P-OR-F. MOVE SPACE TO COMPUTED-X.              OBSQ44.2
032000     MOVE SPACE TO CORRECT-X.                                     OBSQ44.2
032100     IF     REC-CT EQUAL TO ZERO  MOVE SPACE TO PAR-NAME.         OBSQ44.2
032200     MOVE     SPACE TO RE-MARK.                                   OBSQ44.2
032300 HEAD-ROUTINE.                                                    OBSQ44.2
032400     MOVE CCVS-H-1 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   OBSQ44.2
032500     MOVE CCVS-H-2 TO DUMMY-RECORD. PERFORM WRITE-LINE 5 TIMES.   OBSQ44.2
032600     MOVE CCVS-H-3 TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.   OBSQ44.2
032700 COLUMN-NAMES-ROUTINE.                                            OBSQ44.2
032800     MOVE CCVS-C-1 TO DUMMY-RECORD. PERFORM WRITE-LINE.           OBSQ44.2
032900     MOVE CCVS-C-2 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   OBSQ44.2
033000     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE.        OBSQ44.2
033100 END-ROUTINE.                                                     OBSQ44.2
033200     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE 5 TIMES.OBSQ44.2
033300 END-RTN-EXIT.                                                    OBSQ44.2
033400     MOVE CCVS-E-1 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   OBSQ44.2
033500 END-ROUTINE-1.                                                   OBSQ44.2
033600      ADD ERROR-COUNTER TO ERROR-HOLD ADD INSPECT-COUNTER TO      OBSQ44.2
033700      ERROR-HOLD. ADD DELETE-CNT TO ERROR-HOLD.                   OBSQ44.2
033800      ADD PASS-COUNTER TO ERROR-HOLD.                             OBSQ44.2
033900*     IF PASS-COUNTER EQUAL TO ERROR-HOLD GO TO END-ROUTINE-12.   OBSQ44.2
034000      MOVE PASS-COUNTER TO CCVS-E-4-1.                            OBSQ44.2
034100      MOVE ERROR-HOLD TO CCVS-E-4-2.                              OBSQ44.2
034200      MOVE CCVS-E-4 TO CCVS-E-2-2.                                OBSQ44.2
034300      MOVE CCVS-E-2 TO DUMMY-RECORD PERFORM WRITE-LINE.           OBSQ44.2
034400  END-ROUTINE-12.                                                 OBSQ44.2
034500      MOVE "TEST(S) FAILED" TO ENDER-DESC.                        OBSQ44.2
034600     IF       ERROR-COUNTER IS EQUAL TO ZERO                      OBSQ44.2
034700         MOVE "NO " TO ERROR-TOTAL                                OBSQ44.2
034800         ELSE                                                     OBSQ44.2
034900         MOVE ERROR-COUNTER TO ERROR-TOTAL.                       OBSQ44.2
035000     MOVE     CCVS-E-2 TO DUMMY-RECORD.                           OBSQ44.2
035100     PERFORM WRITE-LINE.                                          OBSQ44.2
035200 END-ROUTINE-13.                                                  OBSQ44.2
035300     IF DELETE-CNT IS EQUAL TO ZERO                               OBSQ44.2
035400         MOVE "NO " TO ERROR-TOTAL  ELSE                          OBSQ44.2
035500         MOVE DELETE-CNT TO ERROR-TOTAL.                          OBSQ44.2
035600     MOVE "TEST(S) DELETED     " TO ENDER-DESC.                   OBSQ44.2
035700     MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.           OBSQ44.2
035800      IF   INSPECT-COUNTER EQUAL TO ZERO                          OBSQ44.2
035900          MOVE "NO " TO ERROR-TOTAL                               OBSQ44.2
036000      ELSE MOVE INSPECT-COUNTER TO ERROR-TOTAL.                   OBSQ44.2
036100      MOVE "TEST(S) REQUIRE INSPECTION" TO ENDER-DESC.            OBSQ44.2
036200      MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.          OBSQ44.2
036300     MOVE CCVS-E-3 TO DUMMY-RECORD. PERFORM WRITE-LINE.           OBSQ44.2
036400 WRITE-LINE.                                                      OBSQ44.2
036500     ADD 1 TO RECORD-COUNT.                                       OBSQ44.2
036600Y    IF RECORD-COUNT GREATER 50                                   OBSQ44.2
036700Y        MOVE DUMMY-RECORD TO DUMMY-HOLD                          OBSQ44.2
036800Y        MOVE SPACE TO DUMMY-RECORD                               OBSQ44.2
036900Y        WRITE DUMMY-RECORD AFTER ADVANCING PAGE                  OBSQ44.2
037000Y        MOVE CCVS-C-1 TO DUMMY-RECORD PERFORM WRT-LN             OBSQ44.2
037100Y        MOVE CCVS-C-2 TO DUMMY-RECORD PERFORM WRT-LN 2 TIMES     OBSQ44.2
037200Y        MOVE HYPHEN-LINE TO DUMMY-RECORD PERFORM WRT-LN          OBSQ44.2
037300Y        MOVE DUMMY-HOLD TO DUMMY-RECORD                          OBSQ44.2
037400Y        MOVE ZERO TO RECORD-COUNT.                               OBSQ44.2
037500     PERFORM WRT-LN.                                              OBSQ44.2
037600 WRT-LN.                                                          OBSQ44.2
037700     WRITE    DUMMY-RECORD AFTER ADVANCING 1 LINES.               OBSQ44.2
037800     MOVE SPACE TO DUMMY-RECORD.                                  OBSQ44.2
037900 BLANK-LINE-PRINT.                                                OBSQ44.2
038000     PERFORM WRT-LN.                                              OBSQ44.2
038100 FAIL-ROUTINE.                                                    OBSQ44.2
038200     IF COMPUTED-X NOT EQUAL TO SPACE GO TO FAIL-ROUTINE-WRITE.   OBSQ44.2
038300     IF CORRECT-X NOT EQUAL TO SPACE GO TO FAIL-ROUTINE-WRITE.    OBSQ44.2
038400     MOVE "NO FURTHER INFORMATION, SEE PROGRAM." TO INFO-TEXT.    OBSQ44.2
038500     MOVE XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.     OBSQ44.2
038600     GO TO FAIL-ROUTINE-EX.                                       OBSQ44.2
038700 FAIL-ROUTINE-WRITE.                                              OBSQ44.2
038800     MOVE TEST-COMPUTED TO PRINT-REC PERFORM WRITE-LINE           OBSQ44.2
038900     MOVE TEST-CORRECT TO PRINT-REC PERFORM WRITE-LINE 2 TIMES.   OBSQ44.2
039000 FAIL-ROUTINE-EX. EXIT.                                           OBSQ44.2
039100 BAIL-OUT.                                                        OBSQ44.2
039200     IF COMPUTED-A NOT EQUAL TO SPACE GO TO BAIL-OUT-WRITE.       OBSQ44.2
039300     IF CORRECT-A EQUAL TO SPACE GO TO BAIL-OUT-EX.               OBSQ44.2
039400 BAIL-OUT-WRITE.                                                  OBSQ44.2
039500     MOVE CORRECT-A TO XXCORRECT. MOVE COMPUTED-A TO XXCOMPUTED.  OBSQ44.2
039600     MOVE XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.     OBSQ44.2
039700 BAIL-OUT-EX. EXIT.                                               OBSQ44.2
039800 CCVS1-EXIT.                                                      OBSQ44.2
039900     EXIT.                                                        OBSQ44.2
040000 SECT-OBSQ4A-0001 SECTION.                                        OBSQ44.2
040100 SEQ-INIT-001.                                                    OBSQ44.2
040200     MOVE 0 TO RECORDS-COUNT, RECORDS-IN-ERROR.                   OBSQ44.2
040300     OPEN INPUT SQ-FS1.                                           OBSQ44.2
040400 SEQ-TEST-001.                                                    OBSQ44.2
040500     READ SQ-FS1 AT END GO TO SEQ-TEST-001-01.                    OBSQ44.2
040600     MOVE SQ-FS1R1-F-G-120 TO FILE-RECORD-INFO-P1-120 (1).        OBSQ44.2
040700     ADD 1 TO RECORDS-COUNT.                                      OBSQ44.2
040800     IF RECORDS-COUNT GREATER THAN 750                            OBSQ44.2
040900              MOVE "MORE THAN 750 RECORDS" TO RE-MARK             OBSQ44.2
041000              GO TO SEQ-FAIL-001.                                 OBSQ44.2
041100     IF RECORDS-COUNT NOT EQUAL TO XRECORD-NUMBER (1)             OBSQ44.2
041200              ADD 1 TO RECORDS-IN-ERROR                           OBSQ44.2
041300              GO TO SEQ-TEST-001.                                 OBSQ44.2
041400     IF XFILE-NAME (1) NOT EQUAL TO "SQ-FS1"                      OBSQ44.2
041500              ADD 1 TO RECORDS-IN-ERROR                           OBSQ44.2
041600              GO TO SEQ-TEST-001.                                 OBSQ44.2
041700     IF CHARS-OR-RECORDS (1) NOT EQUAL TO "RC"                    OBSQ44.2
041800              ADD 1 TO RECORDS-IN-ERROR                           OBSQ44.2
041900              GO TO SEQ-TEST-001.                                 OBSQ44.2
042000     IF XBLOCK-SIZE (1) NOT EQUAL TO 1                            OBSQ44.2
042100              ADD 1 TO RECORDS-IN-ERROR.                          OBSQ44.2
042200     GO TO SEQ-TEST-001.                                          OBSQ44.2
042300 SEQ-TEST-001-01.                                                 OBSQ44.2
042400     IF RECORDS-IN-ERROR EQUAL TO ZERO                            OBSQ44.2
042500              GO TO SEQ-PASS-001.                                 OBSQ44.2
042600     MOVE "ERRORS IN READING SQ-FS1" TO RE-MARK.                  OBSQ44.2
042700 SEQ-FAIL-001.                                                    OBSQ44.2
042800     MOVE "RECORDS IN ERROR" TO COMPUTED-A.                       OBSQ44.2
042900     MOVE RECORDS-IN-ERROR TO CORRECT-18V0.                       OBSQ44.2
043000     PERFORM FAIL.                                                OBSQ44.2
043100     GO TO SEQ-WRITE-001.                                         OBSQ44.2
043200 SEQ-PASS-001.                                                    OBSQ44.2
043300     PERFORM PASS.                                                OBSQ44.2
043400     MOVE "FILE VERIFIED RECS =" TO COMPUTED-A.                   OBSQ44.2
043500     MOVE RECORDS-COUNT TO CORRECT-18V0.                          OBSQ44.2
043600 SEQ-WRITE-001.                                                   OBSQ44.2
043700     MOVE "SEQ-TEST-001" TO PAR-NAME.                             OBSQ44.2
043800     MOVE "VERIFY FILE SQ-FS1" TO FEATURE.                        OBSQ44.2
043900     PERFORM PRINT-DETAIL.                                        OBSQ44.2
044000 SEQ-CLOSE-001.                                                   OBSQ44.2
044100     CLOSE SQ-FS1.                                                OBSQ44.2
044200 SEQ-INIT-002.                                                    OBSQ44.2
044300*             THIS TEST READS AND VALIDATES FILE SQ-FS3.          OBSQ44.2
044400     MOVE 0 TO RECORDS-COUNT, RECORDS-IN-ERROR.                   OBSQ44.2
044500     OPEN INPUT SQ-FS3.                                           OBSQ44.2
044600 SEQ-TEST-002.                                                    OBSQ44.2
044700     READ SQ-FS3 AT END GO TO SEQ-TEST-002-01.                    OBSQ44.2
044800     MOVE SQ-FS3R1-F-G-120 TO FILE-RECORD-INFO-P1-120 (1).        OBSQ44.2
044900     ADD 1 TO RECORDS-COUNT.                                      OBSQ44.2
045000     IF RECORDS-COUNT GREATER THAN 750                            OBSQ44.2
045100              MOVE "MORE THAN 750 RECORDS" TO RE-MARK             OBSQ44.2
045200              GO TO SEQ-FAIL-002.                                 OBSQ44.2
045300     IF RECORDS-COUNT NOT EQUAL TO XRECORD-NUMBER (1)             OBSQ44.2
045400              ADD 1 TO RECORDS-IN-ERROR                           OBSQ44.2
045500              GO TO SEQ-TEST-002.                                 OBSQ44.2
045600     IF XFILE-NAME (1) NOT EQUAL TO "SQ-FS3"                      OBSQ44.2
045700              ADD 1 TO RECORDS-IN-ERROR                           OBSQ44.2
045800              GO TO SEQ-TEST-002.                                 OBSQ44.2
045900     IF CHARS-OR-RECORDS (1) NOT EQUAL TO "CH"                    OBSQ44.2
046000              ADD 1 TO RECORDS-IN-ERROR                           OBSQ44.2
046100              GO TO SEQ-TEST-002.                                 OBSQ44.2
046200     IF XBLOCK-SIZE (1) NOT EQUAL TO 1200                         OBSQ44.2
046300              ADD 1 TO RECORDS-IN-ERROR.                          OBSQ44.2
046400     GO TO SEQ-TEST-002.                                          OBSQ44.2
046500 SEQ-TEST-002-01.                                                 OBSQ44.2
046600     IF RECORDS-IN-ERROR EQUAL TO ZERO                            OBSQ44.2
046700              GO TO SEQ-PASS-002.                                 OBSQ44.2
046800     MOVE "ERRORS IN READING SQ-FS3" TO RE-MARK.                  OBSQ44.2
046900 SEQ-FAIL-002.                                                    OBSQ44.2
047000     MOVE "RECORDS IN ERROR" TO COMPUTED-A.                       OBSQ44.2
047100     MOVE RECORDS-IN-ERROR TO CORRECT-18V0.                       OBSQ44.2
047200     PERFORM FAIL.                                                OBSQ44.2
047300     GO TO SEQ-WRITE-002.                                         OBSQ44.2
047400 SEQ-PASS-002.                                                    OBSQ44.2
047500     PERFORM PASS.                                                OBSQ44.2
047600     MOVE "FILE VERIFIED RECS =" TO COMPUTED-A.                   OBSQ44.2
047700     MOVE RECORDS-COUNT TO CORRECT-18V0.                          OBSQ44.2
047800 SEQ-WRITE-002.                                                   OBSQ44.2
047900     MOVE "SEQ-TEST-002" TO PAR-NAME.                             OBSQ44.2
048000     MOVE "VERIFY FILE SQ-FS3" TO FEATURE.                        OBSQ44.2
048100     PERFORM PRINT-DETAIL.                                        OBSQ44.2
048200 SEQ-CLOSE-002.                                                   OBSQ44.2
048300     CLOSE SQ-FS3.                                                OBSQ44.2
048400 SEQ-INIT-003.                                                    OBSQ44.2
048500*             THIS TEST READS AND VALIDATES FILE SQ-FS2.          OBSQ44.2
048600     MOVE 0 TO RECORDS-COUNT, RECORDS-IN-ERROR.                   OBSQ44.2
048700     OPEN INPUT SQ-FS2.                                           OBSQ44.2
048800 SEQ-TEST-003.                                                    OBSQ44.2
048900     READ SQ-FS2 AT END GO TO SEQ-TEST-003-01.                    OBSQ44.2
049000     MOVE SQ-FS2R1-F-G-120 TO FILE-RECORD-INFO-P1-120 (1).        OBSQ44.2
049100     ADD 1 TO RECORDS-COUNT.                                      OBSQ44.2
049200     IF RECORDS-COUNT GREATER THAN 750                            OBSQ44.2
049300              MOVE "MORE THAN 750 RECORDS" TO RE-MARK             OBSQ44.2
049400              GO TO SEQ-FAIL-003.                                 OBSQ44.2
049500     IF RECORDS-COUNT NOT EQUAL TO XRECORD-NUMBER (1)             OBSQ44.2
049600              ADD 1 TO RECORDS-IN-ERROR                           OBSQ44.2
049700              GO TO SEQ-TEST-003.                                 OBSQ44.2
049800     IF XFILE-NAME (1) NOT EQUAL TO "SQ-FS2"                      OBSQ44.2
049900              ADD 1 TO RECORDS-IN-ERROR                           OBSQ44.2
050000              GO TO SEQ-TEST-003.                                 OBSQ44.2
050100     IF CHARS-OR-RECORDS (1) NOT EQUAL TO "RC"                    OBSQ44.2
050200              ADD 1 TO RECORDS-IN-ERROR                           OBSQ44.2
050300              GO TO SEQ-TEST-003.                                 OBSQ44.2
050400     IF XBLOCK-SIZE (1) NOT EQUAL TO 5                            OBSQ44.2
050500              ADD 1 TO RECORDS-IN-ERROR.                          OBSQ44.2
050600     GO TO SEQ-TEST-003.                                          OBSQ44.2
050700 SEQ-TEST-003-01.                                                 OBSQ44.2
050800     IF RECORDS-IN-ERROR EQUAL TO ZERO                            OBSQ44.2
050900              GO TO SEQ-PASS-003.                                 OBSQ44.2
051000     MOVE "ERRORS IN READING SQ-FS2" TO RE-MARK.                  OBSQ44.2
051100 SEQ-FAIL-003.                                                    OBSQ44.2
051200     MOVE "RECORDS IN ERROR" TO COMPUTED-A.                       OBSQ44.2
051300     MOVE RECORDS-IN-ERROR TO CORRECT-18V0.                       OBSQ44.2
051400     PERFORM FAIL.                                                OBSQ44.2
051500     GO TO SEQ-WRITE-003.                                         OBSQ44.2
051600 SEQ-PASS-003.                                                    OBSQ44.2
051700     PERFORM PASS.                                                OBSQ44.2
051800     MOVE "FILE VERIFIED RECS =" TO COMPUTED-A.                   OBSQ44.2
051900     MOVE RECORDS-COUNT TO CORRECT-18V0.                          OBSQ44.2
052000 SEQ-WRITE-003.                                                   OBSQ44.2
052100     MOVE "SEQ-TEST-003" TO PAR-NAME.                             OBSQ44.2
052200     MOVE "VERIFY FILE SQ-FS2" TO FEATURE.                        OBSQ44.2
052300     PERFORM PRINT-DETAIL.                                        OBSQ44.2
052400 SEQ-CLOSE-003.                                                   OBSQ44.2
052500     CLOSE SQ-FS2.                                                OBSQ44.2
052600 SEQ-INIT-004.                                                    OBSQ44.2
052700*             THIS TEST READS AND VALIDATES FILE SQ-FS4.          OBSQ44.2
052800     MOVE 0 TO RECORDS-COUNT, RECORDS-IN-ERROR.                   OBSQ44.2
052900     OPEN INPUT SQ-FS4.                                           OBSQ44.2
053000 SEQ-TEST-004.                                                    OBSQ44.2
053100     READ SQ-FS4 AT END GO TO SEQ-TEST-004-01.                    OBSQ44.2
053200     MOVE SQ-FS4R1-F-G-120 TO FILE-RECORD-INFO-P1-120 (1).        OBSQ44.2
053300     ADD 1 TO RECORDS-COUNT.                                      OBSQ44.2
053400     IF RECORDS-COUNT GREATER THAN 750                            OBSQ44.2
053500              MOVE "MORE THAN 750 RECORDS" TO RE-MARK             OBSQ44.2
053600              GO TO SEQ-FAIL-004.                                 OBSQ44.2
053700     IF RECORDS-COUNT NOT EQUAL TO XRECORD-NUMBER (1)             OBSQ44.2
053800              ADD 1 TO RECORDS-IN-ERROR                           OBSQ44.2
053900              GO TO SEQ-TEST-004.                                 OBSQ44.2
054000     IF XFILE-NAME (1) NOT EQUAL TO "SQ-FS4"                      OBSQ44.2
054100              ADD 1 TO RECORDS-IN-ERROR                           OBSQ44.2
054200              GO TO SEQ-TEST-004.                                 OBSQ44.2
054300     IF CHARS-OR-RECORDS (1) NOT EQUAL TO "RC"                    OBSQ44.2
054400              ADD 1 TO RECORDS-IN-ERROR                           OBSQ44.2
054500              GO TO SEQ-TEST-004.                                 OBSQ44.2
054600     IF XBLOCK-SIZE (1) NOT EQUAL TO 10                           OBSQ44.2
054700              ADD 1 TO RECORDS-IN-ERROR.                          OBSQ44.2
054800     GO TO SEQ-TEST-004.                                          OBSQ44.2
054900 SEQ-TEST-004-01.                                                 OBSQ44.2
055000     IF RECORDS-IN-ERROR EQUAL TO ZERO                            OBSQ44.2
055100              GO TO SEQ-PASS-004.                                 OBSQ44.2
055200     MOVE "ERRORS IN READING SQ-FS4" TO RE-MARK.                  OBSQ44.2
055300 SEQ-FAIL-004.                                                    OBSQ44.2
055400     MOVE "RECORDS IN ERROR" TO COMPUTED-A.                       OBSQ44.2
055500     MOVE RECORDS-IN-ERROR TO CORRECT-18V0.                       OBSQ44.2
055600     PERFORM FAIL.                                                OBSQ44.2
055700     GO TO SEQ-WRITE-004.                                         OBSQ44.2
055800 SEQ-PASS-004.                                                    OBSQ44.2
055900     PERFORM PASS.                                                OBSQ44.2
056000     MOVE "FILE VERIFIED RECS =" TO COMPUTED-A.                   OBSQ44.2
056100     MOVE RECORDS-COUNT TO CORRECT-18V0.                          OBSQ44.2
056200 SEQ-WRITE-004.                                                   OBSQ44.2
056300     MOVE "SEQ-TEST-004" TO PAR-NAME.                             OBSQ44.2
056400     MOVE "VERIFY FILE SQ-FS4" TO FEATURE.                        OBSQ44.2
056500     PERFORM PRINT-DETAIL.                                        OBSQ44.2
056600 SEQ-CLOSE-004.                                                   OBSQ44.2
056700     CLOSE SQ-FS4.                                                OBSQ44.2
056800 OBSQ4A-END-ROUTINE.                                              OBSQ44.2
056900     MOVE "END OF OBSQ4A VALIDATION TESTS" TO PRINT-REC.          OBSQ44.2
057000     WRITE PRINT-REC AFTER ADVANCING 1 LINE.                      OBSQ44.2
057100     GO TO CCVS-EXIT.                                             OBSQ44.2
057200 CCVS-EXIT SECTION.                                               OBSQ44.2
057300 CCVS-999999.                                                     OBSQ44.2
057400     GO TO CLOSE-FILES.                                           OBSQ44.2
       END PROGRAM OBSQ4A.
000100 IDENTIFICATION DIVISION.                                         OBSQ54.2
000200 PROGRAM-ID.                                                      OBSQ54.2
000300     OBSQ5A.                                                      OBSQ54.2
000400****************************************************************  OBSQ54.2
000500*                                                              *  OBSQ54.2
000600*    VALIDATION FOR:-                                          *  OBSQ54.2
000700*    " HIGH       ".                                              OBSQ54.2
000800*    USING CCVS85 VERSION 1.0 ISSUED IN JANUARY 1986.          *  OBSQ54.2
000900*                                                              *  OBSQ54.2
001000*    CREATION DATE     /     VALIDATION DATE                   *  OBSQ54.2
001100*    "4.2 ".                                                      OBSQ54.2
001200*                                                              *  OBSQ54.2
001300*         THE ROUTINE OBSQ5A TESTS THE USE OF THE MULTIPLE FILE   OBSQ54.2
001400*    CLAUSE BY READING AND VALIDATING THE TWO MULTIPLE FILE TAPES OBSQ54.2
001500*    CREATED IN OBSQ3A. TAPE ONE IS PROCESSED USING THE MULTIPLE  OBSQ54.2
001600*    FILE CLAUSE WITH POSITION PHRASE.  ONLY FILE SQ-FS3 IS       OBSQ54.2
001700*    SPECIFIED AND PROCESSED FROM THIS TAPE.  TAPE TWO IS         OBSQ54.2
001800*    PROCESSED USING THE MULTIPLE FILE CLAUSE WITHOUT THE         OBSQ54.2
001900*    POSITION PHRASE.  ALL FOUR FILES ON THIS TAPE ARE PROCESSED. OBSQ54.2
002000*    THESE FILES WERE CREATED USING A MULTIPLE FILE CLAUSE WITH   OBSQ54.2
002100*    POSITION PHRASE.                                             OBSQ54.2
002200 ENVIRONMENT DIVISION.                                            OBSQ54.2
002300 CONFIGURATION SECTION.                                           OBSQ54.2
002400 SOURCE-COMPUTER.                                                 OBSQ54.2
002500     XXXXX082.                                                    OBSQ54.2
002600 OBJECT-COMPUTER.                                                 OBSQ54.2
002700     XXXXX083.                                                    OBSQ54.2
002800 INPUT-OUTPUT SECTION.                                            OBSQ54.2
002900 FILE-CONTROL.                                                    OBSQ54.2
003000P    SELECT RAW-DATA   ASSIGN TO                                  OBSQ54.2
003100P    XXXXX062                                                     OBSQ54.2
003200P           ORGANIZATION IS INDEXED                               OBSQ54.2
003300P           ACCESS MODE IS RANDOM                                 OBSQ54.2
003400P           RECORD KEY IS RAW-DATA-KEY.                           OBSQ54.2
003500     SELECT PRINT-FILE ASSIGN TO                                  OBSQ54.2
003600     XXXXX055.                                                    OBSQ54.2
003700     SELECT SQ-FS3 ASSIGN TO                                      OBSQ54.2
003800     XXXXD009.                                                    OBSQ54.2
003900     SELECT SQ-FS5 ASSIGN TO                                      OBSQ54.2
004000     XXXXD005.                                                    OBSQ54.2
004100     SELECT SQ-FS6 ASSIGN TO                                      OBSQ54.2
004200     XXXXD011.                                                    OBSQ54.2
004300     SELECT SQ-FS7 ASSIGN TO                                      OBSQ54.2
004400     XXXXD012.                                                    OBSQ54.2
004500     SELECT SQ-FS8 ASSIGN TO                                      OBSQ54.2
004600     XXXXD013.                                                    OBSQ54.2
004700 I-O-CONTROL.                                                     OBSQ54.2
004800     MULTIPLE FILE TAPE CONTAINS SQ-FS3 POSITION 3;               OBSQ54.2
004900     MULTIPLE FILE TAPE SQ-FS5,                                   OBSQ54.2
005000                        SQ-FS6,                                   OBSQ54.2
005100                        SQ-FS7,                                   OBSQ54.2
005200                        SQ-FS8.                                   OBSQ54.2
005300 DATA DIVISION.                                                   OBSQ54.2
005400 FILE SECTION.                                                    OBSQ54.2
005500P                                                                 OBSQ54.2
005600PFD  RAW-DATA.                                                    OBSQ54.2
005700P                                                                 OBSQ54.2
005800P01  RAW-DATA-SATZ.                                               OBSQ54.2
005900P    05  RAW-DATA-KEY        PIC X(6).                            OBSQ54.2
006000P    05  C-DATE              PIC 9(6).                            OBSQ54.2
006100P    05  C-TIME              PIC 9(8).                            OBSQ54.2
006200P    05  C-NO-OF-TESTS       PIC 99.                              OBSQ54.2
006300P    05  C-OK                PIC 999.                             OBSQ54.2
006400P    05  C-ALL               PIC 999.                             OBSQ54.2
006500P    05  C-FAIL              PIC 999.                             OBSQ54.2
006600P    05  C-DELETED           PIC 999.                             OBSQ54.2
006700P    05  C-INSPECT           PIC 999.                             OBSQ54.2
006800P    05  C-NOTE              PIC X(13).                           OBSQ54.2
006900P    05  C-INDENT            PIC X.                               OBSQ54.2
007000P    05  C-ABORT             PIC X(8).                            OBSQ54.2
007100 FD  PRINT-FILE.                                                  OBSQ54.2
007200 01  PRINT-REC PICTURE X(120).                                    OBSQ54.2
007300 01  DUMMY-RECORD PICTURE X(120).                                 OBSQ54.2
007400 FD  SQ-FS3                                                       OBSQ54.2
007500     LABEL RECORD IS STANDARD                                     OBSQ54.2
007600     RECORD CONTAINS 120 CHARACTERS                               OBSQ54.2
007700     BLOCK CONTAINS 1200 CHARACTERS.                              OBSQ54.2
007800 01  SQ-FS3R1-F-G-120   PIC X(120).                               OBSQ54.2
007900 FD  SQ-FS5                                                       OBSQ54.2
008000     LABEL RECORD STANDARD                                        OBSQ54.2
008100     BLOCK CONTAINS 5 RECORDS.                                    OBSQ54.2
008200 01  SQ-FS5R1-F-G-120   PIC X(120).                               OBSQ54.2
008300 FD  SQ-FS6                                                       OBSQ54.2
008400     LABEL RECORD STANDARD                                        OBSQ54.2
008500     BLOCK CONTAINS 10 RECORDS.                                   OBSQ54.2
008600 01  SQ-FS6R1-F-G-120   PIC X(120).                               OBSQ54.2
008700 FD  SQ-FS7                                                       OBSQ54.2
008800     LABEL RECORD STANDARD                                        OBSQ54.2
008900     BLOCK CONTAINS 2400 CHARACTERS.                              OBSQ54.2
009000 01  SQ-FS7R1-F-G-120   PIC X(120).                               OBSQ54.2
009100 FD  SQ-FS8                                                       OBSQ54.2
009200     LABEL RECORD STANDARD                                        OBSQ54.2
009300     RECORD 120                                                   OBSQ54.2
009400     BLOCK CONTAINS 120 CHARACTERS.                               OBSQ54.2
009500 01  SQ-FS8R1-F-G-120   PIC X(120).                               OBSQ54.2
009600 WORKING-STORAGE SECTION.                                         OBSQ54.2
009700 77  COUNT-OF-RECS  PICTURE 999 VALUE 0.                          OBSQ54.2
009800 77  RECORDS-IN-ERROR   PIC 999   VALUE 0.                        OBSQ54.2
009900 01  FILE-RECORD-INFORMATION-REC.                                 OBSQ54.2
010000     03 FILE-RECORD-INFO-SKELETON.                                OBSQ54.2
010100        05 FILLER                 PICTURE X(48)       VALUE       OBSQ54.2
010200             "FILE=      ,RECORD=      /0,RECNO=000000,UPDT=00".  OBSQ54.2
010300        05 FILLER                 PICTURE X(46)       VALUE       OBSQ54.2
010400             ",ODO=0000,PGM=     ,LRECL=000000,BLKSIZ  =0000".    OBSQ54.2
010500        05 FILLER                 PICTURE X(26)       VALUE       OBSQ54.2
010600             ",LFIL=000000,ORG=  ,LBLR= ".                        OBSQ54.2
010700        05 FILLER                 PICTURE X(37)       VALUE       OBSQ54.2
010800             ",RECKEY=                             ".             OBSQ54.2
010900        05 FILLER                 PICTURE X(38)       VALUE       OBSQ54.2
011000             ",ALTKEY1=                             ".            OBSQ54.2
011100        05 FILLER                 PICTURE X(38)       VALUE       OBSQ54.2
011200             ",ALTKEY2=                             ".            OBSQ54.2
011300        05 FILLER                 PICTURE X(7)        VALUE SPACE.OBSQ54.2
011400     03 FILE-RECORD-INFO          OCCURS  10  TIMES.              OBSQ54.2
011500        05 FILE-RECORD-INFO-P1-120.                               OBSQ54.2
011600           07 FILLER              PIC X(5).                       OBSQ54.2
011700           07 XFILE-NAME           PIC X(6).                      OBSQ54.2
011800           07 FILLER              PIC X(8).                       OBSQ54.2
011900           07 XRECORD-NAME         PIC X(6).                      OBSQ54.2
012000           07 FILLER              PIC X(1).                       OBSQ54.2
012100           07 REELUNIT-NUMBER     PIC 9(1).                       OBSQ54.2
012200           07 FILLER              PIC X(7).                       OBSQ54.2
012300           07 XRECORD-NUMBER       PIC 9(6).                      OBSQ54.2
012400           07 FILLER              PIC X(6).                       OBSQ54.2
012500           07 UPDATE-NUMBER       PIC 9(2).                       OBSQ54.2
012600           07 FILLER              PIC X(5).                       OBSQ54.2
012700           07 ODO-NUMBER          PIC 9(4).                       OBSQ54.2
012800           07 FILLER              PIC X(5).                       OBSQ54.2
012900           07 XPROGRAM-NAME        PIC X(5).                      OBSQ54.2
013000           07 FILLER              PIC X(7).                       OBSQ54.2
013100           07 XRECORD-LENGTH       PIC 9(6).                      OBSQ54.2
013200           07 FILLER              PIC X(7).                       OBSQ54.2
013300           07 CHARS-OR-RECORDS    PIC X(2).                       OBSQ54.2
013400           07 FILLER              PIC X(1).                       OBSQ54.2
013500           07 XBLOCK-SIZE          PIC 9(4).                      OBSQ54.2
013600           07 FILLER              PIC X(6).                       OBSQ54.2
013700           07 RECORDS-IN-FILE     PIC 9(6).                       OBSQ54.2
013800           07 FILLER              PIC X(5).                       OBSQ54.2
013900           07 XFILE-ORGANIZATION   PIC X(2).                      OBSQ54.2
014000           07 FILLER              PIC X(6).                       OBSQ54.2
014100           07 XLABEL-TYPE          PIC X(1).                      OBSQ54.2
014200        05 FILE-RECORD-INFO-P121-240.                             OBSQ54.2
014300           07 FILLER              PIC X(8).                       OBSQ54.2
014400           07 XRECORD-KEY          PIC X(29).                     OBSQ54.2
014500           07 FILLER              PIC X(9).                       OBSQ54.2
014600           07 ALTERNATE-KEY1      PIC X(29).                      OBSQ54.2
014700           07 FILLER              PIC X(9).                       OBSQ54.2
014800           07 ALTERNATE-KEY2      PIC X(29).                      OBSQ54.2
014900           07 FILLER              PIC X(7).                       OBSQ54.2
015000 01  TEST-RESULTS.                                                OBSQ54.2
015100     02 FILLER                    PICTURE X VALUE SPACE.          OBSQ54.2
015200     02 FEATURE                   PICTURE X(20) VALUE SPACE.      OBSQ54.2
015300     02 FILLER                    PICTURE X VALUE SPACE.          OBSQ54.2
015400     02 P-OR-F                    PICTURE X(5) VALUE SPACE.       OBSQ54.2
015500     02 FILLER                    PICTURE X  VALUE SPACE.         OBSQ54.2
015600     02  PAR-NAME.                                                OBSQ54.2
015700       03 FILLER PICTURE X(12) VALUE SPACE.                       OBSQ54.2
015800       03  PARDOT-X PICTURE X  VALUE SPACE.                       OBSQ54.2
015900       03 DOTVALUE PICTURE 99  VALUE ZERO.                        OBSQ54.2
016000       03 FILLER PIC X(5) VALUE SPACE.                            OBSQ54.2
016100     02 FILLER PIC X(10) VALUE SPACE.                             OBSQ54.2
016200     02 RE-MARK PIC X(61).                                        OBSQ54.2
016300 01  TEST-COMPUTED.                                               OBSQ54.2
016400     02 FILLER PIC X(30) VALUE SPACE.                             OBSQ54.2
016500     02 FILLER PIC X(17) VALUE "       COMPUTED=".                OBSQ54.2
016600     02 COMPUTED-X.                                               OBSQ54.2
016700     03 COMPUTED-A                PICTURE X(20) VALUE SPACE.      OBSQ54.2
016800     03 COMPUTED-N REDEFINES COMPUTED-A PICTURE -9(9).9(9).       OBSQ54.2
016900     03 COMPUTED-0V18 REDEFINES COMPUTED-A  PICTURE -.9(18).      OBSQ54.2
017000     03 COMPUTED-4V14 REDEFINES COMPUTED-A  PICTURE -9(4).9(14).  OBSQ54.2
017100     03 COMPUTED-14V4 REDEFINES COMPUTED-A  PICTURE -9(14).9(4).  OBSQ54.2
017200     03       CM-18V0 REDEFINES COMPUTED-A.                       OBSQ54.2
017300         04 COMPUTED-18V0                   PICTURE -9(18).       OBSQ54.2
017400         04 FILLER                          PICTURE X.            OBSQ54.2
017500     03 FILLER PIC X(50) VALUE SPACE.                             OBSQ54.2
017600 01  TEST-CORRECT.                                                OBSQ54.2
017700     02 FILLER PIC X(30) VALUE SPACE.                             OBSQ54.2
017800     02 FILLER PIC X(17) VALUE "       CORRECT =".                OBSQ54.2
017900     02 CORRECT-X.                                                OBSQ54.2
018000     03 CORRECT-A                 PICTURE X(20) VALUE SPACE.      OBSQ54.2
018100     03 CORRECT-N REDEFINES CORRECT-A PICTURE -9(9).9(9).         OBSQ54.2
018200     03 CORRECT-0V18 REDEFINES CORRECT-A    PICTURE -.9(18).      OBSQ54.2
018300     03 CORRECT-4V14 REDEFINES CORRECT-A    PICTURE -9(4).9(14).  OBSQ54.2
018400     03 CORRECT-14V4 REDEFINES CORRECT-A    PICTURE -9(14).9(4).  OBSQ54.2
018500     03      CR-18V0 REDEFINES CORRECT-A.                         OBSQ54.2
018600         04 CORRECT-18V0                    PICTURE -9(18).       OBSQ54.2
018700         04 FILLER                          PICTURE X.            OBSQ54.2
018800     03 FILLER PIC X(50) VALUE SPACE.                             OBSQ54.2
018900 01  CCVS-C-1.                                                    OBSQ54.2
019000     02 FILLER PICTURE IS X(99) VALUE IS " FEATURE              PAOBSQ54.2
019100-    "SS  PARAGRAPH-NAME                                          OBSQ54.2
019200-    "        REMARKS".                                           OBSQ54.2
019300     02 FILLER PICTURE IS X(20) VALUE IS SPACE.                   OBSQ54.2
019400 01  CCVS-C-2.                                                    OBSQ54.2
019500     02 FILLER PICTURE IS X VALUE IS SPACE.                       OBSQ54.2
019600     02 FILLER PICTURE IS X(6) VALUE IS "TESTED".                 OBSQ54.2
019700     02 FILLER PICTURE IS X(15) VALUE IS SPACE.                   OBSQ54.2
019800     02 FILLER PICTURE IS X(4) VALUE IS "FAIL".                   OBSQ54.2
019900     02 FILLER PICTURE IS X(94) VALUE IS SPACE.                   OBSQ54.2
020000 01  REC-SKL-SUB PICTURE 9(2) VALUE ZERO.                         OBSQ54.2
020100 01  REC-CT PICTURE 99 VALUE ZERO.                                OBSQ54.2
020200 01  DELETE-CNT                   PICTURE 999  VALUE ZERO.        OBSQ54.2
020300 01  ERROR-COUNTER PICTURE IS 999 VALUE IS ZERO.                  OBSQ54.2
020400 01  INSPECT-COUNTER PIC 999 VALUE ZERO.                          OBSQ54.2
020500 01  PASS-COUNTER PIC 999 VALUE ZERO.                             OBSQ54.2
020600 01  TOTAL-ERROR PIC 999 VALUE ZERO.                              OBSQ54.2
020700 01  ERROR-HOLD PIC 999 VALUE ZERO.                               OBSQ54.2
020800 01  DUMMY-HOLD PIC X(120) VALUE SPACE.                           OBSQ54.2
020900 01  RECORD-COUNT PIC 9(5) VALUE ZERO.                            OBSQ54.2
021000 01  REC-COUNT    PIC 9(5) VALUE ZERO.                            OBSQ54.2
021100 01  CCVS-H-1.                                                    OBSQ54.2
021200     02  FILLER   PICTURE X(27)  VALUE SPACE.                     OBSQ54.2
021300     02 FILLER PICTURE X(67) VALUE                                OBSQ54.2
021400     " FEDERAL SOFTWARE TESTING CENTER COBOL COMPILER VALIDATION  OBSQ54.2
021500-    " SYSTEM".                                                   OBSQ54.2
021600     02  FILLER     PICTURE X(26)  VALUE SPACE.                   OBSQ54.2
021700 01  CCVS-H-2.                                                    OBSQ54.2
021800     02 FILLER PICTURE X(52) VALUE IS                             OBSQ54.2
021900     "CCVS85 FSTC COPY, NOT FOR DISTRIBUTION.".                   OBSQ54.2
022000     02 FILLER PICTURE IS X(19) VALUE IS "TEST RESULTS SET-  ".   OBSQ54.2
022100     02 TEST-ID PICTURE IS X(9).                                  OBSQ54.2
022200     02 FILLER PICTURE IS X(40) VALUE IS SPACE.                   OBSQ54.2
022300 01  CCVS-H-3.                                                    OBSQ54.2
022400     02  FILLER PICTURE X(34) VALUE                               OBSQ54.2
022500     " FOR OFFICIAL USE ONLY    ".                                OBSQ54.2
022600     02  FILLER PICTURE X(58) VALUE                               OBSQ54.2
022700     "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".OBSQ54.2
022800     02  FILLER PICTURE X(28) VALUE                               OBSQ54.2
022900     "  COPYRIGHT   1985 ".                                       OBSQ54.2
023000 01  CCVS-E-1.                                                    OBSQ54.2
023100     02 FILLER PICTURE IS X(52) VALUE IS SPACE.                   OBSQ54.2
023200     02 FILLER PICTURE IS X(14) VALUE IS "END OF TEST-  ".        OBSQ54.2
023300     02 ID-AGAIN PICTURE IS X(9).                                 OBSQ54.2
023400     02 FILLER PICTURE X(45) VALUE IS                             OBSQ54.2
023500     " NTIS DISTRIBUTION COBOL 85".                               OBSQ54.2
023600 01  CCVS-E-2.                                                    OBSQ54.2
023700     02  FILLER                   PICTURE X(31)  VALUE            OBSQ54.2
023800     SPACE.                                                       OBSQ54.2
023900     02  FILLER                   PICTURE X(21)  VALUE SPACE.     OBSQ54.2
024000     02 CCVS-E-2-2.                                               OBSQ54.2
024100         03 ERROR-TOTAL PICTURE IS XXX VALUE IS SPACE.            OBSQ54.2
024200         03 FILLER PICTURE IS X VALUE IS SPACE.                   OBSQ54.2
024300         03 ENDER-DESC PIC X(46) VALUE "ERRORS ENCOUNTERED".      OBSQ54.2
024400 01  CCVS-E-3.                                                    OBSQ54.2
024500     02  FILLER PICTURE X(22) VALUE                               OBSQ54.2
024600     " FOR OFFICIAL USE ONLY".                                    OBSQ54.2
024700     02  FILLER PICTURE X(12) VALUE SPACE.                        OBSQ54.2
024800     02  FILLER PICTURE X(58) VALUE                               OBSQ54.2
024900     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".OBSQ54.2
025000     02  FILLER PICTURE X(13) VALUE SPACE.                        OBSQ54.2
025100     02 FILLER PIC X(15) VALUE " COPYRIGHT 1985".                 OBSQ54.2
025200 01  CCVS-E-4.                                                    OBSQ54.2
025300     02 CCVS-E-4-1 PIC XXX VALUE SPACE.                           OBSQ54.2
025400     02 FILLER PIC XXXX VALUE " OF ".                             OBSQ54.2
025500     02 CCVS-E-4-2 PIC XXX VALUE SPACE.                           OBSQ54.2
025600     02 FILLER PIC X(40) VALUE                                    OBSQ54.2
025700      "  TESTS WERE EXECUTED SUCCESSFULLY".                       OBSQ54.2
025800 01  XXINFO.                                                      OBSQ54.2
025900     02 FILLER PIC X(30) VALUE "        *** INFORMATION  ***".    OBSQ54.2
026000     02 INFO-TEXT.                                                OBSQ54.2
026100     04 FILLER PIC X(20) VALUE SPACE.                             OBSQ54.2
026200     04 XXCOMPUTED PIC X(20).                                     OBSQ54.2
026300     04 FILLER PIC X(5) VALUE SPACE.                              OBSQ54.2
026400     04 XXCORRECT PIC X(20).                                      OBSQ54.2
026500 01  HYPHEN-LINE.                                                 OBSQ54.2
026600     02 FILLER PICTURE IS X VALUE IS SPACE.                       OBSQ54.2
026700     02 FILLER PICTURE IS X(65) VALUE IS "************************OBSQ54.2
026800-    "*****************************************".                 OBSQ54.2
026900     02 FILLER PICTURE IS X(54) VALUE IS "************************OBSQ54.2
027000-    "******************************".                            OBSQ54.2
027100 01  CCVS-PGM-ID PIC X(6) VALUE                                   OBSQ54.2
027200     "OBSQ5A".                                                    OBSQ54.2
027300 PROCEDURE DIVISION.                                              OBSQ54.2
027400 CCVS1 SECTION.                                                   OBSQ54.2
027500 OPEN-FILES.                                                      OBSQ54.2
027600P    OPEN I-O RAW-DATA.                                           OBSQ54.2
027700P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            OBSQ54.2
027800P    READ RAW-DATA INVALID KEY GO TO END-E-1.                     OBSQ54.2
027900P    MOVE "ABORTED " TO C-ABORT.                                  OBSQ54.2
028000P    ADD 1 TO C-NO-OF-TESTS.                                      OBSQ54.2
028100P    ACCEPT C-DATE  FROM DATE.                                    OBSQ54.2
028200P    ACCEPT C-TIME  FROM TIME.                                    OBSQ54.2
028300P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO END-E-1.             OBSQ54.2
028400PEND-E-1.                                                         OBSQ54.2
028500P    CLOSE RAW-DATA.                                              OBSQ54.2
028600     OPEN     OUTPUT PRINT-FILE.                                  OBSQ54.2
028700     MOVE CCVS-PGM-ID TO TEST-ID. MOVE CCVS-PGM-ID TO ID-AGAIN.   OBSQ54.2
028800     MOVE    SPACE TO TEST-RESULTS.                               OBSQ54.2
028900     PERFORM  HEAD-ROUTINE THRU COLUMN-NAMES-ROUTINE.             OBSQ54.2
029000     MOVE ZERO TO REC-SKL-SUB.                                    OBSQ54.2
029100     PERFORM CCVS-INIT-FILE 9 TIMES.                              OBSQ54.2
029200 CCVS-INIT-FILE.                                                  OBSQ54.2
029300     ADD 1 TO REC-SKL-SUB.                                        OBSQ54.2
029400     MOVE FILE-RECORD-INFO-SKELETON TO                            OBSQ54.2
029500                  FILE-RECORD-INFO (REC-SKL-SUB).                 OBSQ54.2
029600 CCVS-INIT-EXIT.                                                  OBSQ54.2
029700     GO TO CCVS1-EXIT.                                            OBSQ54.2
029800 CLOSE-FILES.                                                     OBSQ54.2
029900     PERFORM END-ROUTINE THRU END-ROUTINE-13. CLOSE PRINT-FILE.   OBSQ54.2
030000P    OPEN I-O RAW-DATA.                                           OBSQ54.2
030100P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            OBSQ54.2
030200P    READ RAW-DATA INVALID KEY GO TO END-E-2.                     OBSQ54.2
030300P    MOVE "OK.     " TO C-ABORT.                                  OBSQ54.2
030400P    MOVE PASS-COUNTER TO C-OK.                                   OBSQ54.2
030500P    MOVE ERROR-HOLD   TO C-ALL.                                  OBSQ54.2
030600P    MOVE ERROR-COUNTER TO C-FAIL.                                OBSQ54.2
030700P    MOVE DELETE-CNT TO C-DELETED.                                OBSQ54.2
030800P    MOVE INSPECT-COUNTER TO C-INSPECT.                           OBSQ54.2
030900P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO END-E-2.             OBSQ54.2
031000PEND-E-2.                                                         OBSQ54.2
031100P    CLOSE RAW-DATA.                                              OBSQ54.2
031200 TERMINATE-CCVS.                                                  OBSQ54.2
031300S    EXIT PROGRAM.                                                OBSQ54.2
031400STERMINATE-CALL.                                                  OBSQ54.2
031500     STOP     RUN.                                                OBSQ54.2
031600 INSPT. MOVE "INSPT" TO P-OR-F. ADD 1 TO INSPECT-COUNTER.         OBSQ54.2
031700 PASS.  MOVE "PASS " TO P-OR-F.  ADD 1 TO PASS-COUNTER.           OBSQ54.2
031800 FAIL.  MOVE "FAIL*" TO P-OR-F.  ADD 1 TO ERROR-COUNTER.          OBSQ54.2
031900 DE-LETE.  MOVE "*****" TO P-OR-F.  ADD 1 TO DELETE-CNT.          OBSQ54.2
032000     MOVE "****TEST DELETED****" TO RE-MARK.                      OBSQ54.2
032100 PRINT-DETAIL.                                                    OBSQ54.2
032200     IF REC-CT NOT EQUAL TO ZERO                                  OBSQ54.2
032300             MOVE "." TO PARDOT-X                                 OBSQ54.2
032400             MOVE REC-CT TO DOTVALUE.                             OBSQ54.2
032500     MOVE     TEST-RESULTS TO PRINT-REC. PERFORM WRITE-LINE.      OBSQ54.2
032600     IF P-OR-F EQUAL TO "FAIL*"  PERFORM WRITE-LINE               OBSQ54.2
032700        PERFORM FAIL-ROUTINE THRU FAIL-ROUTINE-EX                 OBSQ54.2
032800          ELSE PERFORM BAIL-OUT THRU BAIL-OUT-EX.                 OBSQ54.2
032900     MOVE SPACE TO P-OR-F. MOVE SPACE TO COMPUTED-X.              OBSQ54.2
033000     MOVE SPACE TO CORRECT-X.                                     OBSQ54.2
033100     IF     REC-CT EQUAL TO ZERO  MOVE SPACE TO PAR-NAME.         OBSQ54.2
033200     MOVE     SPACE TO RE-MARK.                                   OBSQ54.2
033300 HEAD-ROUTINE.                                                    OBSQ54.2
033400     MOVE CCVS-H-1 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   OBSQ54.2
033500     MOVE CCVS-H-2 TO DUMMY-RECORD. PERFORM WRITE-LINE 5 TIMES.   OBSQ54.2
033600     MOVE CCVS-H-3 TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.   OBSQ54.2
033700 COLUMN-NAMES-ROUTINE.                                            OBSQ54.2
033800     MOVE CCVS-C-1 TO DUMMY-RECORD. PERFORM WRITE-LINE.           OBSQ54.2
033900     MOVE CCVS-C-2 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   OBSQ54.2
034000     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE.        OBSQ54.2
034100 END-ROUTINE.                                                     OBSQ54.2
034200     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE 5 TIMES.OBSQ54.2
034300 END-RTN-EXIT.                                                    OBSQ54.2
034400     MOVE CCVS-E-1 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   OBSQ54.2
034500 END-ROUTINE-1.                                                   OBSQ54.2
034600      ADD ERROR-COUNTER TO ERROR-HOLD ADD INSPECT-COUNTER TO      OBSQ54.2
034700      ERROR-HOLD. ADD DELETE-CNT TO ERROR-HOLD.                   OBSQ54.2
034800      ADD PASS-COUNTER TO ERROR-HOLD.                             OBSQ54.2
034900*     IF PASS-COUNTER EQUAL TO ERROR-HOLD GO TO END-ROUTINE-12.   OBSQ54.2
035000      MOVE PASS-COUNTER TO CCVS-E-4-1.                            OBSQ54.2
035100      MOVE ERROR-HOLD TO CCVS-E-4-2.                              OBSQ54.2
035200      MOVE CCVS-E-4 TO CCVS-E-2-2.                                OBSQ54.2
035300      MOVE CCVS-E-2 TO DUMMY-RECORD PERFORM WRITE-LINE.           OBSQ54.2
035400  END-ROUTINE-12.                                                 OBSQ54.2
035500      MOVE "TEST(S) FAILED" TO ENDER-DESC.                        OBSQ54.2
035600     IF       ERROR-COUNTER IS EQUAL TO ZERO                      OBSQ54.2
035700         MOVE "NO " TO ERROR-TOTAL                                OBSQ54.2
035800         ELSE                                                     OBSQ54.2
035900         MOVE ERROR-COUNTER TO ERROR-TOTAL.                       OBSQ54.2
036000     MOVE     CCVS-E-2 TO DUMMY-RECORD.                           OBSQ54.2
036100     PERFORM WRITE-LINE.                                          OBSQ54.2
036200 END-ROUTINE-13.                                                  OBSQ54.2
036300     IF DELETE-CNT IS EQUAL TO ZERO                               OBSQ54.2
036400         MOVE "NO " TO ERROR-TOTAL  ELSE                          OBSQ54.2
036500         MOVE DELETE-CNT TO ERROR-TOTAL.                          OBSQ54.2
036600     MOVE "TEST(S) DELETED     " TO ENDER-DESC.                   OBSQ54.2
036700     MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.           OBSQ54.2
036800      IF   INSPECT-COUNTER EQUAL TO ZERO                          OBSQ54.2
036900          MOVE "NO " TO ERROR-TOTAL                               OBSQ54.2
037000      ELSE MOVE INSPECT-COUNTER TO ERROR-TOTAL.                   OBSQ54.2
037100      MOVE "TEST(S) REQUIRE INSPECTION" TO ENDER-DESC.            OBSQ54.2
037200      MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.          OBSQ54.2
037300     MOVE CCVS-E-3 TO DUMMY-RECORD. PERFORM WRITE-LINE.           OBSQ54.2
037400 WRITE-LINE.                                                      OBSQ54.2
037500     ADD 1 TO RECORD-COUNT.                                       OBSQ54.2
037600Y    IF RECORD-COUNT GREATER 50                                   OBSQ54.2
037700Y        MOVE DUMMY-RECORD TO DUMMY-HOLD                          OBSQ54.2
037800Y        MOVE SPACE TO DUMMY-RECORD                               OBSQ54.2
037900Y        WRITE DUMMY-RECORD AFTER ADVANCING PAGE                  OBSQ54.2
038000Y        MOVE CCVS-C-1 TO DUMMY-RECORD PERFORM WRT-LN             OBSQ54.2
038100Y        MOVE CCVS-C-2 TO DUMMY-RECORD PERFORM WRT-LN 2 TIMES     OBSQ54.2
038200Y        MOVE HYPHEN-LINE TO DUMMY-RECORD PERFORM WRT-LN          OBSQ54.2
038300Y        MOVE DUMMY-HOLD TO DUMMY-RECORD                          OBSQ54.2
038400Y        MOVE ZERO TO RECORD-COUNT.                               OBSQ54.2
038500     PERFORM WRT-LN.                                              OBSQ54.2
038600 WRT-LN.                                                          OBSQ54.2
038700     WRITE    DUMMY-RECORD AFTER ADVANCING 1 LINES.               OBSQ54.2
038800     MOVE SPACE TO DUMMY-RECORD.                                  OBSQ54.2
038900 BLANK-LINE-PRINT.                                                OBSQ54.2
039000     PERFORM WRT-LN.                                              OBSQ54.2
039100 FAIL-ROUTINE.                                                    OBSQ54.2
039200     IF COMPUTED-X NOT EQUAL TO SPACE GO TO FAIL-ROUTINE-WRITE.   OBSQ54.2
039300     IF CORRECT-X NOT EQUAL TO SPACE GO TO FAIL-ROUTINE-WRITE.    OBSQ54.2
039400     MOVE "NO FURTHER INFORMATION, SEE PROGRAM." TO INFO-TEXT.    OBSQ54.2
039500     MOVE XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.     OBSQ54.2
039600     GO TO FAIL-ROUTINE-EX.                                       OBSQ54.2
039700 FAIL-ROUTINE-WRITE.                                              OBSQ54.2
039800     MOVE TEST-COMPUTED TO PRINT-REC PERFORM WRITE-LINE           OBSQ54.2
039900     MOVE TEST-CORRECT TO PRINT-REC PERFORM WRITE-LINE 2 TIMES.   OBSQ54.2
040000 FAIL-ROUTINE-EX. EXIT.                                           OBSQ54.2
040100 BAIL-OUT.                                                        OBSQ54.2
040200     IF COMPUTED-A NOT EQUAL TO SPACE GO TO BAIL-OUT-WRITE.       OBSQ54.2
040300     IF CORRECT-A EQUAL TO SPACE GO TO BAIL-OUT-EX.               OBSQ54.2
040400 BAIL-OUT-WRITE.                                                  OBSQ54.2
040500     MOVE CORRECT-A TO XXCORRECT. MOVE COMPUTED-A TO XXCOMPUTED.  OBSQ54.2
040600     MOVE XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.     OBSQ54.2
040700 BAIL-OUT-EX. EXIT.                                               OBSQ54.2
040800 CCVS1-EXIT.                                                      OBSQ54.2
040900     EXIT.                                                        OBSQ54.2
041000 SECT-OBSQ5A-0001 SECTION.                                        OBSQ54.2
041100 SEQ-INIT-001.                                                    OBSQ54.2
041200     MOVE 0 TO REC-COUNT,    RECORDS-IN-ERROR.                    OBSQ54.2
041300     OPEN INPUT SQ-FS3.                                           OBSQ54.2
041400 SEQ-TEST-001.                                                    OBSQ54.2
041500     READ SQ-FS3 AT END GO TO SEQ-TEST-001-01.                    OBSQ54.2
041600     MOVE SQ-FS3R1-F-G-120 TO FILE-RECORD-INFO-P1-120 (1).        OBSQ54.2
041700     ADD 1 TO REC-COUNT.                                          OBSQ54.2
041800     IF REC-COUNT    GREATER THAN 750                             OBSQ54.2
041900              MOVE "MORE THAN 750 RECORDS" TO RE-MARK             OBSQ54.2
042000              GO TO SEQ-FAIL-001.                                 OBSQ54.2
042100     IF REC-COUNT    NOT EQUAL TO XRECORD-NUMBER (1)              OBSQ54.2
042200              ADD 1 TO RECORDS-IN-ERROR                           OBSQ54.2
042300              GO TO SEQ-TEST-001.                                 OBSQ54.2
042400     IF XFILE-NAME (1) NOT EQUAL TO "SQ-FS3"                      OBSQ54.2
042500              ADD 1 TO RECORDS-IN-ERROR                           OBSQ54.2
042600              GO TO SEQ-TEST-001.                                 OBSQ54.2
042700     IF CHARS-OR-RECORDS (1) NOT EQUAL TO "CH"                    OBSQ54.2
042800              ADD 1 TO RECORDS-IN-ERROR                           OBSQ54.2
042900              GO TO SEQ-TEST-001.                                 OBSQ54.2
043000     IF XBLOCK-SIZE (1) NOT EQUAL TO 1200                         OBSQ54.2
043100              ADD 1 TO RECORDS-IN-ERROR.                          OBSQ54.2
043200     GO TO SEQ-TEST-001.                                          OBSQ54.2
043300 SEQ-TEST-001-01.                                                 OBSQ54.2
043400     IF RECORDS-IN-ERROR EQUAL TO ZERO                            OBSQ54.2
043500              GO TO SEQ-PASS-001.                                 OBSQ54.2
043600     MOVE "ERRORS IN READING SQ-FS3" TO RE-MARK.                  OBSQ54.2
043700 SEQ-FAIL-001.                                                    OBSQ54.2
043800     MOVE "RECORDS IN ERROR" TO COMPUTED-A.                       OBSQ54.2
043900     MOVE RECORDS-IN-ERROR TO CORRECT-18V0.                       OBSQ54.2
044000     PERFORM FAIL.                                                OBSQ54.2
044100     GO TO SEQ-WRITE-001.                                         OBSQ54.2
044200 SEQ-PASS-001.                                                    OBSQ54.2
044300     PERFORM PASS.                                                OBSQ54.2
044400     MOVE "FILE VERIFIED RECS =" TO COMPUTED-A.                   OBSQ54.2
044500     MOVE REC-COUNT    TO CORRECT-18V0.                           OBSQ54.2
044600 SEQ-WRITE-001.                                                   OBSQ54.2
044700     MOVE "SEQ-TEST-001" TO PAR-NAME.                             OBSQ54.2
044800     MOVE "VERIFY FILE SQ-FS3" TO FEATURE.                        OBSQ54.2
044900     PERFORM PRINT-DETAIL.                                        OBSQ54.2
045000 SEQ-CLOSE-001.                                                   OBSQ54.2
045100     CLOSE SQ-FS3.                                                OBSQ54.2
045200 SEQ-INIT-002.                                                    OBSQ54.2
045300*             THIS TEST READS AND VALIDATES FILE SQ-FS5.          OBSQ54.2
045400     MOVE 0 TO REC-COUNT,    RECORDS-IN-ERROR.                    OBSQ54.2
045500     OPEN INPUT SQ-FS5.                                           OBSQ54.2
045600 SEQ-TEST-002.                                                    OBSQ54.2
045700     READ SQ-FS5 AT END GO TO SEQ-TEST-002-01.                    OBSQ54.2
045800     MOVE SQ-FS5R1-F-G-120 TO FILE-RECORD-INFO-P1-120 (1).        OBSQ54.2
045900     ADD 1 TO REC-COUNT.                                          OBSQ54.2
046000     IF REC-COUNT    GREATER THAN 750                             OBSQ54.2
046100              MOVE "MORE THAN 750 RECORDS" TO RE-MARK             OBSQ54.2
046200              GO TO SEQ-FAIL-002.                                 OBSQ54.2
046300     IF REC-COUNT    NOT EQUAL TO XRECORD-NUMBER (1)              OBSQ54.2
046400              ADD 1 TO RECORDS-IN-ERROR                           OBSQ54.2
046500              GO TO SEQ-TEST-002.                                 OBSQ54.2
046600     IF XFILE-NAME (1) NOT EQUAL TO "SQ-FS5"                      OBSQ54.2
046700              ADD 1 TO RECORDS-IN-ERROR                           OBSQ54.2
046800              GO TO SEQ-TEST-002.                                 OBSQ54.2
046900     IF CHARS-OR-RECORDS (1) NOT EQUAL TO "RC"                    OBSQ54.2
047000              ADD 1 TO RECORDS-IN-ERROR                           OBSQ54.2
047100              GO TO SEQ-TEST-002.                                 OBSQ54.2
047200     IF XBLOCK-SIZE (1) NOT EQUAL TO 5                            OBSQ54.2
047300              ADD 1 TO RECORDS-IN-ERROR.                          OBSQ54.2
047400     GO TO SEQ-TEST-002.                                          OBSQ54.2
047500 SEQ-TEST-002-01.                                                 OBSQ54.2
047600     IF RECORDS-IN-ERROR EQUAL TO ZERO                            OBSQ54.2
047700              GO TO SEQ-PASS-002.                                 OBSQ54.2
047800     MOVE "ERRORS IN READINGS SQ-FS5" TO RE-MARK.                 OBSQ54.2
047900 SEQ-FAIL-002.                                                    OBSQ54.2
048000     MOVE "RECORDS IN ERROR" TO COMPUTED-A.                       OBSQ54.2
048100     MOVE RECORDS-IN-ERROR TO CORRECT-18V0.                       OBSQ54.2
048200     PERFORM FAIL.                                                OBSQ54.2
048300     GO TO SEQ-WRITE-002.                                         OBSQ54.2
048400 SEQ-PASS-002.                                                    OBSQ54.2
048500     PERFORM PASS.                                                OBSQ54.2
048600     MOVE "FILE VERIFIED RECS =" TO COMPUTED-A.                   OBSQ54.2
048700     MOVE REC-COUNT    TO CORRECT-18V0.                           OBSQ54.2
048800 SEQ-WRITE-002.                                                   OBSQ54.2
048900     MOVE "SEQ-TEST-002" TO PAR-NAME.                             OBSQ54.2
049000     MOVE "VERIFY FILE SQ-FS5" TO FEATURE                         OBSQ54.2
049100     PERFORM PRINT-DETAIL.                                        OBSQ54.2
049200 SEQ-CLOSE-002.                                                   OBSQ54.2
049300     CLOSE SQ-FS5 WITH NO REWIND.                                 OBSQ54.2
049400 SEQ-INIT-003.                                                    OBSQ54.2
049500*             THIS TEST READS AND VALIDATES FILE SQ-FS6.          OBSQ54.2
049600     MOVE 0 TO REC-COUNT,    RECORDS-IN-ERROR.                    OBSQ54.2
049700     OPEN INPUT SQ-FS6 WITH NO REWIND.                            OBSQ54.2
049800 SEQ-TEST-003.                                                    OBSQ54.2
049900     READ SQ-FS6 AT END GO TO SEQ-TEST-003-01.                    OBSQ54.2
050000     MOVE SQ-FS6R1-F-G-120 TO FILE-RECORD-INFO-P1-120 (1).        OBSQ54.2
050100     ADD 1 TO REC-COUNT.                                          OBSQ54.2
050200     IF REC-COUNT    GREATER THAN 750                             OBSQ54.2
050300              MOVE "MORE THAN 750 RECORDS" TO RE-MARK             OBSQ54.2
050400              GO TO SEQ-FAIL-003.                                 OBSQ54.2
050500     IF REC-COUNT    NOT EQUAL TO XRECORD-NUMBER (1)              OBSQ54.2
050600              ADD 1 TO RECORDS-IN-ERROR                           OBSQ54.2
050700              GO TO SEQ-TEST-003.                                 OBSQ54.2
050800     IF XFILE-NAME (1) NOT EQUAL TO "SQ-FS6"                      OBSQ54.2
050900              ADD 1 TO RECORDS-IN-ERROR                           OBSQ54.2
051000              GO TO SEQ-TEST-003.                                 OBSQ54.2
051100     IF CHARS-OR-RECORDS (1) NOT EQUAL TO "RC"                    OBSQ54.2
051200              ADD 1 TO RECORDS-IN-ERROR                           OBSQ54.2
051300              GO TO SEQ-TEST-003.                                 OBSQ54.2
051400     IF XBLOCK-SIZE (1) NOT EQUAL TO 10                           OBSQ54.2
051500              ADD 1 TO RECORDS-IN-ERROR.                          OBSQ54.2
051600     GO TO SEQ-TEST-003.                                          OBSQ54.2
051700 SEQ-TEST-003-01.                                                 OBSQ54.2
051800     IF RECORDS-IN-ERROR EQUAL TO ZERO                            OBSQ54.2
051900              GO TO SEQ-PASS-003.                                 OBSQ54.2
052000     MOVE "ERRORS IN READING SQ-FS6" TO RE-MARK.                  OBSQ54.2
052100 SEQ-FAIL-003.                                                    OBSQ54.2
052200     MOVE "RECORDS IN ERROR" TO COMPUTED-A.                       OBSQ54.2
052300     MOVE RECORDS-IN-ERROR TO CORRECT-18V0.                       OBSQ54.2
052400     PERFORM FAIL.                                                OBSQ54.2
052500     GO TO SEQ-WRITE-003.                                         OBSQ54.2
052600 SEQ-PASS-003.                                                    OBSQ54.2
052700     PERFORM PASS.                                                OBSQ54.2
052800     MOVE "FILE VERIFIED RECS =" TO COMPUTED-A.                   OBSQ54.2
052900     MOVE REC-COUNT    TO CORRECT-18V0.                           OBSQ54.2
053000 SEQ-WRITE-003.                                                   OBSQ54.2
053100     MOVE "SEQ-TEST-003" TO PAR-NAME.                             OBSQ54.2
053200     MOVE "VERIFY FILE SQ-FS6" TO FEATURE.                        OBSQ54.2
053300     PERFORM PRINT-DETAIL.                                        OBSQ54.2
053400 SEQ-CLOSE-003.                                                   OBSQ54.2
053500     CLOSE SQ-FS6 WITH NO REWIND.                                 OBSQ54.2
053600 SEQ-INIT-004.                                                    OBSQ54.2
053700*             THIS TEST READS AND VALIDATES FILE SQ-FS7.          OBSQ54.2
053800     MOVE 0 TO REC-COUNT,    RECORDS-IN-ERROR.                    OBSQ54.2
053900     OPEN INPUT SQ-FS7 WITH NO REWIND.                            OBSQ54.2
054000 SEQ-TEST-004.                                                    OBSQ54.2
054100     READ SQ-FS7 AT END GO TO SEQ-TEST-004-01.                    OBSQ54.2
054200     MOVE SQ-FS7R1-F-G-120 TO FILE-RECORD-INFO-P1-120 (1).        OBSQ54.2
054300     ADD 1 TO REC-COUNT.                                          OBSQ54.2
054400     IF REC-COUNT    GREATER THAN 750                             OBSQ54.2
054500              MOVE "MORE THAN 750 RECORDS" TO RE-MARK             OBSQ54.2
054600              GO TO SEQ-FAIL-004.                                 OBSQ54.2
054700     IF REC-COUNT    NOT EQUAL TO XRECORD-NUMBER (1)              OBSQ54.2
054800              ADD 1 TO RECORDS-IN-ERROR                           OBSQ54.2
054900              GO TO SEQ-TEST-004.                                 OBSQ54.2
055000     IF XFILE-NAME (1) NOT EQUAL TO "SQ-FS7"                      OBSQ54.2
055100              ADD 1 TO RECORDS-IN-ERROR                           OBSQ54.2
055200              GO TO SEQ-TEST-004.                                 OBSQ54.2
055300     IF CHARS-OR-RECORDS (1) NOT EQUAL "CH"                       OBSQ54.2
055400              ADD 1 TO RECORDS-IN-ERROR                           OBSQ54.2
055500              GO TO SEQ-TEST-004.                                 OBSQ54.2
055600     IF XBLOCK-SIZE (1) NOT EQUAL TO 2400                         OBSQ54.2
055700              ADD 1 TO RECORDS-IN-ERROR.                          OBSQ54.2
055800     GO TO SEQ-TEST-004.                                          OBSQ54.2
055900 SEQ-TEST-004-01.                                                 OBSQ54.2
056000     IF RECORDS-IN-ERROR EQUAL TO ZERO                            OBSQ54.2
056100              GO TO SEQ-PASS-004.                                 OBSQ54.2
056200     MOVE "ERRORS IN READING SQ-FS7" TO RE-MARK.                  OBSQ54.2
056300 SEQ-FAIL-004.                                                    OBSQ54.2
056400     MOVE "RECORDS IN ERROR" TO COMPUTED-A.                       OBSQ54.2
056500     MOVE RECORDS-IN-ERROR TO CORRECT-18V0.                       OBSQ54.2
056600     PERFORM FAIL.                                                OBSQ54.2
056700     GO TO SEQ-WRITE-004.                                         OBSQ54.2
056800 SEQ-PASS-004.                                                    OBSQ54.2
056900     PERFORM PASS.                                                OBSQ54.2
057000     MOVE "FILE VERIFIED RECS =" TO COMPUTED-A.                   OBSQ54.2
057100     MOVE REC-COUNT    TO CORRECT-18V0.                           OBSQ54.2
057200 SEQ-WRITE-004.                                                   OBSQ54.2
057300     MOVE "SEQ-TEST-004" TO PAR-NAME.                             OBSQ54.2
057400     MOVE "VERIFY FILE SQ-FS7" TO FEATURE.                        OBSQ54.2
057500     PERFORM PRINT-DETAIL.                                        OBSQ54.2
057600 SEQ-CLOSE-004.                                                   OBSQ54.2
057700     CLOSE SQ-FS7 WITH NO REWIND.                                 OBSQ54.2
057800 SEQ-INIT-005.                                                    OBSQ54.2
057900*             THIS TEST READS AND VALIDATES FILE SQ-FS8.          OBSQ54.2
058000     MOVE 0 TO REC-COUNT,    RECORDS-IN-ERROR.                    OBSQ54.2
058100     OPEN INPUT SQ-FS8 WITH NO REWIND.                            OBSQ54.2
058200 SEQ-TEST-005.                                                    OBSQ54.2
058300     READ SQ-FS8 AT END GO TO SEQ-TEST-005-01.                    OBSQ54.2
058400     MOVE SQ-FS8R1-F-G-120 TO FILE-RECORD-INFO-P1-120 (1).        OBSQ54.2
058500     ADD 1 TO REC-COUNT.                                          OBSQ54.2
058600     IF REC-COUNT    GREATER THAN 750                             OBSQ54.2
058700              MOVE "MORE THAN 750 RECORDS" TO RE-MARK             OBSQ54.2
058800              GO TO SEQ-FAIL-005.                                 OBSQ54.2
058900     IF REC-COUNT    NOT EQUAL TO XRECORD-NUMBER (1)              OBSQ54.2
059000              ADD 1 TO RECORDS-IN-ERROR                           OBSQ54.2
059100              GO TO SEQ-TEST-005.                                 OBSQ54.2
059200     IF XFILE-NAME (1) NOT EQUAL TO "SQ-FS8"                      OBSQ54.2
059300              ADD 1 TO RECORDS-IN-ERROR                           OBSQ54.2
059400              GO TO SEQ-TEST-005.                                 OBSQ54.2
059500     IF CHARS-OR-RECORDS (1) NOT EQUAL TO "CH"                    OBSQ54.2
059600              ADD 1 TO RECORDS-IN-ERROR                           OBSQ54.2
059700              GO TO SEQ-TEST-005.                                 OBSQ54.2
059800     IF XBLOCK-SIZE (1) NOT EQUAL TO 120                          OBSQ54.2
059900              ADD 1 TO RECORDS-IN-ERROR.                          OBSQ54.2
060000     GO TO SEQ-TEST-005.                                          OBSQ54.2
060100 SEQ-TEST-005-01.                                                 OBSQ54.2
060200     IF RECORDS-IN-ERROR EQUAL TO ZERO                            OBSQ54.2
060300              GO TO SEQ-PASS-005.                                 OBSQ54.2
060400     MOVE "ERRORS IN READING SQ-FS8" TO RE-MARK.                  OBSQ54.2
060500 SEQ-FAIL-005.                                                    OBSQ54.2
060600     MOVE "RECORDS IN ERROR" TO COMPUTED-A.                       OBSQ54.2
060700     MOVE RECORDS-IN-ERROR TO CORRECT-18V0.                       OBSQ54.2
060800     PERFORM FAIL.                                                OBSQ54.2
060900     GO TO SEQ-WRITE-005.                                         OBSQ54.2
061000 SEQ-PASS-005.                                                    OBSQ54.2
061100     PERFORM PASS.                                                OBSQ54.2
061200     MOVE "FILE VERIFIED RECS =" TO COMPUTED-A.                   OBSQ54.2
061300     MOVE REC-COUNT    TO CORRECT-18V0.                           OBSQ54.2
061400 SEQ-WRITE-005.                                                   OBSQ54.2
061500     MOVE "SEQ-TEST-005" TO PAR-NAME.                             OBSQ54.2
061600     MOVE "VERIFY FILE SQ-FS8" TO FEATURE.                        OBSQ54.2
061700     PERFORM PRINT-DETAIL.                                        OBSQ54.2
061800 SEQ-CLOSE-005.                                                   OBSQ54.2
061900     CLOSE SQ-FS8.                                                OBSQ54.2
062000 OBSQ5A-END-ROUTINE.                                              OBSQ54.2
062100     MOVE "END OF OBSQ5A VALIDATION TESTS" TO PRINT-REC.          OBSQ54.2
062200     WRITE PRINT-REC AFTER ADVANCING 1 LINE.                      OBSQ54.2
062300     GO TO CCVS-EXIT.                                             OBSQ54.2
062400 CCVS-EXIT SECTION.                                               OBSQ54.2
062500 CCVS-999999.                                                     OBSQ54.2
062600     GO TO CLOSE-FILES.                                           OBSQ54.2
