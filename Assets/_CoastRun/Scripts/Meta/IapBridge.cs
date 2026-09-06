using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if COASTRUN_IAP
using UnityEngine.Purchasing;
using Unity.Services.Core;
#endif

namespace CoastRun
{
    /// 결제 브리지 — Unity IAP 5.x(Google Play Billing). 패키지가 있으면 CoastRun.Runtime.asmdef의
    /// versionDefines가 COASTRUN_IAP를 켜서 실제 스토어와 통신하고, 없으면 에디터/개발 빌드 테스트 언락으로 동작.
    /// 상품 ID는 플레이 콘솔의 인앱 상품과 같아야 한다. 부팅 시 Init()을 한 번 부르면 이후 구매·복원이 즉시 가능.
    public static class IapBridge
    {
        public const string AlbumProductId = "coastrun_album_full";   // 비소모성, ₩9,900 / $8.99

        static bool _initStarted;
        static Action<bool> _pendingDone;
        static string _localizedPrice;

#if COASTRUN_IAP
        static StoreController _store;
        static bool _connected, _productsReady;

        public static bool StoreReady => _connected && _productsReady;
#else
        public static bool StoreReady => false;
#endif

        /// 스토어 가격 문자열(있으면). 없으면 null → Collection.PriceLabel 사용.
        public static string LocalizedPrice => _localizedPrice;

        public static void Init()
        {
            if (_initStarted) return;
            _initStarted = true;
#if COASTRUN_IAP
            InitAsync();
#endif
        }

#if COASTRUN_IAP
        static async void InitAsync()
        {
            try
            {
                try { await UnityServices.InitializeAsync(); }
                catch (Exception e) { Debug.Log("[IAP] UGS init skipped: " + e.Message); }

                _store = UnityIAPServices.StoreController();
                _store.OnStoreConnected += () => { _connected = true; Debug.Log("[IAP] store connected"); };
                _store.OnStoreDisconnected += d => { _connected = false; Debug.LogWarning("[IAP] disconnected: " + d.message); };
                _store.OnProductsFetched += OnProductsFetched;
                _store.OnProductsFetchFailed += f => Debug.LogWarning("[IAP] products fetch failed: " + f.FailureReason);
                _store.OnPurchasePending += OnPurchasePending;
                _store.OnPurchaseConfirmed += OnPurchaseConfirmed;
                _store.OnPurchaseFailed += OnPurchaseFailed;
                _store.OnPurchasesFetched += OnPurchasesFetched;
                _store.OnPurchasesFetchFailed += f => Debug.LogWarning("[IAP] purchases fetch failed: " + f.message);

                await _store.Connect();
                _connected = true;
                _store.FetchProducts(new List<ProductDefinition> { new ProductDefinition(AlbumProductId, ProductType.NonConsumable) });
            }
            catch (Exception e)
            {
                Debug.LogWarning("[IAP] init failed: " + e.Message);
            }
        }

        static void OnProductsFetched(List<Product> products)
        {
            _productsReady = true;
            var p = products.FirstOrDefault(x => x.definition.id == AlbumProductId);
            if (p != null && p.metadata != null && !string.IsNullOrEmpty(p.metadata.localizedPriceString))
                _localizedPrice = p.metadata.localizedPriceString;
            Debug.Log("[IAP] products fetched: " + products.Count + " price=" + _localizedPrice);
            // 앱 재설치·기기 변경: 이미 산 사용자는 자동 복원
            _store.FetchPurchases();
        }

        static bool OrderHas(Order o, string id)
        {
            try { return o?.CartOrdered?.Items()?.Any(i => i.Product != null && i.Product.definition.id == id) == true; }
            catch { return false; }
        }

        static void OnPurchasePending(PendingOrder order)
        {
            // 영수증 검증 자리(서버 없음 → 로컬 확정). 권한 지급 후 확정.
            if (OrderHas(order, AlbumProductId)) Collection.GrantAlbum();
            _store.ConfirmPurchase(order);
        }

        static void OnPurchaseConfirmed(Order order)
        {
            bool ok = order is ConfirmedOrder && OrderHas(order, AlbumProductId);
            if (order is FailedOrder f)
            {
                // 이미 소유(중복 거래)도 성공으로 본다
                ok = f.FailureReason == PurchaseFailureReason.DuplicateTransaction && OrderHas(order, AlbumProductId);
                Debug.LogWarning("[IAP] confirm failed: " + f.FailureReason + " " + f.Details);
            }
            if (ok) Collection.GrantAlbum();
            var d = _pendingDone; _pendingDone = null; d?.Invoke(ok);
        }

        static void OnPurchaseFailed(FailedOrder f)
        {
            Debug.LogWarning("[IAP] purchase failed: " + f.FailureReason + " " + f.Details);
            bool ok = f.FailureReason == PurchaseFailureReason.DuplicateTransaction && OrderHas(f, AlbumProductId);
            if (ok) Collection.GrantAlbum();
            var d = _pendingDone; _pendingDone = null; d?.Invoke(ok);
        }

        static void OnPurchasesFetched(Orders orders)
        {
            bool owned = orders.ConfirmedOrders.Any(o => OrderHas(o, AlbumProductId));
            foreach (var p in orders.PendingOrders) if (OrderHas(p, AlbumProductId)) { owned = true; _store.ConfirmPurchase(p); }
            if (owned) Collection.GrantAlbum();
            Debug.Log("[IAP] purchases fetched, album owned=" + owned);
        }
#endif

        public static void Purchase(string productId, Action<bool> done)
        {
            Init();
#if COASTRUN_IAP
            if (StoreReady)
            {
                var product = _store.GetProducts().FirstOrDefault(p => p.definition.id == productId);
                if (product == null) { Debug.LogWarning("[IAP] product missing: " + productId); done?.Invoke(false); return; }
                _pendingDone = done;
                _store.PurchaseProduct(product);
                return;
            }
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[IAP] test purchase granted (no store): " + productId);
            done?.Invoke(true);
#else
            Debug.LogWarning("[IAP] store not ready");
            done?.Invoke(false);
#endif
        }

        public static void Restore(Action<bool> done)
        {
            Init();
            if (Collection.AlbumOwned) { done?.Invoke(true); return; }
#if COASTRUN_IAP
            if (StoreReady)
            {
                _store.RestoreTransactions((ok, msg) =>
                {
                    Debug.Log("[IAP] restore: " + ok + " " + msg);
                    // 실제 소유 여부는 OnPurchasesFetched로 들어온다 — 잠시 뒤 상태 확인
                    if (ok) _store.FetchPurchases();
                    done?.Invoke(Collection.AlbumOwned);
                });
                return;
            }
#endif
            done?.Invoke(PlayerPrefs.GetInt("CoastRun_AlbumOwned", 0) == 1);
        }
    }
}
