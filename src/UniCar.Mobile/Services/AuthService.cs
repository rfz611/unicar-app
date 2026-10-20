using System.Net;
using System.Net.Http.Json;
using UniCar.Mobile.Models;

namespace UniCar.Mobile.Services;

public sealed record ApiResult<T>(bool Success, T? Data, string? Error)
{
    public static ApiResult<T> Ok(T data) => new(true, data, null);
    public static ApiResult<T> Fail(string error) => new(false, default, error);
}

public class AuthService
{
    private readonly HttpClient _http;

    public AuthService()
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(AppConfig.ApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public async Task<ApiResult<AuthResponse>> LoginAsync(string email, string password)
        => RequireToken(await PostAsync<LoginRequest, AuthResponse>("api/Auth/login",
            new LoginRequest(email.Trim().ToLowerInvariant(), password),
            status => status == HttpStatusCode.Unauthorized
                ? "E-mail ou senha incorretos."
                : null));

    public async Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest request)
        => RequireToken(await PostAsync<RegisterRequest, AuthResponse>("api/Auth/register",
            request with { Email = request.Email.Trim().ToLowerInvariant() },
            status => status == HttpStatusCode.Conflict
                ? "Este e-mail, RA ou CNH já está cadastrado."
                : null));

    public Task<ApiResult<MessageResponse>> ForgotPasswordAsync(string email)
        => PostAsync<ForgotPasswordRequest, MessageResponse>("api/Auth/forgot-password",
            new ForgotPasswordRequest(email.Trim().ToLowerInvariant()),
            _ => null);

    public Task<ApiResult<MessageResponse>> ResetPasswordAsync(string email, string code, string newPassword)
        => PostAsync<ResetPasswordRequest, MessageResponse>("api/Auth/reset-password",
            new ResetPasswordRequest(email.Trim().ToLowerInvariant(), code.Trim(), newPassword),
            status => status == HttpStatusCode.BadRequest
                ? "Código inválido ou expirado. Peça um novo código."
                : null);

    private static ApiResult<AuthResponse> RequireToken(ApiResult<AuthResponse> result)
        => result.Success && string.IsNullOrEmpty(result.Data?.Token)
            ? ApiResult<AuthResponse>.Fail("Resposta inesperada do servidor.")
            : result;

    // As mensagens são definidas aqui (e não lidas da API) para o app sempre exibir texto legível.
    private async Task<ApiResult<TResponse>> PostAsync<TBody, TResponse>(
        string url, TBody body, Func<HttpStatusCode, string?> mapError) where TResponse : class
    {
        try
        {
            using var response = await _http.PostAsJsonAsync(url, body);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<TResponse>();
                return data is null
                    ? ApiResult<TResponse>.Fail("Resposta inesperada do servidor.")
                    : ApiResult<TResponse>.Ok(data);
            }

            var message = mapError(response.StatusCode)
                ?? (response.StatusCode == HttpStatusCode.BadRequest
                    ? await ReadApiMessageAsync(response) ?? "Verifique os dados informados e tente novamente."
                    : "Algo deu errado. Tente novamente em instantes.");
            return ApiResult<TResponse>.Fail(message);
        }
        catch (TaskCanceledException)
        {
            return ApiResult<TResponse>.Fail("O servidor demorou para responder. Tente novamente.");
        }
        catch (HttpRequestException)
        {
            return ApiResult<TResponse>.Fail(
                "Não foi possível conectar ao servidor. Confira se a API está rodando e sua conexão.");
        }
    }

    // Em erros 400 de regra de negócio (ex.: CNH vencida) a API devolve { "message": "..." } em português.
    private static async Task<string?> ReadApiMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<MessageResponse>();
            return string.IsNullOrWhiteSpace(body?.Message) ? null : body.Message;
        }
        catch
        {
            return null; // 400 de validação automática do ASP.NET (ProblemDetails): usa o texto padrão
        }
    }
}
