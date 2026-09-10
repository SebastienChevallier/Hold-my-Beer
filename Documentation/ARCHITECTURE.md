# Architecture — Hold My Beer

Complément de `CLAUDE.md` : ce document explique **pourquoi** chaque brique existe et
comment le flux se déroule bout en bout.

---

## 1. Flux complet, du lancement à la partie

```
   [Boot]
      │  GameBootstrapper.Awake()
      │    ├─ ServiceContainer
      │    ├─ SceneLoader, PlayerProfile
      │    ├─ découverte des ISessionTransport (DirectIp, Relay…)
      │    ├─ NetworkSessionService
      │    ├─ ConnectionApprovalHandler + policies
      │    └─ LobbyLauncher, PlayerSpawner
      ▼
   [Menu] ── écran MainMenu ──────────────────────────────┐
      │        │                                          │
      │   CREATE│                                     JOIN │
      │        ▼                                          ▼
      │  session.HostAsync()                     session.JoinAsync()
      │  → transport.ConfigureHostAsync()        → ConfigureClientAsync()
      │  → NetworkManager.StartHost()            → StartClient()
      │  → lobbyLauncher.SpawnLobby()                     │
      │        │                                          │
      │        │                    ┌── approbation serveur (policies)
      │        │                    │   ↳ refus → DisconnectReason → retour menu
      │        ▼                    ▼
      └── écran Lobby ◄──── LobbyNetworkService répliqué
               │
               │  host : START (si tous prêts)   /   clients : READY
               ▼
      StartMatchRpc (serveur)
               ├─ _isOpen = false        ← ferme la porte aux retardataires
               └─ LoadNetworkScene(Game) ← NetworkSceneManager, tout le monde suit
                        ▼
                     [Game]
                GameSceneBootstrap (serveur uniquement)
                   ↳ attend OnLoadEventCompleted
                   ↳ PlayerSpawner.SpawnAllConnectedPlayers()
                        ↳ pose fournie par ISpawnPointProvider
                        ↳ NetworkObject.SpawnAsPlayerObject(clientId)
                             ↳ chez le propriétaire : caméra + input actifs
                             ↳ chez les autres : transform répliqué
```

---

## 2. Décisions et justifications

### Un conteneur de services plutôt que des singletons
Chaque système singleton devient à terme un nœud de dépendances impossible à tester.
Ici, un seul point statique (`AppServices`) expose un conteneur ; tout le reste
dépend d'interfaces. C'est le compromis entre un vrai framework DI (VContainer) et le
`FindObjectOfType` généralisé. Migrer vers VContainer ne toucherait que
`GameBootstrapper`.

### Le transport derrière une stratégie
`Direct IP` et `Relay` n'ont ni les mêmes prérequis, ni la même UX (adresse IP contre
code d'invitation), ni la même fiabilité. Les enfermer dans un `if` aurait contaminé
l'UI et la session. `ISessionTransport` isole la seule chose qui diffère réellement :
**configurer le transport avant `StartHost`/`StartClient`**.

La découverte par `ISessionTransportInstaller` va plus loin : l'assembly Relay peut
être supprimée du projet sans qu'aucune autre ligne ne change. C'est ce qui permet de
livrer un build LAN sans dépendance à Unity Gaming Services.

### L'échec réseau est une valeur, pas une exception
`SessionResult` porte `Success` / `Error`. Une connexion échoue constamment et pour
des raisons banales (mauvaise IP, port fermé, session pleine, version différente) :
ce n'est pas exceptionnel, c'est un cas nominal que l'UI doit afficher.

### Le lobby est un objet réseau, pas un manager de scène
`LobbyNetworkService` est spawné dynamiquement par le host. Deux bénéfices :
il est répliqué automatiquement à chaque nouveau client (état complet, sans RPC de
synchronisation à écrire), et il survit au changement de scène.

`ILobbyProvider` existe parce que le lobby **n'existe pas au démarrage** : l'UI ne
peut pas le résoudre une fois pour toutes, elle s'abonne à son apparition.

