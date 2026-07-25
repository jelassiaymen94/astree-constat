# Processus de préparation et d’import

## Source et traçabilité

Le script `scripts/prepare_import_data.py` lit `data/raw/donnees_assurance_tunisie2.xlsx`. `import-report.json` conserve le nom et l’empreinte SHA-256 de la source.

## Règle de sélection

```text
Date_Debut_Contrat <= Date_Sinistre_Claim <= Date_Fin_Contrat
```

| Contrôle | Résultat |
|---|---:|
| Sinistres analysés | 5 252 |
| Sinistres valides | 3 460 |
| Sinistres exclus | 1 792 |
| Avant le début du contrat | 1 182 |
| Après la fin du contrat | 610 |

Aucune date ne manque et aucune relation source n’est cassée.

## Périmètre généré

| Fichier | Lignes |
|---|---:|
| `clients.csv` | 2 048 |
| `contrats.csv` | 2 179 |
| `vehicules.csv` | 2 179 |
| `sinistres.csv` | 3 460 |
| `sinistres_exclus.csv` | 1 792 |

Les CSV sont encodés en UTF-8 avec BOM, utilisent des dates `YYYY-MM-DD` et des décimales avec point.

## Contrôles avant génération

- feuilles et colonnes attendues ;
- valeurs obligatoires ;
- conversion des dates et montants ;
- unicité des identifiants ;
- périodes de contrats valides ;
- montants non négatifs ;
- relations client–contrat–véhicule–sinistre ;
- longueurs compatibles avec le schéma SQL.

Le script arrête la génération si un contrôle critique échoue.

## Commande

```bash
python scripts/prepare_import_data.py --input data/raw/donnees_assurance_tunisie2.xlsx --output-dir data/processed
```

## Import .NET

```bash
cd backend/AstreeClaims.Api
dotnet run -- --import-data --import-dir ../../data/processed
```

L’importeur relit les CSV, valide les données puis utilise une transaction SQL. Ordre :

1. clients ;
2. contrats ;
3. véhicules ;
4. sinistres.

En cas d’erreur, la transaction est annulée. Les identifiants déjà présents sont ignorés.

## Idempotence

Première exécution attendue : 2 048, 2 179, 2 179 et 3 460 insertions. Deuxième exécution : zéro insertion et toutes les lignes marquées comme ignorées.

## Vérification SQL

Exécuter `database/verify-import.sql`. Il vérifie les comptages, les relations croisées, la règle temporelle, les doublons et les répartitions par statut et type.

## Données versionnées

- source Excel dans `data/raw/` : ignorée par Git ;
- CSV dans `data/processed/` : ignorés par Git ;
- rapport JSON : versionnable ;
- scripts de préparation et de vérification : versionnés.
