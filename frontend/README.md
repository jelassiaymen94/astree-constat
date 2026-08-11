# Frontend — ASTREE Claims AI

Prototype React en français pour consulter les sinistres, afficher leur contexte consolidé, générer des brouillons et consulter leur historique.

## Démarrage

Lancer d’abord SQL Server, FastAPI et l’API .NET depuis la racine :

```powershell
.\start.cmd
```

Puis :

```bash
cd frontend
npm install
npm run dev
```

Ouvrir `http://localhost:5173`. Le proxy Vite redirige `/api` vers `http://localhost:5294`.

## Build

```bash
npm run build
npm run preview
```

Les contenus IA restent des brouillons soumis à validation humaine.