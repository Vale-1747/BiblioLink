using BiblioLink.DTOs;

namespace BiblioLink.Interfaces;

public interface IUsuarioService
{
    Task<IEnumerable<UsuarioReadDTO>> GetAllAsync();
    Task<UsuarioReadDTO?> GetByIdAsync(int id);
    Task<UsuarioReadDTO> CreateAsync(UsuarioCreateDTO usuarioDto);
    Task<UsuarioReadDTO?> UpdateAsync(int id, UsuarioUpdateDTO usuarioDto);
    Task<bool> DeleteAsync(int id);
}