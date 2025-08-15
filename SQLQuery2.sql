/* ========================================================================
   REBUILD DB + SEED WITH UNIQUE STUDENT NAMES
   ======================================================================== */

-- 0) DROP + CREATE DB
IF DB_ID('KinemathikaDb') IS NOT NULL
BEGIN
    ALTER DATABASE KinemathikaDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE KinemathikaDb;
END;
GO
CREATE DATABASE KinemathikaDb;
GO
USE KinemathikaDb;
GO

-- 1) Core schema --------------------------------------------------------------
CREATE TABLE dbo.Classrooms
(
  Id         INT IDENTITY(1,1) CONSTRAINT PK_Classrooms PRIMARY KEY,
  Name       NVARCHAR(128) NOT NULL UNIQUE,
  IsArchived BIT NOT NULL CONSTRAINT DF_Classrooms_IsArchived DEFAULT(0),
  CreatedAt  DATETIME2(0) NOT NULL CONSTRAINT DF_Classrooms_CreatedAt DEFAULT(SYSDATETIME())
);

CREATE TABLE dbo.Students
(
  Id         INT IDENTITY(1,1) CONSTRAINT PK_Students PRIMARY KEY,
  StudentId  NVARCHAR(64)  NOT NULL UNIQUE,
  Name       NVARCHAR(128) NOT NULL,
  Email      NVARCHAR(128) NOT NULL UNIQUE,
  CreatedAt  DATETIME2(0)  NOT NULL CONSTRAINT DF_Students_CreatedAt DEFAULT(SYSDATETIME())
);
ALTER TABLE dbo.Students ADD StudentDbId AS (Id) PERSISTED;

CREATE TABLE dbo.Enrollments
(
  ClassroomId INT NOT NULL,
  StudentDbId INT NOT NULL,
  EnrolledAt  DATETIME2(0) NOT NULL CONSTRAINT DF_Enrollments_EnrolledAt DEFAULT(SYSDATETIME()),
  CONSTRAINT PK_Enrollments PRIMARY KEY (ClassroomId, StudentDbId),
  CONSTRAINT FK_Enrollments_Classrooms FOREIGN KEY (ClassroomId) REFERENCES dbo.Classrooms(Id) ON DELETE CASCADE,
  CONSTRAINT FK_Enrollments_Students   FOREIGN KEY (StudentDbId) REFERENCES dbo.Students(Id)   ON DELETE CASCADE
);

CREATE INDEX IX_Classrooms_IsArchived ON dbo.Classrooms(IsArchived) INCLUDE(Name);
CREATE INDEX IX_Enrollments_Classroom ON dbo.Enrollments(ClassroomId);
CREATE INDEX IX_Enrollments_Student   ON dbo.Enrollments(StudentDbId);

-- 2) Analytics schema --------------------------------------------------------
CREATE TABLE dbo.AttemptRecords
(
  id                     INT IDENTITY(1,1) CONSTRAINT PK_AttemptRecords PRIMARY KEY,
  student_id             NVARCHAR(64)  NOT NULL,
  session_id             NVARCHAR(64)  NOT NULL,
  concept_id             NVARCHAR(64)  NOT NULL,
  worksheet_id           NVARCHAR(64)  NOT NULL,
  problem_id             NVARCHAR(64)  NOT NULL,
  started_at             DATETIME2(0)  NOT NULL,
  ended_at               DATETIME2(0)  NOT NULL,
  ended_status           NVARCHAR(16)  NOT NULL
     CONSTRAINT CK_AttemptRecords_Status CHECK (ended_status IN (N'correct',N'exit',N'timeout')),
  first_attempt_correct  BIT           NOT NULL,
  attempts_to_correct    INT           NOT NULL,
  level_attempt_accuracy FLOAT         NOT NULL,
  time_to_correct_ms     INT           NOT NULL,
  mastery_valid          BIT           NOT NULL
);
ALTER TABLE dbo.AttemptRecords ADD StudentId AS (student_id) PERSISTED;

