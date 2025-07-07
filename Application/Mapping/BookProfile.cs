using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mapping;

public class BookProfile : Profile
{
    public BookProfile()
    {
        // Book → BookDto
        CreateMap<Book, BookDto>();

        // BookDto → Book: solo campos no nulos o no vacíos
        CreateMap<BookDto, Book>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) =>
                srcMember switch
                {
                    string str => !string.IsNullOrWhiteSpace(str),
                    _ => srcMember != null
                }
            ));

        // Crear libro (petición de creación)
        CreateMap<CreateBookRequest, Book>();

        // Actualizar libro (solo campos que vienen no nulos)
        CreateMap<UpdateBookRequest, Book>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
