using UniCar.Mobile.Models;
using UniCar.Mobile.Services;

namespace UniCar.Mobile.Pages;

public partial class HomePage : ContentPage
{
    private readonly SessionService _session;
    private readonly RideService _rides;
    private readonly RouteQuoteService _routes;
    private readonly NegotiationService _negotiations;
    private string _tab = "CAR";
    private bool _scheduling;
    private readonly AsyncReloadGate _loadGate=new();
    private bool _loading=>_loadGate.IsBusy;
    private int _generation;private bool _visible;
    private bool _joining;
    private async void OnDestinationSelected(object? sender,EventArgs e)
    {
        if(DestinationEntry.SelectedPoint is not null)await LoadRidesAsync();
    }

    public HomePage(SessionService session, RideService rides,RouteQuoteService routes,NegotiationService negotiations)
    {
        InitializeComponent();
        _session = session;
        _rides = rides;
        _routes=routes;_negotiations=negotiations;
        DestinationEntry.Configure(routes);
        ScheduleDate.MinimumDate = DateTime.Today;
        ScheduleDate.Date = DateTime.Today.AddDays(1);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _visible=true;
        var firstName = _session.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        GreetingLabel.Text = string.IsNullOrEmpty(firstName) ? "Olá!" : $"Olá, {firstName}";
        RoleLabel.Text = UserTypes.Describe(_session.Role);
        UpdateTabs();
        await LoadRidesAsync();
    }

    protected override void OnDisappearing(){base.OnDisappearing();_visible=false;_generation++;RidesList.ItemsSource=null;StatePanel.IsVisible=false;}

    private void UpdateTabs()
    {
        var reservations = _tab == "RESERVATIONS";
        MotoTab.Style = (Style)Application.Current!.Resources[_tab == "MOTO" ? "SelectedOptionButton" : "OptionButton"];
        CarTab.Style = (Style)Application.Current.Resources[_tab == "CAR" ? "SelectedOptionButton" : "OptionButton"];
        ReservationsTab.Style = (Style)Application.Current.Resources[reservations ? "SelectedOptionButton" : "OptionButton"];
        ScheduleButton.IsVisible = reservations;
        ScheduleButton.Text = _scheduling ? "Ver minhas reservas" : "Agendar nova viagem";
        SearchPanel.IsVisible = !reservations || _scheduling;
        ScheduleFields.IsVisible = reservations && _scheduling;
        SearchTitle.Text = _scheduling ? "Agendar nova viagem" : "Para onde você vai?";
        ListTitle.Text = reservations ? (_scheduling ? "Caronas para a data escolhida" : "Minhas reservas")
            : _tab == "MOTO" ? "Caronas de moto" : "Caronas de carro";
        OfferButton.IsVisible = !reservations && _session.Role is (UserTypes.Driver or UserTypes.Both);
    }

    private async void OnTabClicked(object? sender, EventArgs e)
    {
        if (_loading || sender is not Button { CommandParameter: string tab }) return;
        _tab = tab;
        _scheduling = false;
        UpdateTabs();
        await LoadRidesAsync();
    }

    private async void OnScheduleClicked(object? sender, EventArgs e)
    {
        if (_loading)return;
        _scheduling = !_scheduling;
        UpdateTabs();
        if (_scheduling) { RidesList.ItemsSource = null; StatePanel.IsVisible = false; }
        else await LoadRidesAsync();
    }

    private async void OnSearchClicked(object? sender, EventArgs e) => await LoadRidesAsync();