CREATE INDEX IX_Attempts_Concept ON dbo.AttemptRecords(concept_id);
CREATE INDEX IX_Attempts_EndedAt ON dbo.AttemptRecords(ended_at DESC);
CREATE INDEX IX_Attempts_Student ON dbo.AttemptRecords(student_id);

-- 3) Seed classes ------------------------------------------------------------
INSERT dbo.Classrooms(Name, IsArchived) VALUES (N'Physics 1-A', 0), (N'Physics 1-B', 1);
DECLARE @ClassAId INT = (SELECT Id FROM dbo.Classrooms WHERE Name=N'Physics 1-A');
DECLARE @ClassBId INT = (SELECT Id FROM dbo.Classrooms WHERE Name=N'Physics 1-B');

-- 4) Student name pools ------------------------------------------------------
DECLARE @FirstNames TABLE (Name NVARCHAR(50));
INSERT @FirstNames VALUES
(N'Alexander'),(N'Isabella'),(N'Liam'),(N'Olivia'),(N'Ethan'),(N'Emma'),(N'Mason'),(N'Ava'),
(N'Noah'),(N'Sophia'),(N'Lucas'),(N'Mia'),(N'Logan'),(N'Amelia'),(N'James'),(N'Harper'),
(N'Benjamin'),(N'Evelyn'),(N'Jacob'),(N'Abigail'),(N'Daniel'),(N'Emily'),(N'Matthew'),(N'Scarlett'),
(N'Joseph'),(N'Elizabeth'),(N'Jack'),(N'Avery'),(N'Samuel'),(N'Sofia');

DECLARE @LastNames TABLE (Name NVARCHAR(50));
INSERT @LastNames VALUES
(N'Smith'),(N'Johnson'),(N'Williams'),(N'Brown'),(N'Jones'),(N'Miller'),(N'Davis'),(N'Garcia'),
(N'Rodriguez'),(N'Wilson'),(N'Martinez'),(N'Anderson'),(N'Taylor'),(N'Thomas'),(N'Hernandez'),
(N'Moore'),(N'Martin'),(N'Jackson'),(N'Thompson'),(N'White'),(N'Lopez'),(N'Lee'),(N'Gonzalez'),
(N'Harris'),(N'Clark'),(N'Lewis'),(N'Robinson'),(N'Walker'),(N'Perez'),(N'Hall');

-- 5) Insert Class A: 20 unique students --------------------------------------
DECLARE @i INT = 1;
WHILE (@i <= 20)
BEGIN
    DECLARE @fn NVARCHAR(50) = (SELECT TOP 1 Name FROM @FirstNames ORDER BY NEWID());
    DECLARE @ln NVARCHAR(50) = (SELECT TOP 1 Name FROM @LastNames ORDER BY NEWID());
    DECLARE @full NVARCHAR(128) = @fn + N' ' + @ln;
    DECLARE @sid NVARCHAR(64) = CONCAT('C1-', RIGHT('000' + CONVERT(VARCHAR(10),@i), 3));
    DECLARE @em NVARCHAR(128) = LOWER(@fn + '.' + @ln + CONVERT(VARCHAR(3),@i) + '@classA.local');

    INSERT dbo.Students(StudentId, Name, Email) VALUES (@sid, @full, @em);
    INSERT dbo.Enrollments(ClassroomId, StudentDbId) VALUES (@ClassAId, SCOPE_IDENTITY());
    SET @i += 1;
END

