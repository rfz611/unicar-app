using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using UniCar.Mobile.Models;
namespace UniCar.Mobile.Services;
public sealed class TravelApiClient(SessionService session)
{
 private readonly HttpClient _http=new(){BaseAddress=new Uri(AppConfig.ApiBaseUrl),Timeout=TimeSpan.FromSeconds(20)};
 public async Task<RideResult<T>> SendAsync<T>(HttpMethod method,string path,object? body=null,CancellationToken ct=default)
 {
  var token=await session.GetTokenAsync();
  if(string.IsNullOrEmpty(token))return new(false,default,"Sua sessão expirou. Entre novamente.");
  try {
   using var request=new HttpRequestMessage(method,path);request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);
   if(body is not null)request.Content=JsonContent.Create(body);
   using var response=await _http.SendAsync(request,ct);
   if(token!=await session.GetTokenAsync())return new(false,default,"A sessão foi alterada.");
   if(response.IsSuccessStatusCode){var value=await response.Content.ReadFromJsonAsync<T>(cancellationToken:ct);return value is null?new(false,default,"Resposta inesperada."):new(true,value,null);}
   if((int)response.StatusCode==401)return new(false,default,"Sua sessão expirou. Entre novamente.");
   var text=await response.Content.ReadAsStringAsync(ct);
   try {var message=JsonSerializer.Deserialize<MessageResponse>(text,new JsonSerializerOptions(JsonSerializerDefaults.Web));if(!string.IsNullOrWhiteSpace(message?.Message))return new(false,default,message.Message);}catch(JsonException){}
   return new(false,default,"Não foi possível concluir. Atualize os dados e tente novamente.");
  }catch(TaskCanceledException){return new(false,default,"O servidor demorou para responder. Atualize antes de tentar novamente.");}
  catch(HttpRequestException){return new(false,default,"Não foi possível conectar ao servidor.");}
  catch(JsonException){return new(false,default,"Resposta inesperada do servidor.");}
 }
}
