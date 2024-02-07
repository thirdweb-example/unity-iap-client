using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public TMP_Text AddressText;
    public TMP_Text BalanceText;
    public TMP_Text LogText;

    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UpdateAddress(string address)
    {
        AddressText.text = $"Player ID: {address}";
    }

    public void UpdateBalance(string balance)
    {
        BalanceText.text = $"Balance: {balance} Tokens";
    }

    public void UpdateLog(string log)
    {
        LogText.text = log;
        Debug.Log(log);
    }
}
