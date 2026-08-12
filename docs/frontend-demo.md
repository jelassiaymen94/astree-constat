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
- Après une génération, l’éditeur d’e-mail et l’aperçu ASTREE apparaissent.
- Un envoi confirmé est visible dans Mailtrap et dans l’historique du dossier.
- `actualRecipientEmail` correspond à l’adresse de redirection prévue en mode démonstration.
- Aucun secret ou contenu sensible n’est visible à l’écran.

## Parcours pour l’encadrant

1. Montrer la liste, la recherche, les filtres et la pagination.
2. Ouvrir un dossier et expliquer le contexte consolidé.
3. Générer une synthèse avec une instruction courte.
4. Montrer le modèle, la version du prompt, la durée et l’historique.
5. Modifier le contenu dans l’éditeur, ouvrir l’aperçu ASTREE et confirmer l’envoi de démonstration.
6. Montrer le message dans Mailtrap puis l’entrée `sent` dans l’historique des e-mails.
7. Expliquer les deux flux : React → .NET → FastAPI → fournisseur IA → `GenerationLogs`, puis React → .NET → SMTP → Mailtrap → `EmailLogs`.

## Parcours pour un employé ASTREE

1. Rechercher un dossier par sa référence.
2. Lire les informations du sinistre, de l’assuré, du contrat et du véhicule.
3. Choisir « Synthèse interne », « Courrier à l’assuré » ou « Réponse contextualisée ».
4. Générer, relire et éventuellement copier le brouillon.
5. Pour un courrier, modifier l’objet ou le contenu, prévisualiser le modèle puis confirmer l’envoi de démonstration.
6. Vérifier l’historique des générations et des e-mails.
7. Insister sur la validation humaine et l’absence d’envoi automatique.

## Plan de secours

- Garder `LLM_PROVIDER=deterministic` prêt dans `.env`.
- Sélectionner deux références de sinistres à l’avance.
- Faire une génération avant la présentation pour disposer d’un historique visible.
- Conserver Swagger ouvert dans un onglet séparé pour le public technique.
- Si Groq échoue, expliquer que le fournisseur est interchangeable puis relancer en mode déterministe.
