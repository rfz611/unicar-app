namespace UniCar.Api.Common;

/// <summary>Formato único de erro: { "code": "...", "message": "..." }. A mensagem já vem pronta para mostrar ao usuário.</summary>
public record ApiError(string Code, string Message);
