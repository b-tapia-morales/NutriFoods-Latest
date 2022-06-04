﻿using System;
using System.Collections.Generic;

namespace Domain.Models
{
    public partial class NutrientType
    {
        public NutrientType()
        {
            NutrientSubtypes = new HashSet<NutrientSubtype>();
        }

        public int Id { get; set; }
        public string Name { get; set; } = null!;

        public virtual ICollection<NutrientSubtype> NutrientSubtypes { get; set; }
    }
}