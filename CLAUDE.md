# Hold My Beer — guide de développement

Jeu multijoueur **peer-to-peer hébergé par un joueur** (pas de serveur dédié), Unity **6000.6** / URP,
**Netcode for GameObjects 2.x** + **Unity Transport**, avec **Unity Relay** en option.

Ce document est la référence pour tout développement multijoueur sur ce projet.
Lis-le avant d'ajouter la moindre fonctionnalité réseau.

---

## 1. Démarrage

```
1. Ouvrir le projet dans Unity 6000.6 (les packages se résolvent au premier import).
2. Les scènes et prefabs se génèrent automatiquement au premier lancement.
   Sinon : Tools > Hold My Beer > Generate Project Assets
3. Jouer : Tools > Hold My Beer > Play From Boot
```

**Toujours entrer en Play depuis la scène `Boot`.** Les autres scènes détectent
l'absence de bootstrap et le signalent dans la console au lieu de planter en silence.

Tester à plusieurs sur une machine : `Window > Multiplayer > Multiplayer Play Mode`
(virtual players), ou un build + l'éditeur, en mode **Direct IP** sur `127.0.0.1`.

---

## 2. Topologie : host-client, pas de serveur dédié

Un joueur lance `StartHost()` : son process est **serveur *et* client** en même temps.
Les autres font `StartClient()`.

```
        ┌──────────────────────────────┐
        │  Process du HOST             │
        │  ┌────────┐    ┌──────────┐  │
        │  │ Server │◄──►│ Client 0 │  │   ← autorité
        │  └───┬────┘    └──────────┘  │
        └──────┼───────────────────────┘
               │ UDP direct  ou  Relay
        ┌──────┴────────┬───────────────┐
   ┌────▼─────┐   ┌─────▼────┐    ┌─────▼────┐
   │ Client 1 │   │ Client 2 │    │ Client N │
   └──────────┘   └──────────┘    └──────────┘
```

Conséquences à ne jamais oublier :

- `IsServer` est **vrai chez le host**, qui est aussi un joueur. Un code serveur qui
  suppose « pas de joueur local » est faux ici.
- Si le host quitte, **la session meurt**. C'est assumé (pas de host migration).
- Le host a 0 ms de latence : ne jamais valider un ressenti de jeu uniquement chez lui.

### Les deux modes de connexion

| Mode | Quand | Prérequis |
|---|---|---|
| **Direct IP** | LAN, dev quotidien, tests solo sur `127.0.0.1` | aucun |
| **Relay** | jouer entre amis via internet, sans ouvrir de port | projet lié à Unity Gaming Services (offre gratuite) |

Le code de jeu **ne sait pas** lequel est actif : il ne parle qu'à `INetworkSessionService`.

---

## 3. Architecture

### Scènes

| Scène | Rôle | Chargée par |
|---|---|---|
| `Boot` | composition root : construit tous les services, puis passe au menu | démarrage |
| `Menu` | menu principal **et** lobby (deux écrans, une seule scène) | `SceneLoader` (local) |
| `Game` | terrain + points de spawn | `NetworkSceneManager` (**réseau**) |

> Menu et Lobby partagent une scène : le lobby n'a aucun contenu 3D, et éviter un
> chargement de scène pendant la poignée de main réseau supprime toute une classe de
> bugs de timing.

### Assemblies (`.asmdef`) — le sens des dépendances

```
                 HoldMyBeer.App          ← composition root, seul à connaître les types concrets
                  │   │   │   │
      ┌───────────┘   │   │   └────────────┐
      ▼               ▼   ▼                ▼
HoldMyBeer.UI   HoldMyBeer.Gameplay   HoldMyBeer.Player
      │               │
      └───────┬───────┘
              ▼
     HoldMyBeer.Networking ──► HoldMyBeer.Networking.Relay (optionnel, supprimable)
              │
              ▼
        HoldMyBeer.Core        ← aucune dépendance projet
```

Règle : **les flèches ne remontent jamais.** Si tu as besoin de l'inverse, c'est qu'il
manque une interface dans la couche du dessous.

### Composition root

`GameBootstrapper` (scène `Boot`) est le **seul** endroit qui fait `new` sur des types
concrets. Tout le reste reçoit des interfaces via `AppServices.Container`.

```csharp
// Dans un MonoBehaviour, une seule fois, dans Start/Awake — jamais dans Update :
var session = AppServices.Container.Resolve<INetworkSessionService>();
```

---

## 4. Les règles du multijoueur sur ce projet

### 4.1 Autorité

**Le serveur décide, le client demande.** Un client n'écrit jamais un état partagé.

```csharp
// ✅ le client exprime une intention
[Rpc(SendTo.Server)]
private void SetReadyRpc(bool ready, RpcParams rpcParams = default)
{
    var sender = rpcParams.Receive.SenderClientId;   // ← identité fiable, fournie par NGO
    ApplyReady(sender, ready);                        // ← le serveur applique
}
```

```csharp
// ❌ ne jamais faire confiance à un id envoyé par le client
[Rpc(SendTo.Server)]
private void SetReadyRpc(ulong clientId, bool ready) { /* n'importe qui usurpe n'importe qui */ }
```

Toujours vérifier l'autorité côté serveur, même pour une action « réservée au host » :
`if (rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId) return;`

**Exception assumée : le mouvement du joueur.** `OwnerNetworkTransform` est
*owner-authoritative* — chacun simule son avatar et réplique le résultat. C'est
réactif, simple, et trichable. Acceptable entre amis ; à remplacer par un motor
serveur + réconciliation si le jeu devient compétitif. Le calcul de mouvement est
déjà isolé dans `FirstPersonMotor`, précisément pour rendre ce basculement local.

### 4.2 Choisir son outil de réplication

