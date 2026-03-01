using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    /// <summary> Just in case of future need for common properties or methods for all entities. </summary>
    public class EntityBase
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
