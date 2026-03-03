using AutoMapper;
using QuanApi.Data;
using QuanApi.Dtos;

namespace QuanApi.MappingProfiles
{
    public class NhanVienProfile : Profile
    {
        public NhanVienProfile()
        {

            CreateMap<NhanVienCreateDto, NhanVien>()
                .ForMember(dest => dest.IDNhanVien, opt => opt.Ignore())
                .ForMember(dest => dest.NgayTao, opt => opt.Ignore())
                .ForMember(dest => dest.NguoiTao, opt => opt.Ignore())
                .ForMember(dest => dest.LanCapNhatCuoi, opt => opt.Ignore())
                .ForMember(dest => dest.NguoiCapNhat, opt => opt.Ignore())
                .ForMember(dest => dest.TrangThai, opt => opt.Ignore())
                .ForMember(dest => dest.NgaySinh, opt => opt.MapFrom(src =>
                    src.NgaySinh.HasValue && src.NgaySinh.Value.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(src.NgaySinh.Value, DateTimeKind.Utc)
                        : src.NgaySinh.HasValue && src.NgaySinh.Value.Kind == DateTimeKind.Local
                            ? src.NgaySinh.Value.ToUniversalTime()
                            : src.NgaySinh));


            CreateMap<NhanVienUpdateDto, NhanVien>()
                .ForMember(dest => dest.IDNhanVien, opt => opt.Ignore())
                .ForMember(dest => dest.NgayTao, opt => opt.Ignore())
                .ForMember(dest => dest.NguoiTao, opt => opt.Ignore())
                .ForMember(dest => dest.LanCapNhatCuoi, opt => opt.Ignore())
                .ForMember(dest => dest.NguoiCapNhat, opt => opt.Ignore());


            CreateMap<NhanVien, NhanVienResponseDto>()
                .ForMember(dest => dest.TenVaiTro, opt => opt.MapFrom(src => src.VaiTro.TenVaiTro));
        }
    }
}
