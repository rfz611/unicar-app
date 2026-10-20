using System.Text.Json;
using UniCar.Mobile.Models;
using UniCar.Mobile.Services;
namespace UniCar.Mobile.Pages;

// Uses the same Google autocomplete as the route picker, without requesting a route.
public sealed class PlaceSearchPage : ContentPage
{
    private readonly Uri _page=new(new Uri(AppConfig.ApiBaseUrl),"map/index.html");
    private readonly string _nonce=Guid.NewGuid().ToString("N");
    private readonly TaskCompletionSource<string?> _result=new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _trusted,_closed;
    public PlaceSearchPage()
    {
        var cancel=new Button{Text="Cancelar",HorizontalOptions=LayoutOptions.Start};
        cancel.Clicked+=async (_,_)=>await Navigation.PopModalAsync();
        var web=new WebView{Source=new UrlWebViewSource{Url=_page+"?mode=place&nonce="+_nonce}};
        web.Navigated+=(_,e)=>_trusted=Trusted(e.Url)&&e.Result==WebNavigationResult.Success;
        web.Navigating+=async (_,e)=>{
            if(!e.Url.StartsWith("unicar-map:",StringComparison.OrdinalIgnoreCase)){e.Cancel=!Trusted(e.Url);return;}
            e.Cancel=true;if(!_trusted||_closed)return;
            try{
                var uri=new Uri(e.Url);if(uri.Host!="selection"||!uri.Query.StartsWith("?data="))return;
                using var doc=JsonDocument.Parse(Uri.UnescapeDataString(uri.Query[6..]));var root=doc.RootElement;
                if(root.GetProperty("nonce").GetString()!=_nonce||root.GetProperty("action").GetString()!="place")return;
                var label=root.GetProperty("request").GetProperty("label").GetString();
                if(string.IsNullOrWhiteSpace(label)||label.Length>150)return;
                _closed=true;_result.TrySetResult(label);await Navigation.PopModalAsync();
            }catch(Exception ex)when(ex is JsonException or KeyNotFoundException or InvalidOperationException or UriFormatException){}
        };
        var layout=new Grid{RowDefinitions={new RowDefinition(GridLength.Auto),new RowDefinition(GridLength.Star)},Padding=new Thickness(16)};
        layout.Add(cancel);layout.Add(web,0,1);Content=layout;BackgroundColor=Colors.White;
    }
    private bool Trusted(string url)=>Uri.TryCreate(url,UriKind.Absolute,out var u)&&u.Scheme==_page.Scheme&&u.Authority==_page.Authority&&u.AbsolutePath==_page.AbsolutePath;
    public async Task<string?> PickAsync(Page owner){await owner.Navigation.PushModalAsync(this);return await _result.Task;}
    protected override void OnDisappearing(){base.OnDisappearing();_closed=true;_result.TrySetResult(null);}
}
