using AutoMapper;
using QuanApi.Data;
using QuanApi.Dtos;

namespace QuanApi.MappingProfiles
{
    public class DiaChiMappingProfile : Profile
    {
        public DiaChiMappingProfile()
        {
            CreateMap<DiaChi, DiaChiDto>();

            CreateMap<CreateDiaChiDto, DiaChi>()
                .ForMember(dest => dest.IDDiaChi, opt => opt.Ignore())
                .ForMember(dest => dest.NgayTao, opt => opt.Ignore())
                .ForMember(dest => dest.NguoiTao, opt => opt.Ignore())
                .ForMember(dest => dest.LanCapNhatCuoi, opt => opt.Ignore())
                .ForMember(dest => dest.NguoiCapNhat, opt => opt.Ignore());

            CreateMap<UpdateDiaChiDto, DiaChi>()
                .ForMember(dest => dest.NgayTao, opt => opt.Ignore())
                .ForMember(dest => dest.NguoiTao, opt => opt.Ignore())
                .ForMember(dest => dest.LanCapNhatCuoi, opt => opt.Ignore())
                .ForMember(dest => dest.NguoiCapNhat, opt => opt.Ignore());
        }
    }
}