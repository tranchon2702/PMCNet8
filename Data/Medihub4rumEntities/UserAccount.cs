using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Medihub4rumEntities
{
    public class UserAccount
    {
        [Key]
        public Guid Id { get; set; }
        public string UserName { get; set; }    
        public string Password { get; set; }
    }
}
