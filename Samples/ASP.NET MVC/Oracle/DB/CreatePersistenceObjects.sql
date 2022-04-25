/*
Company: OptimaJet
Project: WorkflowEngine.NET Provider for Oracle
Version: 5.3
File: CreatePersistenceObjects.sql
*/

CREATE TABLE WORKFLOWINBOX (
  ID RAW(16),
  PROCESSID RAW(16) NOT NULL,
  IDENTITYID NVARCHAR2(256),
  ADDINGDATE TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
  AVAILABLECOMMANDS NVARCHAR2(1024) DEFAULT '' NULL,
  CONSTRAINT PK_WORKFLOWINBOX PRIMARY KEY (ID) USING INDEX STORAGE ( INITIAL 64K NEXT 1M MAXEXTENTS UNLIMITED ))
LOGGING;

CREATE INDEX IDX_WORKFLOWINBOX_IDENTITYID ON WORKFLOWINBOX (IDENTITYID)
LOGGING;

CREATE INDEX IDX_WORKFLOWINBOX_PROCESSID ON WORKFLOWINBOX (PROCESSID)
LOGGING;

CREATE TABLE WORKFLOWPROCESSINSTANCE (
  ID RAW(16),
  STATENAME NVARCHAR2(1024),
  ACTIVITYNAME NVARCHAR2(1024) NOT NULL,
  SCHEMEID RAW(16),
  PREVIOUSSTATE NVARCHAR2(1024),
  PREVIOUSSTATEFORDIRECT NVARCHAR2(1024),
  PREVIOUSSTATEFORREVERSE NVARCHAR2(1024),
  PREVIOUSACTIVITY NVARCHAR2(1024),
  PREVIOUSACTIVITYFORDIRECT NVARCHAR2(1024),
  PREVIOUSACTIVITYFORREVERSE NVARCHAR2(1024),
  ISDETERMININGPARAMETERSCHANGED CHAR(1 BYTE) DEFAULT 0,
  PARENTPROCESSID RAW(16) NULL,
  ROOTPROCESSID RAW(16) NOT NULL,
  TENANTID NVARCHAR2(1024),
  STARTINGTRANSITION CLOB NULL,
  SUBPROCESSNAME CLOB NULL,
  CREATIONDATE TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
  LASTTRANSITIONDATE TIMESTAMP NULL,
  CONSTRAINT PK_WORKFLOWPROCESSINSTANCE PRIMARY KEY (ID) USING INDEX STORAGE ( INITIAL 64K NEXT 1M MAXEXTENTS UNLIMITED ))
LOGGING;

CREATE INDEX IDX_WORKFLOWPROCESSINSTANCE_ROOT ON WORKFLOWPROCESSINSTANCE (ROOTPROCESSID)
LOGGING;

CREATE TABLE WORKFLOWPROCESSSCHEME (
  ID RAW(16),
  SCHEME CLOB NOT NULL,
  DEFININGPARAMETERS CLOB NOT NULL,
  DEFININGPARAMETERSHASH NVARCHAR2(24) NOT NULL,
  SCHEMECODE NVARCHAR2(256) NOT NULL,
  ISOBSOLETE CHAR(1 BYTE) DEFAULT 0 NOT NULL,
  ROOTSCHEMECODE NVARCHAR2(256) NULL,
  ROOTSCHEMEID RAW(16) NULL,
  ALLOWEDACTIVITIES CLOB NULL,
  STARTINGTRANSITION CLOB NULL,
  CONSTRAINT PK_WORKFLOWPROCESSSCHEME PRIMARY KEY (ID) USING INDEX STORAGE ( INITIAL 64K NEXT 1M MAXEXTENTS UNLIMITED ))
LOGGING;

CREATE INDEX IDX_WORKFLOWPROCESSSCHEME_SCHE ON WORKFLOWPROCESSSCHEME (SCHEMECODE,DEFININGPARAMETERSHASH,ISOBSOLETE)
LOGGING;

CREATE TABLE WORKFLOWPROCESSTIMER (
  ID RAW(16),
  PROCESSID RAW(16) NOT NULL,
  ROOTPROCESSID RAW(16) NOT NULL,
  NAME VARCHAR2(256 BYTE) NOT NULL,
  NEXTEXECUTIONDATETIME TIMESTAMP NOT NULL,
  IGNORE CHAR(1 BYTE) NOT NULL,
  CONSTRAINT PK_WORKFLOWPROCESSTIMER PRIMARY KEY (ID) USING INDEX STORAGE ( INITIAL 64K NEXT 1M MAXEXTENTS UNLIMITED ))
LOGGING;

CREATE INDEX IDX_WORKFLOWPROCESSTIMER_DATE ON WORKFLOWPROCESSTIMER (NEXTEXECUTIONDATETIME)
LOGGING;

CREATE INDEX IX_PROCESSID ON WORKFLOWPROCESSTIMER (PROCESSID)
LOGGING;

