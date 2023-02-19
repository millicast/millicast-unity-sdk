
using System.Text.RegularExpressions;
using UnityEngine;

public class OSDetails
{
    public static string GetOSName(string input)
    {
       var match = Regex.Match(input, "^([a-z]*)", RegexOptions.IgnoreCase);
       
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        else
        {
        #if UNITY_STANDALONE_WIN
            return "Windows";
        #elif UNITY_STANDALONE_OSX
            return "Mac";
        #elif UNITY_IOS
            return "iOS";
        #elif UNITY_ANDROID
            return "Android";
        #else
            return "UnKnown";
        #endif
        }      
        
    }
    public static string GetOSVersion(string input)
    {
    #if UNITY_ANDROID && !UNITY_EDITOR
        var version = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<string>("RELEASE");
        return version;
    #else
        Regex pattern1 = new Regex("\\d+(\\.\\d+)+");
        Match m = pattern1.Match(input);
        return m.Value;
    #endif
    }

}
