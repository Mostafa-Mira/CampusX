using AutoMapper;
using CampusX.Models;
using CampusX.ViewModel;
using CampusX.ViewModels;

namespace CampusX.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // 1. Add/Edit mappings
            CreateMap<AddProductViewModel, Product>()
                .ForMember(dest => dest.Category, opt => opt.Ignore());

            CreateMap<Product, AddProductViewModel>();

            CreateMap<Product, EditProductViewModel>().ReverseMap();

            // 2. Display & Details mappings (الخطوة الجديدة)
            // تحويل المنتج إلى بطاقة عرض في الصفحة الرئيسية
            CreateMap<Product, ProductCardViewModel>();

            // تحويل المنتج إلى نموذج تفاصيل المنتج
            CreateMap<Product, ProductDetailsViewModel>();
        }
    }
}