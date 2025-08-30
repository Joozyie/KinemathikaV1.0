using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kinemathika.Data
{
    [Table("class", Schema = "organization")]
    public class SbClass
    {
        [Key]
        [Column("class_id")]
        public Guid ClassId { get; set; }

        [Column("school_id")]
        public Guid SchoolId { get; set; }

        [Column("teacher_in_charge_id")]
        public Guid TeacherInChargeId { get; set; }

        [Column("class_name")]
        public required string ClassName { get; set; }

        [Column("is_archived")]
        public bool IsArchived { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Navigation
        public required ICollection<SbStudent> Students { get; set; }
    }

    [Table("student", Schema = "accounts")]
    public class SbStudent
    {
        [Key]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("school_id")]
        public Guid SchoolId { get; set; }

        [Column("class_id")]
        public Guid ClassId { get; set; }

        [Column("student_number")]
        public required string StudentNumber { get; set; }

        [Column("teacher_in_charge_id")]
        public Guid TeacherInChargeId { get; set; }

        // Navigation
        public required SbClass Class { get; set; }
        public required ICollection<ProblemAttempt> ProblemAttempts { get; set; }
    }

    [Table("problemattempt", Schema = "analytics")]
    public class ProblemAttempt
    {
        [Key]
        [Column("session_id")]
        public Guid SessionId { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("problem_id")]
        public required string ProblemId { get; set; }

        [Column("concept_id")]
        public required string ConceptId { get; set; }

        [Column("worksheet_id")]
        public required string WorksheetId { get; set; }

        [Column("started_at")]
        public DateTime StartedAt { get; set; }

        [Column("ended_at")]
        public DateTime? EndedAt { get; set; }

        [Column("ended_status")]
        public required string EndedStatus { get; set; }

        [Column("first_attempt_correct")]
        public bool FirstAttemptCorrect { get; set; }

        [Column("attempts_to_correct")]
        public int AttemptsToCorrect { get; set; }

        [Column("time_to_correct_ms")]
        public int TimeToCorrectMs { get; set; }

        [Column("mastery_valid")]
        public bool MasteryValid { get; set; }

        [Column("teacher_in_charge_id")]
        public Guid TeacherInChargeId { get; set; }

        // Navigation
        public required SbStudent Student { get; set; }
    }
}
