using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Text;
using UnityEngine.SceneManagement;

public class AuthManager : MonoBehaviour
{
    [Header("--- UI Panels ---")]
    public GameObject loginPanel;
    public GameObject registerPanel;
    public GameObject updatePanel;

    [Header("--- Register Inputs ---")]
    public TMP_InputField registerUsername;
    public TMP_InputField registerEmail;
    public TMP_InputField registerPassword;

    [Header("--- Login Inputs ---")]
    public TMP_InputField loginUsername;
    public TMP_InputField loginPassword;

    [Header("--- Update Inputs ---")]
    public TMP_InputField updateEmail;
    public TMP_InputField updateNewUsername;
    public TMP_InputField updateNewPassword;

    [Header("--- Global Message Text ---")]
    public TextMeshProUGUI messageText;

    // ================= NGROK URL =================
    private string baseURL = "https://rejoin-synopses-backfire.ngrok-free.dev/api/auth/";

    // ================= START =================
    private void Start()
    {
        SwitchToLoginPanel();
    }

    // ================= PANEL SWITCH =================
    public void SwitchToRegisterPanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        updatePanel.SetActive(false);
        ClearMessage();
    }

    public void SwitchToLoginPanel()
    {
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        updatePanel.SetActive(false);
        ClearMessage();
    }

    public void SwitchToUpdatePanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        updatePanel.SetActive(true);
        ClearMessage();
    }

    // ================= REGISTER =================
    public void Register()
    {
        StartCoroutine(RegisterCoroutine());
    }

    IEnumerator RegisterCoroutine()
    {
        ShowNormal("Registering account...");

        RegisterData data = new RegisterData
        {
            Username = registerUsername.text,
            Email = registerEmail.text,
            Password = registerPassword.text
        };

        string json = JsonUtility.ToJson(data);

        yield return StartCoroutine(PostRequest("register", json, (isSuccess, responseText) =>
        {
            if (isSuccess)
            {
                ShowSuccess("Register successful!");
                Invoke("SwitchToLoginPanel", 1.5f);
            }
            else
            {
                ShowError(responseText);
            }
        }));
    }

    // ================= LOGIN =================
    public void Login()
    {
        StartCoroutine(LoginCoroutine());
    }

    IEnumerator LoginCoroutine()
    {
        ShowNormal("Logging in...");

        LoginData data = new LoginData
        {
            Username = loginUsername.text,
            Password = loginPassword.text
        };

        string json = JsonUtility.ToJson(data);

        yield return StartCoroutine(PostRequest("login", json, (isSuccess, responseText) =>
        {
            if (isSuccess)
            {
                ShowSuccess("Login successful!");

                PlayerPrefs.SetString("UserMode", "Member");
                PlayerPrefs.SetString("CurrentUsername", loginUsername.text);
                PlayerPrefs.Save();

                Invoke("LoadGameplayScene", 1f);
            }
            else
            {
                ShowError(responseText);
            }
        }));
    }

    // ================= UPDATE ACCOUNT =================
    public void UpdateAccount()
    {
        StartCoroutine(UpdateCoroutine());
    }

    IEnumerator UpdateCoroutine()
    {
        ShowNormal("Updating account...");

        UpdateData data = new UpdateData
        {
            Email = updateEmail.text,
            NewUsername = updateNewUsername.text,
            NewPassword = updateNewPassword.text
        };

        string json = JsonUtility.ToJson(data);

        yield return StartCoroutine(PostRequest("update", json, (isSuccess, responseText) =>
        {
            if (isSuccess)
            {
                ShowSuccess("Account updated successfully!");
            }
            else
            {
                ShowError(responseText);
            }
        }));
    }

    // ================= QUICK PLAY =================
    public void QuickPlay()
    {
        PlayerPrefs.SetString("UserMode", "Guest");
        PlayerPrefs.SetString("CurrentUsername", "Guest");
        PlayerPrefs.Save();

        ShowSuccess("Entering as Guest...");
        Invoke("LoadGameplayScene", 1f);
    }

    // ================= POST REQUEST (Bypass Ngrok Warning) =================
    IEnumerator PostRequest(string endpoint, string json, System.Action<bool, string> callback)
    {
        string fullURL = (baseURL + endpoint).Trim();
        UnityWebRequest request = new UnityWebRequest(fullURL, "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        // C?u hình các Header b?t bu?c
        request.SetRequestHeader("Content-Type", "application/json");

        // QUAN TR?NG: Thêm dòng này ?? b? qua màn hình c?nh báo ch?n k?t n?i c?a ngrok
        request.SetRequestHeader("ngrok-skip-browser-warning", "true");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            string errorResponse = !string.IsNullOrEmpty(request.downloadHandler.text)
                ? request.downloadHandler.text
                : request.error;

            // In log l?i chi ti?t ra Console c?a Unity ?? d? debug
            Debug.LogError($"[AuthManager] L?i k?t n?i ??n: {fullURL}\nChi ti?t: {errorResponse}");

            callback?.Invoke(false, errorResponse);
        }
        else
        {
            callback?.Invoke(true, request.downloadHandler.text);
        }
    }

    // ================= MESSAGE UI =================
    void ShowNormal(string msg)
    {
        messageText.text = msg;
        messageText.color = Color.white;
    }

    void ShowSuccess(string msg)
    {
        messageText.text = msg;
        messageText.color = Color.green;
    }

    void ShowError(string msg)
    {
        messageText.text = msg;
        messageText.color = Color.red;
    }

    void ClearMessage()
    {
        messageText.text = "";
    }

    // ================= LOAD SCENE =================
    void LoadGameplayScene()
    {
        // Hãy ch?c ch?n r?ng b?n ?ã thêm Scene mang tên "MainMenu" vào Build Settings c?a Unity
        SceneManager.LoadScene("MainMenu");
    }
}

// ================= DTO DATA CLASSES =================

[System.Serializable]
public class RegisterData
{
    public string Username;
    public string Email;
    public string Password;
}

[System.Serializable]
public class LoginData
{
    public string Username;
    public string Password;
}

[System.Serializable]
public class UpdateData
{
    public string Email;
    public string NewUsername;
    public string NewPassword;
}