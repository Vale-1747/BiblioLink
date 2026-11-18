using BiblioLink.DTOs;

namespace BiblioLink.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDTO> RegisterAsync(RegistroCuentaDTO dto);
    Task<AuthResponseDTO?> LoginAsync(LoginDTO dto);
}
