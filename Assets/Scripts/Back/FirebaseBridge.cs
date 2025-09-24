using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Runtime.InteropServices;

[DisallowMultipleComponent]
public class FirebaseBridge : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void FirebaseSignIn();
    [DllImport("__Internal")] private static extern void FirebaseSignOut();
#else
    private static void FirebaseSignIn() { Debug.Log("[Stub] SignIn (Editor)"); }
    private static void FirebaseSignOut() { Debug.Log("[Stub] SignOut (Editor)"); }
#endif

    [Serializable]
    class LoginPayload
    {
        public string uid; public string displayName; public string email; public string photoURL; public string idToken; public string error;
    }
    [Serializable]
    class StatePayload
    {
        public bool signedIn; public string uid; public string email; public string idToken; public string error;
    }

    // UI 버튼에 연결
    public void ClickSignIn() => FirebaseSignIn();
    public void TestSignIn() => SceneManager.LoadScene("LobbyScene");
    public void ClickSignOut() => FirebaseSignOut();

    // JS → Unity 콜백
    public void OnFirebaseLogin(string json)
    {
        Debug.Log($"[Firebase] OnFirebaseLogin: {json}");
        var p = JsonUtility.FromJson<LoginPayload>(string.IsNullOrEmpty(json) ? "{}" : json);
        if (!string.IsNullOrEmpty(p?.error))
        {
            // TODO: 에러 UI 표시
            Debug.LogWarning($"[Firebase] Login Error: {p.error}");
            return;
        }

        // TODO: 게임 세션 초기화, 프로필 노출 등
        Debug.Log($"[Firebase] Login OK: {p.displayName} / {p.email}");
        Debug.Log($"[Firebase] ID Token: {p.idToken?.Substring(0, 16)}...");
        SceneManager.LoadScene("LobbyScene");

    }

    public void OnAuthState(string json)
    {
        Debug.Log($"[Firebase] OnAuthState: {json}");
        var p = JsonUtility.FromJson<StatePayload>(string.IsNullOrEmpty(json) ? "{}" : json);
        // TODO: 로그인/로그아웃 UI 토글
        Debug.Log($"[Firebase] signedIn={p.signedIn}, uid={p.uid}, email={p.email}");
    }
}
