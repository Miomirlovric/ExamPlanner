using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class GraphEntity
    {
        public int Id { get; set; }
        public bool IsDirected { get; set; } = false;
        public Guid FileId { get; set; }
        public FileEntity File { get; set; }

        public ICollection<GraphRelation> GraphRelations { get; set; } = [];
    }
}
