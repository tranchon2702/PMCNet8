﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Medihub4rumEntities
{
    public class Sponsor
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public virtual SponsorHub SponsorHub { get; set; }

        // Thêm collection navigation property cho SponsorUser
        public virtual ICollection<SponsorUser> SponsorUsers { get; set; }
    }
}
