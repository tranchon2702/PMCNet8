﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Medihub4rumEntities
{
    public class SponsorUser
    {
       public Guid SponsorId { get; set; }
        public Guid UserId { get; set; }
    }
}
