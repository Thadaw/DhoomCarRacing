using UnityEngine;
using Firebase;
using Firebase.Auth;
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
        DependencyStatus dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus == DependencyStatus.Available)
        {
            auth = FirebaseAuth.DefaultInstance;
            await SignInAnonymously();
            Initialized = true;
            Debug.Log("FirebaseManager: Initialized, UserId=" + UserId);
        }
        else
        {
            Debug.LogError("FirebaseManager: Could not resolve dependencies: " + dependencyStatus);
        }
    }

    private async Task SignInAnonymously()
    {
        AuthResult result = await auth.SignInAnonymouslyAsync();
        UserId = result.User.UserId;
    }
}
