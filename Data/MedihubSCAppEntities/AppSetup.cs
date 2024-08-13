using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.MedihubSCAppEntities
{
    public class AppSetup
    {
        [Key]
        public long Id { get; set; }
        public string Imei { get; set; }
        public string DrugName { get; set; }
        public string? BankCode { get; set; }
        public string? BankName { get; set; }
        public string Address { get; set;}
        public string Status { get; set;}
        [AllowNull]
        public string? KeyCodeActive { get; set; }
        public DateTime CreateDate { get; set; }
        public string? Installer { get; set;}
        public string?PhoneNumber { get; set;}
        public string? OS { get; set;}
        public string? Token { get; set;}
        public string? SCName { get; set;}
        public string? MediHubCode { get; set;}
        public bool OutletOwner { get;set;}
        public string? ProvinceCode { get;set;}
        public string? DistrictCode { get;set;}
        public string? WardCode { get;set;}
        public string? StreetName { get;set;}
        public string? BankAccount { get;set;}
        public string? BankBranch { get;set;}
        public int PointsCMEOnline { get;set; }
        public string? MemberRank { get;set; }
        public string? Specialistly { get;set;}
        public string? Career { get;set;}
        public bool? isActiveEvent { get;set;}
        public string? DynamicLink { get;set;}
        public string? BannerLandingPage { get;set;}
        public string? GPPNumber { get;set;}
        public string? GPPImage { get;set;}
        public string? WorkPlace { get;set;}
        public bool? IsUpdateProfile { get;set;}
        public string? InviteCode { get;set;}
        public string? InvitePhone { get;set;}

    }
}
