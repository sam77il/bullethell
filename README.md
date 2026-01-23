# Unity Bullethell - Multiplayer 3D Top Down Game

## Projektbeschreibung

Ein Multiplayer 3D Top-Down-Shooter, bei dem Spieler gemeinsam gegen Gegnerwellen kämpfen und überleben müssen.

## Features

### Multiplayer-Funktionalität
- Vollständiges Lobby-System
- Server-Erstellung durch Host
- Passwortgeschützter Beitritt
- Host-Kontrollen (Starten, Spieler kicken)

### Gameplay
- 3D Top-Down-Perspektive
- Gegnerwellen überleben
- Dash-Mechanik für Spieler
- Serverseitige Bullet-Logik (keine Friendly-Fire)

## Technische Implementierung

### Server RPCs

```csharp
private void ShootServerRpc(Vector3 spawnPos, Vector3 shootDirection)
public void RequestServerClose()
public void RequestStartGame()
private void RegisterPlayerServerRpc(string playerName)
public void AddPlayer(Player player, string playerName)
public async void NotifyAllPlayers()
public IEnumerator UpdateServerPlayerCount()
public void CloseGameServerRpc()
private IEnumerator CloseGameInBroadcast()
public void LeaveLobby(Player player)
public void StartGame()
public void TakeDamage(float damage)
public void AddKill()
private void DespawnProjectile()
public void NotifyPlayerSpawned()
```

### Target RPCs

```csharp
public void Disconnect(NetworkConnection conn)
public void UpdateLobby(NetworkConnection conn, List<PlayerData> playersData)
```

### Synchronisierte Variablen

```csharp
public readonly SyncList<PlayerData> PlayerList = new SyncList<PlayerData>();
```

## Lobby-Ablauf

1. Host erstellt einen Server
2. Spieler können mit Passwort beitreten
3. Host kann Spieler kicken
4. Host startet das Spiel

## Spielmechaniken

### Bullets
- Serverseitiges Spawning
- Keine Auswirkung auf Teammitglieder (kein Friendly-Fire)

### Spielerbewegung
- Dash-Fähigkeit verfügbar

### Gegner
- Gegner warten vor Schadensanwendung (über Komponente konfigurierbar)

## Bekannte Einschränkungen

- Spieler können aus der Map laufen (beabsichtigt)
- Gegner-Damage-Delay ist einstellbar

## Technologie-Stack

- Unity Engine
- Unity Fishnet
- C# Scripting
- NodeJs mit Express

---

*Entwickelt als Multiplayer-Projekt mit Fokus auf kooperatives Gameplay*


## Wer hat was gemaht
Lobby: Samil
Broadcast: Samil
Mechanics: Furkan
Wellen-/Gegner Logik: Furkan

Samil Files:
FindGameItem.cs
Game.cs
Leaderboard.cs
LeaderboardItem.cs
LobbyManager.cs
LobbyPlayerItem.cs
MyServerManager.cs
Player.cs
PlayerData.cs

Furkan:
CameraFollow2D_Smooth.cs
Enemy.cs
GameOverManager.cs
HealthDisplay.cs
KillDisplay.cs
Player.cs
Projectile.cs
WaveSpawner.cs