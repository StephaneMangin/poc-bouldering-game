# 🧗 ClimbingGym Unity — Prises & Volumes Block'Out

Package Unity pour la création de salles d'escalade indoor (style Block'Out).
Inspiré des photos de la salle avec ses couleurs teal/orange caractéristiques.

---

## 📦 Contenu du package

### Prises d'escalade — 31 prefabs

| Catégorie    | Prefabs | Description technique |
|--------------|---------|----------------------|
| **Crimps**   | 4       | Réglettes 5mm→25cm, rail |
| **Slopers**  | 4       | Bombées ronde, plate, facettes, paume |
| **Jugs**     | 4       | Confortables, rail 30cm |
| **Pinches**  | 3       | Pince étroite, large, ronde |
| **Pockets**  | 3       | Mono/2/3 doigts |
| **Sidepulls**| 4       | Gauche, droite, sous-cotée, gaston |
| **Edges**    | 3       | Arête vive, arrondie, inclinée |
| **Specials** | 6       | Talonnette, pointe, compression ×2, tonneau, jug triangle |

### Volumes muraux — 13 prefabs

| Catégorie    | Prefabs | Taille indicative |
|--------------|---------|-------------------|
| **Triangles**| 3       | S / M / L (0.4m → 1.1m) |
| **Wedges**   | 3       | Coins bas, haut, angle |
| **Cubes**    | 2       | 30cm / 60cm |
| **Hexagons** | 2       | Plat / Tall |
| **Complex**  | 3       | Diamant, Pyramide, Prisme multi-faces |

### Matériaux — 9 couleurs

`mat_black` · `mat_blue` · `mat_yellow` · `mat_red` · `mat_orange`
`mat_green` · `mat_white` · `mat_teal` · `mat_purple`

---

## 🗂 Structure de dossiers

```
Assets/ClimbingGym/
├── Holds/
│   ├── Crimps/
│   │   ├── hold_crimp_micro.prefab
│   │   ├── hold_crimp_standard.prefab
│   │   ├── hold_crimp_deep.prefab
│   │   └── hold_crimp_rail.prefab
│   ├── Slopers/
│   ├── Jugs/
│   ├── Pinches/
│   ├── Pockets/
│   ├── Sidepulls/
│   ├── Edges/
│   └── Specials/
├── Volumes/
│   ├── Triangles/
│   ├── Wedges/
│   ├── Cubes/
│   ├── Hexagons/
│   └── Complex/
├── Materials/
│   └── Colors/          ← 9 fichiers .mat
└── Scripts/
    ├── Data/
    │   ├── ClimbingHoldData.cs    ← ScriptableObject prise
    │   └── ClimbingVolumeData.cs  ← ScriptableObject volume
    ├── Runtime/
    │   └── ClimbingHold.cs        ← Composant MonoBehaviour
    ├── Mesh/
    │   ├── ProceduralHoldMesh.cs  ← Générateur mesh prises
    │   └── ProceduralVolumeMesh.cs ← Générateur mesh volumes
    └── Editor/
        └── HoldPrefabGenerator.cs ← Outil éditeur Unity
```

---

## 🚀 Utilisation dans Unity

### Méthode 1 — Outil éditeur (recommandé)

1. Copiez tout le dossier `Assets/ClimbingGym/` dans votre projet Unity
2. Attendez que Unity compile les scripts
3. Dans le menu Unity : **Tools → ClimbingGym → Generate All Prefabs**
4. Les prefabs sont créés avec meshes procéduraux, colliders et matériaux

### Méthode 2 — Script Python (pré-génération)

```bash
python3 generate_prefabs.py --output-dir ./Assets/ClimbingGym
```
Les fichiers `.prefab` YAML sont générés directement, prêts à être importés.

### Méthode 3 — Prefabs directs

Glissez-déposez les `.prefab` depuis `Assets/ClimbingGym/Holds/` ou `Volumes/`
directement dans votre scène.

---

## 🎮 API Runtime

```csharp
// Récupérer une prise
var hold = GetComponent<ClimbingHold>();

// Propriétés
Debug.Log(hold.holdType);      // HoldType.Crimp
Debug.Log(hold.difficulty);    // HoldDifficulty.Advanced
Debug.Log(hold.IsFootHold);    // false

// Point de préhension en WorldSpace
Vector3 grip = hold.GetGripPoint();
Quaternion orientation = hold.GetGripRotation();

// Events
hold.OnGrab    += (h) => Debug.Log($"Grabbed {h.holdName}");
hold.OnRelease += (h) => animator.SetBool("isHolding", false);

// Déclencher manuellement
hold.Grab();
hold.Release();
```

---

## 🔧 Nommage

### Convention snake_case

```
hold_{type}_{variante}
volume_{forme}_{taille_ou_variante}
mat_{couleur}
```

### Exemples

| Prefab | Signification |
|--------|---------------|
| `hold_crimp_micro` | Réglette très petite, avancé |
| `hold_jug_rail` | Jug en rail horizontal |
| `hold_pocket_two_finger` | Trou 2 doigts |
| `volume_triangle_large` | Grand triangle mural |
| `volume_wedge_corner` | Coin d'angle |
| `mat_teal` | Couleur vert-bleu Block'Out |

---

## 🎨 Palette couleurs Block'Out

Inspiré des photos de la salle :

- 🟦 **Teal** `#00A6A6` — couleur dominante des volumes
- 🟧 **Orange** `#FF7300` — accents et zones de difficulté
- 🟥 **Rouge** — prises difficiles
- 🟨 **Jaune** — prises débutant
- ⬛ **Noir** — prises avancées / compétition

---

## 📋 Compatibilité

- Unity 2021.3 LTS et supérieur
- URP et Built-in Render Pipeline (shader auto-détecté)
- C# 9.0+ (records utilisés dans le script Editor)
- Pas de dépendances externes

---

## 📝 Généré par

`generate_prefabs.py` — Script Python autonome
Modèles inspirés de la salle Block'Out (photos 2016-2020)
