using AutoMapper;
using QuanApi.Data;
using QuanApi.Dtos;

namespace QuanApi.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<CreatePhieuGiamGiaDto, PhieuGiamGia>()
                .ForMember(dest => dest.IDPhieuGiamGia, opt => opt.Ignore())
                .ForMember(dest => dest.LanCapNhatCuoi, opt => opt.Ignore())
                .ForMember(dest => dest.NguoiCapNhat, opt => opt.Ignore())
                .ForMember(dest => dest.KhachHangPhieuGiams, opt => opt.Ignore())
                .ForMember(dest => dest.HoaDons, opt => opt.Ignore());
            CreateMap<PhieuGiamGia, PhieuGiamGiaDto>()
                .ForMember(dest => dest.IDKhachHang, opt => opt.Ignore());
        }
    }
}
