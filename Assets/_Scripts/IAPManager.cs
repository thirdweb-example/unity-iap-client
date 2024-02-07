using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Security;

[System.Serializable]
public enum Environment
{
    Production, // Requires build and proper setup on the publisher console and your server
    Sandbox, // Requires build and proper setup on the publisher console and your server
    Local // Client side minting for editor testing, unsafe for production as it exposes a minter's private key
}

public class IAPManager : MonoBehaviour, IDetailedStoreListener
{
    [SerializeField]
    private Environment currentEnvironment = Environment.Local;

    [SerializeField]
    private string productId = "100_tokens";

    private static IStoreController m_StoreController;
    private static IExtensionProvider m_StoreExtensionProvider;

    #region Initialization

    private async void Start()
    {
        // Initialize UGS
        try
        {
            string env = currentEnvironment == Environment.Production ? "production" : "sandbox";
            var options = new InitializationOptions().SetEnvironmentName(env);
            await UnityServices.InitializeAsync(options);
            UIManager.Instance.UpdateLog("Unity Game Services initialized successfully");
        }
        catch (System.Exception exception)
        {
            UIManager.Instance.UpdateLog($"Unity Game Services failed to initialize: {exception.Message}");
            return;
        }

        // Initialize IAP
        if (m_StoreController == null)
        {
            InitializePurchasing();
        }
    }

    private void InitializePurchasing()
    {
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        builder.AddProduct(productId, ProductType.Consumable);
        UnityPurchasing.Initialize(this, builder);
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        m_StoreController = controller;
        m_StoreExtensionProvider = extensions;
        UIManager.Instance.UpdateLog("OnInitialized: PASS");
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        return;
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        switch (error)
        {
            case InitializationFailureReason.AppNotKnown:
                UIManager.Instance.UpdateLog("Is your App correctly uploaded on the relevant publisher console?");
                break;
            case InitializationFailureReason.PurchasingUnavailable:
                UIManager.Instance.UpdateLog("Billing disabled!");
                break;
            case InitializationFailureReason.NoProductsAvailable:
                UIManager.Instance.UpdateLog("No products available for purchase!");
                break;
        }
    }

    #endregion

    #region Purchase

    public void OnPurchaseClicked()
    {
        BuyProductID(productId);
    }

    private void BuyProductID(string productId)
    {
        if (m_StoreController != null && m_StoreExtensionProvider != null)
        {
            Product product = m_StoreController.products.WithID(productId);
            if (product != null && product.availableToPurchase)
            {
                UIManager.Instance.UpdateLog($"Purchasing product asychronously: '{product.definition.id}'");
                m_StoreController.InitiatePurchase(product);
            }
            else
            {
                UIManager.Instance.UpdateLog("BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");
            }
        }
        else
        {
            UIManager.Instance.UpdateLog("BuyProductID FAIL. Not initialized.");
        }
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        return;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        UIManager.Instance.UpdateLog($"OnPurchaseFailed: FAIL. Product: '{product.definition.storeSpecificId}', PurchaseFailureReason: {failureDescription}");
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
    {
        bool validPurchase = true; // Presume valid for platforms with no R.V.
        IPurchaseReceipt[] result = new IPurchaseReceipt[0];

        // Unity IAP's validation logic is only included on these platforms.
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_OSX)
        // Prepare the validator with the secrets we prepared in the Editor obfuscation window.
        var validator = new CrossPlatformValidator(GooglePlayTangle.Data(), AppleTangle.Data(), Application.identifier);

        try
        {
            // On Google Play, result has a single product ID.
            // On Apple stores, receipts contain multiple products.
            result = validator.Validate(e.purchasedProduct.receipt);
            // For informational purposes, we list the receipt(s)
            Debug.Log("Receipt is valid. Contents:");
            foreach (IPurchaseReceipt productReceipt in result)
            {
                Debug.Log(productReceipt.productID);
                Debug.Log(productReceipt.purchaseDate);
                Debug.Log(productReceipt.transactionID);
            }
        }
        catch (IAPSecurityException)
        {
            Debug.Log("Invalid receipt, not unlocking content");
            validPurchase = false;
        }
#endif

        if (validPurchase)
        {
            Debug.Log("Receipt is valid. Contents:");
            foreach (IPurchaseReceipt productReceipt in result)
            {
                Debug.Log(productReceipt.productID);
                Debug.Log(productReceipt.purchaseDate);
                Debug.Log(productReceipt.transactionID);

                GooglePlayReceipt google = productReceipt as GooglePlayReceipt;
                if (null != google)
                {
                    // This is Google's Order ID.
                    // Note that it is null when testing in the sandbox
                    // because Google's sandbox does not provide Order IDs.
                    Debug.Log(google.transactionID);
                    Debug.Log(google.purchaseState);
                    Debug.Log(google.purchaseToken);
                }

                AppleInAppPurchaseReceipt apple = productReceipt as AppleInAppPurchaseReceipt;
                if (null != apple)
                {
                    Debug.Log(apple.originalTransactionIdentifier);
                    Debug.Log(apple.subscriptionExpirationDate);
                    Debug.Log(apple.cancellationDate);
                    Debug.Log(apple.quantity);
                }
            }

            UIManager.Instance.UpdateLog("Purchase successful");
            HandlePostPurchase(result.Length > 0 ? result[0] : null);
        }
        else
        {
            UIManager.Instance.UpdateLog("Invalid purchase");
        }

        return PurchaseProcessingResult.Complete;
    }

    #endregion

    #region Post Purchase Flow

    private async void HandlePostPurchase(IPurchaseReceipt receipt)
    {
        // Your post purchase flow here
        try
        {
            UIManager.Instance.UpdateLog("Awarding tokens...");
            await BlockchainManager.Instance.AwardTokens(receipt, currentEnvironment);
            UIManager.Instance.UpdateLog("Awarded tokens successfully");
        }
        catch (System.Exception exception)
        {
            UIManager.Instance.UpdateLog($"Failed to award tokens: {exception.Message}");
        }
    }

    #endregion
}