    private async Task LoadRidesAsync()
    {
        if (_scheduling && (ScheduleDate.Date is null || ScheduleDate.Date.Value.Date < DateTime.Today))
        { await DisplayAlertAsync("Data da viagem", "Escolha uma data a partir de hoje.", "OK"); return; }
        if(!_loadGate.TryStart())return;var generation=_generation;
        SetSearchEnabled(false);
        StatePanel.IsVisible = false;
        RidesList.IsVisible = false;
        LoadingIndicator.IsRunning = true;
        try
        {
            var token = await _session.GetTokenAsync();
            var saved = string.IsNullOrEmpty(token)
                ? new RideResult<List<Ride>>(false, null, "Sua sessão expirou. Saia e entre novamente.")
                : await _rides.ReservationsAsync(token);
            var viewingReservations = _tab == "RESERVATIONS" && !_scheduling;
            RideResult<List<Ride>> result;
            if (viewingReservations || !saved.Success) result = saved;
            else result = await _rides.SearchAsync(DestinationEntry.Text ?? "",
                _scheduling ? (ScheduleType.SelectedIndex == 1 ? "MOTO" : "CAR") : _tab,
                _scheduling ? ScheduleDate.Date : null);
            if(generation!=_generation)return;
            var bookedIds = saved.Data?.Select(r => r.Id).ToHashSet() ?? [];
            var items = result.Data?.Where(r => viewingReservations ||
                (r.DepartureTime > DateTime.Now && r.AvailableSeats > 0 && !bookedIds.Contains(r.Id)))
                .OrderBy(r => r.DepartureTime < DateTime.Now).ThenBy(r => r.DepartureTime).ToList() ?? [];
            RidesList.ItemsSource = items;
            RidesList.IsVisible = result.Success && items.Count > 0;
            StatePanel.IsVisible = !RidesList.IsVisible;
            StateTitle.Text = result.Success ? (viewingReservations ? "Você ainda não tem reservas" : "Nenhuma carona por aqui ainda") : "Não foi possível carregar";
            StateMessage.Text = result.Success
                ? (viewingReservations ? "Toque em Agendar nova viagem para encontrar uma carona." : "Tente outro destino ou data para ver novas caronas.")
                : result.Error;
            RetryButton.IsVisible = !result.Success;
        }
        finally
        {
            var reload=_loadGate.Complete(_visible);
            LoadingIndicator.IsRunning = false;
            SetSearchEnabled(true);
            if(reload)await LoadRidesAsync();
        }
    }

    private void SetSearchEnabled(bool enabled)
    {
        SearchButton.IsEnabled = RetryButton.IsEnabled = DestinationEntry.IsEnabled = enabled;
        MotoTab.IsEnabled = CarTab.IsEnabled = ReservationsTab.IsEnabled = ScheduleButton.IsEnabled = enabled;
        ScheduleFields.IsEnabled = enabled;
    }

    private async void OnJoinClicked(object? sender, EventArgs e)
    {
        if (_joining || sender is not Button { CommandParameter: Ride ride } button) return;
        if(ride.PricingMode=="LegacyUnknown"){await DisplayAlertAsync("Valor não informado","O motorista precisa atualizar o percurso e a contribuição desta carona.","OK");return;}
        if(ride.PricingMode=="Negotiable"){await Navigation.PushModalAsync(new NegotiationPage(ride,_negotiations));return;}
        _joining = true;
        button.IsEnabled = false;
        try
        {
            if (!await DisplayAlertAsync("Solicitar vaga", $"Confirmar a carona de {ride.Origin} para {ride.Destination}, em {ride.DepartureLabel}?", "Confirmar", "Cancelar")) return;
            var token = await _session.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                await DisplayAlertAsync("Entre novamente", "Sua sessão expirou. Saia e entre novamente.", "OK");
                return;
            }
            var result = await _rides.JoinAsync(ride.Id, token);
            if(!SessionResponseGuard.Matches(token,await _session.GetTokenAsync()))return;
            if (result.Success)
            {
                _tab = "RESERVATIONS";
                _scheduling = false;
                UpdateTabs();
            }
            await DisplayAlertAsync(result.Success ? "Vaga reservada" : "Não foi possível reservar",
                result.Success ? "Sua reserva foi confirmada." : result.Error ?? "Tente novamente.", "OK");
            await LoadRidesAsync();
        }
        finally { _joining = false; button.IsEnabled = true; }
    }

    private async void OnOfferClicked(object? sender, EventArgs e)
    {
        OfferButton.IsEnabled = false;
        try { await Navigation.PushModalAsync(new OfferRidePage(_session, _rides,_routes,initialVehicle:_tab)); }
        finally { OfferButton.IsEnabled = true; }
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        _tab = "CAR";
        _scheduling = false;
        _session.Clear();
        await Shell.Current.GoToAsync("//welcome");
    }
}
