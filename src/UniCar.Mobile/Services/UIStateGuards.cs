namespace UniCar.Mobile.Services;
public sealed class AsyncReloadGate
{
 public bool IsBusy { get; private set; }
 private bool _pending;
 public bool TryStart(){if(IsBusy){_pending=true;return false;}IsBusy=true;return true;}
 public bool Complete(bool active){IsBusy=false;var again=_pending&&active;_pending=false;return again;}
}
public sealed class PriceDraftGuard
{
 public int Version { get; private set; }
 public void UserEdited()=>Version++;
 public bool CanApplySuggestion(int startedAt)=>Version==startedAt;
}
public static class SessionResponseGuard
{
 public static bool Matches(string? originalToken,string? currentToken)=>!string.IsNullOrEmpty(originalToken)&&originalToken==currentToken;
}