CREATE TABLE WORKFLOWPROCESSASSIGNMENT (
    ID RAW(16),
    ASSIGNMENTCODE VARCHAR2(256 BYTE) NOT NULL,
    PROCESSID RAW(16) NOT NULL,
    NAME VARCHAR2(256 BYTE) NOT NULL,
    DESCRIPTION VARCHAR2(256 BYTE),
    STATUSSTATE VARCHAR2(256 BYTE) NOT NULL,
    DATECREATION TIMESTAMP NOT NULL,
    DATESTART TIMESTAMP,
    DATEFINISH TIMESTAMP,
    DEADLINETOSTART TIMESTAMP,
    DEADLINETOCOMPLETE TIMESTAMP,
    EXECUTOR VARCHAR2(256 BYTE) NOT NULL,
    OBSERVERS VARCHAR2(256 BYTE),
    TAGS VARCHAR2(256 BYTE),
    ISACTIVE CHAR(1 BYTE) DEFAULT 0 NOT NULL,
    ISDELETED CHAR(1 BYTE) DEFAULT 0 NOT NULL,
    CONSTRAINT PK_WORKFLOWPROCESSASSIGNMENT PRIMARY KEY (ID) USING INDEX STORAGE ( INITIAL 64K NEXT 1M MAXEXTENTS UNLIMITED ))
LOGGING;

CREATE INDEX IDX_WORKFLOWPROCESSASSIGNMENT_PROCESSID ON WORKFLOWPROCESSASSIGNMENT (PROCESSID)
LOGGING;
CREATE INDEX IDX_WORKFLOWPROCESSASSIGNMENT_ASSIGNMENTCODE ON WORKFLOWPROCESSASSIGNMENT (ASSIGNMENTCODE)
LOGGING;
CREATE INDEX IDX_WORKFLOWPROCESSASSIGNMENT_EXECUTOR ON WORKFLOWPROCESSASSIGNMENT (EXECUTOR)
LOGGING;
CREATE INDEX IDX_WORKFLOWPROCESSASSIGNMENT_PE ON WORKFLOWPROCESSASSIGNMENT (PROCESSID, EXECUTOR)
LOGGING;

CREATE TABLE WORKFLOWPROCESSTRANSITIONH (
  ID RAW(16),
  PROCESSID RAW(16) NOT NULL,
  EXECUTORIDENTITYID NVARCHAR2(256),
  ACTORIDENTITYID NVARCHAR2(256),
  EXECUTORNAME NVARCHAR2(256),
  ACTORNAME NVARCHAR2(256),
  FROMACTIVITYNAME NVARCHAR2(256) NULL,
  TOACTIVITYNAME NVARCHAR2(256) NOT NULL,
  TOSTATENAME NVARCHAR2(256),
  TRANSITIONTIME DATE NOT NULL,
  TRANSITIONCLASSIFIER NVARCHAR2(256) NOT NULL,
  ISFINALISED CHAR(1 BYTE) NOT NULL,
  FROMSTATENAME NVARCHAR2(256),
  TRIGGERNAME NVARCHAR2(256),
  STARTTRANSITIONTIME TIMESTAMP,
  TRANSITIONDURATION Number(10,0),
  CONSTRAINT PK_WORKFLOWPROCESSTRANSITIONH PRIMARY KEY (ID) USING INDEX STORAGE ( INITIAL 64K NEXT 1M MAXEXTENTS UNLIMITED ))
LOGGING;

CREATE INDEX IDX_WORKFLOWPROCESSTRANSITIONH ON WORKFLOWPROCESSTRANSITIONH (PROCESSID)
LOGGING;

CREATE INDEX IDX_WORKFLOWPROCESSTRANSITIONH_EX ON WORKFLOWPROCESSTRANSITIONH (EXECUTORIDENTITYID)
LOGGING;

CREATE TABLE WORKFLOWPROCESSINSTANCEP (
  ID RAW(16),
  PROCESSID RAW(16) NOT NULL,
  PARAMETERNAME NVARCHAR2(256) NOT NULL,
  VALUE NCLOB NOT NULL,
  CONSTRAINT PK_WORKFLOWPROCESSINSTANCEP PRIMARY KEY (ID) USING INDEX STORAGE ( INITIAL 64K NEXT 1M MAXEXTENTS UNLIMITED ))
LOGGING;

CREATE INDEX IDX_WORKFLOWPROCESSINSTANCEP_P ON WORKFLOWPROCESSINSTANCEP (PROCESSID)
LOGGING;

CREATE TABLE WORKFLOWSCHEME (
  CODE NVARCHAR2(256),
  SCHEME CLOB NOT NULL,
  CANBEINLINED CHAR(1 BYTE) DEFAULT 0,
  INLINEDSCHEMES NVARCHAR2(1024),
  TAGS NVARCHAR2(1024),
  CONSTRAINT PK_WORKFLOWSCHEME PRIMARY KEY (CODE) USING INDEX STORAGE ( INITIAL 64K NEXT 1M MAXEXTENTS UNLIMITED ))
