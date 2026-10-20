using UniCar.Mobile.Models;
using UniCar.Mobile.Services;
namespace UniCar.Mobile.Pages;
public partial class ActivitiesPage:ContentPage
{
 private readonly ActivityService _activities;
 private readonly NegotiationService _negotiations;
 private readonly SessionService _session;private readonly RideService _rides;private readonly RouteQuoteService _routes;
 private bool _busy;private bool _visible;private bool _reloadPending;private bool _proposals;private int _page;private int _generation;
 private readonly List<ActivityTripDto> _trips=[];private readonly List<NegotiationDto> _offers=[];
 public ActivitiesPage(ActivityService activities,NegotiationService negotiations,SessionService session,RideService rides,RouteQuoteService routes){InitializeComponent();_activities=activities;_negotiations=negotiations;_session=session;_rides=rides;_routes=routes;}
 protected override async void OnAppearing(){base.OnAppearing();_visible=true;await LoadAsync(true);}
 protected override void OnDisappearing(){base.OnDisappearing();_visible=false;_generation++;TripList.ItemsSource=null;ProposalList.ItemsSource=null;_trips.Clear();_offers.Clear();}
 private async void OnRefresh(object? s,EventArgs e)=>await LoadAsync(true);
 private async void OnMore(object? s,EventArgs e)=>await LoadAsync(false);
 private async void OnTab(object? s,EventArgs e){if(_busy||s is not Button {CommandParameter:string tab})return;_proposals=tab=="proposals";TripList.IsVisible=!_proposals;ProposalList.IsVisible=_proposals;TripsTab.Style=(Style)Application.Current!.Resources[_proposals?"OptionButton":"SelectedOptionButton"];ProposalsTab.Style=(Style)Application.Current.Resources[_proposals?"SelectedOptionButton":"OptionButton"];await LoadAsync(true);}
 private async Task LoadAsync(bool reset)
 {
  if(_busy){if(reset)_reloadPending=true;return;}_busy=true;var generation=_generation;Loading.IsRunning=true;MoreButton.IsVisible=false;StateLabel.IsVisible=false;
  if(reset){_page=0;_trips.Clear();_offers.Clear();TripList.ItemsSource=null;ProposalList.ItemsSource=null;}
  try{
   bool success;string? error;bool more;
   if(_proposals){var r=await _activities.GetProposalsAsync(_page+1);if(generation!=_generation)return;success=r.Success;error=r.Error;more=r.Data?.HasMore??false;if(r.Success){_offers.AddRange(r.Data!.Items);ProposalList.ItemsSource=_offers.ToList();}}
   else{var r=await _activities.GetTripsAsync(_page+1);if(generation!=_generation)return;success=r.Success;error=r.Error;more=r.Data?.HasMore??false;if(r.Success){_trips.AddRange(r.Data!.Items);TripList.ItemsSource=_trips.ToList();}}
   if(success)_page++;StateLabel.Text=success?(_proposals?"Você ainda não tem propostas.":"Seu histórico de caronas aparecerá aqui."):error;StateLabel.IsVisible=!success||(_proposals?_offers.Count==0:_trips.Count==0);MoreButton.IsVisible=more;
  }finally{_busy=false;Loading.IsRunning=false;if(_reloadPending&&_visible){_reloadPending=false;await LoadAsync(true);}}
 }
 private async void OnTrip(object? s,EventArgs e)
 {
  if(s is not Button {CommandParameter:ActivityTripDto trip})return;
  if(trip.Role!="Driver"||trip.RideSummary.DepartureTime<=DateTime.Now){await DisplayAlertAsync("Detalhes da carona",trip.Details,"OK");return;}
  var options=trip.RideSummary.PricingMode=="Negotiable"?new[]{"Ver detalhes","Editar contribuição de referência","Atualizar percurso e oferta"}:new[]{"Ver detalhes","Atualizar percurso e oferta"};
  var action=await DisplayActionSheetAsync("Sua carona","Fechar",null,options);
  if(action=="Ver detalhes")await DisplayAlertAsync("Detalhes da carona",trip.Details,"OK");
  else if(action=="Atualizar percurso e oferta")await Navigation.PushModalAsync(new OfferRidePage(_session,_rides,_routes,trip.RideSummary));
  else if(action=="Editar contribuição de referência"){
   var text=await DisplayPromptAsync("Contribuição de referência","Novo valor por passageiro (R$). Acordos aceitos serão preservados.","Salvar","Cancelar",keyboard:Keyboard.Numeric,initialValue:trip.RideSummary.ReferenceAmount?.ToString("0.00",System.Globalization.CultureInfo.GetCultureInfo("pt-BR")));
   if(text is null)return;
   if(!MoneyInput.TryParse(text,out var amount)){await DisplayAlertAsync("Valor inválido","Informe reais com até duas casas decimais.","OK");return;}
   var token=await _session.GetTokenAsync();if(string.IsNullOrEmpty(token))return;
   var result=await _rides.UpdateReferenceAsync(trip.RideSummary.Id,amount,token);
   await DisplayAlertAsync(result.Success?"Referência atualizada":"Não foi possível atualizar",result.Success?"O percurso e os valores combinados foram preservados.":result.Error??"Tente novamente.","OK");await LoadAsync(true);
  }
 }
 private async void OnProposal(object? s,EventArgs e){if(s is Button {CommandParameter:NegotiationDto n})await Navigation.PushModalAsync(new NegotiationPage(n.Id,_negotiations));}
}
