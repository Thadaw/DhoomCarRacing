using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

public static class GoogleDesktopAuth
{
    public static string ClientId = "192462686702-u6q3vli1egqqb2p57dbhbjfh8u02mi74.apps.googleusercontent.com";
    public static string ClientSecret = "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";

    public static async Task<string> GetUserName()
    {
        if (string.IsNullOrEmpty(ClientId) || ClientId == "YOUR_CLIENT_ID")
        {
            Debug.LogError("GoogleDesktopAuth: ClientId not configured.");
            return null;
        }

        int port = FindAvailablePort(51999);
        TcpListener listener = null;
        CancellationTokenSource cts = new CancellationTokenSource();

        try
        {
            listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
        }
        catch (Exception ex)
        {
            Debug.LogError($"GoogleDesktopAuth: Failed to start TCP listener on port {port}: {ex.Message}");
            return null;
        }

        string redirectUri = $"http://127.0.0.1:{port}/";

        string oauthUrl = "https://accounts.google.com/o/oauth2/v2/auth?" +
            "client_id=" + Uri.EscapeDataString(ClientId) +
            "&redirect_uri=" + Uri.EscapeDataString(redirectUri) +
            "&response_type=code" +
            "&scope=" + Uri.EscapeDataString("openid profile email") +
            "&access_type=offline" +
            "&prompt=select_account";

        Debug.Log($"Opening browser for Google OAuth... listening on 127.0.0.1:{port}");

        try
        {
            System.Diagnostics.Process.Start(oauthUrl);
        }
        catch (Exception ex)
        {
            Debug.LogError($"GoogleDesktopAuth: Failed to open browser: {ex.Message}");
            Debug.Log($"Open this URL manually:\n{oauthUrl}");
        }

        string authCode = null;

        try
        {
            authCode = await Task.Run(() =>
            {
                try
                {
                    using (TcpClient client = listener.AcceptTcpClient())
                    {
                        NetworkStream stream = client.GetStream();
                        byte[] buffer = new byte[4096];
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        Debug.Log("Google OAuth redirect received:\n" + request);

                        Match match = Regex.Match(request, @"[?&]code=([^&\s]+)");
                        if (match.Success)
                        {
                            string code = Uri.UnescapeDataString(match.Groups[1].Value);
                            SendHttpResponse(stream, "200 OK", "text/html",
                                "<html><body><script>window.close()</script><h1>Signed in! You can close this window.</h1></body></html>");
                            return code;
                        }
                        else
                        {
                            SendHttpResponse(stream, "400 Bad Request", "text/html",
                                "<html><body><h1>Authentication failed.</h1></body></html>");
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("GoogleDesktopAuth: TCP accept failed: " + ex.Message);
                    return null;
                }
            }, cts.Token).WithTimeout(TimeSpan.FromMinutes(3), cts);
        }
        finally
        {
            cts.Cancel();
            cts.Dispose();
            listener.Stop();
        }

        if (string.IsNullOrEmpty(authCode))
        {
            Debug.LogError("GoogleDesktopAuth: No auth code received (timed out or failed).");
            return null;
        }

        string accessToken = await ExchangeCodeForToken(authCode, redirectUri);

        if (string.IsNullOrEmpty(accessToken))
            return null;

        return await FetchUserName(accessToken);
    }

    private static int FindAvailablePort(int preferredPort)
    {
        try
        {
            TcpListener test = new TcpListener(IPAddress.Loopback, preferredPort);
            test.Start();
            test.Stop();
            return preferredPort;
        }
        catch
        {
            TcpListener fallback = new TcpListener(IPAddress.Loopback, 0);
            fallback.Start();
            int port = ((IPEndPoint)fallback.LocalEndpoint).Port;
            fallback.Stop();
            Debug.LogWarning($"GoogleDesktopAuth: Port {preferredPort} in use, using port {port}");
            return port;
        }
    }

    private static void SendHttpResponse(NetworkStream stream, string status, string contentType, string body)
    {
        byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
        string header = $"HTTP/1.1 {status}\r\n" +
                        $"Content-Type: {contentType}; charset=utf-8\r\n" +
                        $"Content-Length: {bodyBytes.Length}\r\n" +
                        "Connection: close\r\n" +
                        "\r\n";
        byte[] headerBytes = Encoding.ASCII.GetBytes(header);
        stream.Write(headerBytes, 0, headerBytes.Length);
        stream.Write(bodyBytes, 0, bodyBytes.Length);
    }

    private static Task<string> ExchangeCodeForToken(string code, string redirectUri)
    {
        var tcs = new TaskCompletionSource<string>();

        string formData = "code=" + Uri.EscapeDataString(code) +
            "&client_id=" + Uri.EscapeDataString(ClientId) +
            "&client_secret=" + Uri.EscapeDataString(ClientSecret) +
            "&redirect_uri=" + Uri.EscapeDataString(redirectUri) +
            "&grant_type=authorization_code";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(formData);
        UnityWebRequest www = new UnityWebRequest("https://oauth2.googleapis.com/token", "POST");
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

        var operation = www.SendWebRequest();

        operation.completed += (_) =>
        {
            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                Debug.Log("Google token response: " + json);
                TokenResponse tokenResponse = JsonUtility.FromJson<TokenResponse>(json);
                if (string.IsNullOrEmpty(tokenResponse?.access_token))
                    Debug.LogError("Google token response missing access_token");
                tcs.TrySetResult(tokenResponse?.access_token);
            }
            else
            {
                Debug.LogError("Google token exchange failed: " + www.error + " | Response: " + (www.downloadHandler?.text ?? "none"));
                tcs.TrySetResult(null);
            }
            www.Dispose();
        };

        return tcs.Task;
    }

    private static Task<string> FetchUserName(string accessToken)
    {
        var tcs = new TaskCompletionSource<string>();

        UnityWebRequest www = UnityWebRequest.Get("https://www.googleapis.com/oauth2/v2/userinfo");
        www.SetRequestHeader("Authorization", "Bearer " + accessToken);

        var operation = www.SendWebRequest();

        operation.completed += (_) =>
        {
            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                Debug.Log("Google userinfo response: " + json);
                UserInfoResponse info = JsonUtility.FromJson<UserInfoResponse>(json);
                tcs.TrySetResult(info?.name);
            }
            else
            {
                Debug.LogError("Google userinfo failed: " + www.error);
                tcs.TrySetResult(null);
            }
            www.Dispose();
        };

        return tcs.Task;
    }

    [Serializable]
    private class TokenResponse
    {
        public string access_token;
    }

    [Serializable]
    private class UserInfoResponse
    {
        public string name;
    }
}

public static class TaskExtensions
{
    public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout, CancellationTokenSource cts = null)
    {
        if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
            return await task;
        cts?.Cancel();
        return default;
    }
}
