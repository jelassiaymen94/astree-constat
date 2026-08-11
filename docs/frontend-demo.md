# Démonstration du frontend

## Préparation recommandée

### 1. Choisir le fournisseur IA

Pour une démonstration stable sans dépendance réseau, utiliser dans `.env` :

```dotenv
LLM_PROVIDER="deterministic"
```

Pour montrer la génération réelle, utiliser `groq` et vérifier la clé avant la présentation. Conserver le mode déterministe comme solution de secours.

### 2. Démarrer les services

Depuis la racine du dépôt :

```powershell
.\start.cmd
```

Puis, dans une seconde fenêtre :

```powershell
.\frontend\start.cmd
```

Ouvrir `http://localhost:5173`.

### 3. Vérifications avant présentation

- `http://localhost:5294/api/health/database` indique que SQL Server est connecté.
- `http://localhost:5294/api/health/ai` indique que FastAPI est connecté.
- La liste des sinistres apparaît dans le frontend.
- Un dossier connu s’ouvre correctement.
- Une synthèse peut être générée et apparaît dans l’historique.
- Aucun secret ou contenu sensible n’est visible à l’écran.

## Parcours pour l’encadrant

1. Montrer la liste, la recherche, les filtres et la pagination.
2. Ouvrir un dossier et expliquer le contexte consolidé.
3. Générer une synthèse avec une instruction courte.
4. Montrer le modèle, la version du prompt, la durée et l’historique.
5. Expliquer la séparation React → .NET → FastAPI → fournisseur IA → journalisation SQL.

## Parcours pour un employé ASTREE

1. Rechercher un dossier par sa référence.
2. Lire les informations du sinistre, de l’assuré, du contrat et du véhicule.
3. Choisir « Synthèse interne » ou « Courrier à l’assuré ».
4. Générer et copier le brouillon.
5. Insister sur le message « validation humaine obligatoire ».

## Plan de secours

- Garder `LLM_PROVIDER=deterministic` prêt dans `.env`.
- Sélectionner deux références de sinistres à l’avance.
- Faire une génération avant la présentation pour disposer d’un historique visible.
- Conserver Swagger ouvert dans un onglet séparé pour le public technique.
- Si Groq échoue, expliquer que le fournisseur est interchangeable puis relancer en mode déterministe.
