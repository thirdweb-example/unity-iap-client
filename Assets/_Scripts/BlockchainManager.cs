using System.Threading.Tasks;
using Thirdweb;
using UnityEngine;
using UnityEngine.Events;
using Thirdweb.Redcode.Awaiting;
using System.Net.Http;
using Newtonsoft.Json;
using UnityEngine.Purchasing.Security;
using System.Numerics;

[System.Serializable]
public class Result<T>
{
    public T result;
}

public class BlockchainManager : MonoBehaviour
{
    [SerializeField]
    private string tokenAddress = "0x33D1a021aFbE0CFB0AC7CcB7c5A247777b3e7c50";

    [SerializeField]
    private string serverPath = "http://localhost:8000/engine/validate";

    public UnityEvent<string> OnConnected;

    public static BlockchainManager Instance { get; private set; }

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

    public async void OnLoginClicked()
    {
        UIManager.Instance.UpdateLog("Connecting to wallet...");
        var chainId = await ThirdwebManager.Instance.SDK.wallet.GetChainId();
        var connection = new WalletConnection(provider: WalletProvider.SmartWallet, chainId: chainId, personalWallet: WalletProvider.LocalWallet);
        var addy = await ThirdwebManager.Instance.SDK.wallet.Connect(connection);
        UIManager.Instance.UpdateAddress(addy);
        UpdateBalancePeriodically(addy);
        OnConnected?.Invoke(addy);
        UIManager.Instance.UpdateLog($"Connected to: {addy}");
    }

    private async void UpdateBalancePeriodically(string address)
    {
        while (Application.isPlaying)
        {
            var balance = await FetchTokenBalance(address);
            UIManager.Instance.UpdateBalance(balance);
            await new WaitForSeconds(5f);
        }
    }

    public async Task<string> FetchTokenBalance(string address)
    {
        var contract = ThirdwebManager.Instance.SDK.GetContract(tokenAddress);
        var balance = await contract.ERC20.BalanceOf(address);
        return balance.displayValue;
    }

    public async Task AwardTokens(IPurchaseReceipt receipt, Environment currentEnvironment)
    {
        // Get reference to contract

        var contract = ThirdwebManager.Instance.SDK.GetContract(tokenAddress);
        string receivingAddress = await ThirdwebManager.Instance.SDK.wallet.GetAddress();
        var currentBalance = await contract.ERC20.BalanceOf(receivingAddress);

        // Communicate with server to validate receipt and award tokens
        if (currentEnvironment == Environment.Production || currentEnvironment == Environment.Sandbox)
        {
            using var client = new System.Net.Http.HttpClient();
            var json = JsonConvert.SerializeObject(new { receipt = new { receiptData = receipt }, toAddress = receivingAddress, });
            Debug.Log(json);
            var httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(serverPath, httpContent);
            var responseContent = await response.Content.ReadAsStringAsync();
            Debug.Log(responseContent);
            if (!response.IsSuccessStatusCode)
                throw new System.Exception("Failed to award tokens: " + responseContent);
        }
        // Use signature minting for testing locally, requires funds or gasless smart wallet, do not do this in production unless the generation of the signature is done server side
        else if (currentEnvironment == Environment.Local)
        {
            // FOR TESTING PURPOSES ONLY - DO NOT DO THIS IN PRODUCTION - DO NOT SIGN PAYLOAD CLIENT SIDE
            ERC20MintPayload payload = new(receivingAddress, "100");
            // 0xbB7A354246111849b1e89Eb88ed6DB6A12b92cbC has minter role permissions on this contract
            string minterPrivateKey = "e6ed37d3f0ac5bc7778b82692b1df74d6bae332cd8ad03113e70f7b8fa4cac27";
            ERC20SignedPayload signedPayload = await contract.ERC20.signature.Generate(payload, minterPrivateKey);
            var tx = await contract.ERC20.signature.Mint(signedPayload);
            Debug.Log(tx.ToString());
            // FOR TESTING PURPOSES ONLY - DO NOT DO THIS IN PRODUCTION - DO NOT SIGN PAYLOAD CLIENT SIDE
        }
        else
        {
            throw new System.Exception("Invalid environment!");
        }

        // Wait for balance to update

        UIManager.Instance.UpdateLog("Validated successfully! Updating balance...");
        BigInteger oldBalance = BigInteger.Parse(currentBalance.value);
        BigInteger newBalance = 0;
        while (newBalance <= oldBalance)
        {
            var updatedBalance = await contract.ERC20.BalanceOf(receivingAddress);
            newBalance = BigInteger.Parse(updatedBalance.value);
            await new WaitForSeconds(1f);
        }

        // Mint tokens
        UIManager.Instance.UpdateLog("Balance updated successfully! Awarded tokens.");
    }
}
