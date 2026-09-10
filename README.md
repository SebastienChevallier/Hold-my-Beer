# Hold My Beer

Jeu multijoueur en première personne, **hébergé par un joueur** (pas de serveur dédié).
Unity 6000.6 · URP · Netcode for GameObjects 2.x · Unity Transport (+ Relay en option).

## Démarrer

1. Ouvrir le projet dans **Unity 6000.6** et laisser les packages se résoudre.
2. Les scènes et prefabs sont générés automatiquement au premier import.
   Au besoin : `Tools > Hold My Beer > Generate Project Assets`.
3. Lancer : `Tools > Hold My Beer > Play From Boot`.

Tester à plusieurs : `Window > Multiplayer > Multiplayer Play Mode`, ou un build + l'éditeur,
en mode **Direct IP** sur `127.0.0.1`.

## Documentation

- [`CLAUDE.md`](CLAUDE.md) — règles de développement multijoueur, à lire avant de coder.
- [`Documentation/ARCHITECTURE.md`](Documentation/ARCHITECTURE.md) — flux complet et
  justification des choix techniques.
