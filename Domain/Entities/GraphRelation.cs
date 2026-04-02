using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class GraphRelation
    {
        public Guid Id { get; set; }

        public int GraphEntityId { get; set; }
        public GraphEntity GraphEntity { get; set; }
        public double? Weight { get; set; }
        //Vertexes relations
        public required string A { get; set; }
        public required string B { get; set; }
    }
}
