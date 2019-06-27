using System.Globalization;
using System.Text.RegularExpressions;

namespace Destringer {
  public static class StringUtility {
    // see: https://stackoverflow.com/a/55615973
    public static string PascalCase(string str) {
      TextInfo cultInfo = new CultureInfo("en-US", false).TextInfo;
      str = Regex.Replace(str, "([A-Z]+)", " $1");
      str = cultInfo.ToTitleCase(str);
      str = str.Replace(" ", "");
      return str;
    }
  }
}