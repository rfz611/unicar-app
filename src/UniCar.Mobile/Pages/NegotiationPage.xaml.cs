using UniCar.Mobile.Models;
using UniCar.Mobile.Services;
namespace UniCar.Mobile.Pages;
public partial class NegotiationPage:ContentPage
{
 private readonly NegotiationService _service;private readonly Ride? _ride;private readonly int _id;private NegotiationDto? _n;private bool _busy;private bool _closed;
 public NegotiationPage(Ride ride,NegotiationService service){InitializeComponent();_ride=ride;_service=service;SummaryLabel.Text=$"{ride.DriverName}\n{ride.Origin} → {ride.Destination}\n{ride.DepartureLabel}\n{ride.PriceLabel}";AmountEntry.Text=ride.ReferenceAmount?.ToString("0.00",System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));StatusLabel.Text="Envie uma contribuição por sua vaga. A reserva depende da aceitação.";}
 public NegotiationPage(int id,NegotiationService service){InitializeComponent();_id=id;_service=service;}
 protected override async void OnAppearing(){base.OnAppearing();_closed=false;if(_id>0||_n is not null)await Reload();}
 protected override void OnDisappearing(){base.OnDisappearing();_closed=true;}
 private void ShowError(string? text){ErrorLabel.Text=text;ErrorLabel.IsVisible=!string.IsNullOrWhiteSpace(text);}
 private void Render(NegotiationDto n){_n=n;SummaryLabel.Text=$"{n.CounterpartyName}\n{n.RideSummary.Route}\n{n.RideSummary.DateLabel}\nÚltima proposta: {TravelText.Money(n.LatestAmount)}";StatusLabel.Text=n.StatusLabel;BindableLayout.SetItemsSource(History,n.Proposals);AmountPanel.IsVisible=n.CanRespond||n.CanReopen;SubmitButton.Text=n.CanReopen?"Enviar nova proposta":"Enviar contraproposta";AcceptButton.IsVisible=DeclineButton.IsVisible=n.CanRespond;WithdrawButton.IsVisible=n.CanWithdraw;}
 private async Task Reload(){var r=await _service.GetAsync(_n?.Id??_id);if(_closed)return;if(r.Success&&r.Data is not null)Render(r.Data);else{Actions.IsEnabled=false;ShowError(r.Error);}}
 private async void OnRefresh(object? s,EventArgs e){if(_busy||(_id==0&&_n is null))return;_busy=true;Loading.IsRunning=true;try{await Reload();}finally{_busy=false;Loading.IsRunning=false;Actions.IsEnabled=true;}}
 private async Task Mutate(Func<Task<RideResult<NegotiationDto>>> operation)
 {
  if(_busy)return;_busy=true;Actions.IsEnabled=false;Loading.IsRunning=true;ShowError(null);
  try{var r=await operation();if(_closed)return;if(r.Success&&r.Data is not null)Render(r.Data);else{if(_n is not null)await Reload();ShowError(r.Error);}}
  finally{_busy=false;Actions.IsEnabled=true;Loading.IsRunning=false;}
 }
 private async void OnSubmit(object? s,EventArgs e){if(!MoneyInput.TryParse(AmountEntry.Text,out var amount)){ShowError("Informe um valor em reais com até duas casas decimais; zero é permitido.");return;}
  var prior=_n;
  if(prior is null){if(_ride is not null)await Mutate(()=>_service.SubmitAsync(_ride.Id,new(0,amount)));}
  else if(prior.CanReopen)await Mutate(()=>_service.SubmitAsync(prior.RideId,new(prior.Version,amount)));
  else await Mutate(()=>_service.CounterAsync(prior.Id,new(prior.Version,amount)));
 }
 private async void OnAccept(object? s,EventArgs e)
 {
  if(_busy||_n is null||!_n.CanRespond)return;_busy=true;Actions.IsEnabled=false;var n=_n;
  try{
   if(!await DisplayAlertAsync("Confirmar acordo",$"Aceitar {TravelText.Money(n.LatestAmount)} por uma vaga nesta carona com {n.CounterpartyName}?","Confirmar","Voltar"))return;
   Loading.IsRunning=true;var r=await _service.AcceptAsync(n.Id,n.Version);
   // Consulta a confirmação persistida inclusive após timeout ou resposta perdida.
   await Reload();if(_closed)return;
   if(_n?.Status=="Accepted"){ShowError(null);await DisplayAlertAsync("Reserva confirmada","O valor combinado e sua vaga foram salvos. Veja em Reservas ou Atividades.","OK");}
   else ShowError(r.Error??"Confira a última proposta antes de confirmar novamente.");
  }finally{_busy=false;Actions.IsEnabled=true;Loading.IsRunning=false;}
 }
 private async void OnDecline(object? s,EventArgs e){if(_n is { } n)await Mutate(()=>_service.DeclineAsync(n.Id,n.Version));}
 private async void OnWithdraw(object? s,EventArgs e){if(_n is { } n)await Mutate(()=>_service.WithdrawAsync(n.Id,n.Version));}
 private async void OnClose(object? s,EventArgs e){if(!_busy)await Navigation.PopModalAsync();}
 private async void OnActivities(object? s,EventArgs e){if(_busy)return;await Navigation.PopModalAsync();await Shell.Current.GoToAsync("//home/activities");}
 protected override bool OnBackButtonPressed()=>_busy||base.OnBackButtonPressed();
}
