using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class ExamEntity : EntityBase
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public ICollection<ExamSection> Sections { get; set; } = [];
    }
}
