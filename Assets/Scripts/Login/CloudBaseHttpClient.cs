using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using MiniJSON;

public static class CloudBaseHttpClient
{
    // 👇 替换为你在第2步中获取到的云函数 URL
    private const string CLOUD_FUNCTION_URL = "https://mygame-8g2gp1rw8dd149f7-1420574527.ap-shanghai.app.tcloudbase.com/databaseProxy";

    /// <summary>
    /// 添加文档
    /// </summary>
    public static IEnumerator Add(string collectionName, Dictionary<string, object> doc, System.Action<bool, string> callback)
    {
        var payload = new Dictionary<string, object>
        {
            { "action", "add" },
            { "collectionName", collectionName },
            { "data", doc }
        };
        string json = Json.Serialize(payload);
        yield return Post(CLOUD_FUNCTION_URL, json, (success, response) =>
        {
            if (success)
            {
                var parsed = Json.Deserialize(response) as Dictionary<string, object>;
                if (parsed != null && parsed.ContainsKey("code") && (long)parsed["code"] == 0)
                {
                    // 成功，可以尝试从响应中解析 _id
                    var data = parsed["data"] as Dictionary<string, object>;
                    string id = data != null && data.ContainsKey("id") ? data["id"].ToString() : "";
                    callback(true, id);
                }
                else
                {
                    Debug.LogError($"添加文档失败：{response}");
                    callback(false, response);
                }
            }
            else
            {
                callback(false, response);
            }
        });
    }

    /// <summary>
    /// 查询文档
    /// </summary>
    public static IEnumerator Query(string collectionName, Dictionary<string, object> where, System.Action<bool, List<Dictionary<string, object>>> callback)
    {
        var payload = new Dictionary<string, object>
        {
            { "action", "query" },
            { "collectionName", collectionName },
            { "query", where ?? new Dictionary<string, object>() }
        };
        string json = Json.Serialize(payload);
        yield return Post(CLOUD_FUNCTION_URL, json, (success, response) =>
        {
            if (success)
            {
                var parsed = Json.Deserialize(response) as Dictionary<string, object>;
                if (parsed != null && parsed.ContainsKey("code") && (long)parsed["code"] == 0)
                {
                    var dataList = parsed["data"] as List<object>;
                    var result = new List<Dictionary<string, object>>();
                    if (dataList != null)
                    {
                        foreach (var item in dataList)
                        {
                            if (item is Dictionary<string, object> dict)
                                result.Add(dict);
                        }
                    }
                    callback(true, result);
                }
                else
                {
                    Debug.LogError($"查询失败：{response}");
                    callback(false, null);
                }
            }
            else
            {
                callback(false, null);
            }
        });
    }

    private static IEnumerator Post(string url, string jsonData, System.Action<bool, string> callback)
    {
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✅ 请求成功：{request.downloadHandler.text}");
                callback(true, request.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"❌ 请求失败：{request.error}，响应：{request.downloadHandler.text}");
                callback(false, request.error);
            }
        }
    }

    public static IEnumerator Update(string collectionName, string docId, Dictionary<string, object> updates, System.Action<bool, string> callback)
    {
        var payload = new Dictionary<string, object>
    {
        { "action", "update" },
        { "collectionName", collectionName },
        { "docId", docId },
        { "data", updates }
    };
        string json = Json.Serialize(payload);
        yield return Post(CLOUD_FUNCTION_URL, json, (success, response) =>
        {
            if (success)
            {
                var parsed = Json.Deserialize(response) as Dictionary<string, object>;
                if (parsed != null && parsed.ContainsKey("code") && (long)parsed["code"] == 0)
                    callback(true, response);
                else
                    callback(false, response);
            }
            else
                callback(false, response);
        });
    }

    /// <summary>
    /// 获取最大 playerId（聚合查询）
    /// </summary>
    public static IEnumerator GetMaxPlayerId(System.Action<bool, long> callback)
    {
        var payload = new Dictionary<string, object>
    {
        { "action", "getMaxPlayerId" },
        { "collectionName", "players" }
    };
        string json = Json.Serialize(payload);
        yield return Post(CLOUD_FUNCTION_URL, json, (success, response) =>
        {
            if (success)
            {
                var parsed = Json.Deserialize(response) as Dictionary<string, object>;
                if (parsed != null && parsed.ContainsKey("code") && (long)parsed["code"] == 0)
                {
                    var data = parsed["data"] as Dictionary<string, object>;
                    long maxId = data != null && data.ContainsKey("maxPlayerId") ? Convert.ToInt64(data["maxPlayerId"]) : 10009;
                    callback(true, maxId);
                }
                else
                {
                    Debug.LogError($"获取最大playerId失败：{response}");
                    callback(false, 0);
                }
            }
            else
            {
                callback(false, 0);
            }
        });
    }

    /// <summary>
    /// 分页查询（支持 skip 和 limit）
    /// </summary>
    public static IEnumerator QueryWithPage(string collectionName, Dictionary<string, object> where, int skip, int limit, System.Action<bool, List<Dictionary<string, object>>> callback)
    {
        var payload = new Dictionary<string, object>
    {
        { "action", "query" },
        { "collectionName", collectionName },
        { "query", where ?? new Dictionary<string, object>() },
        { "skip", skip },
        { "limit", limit }
    };
        string json = Json.Serialize(payload);
        yield return Post(CLOUD_FUNCTION_URL, json, (success, response) =>
        {
            if (success)
            {
                var parsed = Json.Deserialize(response) as Dictionary<string, object>;
                if (parsed != null && parsed.ContainsKey("code") && (long)parsed["code"] == 0)
                {
                    var dataList = parsed["data"] as List<object>;
                    var result = new List<Dictionary<string, object>>();
                    if (dataList != null)
                    {
                        foreach (var item in dataList)
                        {
                            if (item is Dictionary<string, object> dict)
                                result.Add(dict);
                        }
                    }
                    callback(true, result);
                }
                else
                {
                    Debug.LogError($"分页查询失败：{response}");
                    callback(false, null);
                }
            }
            else
            {
                callback(false, null);
            }
        });
    }
}