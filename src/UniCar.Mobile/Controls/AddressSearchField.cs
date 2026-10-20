using Microsoft.Maui.Controls.Shapes;
using UniCar.Mobile.Models;
using UniCar.Mobile.Services;
namespace UniCar.Mobile.Controls;
public sealed class AddressSearchField:ContentView
{
 private readonly Label _title=new(){FontSize=14,FontAttributes=FontAttributes.Bold,TextColor=Color.FromArgb("#052355")};
 private readonly Entry _entry=new(){FontSize=16,TextColor=Color.FromArgb("#052355"),PlaceholderColor=Color.FromArgb("#9FB0D3"),BackgroundColor=Colors.Transparent,VerticalOptions=LayoutOptions.Center,MaxLength=150};
 private readonly VerticalStackLayout _suggestions=new(){Spacing=8};
 private readonly Label _state=new(){FontSize=12,TextColor=Color.FromArgb("#5b6b85"),IsVisible=false};
 private RouteQuoteService? _routes;private int _version;private bool _setting;private string _session=Guid.NewGuid().ToString("N");private CancellationTokenSource? _pending;
 public MapPoint? SelectedPoint{get;private set;}
 public event EventHandler? SelectedPointChanged;
 public string Title{get=>_title.Text;set=>_title.Text=value;}
 public string Placeholder{get=>_entry.Placeholder;set=>_entry.Placeholder=value;}
 public string Text{get=>_entry.Text??"";set{_setting=true;_entry.Text=value;_setting=false;}}
 public void Configure(RouteQuoteService routes)=>_routes=routes;
 public AddressSearchField(){
  var box=new Border{
   BackgroundColor=Color.FromArgb("#F1F4F9"),Stroke=Colors.Transparent,StrokeThickness=2,
   StrokeShape=new RoundRectangle{CornerRadius=new CornerRadius(8)},
   Padding=new Thickness(14,0),HeightRequest=56,Content=_entry
  };
  _entry.Focused+=(_,_)=>box.Stroke=Color.FromArgb("#052355");
  _entry.Unfocused+=(_,_)=>box.Stroke=Colors.Transparent;
  Content=new VerticalStackLayout{Spacing=6,Children={_title,box,_suggestions,_state}};
  _entry.TextChanged+=OnEdited;
  Unloaded+=(_,_)=>{_pending?.Cancel();_version++;};
 }
 private static void AlignSuggestionText(Button button)
 {
#if MACCATALYST || IOS
  if(button.Handler?.PlatformView is UIKit.UIButton native){native.HorizontalAlignment=UIKit.UIControlContentHorizontalAlignment.Left;if(native.TitleLabel is not null)native.TitleLabel.TextAlignment=UIKit.UITextAlignment.Left;}
#elif ANDROID
  if(button.Handler?.PlatformView is Android.Widget.Button native)native.Gravity=Android.Views.GravityFlags.Start|Android.Views.GravityFlags.CenterVertical;
#elif WINDOWS
  if(button.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Button native)native.HorizontalContentAlignment=Microsoft.UI.Xaml.HorizontalAlignment.Left;
#endif
 }
 private void State(string? text){_state.Text=text;_state.IsVisible=!string.IsNullOrEmpty(text);}
 private async void OnEdited(object? sender,TextChangedEventArgs e){
  if(_setting)return;var version=++_version;_pending?.Cancel();_pending=new();var ct=_pending.Token;
  SelectedPoint=null;SelectedPointChanged?.Invoke(this,EventArgs.Empty);_suggestions.Clear();State(null);
  var query=e.NewTextValue?.Trim()??"";if(query.Length<3||_routes is null)return;
  try{
   await Task.Delay(350,ct);State("Buscando locais no Brasil…");
   var result=await _routes.SearchPlacesAsync(query,_session,ct);if(version!=_version||ct.IsCancellationRequested)return;
   if(!result.Success){State(result.Error);return;}
   var rows=result.Data??[];State(rows.Count==0?"Nenhum local encontrado. Tente completar o endereço.":null);
   foreach(var row in rows){
    var button=new Button{Text=row.Title+"\n"+row.Address,FontSize=14,TextColor=Color.FromArgb("#052355"),BackgroundColor=Color.FromArgb("#f1f4f9"),HorizontalOptions=LayoutOptions.Fill,MinimumHeightRequest=64,CornerRadius=16,Padding=new Thickness(16,12),BorderWidth=1,BorderColor=Color.FromArgb("#E4EAF3"),LineBreakMode=LineBreakMode.WordWrap};
    button.HandlerChanged+=(_,_)=>AlignSuggestionText(button);
    button.Clicked+=async (_,_)=>{
     if(version!=_version||_routes is null)return;_suggestions.Clear();State("Selecionando local…");
     var selected=await _routes.ResolvePlaceAsync(row.Id,_session,ct);if(version!=_version||ct.IsCancellationRequested)return;
     if(!selected.Success||selected.Data is null){State(selected.Error);return;}
     _session=Guid.NewGuid().ToString("N");SelectedPoint=selected.Data;Text=selected.Data.Label;State(null);_entry.Unfocus();SelectedPointChanged?.Invoke(this,EventArgs.Empty);
    };_suggestions.Add(button);
   }
   if(rows.Count>0)_suggestions.Add(new Label{Text="Google Maps",FontSize=14,TextColor=Color.FromArgb("#5e5e5e"),HorizontalOptions=LayoutOptions.End,Margin=new Thickness(10,8)});
  }catch(OperationCanceledException){}
 }
}