-- 6) Insert Class B: 23 unique students --------------------------------------
SET @i = 1;
WHILE (@i <= 23)
BEGIN
    DECLARE @fn2 NVARCHAR(50) = (SELECT TOP 1 Name FROM @FirstNames ORDER BY NEWID());
    DECLARE @ln2 NVARCHAR(50) = (SELECT TOP 1 Name FROM @LastNames ORDER BY NEWID());
    DECLARE @full2 NVARCHAR(128) = @fn2 + N' ' + @ln2;
    DECLARE @sid2 NVARCHAR(64) = CONCAT('C2-', RIGHT('000' + CONVERT(VARCHAR(10),@i), 3));
    DECLARE @em2 NVARCHAR(128) = LOWER(@fn2 + '.' + @ln2 + CONVERT(VARCHAR(3),@i) + '@classB.local');

    INSERT dbo.Students(StudentId, Name, Email) VALUES (@sid2, @full2, @em2);
    INSERT dbo.Enrollments(ClassroomId, StudentDbId) VALUES (@ClassBId, SCOPE_IDENTITY());
    SET @i += 1;
END

-- 7) Seed analytics ----------------------------------------------------------
DECLARE @now DATETIME2(0) = SYSDATETIME();
DECLARE @concepts TABLE (c NVARCHAR(64), ws NVARCHAR(64));
INSERT @concepts VALUES (N'Distance',N'W-001'),(N'Velocity',N'W-021'),
                        (N'Acceleration',N'W-030'),(N'Displacement',N'W-008');

DECLARE @studentCount INT = (SELECT COUNT(*) FROM dbo.Students);
DECLARE @row INT = 1;

WHILE (@row <= @studentCount)
BEGIN
    DECLARE @student NVARCHAR(64) = (SELECT StudentId FROM dbo.Students ORDER BY Id OFFSET (@row-1) ROWS FETCH NEXT 1 ROWS ONLY);
    DECLARE @s INT = 1;

    WHILE (@s <= 3)
    BEGIN
        DECLARE @c NVARCHAR(64) = (SELECT TOP 1 c FROM @concepts ORDER BY ABS(CHECKSUM(NEWID())));
        DECLARE @ws NVARCHAR(64) = (SELECT ws FROM @concepts WHERE c=@c);
        DECLARE @attempts INT = CASE WHEN @s % 5 = 0 THEN 1 ELSE ((@row+@s) % 3) + 1 END;
        DECLARE @status NVARCHAR(16) =
          CASE WHEN (@row+@s) % 10 = 0 THEN N'exit'
               WHEN (@row+@s) % 9  = 0 THEN N'timeout'
               ELSE N'correct' END;
        DECLARE @start DATETIME2(0) = DATEADD(MINUTE, - (60 - ((@row+@s) % 45)), @now);
        DECLARE @end   DATETIME2(0) = DATEADD(SECOND,  30 + ((@row*@s) % 120), @start);
        DECLARE @ms    INT = DATEDIFF(MILLISECOND, @start, @end);
        DECLARE @acc   FLOAT = 1.0 / CONVERT(FLOAT, @attempts);

        INSERT dbo.AttemptRecords
        ( student_id, session_id, concept_id, worksheet_id, problem_id,
          started_at, ended_at, ended_status,
          first_attempt_correct, attempts_to_correct, level_attempt_accuracy, time_to_correct_ms, mastery_valid )
        VALUES
        ( @student,
          CONCAT(N'S', ((@row+@s) % 3) + 1),
          @c, @ws,
          CONCAT(@c, N'-P', RIGHT('000' + CONVERT(VARCHAR(10), (@row*10+@s)), 3)),
          @start, @end, @status,
          CASE WHEN @attempts = 1 THEN 1 ELSE 0 END,
          @attempts, @acc, @ms,
          CASE WHEN @status = N'correct' THEN 1 ELSE 0 END );

        SET @s += 1;
    END
    SET @row += 1;
END

-- 8) Checks ------------------------------------------------------------------
SELECT c.Name, c.IsArchived, COUNT(e.StudentDbId) AS StudentCount
FROM dbo.Classrooms c
LEFT JOIN dbo.Enrollments e ON e.ClassroomId = c.Id
GROUP BY c.Name, c.IsArchived
ORDER BY c.Name;


