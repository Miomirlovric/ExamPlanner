using Domain.Values;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class ExamSection : EntityBase
    {
        public Guid Id { get; set; }

        public ExamEntity ExamEntity { get; set; }
        public Guid ExamEntityId { get; set; }

        public GraphEntity GraphEntity { get; set; }
        public int GraphEntityId { get; set; }


        public string Title { get; set; }
        public string Question { get; set; }

        public QuestionTypeEnum QuestionTypeEnum { get; set; }
        
        public string? MoodleXML { get; set; }
        // Could be a req for many objects as answers so well serialize them instead
        public string? AnswerObject { get; set; }
    }
}
