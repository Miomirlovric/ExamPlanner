using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class ExamEntity : EntityBase
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public ICollection<ExamQuestion> Questions { get; set; } = [];
    }
}
