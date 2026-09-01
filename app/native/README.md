# Murim — Application Windows native

Premier client natif de Murim, indépendant de `game.html`.

## Construction

Prérequis : Visual Studio 2022 avec le composant **Desktop development with C++** et CMake.

```powershell
cmake -S app/native -B build/native -A x64
cmake --build build/native --config Release --parallel
```

Le binaire est produit dans `build/native/Release/Murim.exe`.

## Socle de jeu

- Nouvelle vie sans dépendance au moteur web.
- Personnage à la naissance.
- Corps, esprit, trait et Qi de départ.
- Règle bébé : pas de déplacement autonome avant l'âge approprié.
- Avancement du temps par mois.
- PNJ du voisinage dans le moteur C++.
- Sauvegarde et chargement locaux.
- Plein écran.
- Journal de chronique.

Les catalogues massifs et les portraits sont volontairement chargés plus tard, à la demande, pour garder un démarrage instantané et éviter les freezes rencontrés dans la version web.
