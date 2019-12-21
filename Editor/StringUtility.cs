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

    // see: https://stackoverflow.com/a/8200722/317135
    public static string Center(this string stringToCenter, int totalLength) {
      return stringToCenter
        .PadLeft(
          ((totalLength - stringToCenter.Length) / 2) + stringToCenter.Length
        ).PadRight(totalLength);
    }
  }
}