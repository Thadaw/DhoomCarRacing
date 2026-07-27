using UnityEngine;
using Firebase;
using Firebase.Auth;
using System;
using System.Threading.Tasks;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    private FirebaseAuth auth;
    public string UserId { get; private set; }
    public bool Initialized { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        if (Instance != null) return;
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _ = Initialize();
    }

    public static void EnsureExists()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("FirebaseManager");
            Instance = go.AddComponent<FirebaseManager>();
        }
    }

    private async Task Initialize()
    {
        try
        {
            DependencyStatus dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dependencyStatus == DependencyStatus.Available)
            {
                try
                {
                    auth = FirebaseAuth.DefaultInstance;
                    await SignInAnonymously();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("FirebaseManager: Auth sign-in failed (non-fatal): " + ex.Message);
                }
                Initialized = true;
                Debug.Log("FirebaseManager: Initialized, UserId=" + UserId);
            }
            else
            {
                Debug.LogError("FirebaseManager: Could not resolve dependencies: " + dependencyStatus);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("FirebaseManager: Initialize failed: " + ex.Message);
        }
    }

    private async Task SignInAnonymously()
    {
        AuthResult result = await auth.SignInAnonymouslyAsync();
        UserId = result.User.UserId;
    }
}
