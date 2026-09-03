# Core Grind Loop

```
장애물 NearMiss → 코인 → 스탯 업그레이드(지수 비용) → MaxSpeed↑ → 송전탑 도달
```

## Scripts

| 시스템 | 경로 |
|--------|------|
| NearMiss | `Scripts/Economy/NearMissSystem.cs`, `NearMissZone.cs`, `ObstacleHazard.cs` |
| Wallet | `Scripts/Economy/CoinWallet.cs` |
| Spawner | `Scripts/Economy/ObstacleSpawner.cs` |
| Upgrades | `Scripts/Progression/UpgradeManager.cs`, `UpgradeConfig.cs` |
| Destination | `Scripts/Progression/DestinationGate.cs` |
| UI | `Scripts/UI/UI_FeedbackController.cs` |

## Collider setup

```
Obstacle root
 ├─ Visual
 ├─ HardHit   CapsuleCollider (trigger, tight) + ObstacleHazard
 └─ NearMiss  CapsuleCollider (trigger, wide)  + NearMissZone
```

Player: kinematic Rigidbody + CapsuleCollider.  
NearMiss 통과 후 HardHit 없이 Exit → 코인.

## Upgrade cost

`cost(level) = baseCost × growth^level`  
예: MaxSpeed base 25, growth 1.45

## Editor hotkeys (임시 상점)

| Key | Stat |
|-----|------|
| U | MaxSpeed |
| I | CoinMultiplier |
| O | MagnetRadius |

## DOTween

프로젝트에 DOTween 없음 → `SimpleTween` 코루틴 사용.  
DOTween 추가 시 `UI_FeedbackController`의 `SimpleTween.MoveFade`만 교체하면 됨.
