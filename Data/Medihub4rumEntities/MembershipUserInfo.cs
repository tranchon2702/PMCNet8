using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Medihub4rumEntities
{
    public class MembershipUserInfo
    {
        [Key]
        public Guid UserId  { get; set; }
        public string CCCD_So { get; set; }
        public DateTime CCCD_NgayCap { get; set; }
        public string CCCD_NoiCap { get; set; }
        public string CCCD_HinhMatTruoc { get; set; }
        public string CCCD_HinhMatSau {  get; set; }
        public string CCHN_So { get; set; }
        public string CCHN_Hinh {  get; set; }
        public string BCCM_Loai { get; set; }
        public string BCCM_Hinh { get; set; }
        public int TrangThai { get; set; }
        public string GhiChu { get; set; }
        public DateTime NgayGui {  get; set; }
        public DateTime NgayDuyet { get; set; }
        public MembershipUser MembershipUser { get; set; }
    }
}