### Le mouvement est séparé du réseau
`FirstPersonMotor` ne connaît ni NGO ni l'Input System : il transforme des entrées en
déplacement de `CharacterController`. `PlayerController` fait la colle
(propriétaire ? caméra ? curseur ?). Passer un jour en autorité serveur consiste à
appeler le même motor depuis le serveur avec des inputs répliqués — sans réécrire la
physique du joueur.

### L'UI est construite par code
Le projet a été écrit sans éditeur Unity disponible : une scène ou un prefab d'UI
écrit à la main hors éditeur est un fichier cassé (GUID inventés). L'UI générée par
`UiFactory` est du placeholder fonctionnel et sans conflit de merge. Les écrans
implémentent `IScreen` et ne parlent qu'à des interfaces de service : les remplacer
par des prefabs ou de l'UI Toolkit ne touche pas la logique.

### Les scènes et prefabs sont générés
`ProjectAssetGenerator` construit `Boot`, `Menu`, `Game`, le prefab joueur, le prefab
de lobby et la `NetworkPrefabsList`, puis renseigne les Build Settings. Même raison :
c'est reproductible et vérifiable. Une fois générés, ces assets sont des assets
ordinaires, à committer et à éditer normalement.

---

## 3. Carte des fichiers

```
Assets/HoldMyBeer/
├── Runtime/
│   ├── Core/            aucune dépendance projet
│   │   ├── AppServices, ServiceContainer      accès aux services
│   │   ├── SceneLoader, CoroutineRunner       chargement local
│   │   ├── PlayerPrefsPlayerProfile           pseudo local + sanitisation
│   │   └── SceneNames                         constantes de scènes
│   ├── Networking/
│   │   ├── NetworkSessionService              cycle de vie de la session
│   │   ├── ISessionTransport (+ DirectIp)     stratégie de connexion
│   │   ├── TransportInstallerScanner          découverte des transports
│   │   ├── ConnectionApprovalHandler          portier serveur
│   │   ├── LobbyNetworkService, LobbyPlayer   lobby répliqué
│   │   ├── LobbyLauncher, LobbyProvider       création et exposition du lobby
│   │   └── Relay/                             assembly optionnelle
│   ├── Gameplay/
│   │   ├── PlayerSpawner                      spawn serveur
│   │   ├── SpawnPointRegistry                 marqueurs de scène
│   │   └── GameSceneBootstrap                 entrée de la scène Game
│   ├── Player/
│   │   ├── FirstPersonMotor                   maths pures, testables
│   │   ├── PlayerController                   colle réseau/caméra/input
│   │   ├── KeyboardMousePlayerInputSource     entrées
│   │   └── OwnerNetworkTransform              réplication owner-authoritative
│   ├── UI/
│   │   ├── UiFactory                          construction uGUI
│   │   ├── MainMenuScreen, LobbyScreen        écrans
│   │   └── MenuSceneController                orchestration de la scène Menu
│   └── App/
│       ├── GameBootstrapper                   composition root
│       └── LobbyOpenPolicyAdapter             règle d'entrée dépendant du lobby
└── Editor/
    ├── ProjectAssetGenerator                  génère scènes et prefabs
    └── ProjectSetupOnLoad                     exécution au premier import
```

---

## 4. Évolutions prévues et où les brancher

| Évolution | Point d'accroche |
|---|---|
| Serveur autoritaire | appeler `FirstPersonMotor` côté serveur, `OwnerNetworkTransform` → `NetworkTransform` |
| Reconnexion | clé stable dans `ConnectionPayload` + table `sessionId → clientId` dans le handler d'approbation |
| Lobby public / matchmaking | `com.unity.services.lobby` derrière un `ISessionTransport` + un service de listing |
| Chat, emotes | RPC sur `LobbyNetworkService` (événements, donc RPC et non NetworkVariable) |
| Vraie UI | remplacer `UiFactory` ; `IScreen` et les services ne bougent pas |
| Modes de jeu | un `IGameMode` résolu par `GameSceneBootstrap`, spawn délégué |
