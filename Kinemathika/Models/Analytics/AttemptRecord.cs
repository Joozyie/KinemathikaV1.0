// WHAT IT DOES: EF entity mapping 1:1 to dbo.AttemptRecords (new schema).
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using Kinemathika.Models; // for Classroom/Student navs

/* 
    SUBJECT FOR DELETION
    DOUBLE CHECK MATTTTTTTTTTTTTTTTTTT
*/

namespace Kinemathika.Models.Analytics
{
    [Table("AttemptRecords")]
    public class AttemptRecord
    {
        [Key]
        [Column("attempt_id")]
        public long AttemptId { get; set; } // BIGINT IDENTITY

        [Column("class_id")]
        public int ClassId { get; set; }

        [Column("student_id")]
        [MaxLength(20)]
        public string StudentId { get; set; } = string.Empty;

        [Column("concept_id")]
        [MaxLength(10)]
        [DefaultValue("")]
        public string ConceptId { get; set; } = string.Empty; // 'dd'|'sv'|'acc'

        [Column("attempts_to_correct")]
        public int AttemptsToCorrect { get; set; }

        [Column("time_to_correct_ms")]
        public int TimeToCorrectMs { get; set; }

        [Column("ended_at")]
        public DateTime EndedAt { get; set; }

        [Column("problem_no")]
        public byte ProblemNo { get; set; } // 1..15

        // --- Navigation properties ---
        public Classroom Class { get; set; } = default!;
        public Student Student { get; set; } = default!;
    }
}
