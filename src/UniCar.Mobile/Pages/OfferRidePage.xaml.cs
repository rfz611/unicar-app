using UniCar.Mobile.Models;
using UniCar.Mobile.Services;

namespace UniCar.Mobile.Pages;

public partial class OfferRidePage : ContentPage
{
    private readonly SessionService _session;
    private readonly RideService _rides;
    private bool _publishing;
    private readonly RouteQuoteService _routes;
    private RouteQuoteDto? _quote;
    private int _suggestionGeneration;
    private readonly PriceDraftGuard _priceDraft=new();
    private readonly int? _editId;

    public OfferRidePage(SessionService session, RideService rides, RouteQuoteService routes,RideSummaryDto? existing=null,string initialVehicle="CAR")
    {
        InitializeComponent();
        _session = session;
        _rides = rides;
        _routes = routes;
        OriginField.Configure(routes);DestinationField.Configure(routes);
        VehicleSection.IsVisible=existing is not null;
        VehicleSummaryLabel.IsVisible=existing is null;
        VehicleSummaryLabel.Text=initialVehicle=="MOTO"?"Carona de moto":"Carona de carro";
        VehiclePicker.SelectedIndex=initialVehicle=="MOTO"?1:0;
        DepartureDate.MinimumDate = DateTime.Today;
        var suggested = DateTime.Now.AddHours(1);
        DepartureDate.Date = suggested.Date;
        DepartureTime.Time = new TimeSpan(suggested.Hour, suggested.Minute, 0);
        if(existing is not null){_editId=existing.Id;Title="Atualizar carona";VehiclePicker.SelectedIndex=existing.VehicleType=="MOTO"?1:0;OriginField.Text=existing.Origin;DestinationField.Text=existing.Destination;DepartureDate.Date=existing.DepartureTime.Date;DepartureTime.Time=existing.DepartureTime.TimeOfDay;SeatsPicker.SelectedIndex=Math.Clamp(existing.OriginalSeats-1,0,3);PriceModePicker.SelectedIndex=existing.PricingMode=="Negotiable"?1:0;ReferenceEntry.Text=existing.ReferenceAmount?.ToString("0.00",System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));PublishButton.Text="Atualizar carona";}

    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        if (!_publishing) await Navigation.PopModalAsync();
    }

    protected override bool OnBackButtonPressed() => _publishing || base.OnBackButtonPressed();

    private async void OnPublishClicked(object? sender, EventArgs e)
    {
        if (_publishing) return;
        ErrorBanner.IsVisible = false;
        var origin = OriginField.Text.Trim();
        var destination = DestinationField.Text.Trim();
        // MAUI 10 pickers allow an empty value.
        var date = DepartureDate.Date;
        var time = DepartureTime.Time;
        if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination))
        { ShowError("Informe o local de saída e o destino."); return; }
        if (date is null || time is null)
        { ShowError("Informe a data e o horário da saída."); return; }
        var departure = date.Value.Date + time.Value;
        if (departure <= DateTime.Now)
        { ShowError("Escolha uma data e um horário futuros."); return; }
        if (_session.Role is not (UserTypes.Driver or UserTypes.Both))
        { ShowError("É necessário um perfil de motorista para publicar."); return; }
        if (SeatsPicker.SelectedIndex < 0)
        { ShowError("Selecione a quantidade de vagas."); return; }

        if(_quote is null || _quote.VehicleType!=(VehiclePicker.SelectedIndex==1?"MOTO":"CAR") || _quote.ExpiresAt<=DateTimeOffset.UtcNow){ShowError("Selecione as sugestões de saída e destino e aguarde o cálculo do percurso antes de publicar.");return;}
        decimal? reference=0m;
        if(PriceModePicker.SelectedIndex==1){if(!MoneyInput.TryParse(ReferenceEntry.Text,out var amount)){ShowError("Informe a contribuição em reais, com até duas casas decimais.");return;}reference=amount;}
        _publishing = true;
        PublishButton.IsEnabled = CloseButton.IsEnabled = FormFields.IsEnabled = false;
        PublishButton.Text = "Publicando...";
        try
        {
            var token = await _session.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            { ShowError("Sua sessão expirou. Saia e entre novamente."); return; }
            var request=new CreateRideRequest(origin,destination,departure,VehiclePicker.SelectedIndex==1?1:SeatsPicker.SelectedIndex+1,VehiclePicker.SelectedIndex==1?"MOTO":"CAR",PriceModePicker.SelectedIndex==0?"Free":"Negotiable",reference,_quote.Id);
            var result=_editId is int id?await _rides.UpdateAsync(id,request,token):await _rides.CreateAsync(request,token);
            if (!result.Success) { ShowError(result.Error ?? "Tente novamente."); return; }
            await DisplayAlertAsync(_editId.HasValue?"Carona atualizada":"Carona publicada", "Os dados da carona foram salvos.", "OK");
            await Navigation.PopModalAsync();
        }
        finally
        {
            _publishing = false;
            PublishButton.IsEnabled = CloseButton.IsEnabled = FormFields.IsEnabled = true;
            PublishButton.Text = _editId.HasValue?"Atualizar carona":"Publicar carona";
        }
    }

    private void OnVehicleChanged(object? sender, EventArgs e)
    {
        // SelectedIndexChanged também é disparado durante InitializeComponent.
        if (SeatsPicker is null) return;
        _quote=null;_routeGeneration++;_suggestionGeneration++;RouteSummaryLabel.Text="Selecione o percurso para o veículo escolhido.";
        SeatsPicker.IsEnabled = VehiclePicker.SelectedIndex != 1;
        if (VehiclePicker.SelectedIndex == 1) SeatsPicker.SelectedIndex = 0;
        if(_routes is not null)OnRoutePointChanged(this,EventArgs.Empty);
    }

    private int _routeGeneration;
    private async void OnRoutePointChanged(object? sender,EventArgs e)
    {
        var generation=++_routeGeneration;_quote=null;_suggestionGeneration++;
        var origin=OriginField.SelectedPoint;var destination=DestinationField.SelectedPoint;
        RouteSummaryLabel.Text="Selecione uma sugestão de saída e destino para calcular o percurso.";
        if(origin is null||destination is null)return;
        RouteSummaryLabel.Text="Calculando percurso…";
        var result=await _routes.CreateAsync(new RouteRequest(origin,destination,VehiclePicker.SelectedIndex==1?"MOTO":"CAR"));
        if(generation!=_routeGeneration)return;
        if(!result.Success||result.Data is null){RouteSummaryLabel.Text=result.Error;return;}
        _quote=result.Data;
        RouteSummaryLabel.Text=$"{_quote.DistanceMeters/1000.0:0.0} km · cerca de {Math.Ceiling(_quote.DurationSeconds/60.0)} min (estimativa Google Maps)";
        await SuggestAsync();
    }
    private void OnReferenceEdited(object? sender,TextChangedEventArgs e)=>_priceDraft.UserEdited();
    private async void OnPriceChanged(object? sender,EventArgs e){if(ReferenceEntry is null||SuggestionLabel is null||_routes is null)return;ReferenceEntry.IsVisible=PriceModePicker.SelectedIndex==1;await SuggestAsync();}
    private async Task SuggestAsync()
    {
        var generation=++_suggestionGeneration;var draftVersion=_priceDraft.Version;
        if(PriceModePicker.SelectedIndex==0){SuggestionLabel.Text="Gratuita: sem contribuição.";return;}
        if(_quote is null){SuggestionLabel.Text="Selecione o percurso para calcular uma sugestão.";return;}
        var seats=VehiclePicker.SelectedIndex==1?1:SeatsPicker.SelectedIndex+1;
        var r=await _routes.SuggestAsync(_quote.Id,seats,false);
        if(generation!=_suggestionGeneration)return;
        SuggestionLabel.Text=r.Success&&r.Data?.Amount is decimal amount?$"Sugestão: {TravelText.Money(amount)} por passageiro. Cálculo: {_quote.DistanceMeters/1000.0:0.0} km × {TravelText.Money(r.Data.CostPerKm)} / {seats+1} pessoas.":r.Data?.UnavailableReason??r.Error??"Estimativa indisponível.";
        if(r.Success&&r.Data?.Amount is decimal suggested&&_priceDraft.CanApplySuggestion(draftVersion)&&string.IsNullOrWhiteSpace(ReferenceEntry.Text))ReferenceEntry.Text=suggested.ToString("0.00",System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorBanner.IsVisible = true;
    }
}