| Besoin | Outil | Coût |
|---|---|---|
| Valeur qui change et que les nouveaux arrivants doivent connaître (score, état) | `NetworkVariable<T>` | réplique l'état, synchro à la connexion |
| Collection répliquée (liste de joueurs) | `NetworkList<T>` | idem, `T` doit être `unmanaged` |
| Événement ponctuel (tir, son, effet) | `[Rpc(...)]` | rien n'est stocké, un retardataire ne le verra jamais |

Piège classique : envoyer un RPC pour synchroniser un état. Un client qui se connecte
après ne le recevra pas. **État → NetworkVariable. Événement → RPC.**

### 4.3 Types réplicables

`NetworkList<T>` / `NetworkVariable<T>` exigent des types **unmanaged** :
pas de `string`, pas de classe. Utiliser `FixedString32Bytes` (29 octets utiles !)
et implémenter `INetworkSerializable` **et** `IEquatable<T>` — voir `LobbyPlayer`.

### 4.4 Cycle de vie

Sur un `NetworkBehaviour`, ne jamais utiliser `Start()` pour de la logique réseau :

```csharp
public override void OnNetworkSpawn()    // ← ici l'objet est réplicable, IsOwner/IsServer sont fiables
public override void OnNetworkDespawn()  // ← se désabonner ici, systématiquement
```

Objets **spawnés dynamiquement** (`Spawn()`) : survivent au changement de scène
(NGO les place en DontDestroyOnLoad). C'est pour ça que `LobbyState` reste vivant du
menu jusqu'au jeu. Objets **placés dans une scène** : détruits avec elle.

### 4.5 Prefabs réseau

Tout prefab spawné doit :
1. porter un `NetworkObject` ;
2. être listé dans `Prefabs/HoldMyBeerNetworkPrefabs.asset` ;
3. être **identique** chez tout le monde — la liste est ordonnée et hashée.

Un prefab manquant côté client = déconnexion sèche avec un message obscur.

### 4.6 Connexion et approbation

Toute connexion passe par `ConnectionApprovalHandler`, qui applique des
`IConnectionApprovalPolicy`. Ajouter une règle = ajouter une classe, sans toucher au
reste (`MaxPlayersPolicy`, `BuildVersionPolicy`, `LobbyOpenPolicyAdapter`).

Le payload de connexion est **plafonné à 1024 octets** par défaut et n'est **pas de
confiance** : il est re-nettoyé côté serveur (`PlayerPrefsPlayerProfile.Sanitize`).

### 4.7 Changement de scène

En session, **jamais** `SceneManager.LoadScene`. Le serveur appelle :

```csharp
AppServices.Container.Resolve<INetworkSessionService>().LoadNetworkScene(SceneNames.Game);
```

Tous les clients suivent. Le spawn des joueurs attend `OnLoadEventCompleted`, sinon on
spawne pour des clients qui n'ont pas encore la scène.

---

## 5. Ajouter une fonctionnalité — recettes

**Un nouveau mode de transport (Steam, LAN discovery…)**
1. Implémenter `ISessionTransport`.
2. Implémenter `ISessionTransportInstaller` dans la même assembly.
3. Rien d'autre : le bootstrap le découvre par réflexion.

**Une nouvelle règle d'entrée** → une classe `IConnectionApprovalPolicy`, ajoutée dans
`GameBootstrapper.BuildContainer()`.

**Un objet de jeu répliqué** → prefab + `NetworkObject`, ajouté à la
`NetworkPrefabsList`, spawné **par le serveur** avec `.Spawn()`.

**Une politique de spawn différente** → implémenter `ISpawnPointProvider` ;
`PlayerSpawner` n'a pas à changer.

---

## 6. Conventions de code

- **SOLID**, appliqué avec discernement : une interface quand il y a une vraie raison
  de substituer (transport, spawn, input), pas par réflexe.
- `private` par défaut, `readonly` dès que possible, `sealed` sur les classes non
  destinées à l'héritage.
- Champs privés : `_camelCase`. Champs `[SerializeField]` : `camelCase`.
- Une classe = un fichier, du même nom.
- Pas de singleton, à l'exception d'`AppServices` (documentée dans le fichier) et de
  `NetworkManager.Singleton` (imposé par NGO).
- Pas de `Resolve<T>()` ni de `GetComponent` dans `Update`.
- Les commentaires expliquent **pourquoi**, jamais **quoi**.

---

## 7. Pièges déjà rencontrés (et traités)

| Symptôme | Cause | Où c'est géré |
|---|---|---|
| « No cameras rendering » | scènes générées vides | caméras créées dans `Boot`/`Menu` |
| Le client rejoint mais reste bloqué | `StartClient()` ne signale pas l'échec d'approbation | `OnClientStopped` + `DisconnectReason` |
| Le lobby disparaît en lançant la partie | objet de scène détruit au load | lobby **spawné dynamiquement** |
| Un joueur n'a pas d'avatar | spawn avant que sa scène soit prête | attente d'`OnLoadEventCompleted` |
| La souris tourne 10× trop vite | `Mouse.delta` multiplié par `deltaTime` | non multiplié dans `KeyboardMousePlayerInputSource` |
| Le host démarre mais personne ne rejoint | pare-feu / port 7777 fermé | passer en mode **Relay** |

---

## 8. État actuel et limites assumées

Fait : boot, menu, lobby répliqué avec ready/start, chargement réseau de la scène de
jeu, spawn des joueurs, contrôleur FPS, Direct IP + Relay.

Non fait (volontairement) : host migration, reconnexion, voix, anti-triche,
persistance, interpolation avancée, UI en prefabs (l'UI est construite par code, voir
`UiFactory` — le remplacement est localisé dans ce seul fichier).
