using System.Text.Json;
using UniCar.Mobile.Models;
using UniCar.Mobile.Services;
namespace UniCar.Mobile.Pages;
public partial class RoutePickerPage:ContentPage
{
 private readonly RouteQuoteService _routes;private readonly string _vehicle;private readonly string _nonce=Guid.NewGuid().ToString("N");
 private readonly Uri _page=new(new Uri(AppConfig.ApiBaseUrl),"map/index.html");
 private readonly TaskCompletionSource<RouteQuoteDto?> _completion=new(TaskCreationOptions.RunContinuationsAsynchronously);
 private bool _trusted;private int _latest;private bool _closed;private RouteQuoteDto? _quote;
 public RoutePickerPage(string vehicle,RouteQuoteService routes,string focus="origin"){InitializeComponent();_routes=routes;_vehicle=vehicle;MapWeb.Source=new UrlWebViewSource{Url=_page+"?nonce="+_nonce+"&vehicle="+vehicle+"&focus="+(focus=="destination"?"destination":"origin")};}
 public async Task<RouteQuoteDto?> PickAsync(Page owner){await owner.Navigation.PushModalAsync(this);return await _completion.Task;}
 private bool Trusted(string url)=>Uri.TryCreate(url,UriKind.Absolute,out var uri)&&uri.Scheme==_page.Scheme&&uri.Authority==_page.Authority&&uri.AbsolutePath==_page.AbsolutePath;
 private void OnNavigated(object? sender,WebNavigatedEventArgs e){_trusted=Trusted(e.Url)&&e.Result==WebNavigationResult.Success;if(!_trusted)RouteLabel.Text="Não foi possível abrir o mapa. Confira a conexão com a API.";}
 private async void OnNavigating(object? sender,WebNavigatingEventArgs e)
 {
  if(!e.Url.StartsWith("unicar-map:",StringComparison.OrdinalIgnoreCase)){if(!Trusted(e.Url))e.Cancel=true;else _trusted=false;return;}
  e.Cancel=true;
  if(!_trusted||_closed||!RouteSelectionGuard.TryRead(e.Url,_nonce,out var message)||message is null||message.RequestId<_latest)return;
  _latest=message.RequestId;_quote=null;ConfirmButton.IsEnabled=false;
  if(message.Action=="invalidate"){RouteLabel.Text="Calcule novamente para confirmar o percurso.";return;}
  if(message.Action=="location"){
   try {var permission=await Permissions.RequestAsync<Permissions.LocationWhenInUse>();if(permission!=PermissionStatus.Granted){RouteLabel.Text="Localização não autorizada. Pesquise ou marque os pontos no mapa.";return;}var location=await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium,TimeSpan.FromSeconds(8)));if(location is not null&&!_closed)await MapWeb.EvaluateJavaScriptAsync($"applyCurrentLocation({JsonSerializer.Serialize(location.Latitude)},{JsonSerializer.Serialize(location.Longitude)},{message.RequestId})");}
   catch(Exception ex)when(ex is FeatureNotSupportedException or FeatureNotEnabledException or PermissionException or TaskCanceledException){RouteLabel.Text="Localização indisponível. Selecione os pontos no mapa.";}
   return;
  }
  if(message.Request is null||message.Request.VehicleType!=_vehicle)return;
  var requestId=message.RequestId;RouteLabel.Text="Calculando percurso…";
  var result=await _routes.CreateAsync(message.Request);
  if(_closed||requestId!=_latest)return;
  if(!result.Success||result.Data is null){RouteLabel.Text=result.Error;await MapWeb.EvaluateJavaScriptAsync($"showRouteState({JsonSerializer.Serialize(result.Error)},{requestId})");return;}
  _quote=result.Data;RouteLabel.Text=$"{_quote.DistanceMeters/1000.0:0.0} km · cerca de {Math.Ceiling(_quote.DurationSeconds/60.0)} min · estimativa Google Maps";
  await MapWeb.EvaluateJavaScriptAsync($"renderRoute({JsonSerializer.Serialize(_quote.EncodedPolyline)},{requestId})");
  if(!_closed&&requestId==_latest)ConfirmButton.IsEnabled=true;
 }
 private async void OnConfirm(object? sender,EventArgs e){if(_quote is null||_quote.ExpiresAt<=DateTimeOffset.UtcNow){RouteLabel.Text="O percurso expirou. Calcule novamente.";ConfirmButton.IsEnabled=false;return;}_completion.TrySetResult(_quote);await Navigation.PopModalAsync();}
 private async void OnCancel(object? sender,EventArgs e){_completion.TrySetResult(null);await Navigation.PopModalAsync();}
 protected override void OnDisappearing(){base.OnDisappearing();_closed=true;_completion.TrySetResult(null);}
}
