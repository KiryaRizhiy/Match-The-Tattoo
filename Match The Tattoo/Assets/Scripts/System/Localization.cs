using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Text.RegularExpressions;
using UnityEngine;

public static class Localization
{
    private static String[] texts;
    private static String[] indexes;

    public static void LoadLocals(SystemLanguage lang)
    {
        TextAsset _txt = Resources.Load<TextAsset>("Other/Locals");
        String[] _full = _txt.text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        Debug.Log("Locals file rows:" + Environment.NewLine + Functions.EnumerableAsString(_full));
        int rowNum = Array.FindAll<Char>(Regex.Replace(_full[0], lang.ToString() + "{1}.*", String.Empty, RegexOptions.IgnoreCase).ToCharArray(), x => x == Settings.LocalsSeparator).Length;
        Debug.Log("Lang row is: " + rowNum);
        texts = new String[_full.Length-1];
        indexes = new String[_full.Length-1];
        for (int i = 0; i < _full.Length - 1; i++)
        {
            indexes[i] = _full[i].Split(Settings.LocalsSeparator)[0];
            texts[i] = Regex.Match(_full[i], @"(?<=^([^" + Settings.LocalsSeparator + @"]+" + Settings.LocalsSeparator + @"){" + rowNum + @"})[^" + Settings.LocalsSeparator + @"]*").Value;
        }
        Debug.Log("Locals loaded. Indexes:" + Environment.NewLine + Functions.EnumerableAsString(indexes) + Environment.NewLine + lang.ToString() + " texts:" +Environment.NewLine + Functions.EnumerableAsString(texts));
    }
    public static string GetLocal(string code)
    {
        if (code == null)
            return code;
        if (Array.IndexOf(indexes, code) > 0)
            return texts[Array.IndexOf(indexes, code)];
        else
            return code;
    }
}