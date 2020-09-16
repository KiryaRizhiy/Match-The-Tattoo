using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Logger
{
    private static List<LogContentInfo> LogStorage = new List<LogContentInfo>();

    public static void UpdateContent(UILogDataType type, string info, bool isSFC = false, bool STL = false)
    {
        if (LogStorage.Find(x => x.dataType == type) == null)
        {
            LogStorage.Add(new LogContentInfo(type, info, isSFC, STL));
        }
        else
        {
            LogStorage.Find(x => x.dataType == type).UpdateContent(info, isSFC, STL);
        }
    }
    public static void AddContent(UILogDataType type, string info, bool isSFC = false, bool STL = false)
    {
        if (LogStorage.Find(x => x.dataType == type) == null)
        {
            LogStorage.Add(new LogContentInfo(type, info, isSFC, STL));
        }
        else
        {
            LogStorage.Find(x => x.dataType == type).AddContent(info, isSFC, STL);
        }
    }
    public static string GetAllContent()
    {
        string text = "";
        foreach (UILogDataType _dataType in Enum.GetValues(typeof(UILogDataType)))
        {
            if (!String.IsNullOrEmpty(text))
                text = text + System.Environment.NewLine;
            text = text + GetContentByType(_dataType);
        }
        return text;
    }
    public static void RemoveAllContent()
    {
        foreach (UILogDataType _dataType in Enum.GetValues(typeof(UILogDataType)))
            LogStorage.Remove(LogStorage.Find(x => x.dataType == _dataType));
    }
    private static string GetContentByType(UILogDataType type)
    {
        LogContentInfo currContent = LogStorage.Find(x => x.dataType == type);
        if (currContent == null)
            return null;
        else
        {
            string info = currContent.content;
            if (currContent.isSingleFrameContent) LogStorage.Remove(currContent);
            return info;
        }
    }
    private class LogContentInfo
    {
        public UILogDataType dataType;
        public bool isSingleFrameContent { get; private set; }
        public bool sendToLogs { get; private set; }
        public string content;
        public LogContentInfo(UILogDataType type, string info, bool isSFC, bool STL)
        {
            dataType = type;
            isSingleFrameContent = isSFC;
            content = info;
            sendToLogs = STL;
            if (sendToLogs)
                Debug.Log(dataType.ToString() + " : " + content);
        }
        public void UpdateContent(string info, bool isSFC, bool STL)
        {
            content = info;
            isSingleFrameContent = isSFC;
            sendToLogs = STL;
            if (sendToLogs)
                Debug.Log(dataType.ToString() + " : " + content);
        }
        public void AddContent(string info, bool isSFC, bool STL)
        {
            content = content + " | " + System.Environment.NewLine + info;
            sendToLogs = STL;
            if (sendToLogs)
                Debug.Log(dataType.ToString() + " : " + content);
        }
    }
}
public enum UILogDataType { Init, Level, Controls, Logic, GameState, Monetization };