LOGGING;

CREATE TABLE WORKFLOWPROCESSINSTANCES (
  ID RAW(16),
  STATUS NUMBER(3) NOT NULL,
  LOCKFLAG RAW(16) NOT NULL,
  RUNTIMEID NVARCHAR2(900) NOT NULL,
  SETTIME DATE NOT NULL,
  CONSTRAINT PK_WORKFLOWPROCESSINSTANCES PRIMARY KEY (ID) USING INDEX STORAGE ( INITIAL 64K NEXT 1M MAXEXTENTS UNLIMITED ))
LOGGING;

CREATE INDEX IDX_WORKFLOWPROCESSINSTANCES_S ON WORKFLOWPROCESSINSTANCES (STATUS)
LOGGING;

CREATE INDEX IDX_WORKFLOWPROCESSINSTANCES_SR ON WORKFLOWPROCESSINSTANCES (STATUS, RUNTIMEID)
LOGGING;


CREATE TABLE WORKFLOWGLOBALPARAMETER (
  ID RAW(16),
  TYPE NVARCHAR2(512)  NOT NULL,
  NAME NVARCHAR2(256)  NOT NULL,
  VALUE CLOB NOT NULL,
  CONSTRAINT PK_WORKFLOWGLOBALPARAMETER PRIMARY KEY (ID) USING INDEX STORAGE ( INITIAL 64K NEXT 1M MAXEXTENTS UNLIMITED ))
LOGGING;

CREATE INDEX IDX_WORKFLOWGLOBALPARAMETER_TY ON WORKFLOWGLOBALPARAMETER (TYPE,NAME)
LOGGING;

CREATE TABLE WORKFLOWRUNTIME (
  RUNTIMEID NVARCHAR2(900),
  STATUS NUMBER(3) NOT NULL,
  RESTORERID NVARCHAR2(900),
  LOCKFLAG RAW(16) NOT NULL,
  NEXTTIMERTIME TIMESTAMP NULL,
  NEXTSERVICETIMERTIME TIMESTAMP NULL,
  LASTALIVESIGNAL TIMESTAMP NULL,
  CONSTRAINT PK_WORKFLOWRUNTIME PRIMARY KEY (RUNTIMEID) USING INDEX STORAGE ( INITIAL 64K NEXT 1M MAXEXTENTS UNLIMITED ))
LOGGING;

CREATE TABLE WORKFLOWSYNC (
  NAME NVARCHAR2(900),
  LOCKFLAG RAW(16) NOT NULL,
  CONSTRAINT PK_WORKFLOWSYNC PRIMARY KEY (NAME) USING INDEX STORAGE ( INITIAL 64K NEXT 1M MAXEXTENTS UNLIMITED ))
LOGGING;

INSERT INTO WORKFLOWSYNC (Name, LOCKFLAG) VALUES ('Timer', sys_guid());
INSERT INTO WORKFLOWSYNC (Name, LOCKFLAG) VALUES ('ServiceTimer', sys_guid());

CREATE TABLE WORKFLOWAPPROVALHISTORY (
ID RAW(16),
PROCESSID RAW(16) NOT NULL,
IDENTITYID NVARCHAR2(256),
ALLOWEDTO CLOB NULL,
TRANSITIONTIME TIMESTAMP NULL,
SORT NUMBER(19) NULL,
INITIALSTATE NVARCHAR2(1024) NOT NULL,
DESTINATIONSTATE NVARCHAR2(1024) NOT NULL,
TRIGGERNAME NVARCHAR2(1024) NULL,
COMMENTARY CLOB NULL,
CONSTRAINT PK_WORKFLOWAPPROVALHISTORY PRIMARY KEY (ID) USING INDEX STORAGE ( INITIAL 64K NEXT 1M MAXEXTENTS UNLIMITED )
)
LOGGING;

CREATE INDEX IDX_WORKFLOWAPPROVALHISTORY_PID ON WORKFLOWAPPROVALHISTORY (PROCESSID)
LOGGING;

CREATE INDEX IDX_WORKFLOWAPPROVALHISTORY_IDENTITYID ON WORKFLOWAPPROVALHISTORY (IDENTITYID)
LOGGING;

CREATE OR REPLACE FUNCTION DropUnusedWorkflowProcessScheme
    RETURN NUMBER
    IS
    st NUMBER := 0;
BEGIN

    DELETE FROM WorkflowProcessScheme
    WHERE WorkflowProcessScheme.IsObsolete = 1
      AND NOT EXISTS (SELECT * FROM WorkflowProcessInstance  WHERE WorkflowProcessInstance.SchemeId = WorkflowProcessScheme.Id );

    SELECT COUNT(*) into st
    FROM WorkflowProcessInstance LEFT OUTER JOIN WorkflowProcessScheme ON WorkflowProcessInstance.SchemeId = WorkflowProcessScheme.Id
    WHERE WorkflowProcessScheme.Id IS NULL;

    RETURN st;
END;

COMMIT;