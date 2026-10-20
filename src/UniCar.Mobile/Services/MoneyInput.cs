using System.Globalization;
namespace UniCar.Mobile.Services;
public static class MoneyInput
{
 public static bool TryParse(string? text,out decimal amount)
 {
  amount=0;if(string.IsNullOrWhiteSpace(text))return false;
  text=text.Trim();if(!System.Text.RegularExpressions.Regex.IsMatch(text,@"^\d{1,8}([.,]\d{1,2})?$"))return false;
  return decimal.TryParse(text.Replace(',','.'),NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out amount)&&amount<=99999999.99m;
 }
}
