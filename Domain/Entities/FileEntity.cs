using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class FileEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }

        public GraphEntity? Graph { get; set; }
        public Guid? GraphId { get; set; }
    }
}
