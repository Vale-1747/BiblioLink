using BiblioLink.DTOs;

namespace BiblioLink.Interfaces;

public interface IApiExternaService
{
    Task<IEnumerable<ApiExternaReadDto>> GetTodosAsync();
    Task<ApiExternaReadDto> GetPorIdAsync(int id);
    Task<ApiExternaReadDto> CrearAsync(ApiExternaCreateDto dto);
    Task<ApiExternaReadDto> ActualizarAsync(int id, ApiExternaUpdateDto dto);
    Task EliminarAsync(int id);
}